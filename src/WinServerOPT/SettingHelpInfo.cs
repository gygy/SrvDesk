namespace WinOpt;

internal sealed class SettingHelpInfo
{
    public string Summary { get; }
    public string Purpose { get; }
    public string Benefit { get; }
    public string Guide { get; }
    public string Effect { get; }

    public SettingHelpInfo(string summary, string purpose, string benefit, string guide, string effect)
    {
        Summary = summary;
        Purpose = purpose;
        Benefit = benefit;
        Guide = guide;
        Effect = effect;
    }

    public string FormatDetail() =>
        $"【作用】{Purpose}\r\n【好处】{Benefit}\r\n【指引】{Guide}\r\n【生效】{Effect}";
}
