#include "stdafx.h"
#include "ResponseListener.h"
#include "TableListener.h"

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

void TableListener::onAdded(const char *rowID, IO2GRow *row)
{
    O2GTable type = row->getTableType();
    switch(type)
    {
    case Orders:
    {
        IO2GOrderRow *order = static_cast<IO2GOrderRow *>(row);
        if (mRequestID != order->getRequestID())
            break;
        std::cout << "The order has been added. Order ID: " << order->getOrderID() << std::endl;
        mOrderID = order->getOrderID();
    }
    break;
    case Trades:
    {
        IO2GTradeRow *trade = static_cast<IO2GTradeRow *>(row);
        std::cout << "The position has been opened. TradeID='" << trade->getTradeID()
            << "', TradeIDOrigin='" << trade->getTradeIDOrigin()<< "'" << std::endl;
    }
    break;
    case ClosedTrades:
    {
        IO2GClosedTradeRow *closedTrade = static_cast<IO2GClosedTradeRow *>(row);
        std::cout << "The position has been closed. TradeID='" << closedTrade->getTradeID()
            << "'" << std::endl;
    }
    break;
    case Messages:
    {
        IO2GMessageRow *message = static_cast<IO2GMessageRow *>(row);
        std::string text(message->getText());
        size_t findPos = text.find(mOrderID.c_str());
        if (findPos != std::string::npos)
        {
            std::cout << "Feature='" << message->getFeature() << "', Message='"
                << text << "'" << std::endl;
        }
    }
    break;
    }

}

void TableListener::onChanged(const char *rowID, IO2GRow *row)
{
    O2GTable type = row->getTableType();
    if (type == Accounts)
    {
        IO2GAccountTableRow *account = (IO2GAccountTableRow *)row;
        std::cout << "The balance has been changed AccountID=" << account->getAccountID() <<
                      " Balance=" << std::fixed << account->getBalance() <<
                      " Equity=" << std::fixed << account->getEquity() << std::endl;
    }
}

void TableListener::onDeleted(const char *rowID, IO2GRow *row)
{
    IO2GOrderRow *order = static_cast<IO2GOrderRow *>(row);
    if (mRequestID == order->getRequestID())
    {
        const char *sStatus = order->getStatus();
        if (sStatus[0] == 'R')
        {
            printOrder("An order has been rejected", order);
        }
        else
        {
            printOrder("An order is going to be removed", order);
        }
        mResponseListener->stopWaiting();
    }
}

void TableListener::onStatusChanged(O2GTableStatus status)
{
}

void TableListener::printOrder(const char *sCaption, IO2GOrderRow *orderRow)
{
    std::cout << sCaption << ": OrderID='" << orderRow->getOrderID() << "', "
        << "TradeID='" << orderRow->getTradeID() << "', "
        << "Status='" << orderRow->getStatus() << "', "
        << "Amount='" << orderRow->getAmount() << "', "
        << "OriginAmount='" << orderRow->getOriginAmount() << "', "
        << "FilledAmount='" << orderRow->getFilledAmount() << "'" << std::endl;
}

void TableListener::subscribeEvents(IO2GTableManager *manager)
{
    O2G2Ptr<IO2GAccountsTable> accountsTable = (IO2GAccountsTable *)manager->getTable(Accounts);
    O2G2Ptr<IO2GOrdersTable> ordersTable = (IO2GOrdersTable *)manager->getTable(Orders);
    O2G2Ptr<IO2GTradesTable> tradesTable = (IO2GTradesTable *)manager->getTable(Trades);
    O2G2Ptr<IO2GMessagesTable> messagesTable = (IO2GMessagesTable *)manager->getTable(Messages);
    O2G2Ptr<IO2GClosedTradesTable> closedTradesTable = (IO2GClosedTradesTable *)manager->getTable(ClosedTrades);

    accountsTable->subscribeUpdate(Update, this);
    ordersTable->subscribeUpdate(Insert, this);
    ordersTable->subscribeUpdate(Delete, this);
    tradesTable->subscribeUpdate(Insert, this);
    tradesTable->subscribeUpdate(Update, this);
    closedTradesTable->subscribeUpdate(Insert, this);
    messagesTable->subscribeUpdate(Insert, this);
}

void TableListener::unsubscribeEvents(IO2GTableManager *manager)
{
    O2G2Ptr<IO2GAccountsTable> accountsTable = (IO2GAccountsTable *)manager->getTable(Accounts);
    O2G2Ptr<IO2GOrdersTable> ordersTable = (IO2GOrdersTable *)manager->getTable(Orders);
    O2G2Ptr<IO2GTradesTable> tradesTable = (IO2GTradesTable *)manager->getTable(Trades);
    O2G2Ptr<IO2GMessagesTable> messagesTable = (IO2GMessagesTable *)manager->getTable(Messages);
    O2G2Ptr<IO2GClosedTradesTable> closedTradesTable = (IO2GClosedTradesTable *)manager->getTable(ClosedTrades);

    accountsTable->unsubscribeUpdate(Update, this);
    ordersTable->unsubscribeUpdate(Insert, this);
    ordersTable->unsubscribeUpdate(Delete, this);
    tradesTable->unsubscribeUpdate(Insert, this);
    tradesTable->unsubscribeUpdate(Update, this);
    closedTradesTable->unsubscribeUpdate(Insert, this);
    messagesTable->unsubscribeUpdate(Insert, this);
}
