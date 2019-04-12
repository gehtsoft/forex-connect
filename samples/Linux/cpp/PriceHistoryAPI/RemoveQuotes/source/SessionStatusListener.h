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
#pragma once

/** Status listener for the sessions. */
class SessionStatusListener : public IO2GSessionStatus
{
 private:
    volatile unsigned int mRefCount;
    /** Subsession identifier. */
    std::string mSessionID;
    /** Pin code. */
    std::string mPin;
    /** Flag indicating that connection is set. */
    bool mConnected;
    /** Flag indicating that connection was ended. */
    bool mDisconnected;
    /** Flag indicating whether sessions must be printed. */
    bool mPrintSubsessions;
    /** Session object. */
    IO2GSession *mSession;
    /** Event handle. */
    HANDLE mSessionEvent;

 protected:
    /** Destructor. */
    ~SessionStatusListener();

 public:
    /** Constructor.
        @param session          Session to listen to.
        @param printSubsessions To print subsessions or not.
        @param sessionID        Identifier of the subsession or NULL in case
                                no subsession selector is expected.
        @param pin              Pin code or NULL in case no pin code request is expected.
    */
    SessionStatusListener(IO2GSession *session, bool printSubsessions, const char *sessionID = 0, const char *pin = 0);

    /** Increase reference counter. */
    virtual long addRef();

    /** Decrease reference counter. */
    virtual long release();

    /** Callback called when login has been failed. */
    virtual void onLoginFailed(const char *error);

    /** Callback called when session status has been changed. */
    virtual void onSessionStatusChanged(IO2GSessionStatus::O2GSessionStatus status);

    /** Check whether session is connected. */
    bool isConnected() const;

    /** Check whether session is disconnected. */
    bool isDisconnected() const;

    /** Reset error information (use before login/logout). */
    void reset();

    /** Wait for connection or error. */
    bool waitEvents();
};

