using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace Common
{
    class SampleParams
    {
        public string Instrument
        {
            get
            {
                return mInstrument;
            }
        }
        private string mInstrument;

        public string BuySell
        {
            get
            {
                return mBuySell;
            }
        }
        private string mBuySell;

        public int Lots
        {
            get
            {
                return mLots;
            }
        }
        private int mLots;

        public string AccountID
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
        private string mAccountID;

        public string AccountID2
        {
            get
            {
                return mAccountID2;
            }
            set
            {
                mAccountID2 = value;
            }
        }
        private string mAccountID2;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="args"></param>
        public SampleParams(NameValueCollection args)
        {
            mInstrument = GetRequiredArgument(args, "Instrument");
            mBuySell = GetRequiredArgument(args, "BuySell");
            string sLots = args["Lots"];
            if (string.IsNullOrEmpty(sLots))
            {
                sLots = "1"; // default
            }
            Int32.TryParse(sLots, out mLots);
            if (mLots <= 0)
            {
                throw new Exception(string.Format("\"Lots\" value {0} is invalid; please fix the value in the configuration file", sLots));
            }
            mAccountID = args["Account"];
            mAccountID2 = args["Account2"];
        }

        /// <summary>
        /// Get required argument from configuration file
        /// </summary>
        /// <param name="args">Configuration file key-value collection</param>
        /// <param name="sArgumentName">Argument name (key) from configuration file</param>
        /// <returns>Argument value</returns>
        private string GetRequiredArgument(NameValueCollection args, string sArgumentName)
        {
            string sArgument = args[sArgumentName];
            if (!string.IsNullOrEmpty(sArgument))
            {
                sArgument = sArgument.Trim();
            }
            if (string.IsNullOrEmpty(sArgument))
            {
                throw new Exception(string.Format("Please provide {0} in configuration file", sArgumentName));
            }
            return sArgument;
        }
    }
}
