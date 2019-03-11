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
import traceback
from time import sleep

from forexconnect import fxcorepy, ForexConnect, Common, EachRowListener

import common_samples

stop = None
orderid = None
peggedstop = None
pegstoptype = None
limit = None
peggedlimit = None
peglimittype = None
order_changed = None


def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)
    parser.add_argument('-orderid', metavar="OrderID", required=True,
                        help='Order ID')
    parser.add_argument('-peggedstop', metavar="Peggedstop",
                        help='Pegged stop')
    parser.add_argument('-pegstoptype', metavar="Pegstoptype",
                        help='Peg stop type')
    parser.add_argument('-stop', metavar="STOP", type=float, default=0,
                        help='Stop level')
    parser.add_argument('-peggedlimit', metavar="Peggedlimit",
                        help='Pegged limit')
    parser.add_argument('-peglimittype', metavar="Peglimittype",
                        help='Peg limit type')
    parser.add_argument('-limit', metavar="LIMIT", type=float, default=0, 
                        help='Limit level')
    args = parser.parse_args()

    return args


def change_order(fx, order):
    global stop
    global peggedstop
    global pegstoptype
    global limit
    global peggedlimit
    global peglimittype
    global order_changed

    amount = order.amount

    buy = fxcorepy.Constants.BUY
    sell = fxcorepy.Constants.SELL

    offers_table = fx.get_table(ForexConnect.OFFERS)
    offers = offers_table.get_rows_by_column_value("offer_id", order.offer_id)
    for offer_row in offers:
        offer = offer_row
        break

    str_account = order.account_id

    buy_sell = sell if order.buy_sell == buy else buy

    stop_order_id = order.stop_order_id
    limit_order_id = order.limit_order_id

    open_price = order.rate
    amount = order.amount

    def on_changed_order(_, __, order_row):
        nonlocal stop_order_id
        nonlocal limit_order_id
        global order_changed
        if not order_changed and (order_row.stop_order_id == stop_order_id or order_row.limit_order_id == limit_order_id):
            order_changed = True
            print("The order has been changed. Order ID: {0:s}".format(
                order_row.order_id))

    def on_added_order(_, __, order_row):
        nonlocal stop_order_id
        nonlocal limit_order_id
        global order_changed
        if not order_changed and (order_row.stop_order_id == stop_order_id or order_row.limit_order_id == limit_order_id):
            order_changed = True
            print("The order has been added. Order ID: {0:s}".format(
                order_row.order_id))

    orders_table = fx.get_table(ForexConnect.ORDERS)

    orders_listener = Common.subscribe_table_updates(orders_table,
                              on_change_callback=on_changed_order,
                              on_add_callback=on_added_order)

    if buy_sell == 'S':
        stopv = -stop
        limitv = limit
    else:
        stopv = stop
        limitv = -limit

    if stop:
        if peggedstop:
            if stop_order_id:
                stop_request = fx.create_order_request(
                    order_type=fxcorepy.Constants.Orders.STOP,
                    command=fxcorepy.Constants.Commands.EDIT_ORDER,
                    ACCOUNT_ID=str_account,
                    PEG_TYPE=pegstoptype,
                    PEG_OFFSET=stopv,
                    ORDER_ID=order.stop_order_id
                )
            else:
                stop_request = fx.create_order_request(
                    order_type=fxcorepy.Constants.Orders.STOP,
                    command=fxcorepy.Constants.Commands.CREATE_ORDER,
                    ACCOUNT_ID=str_account,
                    BUY_SELL=buy_sell,
                    PEG_TYPE=pegstoptype,
                    PEG_OFFSET=stopv,
                    OFFER_ID=offer.offer_id,
                    AMOUNT=amount,
                    TRADE_ID=order.trade_id
                )
        else:
            if stop_order_id:
                stop_request = fx.create_order_request(
                    order_type=fxcorepy.Constants.Orders.STOP,
                    command=fxcorepy.Constants.Commands.EDIT_ORDER,
                    ACCOUNT_ID=str_account,
                    RATE=stop,
                    ORDER_ID=order.stop_order_id
                )
            else:
                stop_request = fx.create_order_request(
                    order_type=fxcorepy.Constants.Orders.STOP,
                    command=fxcorepy.Constants.Commands.CREATE_ORDER,
                    ACCOUNT_ID=str_account,
                    BUY_SELL=buy_sell,
                    OFFER_ID=offer.offer_id,
                    RATE=stop,
                    AMOUNT=amount,
                    TRADE_ID=order.trade_id,
            )

    if limit>0:
        if peggedlimit:
            if limit_order_id:
                limit_request = fx.create_order_request(
                    order_type=fxcorepy.Constants.Orders.LIMIT,
                    command=fxcorepy.Constants.Commands.EDIT_ORDER,
                    ACCOUNT_ID=str_account,
                    PEG_TYPE=peglimittype,
                    PEG_OFFSET=limitv,
                    ORDER_ID=order.limit_order_id
                )
            else:
                limit_request = fx.create_order_request(
                    order_type=fxcorepy.Constants.Orders.LIMIT,
                    command=fxcorepy.Constants.Commands.CREATE_ORDER,
                    ACCOUNT_ID=str_account,
                    BUY_SELL=buy_sell,
                    PEG_TYPE=peglimittype,
                    PEG_OFFSET=limitv,
                    OFFER_ID=offer.offer_id,
                    AMOUNT=amount,
                    TRADE_ID=order.trade_id
                )
        else:
            if limit_order_id:
                limit_request = fx.create_order_request(
                    order_type=fxcorepy.Constants.Orders.LIMIT,
                    command=fxcorepy.Constants.Commands.EDIT_ORDER,
                    ACCOUNT_ID=str_account,
                    RATE=limit,
                    ORDER_ID=order.limit_order_id
                )
            else:
                limit_request = fx.create_order_request(
                    order_type=fxcorepy.Constants.Orders.LIMIT,
                    command=fxcorepy.Constants.Commands.CREATE_ORDER,
                    ACCOUNT_ID=str_account,
                    BUY_SELL=buy_sell,
                    OFFER_ID=offer.offer_id,
                    RATE=limit,
                    AMOUNT=amount,
                    TRADE_ID=order.trade_id,
                )

    if stop_request is None and limit_request is None:
        raise Exception("Cannot create request")

    try:
        stop_resp = fx.send_request(stop_request)
        limit_resp = fx.send_request(limit_request)

    except Exception as e:
        common_samples.print_exception(e)
        orders_listener.unsubscribe()
    else:
        sleep(1)
        orders_listener.unsubscribe()


