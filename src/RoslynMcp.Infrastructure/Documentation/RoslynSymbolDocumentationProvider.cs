using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace RoslynMcp.Infrastructure.Documentation;

internal sealed partial class RoslynSymbolDocumentationProvider : ISymbolDocumentationProvider
{
    private static readonly Regex WhitespacePattern = WhitespaceRegex();

    public SymbolDocumentation? GetDocumentation(ISymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        var xml = symbol.GetDocumentationCommentXml(cancellationToken: CancellationToken.None);
        if (string.IsNullOrWhiteSpace(xml))
            return null;

        XElement root;
        try
        {
            root = XElement.Parse($"<root>{xml}</root>", LoadOptions.PreserveWhitespace);
        }
        catch (XmlException)
        {
            return null;
        }

        var summary = NormalizeElementText(root.Descendants("summary").FirstOrDefault());
        var returns = NormalizeElementText(root.Descendants("returns").FirstOrDefault());
        var parameters = root.Descendants("param")
            .Select(static element => CreateParameterDocumentation(element))
            .Where(static parameter => parameter is not null)
            .Cast<SymbolParameterDocumentation>()
            .ToArray();

        if (summary == null && returns == null && parameters.Length == 0)
        {
            return null;
        }

        return new SymbolDocumentation(summary, returns, parameters);
    }

    private static SymbolParameterDocumentation? CreateParameterDocumentation(XElement element)
    {
        var name = NormalizeText(element.Attribute("name")?.Value);
        var description = NormalizeElementText(element);
        if (name == null || description == null)
        {
            return null;
        }

        return new SymbolParameterDocumentation(name, description);
    }

    private static string? NormalizeElementText(XElement? element)
    {
        if (element == null)
        {
            return null;
        }

        var builder = new StringBuilder();
        AppendNodeText(element, builder);
        return NormalizeText(builder.ToString());
    }

    private static void AppendNodeText(XNode node, StringBuilder builder)
    {
        switch (node)
        {
            case XText text:
                builder.Append(text.Value);
                return;

            case XElement element when element.Name.LocalName is "see" or "seealso":
                builder.Append(NormalizeSymbolReference(element.Attribute("cref")?.Value));
                return;

            case XElement element when element.Name.LocalName is "paramref" or "typeparamref":
                builder.Append(element.Attribute("name")?.Value);
                return;

            case XElement element:
                foreach (var child in element.Nodes())
                {
                    AppendNodeText(child, builder);
                }

                return;
        }
    }

    private static string? NormalizeSymbolReference(string? cref)
    {
        var normalized = NormalizeText(cref);
        if (normalized == null)
        {
            return null;
        }

        return normalized.Length > 2 && normalized[1] == ':'
            ? normalized[2..]
            : normalized;
    }

    private static string? NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return WhitespacePattern.Replace(value.Trim(), " ");
    }

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}
