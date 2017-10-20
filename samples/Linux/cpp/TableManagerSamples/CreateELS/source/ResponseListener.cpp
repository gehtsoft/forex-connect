#include "stdafx.h"
#include <math.h>

#include <sstream>
#include <iomanip>
#include "ResponseListener.h"

ResponseListener::ResponseListener()
{
    mRefCount = 1;
    mRequestID = "";
    mResponseEvent = CreateEvent(0, FALSE, FALSE, 0);
}

ResponseListener::~ResponseListener()
{
    CloseHandle(mResponseEvent);
}

/** Increase reference counter. */
long ResponseListener::addRef()
{
    return InterlockedIncrement(&mRefCount);
}

/** Decrease reference counter. */
long ResponseListener::release()
{
    long rc = InterlockedDecrement(&mRefCount);
    if (rc == 0)
        delete this;
    return rc;
}

/** Set request. */
void ResponseListener::setRequestID(const char *sRequestID)
{
    mRequestID = sRequestID;
    ResetEvent(mResponseEvent);
}

bool ResponseListener::waitEvents()
{
    return WaitForSingleObject(mResponseEvent, _TIMEOUT) == 0;
}

void ResponseListener::stopWaiting()
{
    SetEvent(mResponseEvent);
}

/** Request execution completed data handler. */
void ResponseListener::onRequestCompleted(const char *requestId, IO2GResponse *response)
{
}

/** Request execution failed data handler. */
void ResponseListener::onRequestFailed(const char *requestId , const char *error)
{
    if (mRequestID == requestId)
    {
        std::cout << "The request has been failed. ID: " << requestId << " : " << error << std::endl;
        stopWaiting();
    }
}

/** Request update data received data handler. */
void ResponseListener::onTablesUpdates(IO2GResponse *data)
{
}

