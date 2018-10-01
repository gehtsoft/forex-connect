from typing import List

from forexconnect import fxcorepy

from common_samples.OrderMonitor import OrderMonitor


class BatchOrderMonitor:
    __request_ids = None
    __monitors = []

    def __init__(self) -> None:
        pass

    @property
    def monitors(self) -> List[OrderMonitor]:
        return self.__monitors

    @property
    def is_batch_executed(self) -> bool:
        all_completed = True
        for monitor in self.__monitors:
            if monitor.is_order_completed:
                self.remove_request_id(monitor.order.request_id)
            else:
                all_completed = False
        return len(self.__request_ids) == 0 and all_completed

    def set_request_ids(self, request_ids: List[str]) -> None:
        self.__request_ids = request_ids

    def on_request_completed(self, request_id: str, response: fxcorepy.O2GResponse) -> None:
        pass
    
    def remove_request_id(self, request_id: str) -> None:
        if self.is_own_request(request_id):
            self.__request_ids.remove(request_id)

    def on_request_failed(self, request_id: str) -> None:
        self.remove_request_id(request_id)

    def on_trade_added(self, trade_row: fxcorepy.O2GTradeRow) -> None:
        for monitor in self.__monitors:
            monitor.on_trade_added(trade_row)

    def on_order_added(self, order: fxcorepy.O2GOrderRow) -> None:
        request_id = order.request_id
        print("Order Added " + order.order_id)
        if self.is_own_request(request_id):
            if OrderMonitor.is_closing_order(order) or OrderMonitor.is_opening_order(order):
                self._add_to_monitoring(order)

    def on_order_deleted(self, order: fxcorepy.O2GOrderRow) -> None:
        for monitor in self.__monitors:
            monitor.on_order_deleted(order)

    def on_message_added(self, message: fxcorepy.O2GMessageRow) -> None:
        for monitor in self.__monitors:
            monitor.on_message_added(message)

    def on_closed_trade_added(self, close_trade_row: fxcorepy.O2GClosedTradeRow) -> None:
        for monitor in self.__monitors:
            monitor.on_closed_trade_added(close_trade_row)

    def is_own_request(self, request_id: str) -> bool:
        return request_id in self.__request_ids

    def _add_to_monitoring(self, order: fxcorepy.O2GOrderRow) -> None:
        self.__monitors.append(OrderMonitor(order))
