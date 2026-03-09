using Microsoft.CodeAnalysis;

namespace RoslynMcp.Infrastructure.Documentation;

internal interface ISymbolDocumentationProvider
{
    SymbolDocumentation? GetDocumentation(ISymbol symbol);
}

internal sealed record SymbolDocumentation(
    string? Summary,
    string? Returns,
    IReadOnlyList<SymbolParameterDocumentation> Parameters);

internal sealed record SymbolParameterDocumentation(
    string Name,
    string Description);
