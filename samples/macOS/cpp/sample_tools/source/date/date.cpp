#include "stdafx.h"
#include <cstdio>
#include <cstring>
#include <cmath>
#include <ctime>
#include "win_emul/winEmul.h"
#include "date/date.h"

#ifndef WIN32
#   include <fcntl.h>
#   include <sys/types.h>
#   include <sys/stat.h>
#   include <unistd.h>
#   include <stdlib.h>
#endif

static const long minDate = -657434L;
static const long maxDate = 2958465L;
static const double halfSecond = 1.0/172800.0;
static const long monthDays[13] = {0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365};

// Init EST time zone structure.
// @TODO: Load information from windows register/Olsen DB
static TIME_ZONE_INFORMATION g_tziEST = {300, {0}, {0, 11, 0, 1, 2, 0, 0, 0}, 0, {0}, {0, 3, 0, 2, 2, 0, 0, 0}, -60};
static TIME_ZONE_INFORMATION g_tziLocal = {0};
static bool g_bTZinitialized = false;

inline long BigEndianToLong(unsigned char *p)
{
    return (p[0]<<24)+(p[1]<<16)+(p[2]<<8)+p[3];
}

inline long BigEndianToNonNegativeLong(unsigned char *p)
{
    long lValue = BigEndianToLong(p);
    return (lValue < 0) ? 0 : lValue;
}

inline bool IsLeapYear(int year)
{
    return (year % 4 == 0 && (year % 100 != 0 || year % 400 == 0));
}

inline WORD GaussGetDayOfWeek(const LPSYSTEMTIME st)
{
    // Gauss formula
    int a = (14 - st->wMonth)/12; 
    int y = st->wYear + 4800 - a; 
    int m = st->wMonth + 12*a - 3;
    int d = st->wDay + (153*m + 2)/5 + 365*y + y/4 - y/100 + y/400 - 32045;
    return (d + 1) % 7;
}


void FillTransitionDate(SYSTEMTIME *st, const tm *tm1, const tm *tm2);
DATE GetDayFromTimeZoneInfo(SYSTEMTIME *lpTimeZone, int nYear);
bool IsDaylightTime(DATE dt, LPTIME_ZONE_INFORMATION lptzi, bool bUTC);

using namespace sample_tools;

