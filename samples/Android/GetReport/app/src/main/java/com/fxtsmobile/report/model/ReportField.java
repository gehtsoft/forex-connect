package com.fxtsmobile.report.model;

public class ReportField {

    private final String value;
    private final String description;

    public ReportField(String value, String description) {

        this.value = value;
        this.description = description;
    }

    public String getValue() {
        return value;
    }

    public String getDescription() {
        return description;
    }
}
