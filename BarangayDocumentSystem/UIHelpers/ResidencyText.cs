namespace BarangayDocumentSystem.UIHelpers;

public static class ResidencyText
{
    public static string FormatDuration(int months)
    {
        if (months < 1) return "Less than a month";

        int years = months / 12;
        int remainingMonths = months % 12;
        string yearText = years == 0 ? "" : $"{years} {(years == 1 ? "year" : "years")}";
        string monthText = remainingMonths == 0 ? "" : $"{remainingMonths} {(remainingMonths == 1 ? "month" : "months")}";
        return years > 0 && remainingMonths > 0 ? yearText + ", " + monthText : yearText + monthText;
    }
}