INT date::OleTimeToWindowsTime(double dt, SYSTEMTIME *st)
{
    if (dt > maxDate || dt < minDate) // about year 100 to about 9999
        return FALSE;

    long nDays;             // Number of days since Dec. 30, 1899
    long nDaysAbsolute;     // Number of days since 1/1/0
    long nMinutesInDay;     // Minutes in day

    long n400Years;         // Number of 400 year increments since 1/1/0
    long n400Century;       // Century within 400 year block (0,1,2 or 3)
    long n4Years;           // Number of 4 year increments since 1/1/0
    long n4Day;             // Day within 4 year block
                                                    //  (0 is 1/1/yr1, 1460 is 12/31/yr4)
    long n4Yr;              // Year within 4 year block (0,1,2 or 3)
    bool bLeap4 = true;     // TRUE if 4 year block includes leap year

    double dblDate = dt; // tempory serial date
    // To prevent floor of number like x.999(9) to previous day value
    dblDate += 1e-9; // 5e-8 ~= 1 / (60 * 60 * 24 * 1000) / 10 , its lesser than 1ms

    // If a valid date, then this conversion should not overflow
    nDays = (long)dblDate;

    nDaysAbsolute = (long)dblDate + 693959L; // Add days from 1/1/0 to 12/30/1899

    dblDate = fabs(dblDate);
    long nMSecsInDay = ((long)((dblDate - floor(dblDate)) * 86400000) % 86400000) + 1;

    //Calculate the day of week (sun=1, mon=2...)
    //  -1 because 1/1/0 is Sat.  +1 because we want 1-based

    // Leap years every 4 yrs except centuries not multiples of 400.
    n400Years = (long)(nDaysAbsolute / 146097L);

    // Set nDaysAbsolute to day within 400-year block
    nDaysAbsolute %= 146097L;

    // -1 because first century has extra day
    n400Century = (long)((nDaysAbsolute - 1) / 36524L);

    // Non-leap century
    if (n400Century != 0)
    {
            // Set nDaysAbsolute to day within century
            nDaysAbsolute = (nDaysAbsolute - 1) % 36524L;

            // +1 because 1st 4 year increment has 1460 days
            n4Years = (long)((nDaysAbsolute + 1) / 1461L);

            if (n4Years != 0)
                    n4Day = (long)((nDaysAbsolute + 1) % 1461L);
            else
            {
                    bLeap4 = false;
                    n4Day = (long)nDaysAbsolute;
            }
    }
    else
    {
            // Leap century - not special case!
            n4Years = (long)(nDaysAbsolute / 1461L);
            n4Day = (long)(nDaysAbsolute % 1461L);
    }

    if (bLeap4)
    {
            // -1 because first year has 366 days
            n4Yr = (n4Day - 1) / 365;

            if (n4Yr != 0)
                    n4Day = (n4Day - 1) % 365;
    }
    else
    {
            n4Yr = n4Day / 365;
            n4Day %= 365;
    }

    // n4Day is now 0-based day of year. Save 1-based day of year, year number
    st->wYear = (unsigned short)(n400Years * 400 + n400Century * 100 + n4Years * 4 + n4Yr);

    // Handle leap year: before, on, and after Feb. 29.
    if (n4Yr == 0 && bLeap4)
    {
            // Leap Year
            if (n4Day == 59)
            {
                    /* Feb. 29 */
                    st->wMonth = 2;
                    st->wDay = 29;
                    goto DoTime;
            }

            // Pretend it's not a leap year for month/day comp.
            if (n4Day >= 60)
                    --n4Day;
    }

    // Make n4DaY a 1-based day of non-leap year and compute
    //  month/day for everything but Feb. 29.
    ++n4Day;

    // Month number always >= n/32, so save some loop time */
    for (st->wMonth = (unsigned short)(n4Day >> 5) + 1; n4Day > monthDays[st->wMonth]; st->wMonth++);

    st->wDay = (int)(n4Day - monthDays[st->wMonth - 1]);

DoTime:
    if (nMSecsInDay == 0)
        st->wHour = st->wMinute = st->wSecond = st->wMilliseconds = 0;
    else
    {

        st->wMilliseconds = (unsigned short)(nMSecsInDay % 1000L);
        if (st->wMilliseconds > 0)
            st->wMilliseconds--;
        long nSecsInDay = (long)nMSecsInDay / 1000;
        st->wSecond = (unsigned short)(nSecsInDay % 60L);
        nMinutesInDay = nSecsInDay / 60L;
        st->wMinute = (int)nMinutesInDay % 60;
        st->wHour = (int)nMinutesInDay / 60;
    }
    
    st->wDayOfWeek = GaussGetDayOfWeek(st);

    return TRUE;
}

INT date::WindowsTimeToOleTime(SYSTEMTIME *st, double *dt)
{
    //Validate year and month (ignore day of week and milliseconds)
    if (st->wYear > 9999 || st->wMonth < 1 || st->wMonth > 12)
        return FALSE;

    //Check for leap year and set the number of days in the month
    bool bLeapYear = ((st->wYear & 3) == 0) &&
                      ((st->wYear % 100) != 0 || (st->wYear % 400) == 0);

    int nDaysInMonth = monthDays[st->wMonth] - monthDays[st->wMonth-1] +
                       ((bLeapYear && st->wDay == 29 && st->wMonth == 2) ? 1 : 0);

    //Finish validating the date
    if (st->wDay < 1 || st->wDay > nDaysInMonth || st->wHour > 23 || st->wMinute > 59 || st->wSecond > 59)
        return 0;

    //Cache the date in days and time in fractional days
    long nDate;
    double dblTime;

    //It is a valid date; make Jan 1, 1AD be 1
    nDate = (st->wYear * 365L) + (st->wYear / 4) - (st->wYear / 100) + (st->wYear / 400) + (monthDays[st->wMonth-1] + st->wDay);

    //If leap year and it's before March, subtract 1:
    if (st->wMonth <= 2 && bLeapYear)
        --nDate;

    //Offset so that 12/30/1899 is 0
    nDate -= 693959L;

    // TCL 10 Sep 07 msec support
    dblTime = (((long)st->wHour * 3600L) +  // hrs in seconds
               ((long)st->wMinute * 60L) +  // mins in seconds
               ((long)st->wSecond)) / 86400. +
               ((long)st->wMilliseconds / 86400000.);


    *dt = (double) nDate + ((nDate >= 0) ? dblTime : -dblTime);

    return TRUE;
}

