using System.Text.RegularExpressions;

namespace TgLitePanel.Core.Abstractions.Utils;

public static class OtpCodeExtractor
{
    private static readonly Regex OtpRegex = new(@"\b\d{4,8}\b", RegexOptions.Compiled);

    public static bool TryExtract(string text, out string code)
    {
        code = string.Empty;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var match = OtpRegex.Match(text);
        if (!match.Success)
            return false;

        code = match.Value;
        return true;
    }
}

