namespace MiniGameBrowser.Core;

public static class UrlHelper
{
    public static bool TryNormalize(string input, out Uri? uri)
    {
        uri = null;
        input = input.Trim();
        if (string.IsNullOrWhiteSpace(input)) return false;
        if (!input.Contains(' ') && !input.Contains('.') && !input.Contains(':'))
            input = "https://www.google.com/search?q=" + Uri.EscapeDataString(input);
        else if (!input.Contains("://")) input = "https://" + input;
        return Uri.TryCreate(input, UriKind.Absolute, out uri) &&
               (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) || uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase));
    }
}