DATE date::OneSecond()
{
    return 2 * halfSecond;
}

DATE date::DateNow()
{
    DATE dt = 0.0;
    SYSTEMTIME st;
    GetLocalWindowsTime(&st);
    WindowsTimeToOleTime(&st, &dt);
    return dt;
}

INT date::OleTimeToCTime(double dt, tm *t)
{
    SYSTEMTIME st;
    int iRes = OleTimeToWindowsTime(dt, &st);
    if (iRes)
        WindowsTimeToCTime(&st, t);
    return iRes;
}

INT date::CTimeToOleTime(tm *t, double *dt)
{
    SYSTEMTIME st;
    CTimeToWindowsTime(t, &st);
    int iRes = WindowsTimeToOleTime(&st, dt);
    return iRes;
}

void date::CTimeToWindowsTime(const tm *t, SYSTEMTIME *st)
{
    st->wYear = t->tm_year + 1900;
    st->wMonth = t->tm_mon + 1;
    st->wDayOfWeek = t->tm_wday;
    st->wDay = t->tm_mday;
    st->wHour = t->tm_hour;
    st->wMinute = t->tm_min;
    st->wSecond = t->tm_sec;
    st->wMilliseconds = 0;
}

void date::WindowsTimeToCTime(const SYSTEMTIME *st, tm *t)
{
    t->tm_year = st->wYear - 1900;
    t->tm_mon = st->wMonth - 1;
    t->tm_wday = st->wDayOfWeek;
    t->tm_mday = st->wDay;
    t->tm_hour = st->wHour;
    t->tm_min = st->wMinute;
    t->tm_sec = st->wSecond;
    t->tm_isdst = 0;
    
    t->tm_yday = monthDays[t->tm_mon] + ((IsLeapYear(st->wYear) && (t->tm_mon > 1)) ? 1 : 0) + t->tm_mday;
}

void date::GetLocalWindowsTime(SYSTEMTIME *st)
{
    time_t t1 = ::time(NULL);
    struct tm *tm1;
    tm1 = localtime(&t1);
    CTimeToWindowsTime(tm1, st);
}

void date::GetSystemWindowsTime(SYSTEMTIME *st)
{
    time_t t1 = ::time(NULL);
    struct tm *tm1;
    tm1 = gmtime(&t1);
    CTimeToWindowsTime(tm1, st);
}

DATE GSTOOL3 date::DateConvertTz(DATE dt, eTimeZone tzFrom, eTimeZone tzTo)
{
    if (!g_bTZinitialized)
    {
        GetTimeZoneInformation(&g_tziLocal);
        g_bTZinitialized = true;
    }

    if (tzFrom == tzTo)
        return dt;

    if (tzFrom != UTC && tzTo != UTC)
    {
        return date::DateConvertTz(date::DateConvertTz(dt, tzFrom, UTC), UTC, tzTo);
    }

    SYSTEMTIME st1, st2;
    TIME_ZONE_INFORMATION *ptz;

    OleTimeToWindowsTime(dt, &st1);

    switch (tzTo == UTC ? tzFrom : tzTo)
    {
    case EST:
        ptz = &g_tziEST;
        break;
    case Local:
        ptz = &g_tziLocal;
        break;
    default:
        ptz = NULL;
        break;
    }

    if (tzTo == UTC)
        TzSpecificLocalTimeToSystemTime(ptz, &st1, &st2);
    else
        SystemTimeToTzSpecificLocalTime(ptz, &st1, &st2);

    WindowsTimeToOleTime(&st2, &dt);

    return dt;
}

