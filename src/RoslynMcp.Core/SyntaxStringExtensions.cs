namespace RoslynMcp.Core;

internal static class SyntaxStringExtensions
{
    internal static string NormalizeEscapedTypeSyntax(this string input) => input
        .Replace("&lt;", "<", StringComparison.Ordinal)
        .Replace("&gt;", ">", StringComparison.Ordinal);

    internal static string NormalizeEscapedNewlines(this string input) => input
        .Replace("\\r\\n", "\r\n", StringComparison.Ordinal)
        .Replace("\\n", "\n", StringComparison.Ordinal)
        .Replace("\\r", "\r", StringComparison.Ordinal);
}
