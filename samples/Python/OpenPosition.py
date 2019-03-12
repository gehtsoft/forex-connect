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
    common_samples.add_direction_rate_lots_arguments(parser, rate=False)
    common_samples.add_account_arguments(parser)

    args = parser.parse_args()
    return args


class TradesMonitor:
    def __init__(self):
        self.__open_order_id = None
        self.__trades = {}
        self.__event = threading.Event()

    def on_added_trade(self, _, __, trade_row):
        open_order_id = trade_row.open_order_id
        self.__trades[open_order_id] = trade_row
        if self.__open_order_id == open_order_id:
            self.__event.set()

    def wait(self, time, open_order_id):
        self.__open_order_id = open_order_id

        trade_row = self.find_trade(open_order_id)
        if trade_row is not None:
            return trade_row

        self.__event.wait(time)

        return self.find_trade(open_order_id)

    def find_trade(self, open_order_id):
        if open_order_id in self.__trades:
            return self.__trades[open_order_id]
        return None

    def reset(self):
        self.__open_order_id = None
        self.__trades.clear()
        self.__event.clear()


class OrdersMonitor:
    def __init__(self):
        self.__order_id = None
        self.__added_orders = {}
        self.__deleted_orders = {}
        self.__changed_orders = {}
        self.__added_order_event = threading.Event()
        self.__changed_orders_event = threading.Event()
        self.__deleted_order_event = threading.Event()

    def on_added_order(self, _, __, order_row):
        order_id = order_row.order_id
        self.__added_orders[order_id] = order_row
        if self.__order_id == order_id:
            self.__added_order_event.set()

    def on_changed_order(self, _, __, order_row):
        order_id = order_row.order_id
        self.__changed_orders[order_id] = order_row
        if self.__order_id == order_id:
            self.__changed_orders_event.set()

    def on_deleted_order(self, _, __, order_row):
        order_id = order_row.order_id
        self.__deleted_orders[order_id] = order_row
        if self.__order_id == order_id:
            self.__deleted_order_event.set()

    def wait(self, time, order_id):
        self.__order_id = order_id

        is_order_added = True
        is_order_changed = True
        is_order_deleted = True

        # looking for an added order
        if order_id not in self.__added_orders:
            is_order_added = self.__added_order_event.wait(time)

        if is_order_added:
            order_row = self.__added_orders[order_id]
            print("The order has been added. Order ID: {0:s}, Rate: {1:.5f}, Time In Force: {2:s}".format(
                order_row.order_id, order_row.rate, order_row.time_in_force))

        # looking for an changed order
        if order_id not in self.__changed_orders:
            is_order_changed = self.__changed_orders_event.wait(time)

        if is_order_changed:
            order_row = self.__changed_orders[order_id]
            print("The order has been changed. Order ID: {0:s}".format(order_row.order_id))

        # looking for a deleted order
        if order_id not in self.__deleted_orders:
            is_order_deleted = self.__deleted_order_event.wait(time)

        if is_order_deleted:
            order_row = self.__deleted_orders[order_id]
            print("The order has been deleted. Order ID: {0}".format(order_row.order_id))

        return is_order_added and is_order_changed and is_order_deleted

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
    buy_sell = args.d
    lots = args.lots
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

        login_rules = fx.login_rules
        trading_settings_provider = login_rules.trading_settings_provider
        base_unit_size = trading_settings_provider.get_base_unit_size(instrument, account)
        amount = base_unit_size * lots
        market_open = fxcorepy.Constants.Orders.TRUE_MARKET_OPEN

        request = fx.create_order_request(
            order_type=market_open,
            ACCOUNT_ID=str_account,
            BUY_SELL=buy_sell,
            AMOUNT=amount,
            SYMBOL=offer.instrument
        )

        if request is None:
            raise Exception("Cannot create request")

        orders_monitor = OrdersMonitor()
        trades_monitor = TradesMonitor()

        trades_table = fx.get_table(ForexConnect.TRADES)
        orders_table = fx.get_table(ForexConnect.ORDERS)

        trades_listener = Common.subscribe_table_updates(trades_table, on_add_callback=trades_monitor.on_added_trade)
        orders_listener = Common.subscribe_table_updates(orders_table, on_add_callback=orders_monitor.on_added_order,
                                                         on_delete_callback=orders_monitor.on_deleted_order,
                                                         on_change_callback=orders_monitor.on_changed_order)

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

            trade_row = None
            if is_success:
                # Waiting for an trade to appear or timeout (default 30)
                trade_row = trades_monitor.wait(30, order_id)

            if trade_row is None:
                print("Response waiting timeout expired.\n")
            else:
                print("For the order: OrderID = {0} the following positions have been opened:".format(order_id))
                print("Trade ID: {0:s}; Amount: {1:d}; Rate: {2:.5f}".format(trade_row.trade_id, trade_row.amount,
                                                                             trade_row.open_rate))
                sleep(1)
            trades_listener.unsubscribe()
            orders_listener.unsubscribe()

        try:
            fx.logout()
        except Exception as e:
            common_samples.print_exception(e)


if __name__ == '__main__':
    main()
    input("Done! Press enter key to exit\n")
