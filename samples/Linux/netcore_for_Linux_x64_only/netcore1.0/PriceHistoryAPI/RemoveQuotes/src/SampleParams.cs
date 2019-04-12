using System;
using System.Globalization;

namespace ArgParser
{
    public class SampleParams
    {
        public static readonly String INSTRUMENT_HELP_STRING = "/instrument | --instrument | /i | -i\nAn instrument which you want to use in sample. For example, \"EUR/USD\".\n";
        public static readonly String YEAR_HELP_STRING = "/year | --year | /y | -y\nThe year for which you want to remove historical prices from the local storage.. For example, \"2018\".\n";
        public static readonly String INSTRUMENT_NOT_SPECIFIED = "'Instrument' is not specified (/i|-i|/instrument|--instrument)\n";

        public String Instrument
        {
            get
            {
                return mInstrument;
            }
        }
        private String mInstrument;

        public int Year
        {
            get
            {
                return mYear;
            }
        }
        private int mYear;


        // ctor
        public SampleParams(String[] args)
        {
            // Get parameters with short keys
            mInstrument = GetArgument(args, "i");
            String sYear = GetArgument(args, "y");
            // If parameters with short keys are not specified, get parameters with long keys
            if (string.IsNullOrEmpty(mInstrument))
                mInstrument = GetArgument(args, "instrument");
            if (string.IsNullOrEmpty(sYear))
                sYear = GetArgument(args, "year");

            try
            {
                bool bRes = int.TryParse(sYear, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out mYear);
                if (!bRes)
                    mYear = -1;
                else if (mYear <= 0)
                    mYear = -1;
            }
            catch (Exception)
            {
                mYear = -1;
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