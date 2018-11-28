import numpy as np
import time
import argparse
import os
import sys

sys.path.append(os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

from forexconnect import fxcorepy, ForexConnect
from forexconnect import Common, LiveHistoryCreator

from dateutil import parser

import xml.etree.ElementTree as ET

import common_samples
import csv
import pytz
from datetime import timedelta, datetime

delimiter = None
datetime_separator = None
format_decimal_places = None
timezone = None


def parse_args():
    arg_parser = argparse.ArgumentParser(description='Process command parameters.')
    arg_parser.add_argument('-p',
                            metavar="PASSWORD",
                            required=True,
                            help='Your password.')
    arg_parser.add_argument('-config', metavar="CONFIG_FILE", default='Configuration.xml',
                            help='Config file')

    args = arg_parser.parse_args()
    return args


verbose = 0
order_request_id = ""


class SaveNewBar:
    def __init__(self, symbol, bars):
        self.symbol = symbol
        self.bars = bars

    def save_bar(self, instrument, history, filename):
        global delimiter
        global datetime_separator
        global format_decimal_places
        global timezone

        if timezone == 'Local':
            time_difference = -time.timezone
        else:
            tz = pytz.timezone(timezone)
            time_difference = tz.utcoffset(datetime.now).total_seconds()

        hist = history[:-1].tail(1)
        with open(filename, "a", newline="") as file:
            writer = csv.writer(file, delimiter=delimiter)
            last_complete_data_frame = hist
            dtime = str(last_complete_data_frame.index.values[0])

            dtime = dtime.replace('T', ' ')
            dt = parser.parse(dtime)
            dt = dt+timedelta(0, time_difference)
            dt = str(dt)
            dtime = dt.replace(' ', datetime_separator)
            out = [dtime]
            str_prices = dtime + ", "
            for price_name in last_complete_data_frame:
                price_entry = last_complete_data_frame.get(price_name)
                price_value = price_entry.values[0]
                str_prices += price_name + "=" + str(price_value) + ", "
                price = price_entry.values[0]
                if format_decimal_places and price_name != "Volume":
                    if instrument.find("JPY") >= 0:
                        out.append("%.3f" % price)
                    else:
                        out.append("%.5f" % price)
                else:
                    out.append(price)
            writer.writerow(out)
            file.close()
            print("New bar saved to "+filename+": "+str_prices[0: -2])

        return


order_created_count = 0


def find_in_tree(tree, node):
    found = tree.find(node)
    if found is None:
        found = []
    return found


def on_request_completed(request_id, response):
    del request_id, response
    global order_created_count
    order_created_count += 1
    return True


def on_changed(live_history, instrument):
    def _on_changed(table_listener, row_id, row):
        del table_listener, row_id
        try:
            if row.table_type == fxcorepy.O2GTableType.OFFERS and row.instrument == instrument:
                live_history.add_or_update(row)
        except Exception as e:
            common_samples.print_exception(e)
            return

    return _on_changed


def on_bar_added(instrument, filename):
    def _on_bar_added(history):
        snb = SaveNewBar(instrument, history[:-1])
        snb.save_bar(instrument, history, filename)

    return _on_bar_added


def session_status_changed(fx, live_history, instrument, str_user_id, str_password, str_url, str_connection,
                           reconnect_on_disconnected):
    offers_listener = None
    first_call = reconnect_on_disconnected
    orders_listener = None

    def _session_status_changed(session, status):
        nonlocal offers_listener
        nonlocal first_call
        nonlocal orders_listener
        if not first_call:
            common_samples.session_status_changed(session.trading_session_descriptors, status)
        else:
            first_call = False
        if status == fxcorepy.AO2GSessionStatus.O2GSessionStatus.CONNECTED:
            offers = fx.get_table(ForexConnect.OFFERS)
            if live_history is not None:
                on_changed_callback = on_changed(live_history, instrument)
                offers_listener = Common.subscribe_table_updates(offers, on_change_callback=on_changed_callback)
        elif status == fxcorepy.AO2GSessionStatus.O2GSessionStatus.DISCONNECTING or \
                status == fxcorepy.AO2GSessionStatus.O2GSessionStatus.RECONNECTING or \
                status == fxcorepy.AO2GSessionStatus.O2GSessionStatus.SESSION_LOST:
            if orders_listener is not None:
                orders_listener.unsubscribe()
                orders_listener = None
            if offers_listener is not None:
                offers_listener.unsubscribe()
                offers_listener = None
        elif status == fxcorepy.AO2GSessionStatus.O2GSessionStatus.DISCONNECTED and reconnect_on_disconnected:
            fx.session.login(str_user_id, str_password, str_url, str_connection)

    return _session_status_changed


def check_params(instr, tf, offer):
    if not instr:
        raise Exception(
            "The instrument is empty")

    if not tf:
        raise Exception(
            "The timeframe is empty")

    if not offer:
        raise Exception(
            "The instrument '{0}' is not valid".format(instr))


def parse_xml(config_file):
    try:
        os.stat(config_file)
    except OSError:
        raise Exception(
            "The configuration file '{0}' does not exist".format(config_file))

    xmlfile = open(config_file, "r")
    conf = ET.parse(xmlfile)
    root = conf.getroot()

    settings = find_in_tree(root, "Settings")

    str_user_id = find_in_tree(settings, "Login").text
    str_url = find_in_tree(settings, "Url").text
    str_connection = find_in_tree(settings, "Connection").text
    str_session_id = find_in_tree(settings, "SessionID").text
    str_pin = find_in_tree(settings, "Pin").text
    delim = find_in_tree(settings, "Delimiter").text
    output_dir = find_in_tree(settings, "OutputDir").text
    dt_separator = find_in_tree(settings, "DateTimeSeparator").text
    fdp = find_in_tree(settings, "FormatDecimalPlaces").text
    tzone = find_in_tree(settings, "Timezone").text

    if tzone != 'EST' and tzone != 'UTC' and tzone != 'Local':
        print('Timezone is not recognized, using EST')
        tzone = 'EST'

    if output_dir:
        if not os.path.exists(output_dir):
            raise Exception(
                "The output directory '{0}' does not exist".format(output_dir))

    if fdp == "Y" or fdp == "y":
        fdp = True
    else:
        fdp = False

    data = []

    for elem in settings.findall("History"):
        data2 = [find_in_tree(elem, "Instrument").text]
        data2.append(find_in_tree(elem, "Timeframe").text)
        if output_dir:
            data2.append(output_dir+"\\"+find_in_tree(elem, "Filename").text)
        else:
            data2.append(find_in_tree(elem, "Filename").text)
        data2.append(find_in_tree(elem, "NumBars").text)
        data2.append(find_in_tree(elem, "Headers").text)
        data.append(data2)

    if len(data) == 0:
        raise Exception(
            "No instruments in the config file are present")

    return str_user_id, str_url, str_connection, str_session_id, str_pin, \
           delim, output_dir, dt_separator, fdp, tzone, data


def set_init_history(fx, lhc, instr, tf, filename, numbars, str_user_id, str_password, str_url, str_connection):
    lhc.append(LiveHistoryCreator(tf))
    last_index = len(lhc)-1
    on_bar_added_callback = on_bar_added(instr, filename)
    lhc[last_index].subscribe(on_bar_added_callback)
    session_status_changed_callback = session_status_changed(fx, lhc[last_index], instr, str_user_id,
                                                            str_password, str_url, str_connection, True)
    session_status_changed_callback(fx.session, fx.session.session_status)
    fx.set_session_status_listener(session_status_changed_callback)

    nd_array_history = fx.get_history(instr, tf, None, None, int(numbars)+2)

    lhc[last_index].history = nd_array_history

    return lhc, nd_array_history


def get_time_difference(tzone):
    if tzone == 'Local':
        time_difference = -time.timezone
    else:
        tz = pytz.timezone(tzone)
        time_difference = tz.utcoffset(datetime.now).total_seconds()
    return time_difference


def save_old_history(instr, filename, headers, nd_array_history, time_difference, dt_separator):
    header = ['DateTime', 'Bid Open', 'Bid High', 'Bid Low', 'Bid Close', 'Ask Open', 'Ask High',
              'Ask Low', 'Ask Close', 'Volume']
    with open(filename, "w", newline="") as file:
        writer = csv.writer(file, delimiter=delimiter)
        if headers:
            head = [headers]
            writer.writerow(head)
        else:
            writer.writerow(header)
        for i in range(1, len(nd_array_history)-1):
            last_complete_data_frame = nd_array_history[i:i+1]
            str2 = str(last_complete_data_frame[0])
            str2 = str2.replace('(', '')
            str2 = str2.replace(')', '')
            str2 = str2.replace("'", '')
            str2 = str2.replace(' ', '')
            str2 = str2.split(',')
            array = np.array(str2)
            array[0] = array[0].replace('T', ' ')
            dt = parser.parse(array[0])
            dt = dt+timedelta(0, time_difference)
            dt = str(dt)
            array[0] = dt.replace(' ', dt_separator)
            out = [array[0]]
            for i2 in range(1, len(array)-1):
                price = float(array[i2])
                if format_decimal_places:
                    if instr.find("JPY") >= 0:
                        out.append("%.3f" % price)
                    else:
                        out.append("%.5f" % price)
                else:
                    out.append(price)
            out.append(array[len(array)-1])
            writer.writerow(out)


def main():
    global delimiter
    global datetime_separator
    global format_decimal_places
    global timezone

    args = parse_args()
    config_file = args.config

    str_password = args.p

    str_user_id, str_url, str_connection, str_session_id, str_pin, delimiter, output_dir, \
        datetime_separator, format_decimal_places, timezone, data = parse_xml(config_file)

    with ForexConnect() as fx:
        try:
            print("Connecting to: user id:"+str_user_id+", url:"+str_url+", Connection:"+str_connection)
            try:
                fx.login(str_user_id, str_password, str_url,
                         str_connection, str_session_id, str_pin,
                         common_samples.session_status_changed)
            except Exception as e:
                print("Exception: " + str(e))
                raise Exception(
                    "Login failed. Invalid parameters")

            lhc = []
            for param in data:
                offer = Common.get_offer(fx, param[0])

                check_params(param[0], param[1], offer)

                tf = ForexConnect.get_timeframe(fx, param[1])
                if not tf:
                    raise Exception(
                        "The timeframe '{0}' is not valid".format(param[1]))

                lhc, nd_array_history = set_init_history(fx, lhc, param[0], param[1], param[2], param[3], 
                                                         str_user_id, str_password, str_url, str_connection)

                time_difference = get_time_difference(timezone)

                save_old_history(param[0], param[2], param[4], nd_array_history, time_difference, datetime_separator)

                print("Old history saved to "+param[2])

            while True:
                time.sleep(60)

        except Exception as e:
            common_samples.print_exception(e)
        try:
            fx.set_session_status_listener(session_status_changed(fx, None, None, str_user_id, str_password,
                                                                  str_url, str_connection, False))
            fx.logout()
        except Exception as e:
            common_samples.print_exception(e)


if __name__ == "__main__":
    main()
    print("")
    input("Done! Press enter key to exit\n")
