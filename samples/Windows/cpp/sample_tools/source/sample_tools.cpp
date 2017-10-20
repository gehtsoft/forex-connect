// sample_tools.cpp : Defines the entry point for the DLL application.
//

#include "stdafx.h"


#ifdef _MANAGED
#pragma managed(push, off)
#endif

#ifdef WIN32
#   define _dll_at_load
#   define _dll_at_unload
#else
#   define _dll_at_load __attribute__ ((constructor))
#   define _dll_at_unload __attribute__ ((destructor))
#endif

#ifndef WIN32
#include <win_emul/winEmul.h>
#include "win_emul/winevent.h"
void _dll_at_load GSTOOL3 GSTool3AtLoad()
{
    CWinEventHandleSet::init();
}
            
void _dll_at_unload GSTOOL3 GSTool3AtUnload()
{
    CWinEventHandleSet::cleanup();
}
#endif

#ifdef WIN32
BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    return TRUE;
}
#endif

#ifdef _MANAGED
#pragma managed(pop)
#endif

