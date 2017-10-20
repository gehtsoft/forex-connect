using System;
using System.Collections.Generic;
using System.Text;

namespace ArgParser
{
    public enum ParserArgument
    {
        Login,
        Password,
        Url,
        Connection,
        Instrument,
        AccountID,
        SessionID,
        Pin,
        BuySell,
        Rate,
        RateStop,
        RateLimit,
        OrderID,
        PrimaryID,
        SecondaryID,
        ContingencyID,
        TimeFrame,
        Status,
        OrderType, 
        Lots,
        DateFrom,
        DateTo,
        ExpireData,
        Login2,
        Password2,
        SessionID2,
        Pin2,
        AccountID2,
        Url2,
        Connection2
    }

    class ArgumentParser
    {
        private List<ParserArgument> mExpectedParams;
        private String[] mArgs;

        private LoginParams mLoginParams;
        private SampleParams mSampleParams;

        private string mHelpString;
        private String mAppName;

        public LoginParams @LoginParams
        {
            get
            {
                return mLoginParams;
            }
        }

        public SampleParams @SampleParams
        {
            get
            {
                return mSampleParams;
            }
        }

        public bool AreArgumentsValid { get; private set; }

        public ArgumentParser(String[] args, String appName)
        {
            mArgs = args;
            mExpectedParams = new List<ParserArgument>();
            mAppName = appName;
        }

        public void AddArguments(params ParserArgument[] parserArgs)
        {
            foreach (ParserArgument param in parserArgs)
                mExpectedParams.Add(param);
        }
     
        public void ParseArguments()
        {
            String errorMessage = "";

            mLoginParams = new LoginParams(mArgs);
            mSampleParams = new SampleParams(mArgs);

            AreArgumentsValid = CheckParams(ref errorMessage);

            if (!AreArgumentsValid)
                Console.WriteLine(errorMessage);
        }


        public void PrintArguments()
        {
            StringBuilder sb = new StringBuilder();

            foreach (ParserArgument param in mExpectedParams)
            {
                switch (param)
                {
                    case ParserArgument.Login:
                        if (!string.IsNullOrEmpty(mLoginParams.Login))
                            sb.Append("Login=" + mLoginParams.Login + ", ");
                        break;
                    case ParserArgument.Login2:
                        if (!string.IsNullOrEmpty(mLoginParams.Login2))
                            sb.Append("Login2=" + mLoginParams.Login2 + ", ");
                        break;
                    case ParserArgument.Password:
                        if (!string.IsNullOrEmpty(mLoginParams.Password))
                            sb.Append("Password=*, ");
                        break;
                    case ParserArgument.Password2:
                        if (!string.IsNullOrEmpty(mLoginParams.Password2))
                            sb.Append("Password2=*, ");
                        break;
                    case ParserArgument.Url:
                        if (!string.IsNullOrEmpty(mLoginParams.URL))
                            sb.Append("URL=" + mLoginParams.URL + ", ");
                        break;
                    case ParserArgument.Connection:
                        if (!string.IsNullOrEmpty(mLoginParams.Connection))
                            sb.Append("Connection=" + mLoginParams.Connection + ", ");
                        break;
                    case ParserArgument.Url2:
                        if (!string.IsNullOrEmpty(mLoginParams.URL2))
                            sb.Append("URL2=" + mLoginParams.URL2 + ", ");
                        break;
                    case ParserArgument.Connection2:
                        if (!string.IsNullOrEmpty(mLoginParams.Connection2))
                            sb.Append("Connection2=" + mLoginParams.Connection2 + ", ");
                        break;
                    case ParserArgument.AccountID:
                        if (!string.IsNullOrEmpty(mSampleParams.AccountID))
                            sb.Append("AccountID=" + mSampleParams.AccountID + ", ");
                        break;
                    case ParserArgument.AccountID2:
                        if (!string.IsNullOrEmpty(mSampleParams.AccountID2))
                            sb.Append("AccountID2=" + mSampleParams.AccountID2 + ", ");
                        break;
                    case ParserArgument.Instrument:
                        if (!string.IsNullOrEmpty(mSampleParams.Instrument))
                            sb.Append("Instrument=" + mSampleParams.Instrument + ", ");
                        break;
                    case ParserArgument.BuySell:
                        if (!string.IsNullOrEmpty(mSampleParams.BuySell))
                            sb.Append("BuySell=" + mSampleParams.BuySell + ", ");
                        break;
                    case ParserArgument.OrderType:
                        if (!string.IsNullOrEmpty(mSampleParams.OrderType))
                            sb.Append("OrderType=" + mSampleParams.OrderType + ", ");
                        break;
                    case ParserArgument.Rate:
                        if (mSampleParams.Rate != Double.NaN)
                            sb.Append("Rate=" + mSampleParams.Rate + ", ");
                        break;
                    case ParserArgument.RateStop:
                        if (mSampleParams.RateStop != Double.NaN)
                            sb.Append("RateStop=" + mSampleParams.RateStop + ", ");
                        break;
                    case ParserArgument.RateLimit:
                        if (mSampleParams.RateLimit != Double.NaN)
                            sb.Append("RateLimit=" + mSampleParams.RateLimit + ", ");
                        break;
                    case ParserArgument.OrderID:
                        if (!string.IsNullOrEmpty(mSampleParams.OrderID))
                            sb.Append("OrderID=" + mSampleParams.OrderID + ", ");
                        break;
                    case ParserArgument.PrimaryID:
                        if (!string.IsNullOrEmpty(mSampleParams.PrimaryID))
                            sb.Append("PrimaryID=" + mSampleParams.PrimaryID + ", ");
                        break;
                    case ParserArgument.SecondaryID:
                        if (!string.IsNullOrEmpty(mSampleParams.SecondaryID))
                            sb.Append("SecondaryID=" + mSampleParams.SecondaryID + ", ");
                        break;
                    case ParserArgument.ContingencyID:
                        if (!string.IsNullOrEmpty(mSampleParams.ContingencyID))
                            sb.Append("ContingencyID=" + mSampleParams.ContingencyID + ", ");
                        break;
                    case ParserArgument.TimeFrame:
                        if (!string.IsNullOrEmpty(mSampleParams.Timeframe))
                            sb.Append("TimeFrame=" + mSampleParams.Timeframe + ", ");
                        break;
                    case ParserArgument.Status:
                        if (!string.IsNullOrEmpty(mSampleParams.Status))
                            sb.Append("Status=" + mSampleParams.Status + ", ");
                        break;
                    case ParserArgument.SessionID:
                        if (!string.IsNullOrEmpty(mLoginParams.SessionID))
                            sb.Append("SessionID=" + mLoginParams.SessionID + ", ");
                        break;
                    case ParserArgument.Pin:
                        if (!string.IsNullOrEmpty(mLoginParams.Pin))
                            sb.Append("Pin=*, ");
                        break;
                }
            }
            string result = sb.ToString();
            if (result.Length > 0)
                result = result.Substring(0, result.Length - 2);

            Console.WriteLine(result);
            Console.WriteLine();
        }

