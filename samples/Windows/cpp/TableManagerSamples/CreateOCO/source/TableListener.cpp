#include "stdafx.h"
#include <algorithm>
#include "ResponseListener.h"
#include "TableListener.h"

TableListener::TableListener(ResponseListener *responseListener)
{
    mRefCount = 1;
    mResponseListener = responseListener;
    mResponseListener->addRef();
    std::cout.precision(2);
}

TableListener::~TableListener(void)
{
    if (mResponseListener)
        mResponseListener->release();
}

long TableListener::addRef()
{
    return InterlockedIncrement(&mRefCount);
}

long TableListener::release()
{
    InterlockedDecrement(&mRefCount);
    if(mRefCount == 0)
        delete this;
    return mRefCount;
}

/** Set request. */
void TableListener::setRequestIDs(std::vector<std::string> &requestIDs)
{
    mRequestIDs.resize(requestIDs.size());
    std::copy(requestIDs.begin(), requestIDs.end(), mRequestIDs.begin());
}

void TableListener::onAdded(const char *rowID, IO2GRow *row)
{
    if (row->getTableType() == Orders)
    {
        IO2GOrderRow *order = static_cast<IO2GOrderRow *>(row);
        std::vector<std::string>::iterator iter;
        iter = std::find(mRequestIDs.begin(), mRequestIDs.end(), order->getRequestID());
        if (iter != mRequestIDs.end())
        {
            std::cout << "The order has been added. OrderID='" << order->getOrderID() << "', "
                    << "Type='" << order->getType() << "', "
                    << "BuySell='" << order->getBuySell() << "', "
                    << "Rate='" << order->getRate() << "', "
                    << "TimeInForce='" << order->getTimeInForce() << "'"
                    << std::endl;
            mRequestIDs.erase(iter);
            if (mRequestIDs.size() == 0)
                mResponseListener->stopWaiting();
        }
    }
}

void TableListener::onChanged(const char *rowID, IO2GRow *row)
{
}

void TableListener::onDeleted(const char *rowID, IO2GRow *row)
{
}

void TableListener::onStatusChanged(O2GTableStatus status)
{
}

void TableListener::subscribeEvents(IO2GTableManager *manager)
{
    O2G2Ptr<IO2GOrdersTable> ordersTable = (IO2GOrdersTable *)manager->getTable(Orders);

    ordersTable->subscribeUpdate(Insert, this);
}

void TableListener::unsubscribeEvents(IO2GTableManager *manager)
{
    O2G2Ptr<IO2GOrdersTable> ordersTable = (IO2GOrdersTable *)manager->getTable(Orders);

    ordersTable->unsubscribeUpdate(Insert, this);
}
