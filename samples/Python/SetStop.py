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

import argparse
import threading
from time import sleep

from forexconnect import fxcorepy, ForexConnect, Common, EachRowListener

import common_samples

str_account = None
instrument = None
stop = None


def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)
    common_samples.add_instrument_timeframe_arguments(parser, timeframe=False)
    common_samples.add_account_arguments(parser)
    parser.add_argument('-Stop', metavar="STOP", type=float,
                        help='Stop level')
    args = parser.parse_args()

    return args


def change_trade(fx, trade):
    global str_account
    global instrument
    global stop
    amount = trade.amount
    event = threading.Event()

    offer = Common.get_offer(fx, trade.instrument)

    if not offer:
        raise Exception(
            "The instrument '{0}' is not valid".format(instrument))

    buy = fxcorepy.Constants.BUY
    sell = fxcorepy.Constants.SELL

    buy_sell = sell if trade.buy_sell == buy else buy

    order_id = trade.stop_order_id

    open_price = trade.open_rate
    amount = trade.amount
    pip_size = offer.PointSize
    if trade.buy_sell == buy:
        stopv = open_price-stop*pip_size
    else:
        stopv = open_price+stop*pip_size

    if order_id:
        request = fx.create_order_request(
            order_type=fxcorepy.Constants.Orders.STOP,
            command=fxcorepy.Constants.Commands.EDIT_ORDER,
            OFFER_ID=offer.offer_id,
            ACCOUNT_ID=str_account,
            RATE=stopv,
            TRADE_ID=trade.trade_id,
            ORDER_ID=trade.stop_order_id
        )
    else:
        request = fx.create_order_request(
            order_type=fxcorepy.Constants.Orders.STOP,
            command=fxcorepy.Constants.Commands.CREATE_ORDER,
            OFFER_ID=offer.offer_id,
            ACCOUNT_ID=str_account,
            BUY_SELL=buy_sell,
            RATE=stopv,
            AMOUNT=amount,
            TRADE_ID=trade.trade_id,
            ORDER_ID=trade.stop_order_id
        )

    if request is None:
        raise Exception("Cannot create request")

    def on_changed_order(_, __, order_row):
        nonlocal order_id
        if order_row.stop_order_id == order_id:
            print("The order has been changed. Order ID: {0:s}".format(
                order_row.trade_id))

    trades_table = fx.get_table(ForexConnect.TRADES)

    trades_listener = Common.subscribe_table_updates(trades_table,
                              on_change_callback=on_changed_order)

    try:
        resp = fx.send_request(request)

    except Exception as e:
        common_samples.print_exception(e)
        trades_listener.unsubscribe()
    else:
        # Waiting for an order to appear or timeout (default 30)
        trades_listener.unsubscribe()


def on_each_row(fx, row_data):
    global instrument
    trade = None
    if row_data.instrument == instrument:
        change_trade(fx, row_data)


def check_trades(fx, table_manager, account_id):
    orders_table = table_manager.get_table(ForexConnect.TRADES)
    if len(orders_table) == 0:
        print("There are no trades!")
    else:
        for row in orders_table:
            on_each_row(fx, row)


def main():
    global str_account
    global instrument
    global stop

    args = parse_args()
    user_id = args.l
    password = args.p
    str_url = args.u
    connection = args.c
    session_id = args.session
    pin = args.pin
    instrument = args.i
    str_account = args.account
    stop = args.Stop
    event = threading.Event()

    if not stop:
        print("Stop level must be specified")
        return


    with ForexConnect() as fx:
        fx.login(user_id, password, str_url, connection, session_id,
                 pin, common_samples.session_status_changed)

        account = Common.get_account(fx, str_account)

        table_manager = fx.table_manager

        if not account:
            raise Exception(
                "The account '{0}' is not valid".format(account))
        else:
            str_account = account.account_id
            print("AccountID='{0}'".format(str_account))

        offer = Common.get_offer(fx, instrument)

        if not offer:
            raise Exception(
                "The instrument '{0}' is not valid".format(instrument))

        check_trades(fx, table_manager, account.account_id)

        try:
            fx.logout()
        except Exception as e:
            common_samples.print_exception(e)


if __name__ == "__main__":
    main()
    input("Done! Press enter key to exit\n")
