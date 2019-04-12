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
        SessionID,
        Pin,
        TimeFrame,
        DateFrom,
        DateTo,
        QuotesCount,
        OpenPriceCandlesMode
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
                    case ParserArgument.Password:
                        if (!string.IsNullOrEmpty(mLoginParams.Password))
                            sb.Append("Password=*, ");
                        break;
                    case ParserArgument.Url:
                        if (!string.IsNullOrEmpty(mLoginParams.URL))
                            sb.Append("URL=" + mLoginParams.URL + ", ");
                        break;
                    case ParserArgument.Connection:
                        if (!string.IsNullOrEmpty(mLoginParams.Connection))
                            sb.Append("Connection=" + mLoginParams.Connection + ", ");
                        break;
                    case ParserArgument.Instrument:
                        if (!string.IsNullOrEmpty(mSampleParams.Instrument))
                            sb.Append("Instrument=" + mSampleParams.Instrument + ", ");
                        break;
                    case ParserArgument.TimeFrame:
                        if (!string.IsNullOrEmpty(mSampleParams.Timeframe))
                            sb.Append("TimeFrame=" + mSampleParams.Timeframe + ", ");
                        break;
                    case ParserArgument.SessionID:
                        if (!string.IsNullOrEmpty(mLoginParams.SessionID))
                            sb.Append("SessionID=" + mLoginParams.SessionID + ", ");
                        break;
                    case ParserArgument.DateTo:
      			if (Candleworks.PriceHistoryMgr.Constants.ZERODATE == mSampleParams.DateTo)
			{
                            sb.Append("DateTo=\'\'" + ", ");
			}
                        else
 			{
			    sb.Append("DateTo=" + mSampleParams.DateTo.ToString("MM.dd.yyyy HH:mm:ss") + ", ");
			}
                        break;
                    case ParserArgument.DateFrom:
                       if (Candleworks.PriceHistoryMgr.Constants.ZERODATE == mSampleParams.DateFrom)
		       {
                            sb.Append("DateFrom=\'\'" + ", ");
		       }
                       else
 		       {
			    sb.Append("DateFrom=" + mSampleParams.DateFrom.ToString("MM.dd.yyyy HH:mm:ss") + ", ");
		       }
                       break;
                    case ParserArgument.OpenPriceCandlesMode:
                            sb.Append("OpenPriceCandlesMode=" + mSampleParams.OpenPriceCandlesMode.ToString("F") + ", ");
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
                    case ParserArgument.Password:
                        if (string.IsNullOrEmpty(mLoginParams.Password))
                        {
                            err.Append(LoginParams.PASSWORD_NOT_SPECIFIED);
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
                    case ParserArgument.Instrument:
                        if (string.IsNullOrEmpty(mSampleParams.Instrument))
                        {
                            err.Append(SampleParams.INSTRUMENT_NOT_SPECIFIED);
                        }
                        break;  
                    case ParserArgument.TimeFrame:
                        if (string.IsNullOrEmpty(mSampleParams.Timeframe))
                        {
                            err.Append(SampleParams.TIMEFRAME_NOT_SPECIFIED);
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
                    case ParserArgument.Password:
                            sb.Append(LoginParams.PASSWORD_HELP_STRING);
                        break;
                    case ParserArgument.Url:                  
                            sb.Append(LoginParams.URL_HELP_STRING);
                        break;
                    case ParserArgument.Connection:  
                            sb.Append(LoginParams.CONNECTION_HELP_STRING);
                        break;
                    case ParserArgument.SessionID:
                        sb.Append(LoginParams.SESSIONID_HELP_STRING);
                        break;
                    case ParserArgument.Pin:
                        sb.Append(LoginParams.PIN_HELP_STRING);
                        break;
                    case ParserArgument.Instrument:
                            sb.Append(SampleParams.INSTRUMENT_HELP_STRING);
                        break;
                    case ParserArgument.TimeFrame:
                            sb.Append(SampleParams.TIMEFRAME_HELP_STRING);
                        break;
                    case ParserArgument.DateFrom:
                        sb.Append(SampleParams.DATEFROM_HELP_STRING);
                        break;
                    case ParserArgument.DateTo:
                        sb.Append(SampleParams.DATETO_HELP_STRING);
                        break;
                    case ParserArgument.OpenPriceCandlesMode:
                        sb.Append(SampleParams.OPEN_PRICE_CANDLES_MODE_HELP_STRING);
                        break;
                    case ParserArgument.QuotesCount:
                        sb.Append(SampleParams.QUOTES_COUNT_HELP_STRING);
                        break;
                }
            }
            mHelpString = sb.ToString();
        }
    }
}
