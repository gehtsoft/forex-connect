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
import time
from forexconnect import fxcorepy, ForexConnect, Common
import common_samples
from datetime import datetime


def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)
    common_samples.add_instrument_timeframe_arguments(parser, timeframe=False)
    common_samples.add_direction_rate_lots_arguments(parser, rate=False)
    common_samples.add_account_arguments(parser)

    def check_positive(value):
        i_value = int(value)
        if i_value < 0:
            raise argparse.ArgumentTypeError(
                "%s is an invalid positive int value" % value)
        return i_value
    parser.add_argument('-openhour', metavar="OpenHour",
                        default=None, type=check_positive,
                        help='Open hour')
    parser.add_argument('-openminute', metavar="OpenMinute",
                        default=0, type=check_positive,
                        help='Open minute')

    args = parser.parse_args()
    return args


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
    openhour = args.openhour
    openminute = args.openminute
    str_account = args.account
    event = threading.Event()

    if not openhour:
        print("Open hour must be specified")
        return

    openhm = 60*openhour+openminute

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
        base_unit_size = trading_settings_provider.get_base_unit_size(
                                                   instrument, account)
        amount = base_unit_size * lots
        market_open = fxcorepy.Constants.Orders.TRUE_MARKET_OPEN
        order_id = None

        curtime = datetime.now()
        hm = curtime.hour*60+curtime.minute

        print("Waiting...")

        while hm != openhm:
            curtime = datetime.now()
            hm = curtime.hour*60+curtime.minute
            time.sleep(1)

        request = fx.create_order_request(
            order_type=market_open,
            ACCOUNT_ID=str_account,
            BUY_SELL=buy_sell,
            AMOUNT=amount,
            SYMBOL=offer.instrument
        )

        if request is None:
            raise Exception("Cannot create request")

        def on_added_trade(_, __, trade_row):
            if trade_row.open_order_id == order_id:
                print("For the order: OrderID = {0} the following positions\
                        have been opened:".format(order_id))
                print("Trade ID: {0:s}; Amount: {1:d}; Rate: {2:.5f}"
                    .format(trade_row.trade_id, trade_row.amount,
                    trade_row.open_rate))
                event.set()

        def on_added_order(_, __, order_row):
            nonlocal order_id
            if order_row.order_id == order_id:
                print("The order has been added. Order ID: {0:s},\
                     Rate: {1:.5f}, Time In Force: {2:s}".format(
                    order_row.order_id, order_row.rate,
                    order_row.time_in_force))

        def on_deleted_order(_, __, row_data):
            nonlocal order_id
            if row_data.order_id == order_id:
                print("The order has been deleted. Order ID: {0}"
                    .format(row_data.order_id))

        trades_table = fx.get_table(ForexConnect.TRADES)
        orders_table = fx.get_table(ForexConnect.ORDERS)

        trades_listener = Common.subscribe_table_updates(trades_table,
                                       on_add_callback=on_added_trade)
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
                time.sleep(1)
            trades_listener.unsubscribe()
            orders_listener.unsubscribe()

        try:
            fx.logout()
        except Exception as e:
            common_samples.print_exception(e)


if __name__ == '__main__':
    main()
    input("Done! Press enter key to exit\n")
