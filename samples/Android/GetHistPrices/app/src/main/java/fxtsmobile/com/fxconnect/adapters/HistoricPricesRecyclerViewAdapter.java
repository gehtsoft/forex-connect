package fxtsmobile.com.fxconnect.adapters;

import android.content.Context;
import android.support.v7.widget.RecyclerView;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import java.text.SimpleDateFormat;
import java.util.Calendar;
import java.util.List;
import java.util.Locale;

import fxtsmobile.com.fxconnect.R;
import fxtsmobile.com.fxconnect.model.VolumeItem;

public class HistoricPricesRecyclerViewAdapter extends RecyclerView.Adapter<HistoricPricesRecyclerViewAdapter.ViewHolder> {
    private static final int HEADER_POSITION = 0;
    private static final String DATE_TIME_FORMAT = "MM/dd/yyyy HH:mm";

    private static final int TYPE_HEADER = 1;
    private static final int TYPE_ITEM = 2;

    private Context context;
    private List<VolumeItem> volumes;

    public HistoricPricesRecyclerViewAdapter(Context context, List<VolumeItem> volumes) {
        this.context = context;
        this.volumes = volumes;
    }

    @Override
    public ViewHolder onCreateViewHolder(ViewGroup parent, int viewType) {
        int layoutId = viewType == TYPE_HEADER
                ? R.layout.list_historic_volume_header
                : R.layout.list_historic_volume_item;

        View view = LayoutInflater.from(context).inflate(layoutId, parent, false);
        return new ViewHolder(view);
    }

    @Override
    public int getItemViewType(int position) {
        if (position == HEADER_POSITION) {
            return TYPE_HEADER;
        }

        return TYPE_ITEM;
    }

    @Override
    public void onBindViewHolder(ViewHolder holder, int position) {
        if (position == HEADER_POSITION) {
            return;
        }

        VolumeItem volumeItem = volumes.get(position - 1);

        holder.dateTextView.setText(getDate(volumeItem.getCalendar()));
        holder.volumeTextView.setText(Integer.toString(volumeItem.getVolume()));
    }

    private String getDate(Calendar calendar) {
        SimpleDateFormat timeFormat = new SimpleDateFormat(DATE_TIME_FORMAT, Locale.getDefault());
        return timeFormat.format(calendar.getTime());
    }

    @Override
    public int getItemCount() {
        // with header
        return volumes.size() + 1;
    }

    class ViewHolder extends RecyclerView.ViewHolder {
        TextView dateTextView;
        TextView volumeTextView;
        View viewSeparator;

        ViewHolder(View itemView) {
            super(itemView);

            dateTextView = itemView.findViewById(R.id.dateTextView);
            volumeTextView = itemView.findViewById(R.id.volumeTextView);
            viewSeparator = itemView.findViewById(R.id.viewSeparator);
        }
    }
}
