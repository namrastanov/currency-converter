namespace CurrencyConverter.Application.Settings;

public class CacheSettings
{
    public const string SectionName = "CacheSettings";

    public int LatestRatesTtlMinutes { get; set; } = 30;
    public int CurrenciesTtlMinutes { get; set; } = 60;
    public int GapMergeThresholdDays { get; set; } = 5;
}
