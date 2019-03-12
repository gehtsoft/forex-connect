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

from sklearn.linear_model import LinearRegression
from sklearn.metrics import mean_squared_error
import pandas as pd
import argparse

from forexconnect import ForexConnect

import common_samples


def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)
    common_samples.add_instrument_timeframe_arguments(parser)
    common_samples.add_date_arguments(parser)
    common_samples.add_max_bars_arguments(parser)
    parser.add_argument('-prevbars', metavar="PREVBARS", default=50, type=int,
                        help='Number of bars for prediction')
    parser.add_argument('-nextbars', metavar="NEXTBARS", default=5, type=int,
                        help='Number of prediction bars')
    args = parser.parse_args()
    return args


def main():
    args = parse_args()
    user_id = args.l
    password = args.p
    url = args.u
    connection = args.c
    session_id = args.session
    pin = args.pin
    instrument = args.i
    timeframe = args.timeframe
    quotes_count = args.quotescount
    date_from = args.datefrom
    date_to = args.dateto
    prev_bars = args.prevbars
    next_bars = args.nextbars

    with ForexConnect() as fx:
        fx.login(user_id, password, url, connection, session_id, pin,
                 common_samples.session_status_changed)

        nd_array = fx.get_history(instrument, timeframe,
                                  date_from, date_to, quotes_count)
        df = pd.DataFrame(nd_array).rename(columns={'BidOpen': 'Open',
                                                    'BidHigh': 'High',
                                                    'BidLow': 'Low',
                                                    'BidClose': 'Close'
                                                    }).drop(columns=['AskOpen',
                                                                     'AskHigh',
                                                                     'AskLow',
                                                                     'AskClose']).set_index('Date').dropna()

        for i in range(1, prev_bars + 1, 1):
            df[str(i)] = df['Close'].shift(i)

        df = df.dropna()

        x = df.drop(['Open', 'Close', 'Volume', 'High', 'Low'], axis=1)

        y = df['Close']

        length = int(len(df))
        t1 = length - next_bars
        t2 = t1 - prev_bars

        # Train dataset

        x_train = x[t2:t1]

        y_train = y[t2:t1]

        # Test dataset

        x_test = x[t1:]

        y_test = y[t1:]

        linear = LinearRegression().fit(x_train, y_train)

        predicted_price = linear.predict(x_test)

        predicted_price = pd.DataFrame(predicted_price, index=y_test.index,
                                       columns=['price'])

        r2_score = linear.score(x[t1:], y[t1:]) * 100

        res = predicted_price['price']

        print()
        print("Predicted prices:")
        print(list(res))

        print()
        print("Real prices:")
        print(list(y_test))

        print()
        print("Mean squared error: ", mean_squared_error(y_test, predicted_price))
        print("Variance score: {0:.2f}".format(r2_score))

        print()

        fx.logout()


if __name__ == '__main__':
    main()
