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

from typing import List
from enum import Enum

from forexconnect import fxcorepy


class OrderMonitor:
    class ExecutionResult(Enum):
        EXECUTING = 1
        EXECUTED = 2
        PARTIAL_REJECTED = 3
        FULLY_REJECTED = 4
        CANCELED = 5

    class OrderState(Enum):
        ORDER_EXECUTING = 1
        ORDER_EXECUTED = 2
        ORDER_CANCELED = 3
        ORDER_REJECTED = 4

    __market_condition = "5"

    def __init__(self, order: fxcorepy.O2GOrderRow) -> None:
        self.__order = order
        self.__trades = []
        self.__closed_trades = []
        self.__state = OrderMonitor.OrderState.ORDER_EXECUTING
        self.__result = OrderMonitor.ExecutionResult.EXECUTING
        self.__total_amount = 0
        self.__reject_amount = 0
        self.__reject_message = ""

    @staticmethod
    def is_opening_order(order: fxcorepy.O2GOrderRow) -> bool:
        return order.type.startswith("O")

    @staticmethod
    def is_closing_order(order: fxcorepy.O2GOrderRow) -> bool:
        return order.type.startswith("C")

    @property
    def order_row(self) -> fxcorepy.O2GOrderRow:
        return self.__order

    @property
    def trade_rows(self) -> List[fxcorepy.O2GTradeRow]:
        return self.__trades

    @property
    def closed_trade_rows(self) -> List[fxcorepy.O2GClosedTradeRow]:
        return self.__closed_trades

    @property
    def reject_amount(self) -> int:
        return self.__reject_amount

    @property
    def reject_message(self) -> str:
        return self.__reject_message

    @property
    def result(self) -> ExecutionResult:
        return self.__result

    @property
    def is_order_completed(self) -> bool:
        print(self.__result)
        return self.__result != OrderMonitor.ExecutionResult.EXECUTING

    @property
    def is_all_trades_received(self) -> bool:
        if self.__state == OrderMonitor.OrderState.ORDER_EXECUTING:
            return False
        i_current_total_amount = 0
        for trade in self.__trades:
            i_current_total_amount += trade.amount

        for trade in self.__closed_trades:
            i_current_total_amount += trade.amount

        return i_current_total_amount == self.__total_amount

    def on_trade_added(self, trade: fxcorepy.O2GTradeRow) -> None:
        trade_order_id = trade.open_order_id
        order_id = self.__order.order_id
        if trade_order_id == order_id:
            self.__trades.append(trade)
            if self.__state == OrderMonitor.OrderState.ORDER_EXECUTED or \
                    self.__state == OrderMonitor.OrderState.ORDER_REJECTED or \
                    self.__state == OrderMonitor.OrderState.ORDER_CANCELED:
                if self.is_all_trades_received:
                    self.set_result(True)

    def on_closed_trade_added(self, closed_trade: fxcorepy.O2GClosedTradeRow) -> None:
        order_id = self.__order.order_id
        closed_trade_order_id = closed_trade.close_order_id
        print(closed_trade_order_id, order_id)
        if order_id == closed_trade_order_id:
            self.__closed_trades.append(closed_trade)
            if self.__state == OrderMonitor.OrderState.ORDER_EXECUTED or \
                    self.__state == OrderMonitor.OrderState.ORDER_REJECTED or \
                    self.__state == OrderMonitor.OrderState.ORDER_CANCELED:
                if self.is_all_trades_received:
                    self.set_result(True)

    def on_order_deleted(self, order: fxcorepy.O2GOrderRow) -> None:
        deleted_order_id = order.order_id
        order_id = self.__order.order_id
        if deleted_order_id == order_id:
            # Store Reject amount
            if order.status.startswith("R"):
                self.__state = OrderMonitor.OrderState.ORDER_REJECTED
                self.__reject_amount = order.amount
                self.__total_amount = order.origin_amount - self.__reject_amount
                if self.__reject_message != "" and self.is_all_trades_received:
                    self.set_result(True)
            elif order.status.startswith("C"):
                self.__state = OrderMonitor.OrderState.ORDER_CANCELED
                self.__reject_amount = order.amount
                self.__total_amount = order.origin_amount - self.__reject_amount
                if self.is_all_trades_received:
                    self.set_result(False)
            else:
                self.__reject_amount = 0
                self.__total_amount = order.origin_amount
                self.__state = OrderMonitor.OrderState.ORDER_EXECUTED
                if self.is_all_trades_received:
                    self.set_result(True)

    def on_message_added(self, message: fxcorepy.O2GMessageRow) -> None:
        if self.__state == OrderMonitor.OrderState.ORDER_REJECTED or \
                self.__state == OrderMonitor.OrderState.ORDER_EXECUTING:
            is_reject_message = self.check_and_store_message(message)
            if self.__state == OrderMonitor.OrderState.ORDER_REJECTED and is_reject_message:
                self.set_result(True)

    def set_result(self, success: bool) -> None:
        if success:
            if self.__reject_amount == 0:
                self.__result = OrderMonitor.ExecutionResult.EXECUTED
            else:
                self.__result = OrderMonitor.ExecutionResult.FULLY_REJECTED \
                    if (len(self.__trades) == 0 and len(self.__closed_trades) == 0) \
                    else OrderMonitor.ExecutionResult.PARTIAL_REJECTED

        else:
            self.__result = OrderMonitor.ExecutionResult.CANCELED

    def check_and_store_message(self, message: fxcorepy.O2GMessageRow) -> bool:
        feature = message.feature
        if feature == self.__market_condition:
            text = message.text
            if self.__order.order_id in text:
                self.__reject_message = message.text
                return True
        return False
