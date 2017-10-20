#pragma once

/** Each row listener class. */

class EachRowListener : public IO2GEachRowListener
{
 public:
    EachRowListener(const char *);

    /** Increase reference counter. */
    virtual long addRef();

    /** Decrease reference counter. */
    virtual long release();

    virtual void onEachRow(const char *, IO2GRow *);

 private:
    long mRefCount;

    const char *mAccountID;

 protected:
    /** Destructor. */
    virtual ~EachRowListener();

};

