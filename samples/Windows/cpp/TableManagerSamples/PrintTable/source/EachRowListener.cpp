#include "stdafx.h"
#include "EachRowListener.h"
#include "CommonSources.h"

EachRowListener::EachRowListener(const char *sAccountID)
{
    mRefCount = 1;
    mAccountID = sAccountID;
    std::cout.precision(2);
}

EachRowListener::~EachRowListener()
{
}

/** Increase reference counter. */
long EachRowListener::addRef()
{
    return InterlockedIncrement(&mRefCount);
}

/** Decrease reference counter. */
long EachRowListener::release()
{
    long rc = InterlockedDecrement(&mRefCount);
    if (rc == 0)
        delete this;
    return rc;
}

void EachRowListener::onEachRow(const char *sRowID, IO2GRow *row)
{
    if (row->getTableType() == Orders)
    {
        IO2GOrderTableRow *order = dynamic_cast<IO2GOrderTableRow *>(row);
        if (!mAccountID || strlen(mAccountID) == 0 ||
                strcmp(mAccountID, order->getAccountID()) == 0)
        {
            int columnsCount = order->columns()->size();
            for (int i = 0; i < columnsCount; ++i)
            {
                const void* value = row->getCell(i);
                O2G2Ptr<IO2GTableColumn> column = row->columns()->get(i);
                switch (column->getType())
                {
                case IO2GTableColumn::Integer:
                {
                    int iValue = *(const int*)(value);
                    std::cout << column->getID() << "=" << iValue;
                }
                break;
                case IO2GTableColumn::Double:
                {
                    double dValue = *(const double*)(value);
                    std::cout << column->getID() << "=" << dValue;
                }
                break;
                case IO2GTableColumn::Boolean:
                {
                    bool bValue = *(const bool*)(value);
                    const char* value = bValue ? "True" : "False";
                    std::cout << column->getID() << "=" << value;
                }
                break;
                case IO2GTableColumn::Date:
                {
                    char sDate[20];
                    DATE date = *(DATE*)(value);
                    formatDate(date, sDate);
                    std::cout << column->getID() << "=" << sDate;
                }
                break;
                case IO2GTableColumn::String:
                {
                    const char* szValue = (const char*)value;
                    std::cout << column->getID() << "=" << szValue;
                }
                break;
                }
                std::cout << "; ";
            }
            std::cout << std::endl << std::endl;
        }
    }
}

