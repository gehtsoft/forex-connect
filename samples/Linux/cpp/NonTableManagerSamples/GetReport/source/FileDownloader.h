#pragma once

class FileDownloader
{
public:
    /** Downloads file from URL to the file. */
    static void download(const char *url, const char *fileName);
};

