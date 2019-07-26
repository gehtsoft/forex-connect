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

from forexconnect import ForexConnect, EachRowListener

import common_samples


def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)
    parser.add_argument('-table',
                        metavar="TABLE",
                        default="trades",
                        help='The print table. Possible values are: orders - orders table,\
                        trades - trades table. Default value is trades. Optional parameter.')

    args = parser.parse_args()

    return args


def get_account(table_manager):
    accounts_table = table_manager.get_table(ForexConnect.ACCOUNTS)
    for account_row in accounts_table:
        print("AccountID: {0:s}, Balance: {1:.5f}".format(account_row.account_id, account_row.balance))
    return accounts_table.get_row(0)


def print_order_row(order_row, account_id):
    if order_row.table_type == ForexConnect.ORDERS:
        if not account_id or account_id == order_row.account_id:
            string = ""
            for column in order_row.columns:
                string += column.id + "=" + str(order_row[column.id]) + "; "
            print(string)


def print_orders(table_manager, account_id):
    orders_table = table_manager.get_table(ForexConnect.ORDERS)
    if len(orders_table) == 0:
        print("Table is empty!")
    else:
        for order_row in orders_table:
            print_order_row(order_row, account_id)


def print_trade_row(trade_row, account_id):
    if trade_row.table_type == ForexConnect.TRADES:
        if not account_id or account_id == trade_row.account_id:
            string = ""
            for column in trade_row.columns:
                string += column.id + "=" + str(trade_row[column.id]) + "; "
            print(string)


def print_trades(table_manager, account_id):
    trades_table = table_manager.get_table(ForexConnect.TRADES)
    if len(trades_table) == 0:
        print("Table is empty!")
    else:
        for trade_row in trades_table:
            print_trade_row(trade_row, account_id)


def main():
    args = parse_args()
    str_user_id = args.l
    str_password = args.p
    str_url = args.u
    str_connection = args.c
    str_session_i_d = args.session
    str_pin = args.pin
    str_table = args.table

    if str_table != 'orders' and  str_table != 'trades':
        str_table = 'trades'

    with ForexConnect() as fx:

        fx.login(str_user_id, str_password, str_url,
                 str_connection, str_session_i_d, str_pin,
                 common_samples.session_status_changed)

        table_manager = fx.table_manager
        account = get_account(table_manager)

        if not account:
            raise Exception("No valid accounts")

        if str_table == "orders":
            print_orders(table_manager, account.account_id)
        else:
            print_trades(table_manager, account.account_id)

        try:
            fx.logout()
        except Exception as e:
            common_samples.print_exception(e)


if __name__ == "__main__":
    main()
    input("Done! Press enter key to exit\n")
