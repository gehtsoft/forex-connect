#pragma once

class Offer;
class OfferCollection;

class TableListener :
    public IO2GTableListener
{
public:
    TableListener();

    /** Increase reference counter. */
    virtual long addRef();

    /** Decrease reference counter. */
    virtual long release();

    void setInstrument(const char *sInstrument);

    void onStatusChanged(O2GTableStatus);
    void onAdded(const char *, IO2GRow *);
    void onChanged(const char *, IO2GRow *);
    void onDeleted(const char *, IO2GRow *);

    void subscribeEvents(IO2GTableManager *manager);
    void unsubscribeEvents(IO2GTableManager *manager);

    void printOffers(IO2GOffersTable *offersTable, const char *sInstrument);
    void printOffer(IO2GOfferTableRow *offerRow, const char *sInstrument);

private:
    long mRefCount;
    std::string mInstrument;
    OfferCollection *mOffers;

 protected:
    /** Destructor. */
    virtual ~TableListener();
};

