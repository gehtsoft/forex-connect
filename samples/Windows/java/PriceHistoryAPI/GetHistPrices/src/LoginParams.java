/* Copyright 2019 FXCM Global Services, LLC

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use these files except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
package common;

public class LoginParams {
    public static final String LOGIN_NOT_SPECIFIED = "'Login' is not specified (/l|-l|/login|--login)";
    public static final String PASSWORD_NOT_SPECIFIED = "'Password' is not specified (/p|-p|/password|--password)";
    public static final String URL_NOT_SPECIFIED = "'URL' is not specified (/u|-u|/url|--url)";
    public static final String CONNECTION_NOT_SPECIFIED = "'Connection' is not specified (/c|-c|/connection|--connection)";

    // Getters

    public String getLogin() {
        return mLogin;
    }

    private String mLogin;

    public String getPassword() {
        return mPassword;
    }

    private String mPassword;

    public String getURL() {
        return mURL;
    }

    private String mURL;

    public String getConnection() {
        return mConnection;
    }

    private String mConnection;

    public String getSessionID() {
        return mSessionID;
    }
    private String mSessionID;

    public String getPin() {
        return mPin;
    }
    private String mPin;
    
    public LoginParams(String[] args) {
        // Get parameters with short keys
        mLogin = getArgument(args, "l");
        mPassword = getArgument(args, "p");
        mURL = getArgument(args, "u");
        mConnection = getArgument(args, "c");

        // If parameters with short keys are not specified, get parameters with long keys
        if (mLogin.isEmpty()) {
            mLogin = getArgument(args, "login");
        }
        if (mPassword.isEmpty()) {
            mPassword = getArgument(args, "password");
        }
        if (mURL.isEmpty()) {
            mURL = getArgument(args, "url");
        }
        if (mConnection.isEmpty()) {
            mConnection = getArgument(args, "connection");
        }

        // Get optional parameters
        mSessionID = getArgument(args, "sessionid");
        mPin = getArgument(args, "pin");
    }

    private String getArgument(String[] args, String sKey) {
        for (int i = 0; i < args.length; i++) {
            int iDelimOffset = 0;
            if (args[i].startsWith("--")) {
                iDelimOffset = 2;
            } else if (args[i].startsWith("-") || args[i].startsWith("/")) {
                iDelimOffset = 1;
            }

            if (args[i].substring(iDelimOffset).equals(sKey) && (args.length > i + 1)) {
                return args[i + 1];
            }
        }
        return "";
    }
}
