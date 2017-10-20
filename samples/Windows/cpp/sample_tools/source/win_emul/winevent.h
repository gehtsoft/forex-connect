#pragma once
#include <set>
#include "hidden_class.h"
/* CLASS DECLARATION **********************************************************/
/**
  Administrates a std::set of CWinEventHandle objects.
  Must be possible to find a CWinEventHandle by name.
  (std::map not very helpful: There are CWinEventHandles without a name, and
  name is stored in CWinEventHandle).
*******************************************************************************/
class CWinEventHandle;
class CBaseHandle;

typedef std::set<CWinEventHandle*> TWinEventHandleSet;

class HIDDEN_CLASS CWinEventHandleSet
{
private:
  static TWinEventHandleSet *s_Set;
public:
  static void init();
  static void cleanup();
  static CBaseHandle* createEvent(bool manualReset, bool signaled, const wchar_t* name);
  static void closeHandle(CWinEventHandle* eventHandle);
  static HANDLE openEvent(const wchar_t* name);
};

