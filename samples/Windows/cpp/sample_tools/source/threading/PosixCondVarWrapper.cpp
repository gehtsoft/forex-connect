#include "stdafx.h"

#include <pthread.h>
#include <sample_tools.h>
#include "threading/PosixCondVarWrapper.h"

PosixCondVar::PosixCondVar()
    :  mIsSignaled(false)
{
    pthread_cond_init(&mCondVar, NULL);
    pthread_mutex_init(&mCondMutex, NULL);
}

PosixCondVar::~PosixCondVar()
{
    pthread_cond_destroy(&mCondVar);
    pthread_mutex_destroy(&mCondMutex);
}

pthread_cond_t &PosixCondVar::getCondVar()
{
    return mCondVar;
}

pthread_mutex_t &PosixCondVar::getMutex()
{
    return mCondMutex;
}
