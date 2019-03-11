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

from forexconnect import fxcorepy, ForexConnect, Common

import common_samples

profit_level = None
loss_level = None
str_account = None
instrument = None


def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)
    common_samples.add_instrument_timeframe_arguments(parser, timeframe=False)
    common_samples.add_account_arguments(parser)
    parser.add_argument('-Profit_Level', metavar="PROFIT_LEVEL", type=float,
                        help='Profit level')
    parser.add_argument('-Loss_Level', metavar="LOSS_LEVEL", type=float,
                        help='Loss level')
    args = parser.parse_args()

    return args


def close_trade(fx, trade):
    global str_account
    global instrument
    amount = trade.amount
    event = threading.Event()

    offer = Common.get_offer(fx, trade.instrument)

    if not offer:
        raise Exception(
            "The instrument '{0}' is not valid".format(instrument))

    buy = fxcorepy.Constants.BUY
    sell = fxcorepy.Constants.SELL

    buy_sell = sell if trade.buy_sell == buy else buy
    order_id = None

    request = fx.create_order_request(
        order_type=fxcorepy.Constants.Orders.TRUE_MARKET_CLOSE,
        OFFER_ID=offer.offer_id,
        ACCOUNT_ID=str_account,
        BUY_SELL=buy_sell,
        AMOUNT=amount,
        TRADE_ID=trade.trade_id
    )

    if request is None:
        raise Exception("Cannot create request")

    def on_added_closed_trade(_, __, trade_row):
        nonlocal order_id
        if trade_row.close_order_id == order_id:
            print("For the order: OrderID = {0} \
                the following positions have been closed: ".format(order_id))
            print("Closed Trade ID: {0:s}; Amount: {1:d}; \
                Closed Rate: {2:.5f}".format(trade_row.trade_id,
                                             trade_row.amount,
                                             trade_row.close_rate))
            event.set()

    def on_added_order(_, __, order_row):
        nonlocal order_id
        if order_row.order_id == order_id:
            print("The order has been added. Order ID: {0:s}, \
            Rate: {1:.5f}, Time In Force: {2:s}".format(
                order_row.order_id, order_row.rate, order_row.time_in_force))

    def on_deleted_order(_, __, row_data):
        nonlocal order_id
        if row_data.order_id == order_id:
            print("The order has been deleted. Order ID: \
            {0}".format(row_data.order_id))

    closed_trades_table = fx.get_table(ForexConnect.CLOSED_TRADES)
    orders_table = fx.get_table(ForexConnect.ORDERS)

    trades_listener = Common.subscribe_table_updates(closed_trades_table,
                                   on_add_callback=on_added_closed_trade)
    orders_listener = Common.subscribe_table_updates(orders_table,
                                   on_add_callback=on_added_order,
                              on_delete_callback=on_deleted_order)

    try:
        resp = fx.send_request(request)
        order_id = resp.order_id

    except Exception as e:
        common_samples.print_exception(e)
        trades_listener.unsubscribe()
        orders_listener.unsubscribe()
    else:
        # Waiting for an order to appear or timeout (default 30)
        if not event.wait(30):
            print("Response waiting timeout expired.\n")
        else:
            sleep(1)
        trades_listener.unsubscribe()
        orders_listener.unsubscribe()


def on_each_row(fx, row_data):
    global profit_level
    global loss_level
    global instrument
    trade = None
    if row_data.instrument == instrument:
        pl = row_data["pl"]
        if profit_level:
            if pl >= profit_level:
                trade = row_data
        if loss_level:
            if pl <= loss_level:
                trade = row_data
        if trade:
            close_trade(fx, row_data)


def check_trades(fx, table_manager):
    orders_table = table_manager.get_table(ForexConnect.TRADES)
    if len(orders_table) == 0:
        print("There are no trades!")
    else:
        for row in orders_table:
            on_each_row(fx, row)


def main():
    global profit_level
    global loss_level
    global str_account
    global instrument

    args = parse_args()
    user_id = args.l
    password = args.p
    str_url = args.u
    connection = args.c
    session_id = args.session
    pin = args.pin
    instrument = args.i
    str_account = args.account
    profit_level = args.Profit_Level
    loss_level = args.Loss_Level

    if not profit_level and not loss_level:
        print("Profit and/or loss level must be specified")
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

        check_trades(fx, table_manager)

        try:
            fx.logout()
        except Exception as e:
            common_samples.print_exception(e)


if __name__ == "__main__":
    main()
    input("Done! Press enter key to exit\n")
