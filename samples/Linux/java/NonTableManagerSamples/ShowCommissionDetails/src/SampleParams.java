package common;
import java.text.DateFormat;
import java.text.SimpleDateFormat;
import java.util.Calendar;

public class SampleParams {
    public static final String INSTRUMENT_NOT_SPECIFIED = "'Instrument' is not specified (/i|-i|/instrument|--instrument)";
    public static final String BUYSELL_NOT_SPECIFIED = "'BuySell' is not specified (/d|-d|/buysell|--buysell)";
    public static final String LOTS_NOT_SPECIFIED = "'Lots' is not specified (/l|-l|/lots|--lots)";
    public static final String ACCOUNT_NOT_SPECIFIED = "'Account' is not specified (/a|-a|/account|--account)";

    // Getters

    public String getInstrument() {
        return mInstrument;
    }
    private String mInstrument;

    public String getBuySell() {
        return mBuySell;
    }
    private String mBuySell;

    public int getLots() {
        return mLots;
    }
    private int mLots;

    public String getAccountID() {
        return mAccountID;
    }
    private String mAccountID;    

    // Setters

    public void setAccountID(String sAccountID) {
        mAccountID = sAccountID;
    }

    // ctor
    public SampleParams(String[] args) {
        // Get parameters with short keys
        mInstrument = getArgument(args, "i");
        mBuySell = getArgument(args, "d");
 
        // If parameters with short keys are not specified, get parameters with long keys
        if (mInstrument.isEmpty())
            mInstrument = getArgument(args, "instrument");
        if (mBuySell.isEmpty())
            mBuySell = getArgument(args, "buysell");

        String sLots = "";

        // Get parameters with long keys
        sLots = getArgument(args, "lots");
        mAccountID = getArgument(args, "account");

        // Convert types
        try {
            mLots = Integer.parseInt(sLots);
        } catch (Exception ex) {
            mLots = 1;
        }
    }

    private String getArgument(String[] args, String sKey) {
        for (int i = 0; i < args.length; i++) {
            int iDelimOffset = 0;
            if (args[i].startsWith("--")) {
                iDelimOffset = 2;
            } else if (args[i].startsWith("-") || args[i].startsWith("/")) {
                iDelimOffset = 1;
            }

            if (args[i].substring(iDelimOffset).equals(sKey) && (args.length > i+1)) {
                return args[i+1];
            }
        }
        return "";
    }
}
