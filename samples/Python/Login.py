import argparse

from forexconnect import ForexConnect

import common_samples

def parse_args():
    parser = argparse.ArgumentParser(description='Process command parameters.')
    common_samples.add_main_arguments(parser)
    args = parser.parse_args()

    return args


def main():
    args = parse_args()
    str_user_i_d = args.l
    str_password = args.p
    str_url = args.u
    str_connection = args.c
    str_session_id = args.session
    str_pin = args.pin

    with ForexConnect() as fx:
        try:
            fx.login(str_user_i_d, str_password, str_url,
                     str_connection, str_session_id, str_pin,
                     common_samples.session_status_changed)

            print("")
            print("Accounts:")
            accounts_response_reader = fx.get_table_reader(fx.ACCOUNTS)
            for account in accounts_response_reader:
                print("{0:s}".format(account.account_id))

            print("")
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
