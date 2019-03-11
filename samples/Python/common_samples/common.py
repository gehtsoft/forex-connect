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

import logging
import __main__
import datetime
import traceback
import argparse
import sys

from forexconnect import fxcorepy

logging.basicConfig(filename='{0}.log'.format(__main__.__file__), level=logging.INFO,
                    format='%(asctime)s %(levelname)s %(message)s', datefmt='%m.%d.%Y %H:%M:%S')
console = logging.StreamHandler(sys.stdout)
console.setLevel(logging.INFO)
logging.getLogger('').addHandler(console)


def add_main_arguments(parser: argparse.ArgumentParser):
    parser.add_argument('-l',
                        metavar="LOGIN",
                        required=True,
                        help='Your user name.')

    parser.add_argument('-p',
                        metavar="PASSWORD",
                        required=True,
                        help='Your password.')

    parser.add_argument('-u',
                        metavar="URL",
                        required=True,
                        help='The server URL. For example,\
                                 http://www.fxcorporate.com/Hosts.jsp.')

    parser.add_argument('-c',
                        metavar="CONNECTION",
                        required=True,
                        help='The connection name. For example, \
                                 "Demo" or "Real".')

    parser.add_argument('-session',
                        help='The database name. Required only for users who\
                                 have accounts in more than one database.\
                                 Optional parameter.')

    parser.add_argument('-pin',
                        help='Your pin code. Required only for users who have \
                                 a pin. Optional parameter.')

def add_candle_open_price_mode_argument(parser: argparse.ArgumentParser):
    parser.add_argument('-o',
                        metavar="CANDLE_OPEN_PRICE_MODE",
                        default="prev_close",
                        help='Ability to set the open price candles mode. \
                        Possible values are first_tick, prev_close. For more information see description \
                        of O2GCandleOpenPriceMode enumeration. Optional parameter.')

def add_instrument_timeframe_arguments(parser: argparse.ArgumentParser, timeframe: bool = True):
    parser.add_argument('-i',
                        metavar="INSTRUMENT",
                        default="EUR/USD",
                        help='An instrument which you want to use in sample. \
                                  For example, "EUR/USD".')

    if timeframe:
        parser.add_argument('-timeframe',
                            metavar="TIMEFRAME",
                            default="m1",
                            help='Time period which forms a single candle. \
                                      For example, m1 - for 1 minute, H1 - for 1 hour.')


def add_direction_rate_lots_arguments(parser: argparse.ArgumentParser, direction: bool = True, rate: bool = True,
                                      lots: bool = True):
    if direction:
        parser.add_argument('-d', metavar="TYPE", required=True,
                            help='The order direction. Possible values are: B - buy, S - sell.')
    if rate:
        parser.add_argument('-r', metavar="RATE", required=True, type=float,
                            help='Desired price of an entry order.')
    if lots:
        parser.add_argument('-lots', metavar="LOTS", default=1, type=int,
                            help='Trade amount in lots.')


def add_account_arguments(parser: argparse.ArgumentParser):
    parser.add_argument('-account', metavar="ACCOUNT",
                        help='An account which you want to use in sample.')


def valid_datetime(check_future: bool):
    def _valid_datetime(str_datetime: str):
        date_format = '%m.%d.%Y %H:%M:%S'
        try:
            result = datetime.datetime.strptime(str_datetime, date_format).replace(
                tzinfo=datetime.timezone.utc)
            if check_future and result > datetime.datetime.utcnow().replace(tzinfo=datetime.timezone.utc):
                msg = "'{0}' is in the future".format(str_datetime)
                raise argparse.ArgumentTypeError(msg)
            return result
        except ValueError:
            now = datetime.datetime.now()
            msg = "The date '{0}' is invalid. The valid data format is '{1}'. Example: '{2}'".format(
                str_datetime, date_format, now.strftime(date_format))
            raise argparse.ArgumentTypeError(msg)
    return _valid_datetime


