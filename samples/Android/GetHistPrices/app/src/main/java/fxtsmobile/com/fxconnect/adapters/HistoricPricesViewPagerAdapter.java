package fxtsmobile.com.fxconnect.adapters;

import android.content.Context;
import android.support.v4.app.Fragment;
import android.support.v4.app.FragmentManager;
import android.support.v4.app.FragmentStatePagerAdapter;

import fxtsmobile.com.fxconnect.R;
import fxtsmobile.com.fxconnect.fragments.HistoricPricesCandleChartFragment;
import fxtsmobile.com.fxconnect.fragments.HistoricPricesVolumeTableFragment;

public class HistoricPricesViewPagerAdapter extends FragmentStatePagerAdapter {
    private static final int FRAGMENTS_COUNT = 2;

    private Context context;

    public HistoricPricesViewPagerAdapter(FragmentManager fm, Context context) {
        super(fm);
        this.context = context;
    }

    @Override
    public Fragment getItem(int position) {
        switch (position) {
            case 0:
                return HistoricPricesCandleChartFragment.newInstance();

            case 1:
                return HistoricPricesVolumeTableFragment.newInstance();
        }

        throw new IllegalArgumentException("Fragment not found");
    }

    @Override
    public int getCount() {
        return FRAGMENTS_COUNT;
    }

    @Override
    public CharSequence getPageTitle(int position) {
        int titleId = 0;

        switch (position) {
            case 0:
                titleId = R.string.historic_prices_candle_chart_title;
                break;

            case 1:
                titleId = R.string.historic_prices_volume_table_title;
                break;
        }

        return titleId != 0
                   ? context.getString(titleId)
                   : "";
    }
}
