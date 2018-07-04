#include "stdafx.h"
#include "ResponseListener.h"
#include "TableListener.h"

TableListener::TableListener(ResponseListener *responseListener)
{
    mRefCount = 1;
    mResponseListener = responseListener;
    mResponseListener->addRef();
    mRequestID = "";
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
void TableListener::setRequestID(const char *sRequestID)
{
    mRequestID = sRequestID;
}

void TableListener::onAdded(const char *rowID, IO2GRow *row)
{
    O2GTable type = row->getTableType();
    if (type == Orders)
    {
        IO2GOrderRow *order = static_cast<IO2GOrderRow *>(row);
        if (mRequestID == order->getRequestID())
        {
            std::cout << "The order has been added. OrderID='" << order->getOrderID() << "', "
                    << "Type='" << order->getType() << "', "
                    << "BuySell='" << order->getBuySell() << "', "
                    << "Rate='" << order->getRate() << "', "
                    << "TimeInForce='" << order->getTimeInForce() << "'"
                    << std::endl;
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