BOOL date::TzSpecificLocalTimeToUTCTime(LPTIME_ZONE_INFORMATION lpTimeZoneInformation,
                                     LPSYSTEMTIME lpLocalTime,
                                     LPSYSTEMTIME lpUniversalTime)
{
    if (!lpTimeZoneInformation || !lpLocalTime || !lpUniversalTime)
        return FALSE;

    DATE dtLocal;
    DATE dtUTC;
    DATE dtBias = double(lpTimeZoneInformation->Bias) / double(24*60) ;

    if (WindowsTimeToOleTime(lpLocalTime, &dtLocal) == FALSE)
        return FALSE;

    if (IsDaylightTime(dtLocal, lpTimeZoneInformation, false))
        dtBias += double(lpTimeZoneInformation->DaylightBias) / double(24*60);
    else
        dtBias += double(lpTimeZoneInformation->StandardBias) / double(24*60);

    dtUTC = dtLocal + dtBias;
    OleTimeToWindowsTime(dtUTC, lpUniversalTime);
    return TRUE;
}

BOOL date::UTCTimeToTzSpecificLocalTime(LPTIME_ZONE_INFORMATION lpTimeZoneInformation,
                                     LPSYSTEMTIME lpUniversalTime,
                                     LPSYSTEMTIME lpLocalTime)
{
    if (!lpTimeZoneInformation || !lpUniversalTime || !lpLocalTime)
        return FALSE;

    DATE dtUTC;
    DATE dtLocal;
    DATE dtBias = double(lpTimeZoneInformation->Bias) / double(24*60) ;
    if (WindowsTimeToOleTime(lpUniversalTime, &dtUTC) == FALSE)
        return FALSE;

    if (IsDaylightTime(dtUTC, lpTimeZoneInformation, true))
        dtBias += double(lpTimeZoneInformation->DaylightBias) / double(24*60);
    else
        dtBias += double(lpTimeZoneInformation->StandardBias) / double(24*60);

    dtLocal = dtUTC - dtBias;
    OleTimeToWindowsTime(dtLocal, lpLocalTime);
    return TRUE;
}

