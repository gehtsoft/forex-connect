#pragma once

class CThreadSafeAddRefImpl
{
 public:
    CThreadSafeAddRefImpl()
    {
        m_dwRef = 1;
    }
    virtual ~CThreadSafeAddRefImpl(){};

    long internalAddRef()
    {
        return O2GAtomic::InterlockedInc(m_dwRef);
    }

    long internalRelease()
    {
        long lResult = O2GAtomic::InterlockedDec(m_dwRef);
        if (lResult == 0)
            delete this;
        return lResult;
    }
 private:
     volatile unsigned int m_dwRef;
};

template<typename T> class TThreadSafeAddRefImpl : public T
{
 public:
    TThreadSafeAddRefImpl()
    {
        m_dwRef = 1;
    }
    virtual ~TThreadSafeAddRefImpl(){};

    long addRef()
    {
        return O2GAtomic::InterlockedInc(m_dwRef);
    }
    long release()
    {
        long lResult = O2GAtomic::InterlockedDec(m_dwRef);
        if (lResult == 0)
            delete this;
        return lResult;
    }
 private:
     volatile unsigned int m_dwRef;
};
