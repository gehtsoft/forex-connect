from sklearn.svm import SVC
from sklearn.metrics import accuracy_score
import numpy as np
import pandas as pd
import argparse

from forexconnect import ForexConnect, fxcorepy

import common_samples


def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)
    common_samples.add_instrument_timeframe_arguments(parser)
    common_samples.add_date_arguments(parser)
    common_samples.add_max_bars_arguments(parser)

    args = parser.parse_args()
    return args


def main():
    args = parse_args()
    user_id = args.l
    password = args.p
    str_url = args.u
    connection = args.c
    session_id = args.session
    pin = args.pin
    instrument = args.i
    str_timeframe = args.timeframe
    quotes_count = args.quotescount
    date_from = args.datefrom
    date_to = args.dateto

    with ForexConnect() as fx:
        fx.login(user_id, password, str_url, connection, session_id, pin,
                 common_samples.session_status_changed)

        timeframe, _ = ForexConnect.parse_timeframe(str_timeframe)
        if not timeframe:
            raise Exception("ResponseListener: time frame is incorrect")
        nd_array = fx.get_history(instrument, str_timeframe,
                                 date_from, date_to, quotes_count)
        df = pd.DataFrame(nd_array).rename(columns={'BidOpen': 'Open',
                                                    'BidHigh': 'High',
                                                    'BidLow': 'Low',
                                                    'BidClose': 'Close'
                                                    }).drop(columns=['AskOpen',
                                                                     'AskHigh',
                                                                     'AskLow',
                                                                     'AskClose']).set_index('Date').dropna()

        y = np.where(df['Close'].shift(-1) > df['Close'], 1, -1)

        df['Open-Close'] = df.Open - df.Close

        df['High-Low'] = df.High - df.Low

        x = df[['Open-Close', 'High-Low']]

        split_percentage = 0.8

        split = int(split_percentage * len(df))

        x_train = x[:split]

        y_train = y[:split]

        x_test = x[split:]

        y_test = y[split:]

        cls = SVC().fit(x_train, y_train)

        accuracy_train = accuracy_score(y_train, cls.predict(x_train))

        accuracy_test = accuracy_score(y_test, cls.predict(x_test))

        print('Train Accuracy:{: .2f}%'.format(accuracy_train*100))

        print('Test Accuracy:{: .2f}%'.format(accuracy_test*100))
        
        fx.logout()


if __name__ == '__main__':
    main()
