#include "stdafx.h"
#include <string>
#include <vector>
#include <map>
#include "Offer.h"

/** Constructor. */
Offer::Offer(const char *id, const char *instrument, int precision, double pipsize, DATE date, double bid, double ask)
{
    mID = id;
    mInstrument = instrument;
    mPrecision = precision;
    mPipSize = pipsize;
    mDate = date;
    mBid = bid;
    mAsk = ask;
}

/** Update bid. */
void Offer::setBid(double bid)
{
    mBid = bid;
}

/** Update ask. */
void Offer::setAsk(double ask)
{
    mAsk = ask;
}

/** Update date. */
void Offer::setDate(DATE date)
{
    mDate = date;
}

/** Get id. */
const char *Offer::getID() const
{
    return mID.c_str();
}

/** Get instrument. */
const char *Offer::getInstrument() const
{
    return mInstrument.c_str();
}

/** Get precision. */
int Offer::getPrecision() const
{
    return mPrecision;
}

/** Get pipsize. */
double Offer::getPipSize() const
{
    return mPipSize;
}

/** Get bid. */
double Offer::getBid() const
{
    return mBid;
}

/** Get ask. */
double Offer::getAsk() const
{
    return mAsk;
}

/** Get date. */
DATE Offer::getDate() const
{
    return mDate;
}


/** Constructor. */
OfferCollection::OfferCollection()
{
}

/** Destructor. */
OfferCollection::~OfferCollection()
{
    clear();
}

/** Add offer to collection. */
void OfferCollection::addOffer(Offer *offer)
{
    sample_tools::Mutex::Lock lock(m_Mutex);
    std::map<std::string, Offer *>::const_iterator iter = mIndex.find(offer->getID());
    if (iter != mIndex.end())
    {
        for (int i = 0; i < (int)mOffers.size(); i++)
        {
            if (mOffers[i] == offer)
            {
                delete mOffers[i];
                mOffers.erase(mOffers.begin() + i);
                break;
            }
        }
    }
    mOffers.push_back(offer);
    mIndex[offer->getID()] = offer;
}

/** Find offer by id. */
Offer *OfferCollection::findOffer(const char *id) const
{
    sample_tools::Mutex::Lock lock(m_Mutex);
    std::map<std::string, Offer *>::const_iterator iter = mIndex.find(id);
    if (iter == mIndex.end())
        return 0;
    else
        return iter->second;
}

/** Get number of offers. */
int OfferCollection::size() const
{
    sample_tools::Mutex::Lock lock(m_Mutex);
    return (int)mOffers.size();
}

/** Get offer by index. */
Offer *OfferCollection::get(int index) const
{
    sample_tools::Mutex::Lock lock(m_Mutex);
    return mOffers[index];
}

void OfferCollection::clear()
{
    sample_tools::Mutex::Lock lock(m_Mutex);
    int i;
    for (i = 0; i < (int)mOffers.size(); ++i)
        delete mOffers[i];
    mOffers.clear();
    mIndex.clear();
}

