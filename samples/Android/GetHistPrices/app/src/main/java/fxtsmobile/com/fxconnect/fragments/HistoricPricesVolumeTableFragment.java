package fxtsmobile.com.fxconnect.fragments;

import android.os.Bundle;
import android.support.annotation.Nullable;
import android.support.v4.app.Fragment;
import android.support.v7.widget.LinearLayoutManager;
import android.support.v7.widget.RecyclerView;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import java.util.List;

import fxtsmobile.com.fxconnect.R;
import fxtsmobile.com.fxconnect.adapters.HistoricPricesRecyclerViewAdapter;
import fxtsmobile.com.fxconnect.model.HistoricPricesRepository;
import fxtsmobile.com.fxconnect.model.VolumeItem;

public class HistoricPricesVolumeTableFragment extends Fragment {
    private RecyclerView volumeRecyclerView;

    public static HistoricPricesVolumeTableFragment newInstance() {
        return new HistoricPricesVolumeTableFragment();
    }

    @Nullable
    @Override
    public View onCreateView(LayoutInflater inflater, @Nullable ViewGroup container, @Nullable Bundle savedInstanceState) {
        View view = inflater.inflate(R.layout.fragment_historic_prices_volume_table, container, false);
        volumeRecyclerView = view.findViewById(R.id.volumeRecyclerView);
        setupVolume();
        return view;
    }

    private void setupVolume() {
        List<VolumeItem> tableData = HistoricPricesRepository.getInstance().getTableData();
        HistoricPricesRecyclerViewAdapter adapter = new HistoricPricesRecyclerViewAdapter(getContext(), tableData);
        volumeRecyclerView.setLayoutManager(new LinearLayoutManager(getContext()));
        volumeRecyclerView.setAdapter(adapter);
    }

}
