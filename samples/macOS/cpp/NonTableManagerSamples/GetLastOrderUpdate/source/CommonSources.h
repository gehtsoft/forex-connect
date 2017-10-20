#pragma once

class SessionStatusListener;
class LoginParams;

bool login(IO2GSession *session, SessionStatusListener *statusListener, LoginParams *loginParams);
void logout(IO2GSession *session, SessionStatusListener *statusListener);
IO2GOfferRow *getOffer(IO2GSession *session, const char *sInstrument);
IO2GAccountRow *getAccount(IO2GSession *session, const char *sAccountID);
void formatDate(DATE date, char *buf);
bool isNaN(double value);
