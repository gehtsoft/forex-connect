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
    parser.add_argument('-PeriodMA1', metavar="PERIODMA1", default=5, type=int,
                        help='Period of MA1')
    parser.add_argument('-PeriodMA2', metavar="PERIODMA2", default=10, type=int,
                        help='Period of MA2')
    parser.add_argument('-PeriodMA3', metavar="PERIODMA3", default=15, type=int,
                        help='Period of MA3')
    parser.add_argument('-TrainPercent', metavar="TRAINPERCENT", default=80, type=int,
                        help='Percent of training data')
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
    period_ma1 = args.PeriodMA1
    period_ma2 = args.PeriodMA2
    period_ma3 = args.PeriodMA3
    train_percent = args.TrainPercent

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
        df['S_1'] = df['Close'].rolling(window=period_ma1).mean()

        df['S_2'] = df['Close'].rolling(window=period_ma2).mean()

        df['S_3'] = df['Close'].rolling(window=period_ma3).mean()

        df = df.dropna()

        x = df[['S_1', 'S_2', 'S_3']]

        y = df['Close']

        t = train_percent/100.

        t = int(t * len(df))

        # Train dataset

        x_train = x[:t]

        y_train = y[:t]

        # Test dataset

        x_test = x[t:]

        y_test = y[t:]

        linear = LinearRegression().fit(x_train, y_train)

        predicted_price = linear.predict(x_test)

        print('Coefficients: \n', linear.coef_)

        print("Mean squared error: ", mean_squared_error(y_test, predicted_price))

        r2_score = linear.score(x[t:], y[t:]) * 100

        print("Variance score: {0:.2f}".format(r2_score))

        fx.logout()


if __name__ == '__main__':
    main()
