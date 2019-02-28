# Copyright 2018 Gehtsoft USA LLC

# Licensed under the license derived from the Apache License, Version 2.0 (the "License"); 
# you may not use this file except in compliance with the License.

# You may obtain a copy of the License at

# http://fxcodebase.com/licenses/open-source/license.html

# Unless required by applicable law or agreed to in writing, software 
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

import argparse

import pandas as pd
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


def ma(name, df, n):
    column_ma = pd.Series(df[name].rolling(window=n).mean(),
                          name='MA_' + name + '_' + str(n))
    df = df.join(column_ma)

    return df


def adx(df, n):
    i = 0
    upi = []
    doi = []
    while i + 1 <= df.index[-1]:
        up_move = df.at[i + 1, 'High'] - df.at[i, 'High']
        do_move = df.at[i, 'Low'] - df.at[i + 1, 'Low']
        if up_move > do_move and up_move > 0:
            upd = up_move
        else:
            upd = 0
        upi.append(upd)
        if do_move > up_move and do_move > 0:
            dod = do_move
        else:
            dod = 0
        doi.append(dod)
        i = i + 1
    i = 0
    tr_l = [0]
    while i < df.index[-1]:
        tr = max(df.at[i + 1, 'High'],
                 df.at[i, 'Close']) - min(df.at[i + 1, 'Low'],
                                          df.at[i, 'Close'])
        tr_l.append(tr)
        i = i + 1
    tr_s = pd.Series(tr_l)
    atr = tr_s.ewm(span=n, min_periods=n).mean()
    upi = pd.Series(upi)
    doi = pd.Series(doi)
    posdi = upi.ewm(span=n, min_periods=n - 1).mean()/atr
    negdi = doi.ewm(span=n, min_periods=n - 1).mean()/atr
    adx_r = 100 * abs(posdi - negdi) / (posdi + negdi)
    rowadx = adx_r.ewm(span=n, min_periods=n - 1)
    meanadx = rowadx.mean()
    columnadx = meanadx.rename('ADX_' + str(n) + '_' + str(n))
    df = df.join(columnadx)
    return df


def macd(name, df, n_fast, n_slow, n_signal):
    emafast = df[name].ewm(span=n_fast, min_periods=n_slow - 1).mean()
    emaslow = df[name].ewm(span=n_slow, min_periods=n_slow - 1).mean()
    columnmacd = pd.Series(emafast - emaslow,
                           name='MACD_' + str(n_fast) + '_' + str(n_slow))
    rowmacd = columnmacd.ewm(span=n_signal, min_periods=n_signal - 1)
    meanmacd = rowmacd.mean()
    macdsign = meanmacd.rename('MACDsign_' + str(n_fast) + '_' + str(n_slow))
    macddiff = pd.Series(columnmacd - macdsign,
                         name='MACDdiff_' + str(n_fast) + '_' + str(n_slow))
    df = df.join(columnmacd)
    df = df.join(macdsign)
    df = df.join(macddiff)
    return df


def rsi(name, df, n):
    i = 0
    upi = [0]
    doi = [0]
    while i + 1 <= df.index[-1]:
        diff = df.at[i + 1, name] - df.at[i, name]
        if diff > 0:
            upd = diff
        else:
            upd = 0
        upi.append(upd)
        if diff < 0:
            dod = -diff
        else:
            dod = 0
        doi.append(dod)
        i = i + 1
    upi = pd.Series(upi)
    doi = pd.Series(doi)
    posdi = upi.ewm(span=n, min_periods=n - 1).mean()
    negdi = doi.ewm(span=n, min_periods=n - 1).mean()
    columnrsi = pd.Series(100 * posdi / (posdi + negdi), name='RSI_' + str(n))
    df = df.join(columnrsi)
    return df


def bbands(name, df, n, d):
    ma = pd.Series(df[name].rolling(window=n).mean())
    msd = pd.Series(df[name].rolling(window=n).std())
    b11 = ma + d*msd
    b1 = pd.Series(b11, name='Bollinger_TL_' + name + "_" + str(n))
    df = df.join(b1)
    b21 = ma - d*msd
    b2 = pd.Series(b21, name='Bollinger_BL_' + name + "_" + str(n))
    df = df.join(b2)
    return df


