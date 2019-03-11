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

from forexconnect import fxcorepy, ForexConnect

import common_samples

# function for print available descriptors
def primary_session_status_changed(session, status):
    print("PrimarySessionStatus: " + str(status))
    if status == fxcorepy.AO2GSessionStatus.O2GSessionStatus.TRADING_SESSION_REQUESTED:
        descriptors = session.trading_session_descriptors
        print("Session descriptors")
        print(" {0:>7} | {1:>7} | {2:>30} | {3:>7}\n".format("id", "name", "description", "requires pin"))
        for desc in descriptors:
            print(" {0:>7} | {1:>7} | {2:>30} | {3:>7}\n".format(desc.id, desc.name,
                                                                 desc.description,
                                                                 str(desc.requires_pin)))


def secondary_session_status_changed(session, status):
    print("SecondarySessionStatus: " + str(status))
    if status == fxcorepy.AO2GSessionStatus.O2GSessionStatus.TRADING_SESSION_REQUESTED:
        descriptors = session.trading_session_descriptors
        print("Session descriptors for secondary session")
        print(" {0:>7} | {1:>7} | {2:>30} | {3:>7}\n".format("id", "name", "description", "requires pin"))
        for desc in descriptors:
            print(" {0:>7} | {1:>7} | {2:>30} | {3:>7}\n".format(desc.id, desc.name,
                                                                 desc.description,
                                                                 str(desc.requires_pin)))


def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)

    args = parser.parse_args()

    return args


def main():
    args = parse_args()
    str_user_id = args.l
    str_password = args.p
    str_u_r_l = args.u
    str_connection = args.c
    str_session_id = args.session
    str_pin = args.pin

    with ForexConnect() as fx:
        try:
            session = fx.login(str_user_id, str_password, str_u_r_l,
                               str_connection, str_session_id, str_pin,
                               primary_session_status_changed)
            print("")
            print("Requesting a token...")
            token = session.token
            print("Token obtained: {0}".format(token))

            with ForexConnect() as fx_secondary:
                print("")
                print("Login using token...")
                fx_secondary.login_with_token(str_user_id, token, str_u_r_l,
                                              str_connection, str_session_id, str_pin,
                                              secondary_session_status_changed)
                print("")
                print("Logout...")
                fx_secondary.logout()
        except Exception as e:
            common_samples.print_exception(e)
        try:
            fx.logout()
        except Exception as e:
            common_samples.print_exception(e)


if __name__ == "__main__":
    main()
    print("")
    input("Press enter key to exit\n")
