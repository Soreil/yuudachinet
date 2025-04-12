namespace yuudachi.Moon;

internal record MoonPhaseNameWithEmoji(string Name, string Emoji)
{
    public static readonly MoonPhaseNameWithEmoji NewMoon = new("New Moon", "🌑");
    public static readonly MoonPhaseNameWithEmoji WaxingCrescent = new("Waxing Crescent", "🌒");
    public static readonly MoonPhaseNameWithEmoji FirstQuarter = new("First Quarter", "🌓");
    public static readonly MoonPhaseNameWithEmoji WaxingGibbous = new("Waxing Gibbous", "🌔");
    public static readonly MoonPhaseNameWithEmoji FullMoon = new("Full Moon", "🌕");
    public static readonly MoonPhaseNameWithEmoji WaningGibbous = new("Waning Gibbous", "🌖");
    public static readonly MoonPhaseNameWithEmoji LastQuarter = new("Last Quarter", "🌗");
    public static readonly MoonPhaseNameWithEmoji WaningCrescent = new("Waning Crescent", "🌘");
}

internal static class Moon
{
    private static readonly DateTime knownNewMoon = new(2019, 6, 3, 12, 1, 0, DateTimeKind.Local);
    private static readonly double daysPerMonth = 29.53058867;

    public static string MoonPhase()
    {
        var duration = DateTime.Now - knownNewMoon;
        var days = duration.Hours / 24.0;
        var elapsedLunarMonths = days / daysPerMonth;

        var fractionalPart = elapsedLunarMonths - Math.Truncate(elapsedLunarMonths);

        return fractionalPart switch
        {
            < 0.125 => MoonPhaseNameWithEmoji.NewMoon.Emoji,
            < 0.250 => MoonPhaseNameWithEmoji.WaxingCrescent.Emoji,
            < 0.375 => MoonPhaseNameWithEmoji.FirstQuarter.Emoji,
            < 0.500 => MoonPhaseNameWithEmoji.WaxingGibbous.Emoji,
            < 0.625 => MoonPhaseNameWithEmoji.FullMoon.Emoji,
            < 0.750 => MoonPhaseNameWithEmoji.WaningGibbous.Emoji,
            < 0.875 => MoonPhaseNameWithEmoji.LastQuarter.Emoji,
            _ => MoonPhaseNameWithEmoji.WaningCrescent.Emoji,
        };
    }
}
