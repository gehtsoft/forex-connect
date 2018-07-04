#pragma once

/** Table manager listener class. */

class TableManagerListener : public IO2GTableManagerListener
{
 public:
    TableManagerListener();

    /** Increase reference counter. */
    virtual long addRef();

    /** Decrease reference counter. */
    virtual long release();

    bool isLoaded() const;
    bool hasError() const;
    bool waitEvents();
    void reset();

    virtual void onStatusChanged(O2GTableManagerStatus, IO2GTableManager *);

 private:
    long mRefCount;
    O2GTableManagerStatus mLastStatus;
    bool mLoaded;
    bool mError;
    HANDLE mTablesEvent;

 protected:
    /** Destructor. */
    virtual ~TableManagerListener();

};

