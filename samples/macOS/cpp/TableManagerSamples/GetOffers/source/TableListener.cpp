#include "stdafx.h"
#include "Offer.h"
#include "TableListener.h"

TableListener::TableListener()
{
    mRefCount = 1;
    mInstrument = "";
    mOffers = new OfferCollection();
    std::cout.precision(2);
}

TableListener::~TableListener(void)
{
    delete mOffers;
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

void TableListener::setInstrument(const char *sInstrument)
{
    mInstrument = sInstrument;
}

void TableListener::onAdded(const char *rowID, IO2GRow *row)
{
}

void TableListener::onChanged(const char *rowID, IO2GRow *row)
{
    if (row->getTableType() == Offers)
    {
        printOffer((IO2GOfferTableRow *)row, mInstrument.c_str());
    }
}

void TableListener::onDeleted(const char *rowID, IO2GRow *row)
{
}

void TableListener::onStatusChanged(O2GTableStatus status)
{
}

void TableListener::printOffers(IO2GOffersTable *offersTable, const char *sInstrument)
{
    IO2GOfferTableRow *offerRow = NULL;
    IO2GTableIterator iterator;
    while (offersTable->getNextRow(iterator, offerRow))
    {
        printOffer(offerRow, sInstrument);
        offerRow->release();
    }
}

void TableListener::printOffer(IO2GOfferTableRow *offerRow, const char *sInstrument)
{
    Offer *offer = mOffers->findOffer(offerRow->getOfferID());
    if (offer)
    {
        if (offerRow->isTimeValid() && offerRow->isBidValid() && offerRow->isAskValid())
        {
            offer->setDate(offerRow->getTime());
            offer->setBid(offerRow->getBid());
            offer->setAsk(offerRow->getAsk());
        }
    }
    else
    {
        offer = new Offer(offerRow->getOfferID(), offerRow->getInstrument(),
                 offerRow->getDigits(), offerRow->getPointSize(), offerRow->getTime(),
                 offerRow->getBid(), offerRow->getAsk());
        mOffers->addOffer(offer);
    }
    if (!sInstrument || strlen(sInstrument) == 0 || strcmp(offerRow->getInstrument(), sInstrument) == 0)
    {
        std::cout << offer->getID() << ", " << offer->getInstrument() << ", "
                << "Bid=" << std::fixed << offer->getBid() << ", "
                << "Ask=" << std::fixed << offer->getAsk() << std::endl;
    }
}

void TableListener::subscribeEvents(IO2GTableManager *manager)
{
    O2G2Ptr<IO2GOffersTable> offersTable = (IO2GOffersTable *)manager->getTable(Offers);

    offersTable->subscribeUpdate(Update, this);
}

void TableListener::unsubscribeEvents(IO2GTableManager *manager)
{
    O2G2Ptr<IO2GOffersTable> offersTable = (IO2GOffersTable *)manager->getTable(Offers);

    offersTable->unsubscribeUpdate(Update, this);
}
