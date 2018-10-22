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

        print(df)

        df = bbands('Close', df, 2, 3)

        df = adx(df, 5)

        df = macd('Close', df, 3, 4, 5)

        df = ma('Close', df, 3)

        df = rsi('Close', df, 10)

        print(df)

        fx.logout()

if __name__ == "__main__":
    main()
    print("")
    input("Done! Press enter key to exit\n")
