// resVersionGSFrame.h
// Used by resource.rc
//

#include "winver.h"           // extract from windows header
// English

// Defines file type.
#define VER_FILETYPE VFT_DLL
// Define symbol for english version.
//(Required by versionLinkLngShared.h header file)
/* <Language specific definition>*/
#define _FX_VER_LANG 0x0409          //U.S. English
#define _FX_VER_LANG_CHARSET_ID 1252 //Multilingual
/* </Language specific definition>*/


#include "./versionGlobalGehtsoft.h"

/* <File version specific definition>*/
#define _FX_VER_FILE_VER_MAJOR  1
#define _FX_VER_FILE_VER_MINOR  3
#define _FX_VER_FILE_VER_3      1410
#define _FX_VER_FILE_VER_BUILD  1317
/* </File version specific definition>*/

//Dot(.) deparated version number.
#define _FX_VER_USER_FILEVER_STR VER_STR_HELPER4(_FX_VER_FILE_VER_MAJOR, \
                                                  _FX_VER_FILE_VER_MINOR, \
                                                  _FX_VER_FILE_VER_3, \
                                                  _FX_VER_FILE_VER_BUILD, \
                                                  VER_SEP_DOT   )

//Comma(,) deparated version number.
#define _FX_VER_USER_FILEVER_NUM VER_NUM_HELPER4(_FX_VER_FILE_VER_MAJOR, \
                                                  _FX_VER_FILE_VER_MINOR, \
                                                  _FX_VER_FILE_VER_3, \
                                                  _FX_VER_FILE_VER_BUILD)


#if defined(_DUALOS)
    #define _FX_VER_FILEDESCR "GS Sample Tools Library\0"
#elif defined(_UNICODE)
    #define _FX_VER_FILEDESCR "GS Sample Tools Library\0"
#else
    #define _FX_VER_FILEDESCR "GS Sample Tools Library\0"
#endif

#ifdef _DEBUG
    #define _FX_VER_INTERNAL_NAME "sample_tools.dll\0"
#else
    #define _FX_VER_INTERNAL_NAME "sample_tools.dll\0"

#endif

#define _FX_VER_ORIGINAL_NAME  _FX_VER_INTERNAL_NAME
#define _FX_VER_PROD_NAME      "GSSampleTools\0"