DWORD date::GetTzInformation(LPTIME_ZONE_INFORMATION lpTimeZoneInformation, const char *szTimeZone)
{
#ifdef WIN32
    return GetTimeZoneInformation(lpTimeZoneInformation);
#else
    LPTIME_ZONE_INFORMATION ptz = lpTimeZoneInformation;
    if (ptz == NULL)
        return TIME_ZONE_ID_INVALID;

    unsigned char *tzdata;
    struct Zoneinfo
    {
        unsigned int timecnt;       /* ¹ of transition times */
        unsigned int typecnt;       /* ¹ of local time types */
        unsigned int charcnt;       /* ¹ of characters of time zone abbreviation strings */

        unsigned char *ptime;       // pointer to transition dates timestamps table
        unsigned char *ptype;       // pointer to transition type indexes table for timestamps
        unsigned char *ptt;         // pointer to transition types description table
        unsigned char *pzone;       // pointer to timezone abbreviation strings table

    }zoneInfo;

    char buf[150];
    time_t t1 = ::time(NULL);
    struct tm tm1, tm2, tm3, tm4;
    struct stat file_stat;
    const char* szTimeZoneFileName;

    ::localtime_r(&t1, &tm1);

    memset((void *)ptz, 0, sizeof(TIME_ZONE_INFORMATION));
    if (!szTimeZone)
        // By default, lets use local time settings
        ptz->Bias = -(tm1.tm_gmtoff)/60;
    ptz->Bias = -(tm1.tm_gmtoff)/60;

    // Read timezone from timezone file (man tzfile(5) for file format)
    if (!szTimeZone)
        szTimeZoneFileName = "/etc/localtime";
    else
    {
        strcpy(buf, "/usr/share/zoneinfo/");
        strncat(buf, szTimeZone, sizeof(buf) - strlen(buf) - 1);
        szTimeZoneFileName = buf;
    }
    if (stat(szTimeZoneFileName, &file_stat) == -1 || file_stat.st_size <= 0)
        return TIME_ZONE_ID_INVALID;

    int f = open(szTimeZoneFileName, O_RDONLY);
    if (f == -1)
        return TIME_ZONE_ID_INVALID;

    tzdata = (unsigned char *)malloc(file_stat.st_size);

    long int l = read(f, tzdata, file_stat.st_size);
    close(f);
    if (l == -1)
    {
        free(tzdata);
        return TIME_ZONE_ID_INVALID;
    }

    unsigned char *p;
    if (memcmp(tzdata, "TZif", 4))
        p = tzdata + 16; // there are files without signature sometimes in Darwin
    else
        p = tzdata + 4 + 1 + 15;

    // Parse tzfile header
    zoneInfo.timecnt = BigEndianToLong(p + 3 * 4);
    zoneInfo.typecnt = BigEndianToLong(p + 4 * 4);
    if (zoneInfo.typecnt == 0)
    {
        free(tzdata);
        return TIME_ZONE_ID_UNKNOWN;
    }
    zoneInfo.charcnt = BigEndianToLong(p + 5 * 4);
    zoneInfo.ptime = p + 6 * 4;
    zoneInfo.ptype = zoneInfo.ptime + zoneInfo.timecnt * 4;
    zoneInfo.ptt = zoneInfo.ptype + zoneInfo.timecnt;
    zoneInfo.pzone = zoneInfo.ptt + zoneInfo.typecnt * 6;

    if (zoneInfo.timecnt == 0)
    {
        if (zoneInfo.typecnt > 0)
            ptz->Bias = - BigEndianToLong(zoneInfo.ptt) / 60;
        free(tzdata);
        return TIME_ZONE_ID_UNKNOWN;
    }

    // To prevent OOB due to invalid file
    if (zoneInfo.pzone + zoneInfo.charcnt - tzdata > file_stat.st_size)
    {
        free(tzdata);
        return TIME_ZONE_ID_INVALID;
    }

    // Find transition dates range for the current time
    unsigned first;
    for (first = 0 ; first < zoneInfo.timecnt; first++)
        if (BigEndianToNonNegativeLong(zoneInfo.ptime + 4 * first) > t1)
            break;

    if (first != 0)
        first--;
    if (first >= zoneInfo.timecnt - 1)
    {
        // Lack of information to determine DST rules - or may be no DST in use at all
        if (szTimeZone)
        {
            // Non-local timezone, let's find the proper UTC bias
            ptz->Bias = - BigEndianToLong(zoneInfo.ptt + zoneInfo.ptype[zoneInfo.timecnt - 1] * 6) / 60;
        }
        free(tzdata);
        return TIME_ZONE_ID_UNKNOWN;
    }

    bool isDSTFirst = (bool)zoneInfo.ptt[zoneInfo.ptype[first] * 6 + 4];
    bool isDSTSecond = (bool)zoneInfo.ptt[zoneInfo.ptype[first + 1] * 6 + 4];

    if (isDSTFirst == isDSTSecond)
    {
        // Two similar DST transitions - invalid file?
        free(tzdata);
        return TIME_ZONE_ID_INVALID;
    }

    // Determite DST and non-DST transitions
    unsigned int tostd_index, todst_index, tostd_index2, todst_index2;
    if (isDSTFirst)
    {
        todst_index = first;
        tostd_index = first + 1;
    }
    else
    {
        tostd_index = first;
        todst_index = first + 1;
    }
    tostd_index2 = tostd_index + 2;
    if (tostd_index2 >= zoneInfo.timecnt)
        tostd_index2 = -1;
    todst_index2 = todst_index + 2;
    if (todst_index2 >= zoneInfo.timecnt)
        todst_index2 = -1;

    // Find UTC bias and DTC bias
    ptz->Bias = - BigEndianToLong(zoneInfo.ptt + zoneInfo.ptype[tostd_index] * 6) / 60;
    ptz->StandardBias = 0;
    ptz->DaylightBias = - (BigEndianToLong(zoneInfo.ptt + zoneInfo.ptype[todst_index] * 6) / 60 + ptz->Bias);

    // Convert all times from UTC to local, but before the shift (i.e. wall-times right BEFORE transition)
    t1 = (time_t)BigEndianToNonNegativeLong(zoneInfo.ptime + 4 * tostd_index);
    t1 -= (ptz->Bias + ptz->DaylightBias) * 60;
    ::gmtime_r(&t1, &tm1);
    t1 = (time_t)BigEndianToNonNegativeLong(zoneInfo.ptime + 4 * todst_index);
    t1 -= ptz->Bias * 60;
    ::gmtime_r(&t1, &tm2);
    if (tostd_index2 > 0)
    {
        t1 = (time_t)BigEndianToNonNegativeLong(zoneInfo.ptime + 4 * tostd_index2) - ptz->DaylightBias * 60;
        t1 -= (ptz->Bias + ptz->DaylightBias) * 60;
        ::gmtime_r(&t1, &tm3);
    }
    if (todst_index2 > 0)
    {
        t1 = (time_t)BigEndianToNonNegativeLong(zoneInfo.ptime + 4 * todst_index2) - ptz->DaylightBias * 60;
        t1 -= ptz->Bias * 60;
        ::gmtime_r(&t1, &tm4);
    }

    // Determine DST rules and fill transition dates in the Windows TIME_ZONE_INFORMATION format
    FillTransitionDate(&(ptz->StandardDate), &tm1, tostd_index2 > 0 ? &tm3 : NULL);
    FillTransitionDate(&(ptz->DaylightDate), &tm2, todst_index2 > 0 ? &tm4 : NULL);

    free(tzdata);

    /*printf("Time Zone: UTC shift: %ld min, DST: %d, Standard transition date: %hu/%hu/%hu %hu:%hu weekday %hu, \
           Daylight transition date: %hu/%hu/%hu %hu:%hu weekday %hu, DST Bias: %ld min\n",
           -(ptz->Bias), (ptz->StandardDate.wMonth == 0)?0:1, ptz->StandardDate.wDay, ptz->StandardDate.wMonth,
           ptz->StandardDate.wYear, ptz->StandardDate.wHour, ptz->StandardDate.wMinute, ptz->StandardDate.wDayOfWeek,
           ptz->DaylightDate.wDay, ptz->DaylightDate.wMonth, ptz->DaylightDate.wYear,
           ptz->DaylightDate.wHour, ptz->DaylightDate.wMinute, ptz->DaylightDate.wDayOfWeek,
           -(ptz->DaylightBias));*/

    if (tostd_index2 > 0)
        return TIME_ZONE_ID_DAYLIGHT;
    else
        return TIME_ZONE_ID_STANDARD;
#endif
}

