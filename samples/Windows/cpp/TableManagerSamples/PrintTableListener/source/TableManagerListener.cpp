#include "stdafx.h"
#include "TableManagerListener.h"

TableManagerListener::TableManagerListener()
{
    mRefCount = 1;
    reset();
    mTablesEvent = CreateEvent(0, FALSE, FALSE, 0);
}

TableManagerListener::~TableManagerListener()
{
    CloseHandle(mTablesEvent);
}

/** Increase reference counter. */
long TableManagerListener::addRef()
{
    return InterlockedIncrement(&mRefCount);
}

/** Decrease reference counter. */
long TableManagerListener::release()
{
    long rc = InterlockedDecrement(&mRefCount);
    if (rc == 0)
        delete this;
    return rc;
}

void TableManagerListener::reset()
{
    mLastStatus = TablesLoading;
    mLoaded = false;
    mError = false;
}

bool TableManagerListener::hasError() const
{
    return mError;
}

bool TableManagerListener::isLoaded() const
{
    return mLoaded;
}

bool TableManagerListener::waitEvents()
{
    if (mLastStatus == TablesLoading)
        return WaitForSingleObject(mTablesEvent, _TIMEOUT) == 0;
    return true;
}

void TableManagerListener::onStatusChanged(O2GTableManagerStatus status, IO2GTableManager *tableManager)
{
    mLastStatus = status;
    switch (status)
    {
    case TablesLoaded:
        mLoaded = true;
        mError = false;
        SetEvent(mTablesEvent);
        break;
    case TablesLoadFailed:
        mLoaded = false;
        mError = true;
        SetEvent(mTablesEvent);
        break;
    }
}

