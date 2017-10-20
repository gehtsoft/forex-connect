using System;
using System.Globalization;

namespace ArgParser
{
    public class SampleParams
    {
        public static readonly String INSTRUMENT_HELP_STRING = "/instrument | --instrument | /i | -i\nAn instrument which you want to use in sample. For example, \"EUR/USD\".\n";
        public static readonly String ACCOUNT_HELP_STRING = "/account | --account\nAn account which you want to use in sample. Optional parameter\n";
        public static readonly String LOTS_HELP_STRING = "lots | --lots\nTrade amount in lots. Optional parameter.\n";
        public static readonly String BUYSELL_HELP_STRING = "/buysell | --buysell | /d | -d\nThe order direction. Possible values are: B - buy, S - sell.\n";
        public static readonly String PRIMARYID_HELP_STRING = "/primaryid | --primaryid\nFirst order, which you want to add to a new contingency group\n";
        public static readonly String SECONDARYID_HELP_STRING = "/secondaryid | --secondaryid\nSecond order, which you want to add to a new contingency group.\n";
        public static readonly String RATE_HELP_STRING = "/rate | --rate | /r | -r\nDesired price of an entry order.\n";
        public static readonly String TIMEFRAME_HELP_STRING = "/timeframe | --timeframe\nTime period which forms a single candle. For example, m1 - for 1 minute, H1 - for 1 hour.\n";
        public static readonly String DATEFROM_HELP_STRING = "/datefrom | --datefrom\nDate/time from which you want to receive historical prices. If you leave this argument as it is, it will mean from last trading day. Format is \"m.d.Y H:M:S\". Optional parameter.\n";
        public static readonly String DATETO_HELP_STRING = "/dateto | --dateto\nDatetime until which you want to receive historical prices. If you leave this argument as it is, it will mean to now. Format is \"m.d.Y H:M:S\". Optional parameter.\n";
        public static readonly String RATESTOP_HELP_STRING = "/ratestop | --ratestop\nRate of the net stop order.\n";
        public static readonly String RATELIMIT_HELP_STRING = "/ratelimit | --ratelimit\nRate of the net limit order.\n";
        public static readonly String ORDERID_HELP_STRING = "/orderid | --orderid\nOrder, which you want to remove from existing contingency group.\n";
        public static readonly String CONTINGENCYID_HELP_STRING = "/contingencyid | --contingencyid\nContingency ID of group, to which you want to join an entry order. Mandatory argument.\n";
        public static readonly String STATUS_HELP_STRING = "/status | --status\nDesired subscription status of the instrument. Possible values: T, D, V.\n";
        public static readonly String ORDERTYPE_HELP_STRING = "/ordertype | --ordertype\nType of an entry order. Optional argument. Possible values are: LE - entry limit, SE - entry stop.Default value is LE.\n";
        public static readonly String EXPIREDATA_HELP_STRING = "/expiredata | --expiredata\nThe date and time up to which the order should stay live. Optional argument.\n";

        public static readonly String INSTRUMENT_NOT_SPECIFIED = "'Instrument' is not specified (/i|-i|/instrument|--instrument)\n";
        public static readonly String BUYSELL_NOT_SPECIFIED = "'BuySell' is not specified (/d|-d|/buysell|--buysell)\n";
        public static readonly String RATE_NOT_SPECIFIED = "'Rate' is not specified (/r|-r|/rate|--rate)\n";
        public static readonly String RATESTOP_NOT_SPECIFIED = "'RateStop' is not specified (/ratestop|--ratestop)\n";
        public static readonly String RATELIMIT_NOT_SPECIFIED = "'RateLimit' is not specified (/ratelimit|--ratelimit)\n";
        public static readonly String ORDERID_NOT_SPECIFIED = "'OrderID' is not specified (/orderid|--orderid)\n";
        public static readonly String PRIMARYID_NOT_SPECIFIED = "'PrimaryID' is not specified (/primaryid|--primaryid)\n";
        public static readonly String SECONDARYID_NOT_SPECIFIED = "'SecondaryID' is not specified (/secondaryid|--secondaryid)\n";
        public static readonly String CONTINGENCYID_NOT_SPECIFIED = "'ContingencyID' is not specified (/contingencyid|--contingencyid)\n";
        public static readonly String TIMEFRAME_NOT_SPECIFIED = "'Timeframe' is not specified (/timeframe|--timeframe)\n";
        public static readonly String STATUS_NOT_SPECIFIED = "'SubscriptionStatus' is not specified (/status|--status)\n";

