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
    if (mResponse)
        mResponse->addRef();
    return mResponse;
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
                    switch (reader->getUpdateTable(i))
                    {
                    case Orders:
                    {
                        O2G2Ptr<IO2GOrderRow> order = reader->getOrderRow(i);
                        if (mRequestID != order->getRequestID())
                            break;
                        if (reader->getUpdateType(i) == Insert)
                        {
                            std::cout << "The order has been added. Order ID: " << order->getOrderID() << std::endl;
                            mOrderID = order->getOrderID();
                        }
                        else if (reader->getUpdateType(i) == Delete)
                        {
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
                                SetEvent(mResponseEvent);
                            }
                        }
                    }
                    break;
                    case Trades:
                    {
                        if (reader->getUpdateType(i) == Insert)
                        {
                            O2G2Ptr<IO2GTradeRow> trade = reader->getTradeRow(i);
                            std::cout << "The position has been opened. TradeID='" << trade->getTradeID()
                                << "', TradeIDOrigin='" << trade->getTradeIDOrigin()<< "'" << std::endl;
                        }
                    }
                    break;
                    case ClosedTrades:
                    {
                        if (reader->getUpdateType(i) == Insert)
                        {
                            O2G2Ptr<IO2GClosedTradeRow> closedTrade = reader->getClosedTradeRow(i);
                            std::cout << "The position has been closed. TradeID='" << closedTrade->getTradeID()
                                << "'" << std::endl;
                        }
                    }
                    break;
                    case Messages:
                    {
                        if (reader->getUpdateType(i) == Insert)
                        {
                            O2G2Ptr<IO2GMessageRow> message = reader->getMessageRow(i);
                            std::string text(message->getText());
                            size_t findPos = text.find(mOrderID.c_str());
                            if (findPos != std::string::npos)
                            {
                                std::cout << "Feature='" << message->getFeature() << "', Message='"
                                    << text << "'" << std::endl;
                            }
                        }
                    }
                    break;
                    case Accounts:
                    {
                        if (reader->getUpdateType(i) == Update)
                        {
                            O2G2Ptr<IO2GAccountRow> account = reader->getAccountRow(i);
                            std::cout << "The balance has been changed. AccountID=" << account->getAccountID() << " Balance=" << std::fixed << account->getBalance() << std::endl;
                        }
                    }
                    break;
                    }
                }
            }
        }
    }
}

void ResponseListener::printOrder(const char *sCaption, IO2GOrderRow *orderRow)
{
    std::cout << sCaption << ": OrderID='" << orderRow->getOrderID() << "', "
        << "TradeID='" << orderRow->getTradeID() << "', "
        << "Status='" << orderRow->getStatus() << "', "
        << "Amount='" << orderRow->getAmount() << "', "
        << "OriginAmount='" << orderRow->getOriginAmount() << "', "
        << "FilledAmount='" << orderRow->getFilledAmount() << "'" << std::endl;
}

