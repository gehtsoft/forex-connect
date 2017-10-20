#include "stdafx.h"
#include <math.h>

#include <sstream>
#include <iomanip>
#include "ResponseListener.h"

ResponseListener::ResponseListener(IO2GSession *session)
{
    mSession = session;
    mSession->addRef();
    mRefCount = 1;
    mResponseEvent = CreateEvent(0, FALSE, FALSE, 0);
    mRequestID = "";
    mOrderID = "";
    mResponse = NULL;
    std::cout.precision(2);
}

ResponseListener::~ResponseListener()
{
    if (mResponse)
        mResponse->release();
    mSession->release();
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
    if (mResponse)
    {
        mResponse->release();
        mResponse = NULL;
    }
    ResetEvent(mResponseEvent);
}

bool ResponseListener::waitEvents()
{
    return WaitForSingleObject(mResponseEvent, _TIMEOUT) == 0;
}

/** Gets response.*/
IO2GResponse *ResponseListener::getResponse()
{
    mResponse->addRef();
    return mResponse;
}

std::string ResponseListener::getOrderID()
{
    return mOrderID;
}

/** Request execution completed data handler. */
void ResponseListener::onRequestCompleted(const char *requestId, IO2GResponse *response)
{
    if (response && mRequestID == requestId)
    {
        mResponse = response;
        mResponse->addRef();
        if (response->getType() != CreateOrderResponse)
            SetEvent(mResponseEvent);
    }
}

/** Request execution failed data handler. */
void ResponseListener::onRequestFailed(const char *requestId , const char *error)
{
    if (mRequestID == requestId)
    {
        std::cout << "The request has been failed. ID: " << requestId << " : " << error << std::endl;
        SetEvent(mResponseEvent);
    }
}

/** Request update data received data handler. */
void ResponseListener::onTablesUpdates(IO2GResponse *data)
{
    if (data)
    {
        O2G2Ptr<IO2GResponseReaderFactory> factory = mSession->getResponseReaderFactory();
        if (factory)
        {
            O2G2Ptr<IO2GTablesUpdatesReader> reader = factory->createTablesUpdatesReader(data);
            if (reader)
            {
                for (int i = 0; i < reader->size(); ++i)
                {
                    if (reader->getUpdateTable(i) == Orders)
                    {
                        O2G2Ptr<IO2GOrderRow> order = reader->getOrderRow(i);
                        if (mRequestID == order->getRequestID() && reader->getUpdateType(i) == Insert)
                        {
                            std::cout << "The order has been added. OrderID='" << order->getOrderID() << "', "
                                    << "Type='" << order->getType() << "', "
                                    << "BuySell='" << order->getBuySell() << "', "
                                    << "Rate='" << order->getRate() << "', "
                                    << "TimeInForce='" << order->getTimeInForce() << "'"
                                    << std::endl;
                            mOrderID = order->getOrderID();
                            SetEvent(mResponseEvent);
                            break;
                        }
                    }
                }
            }
        }
    }
}