        public String Instrument
        {
            get
            {
                return mInstrument;
            }
        }
        private String mInstrument;

        public String BuySell
        {
            get
            {
                return mBuySell;
            }
        }
        private String mBuySell;

        public String ContingencyID
        {
            get
            {
                return mContingencyID;
            }
        }
        private String mContingencyID;

        public String OrderID
        {
            get
            {
                return mOrderID;
            }
        }
        private String mOrderID;

        public String PrimaryID
        {
            get
            {
                return mPrimaryID;
            }
        }
        private String mPrimaryID;

        public String SecondaryID
        {
            get
            {
                return mSecondaryID;
            }
        }
        private String mSecondaryID;

        public String Timeframe
        {
            get
            {
                return mTimeframe;
            }
        }
        private String mTimeframe;

        public int Lots
        {
            get
            {
                return mLots;
            }
        }
        private int mLots;

        public String AccountID
        {
            get
            {
                return mAccountID;
            }
            set
            {
                mAccountID = value;
            }
        }
        private String mAccountID;

        public String OrderType
        {
            get
            {
                return mOrderType;
            }
            set
            {
                mOrderType = value;
            }
        }
        private String mOrderType;

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

        public double Rate
        {
            get
            {
                return mRate;
            }
        }
        private double mRate;

        public double RateStop
        {
            get
            {
                return mRateStop;
            }
        }
        private double mRateStop;

        public double RateLimit
        {
            get
            {
                return mRateLimit;
            }
        }
        private double mRateLimit;

        public String Status
        {
            get
            {
                return mStatus;
            }
        }
        private String mStatus;

        public String ExpireDate
        {
            get
            {
                return mExpireDate;
            }
        }
        private String mExpireDate;
        // Setters


        // ctor
        public SampleParams(String[] args)
        {
            // Get parameters with short keys
            mInstrument = GetArgument(args, "i");
            mBuySell = GetArgument(args, "d");
            String sRate = GetArgument(args, "r");

            // If parameters with short keys are not specified, get parameters with long keys
            if (string.IsNullOrEmpty(mInstrument))
                mInstrument = GetArgument(args, "instrument");
            if (string.IsNullOrEmpty(mBuySell))
                mBuySell = GetArgument(args, "buysell");
            if (string.IsNullOrEmpty(sRate))
                sRate = GetArgument(args, "rate");

            String sLots = "";
            String sRateStop = "";
            String sRateLimit = "";

            // Get parameters with long keys
            mContingencyID = GetArgument(args, "contingencyid");
            mOrderID = GetArgument(args, "orderid");
            mPrimaryID = GetArgument(args, "primaryid");
            mSecondaryID = GetArgument(args, "secondaryid");
            mTimeframe = GetArgument(args, "timeframe");
            sLots = GetArgument(args, "lots");
            mAccountID = GetArgument(args, "account");
            mOrderType = GetArgument(args, "ordertype");
            sRateStop = GetArgument(args, "ratestop");
            sRateLimit = GetArgument(args, "ratelimit");
            mStatus = GetArgument(args, "status");
            mExpireDate = GetArgument(args, "expiredate");

            // Convert types

            ConvertDates(args);

            try
            {
                mLots = int.Parse(sLots);
            }
            catch (Exception ex)
            {
                mLots = 1;
            }
            try
            {
                bool bRes = Double.TryParse(sRate, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out mRate);
                if (!bRes)
                    mRate = Double.NaN;
            }
            catch (Exception ex)
            {
                mRate = Double.NaN;
            }
            try
            {
                bool bRes = Double.TryParse(sRateStop, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out mRateStop);
                if (!bRes)
                    mRateStop = Double.NaN;
            }
            catch (Exception ex)
            {
                mRateStop = Double.NaN;
            }
            try
            {
                bool bRes = Double.TryParse(sRateLimit, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out mRateLimit);
                if (!bRes)
                    mRateLimit = Double.NaN;
            }
            catch (Exception ex)
            {
                mRateLimit = Double.NaN;
            }
            
            if (string.IsNullOrEmpty(mOrderType))
                mOrderType = "LE";
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
                mDateFrom = fxcore2.DateTimeExtension.FromOADate(0); // ZERODATE
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
                mDateTo = fxcore2.DateTimeExtension.FromOADate(0); // ZERODATE
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