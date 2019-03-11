# Copyright 2019 Gehtsoft USA LLC

# Licensed under the license derived from the Apache License, Version 2.0 (the "License"); 
# you may not use this file except in compliance with the License.

# You may obtain a copy of the License at

# http://fxcodebase.com/licenses/open-source/license.html

# Unless required by applicable law or agreed to in writing, software 
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

import numpy as np
import pandas as pd
import time
import argparse

from forexconnect import fxcorepy, ForexConnect, LiveHistory, ResponseListener, Common

import common_samples

from sklearn.svm import SVC
from sklearn.metrics import accuracy_score
from dateutil import parser
from datetime import timedelta

next_bars = None
difft = None
last_bar = None

def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)
    common_samples.add_direction_rate_lots_arguments(parser, direction=False, rate=False)
    common_samples.add_account_arguments(parser)
    common_samples.add_instrument_timeframe_arguments(parser)
    common_samples.add_date_arguments(parser, date_to=False)

    parser.add_argument('-nextbars', metavar="NEXTBARS", default=5, type=int,
                    help='Number of prediction bars')


    def check_positive(value):
        i_value = int(value)
        if i_value <= 0:
            raise argparse.ArgumentTypeError("%s is an invalid positive int value" % value)
        return i_value
    args = parser.parse_args()
    # specific arguments
    return args


verbose = 0

class NewBarAdded:
    def __init__(self, symbol, bars):
        self.symbol = symbol
        self.bars = bars
    
    def prediction(self):
        global next_bars
        global difft
        global last_bar

        if not last_bar:
            return

        df = pd.DataFrame(self.bars).rename(columns={'BidOpen': 'Open',
                                                     'BidHigh': 'High',
                                                     'BidLow': 'Low',
                                                     'BidClose': 'Close'
                                                     }).drop(columns=['AskOpen',
                                                                      'AskHigh',
                                                                      'AskLow',
                                                                      'AskClose']).dropna()

        y = np.where(df['Close'].shift(-1) > df['Close'], 1, -1)

        for i in range(0, next_bars + 1, 1):
            df['OC'+str(i)] = (df['Close'].shift(i)-df['Close'].shift(i+1))*100000

        df = df[next_bars+1:]
        y = y[next_bars+1:]

        y_train = y

        x_test = df[['OC0']]
        x_test = x_test[len(x_test)-1:]

        last_complete_data_frame = self.bars.tail(1)
        dt = str(last_complete_data_frame.index.values[0])
        dt = parser.parse(dt)

        if dt<=last_bar:
            return

        print()

        print('Forming next bar and recalculate prediction...')

        last_bar = dt

        for i in range(1, next_bars + 1, 1):
            x_train = df[['OC'+str(i)]]

            cls = SVC(gamma='auto').fit(x_train, y_train)

            if cls.predict(x_test)[0]>0:
                res = 'Up'
            else:
                res = 'Dn'

            dt = dt+timedelta(0, difft)
            print(str(dt)+': '+res)

#            print(cls.predict(x_test))

        return 



def on_changed(live_history):
    def _on_changed(table_listener, row_id, row):
        del table_listener, row_id
        try:
            instrument = parse_args().i
            if row.table_type == fxcorepy.O2GTableType.OFFERS and row.instrument == instrument:
                live_history.add_or_update(row)
        except Exception as e:
            common_samples.print_exception(e)
            return
            
    return _on_changed


def on_bar_added(fx, str_account, instrument):
    def _on_bar_added(history):
        newbar = NewBarAdded(instrument, history[:-1])
        newbar.prediction()

    return _on_bar_added


