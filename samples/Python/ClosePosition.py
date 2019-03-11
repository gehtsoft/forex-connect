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


def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)
    common_samples.add_instrument_timeframe_arguments(parser, timeframe=False)
    common_samples.add_account_arguments(parser)
    args = parser.parse_args()

    return args


class ClosedTradesMonitor:
    def __init__(self):
        self.__close_order_id = None
        self.__closed_trades = {}
        self.__event = threading.Event()

    def on_added_closed_trade(self, _, __, closed_trade_row):
        close_order_id = closed_trade_row.close_order_id
        self.__closed_trades[close_order_id] = closed_trade_row
        if self.__close_order_id == close_order_id:
            self.__event.set()

    def wait(self, time, close_order_id):
        self.__close_order_id = close_order_id

        closed_trade_row = self.find_closed_trade(close_order_id)
        if closed_trade_row is not None:
            return closed_trade_row

        self.__event.wait(time)

        return self.find_closed_trade(close_order_id)

    def find_closed_trade(self, close_order_id):
        if close_order_id in self.__closed_trades:
            return self.__closed_trades[close_order_id]
        return None

    def reset(self):
        self.__close_order_id = None
        self.__closed_trades.clear()
        self.__event.clear()


class OrdersMonitor:
    def __init__(self):
        self.__order_id = None
        self.__added_orders = {}
        self.__deleted_orders = {}
        self.__added_order_event = threading.Event()
        self.__deleted_order_event = threading.Event()

    def on_added_order(self, _, __, order_row):
        order_id = order_row.order_id
        self.__added_orders[order_id] = order_row
        if self.__order_id == order_id:
            self.__added_order_event.set()

    def on_deleted_order(self, _, __, order_row):
        order_id = order_row.order_id
        self.__deleted_orders[order_id] = order_row
        if self.__order_id == order_id:
            self.__deleted_order_event.set()

    def wait(self, time, order_id):
        self.__order_id = order_id

        is_order_added = True
        is_order_deleted = True

        # looking for an added order
        if order_id not in self.__added_orders:
            is_order_added = self.__added_order_event.wait(time)

        if is_order_added:
            order_row = self.__added_orders[order_id]
            print("The order has been added. Order ID: {0:s}, Rate: {1:.5f}, Time In Force: {2:s}".format(
                order_row.order_id, order_row.rate, order_row.time_in_force))

        # looking for a deleted order
        if order_id not in self.__deleted_orders:
            is_order_deleted = self.__deleted_order_event.wait(time)

        if is_order_deleted:
            order_row = self.__deleted_orders[order_id]
            print("The order has been deleted. Order ID: {0}".format(order_row.order_id))

        return is_order_added and is_order_deleted

    def reset(self):
        self.__order_id = None
        self.__added_orders.clear()
        self.__deleted_orders.clear()
        self.__added_order_event.clear()
        self.__deleted_order_event.clear()


def main():
    args = parse_args()
    user_id = args.l
    password = args.p
    str_url = args.u
    connection = args.c
    session_id = args.session
    pin = args.pin
    instrument = args.i
    str_account = args.account

    with ForexConnect() as fx:
        fx.login(user_id, password, str_url, connection, session_id,
                 pin, common_samples.session_status_changed)

        account = Common.get_account(fx, str_account)

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

        trade = Common.get_trade(fx, str_account, offer.offer_id)

        if not trade:
            raise Exception("There are no opened positions for instrument '{0}'".format(instrument))

        amount = trade.amount

        buy = fxcorepy.Constants.BUY
        sell = fxcorepy.Constants.SELL

        buy_sell = sell if trade.buy_sell == buy else buy

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

        orders_monitor = OrdersMonitor()
        closed_trades_monitor = ClosedTradesMonitor()

        closed_trades_table = fx.get_table(ForexConnect.CLOSED_TRADES)
        orders_table = fx.get_table(ForexConnect.ORDERS)

        trades_listener = Common.subscribe_table_updates(closed_trades_table,
                                                         on_add_callback=closed_trades_monitor.on_added_closed_trade)
        orders_listener = Common.subscribe_table_updates(orders_table, on_add_callback=orders_monitor.on_added_order,
                                                         on_delete_callback=orders_monitor.on_deleted_order)

        try:
            resp = fx.send_request(request)
            order_id = resp.order_id

        except Exception as e:
            common_samples.print_exception(e)
            trades_listener.unsubscribe()
            orders_listener.unsubscribe()

        else:
            # Waiting for an order to appear/delete or timeout (default 30)
            is_success = orders_monitor.wait(30, order_id)

            closed_trade_row = None
            if is_success:
                # Waiting for a closed trade to appear or timeout (default 30)
                closed_trade_row = closed_trades_monitor.wait(30, order_id)

            if closed_trade_row is None:
                print("Response waiting timeout expired.\n")
            else:
                print("For the order: OrderID = {0} the following positions have been closed: ".format(order_id))
                print("Closed Trade ID: {0:s}; Amount: {1:d}; Closed Rate: {2:.5f}".format(closed_trade_row.trade_id,
                                                                                           closed_trade_row.amount,
                                                                                           closed_trade_row.close_rate))
                sleep(1)
            trades_listener.unsubscribe()
            orders_listener.unsubscribe()

        try:
            fx.logout()
        except Exception as e:
            common_samples.print_exception(e)


if __name__ == "__main__":
    main()
    input("Done! Press enter key to exit\n")
