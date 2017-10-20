package twoconnections;

import com.fxcore2.*;
import common.*;

public class Connection implements Runnable {
    
    O2GSession mSession;
    LoginParams mLoginParams;
    SampleParams mSampleParams;
    boolean mIsFirstAccount;
    
    public Connection(O2GSession session, LoginParams loginParams, SampleParams sampleParams, boolean bIsFirstAccount) {
        mSession = session;
        mLoginParams = loginParams;
        mSampleParams = sampleParams;
        mIsFirstAccount = bIsFirstAccount;
    }
    
    public void run() {
        try {
            String sLogin;
            String sPassword;
            String sSessionID;
            String sPin;
            if (mIsFirstAccount) {
                sLogin = mLoginParams.getLogin();
                sPassword = mLoginParams.getPassword();
                sSessionID = mLoginParams.getSessionID();
                sPin = mLoginParams.getPin();
            } else {
                sLogin = mLoginParams.getLogin2();
                sPassword = mLoginParams.getPassword2();
                sSessionID = mLoginParams.getSessionID2();
                sPin = mLoginParams.getPin2();
            }
            SessionStatusListener statusListener = new SessionStatusListener(mSession, sSessionID, sPin);
            mSession.subscribeSessionStatus(statusListener);
            statusListener.reset();
            mSession.login(sLogin, sPassword, mLoginParams.getURL(), mLoginParams.getConnection());
            if (statusListener.waitEvents() && statusListener.isConnected()) {
                if (!mIsFirstAccount) { // Disable receiving price updates for the second account
                    mSession.setPriceUpdateMode(O2GPriceUpdateMode.NO_PRICE);
                }
                ResponseListener responseListener = new ResponseListener(mSession);
                mSession.subscribeResponse(responseListener);
                O2GAccountRow account = null;
                if (mIsFirstAccount) {
                    boolean bIsAccountEmpty = mSampleParams.getAccountID().isEmpty();
                    account = getAccount(mSession, mSampleParams.getAccountID());
                    if (account != null) {
                        if (bIsAccountEmpty) {
                            mSampleParams.setAccountID(account.getAccountID());
                            System.out.println("Account: " + mSampleParams.getAccountID());
                        }
                    } else {
                        throw new Exception(String.format("The account '%s' is not valid",
                                mSampleParams.getAccountID()));
                    }
                } else {
                    boolean bIsAccountEmpty = mSampleParams.getAccountID2().isEmpty();
                    account = getAccount(mSession, mSampleParams.getAccountID2());
                    if (account != null) {
                        if (bIsAccountEmpty) {
                            mSampleParams.setAccountID2(account.getAccountID());
                            System.out.print("Account2: " + mSampleParams.getAccountID2());
                        }
                    } else {
                        throw new Exception(String.format("The account2 '%s' is not valid",
                                mSampleParams.getAccountID2()));
                    }
                }
                O2GOfferRow offer = getOffer(mSession, mSampleParams.getInstrument());
                if (offer == null) {
                    throw new Exception(String.format("The instrument '%s' is not valid",
                            mSampleParams.getInstrument()));
                }
                O2GLoginRules loginRules = mSession.getLoginRules();
                if (loginRules == null) {
                    throw new Exception("Cannot get login rules");
                }
                O2GTradingSettingsProvider tradingSettingsProvider = loginRules.getTradingSettingsProvider();
                int iBaseUnitSize = tradingSettingsProvider.getBaseUnitSize(mSampleParams.getInstrument(), account);
                int iAmount = iBaseUnitSize * mSampleParams.getLots();
                O2GRequest request;
                request = createTrueMarketOrderRequest(mSession, offer.getOfferID(), account.getAccountID(), iAmount, mSampleParams.getBuySell());
                if (request == null) {
                    throw new Exception("Cannot create request");
                }
                responseListener.setRequestID(request.getRequestId());
                mSession.sendRequest(request);
                if (!responseListener.waitEvents()) {
                    throw new Exception("Response waiting timeout expired");
                }
                System.out.println("Done!");
                statusListener.reset();
                mSession.logout();
                statusListener.waitEvents();
                mSession.unsubscribeResponse(responseListener);
            }
            mSession.unsubscribeSessionStatus(statusListener);
        } catch (Exception e) {
            System.out.println("Exception: " + e.toString());
        }
    }

