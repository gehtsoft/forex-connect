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
from time import sleep
from threading import Event

from forexconnect import fxcorepy, ForexConnect, Common

import common_samples


def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)
    common_samples.add_instrument_timeframe_arguments(parser, timeframe=False)
    common_samples.add_direction_rate_lots_arguments(parser)
    common_samples.add_account_arguments(parser)
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


class OrdersMonitor:
    def __init__(self):
        self.__order_id = None
        self.__orders = {}
        self.__event = Event()

    def on_added_order(self, _, __, order_row):
        order_id = order_row.order_id
        self.__orders[order_id] = order_row
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
        if order_id in self.__orders:
            return self.__orders[order_id]
        else:
            return None

    def reset(self):
        self.__order_id = None
        self.__orders.clear()
        self.__event.clear()


def main():
    args = parse_args()
    str_user_id = args.l
    str_password = args.p
    str_url = args.u
    str_connection = args.c
    str_session_id = args.session
    str_pin = args.pin
    str_instrument = args.i
    str_buy_sell = args.d
    str_rate = args.r
    str_lots = args.lots
    str_account = args.account
    stop = args.stop
    peggedstop = args.peggedstop
    pegstoptype = args.pegstoptype
    limit = args.limit
    peggedlimit = args.peggedlimit
    peglimittype = args.peglimittype
    event = Event()

    if peggedstop:
        if not pegstoptype:
            print('pegstoptype must be specified')
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
        fx.login(str_user_id, str_password, str_url, str_connection, str_session_id,
                 str_pin, common_samples.session_status_changed)

        try:
            account = Common.get_account(fx, str_account)
            if not account:
                raise Exception(
                    "The account '{0}' is not valid".format(str_account))

            else:
                str_account = account.account_id
                print("AccountID='{0}'".format(str_account))

            offer = Common.get_offer(fx, str_instrument)

            if offer is None:
                raise Exception(
                    "The instrument '{0}' is not valid".format(str_instrument))

            login_rules = fx.login_rules

            trading_settings_provider = login_rules.trading_settings_provider

            base_unit_size = trading_settings_provider.get_base_unit_size(
                str_instrument, account)

            amount = base_unit_size * str_lots

            entry = fxcorepy.Constants.Orders.ENTRY

            if str_buy_sell == 'B':
                stopv = -stop
                limitv = limit
            else:
                stopv = stop
                limitv = -limit

            if peggedstop:
                if peggedlimit:
                    request = fx.create_order_request(
                        order_type=entry,
                        OFFER_ID=offer.offer_id,
                        ACCOUNT_ID=str_account,
                        BUY_SELL=str_buy_sell,
                        PEG_TYPE_STOP=pegstoptype,
                        PEG_OFFSET_STOP=stopv,
                        PEG_TYPE_LIMIT=peglimittype,
                        PEG_OFFSET_LIMIT=limitv,
                        AMOUNT=amount,
                        RATE=str_rate,
                    )
                else:
                    request = fx.create_order_request(
                        order_type=entry,
                        OFFER_ID=offer.offer_id,
                        ACCOUNT_ID=str_account,
                        BUY_SELL=str_buy_sell,
                        PEG_TYPE_STOP=pegstoptype,
                        PEG_OFFSET_STOP=stopv,
                        RATE_LIMIT=limit,
                        AMOUNT=amount,
                        RATE=str_rate,
                    )
            else:
                if peggedlimit:
                    request = fx.create_order_request(
                        order_type=entry,
                        OFFER_ID=offer.offer_id,
                        ACCOUNT_ID=str_account,
                        BUY_SELL=str_buy_sell,
                        RATE_STOP=stop,
                        PEG_TYPE_LIMIT=peglimittype,
                        PEG_OFFSET_LIMIT=limitv,
                        AMOUNT=amount,
                        RATE=str_rate,
                    )
                else:
                    request = fx.create_order_request(
                        order_type=entry,
                        OFFER_ID=offer.offer_id,
                        ACCOUNT_ID=str_account,
                        BUY_SELL=str_buy_sell,
                        AMOUNT=amount,
                        RATE_STOP=stop,
                        RATE_LIMIT=limit,
                        RATE=str_rate,
                    )

            orders_monitor = OrdersMonitor()

            orders_table = fx.get_table(ForexConnect.ORDERS)
            orders_listener = Common.subscribe_table_updates(orders_table,
                                                             on_add_callback=orders_monitor.on_added_order)

            try:
                resp = fx.send_request(request)
                order_id = resp.order_id

            except Exception as e:
                common_samples.print_exception(e)
                orders_listener.unsubscribe()

            else:
                # Waiting for an order to appear or timeout (default 30)
                order_row = orders_monitor.wait(30, order_id)
                if order_row is None:
                    print("Response waiting timeout expired.\n")
                else:
                    print("The order has been added. OrderID={0:s}, "
                          "Type={1:s}, BuySell={2:s}, Rate={3:.5f}, TimeInForce={4:s}".format(
                        order_row.order_id, order_row.type, order_row.buy_sell, order_row.rate,
                        order_row.time_in_force))
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