void FillTransitionDate(SYSTEMTIME *st, const tm *tm1, const tm *tm2)
{
    const int naMonthDays[12] = {31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31};
    int nDays1, nWeekNum1, nDays2, nWeekNum2;
    bool bLastWeek1, bLastWeek2;

    st->wMinute = tm1->tm_min;
    st->wHour = tm1->tm_hour;
    st->wMonth =  tm1->tm_mon+1;
    if (!tm2 || tm1->tm_wday != tm2->tm_wday)
    {
        // Not weekday-based DST rules - use "fixed date" rule
        st->wDay =  tm1->tm_mday;
        st->wYear =  tm1->tm_year + 1900;
    }
    else
    {
        st->wDayOfWeek =  tm1->tm_wday;
        nDays1 = naMonthDays[tm1->tm_mon] + ((tm1->tm_mon == 1 && IsLeapYear(tm1->tm_year + 1900)) ? 1 : 0);
        bLastWeek1 = (nDays1 < tm1->tm_mday + 7);
        nDays2 = naMonthDays[tm2->tm_mon] + ((tm2->tm_mon == 1 && IsLeapYear(tm2->tm_year + 1900)) ? 1 : 0);
        bLastWeek2 = (nDays2 < tm2->tm_mday + 7);
        if (bLastWeek1 && bLastWeek2)
        {
            // "Last week with such weekday" rule
            st->wDay = 5;
            st->wYear = 0;
        }
        else
        {
            nWeekNum1 = (tm1->tm_mday - 1) / 7;
            nWeekNum2 = (tm2->tm_mday - 1) / 7;
            if (nWeekNum1 == nWeekNum2)
            {
                // "N-st week with such weekday" rule
                st->wDay = nWeekNum1 + 1;
                st->wYear = 0;
            }
        }
    }
}

