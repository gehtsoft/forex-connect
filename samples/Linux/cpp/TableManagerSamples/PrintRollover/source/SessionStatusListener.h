#pragma once

/** Status listener for the sessions. */

class SessionStatusListener : public IO2GSessionStatus
{
private:
    long mRefCount;
    /** Subsession identifier. */
    std::string mSessionID;
    /** Pin code. */
    std::string mPin;
    /** Error flag. */
    bool mError;
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

    /** Check whether error happened. */
    bool hasError() const;

    /** Check whether session is connected. */
    bool isConnected() const;

    /** Check whether session is disconnected. */
    bool isDisconnected() const;

    /** Reset error information (use before login/logout). */
    void reset();

    /** Wait for connection or error. */
    bool waitEvents();

};