        public bool CheckParams(ref String errorString)
        {
            StringBuilder err = new StringBuilder();

            foreach (ParserArgument param in mExpectedParams)
            {
                switch (param)
                {
                    case ParserArgument.Login:
                        if (string.IsNullOrEmpty(mLoginParams.Login))
                        {
                            err.Append(LoginParams.LOGIN_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.Login2:
                        if (string.IsNullOrEmpty(mLoginParams.Login2))
                        {
                            err.Append(LoginParams.LOGIN2_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.Password:
                        if (string.IsNullOrEmpty(mLoginParams.Password))
                        {
                            err.Append(LoginParams.PASSWORD_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.Password2:
                        if (string.IsNullOrEmpty(mLoginParams.Password2))
                        {
                            err.Append(LoginParams.PASSWORD2_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.Url:
                        if (string.IsNullOrEmpty(mLoginParams.URL))
                        {
                            err.Append(LoginParams.URL_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.Connection:
                        if (string.IsNullOrEmpty(mLoginParams.Connection))
                        {
                            err.Append(LoginParams.CONNECTION_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.Url2:
                        if (string.IsNullOrEmpty(mLoginParams.URL2))
                        {
                            err.Append(LoginParams.URL2_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.Connection2:
                        if (string.IsNullOrEmpty(mLoginParams.Connection2))
                        {
                            err.Append(LoginParams.CONNECTION2_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.Instrument:
                        if (string.IsNullOrEmpty(mSampleParams.Instrument))
                        {
                            err.Append(SampleParams.INSTRUMENT_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.BuySell:
                        if (string.IsNullOrEmpty(mSampleParams.BuySell))
                        {
                            err.Append(SampleParams.BUYSELL_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.Rate:
                        if (Double.IsNaN(mSampleParams.Rate))
                        {
                            err.Append(SampleParams.RATE_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.RateStop:
                        if (Double.IsNaN(mSampleParams.RateStop))
                        {
                            err.Append(SampleParams.RATESTOP_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.RateLimit:
                        if (Double.IsNaN(mSampleParams.RateLimit))
                        {
                            err.Append(SampleParams.RATELIMIT_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.OrderID:
                        if (string.IsNullOrEmpty(mSampleParams.OrderID))
                        {
                            err.Append(SampleParams.ORDERID_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.PrimaryID:
                        if (string.IsNullOrEmpty(mSampleParams.PrimaryID))
                        {
                            err.Append(SampleParams.PRIMARYID_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.SecondaryID:
                        if (string.IsNullOrEmpty(mSampleParams.SecondaryID))
                        {
                            err.Append(SampleParams.SECONDARYID_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.ContingencyID:
                        if (string.IsNullOrEmpty(mSampleParams.ContingencyID))
                        {
                            err.Append(SampleParams.CONTINGENCYID_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.TimeFrame:
                        if (string.IsNullOrEmpty(mSampleParams.Timeframe))
                        {
                            err.Append(SampleParams.TIMEFRAME_NOT_SPECIFIED);
                        }
                        break;
                    case ParserArgument.Status:
                        if (string.IsNullOrEmpty(mSampleParams.Status))
                        {
                            err.Append(SampleParams.STATUS_NOT_SPECIFIED);
                        }
                        break;

                        // other parameters are optional
                }
            }

            errorString = err.ToString();
            bool isOK = errorString.Length == 0;
            if (!isOK) {
                errorString = "Invalid or missing commandline arguments!\n" + errorString;
            }

            return isOK;
        }

        public void PrintUsage()
        {
            if (string.IsNullOrEmpty(mHelpString))
                FillHelpString();

            Console.Write(mHelpString);
        }

        private void FillHelpString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(mAppName + " sample parameters:\n");

            foreach (ParserArgument param in mExpectedParams)
            {
                switch (param)
                {
                    case ParserArgument.Login:
                        sb.Append(LoginParams.LOGIN_HELP_STRING);
                        break;
                    case ParserArgument.Login2:
                        sb.Append(LoginParams.LOGIN2_HELP_STRING);
                        break;
                    case ParserArgument.Password:
                            sb.Append(LoginParams.PASSWORD_HELP_STRING);
                        break;
                    case ParserArgument.Password2:
                        sb.Append(LoginParams.PASSWORD2_HELP_STRING);
                        break;
                    case ParserArgument.Url:                  
                            sb.Append(LoginParams.URL_HELP_STRING);
                        break;
                    case ParserArgument.Connection:  
                            sb.Append(LoginParams.CONNECTION_HELP_STRING);
                        break;
                    case ParserArgument.Url2:
                        sb.Append(LoginParams.URL2_HELP_STRING);
                        break;
                    case ParserArgument.Connection2:
                        sb.Append(LoginParams.CONNECTION2_HELP_STRING);
                        break;
                    case ParserArgument.SessionID:
                        sb.Append(LoginParams.SESSIONID_HELP_STRING);
                        break;
                    case ParserArgument.SessionID2:
                        sb.Append(LoginParams.SESSIONID2_HELP_STRING);
                        break;
                    case ParserArgument.Pin:
                        sb.Append(LoginParams.PIN_HELP_STRING);
                        break;
                    case ParserArgument.Pin2:
                        sb.Append(LoginParams.PIN2_HELP_STRING);
                        break;
                    case ParserArgument.Instrument:
                            sb.Append(SampleParams.INSTRUMENT_HELP_STRING);
                        break;
                    case ParserArgument.AccountID:
                        sb.Append(SampleParams.ACCOUNT_HELP_STRING);
                        break;
                    case ParserArgument.AccountID2:
                        sb.Append(SampleParams.ACCOUNT2_HELP_STRING);
                        break;
                    case ParserArgument.BuySell:
                            sb.Append(SampleParams.BUYSELL_HELP_STRING);
                        break;
                    case ParserArgument.OrderType:
                        sb.Append(SampleParams.ORDERID_HELP_STRING);
                        break;
                    case ParserArgument.Rate:
                            sb.Append(SampleParams.RATE_HELP_STRING);
                        break;
                    case ParserArgument.RateStop:
                            sb.Append(SampleParams.RATESTOP_HELP_STRING);
                        break;
                    case ParserArgument.RateLimit:
                            sb.Append(SampleParams.RATELIMIT_HELP_STRING);
                        break;
                    case ParserArgument.OrderID:
                            sb.Append(SampleParams.ORDERID_HELP_STRING);
                        break;
                    case ParserArgument.PrimaryID:
                            sb.Append(SampleParams.PRIMARYID_HELP_STRING);
                        break;
                    case ParserArgument.SecondaryID:
                            sb.Append(SampleParams.SECONDARYID_HELP_STRING);
                        break;
                    case ParserArgument.ContingencyID:
                            sb.Append(SampleParams.CONTINGENCYID_HELP_STRING);
                        break;
                    case ParserArgument.TimeFrame:
                            sb.Append(SampleParams.TIMEFRAME_HELP_STRING);
                        break;
                    case ParserArgument.Status:
                            sb.Append(SampleParams.STATUS_HELP_STRING);
                        break;
                    case ParserArgument.DateFrom:
                        sb.Append(SampleParams.DATEFROM_HELP_STRING);
                        break;
                    case ParserArgument.DateTo:
                        sb.Append(SampleParams.DATETO_HELP_STRING);
                        break;
                    case ParserArgument.ExpireData:
                        sb.Append(SampleParams.EXPIREDATA_HELP_STRING);
                        break;
                }
            }
            mHelpString = sb.ToString();
        }
    }
}
