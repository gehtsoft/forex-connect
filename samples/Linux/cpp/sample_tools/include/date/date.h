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

namespace sample_tools
{
    namespace date
    {
        INT GSTOOL3 OleTimeToWindowsTime(double dt, SYSTEMTIME *st);
        INT GSTOOL3 WindowsTimeToOleTime(SYSTEMTIME *st, double *dt);
        INT GSTOOL3 OleTimeToCTime(double dt, struct tm *t);
        INT GSTOOL3 CTimeToOleTime(struct tm *t, double *dt);
        void GSTOOL3 CTimeToWindowsTime(const struct tm *t, SYSTEMTIME *st);
        void GSTOOL3 WindowsTimeToCTime(const SYSTEMTIME *st, struct tm *t);
        void GSTOOL3 GetLocalWindowsTime(SYSTEMTIME *st);
        void GSTOOL3 GetSystemWindowsTime(SYSTEMTIME *st);
        char GSTOOL3 *DateStringToCTime(const char *s, const char *format, struct tm *tm);
        BOOL GSTOOL3 TzSpecificLocalTimeToUTCTime(LPTIME_ZONE_INFORMATION lpTimeZoneInformation,
                                             LPSYSTEMTIME lpLocalTime,
                                             LPSYSTEMTIME lpUniversalTime);
        BOOL GSTOOL3 UTCTimeToTzSpecificLocalTime(LPTIME_ZONE_INFORMATION lpTimeZoneInformation,
                                             LPSYSTEMTIME lpUniversalTime,
                                             LPSYSTEMTIME lpLocalTime);
        DWORD GSTOOL3 GetTzInformation(LPTIME_ZONE_INFORMATION lpTimeZoneInformation, const char *szTimeZone = NULL);

        enum eTimeZone
        {
            Local,
            EST,
            UTC
        };

        DATE GSTOOL3 DateConvertTz(DATE dt, eTimeZone tzFrom, eTimeZone tzTo);
        DATE GSTOOL3 OneSecond();
        DATE GSTOOL3 DateNow();
    }
}

#ifndef WIN32
#   define VariantTimeToSystemTime(A,B) sample_tools::date::OleTimeToWindowsTime(A,B)
#   define SystemTimeToVariantTime(A,B) sample_tools::date::WindowsTimeToOleTime(A,B)
#   define GetLocalTime(A) sample_tools::date::GetLocalWindowsTime(A)
#   define GetSystemTime(A) sample_tools::date::GetSystemWindowsTime(A)
#   define TzSpecificLocalTimeToSystemTime(A,B,C) sample_tools::date::TzSpecificLocalTimeToUTCTime(A,B,C)
#   define SystemTimeToTzSpecificLocalTime(A,B,C) sample_tools::date::UTCTimeToTzSpecificLocalTime(A,B,C)
#   define GetTimeZoneInformation(A) sample_tools::date::GetTzInformation(A)
#else
#   define strptime(A,B,C) sample_tools::date::DateStringToCTime(A,B,C)
#endif
#define VariantTimeToUnixTime(A,B) sample_tools::date::OleTimeToCTime(A,B)
#define UnixTimeToVariantTime(A,B) sample_tools::date::CTimeToOleTime(A,B)
#define UnixTimeToSystemTime(A,B) sample_tools::date::CTimeToWindowsTime(A,B)
#define SystemTimeToUnixTime(A,B) sample_tools::date::WindowsTimeToCTime(A,B)