    // Find valid account by ID or get the first valid account
    private O2GAccountRow getAccount(O2GSession session, String sAccountID) throws Exception {
        O2GAccountRow account = null;
        boolean bHasAccount = false;
        O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
        if (readerFactory == null) {
            throw new Exception("Cannot create response reader factory");
        }
        O2GLoginRules loginRules = session.getLoginRules();
        O2GResponse response = loginRules.getTableRefreshResponse(O2GTableType.ACCOUNTS);
        O2GAccountsTableResponseReader accountsResponseReader = readerFactory.createAccountsTableReader(response);
        for (int i = 0; i < accountsResponseReader.size(); i++) {
            account = accountsResponseReader.getRow(i);
            String sAccountKind = account.getAccountKind();
            if (sAccountKind.equals("32") || sAccountKind.equals("36")) {
                if (account.getMarginCallFlag().equals("N")) {
                    if (sAccountID.isEmpty() || sAccountID.equals(account.getAccountID())) {
                        bHasAccount = true;
                        break;
                    }
                }
            }
        }
        if (!bHasAccount) {
            return null;
        } else {
            return account;
        }
    }

    // Find valid offer by instrument name
    private O2GOfferRow getOffer(O2GSession session, String sInstrument) throws Exception {
        O2GOfferRow offer = null;
        boolean bHasOffer = false;
        O2GResponseReaderFactory readerFactory = session.getResponseReaderFactory();
        if (readerFactory == null) {
            throw new Exception("Cannot create response reader factory");
        }
        O2GLoginRules loginRules = session.getLoginRules();
        O2GResponse response = loginRules.getTableRefreshResponse(O2GTableType.OFFERS);
        O2GOffersTableResponseReader offersResponseReader = readerFactory.createOffersTableReader(response);
        for (int i = 0; i < offersResponseReader.size(); i++) {
            offer = offersResponseReader.getRow(i);
            if (offer.getInstrument().equals(sInstrument)) {
                if (offer.getSubscriptionStatus().equals("T")) {
                    bHasOffer = true;
                    break;
                }
            }
        }
        if (!bHasOffer) {
            return null;
        } else {
            return offer;
        }
    }

    // Create true market order request
    private static O2GRequest createTrueMarketOrderRequest(O2GSession session, String sOfferID, String sAccountID, int iAmount, String sBuySell) throws Exception {
        O2GRequest request = null;
        O2GRequestFactory requestFactory = session.getRequestFactory();
        if (requestFactory == null) {
            throw new Exception("Cannot create request factory");
        }
        O2GValueMap valuemap = requestFactory.createValueMap();
        valuemap.setString(O2GRequestParamsEnum.COMMAND, Constants.Commands.CreateOrder);
        valuemap.setString(O2GRequestParamsEnum.ORDER_TYPE, Constants.Orders.TrueMarketOpen);
        valuemap.setString(O2GRequestParamsEnum.ACCOUNT_ID, sAccountID);
        valuemap.setString(O2GRequestParamsEnum.OFFER_ID, sOfferID);
        valuemap.setString(O2GRequestParamsEnum.BUY_SELL, sBuySell);
        valuemap.setInt(O2GRequestParamsEnum.AMOUNT, iAmount);
        valuemap.setString(O2GRequestParamsEnum.CUSTOM_ID, "TrueMarketOrder");
        request = requestFactory.createOrderRequest(valuemap);
        if (request == null) {
            System.out.println(requestFactory.getLastError());
        }
        return request;
    }
}
