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

import time
import argparse

import pandas as pd
from forexconnect import fxcorepy, ForexConnect, Common, LiveHistoryCreator
from dateutil import parser

import common_samples


def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)
    common_samples.add_instrument_timeframe_arguments(parser)
    common_samples.add_bars_arguments(parser)
    common_samples.add_candle_open_price_mode_argument(parser)
    args = parser.parse_args()
    return args


def parse_candle_open_price_mode(str_mode):
    if str_mode == 'first_tick':
        return fxcorepy.O2GCandleOpenPriceMode.FIRST_TICK
    elif str_mode == 'prev_close':
        return fxcorepy.O2GCandleOpenPriceMode.PREVIOUS_CLOSE
    else:
        return None


def on_changed(live_history_creator):
    def _on_changed(table_listener, row_id, row):
        del table_listener, row_id
        instrument = parse_args().i
        if row.table_type == fxcorepy.O2GTableType.OFFERS and row.instrument == instrument:
            live_history_creator.add_or_update(row)
            if live_history_creator.history is not None:
                print("Add or update: ")
                last_complete_data_frame = live_history_creator.history.tail(1)
                dt = str(last_complete_data_frame.index.values[0])
                dt = dt.replace('T', ' ')
                dt = parser.parse(dt)
                str_prices = str(dt) + ", "
                for price_name in last_complete_data_frame:
                    price_entry = last_complete_data_frame.get(price_name)
                    price_value = price_entry.values[0]
                    str_prices += price_name + "=" + str(price_value) + ", "
                print(str_prices[0: -2])

    return _on_changed


def main():
    pd.options.display.float_format = '{:,.5f}'.format
    pd.options.display.max_columns = 9
    args = parse_args()
    str_user_id = args.l
    str_password = args.p
    str_url = args.u
    str_connection = args.c
    str_session_id = args.session
    str_pin = args.pin
    str_instrument = args.i
    str_timeframe = args.timeframe
    quotes_count = args.bars
    candle_open_price_mode = parse_candle_open_price_mode(args.o)

    with ForexConnect() as fx:
        if candle_open_price_mode is None:
            raise Exception("Invalid value of the candle open price mode. Possible values are "
                            "'first_tick' or 'prev_close'.")

        fx.login(str_user_id, str_password, str_url,
                 str_connection, str_session_id, str_pin,
                 common_samples.session_status_changed)
        try:
            current_unit, current_size = ForexConnect.parse_timeframe(str_timeframe)

            if current_unit == fxcorepy.O2GTimeFrameUnit.TICK:
                # we can't from candles from the t1 time frame
                raise Exception("Do NOT use t* time frame")

            print("Begins collecting live history before uploading a story")

            live_history_creator = LiveHistoryCreator(str_timeframe, candle_open_price_mode=candle_open_price_mode)
            offers = fx.get_table(ForexConnect.OFFERS)

            table_listener = Common.subscribe_table_updates(offers, on_change_callback=on_changed(live_history_creator))
            print("")
            print("Loading old history...")
            nd_array_history = fx.get_history(str_instrument, str_timeframe, None, None, 1, candle_open_price_mode)
            print("Done")

            print("Apply collected history")
            live_history_creator.history = nd_array_history

            print("Accumulating live history...")
            sleep_times = int(quotes_count *
                              common_samples.convert_timeframe_to_seconds(current_unit, current_size) / 60)
            if sleep_times < quotes_count:
                sleep_times = quotes_count
            for i in range(sleep_times):
                time.sleep(60)
                print("Patience...")
            table_listener.unsubscribe()
            print("Done")
            print("")

        except Exception as e:
            common_samples.print_exception(e)
        try:
            fx.logout()
        except Exception as e:
            common_samples.print_exception(e)


if __name__ == "__main__":
    main()
    input("Done! Press enter key to exit\n")
