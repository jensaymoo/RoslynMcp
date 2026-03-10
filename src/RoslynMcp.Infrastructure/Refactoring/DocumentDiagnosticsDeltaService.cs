using Microsoft.CodeAnalysis;
using RoslynMcp.Core.Models;

namespace RoslynMcp.Infrastructure.Refactoring;

internal sealed class DocumentDiagnosticsDeltaService
{
    public async Task<DiagnosticsDeltaInfo> GetDeltaAsync(
        Solution beforeSolution,
        Solution afterSolution,
        DocumentId documentId,
        CancellationToken ct)
    {
        var before = await CollectDiagnosticsAsync(beforeSolution, documentId, ct).ConfigureAwait(false);
        var after = await CollectDiagnosticsAsync(afterSolution, documentId, ct).ConfigureAwait(false);
        var beforeKeys = before.Select(CreateKey).ToHashSet(StringComparer.Ordinal);

        var introduced = after
            .Where(diagnostic => !beforeKeys.Contains(CreateKey(diagnostic)))
            .ToArray();

        return new DiagnosticsDeltaInfo(
            introduced.Where(static diagnostic => string.Equals(diagnostic.Severity, "error", StringComparison.Ordinal)).ToArray(),
            introduced.Where(static diagnostic => string.Equals(diagnostic.Severity, "warning", StringComparison.Ordinal)).ToArray());
    }

    private static async Task<IReadOnlyList<MutationDiagnosticInfo>> CollectDiagnosticsAsync(Solution solution, DocumentId documentId, CancellationToken ct)
    {
        var document = solution.GetDocument(documentId);
        if (document == null)
        {
            return Array.Empty<MutationDiagnosticInfo>();
        }

        var syntaxTree = await document.GetSyntaxTreeAsync(ct).ConfigureAwait(false);
        if (syntaxTree == null)
        {
            return Array.Empty<MutationDiagnosticInfo>();
        }

        var compilation = await document.Project.GetCompilationAsync(ct).ConfigureAwait(false);
        if (compilation == null)
        {
            return Array.Empty<MutationDiagnosticInfo>();
        }

        var filePath = document.FilePath ?? document.Name;
        return compilation.GetDiagnostics(ct)
            .Where(static diagnostic => diagnostic.Location.IsInSource)
            .Where(diagnostic => string.Equals(diagnostic.Location.SourceTree?.FilePath, filePath, StringComparison.OrdinalIgnoreCase))
            .Where(static diagnostic => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .Select(static diagnostic => ToMutationDiagnosticInfo(diagnostic))
            .OrderBy(static diagnostic => diagnostic.FilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static diagnostic => diagnostic.Line)
            .ThenBy(static diagnostic => diagnostic.Column)
            .ThenBy(static diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static MutationDiagnosticInfo ToMutationDiagnosticInfo(Diagnostic diagnostic)
    {
        var span = diagnostic.Location.GetLineSpan();
        var start = span.StartLinePosition;

        return new MutationDiagnosticInfo(
            diagnostic.Id,
            diagnostic.Severity == DiagnosticSeverity.Error ? "error" : "warning",
            diagnostic.GetMessage(),
            span.Path,
            start.Line + 1,
            start.Character + 1,
            "compiler");
    }

    private static string CreateKey(MutationDiagnosticInfo diagnostic)
        => string.Join(
            "|",
            diagnostic.Id,
            diagnostic.Severity,
            diagnostic.Message,
            diagnostic.FilePath,
            diagnostic.Line,
            diagnostic.Column,
            diagnostic.Origin ?? string.Empty);
}
