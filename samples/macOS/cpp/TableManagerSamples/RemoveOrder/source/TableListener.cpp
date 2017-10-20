#include "stdafx.h"
#include "ResponseListener.h"
#include "TableListener.h"

bool isLimitEntryOrder(IO2GOrderRow *order)
{
    const char* type = order->getType();
    return strcmp(type, "LE") == 0;
}

TableListener::TableListener(ResponseListener *responseListener)
{
    mRefCount = 1;
    mResponseListener = responseListener;
    mResponseListener->addRef();
    mRequestID = "";
    mOrderID = "";
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

std::string TableListener::getOrderID()
{
    return mOrderID;
}

void TableListener::onAdded(const char *rowID, IO2GRow *row)
{
    if (row->getTableType() == Orders)
    {
        IO2GOrderRow *order = static_cast<IO2GOrderRow *>(row);
        if (mRequestID == order->getRequestID() && 
                (isLimitEntryOrder(order) && mOrderID.empty()))
        {
            mOrderID = order->getOrderID();
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
    if (row->getTableType() == Orders)
    {
        IO2GOrderRow *order = static_cast<IO2GOrderRow *>(row);
        if (mRequestID == order->getRequestID() && !mOrderID.empty())
        {
            std::cout << "The order has been deleted. Order ID='" << order->getOrderID() << "'" << std::endl;
            mResponseListener->stopWaiting();
        }
    }
}

void TableListener::onStatusChanged(O2GTableStatus status)
{
}

void TableListener::subscribeEvents(IO2GTableManager *manager)
{
    O2G2Ptr<IO2GOrdersTable> ordersTable = (IO2GOrdersTable *)manager->getTable(Orders);

    ordersTable->subscribeUpdate(Insert, this);
    ordersTable->subscribeUpdate(Delete, this);
}

void TableListener::unsubscribeEvents(IO2GTableManager *manager)
{
    O2G2Ptr<IO2GOrdersTable> ordersTable = (IO2GOrdersTable *)manager->getTable(Orders);

    ordersTable->unsubscribeUpdate(Insert, this);
    ordersTable->unsubscribeUpdate(Delete, this);
}
