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
import time

from forexconnect import fxcorepy, ForexConnect, Common

import common_samples


def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)
    common_samples.add_instrument_timeframe_arguments(parser, timeframe=False)
    args = parser.parse_args()
    return args


class OffersTableListener:
    def __init__(self, instrument):
        self.__instrument = instrument

    def on_added(self, table_listener, row_id, row):
        pass

    def on_changed(self, table_listener, row_id, row):
        if row.table_type == ForexConnect.OFFERS:
            self.print_offer(row, self.__instrument)

    def on_deleted(self, table_listener, row_id, row):
        pass

    def on_status_changed(self, table_listener, status):
        pass

    def print_offers(self, offers_table):
        for offer_row in offers_table:
            self.print_offer(offer_row, None)

    def print_offer(self, offer_row, selected_instrument):
        offer_id = offer_row.offer_id
        instrument = offer_row.instrument
        bid = offer_row.bid
        ask = offer_row.ask

        if selected_instrument is None or selected_instrument == instrument:
            print("{offer_id}, {instrument}, Bid={bid:.6f}, Ask={ask:.6f}".format(
                offer_id=offer_id,
                instrument=instrument,
                bid=bid,
                ask=ask
            ))


def main():
    args = parse_args()
    str_user_i_d = args.l
    str_password = args.p
    str_url = args.u
    str_connection = args.c
    str_session_id = args.session
    str_pin = args.pin
    str_instrument = args.i

    with ForexConnect() as fx:
        try:
            fx.login(str_user_i_d, str_password, str_url,
                     str_connection, str_session_id, str_pin,
                     common_samples.session_status_changed)

            offers = fx.get_table(ForexConnect.OFFERS)
            offers_listener = OffersTableListener(str_instrument)

            table_listener = Common.subscribe_table_updates(offers,
                                                            on_change_callback=offers_listener.on_changed,
                                                            on_add_callback=offers_listener.on_added,
                                                            on_delete_callback=offers_listener.on_deleted,
                                                            on_status_change_callback=offers_listener.on_changed
                                                            )

            offers_listener.print_offers(offers)

            time.sleep(60)

            table_listener.unsubscribe()

        except Exception as e:
            common_samples.print_exception(e)
        try:
            fx.logout()
        except Exception as e:
            common_samples.print_exception(e)


if __name__ == "__main__":
    main()
    input("Done! Press any key to exit\n")