def session_status_changed(fx, live_history, str_user_id, str_password, str_url, str_connection,
                           reconnect_on_disconnected):
    offers_listener = None
    first_call = reconnect_on_disconnected
    orders_listener = None

    def _session_status_changed(session, status):
        nonlocal offers_listener
        nonlocal first_call
        nonlocal orders_listener
        if not first_call:
            common_samples.session_status_changed(session.trading_session_descriptors, status)
        else:
            first_call = False
        if status == fxcorepy.AO2GSessionStatus.O2GSessionStatus.CONNECTED:
            offers = fx.get_table(ForexConnect.OFFERS)
            if live_history is not None:
                on_changed_callback = on_changed(live_history)
                offers_listener = Common.subscribe_table_updates(offers, on_change_callback=on_changed_callback)
        elif status == fxcorepy.AO2GSessionStatus.O2GSessionStatus.DISCONNECTING or \
                status == fxcorepy.AO2GSessionStatus.O2GSessionStatus.RECONNECTING or \
                status == fxcorepy.AO2GSessionStatus.O2GSessionStatus.SESSION_LOST:
            if orders_listener is not None:
                orders_listener.unsubscribe()
                orders_listener = None
            if offers_listener is not None:
                offers_listener.unsubscribe()
                offers_listener = None
        elif status == fxcorepy.AO2GSessionStatus.O2GSessionStatus.DISCONNECTED and reconnect_on_disconnected:
            fx.session.login(str_user_id, str_password, str_url, str_connection)

    return _session_status_changed


def main():
    global next_bars
    global difft
    global last_bar

    args = parse_args()
    str_user_id = args.l
    str_password = args.p
    str_url = args.u
    str_connection = args.c
    str_session_id = args.session
    str_pin = args.pin
    str_instrument = args.i
    str_timeframe = args.timeframe
    date_from = args.datefrom
    str_account = args.account
    next_bars = args.nextbars
    print("")

    with ForexConnect() as fx:
        try:
            fx.login(str_user_id, str_password, str_url,
                     str_connection, str_session_id, str_pin,
                     common_samples.session_status_changed)

            current_unit, current_size = ForexConnect.parse_timeframe(str_timeframe)
            difft = common_samples.convert_timeframe_to_seconds(current_unit, current_size)

            if current_unit == fxcorepy.O2GTimeFrameUnit.TICK:
                # we can't from candles from the t1 time frame
                raise Exception("Do NOT use t* time frame")
            
            live_history = LiveHistory.LiveHistoryCreator(str_timeframe)
            on_bar_added_callback = on_bar_added(fx, str_account, str_instrument)
            live_history.subscribe(on_bar_added_callback)

            session_status_changed_callback = session_status_changed(fx, live_history, str_user_id, str_password,
                                                                     str_url, str_connection, True)
            session_status_changed_callback(fx.session, fx.session.session_status)
            fx.set_session_status_listener(session_status_changed_callback)

            print("Getting history...")
            history = fx.get_history(str_instrument, str_timeframe, date_from)

            df = pd.DataFrame(history).rename(columns={'BidOpen': 'Open',
                                                       'BidHigh': 'High',
                                                       'BidLow': 'Low',
                                                       'BidClose': 'Close'
                                                       }).drop(columns=['AskOpen',
                                                                        'AskHigh',
                                                                        'AskLow',
                                                                        'AskClose']).set_index('Date').dropna()



            y = np.where(df['Close'].shift(-1) > df['Close'], 1, -1)

            for i in range(0, next_bars + 1, 1):
                df['OC'+str(i)] = (df['Close'].shift(i)-df['Close'].shift(i+1))*100000

            df = df[next_bars+1:]
            y = y[next_bars+1:]

            y_train = y

            x_test = df[['OC0']]
            x_test = x_test[len(x_test)-1:]

            live_history.history = history

            on_bar_added_callback(live_history.history)

            print()
            print('Next '+str(next_bars)+' bar prediction: ')

            last_complete_data_frame = live_history.history.tail(1)
            dt = str(last_complete_data_frame.index.values[0])
            dt = parser.parse(dt)
            dt0 = dt

            for i in range(1, next_bars + 1, 1):
                x_train = df[['OC'+str(i)]]

                cls = SVC(gamma='auto').fit(x_train, y_train)

                if cls.predict(x_test)[0]>0:
                    res = 'Up'
                else:
                    res = 'Dn'
                dt = dt+timedelta(0, difft)
                print(str(dt)+': '+res)
#                print(cls.predict(x_test)[0])

            last_bar = dt0

            while True:
                time.sleep(60)

        except Exception as e:
            common_samples.print_exception(e)
        try:
            fx.set_session_status_listener(session_status_changed(fx, None, str_user_id, str_password,
                                                                  str_url, str_connection, False))
            fx.logout()
        except Exception as e:
            common_samples.print_exception(e)


if __name__ == "__main__":
    main()
    print("")
    input("Done! Press enter key to exit\n")