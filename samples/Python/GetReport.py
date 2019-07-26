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
import datetime
import os
import re
from urllib.parse import urlsplit
from urllib.request import urlopen

from forexconnect import ForexConnect

import common_samples

def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)
    common_samples.add_report_date_arguments(parser)
    args = parser.parse_args()

    return args


def month_delta(date, delta):
    m, y = (date.month + delta) % 12, date.year + (date.month + delta - 1) // 12
    if not m:
        m = 12
    d = min(date.day, [31, 29 if y % 4 == 0 and not y % 400 == 0 else 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31][m-1])
    return date.replace(day=d, month=m, year=y)


def get_reports(fc, dt_from, dt_to):
    accounts_response_reader = fc.get_table_reader(ForexConnect.ACCOUNTS)
    if dt_to is None:
        dt_to = datetime.datetime.today()
    if dt_from is None:
        dt_from = month_delta(datetime.datetime.today(), -1)
    
    for account in accounts_response_reader:
        print("")
        print("Obtaining report URL...")
        url = fc.session.get_report_url(account.account_id, dt_from, dt_to, "html", None)
        
        print("account_id={0:s}; Balance={1:.5f}".format(account.account_id, account.balance))
        print("Report URL={0:s}\n".format(url))
        file_name = os.path.join(os.getcwd(), account.account_id)
        file_name += ".html"
        
        print("Connecting...")
        response = urlopen(url)
        print("OK")
        print("Downloading report...")

        abs_path = '{0.scheme}://{0.netloc}/'.format(urlsplit(url))
        with open(file_name, 'w') as file:
            report = response.read().decode('utf-8')
            report = re.sub(r'((?:src|href)=")[/\\](.*?")', r'\1' + abs_path + r'\2', report)
            file.write(report)
            print("Report is saved to {0:s}\n".format(file_name))


def main():
    args = parse_args()
    str_user_id = args.l
    str_password = args.p
    str_url = args.u
    str_connection = args.c
    str_session_i_d = args.session
    str_pin = args.pin
    date_from = args.datefrom
    date_to = args.dateto

    with ForexConnect() as fx:
        try:
            fx.login(str_user_id, str_password, str_url, str_connection,
                     str_session_i_d, str_pin, common_samples.session_status_changed)
            
            get_reports(fx, date_from, date_to)

        except Exception as e:
            common_samples.print_exception(e)
        try:
            fx.logout()
        except Exception as e:
            common_samples.print_exception(e)


if __name__ == "__main__":
    main()
    print("")
    input("Done! Press enter key to exit\n")
