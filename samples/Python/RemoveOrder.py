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
    # specific arguments
    parser.add_argument('-orderid', metavar="OrderID", required=True,
                        help='OrderID for remove. For example, 193164992')

    args = parser.parse_args()

    return args


class OrdersMonitor:
    def __init__(self):
        self.__order_id = None
        self.__deleted_orders = {}
        self.__event = threading.Event()

    def on_delete_order(self, _, __, order_row):
        order_id = order_row.order_id
        self.__deleted_orders[order_id] = order_row
        if self.__order_id == order_id:
            self.__event.set()

    def wait(self, time, order_id):
        self.__order_id = order_id

        order_row = self.find_order(order_id)
        if order_row is not None:
            return order_row

        self.__event.wait(time)

        return self.find_order(order_id)

    def find_order(self, order_id):
        if order_id in self.__deleted_orders:
            return self.__deleted_orders[order_id]
        else:
            return None

    def reset(self):
        self.__order_id = None
        self.__deleted_orders.clear()
        self.__event.clear()


def main():
    args = parse_args()
    str_user_id = args.l
    str_password = args.p
    str_url = args.u
    str_connection = args.c
    str_session_id = args.session
    str_pin = args.pin
    str_old = args.orderid

    with ForexConnect() as fx:
        try:
            fx.login(str_user_id, str_password, str_url, str_connection, str_session_id,
                     str_pin, common_samples.session_status_changed)

            order_id = str_old
            orders_table = fx.get_table(ForexConnect.ORDERS)
            orders = orders_table.get_rows_by_column_value("order_id", order_id)
            order = None
            
            for order_row in orders:
                order = order_row
                break

            if order is None:
                raise Exception("Order {0} not found".format(order_id))

            request = fx.create_request({

                fxcorepy.O2GRequestParamsEnum.COMMAND: fxcorepy.Constants.Commands.DELETE_ORDER,
                fxcorepy.O2GRequestParamsEnum.ACCOUNT_ID: order.account_id,
                fxcorepy.O2GRequestParamsEnum.ORDER_ID: str_old
            })

            orders_monitor = OrdersMonitor()

            orders_table = fx.get_table(ForexConnect.ORDERS)
            orders_listener = Common.subscribe_table_updates(orders_table, on_delete_callback=orders_monitor.on_delete_order)

            try:
                fx.send_request(request)

            except Exception as e:
                common_samples.print_exception(e)
                orders_listener.unsubscribe()
            else:
                # Waiting for an order to delete or timeout (default 30)
                is_deleted = orders_monitor.wait(30, order_id)
                if not is_deleted:
                    print("Response waiting timeout expired.\n")
                else:
                    print("The order has been deleted. Order ID: {0:s}".format(order_row.order_id))
                    sleep(1)
                orders_listener.unsubscribe()

        except Exception as e:
            common_samples.print_exception(e)
        try:
            fx.logout()
        except Exception as e:
            common_samples.print_exception(e)


if __name__ == "__main__":
    main()
    input("Done! Press enter key to exit\n")
