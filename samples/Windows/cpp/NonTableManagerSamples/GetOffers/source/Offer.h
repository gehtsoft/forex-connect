#pragma once

/** Class to keep offers */
class Offer
{
 private:
    std::string mID;            // offer identifier
    std::string mInstrument;    // instrument
    int mPrecision;             // offer precision
    DATE mDate;                 // date and time of the offer change
    double mPipSize;            // offer pipsize
    double mBid;                // offer bid
    double mAsk;                // offer ask
 public:
    /** Constructor. */
    Offer(const char *id, const char *instrument, int precision, double pipsize, double date, double bid, double ask);

    /** Update bid. */
    void setBid(double bid);

    /** Update ask. */
    void setAsk(double ask);

    /** Update date. */
    void setDate(DATE date);

    /** Get id. */
    const char *getID() const;

    /** Get instrument. */
    const char *getInstrument() const;

    /** Get precision. */
    int getPrecision() const;

    /** Get pipsize. */
    double getPipSize() const;

    /** Get bid. */
    double getBid() const;

    /** Get ask. */
    double getAsk() const;

    /** Get date. */
    DATE getDate() const;
};

/** Collection of the offers. */
class OfferCollection
{
 private:
    std::vector<Offer *> mOffers;
    std::map<std::string, Offer *> mIndex;
    mutable sample_tools::Mutex m_Mutex;
 public:
    /** Constructor. */
    OfferCollection();
    /** Destructor. */
    virtual ~OfferCollection();
    /** Add offer to collection. */
    void addOffer(Offer *offer);
    /** Find offer by id. */
    Offer *findOffer(const char *id) const;
    /** Get number of offers. */
    int size() const;
    /** Get offer by index. */
    Offer *get(int index) const;
    /** Clear offer collection. */
    void clear();
};

