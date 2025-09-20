using System.Text.RegularExpressions;

public static class IconMarkupTMP
{
    // {icon:Key}  ->  <sprite name=Key>
    private static readonly Regex kTag = new Regex(@"\{icon\s*:\s*([^\}\s]+)\s*\}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string ToTMP(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;
        return kTag.Replace(raw, m => $"<sprite name={m.Groups[1].Value}>");
    }
}