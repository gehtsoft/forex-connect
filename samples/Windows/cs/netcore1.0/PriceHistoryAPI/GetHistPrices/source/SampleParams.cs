using Candleworks.QuotesMgr;
using System;
using System.Globalization;

namespace ArgParser
{
    public class SampleParams
    {
        public static readonly String INSTRUMENT_HELP_STRING = "/instrument | --instrument | /i | -i\nAn instrument which you want to use in sample. For example, \"EUR/USD\".\n";
        public static readonly String TIMEFRAME_HELP_STRING = "/timeframe | --timeframe\nTime period which forms a single candle. For example, m1 - for 1 minute, H1 - for 1 hour.\n";
        public static readonly String DATEFROM_HELP_STRING = "/datefrom | --datefrom\nDate/time from which you want to receive historical prices. If you leave this argument as it is, it will mean from last trading day. Format is \"m.d.Y H:M:S\". Optional parameter.\n";
        public static readonly String DATETO_HELP_STRING = "/dateto | --dateto\nDatetime until which you want to receive historical prices. If you leave this argument as it is, it will mean to now. Format is \"m.d.Y H:M:S\". Optional parameter.\n";
        public static readonly String OPEN_PRICE_CANDLES_MODE_HELP_STRING = "/candlesmode | --candlesmode\nThis argument is optional. If it's \"firsttick\" then the opening price of a period equals the first price update inside the period. If it's \"prevclose\" then the opening price of a period equals the prior period's close price.\n";
        public static readonly String QUOTES_COUNT_HELP_STRING = "/count | --count\nThe number of historical prices you want to receive. This argument is optional. If you leave this argument as is, a default value will be used or the argument will be ignored if dateto parameter is specified.\n";

        public static readonly String INSTRUMENT_NOT_SPECIFIED = "'Instrument' is not specified (/i|-i|/instrument|--instrument)\n";
        public static readonly String TIMEFRAME_NOT_SPECIFIED = "'Timeframe' is not specified (/timeframe|--timeframe)\n";
        
        public String Instrument
        {
            get
            {
                return mInstrument;
            }
        }
        private String mInstrument;

        public String Timeframe
        {
            get
            {
                return mTimeframe;
            }
        }
        private String mTimeframe;


        public DateTime DateFrom
        {
            get
            {
                return mDateFrom;
            }
            set
            {
                mDateFrom = value;
            }
        }
        private DateTime mDateFrom;

        public DateTime DateTo
        {
            get
            {
                return mDateTo;
            }
            set
            {
                mDateTo = value;
            }
        }
        private DateTime mDateTo;

        public OpenPriceCandlesMode OpenPriceCandlesMode
        {
            get
            {
                return mOpenPriceCandlesMode;
            }
        }
        private OpenPriceCandlesMode mOpenPriceCandlesMode;

        public int QuotesCount
        {
            get
            {
                return mQuotesCount;
            }
        }
        private int mQuotesCount;
        // Setters

        // ctor
        public SampleParams(String[] args)
        {
            // Get parameters with short keys
            mInstrument = GetArgument(args, "i");

            // If parameters with short keys are not specified, get parameters with long keys
            if (string.IsNullOrEmpty(mInstrument))
                mInstrument = GetArgument(args, "instrument");

            mTimeframe = GetArgument(args, "timeframe");

            string sOpenPriceCandlesMode = GetArgument(args, "candlesmode");
            mOpenPriceCandlesMode = sOpenPriceCandlesMode == "firsttick" ?
                OpenPriceCandlesMode.OpenPriceFirstTick : OpenPriceCandlesMode.OpenPricePrevClose;

            string sQuotesCount = GetArgument(args, "count");
            if (!Int32.TryParse(sQuotesCount, out mQuotesCount))
                mQuotesCount = -1;
            else if (mQuotesCount <= 0)
                mQuotesCount = -1;

            ConvertDates(args);
        }

        private void ConvertDates(String[] args)
        {
            String sDateFrom = GetArgument(args, "datefrom");
            String sDateTo = GetArgument(args, "dateto");
            string sDateFormat = "MM.dd.yyyy HH:mm:ss";

            bool bIsDateFromNotSpecified = false;
            if (!DateTime.TryParseExact(sDateFrom, sDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out mDateFrom))
            {
                bIsDateFromNotSpecified = true;
                mDateFrom = Candleworks.PriceHistoryMgr.Constants.ZERODATE;
            }
            else
            {
                if (DateTime.Compare(mDateFrom, DateTime.UtcNow) >= 0)
                {
                    throw new Exception(string.Format("\"DateFrom\" value {0} is invalid", sDateFrom));
                }
            }

            if (!DateTime.TryParseExact(sDateTo, sDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out mDateTo))
            {
                mDateTo = Candleworks.PriceHistoryMgr.Constants.ZERODATE;
            }
            else
            {
                if (!bIsDateFromNotSpecified && DateTime.Compare(mDateFrom, mDateTo) >= 0)
                {
                    throw new Exception(string.Format("\"DateTo\" value {0} is invalid", sDateTo));
                }
            }
        }

        private String GetArgument(String[] args, String sKey)
        {
            for (int i = 0; i < args.Length; i++)
            {
                int iDelimOffset = 0;
                if (args[i].StartsWith("--"))
                {
                    iDelimOffset = 2;
                }
                else if (args[i].StartsWith("-") || args[i].StartsWith("/"))
                {
                    iDelimOffset = 1;
                }

                if (args[i].Substring(iDelimOffset) == sKey && (args.Length > i + 1))
                {
                    return args[i + 1];
                }
            }
            return "";
        }
    }
}