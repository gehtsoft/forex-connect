#include "FileDownloader.h"

#ifdef WIN32
#include "urlmon.h"

/** Downloads file from URL to the file. */
void FileDownloader::download(const char *url, const char *fileName)
{
    URLDownloadToFile(NULL, url, fileName, 0, NULL);
}

#else
#include <sstream>
#include <stdlib.h>
/** Downloads file from URL to the file. */
void FileDownloader::download(const char *url, const char *fileName)
{
    std::ostringstream stream;
#if defined(__APPLE__) && defined(__MACH__)
    stream << "curl \"" << url << "\" -o \"" << fileName << '\"';
#else
    stream << "wget \"" << url << "\" -O \"" << fileName << '\"';
#endif
    system(stream.str().c_str());
}
#endif

