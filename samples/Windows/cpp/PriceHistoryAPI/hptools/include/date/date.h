#pragma once

#ifndef WIN32
#   ifndef DATE

typedef double DATE;

#   endif
#   ifndef SYSTEMTIME_DEFINED

typedef struct _SYSTEMTIME {
    WORD wYear;
    WORD wMonth;
    WORD wDayOfWeek;
    WORD wDay;
    WORD wHour;
    WORD wMinute;
    WORD wSecond;
    WORD wMilliseconds;
} SYSTEMTIME, *PSYSTEMTIME, *LPSYSTEMTIME;

#       define SYSTEMTIME_DEFINED
#   endif
#   ifndef WCHAR

typedef wchar_t WCHAR;

#   endif
#   ifndef TIME_ZONE_INFORMATION

typedef struct _TIME_ZONE_INFORMATION {
    long Bias;
    WCHAR StandardName[ 32 ];
    SYSTEMTIME StandardDate;
    long StandardBias;
    WCHAR DaylightName[ 32 ];
    SYSTEMTIME DaylightDate;
    long DaylightBias;
} TIME_ZONE_INFORMATION, *PTIME_ZONE_INFORMATION, *LPTIME_ZONE_INFORMATION;

#       define TIME_ZONE_ID_INVALID  ((DWORD)0xFFFFFFFF)
#       define TIME_ZONE_ID_UNKNOWN  ((DWORD)0)
#       define TIME_ZONE_ID_STANDARD ((DWORD)1)
#       define TIME_ZONE_ID_DAYLIGHT ((DWORD)2)
#   endif
#else
#   include <windows.h>
#   include <oleauto.h>
#endif

namespace hptools
{
    namespace date
    {
        INT HP_TOOLS OleTimeToWindowsTime(double dt, SYSTEMTIME *st);
        INT HP_TOOLS WindowsTimeToOleTime(SYSTEMTIME *st, double *dt);
        INT HP_TOOLS OleTimeToCTime(double dt, struct tm *t);
        INT HP_TOOLS CTimeToOleTime(struct tm *t, double *dt);
        void HP_TOOLS CTimeToWindowsTime(const struct tm *t, SYSTEMTIME *st);
        void HP_TOOLS WindowsTimeToCTime(const SYSTEMTIME *st, struct tm *t);
        void HP_TOOLS GetLocalWindowsTime(SYSTEMTIME *st);
        void HP_TOOLS GetSystemWindowsTime(SYSTEMTIME *st);
        char HP_TOOLS *DateStringToCTime(const char *s, const char *format, struct tm *tm);
        BOOL HP_TOOLS TzSpecificLocalTimeToUTCTime(LPTIME_ZONE_INFORMATION lpTimeZoneInformation,
                                             LPSYSTEMTIME lpLocalTime,
                                             LPSYSTEMTIME lpUniversalTime);
        BOOL HP_TOOLS UTCTimeToTzSpecificLocalTime(LPTIME_ZONE_INFORMATION lpTimeZoneInformation,
                                             LPSYSTEMTIME lpUniversalTime,
                                             LPSYSTEMTIME lpLocalTime);
        DWORD HP_TOOLS GetTzInformation(LPTIME_ZONE_INFORMATION lpTimeZoneInformation, const char *szTimeZone = NULL);

        enum eTimeZone
        {
            Local,
            EST,
            UTC
        };

        DATE HP_TOOLS DateConvertTz(DATE dt, eTimeZone tzFrom, eTimeZone tzTo);
        DATE HP_TOOLS OneSecond();
        DATE HP_TOOLS DateNow();
    }
}

#ifndef WIN32
#   define VariantTimeToSystemTime(A,B) hptools::date::OleTimeToWindowsTime(A,B)
#   define SystemTimeToVariantTime(A,B) hptools::date::WindowsTimeToOleTime(A,B)
#   define GetLocalTime(A) hptools::date::GetLocalWindowsTime(A)
#   define GetSystemTime(A) hptools::date::GetSystemWindowsTime(A)
#   define TzSpecificLocalTimeToSystemTime(A,B,C) hptools::date::TzSpecificLocalTimeToUTCTime(A,B,C)
#   define SystemTimeToTzSpecificLocalTime(A,B,C) hptools::date::UTCTimeToTzSpecificLocalTime(A,B,C)
#   define GetTimeZoneInformation(A) hptools::date::GetTzInformation(A)
#else
#   define strptime(A,B,C) hptools::date::DateStringToCTime(A,B,C)
#endif
#define VariantTimeToUnixTime(A,B) hptools::date::OleTimeToCTime(A,B)
#define UnixTimeToVariantTime(A,B) hptools::date::CTimeToOleTime(A,B)
#define UnixTimeToSystemTime(A,B) hptools::date::CTimeToWindowsTime(A,B)
#define SystemTimeToUnixTime(A,B) hptools::date::WindowsTimeToCTime(A,B)