def zigzag(df, depth, deviation, backstep, pip_size):
    i = depth

    zigzag_buffer = pd.Series(0*df['Close'], name='ZigZag')
    high_buffer = pd.Series(0*df['Close'])
    low_buffer = pd.Series(0*df['Close'])

    curlow = 0
    curhigh = 0
    lasthigh = 0
    lastlow = 0

    whatlookfor = 0

    lows = pd.Series(df['Low'].rolling(depth).min())
    highs = pd.Series(df['High'].rolling(depth).max())

    while i + 1 <= df.index[-1]:
        extremum = lows[i]
        if extremum == lastlow:
            extremum = 0
        else:
            lastlow = extremum
            if df.at[i, 'Low']-extremum > deviation*pip_size:
                extremum = 0
            else:
                for back in range(1, backstep):
                    pos = i-back
                    if low_buffer[pos] != 0 and low_buffer[pos] > extremum:
                        low_buffer[pos] = 0

        if df.at[i, 'Low'] == extremum:
            low_buffer[i] = extremum
        else:
            low_buffer[i] = 0

        extremum = highs[i]
        if extremum == lasthigh:
            extremum = 0
        else:
            lasthigh = extremum
            if extremum - df.at[i, 'High'] > deviation*pip_size:
                extremum = 0
            else:
                for back in range(1, backstep):
                    pos = i - back
                    if high_buffer[pos] != 0 and high_buffer[pos] < extremum:
                        high_buffer[pos] = 0

        if df.at[i, 'High'] == extremum:
            high_buffer[i] = extremum
        else:
            high_buffer[i] = 0

        i = i + 1

    lastlow = 0
    lasthigh = 0

    i = depth

    while i + 1 <= df.index[-1]:
        if whatlookfor == 0:
            if lastlow == 0 and lasthigh == 0:
                if high_buffer[i] != 0:
                    lasthigh = df.at[i, 'High']
                    lasthighpos = i
                    whatlookfor = -1
                    zigzag_buffer[i] = lasthigh
                if low_buffer[i] != 0:
                    lastlow = df.at[i, 'Low']
                    lastlowpos = i
                    whatlookfor = 1
                    zigzag_buffer[i] = lastlow
        elif whatlookfor == 1:
            if low_buffer[i] != 0 and low_buffer[i] < lastlow and high_buffer[i] == 0:
                zigzag_buffer[lastlowpos] = 0
                lastlowpos = i
                lastlow = low_buffer[i]
                zigzag_buffer[i] = lastlow
            if high_buffer[i] != 0 and low_buffer[i] == 0:
                lasthigh = high_buffer[i]
                lasthighpos = i
                zigzag_buffer[i] = lasthigh
                whatlookfor = -1
        elif whatlookfor == -1:
            if high_buffer[i] != 0 and high_buffer[i] > lasthigh and low_buffer[i] == 0:
                zigzag_buffer[lasthighpos] = 0
                lasthighpos = i
                lasthigh = high_buffer[i]
                zigzag_buffer[i] = lasthigh
            if low_buffer[i] != 0 and high_buffer[i] == 0:
                lastlow = low_buffer[i]
                lastlowpos = i
                zigzag_buffer[i] = lastlow
                whatlookfor = 1

        i = i + 1

    df = df.join(zigzag_buffer)
    return df


def get_pipsize(fx, s_instrument):
    table_manager = fx.table_manager
    offers_table = table_manager.get_table(ForexConnect.OFFERS)
    for offer_row in offers_table:
        if offer_row.instrument == s_instrument:
            return offer_row.PointSize


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
    quotes_count = args.quotescount
    date_from = args.datefrom
    date_to = args.dateto

    with ForexConnect() as fx:
        fx.login(str_user_id, str_password, str_url,
                 str_connection, str_session_id, str_pin,
                 common_samples.session_status_changed)

        print("")
        print("Requesting a price history...")
        history = fx.get_history(str_instrument,
                                 str_timeframe,
                                 date_from,
                                 date_to,
                                 quotes_count)
        current_unit, _ = ForexConnect.parse_timeframe(str_timeframe)

        df = pd.DataFrame(history).rename(columns={'BidOpen': 'Open',
                                                   'BidHigh': 'High',
                                                   'BidLow': 'Low',
                                                   'BidClose': 'Close'
                                                   }).drop(columns=['AskOpen',
                                                                    'AskHigh',
                                                                    'AskLow',
                                                                    'AskClose'
                                                                    ]).dropna()

        df = df.dropna()

        pip_size = get_pipsize(fx, str_instrument)

        print(df)

        df = bbands('Close', df, 2, 3)

        df = adx(df, 5)

        df = macd('Close', df, 3, 4, 5)

        df = ma('Close', df, 3)

        df = rsi('Close', df, 10)

        df = zigzag(df, 12, 5, 3, pip_size)

        print(df)

        fx.logout()

if __name__ == "__main__":
    main()
    print("")
    input("Done! Press enter key to exit\n")