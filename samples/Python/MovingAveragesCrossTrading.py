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


def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)
    common_samples.add_direction_rate_lots_arguments(parser, direction=False, rate=False)
    common_samples.add_account_arguments(parser)
    common_samples.add_instrument_timeframe_arguments(parser)
    common_samples.add_date_arguments(parser, date_to=False)

    def check_positive(value):
        i_value = int(value)
        if i_value <= 0:
            raise argparse.ArgumentTypeError("%s is an invalid positive int value" % value)
        return i_value
    parser.add_argument('-orderscount', metavar="COUNT", default=3, type=check_positive,
                        help='How many orders will the strategy create before going out.')
    parser.add_argument('-shortperiods', metavar="COUNT", default=5, type=check_positive,
                        help='Short MA periods count.')
    parser.add_argument('-longperiods', metavar="COUNT", default=15, type=check_positive,
                        help='Long MA periods count.')
    args = parser.parse_args()
    # specific arguments
    return args


verbose = 0
order_request_id = ""


class MovingAverageCrossStrategy:
    def __init__(self, symbol, bars, short_window, long_window):
        self.symbol = symbol
        self.bars = bars
        self.short_window = short_window
        self.long_window = long_window
    
    def generate_signals(self):
        """Returns the DataFrame of symbols containing the signals
                to go long (purchase to sell then, expecting the price to grow),
                short (expecting the price to fall)
                or hold
                (1, -1 or 0)."""

        # Two-dimensional size-mutable, potentially heterogeneous tabular
        # data structure with labeled axes (rows and columns)
        signals = pd.DataFrame(index=self.bars.index)
        # fill column with zeroes
        signals['signal'] = 0.0

        short_mean = self.bars['AskClose'].rolling(self.short_window).mean()
        # One-dimensional ndarray with axis labels (including time series)
        signals['short_mavg'] = short_mean
        
        long_mean = self.bars['AskClose'].rolling(self.long_window).mean()
        signals['long_mavg'] = long_mean
        
        # If MA on the short distance is greater then MA on a long distance - considering the price is growing
        # (uptrend), so we tend to buy in attempt to gain profit by selling later for a higher price

        # from the item with the [self.short_window:] index in the column 'signal'
        signals['signal'][self.short_window:] = np.where(
            # Note that comparing number to NaN is always false
            signals['short_mavg'][self.short_window:]
            # Return elements, either from x or y, depending on condition
            > signals['long_mavg'][self.short_window:], 1.0, 0.0)
        
        # Calculates the difference of a DataFrame element compared with another element in the DataFrame
        # (default is the element in the same column of the previous row
        signals['positions'] = signals['signal'].diff()
        
        buy_count = 0
        sell_count = 0
        
        for signal in np.nditer(signals['positions']):
            if pd.notnull(signal) and signal > 0:
                buy_count = buy_count + 1
            elif pd.notnull(signal) and signal < 0:
                sell_count = sell_count + 1
        
        if verbose:
            print("Signals: buy = {0}, sell = {1}".format(str(buy_count), str(sell_count)))
        
        return signals


order_created_count = 0


def on_request_completed(request_id, response):
    del request_id, response
    global order_created_count
    order_created_count += 1
    return True


def on_order_added(listener, row_id, row_data):
    del listener, row_id
    global order_request_id
    if order_request_id == row_data.request_id:
        print("\nOrder has been added:")
        print("OrderID = {0:s}, Type = {1:s}, BuySell = {2:s}, Rate = {3:.5f}, TimeInForce = {4:s}".format(
              row_data.order_id, row_data.type,
              row_data.buy_sell, row_data.rate,
              row_data.time_in_force))


def create_open_market_order(fx, str_account, instrument, lots, buy_sell):
    try:
        account = Common.get_account(fx)

        if not account:
            raise Exception(
                "The account '{0}' is not valid".format(str_account))
        else:
            str_account = account.account_id
        
        offer = Common.get_offer(fx, instrument)
        
        if not offer:
            raise Exception(
                "The instrument '{0}' is not valid".format(instrument))
        
        login_rules = fx.login_rules
        trading_settings_provider = login_rules.trading_settings_provider
        base_unit_size = trading_settings_provider.get_base_unit_size(instrument, account)
        amount = base_unit_size * lots
        
        print("\nCreating order for instrument {0}...".format(offer.instrument))
        response_listener = ResponseListener(fx.session, on_request_completed_callback=on_request_completed)
        try:
            request = fx.create_order_request(
                order_type=fxcorepy.Constants.Orders.TRUE_MARKET_OPEN,
                ACCOUNT_ID=str_account,
                BUY_SELL=buy_sell,
                AMOUNT=amount,
                SYMBOL=offer.instrument
            )
            global order_request_id
            order_request_id = request.request_id
            fx.send_request_async(request, response_listener)
        except Exception as e:
            print("Failed")
            common_samples.print_exception(e)
            return

    except Exception as e:
        common_samples.print_exception(e)
        return


# On offers (prices) change
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


def on_bar_added(fx, str_account, instrument, lots, short_period, long_periods):
    def _on_bar_added(history):
        mac = MovingAverageCrossStrategy(instrument, history[:-1], short_window=short_period,
                                         long_window=long_periods)
        signals = mac.generate_signals()  # DataFrame

        last_signal = signals['positions'].tail(1).values[0]
        if last_signal < 0:
            buy_sell = 'S'
        elif last_signal > 0:
            buy_sell = 'B'
        else:
            return
        create_open_market_order(fx, str_account, instrument, lots, buy_sell)
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
            orders_table = fx.get_table(ForexConnect.ORDERS)
            orders_listener = Common.subscribe_table_updates(orders_table, on_add_callback=on_order_added)

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
    str_lots = args.lots
    orders_count = args.orderscount
    short_periods = args.shortperiods
    long_periods = args.longperiods
    print("")

    with ForexConnect() as fx:
        try:
            fx.login(str_user_id, str_password, str_url,
                     str_connection, str_session_id, str_pin,
                     common_samples.session_status_changed)

            current_unit, _ = ForexConnect.parse_timeframe(str_timeframe)
            if current_unit == fxcorepy.O2GTimeFrameUnit.TICK:
                # we can't from candles from the t1 time frame
                raise Exception("Do NOT use t* time frame")
            
            live_history = LiveHistory.LiveHistoryCreator(str_timeframe)
            on_bar_added_callback = on_bar_added(fx, str_account, str_instrument, str_lots, short_periods, long_periods)
            live_history.subscribe(on_bar_added_callback)

            session_status_changed_callback = session_status_changed(fx, live_history, str_user_id, str_password,
                                                                     str_url, str_connection, True)
            session_status_changed_callback(fx.session, fx.session.session_status)
            fx.set_session_status_listener(session_status_changed_callback)

            print("Getting history...")
            history = fx.get_history(str_instrument, str_timeframe, date_from)

            print("Updating history...")
            live_history.history = history
            # apply for current history
            on_bar_added_callback(live_history.history)

            print("")
            while order_created_count < orders_count:
                print("")
                print("Waiting 1 minute...")
                time.sleep(60)

            print("")
            print("Orders created: {0}".format(str(order_created_count)))
            print("")

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