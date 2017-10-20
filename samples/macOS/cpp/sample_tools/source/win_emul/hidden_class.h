/*
 * This header defines macro to hide class from shared library on Unixes
 */

#ifndef __HIDDEN_CLASS_H__
#   define __HIDDEN_CLASS_H__
#   ifdef WIN32
#       define HIDDEN_CLASS
#   else
#       define HIDDEN_CLASS __attribute__ ((visibility ("hidden")))
#   endif
#endif