def add_date_arguments(parser: argparse.ArgumentParser, date_from: bool = True, date_to: bool = True):
    if date_from:
        parser.add_argument('-datefrom',
                            metavar="\"m.d.Y H:M:S\"",
                            help='Date/time from which you want to receive\
                                      historical prices. If you leave this argument as it \
                                      is, it will mean from last trading day. Format is \
                                      "m.d.Y H:M:S". Optional parameter.',
                            type=valid_datetime(True)
                            )
    if date_to:
        parser.add_argument('-dateto',
                            metavar="\"m.d.Y H:M:S\"",
                            help='Datetime until which you want to receive \
                                      historical prices. If you leave this argument as it is, \
                                      it will mean to now. Format is "m.d.Y H:M:S". \
                                      Optional parameter.',
                            type=valid_datetime(False)
                            )


def add_report_date_arguments(parser: argparse.ArgumentParser, date_from: bool = True, date_to: bool = True):
    if date_from:
        parser.add_argument('-datefrom',
                            metavar="\"m.d.Y H:M:S\"",
                            help='Datetime from which you want to receive\
                                      combo account statement report. If you leave this argument as it \
                                      is, it will mean from last month. Format is \
                                      "m.d.Y H:M:S". Optional parameter.',
                            type=valid_datetime(True)
                            )
    if date_to:
        parser.add_argument('-dateto',
                            metavar="\"m.d.Y H:M:S\"",
                            help='Datetime until which you want to receive \
                                      combo account statement report. If you leave this argument as it is, \
                                      it will mean to now. Format is "m.d.Y H:M:S". \
                                      Optional parameter.',
                            type=valid_datetime(True)
                            )


def add_max_bars_arguments(parser: argparse.ArgumentParser):
    parser.add_argument('-quotescount',
                        metavar="MAX",
                        default=0,
                        type=int,
                        help='Max number of bars. 0 - Not limited')


def add_bars_arguments(parser: argparse.ArgumentParser):
    parser.add_argument('-bars',
                        metavar="COUNT",
                        default=3,
                        type=int,
                        help='Build COUNT bars. Optional parameter.')


def print_exception(exception: Exception):
    logging.error("Exception: {0}\n{1}".format(exception, traceback.format_exc()))


# function for print available descriptors
def session_status_changed(session: fxcorepy.O2GSession,
                           status: fxcorepy.AO2GSessionStatus.O2GSessionStatus):
    logging.info("Status: " + str(status))
    if status == fxcorepy.AO2GSessionStatus.O2GSessionStatus.TRADING_SESSION_REQUESTED:
        descriptors = session.trading_session_descriptors
        logging.info("Session descriptors:")
        logging.info(" {0:>7} | {1:>7} | {2:>30} | {3:>7}\n".format("id", "name", "description", "requires pin"))
        for desc in descriptors:
            logging.info(" {0:>7} | {1:>7} | {2:>30} | {3:>7}\n".format(desc.id, desc.name,
                                                                        desc.description,
                                                                        str(desc.requires_pin)))


def diff_month(year: int, month: int, date2: datetime):
    return (year - date2.year) * 12 + month - date2.month


def convert_timeframe_to_seconds(unit: fxcorepy.O2GTimeFrameUnit, size: int):
    current_unit = unit
    current_size = size
    step = 1
    if current_unit == fxcorepy.O2GTimeFrameUnit.MIN:
        step = 60  # leads to seconds
    elif current_unit == fxcorepy.O2GTimeFrameUnit.HOUR:
        step = 60*60
    elif current_unit == fxcorepy.O2GTimeFrameUnit.DAY:
        step = 60*60*24
    elif current_unit == fxcorepy.O2GTimeFrameUnit.WEEK:
        step = 60*60*24*7
    elif current_unit == fxcorepy.O2GTimeFrameUnit.MONTH:
        step = 60 * 60 * 24 * 30
    elif current_unit == fxcorepy.O2GTimeFrameUnit.TICK:
        step = 1
    return step * current_size
