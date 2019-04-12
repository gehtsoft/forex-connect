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

from forexconnect import ForexConnect, EachRowListener, ResponseListener

from forexconnect import fxcorepy
from forexconnect import SessionStatusListener
from forexconnect.common import Common
from time import sleep

import common_samples

str_instrument = None
old_status = None


def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)
    parser.add_argument('-i', metavar="INSTRUMENT", required=True,
                        help='An instrument which you want to use in sample. For example, "EUR/USD".')
    parser.add_argument('-status', metavar="STATUS", required=True,
                        help='Status')

    args = parser.parse_args()

    return args


def get_offer(fx, s_instrument):
    table_manager = fx.table_manager
    offers_table = table_manager.get_table(ForexConnect.OFFERS)
    for offer_row in offers_table:
        if offer_row.instrument == s_instrument:
            return offer_row


def on_changed():
    def _on_changed(table_listener, row_id, row):
        global str_instrument
        global old_status
        if row.instrument == str_instrument:
            new_status = row.subscription_status
            if new_status != old_status:
                string = 'instrument='+row.instrument+'; new subscription_status='+new_status
                print(string)
                old_status = new_status
        return

    return _on_changed


def main():
    global str_instrument
    global old_status
    args = parse_args()
    str_user_id = args.l
    str_password = args.p
    str_url = args.u
    str_connection = args.c
    str_session_i_d = args.session
    str_pin = args.pin
    str_instrument = args.i
    status = args.status

    with ForexConnect() as fx:
        try:
            fx.login(str_user_id, str_password, str_url,
                     str_connection, str_session_i_d, str_pin,
                     common_samples.session_status_changed)

            offer = get_offer(fx, str_instrument)

            string = 'instrument='+offer.instrument+'; subscription_status='+offer.subscription_status
            old_status = offer.subscription_status
            print(string)

            if status == old_status:
                raise Exception('New status = current status')

            offers_table = fx.get_table(ForexConnect.OFFERS)

            request = fx.create_request({
                fxcorepy.O2GRequestParamsEnum.COMMAND: fxcorepy.Constants.Commands.SET_SUBSCRIPTION_STATUS,
                fxcorepy.O2GRequestParamsEnum.OFFER_ID: offer.offer_id,
                fxcorepy.O2GRequestParamsEnum.SUBSCRIPTION_STATUS: status
            })

            offers_listener = Common.subscribe_table_updates(offers_table, on_change_callback=on_changed())

            try:
                fx.send_request(request)

            except Exception as e:
                common_samples.print_exception(e)
                offers_listener.unsubscribe()
            else:
                sleep(1)
                offers_listener.unsubscribe()

        except Exception as e:
            common_samples.print_exception(e)

        try:
            fx.logout()
        except Exception as e:
            common_samples.print_exception(e)


if __name__ == "__main__":
    main()
    input("Done! Press enter key to exit\n")
