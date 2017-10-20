#pragma once

class PosixCondVar
{
 public:
    PosixCondVar();
    ~PosixCondVar();

    pthread_cond_t &getCondVar();
    pthread_mutex_t &getMutex();

    volatile bool mIsSignaled;

 private:
    pthread_cond_t mCondVar;
    pthread_mutex_t mCondMutex;
};