bool IsDaylightTime(DATE dt, LPTIME_ZONE_INFORMATION lptzi, bool bUTC)
{
    // This time zone don't support daylight.
    if (lptzi->DaylightBias == 0 && lptzi->StandardBias == 0)
        return false;

    SYSTEMTIME st;
    sample_tools::date::OleTimeToWindowsTime(dt, &st);

    /// Tbd where year specified in lpTimeZone
    // Time to transition to daylight time
    DATE dtDaylight = GetDayFromTimeZoneInfo(&lptzi->DaylightDate, st.wYear);
    // Time to transition to standard time. Where we transition from daylight to standard.
    // Time stored in daylight time.
    DATE dtStandard = GetDayFromTimeZoneInfo(&lptzi->StandardDate, st.wYear);

    DATE dtLocalStandard, dtLocalDaylight;
    // If input parameter in utc time time zone.
    if (bUTC)
    {
        // Need convert to local time, because daylight in time zone info stored.
        // as local time.
        dtLocalStandard = dt - double(lptzi->Bias + lptzi->StandardBias) / double(24*60);
        dtLocalDaylight = dt - double(lptzi->Bias + lptzi->DaylightBias) / double(24*60);
    }
    else
    {
        // In local time no need difference compare for time.
        dtLocalStandard  = dt;
        dtLocalDaylight = dt;
    }

    //May 14, 2008 olgas fixed incorrect standard\daylight interval checking in case if the daylight date is later than standar date.
    bool bEqualDaylightBound = (fabs(dtLocalStandard - dtDaylight) < halfSecond);
    bool bEqualStandardBound = (fabs(dtLocalDaylight - dtStandard) < halfSecond);
    if (dtStandard < dtDaylight)
        return (((dtLocalStandard >= dtDaylight) || (dtLocalDaylight < dtStandard)) || bEqualDaylightBound) && !bEqualStandardBound;        
        
    // Day must contains in daylight interval.
    // But precision errors may occurs. Add check of daylight time less then half second.
    // And remove if time is placed in half second interval from standard time.
    return ( ( ((dtDaylight <= dtLocalStandard ) && (dtLocalDaylight < dtStandard)) || (fabs(dtDaylight - dtLocalStandard) < halfSecond))   &&
            !(fabs(dtLocalDaylight - dtStandard) < halfSecond));
}

void __SetMonth(LPSYSTEMTIME lpst, int iM)
{
    if (!iM)
    {
        lpst->wYear--;
        lpst->wMonth = 12;
    }
    else
    {
        if (iM < 0)
        {
            lpst->wYear += (iM / 12) - 1;
            lpst->wMonth = 12 + iM % 12;
        }
        else
        {
            if (iM > 12 )
            {
                lpst->wYear += iM / 12;
                lpst->wMonth = iM % 12;
                if (lpst->wMonth == 0)
                {
                    lpst->wMonth = 12;
                    lpst->wYear -= 1;
                }
            }
            else
              lpst->wMonth = (unsigned short)iM;
        }
    }
}

DATE GetDayFromTimeZoneInfo(SYSTEMTIME *lpTimeZone, int nYear)
{
    int iWeekOrder;
    int wWeekDay;
    wWeekDay = lpTimeZone->wDayOfWeek;
    iWeekOrder = lpTimeZone->wDay;
    int iHours = lpTimeZone->wHour;
    DATE dt;

    SYSTEMTIME st = {0};
    st.wYear = nYear;
    st.wMonth = lpTimeZone->wMonth;
    st.wDay = 1;

    //last specified week day in the month
    if (iWeekOrder == 5)
    {
        __SetMonth(&st, st.wMonth + 1);
        WORD wLastWeekDay = GaussGetDayOfWeek(&st);

        sample_tools::date::WindowsTimeToOleTime(&st, &dt);

        if (wLastWeekDay > wWeekDay)
        {
            return dt - (wLastWeekDay - wWeekDay) + double (iHours) / double(24);
        }
        // Return to  one week in this case.
        return dt + double(wLastWeekDay  - 7 + (wLastWeekDay - wWeekDay)) + double (iHours) / double(24);
    }

    WORD wFirstWeekDay = GaussGetDayOfWeek(&st);
    sample_tools::date::WindowsTimeToOleTime(&st, &dt);

    if (wFirstWeekDay <= wWeekDay)
        dt += (wWeekDay - wFirstWeekDay);
    else
        dt += 7 - (wFirstWeekDay - wWeekDay);
    return dt + double(iWeekOrder <= 0 ? 0 : iWeekOrder - 1) * 7 + double (iHours) / double(24);
}
