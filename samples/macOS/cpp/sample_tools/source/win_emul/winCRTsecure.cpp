#include "stdafx.h"
#include <sys/types.h>
#include <string.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <stdio.h>
#include <win_emul/winEmul.h>

errno_t fopen_s(FILE** pFile, const char *filename, const char *mode)
{
    if (!pFile || !filename || !mode)
    {
        errno = EINVAL;
        return EINVAL;
    }
    *pFile = fopen(filename, mode);
    if (!(*pFile))
    {
        errno = ENOENT;
        return ENOENT;
    }
    return 0;
}

errno_t _sopen_s(int* pfh, const char *filename, int oflag, int shflag, int pmode)
{
    if (!pfh || !filename)
    {
        errno = EINVAL;
        return EINVAL;
    }
    *pfh = open(filename, oflag, shflag);
    if (*pfh != -1)
        return 0;
    else
        return errno;
}

errno_t _strlwr_s(char * str, size_t numberOfElements)
{
    if (!str)
    {
        errno = EINVAL;
        return EINVAL;
    }
    if (numberOfElements < strlen(str))
    {
        errno = ERANGE;
        return ERANGE;
    }
    while(*str)
    {
        *str = tolower(*str);
        ++str;
    }
    return 0;
}

errno_t _strupr_s(char * str, size_t numberOfElements)
{
    if (!str)
    {
        errno = EINVAL;
        return EINVAL;
    }
    if (numberOfElements < strlen(str))
    {
        errno = ERANGE;
        return ERANGE;
    }
    while(*str)
    {
        *str = toupper(*str);
        ++str;
    }
}

errno_t freopen_s(FILE** pFile, const char *path, const char *mode, FILE *stream)
{
    *pFile = freopen(path, mode, stream);
    if (*pFile == NULL)
        return errno;
    return 0;
}

errno_t GSTOOL3 strcpy_s(char *strDest, size_t numberOfElements, const char *strSource)
{
    if (!strDest || !numberOfElements || !strSource)
    {
        errno = EINVAL;
        return EINVAL;
    }
    strcpy(strDest, strSource);
    return 0;
}

errno_t GSTOOL3 strncpy_s(char *strDest, size_t numberOfElements, const char *strSource, size_t count)
{
    if (!strDest || !numberOfElements || !strSource || numberOfElements < count)
    {
        errno = EINVAL;
        return EINVAL;
    }
    strncpy(strDest, strSource, count);
    return 0;
}

errno_t GSTOOL3 strcat_s(char *strDest, size_t numberOfElements, const char *strSource)
{
    if (!strDest || !numberOfElements || !strSource)
    {
        errno = EINVAL;
        return EINVAL;
    }
    strcat(strDest, strSource);
    return 0;
}

