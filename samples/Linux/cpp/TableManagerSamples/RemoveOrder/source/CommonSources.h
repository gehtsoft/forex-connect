#pragma once

class SessionStatusListener;
class LoginParams;

bool login(IO2GSession *session, SessionStatusListener *statusListener, LoginParams *loginParams);
void logout(IO2GSession *session, SessionStatusListener *statusListener);
IO2GOfferTableRow *getOffer(IO2GTableManager *tableManager, const char *sInstrument);
IO2GAccountTableRow *getAccount(IO2GTableManager *tableManager, const char *sAccountID);
void formatDate(DATE date, char *buf);
bool isNaN(double value);
