#include "stdafx.h"
#include <math.h>
#include <algorithm>

#include <sstream>
#include <iomanip>
#include "ResponseListener.h"

ResponseListener::ResponseListener(IO2GSession *session)
{
    mSession = session;
    mSession->addRef();
    mRefCount = 1;
    mResponseEvent = CreateEvent(0, FALSE, FALSE, 0);
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
void ResponseListener::setRequestIDs(std::vector<std::string> &requestIDs)
{
    mRequestIDs.resize(requestIDs.size());
    std::copy(requestIDs.begin(), requestIDs.end(), mRequestIDs.begin());
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
    if (mResponse)
        mResponse->addRef();
    return mResponse;
}

/** Request execution completed data handler. */
void ResponseListener::onRequestCompleted(const char *requestId, IO2GResponse *response)
{
    if (response && std::find(mRequestIDs.begin(), mRequestIDs.end(), requestId) != mRequestIDs.end())
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
    if (std::find(mRequestIDs.begin(), mRequestIDs.end(), requestId) != mRequestIDs.end())
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
                        if (reader->getUpdateType(i) == Insert)
                        {
                            std::vector<std::string>::iterator iter;
                            iter = std::find(mRequestIDs.begin(), mRequestIDs.end(), order->getRequestID());
                            if (iter != mRequestIDs.end())
                            {
                                std::cout << "The order has been added. OrderID='" << order->getOrderID() << "', "
                                        << "Type='" << order->getType() << "', "
                                        << "BuySell='" << order->getBuySell() << "', "
                                        << "Rate='" << order->getRate() << "', "
                                        << "TimeInForce='" << order->getTimeInForce() << "'"
										<< "ContType='" << order->getContingencyType() << "'"
										<< "ContId='" << order->getContingentOrderID() << "'"
										<< "PrimaryID='" << order->getPrimaryID() << "'"
                                        << std::endl;
                                mRequestIDs.erase(iter);
                                if (mRequestIDs.size() == 0)
                                    SetEvent(mResponseEvent);
                            }
                        }
                    }
                }
            }
        }
    }
}

