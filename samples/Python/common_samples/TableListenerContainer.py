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

import traceback

from forexconnect import ForexConnect, Common

from common_samples import OrderMonitor


class TableListenerContainer:
    __response_listener = None
    __request_id = ""
    __order_monitor = None

    def __init__(self, response_listener, fx):
        self.__response_listener = response_listener
        self._fx = fx
        self._listeners = []

    def set_request_id(self, request_id):
        self.__request_id = request_id

    def _on_added_orders(self, listener, row_id, order_row):
        del listener, row_id
        if self.__request_id == order_row.request_id:
            if OrderMonitor.is_closing_order(
                    order_row) or OrderMonitor.is_opening_order(
                        order_row) and self.__order_monitor is None:
                print(
                    "The order has been added. Order ID: {0:s}, Rate: {1:.5f}, Time In Force: {2:s}".format(
                        order_row.order_id, order_row.rate,
                        order_row.time_in_force))
                self.__order_monitor = OrderMonitor(order_row)

    def _on_added_trades(self, listener, row_id, trade_row):
        del listener, row_id
        if self.__order_monitor is not None:
            self.__order_monitor.on_trade_added(trade_row)
            if self.__order_monitor.is_order_completed:
                self._print_result()
                self.__response_listener.stop_waiting()

    def _on_added_closed_trades(self, listener, row_id, closed_trade_row):
        del listener, row_id
        if self.__order_monitor is not None:
            self.__order_monitor.on_closed_trade_added(closed_trade_row)
            if self.__order_monitor.is_order_completed:
                self._print_result()
                self.__response_listener.stop_waiting()

    def _on_added_messages(self, listener, row_id, message_row):
        del listener, row_id
        if self.__order_monitor is not None:
            self.__order_monitor.on_message_added(message_row)
            if self.__order_monitor.is_order_completed:
                self._print_result()
                self.__response_listener.stop_waiting()

    def _on_deleted_orders(self, listener, row_id, row_data):
        del listener, row_id
        order_row = row_data
        if self.__request_id == order_row.request_id:
            if self.__order_monitor is not None:
                print("The order has been deleted. Order ID: {0}".format(
                    order_row.order_id))
                self.__order_monitor.on_order_deleted(order_row)
                if self.__order_monitor.is_order_completed:
                    self._print_result()
                    self.__response_listener.stop_waiting()

    def _print_result_canceled(self, order_id, trades, closed_trades):
        if len(trades) > 0:
            self._print_trades(trades, order_id)
            self._print_closed_trades(closed_trades, order_id)
            print("A part of the order has been canceled. Amount = {0}".format(
                self.__order_monitor.reject_amount))
        else:
            print("The order: OrderID = {0}  has been canceled".format(
                order_id))
            print("The cancel amount = {0}".format(
                self.__order_monitor.reject_amount))

    def _print_result_fully_rejected(self, order_id, trades, closed_trades):
        del trades, closed_trades
        print("The order has been rejected. OrderID = {0}".format(
            order_id))
        print("The rejected amount = {0}".format(
            self.__order_monitor.reject_amount))
        print("Rejection cause: {0}".format(
            self.__order_monitor.reject_message))

    def _print_result_partial_rejected(self, order_id, trades, closed_trades):
        self._print_trades(trades, order_id)
        self._print_closed_trades(closed_trades, order_id)
        print("A part of the order has been rejected. Amount = {0}".format(
            self.__order_monitor.reject_amount))
        print("Rejection cause: {0} ".format(
            self.__order_monitor.reject_message))

    def _print_result_executed(self, order_id, trades, closed_trades):
        self._print_trades(trades, order_id)
        self._print_closed_trades(closed_trades, order_id)

    def _print_result(self):
        if self.__order_monitor is not None:
            result = self.__order_monitor.result
            order = self.__order_monitor.order_row
            order_id = order.order_id
            trades = self.__order_monitor.trade_rows
            closed_trades = self.__order_monitor.closed_trade_rows

            print_result_func = {
                OrderMonitor.ExecutionResult.CANCELED: self._print_result_canceled,
                OrderMonitor.ExecutionResult.FULLY_REJECTED: self._print_result_fully_rejected,
                OrderMonitor.ExecutionResult.PARTIAL_REJECTED: self._print_result_partial_rejected,
                OrderMonitor.ExecutionResult.EXECUTED: self._print_result_executed
            }
            try:
                print_result_func[result](order_id, trades, closed_trades)
            except KeyError:
                pass
            except Exception as e:
                print("Exception: {0}\n".format(e))
                print(traceback.format_exc())

    @staticmethod
    def _print_trades(trades, order_id):
        if len(trades) == 0:
            return
        print(
            "For the order: OrderID = {0} the following positions have been opened:".format(
                order_id))

        for trade in trades:
            trade_id = trade.trade_id
            amount = trade.amount
            rate = trade.open_rate
            print(
                "Trade ID: {0:s}; Amount: {1:d}; Rate: {2:.5f}".format(trade_id,
                                                                       amount,
                                                                       rate))

    @staticmethod
    def _print_closed_trades(closed_trades, order_id):
        if len(closed_trades) == 0:
            return
        print(
            "For the order: OrderID = {0} the following positions have been closed: ".format(
                order_id))

        for closed_trade in closed_trades:
            trade_id = closed_trade.trade_id
            amount = closed_trade.amount
            rate = closed_trade.close_rate
            print(
                "Closed Trade ID: {0:s}; Amount: {1:d}; Closed Rate: {2:.5f}".format(
                    trade_id, amount, rate))

    def subscribe_events(self):
        orders_table = self._fx.get_table(ForexConnect.ORDERS)
        orders_table_listener = Common.subscribe_table_updates(orders_table,
                                                               on_add_callback=self._on_added_orders,
                                                               on_delete_callback=self._on_deleted_orders)
        self._listeners.append(orders_table_listener)

        trades_table = self._fx.get_table(ForexConnect.TRADES)
        trades_table_listener = Common.subscribe_table_updates(trades_table,
                                                               on_add_callback=self._on_added_trades)
        self._listeners.append(trades_table_listener)

        messages_table = self._fx.get_table(ForexConnect.MESSAGES)
        messages_table_listener = Common.subscribe_table_updates(messages_table,
                                                                 on_add_callback=self._on_added_messages)
        self._listeners.append(messages_table_listener)

        closed_trades_table = self._fx.get_table(ForexConnect.CLOSED_TRADES)
        closed_trades_table_listener = Common.subscribe_table_updates(closed_trades_table,
                                                                      on_add_callback=self._on_added_closed_trades)
        self._listeners.append(closed_trades_table_listener)

    def unsubscribe_events(self):
        for listener in self._listeners:
            listener.unsubscribe()
        self._listeners = []