def on_each_row(fx, row_data):
    global orderid

    if row_data.order_id == orderid:
        change_order(fx, row_data)


def check_trades(fx, table_manager):
    orders_table = table_manager.get_table(ForexConnect.ORDERS)
    if len(orders_table) == 0:
        print("There are no orders!")
    else:
        for row in orders_table:
            on_each_row(fx, row)


def main():
    global stop
    global limit
    global orderid
    global peggedstop
    global pegstoptype
    global peggedlimit
    global peglimittype

    args = parse_args()
    user_id = args.l
    password = args.p
    str_url = args.u
    connection = args.c
    session_id = args.session
    pin = args.pin
    orderid = args.orderid
    stop = args.stop
    peggedstop = args.peggedstop
    pegstoptype = args.pegstoptype
    limit = args.limit
    peggedlimit = args.peggedlimit
    peglimittype = args.peglimittype

    if not stop and not limit:
        print('stop or limit must be specified')
        return

    if peggedstop:
        if not pegstoptype:
            print('pegtype must be specified')
            return
        if pegstoptype != 'O' and pegstoptype != 'M':
            print('pegstoptype is invalid. "O" or "M" only.')
            return
        peggedstop = peggedstop.lower()
        if peggedstop != 'y':
            peggedstop = None

    if pegstoptype:
        pegstoptype = pegstoptype.upper()

    if peggedlimit:
        if not peglimittype:
            print('peglimittype must be specified')
            return
        if peglimittype != 'O' and peglimittype != 'M':
            print('peglimittype is invalid. "O" or "M" only.')
            return
        peggedlimit = peggedlimit.lower()
        if peggedlimit != 'y':
            peggedlimit = None

    if peglimittype:
        peglimittype = peglimittype.upper()

    with ForexConnect() as fx:
        fx.login(user_id, password, str_url, connection, session_id,
                 pin, common_samples.session_status_changed)

        table_manager = fx.table_manager

        check_trades(fx, table_manager)

        try:
            fx.logout()
        except Exception as e:
            common_samples.print_exception(e)


if __name__ == "__main__":
    main()
    input("Done! Press enter key to exit\n")
