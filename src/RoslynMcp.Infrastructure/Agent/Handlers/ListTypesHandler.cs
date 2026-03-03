using RoslynMcp.Core;
using RoslynMcp.Core.Models.Agent;
using Microsoft.CodeAnalysis;
using RoslynMcp.Infrastructure.Navigation;

namespace RoslynMcp.Infrastructure.Agent.Handlers;

internal sealed class ListTypesHandler
{
    private readonly CodeUnderstandingQueryService _queries;

    public ListTypesHandler(CodeUnderstandingQueryService queries)
    {
        _queries = queries;
    }

    public async Task<ListTypesResult> HandleAsync(ListTypesRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (solution, solutionError) = await _queries.GetCurrentSolutionWithAutoBootstrapAsync(
            "Call load_solution first to select a solution before listing types.",
            request.ProjectPath,
            ct).ConfigureAwait(false);
        if (solution == null)
        {
            return new ListTypesResult(
                Array.Empty<TypeListEntry>(),
                0,
                AgentErrorInfo.Normalize(solutionError, "Call load_solution first to select a solution before listing types."));
        }

        if (!CodeUnderstandingQueryService.TryNormalizeTypeKind(request.Kind, out var normalizedKind))
        {
            return new ListTypesResult(
                Array.Empty<TypeListEntry>(),
                0,
                AgentErrorInfo.Create(
                    ErrorCodes.InvalidInput,
                    "kind must be one of: class, record, interface, enum, struct.",
                    "Retry list_types with a supported kind filter or omit kind.",
                    ("field", "kind"),
                    ("provided", request.Kind ?? string.Empty),
                    ("expected", "class|record|interface|enum|struct")));
        }

        if (!CodeUnderstandingQueryService.TryNormalizeAccessibility(request.Accessibility, out var normalizedAccessibility))
        {
            return new ListTypesResult(
                Array.Empty<TypeListEntry>(),
                0,
                AgentErrorInfo.Create(
                    ErrorCodes.InvalidInput,
                    "accessibility must be one of: public, internal, protected, private, protected_internal, private_protected.",
                    "Retry list_types with a supported accessibility filter or omit accessibility.",
                    ("field", "accessibility"),
                    ("provided", request.Accessibility ?? string.Empty)));
        }

        var selectedProjects = CodeUnderstandingQueryService.ResolveProjectSelector(
            solution,
            request.ProjectPath,
            request.ProjectName,
            request.ProjectId,
            selectorRequired: true,
            toolName: "list_types",
            out var selectorError);

        if (selectorError != null)
        {
            return new ListTypesResult(Array.Empty<TypeListEntry>(), 0, selectorError);
        }

        var namespacePrefix = CodeUnderstandingQueryService.NormalizeOptional(request.NamespacePrefix);
        var entries = new List<TypeListEntry>();

        foreach (var project in selectedProjects)
        {
            var compilation = await project.GetCompilationAsync(ct).ConfigureAwait(false);
            if (compilation == null)
            {
                continue;
            }

            foreach (var type in CodeUnderstandingQueryService.EnumerateTypes(compilation.Assembly.GlobalNamespace))
            {
                if (!type.Locations.Any(static location => location.IsInSource))
                {
                    continue;
                }

                var kind = CodeUnderstandingQueryService.ToTypeKind(type);
                if (kind == null)
                {
                    continue;
                }

                if (normalizedKind != null && !string.Equals(kind, normalizedKind, StringComparison.Ordinal))
                {
                    continue;
                }

                var accessibility = CodeUnderstandingQueryService.NormalizeAccessibility(type.DeclaredAccessibility);
                if (normalizedAccessibility != null && !string.Equals(accessibility, normalizedAccessibility, StringComparison.Ordinal))
                {
                    continue;
                }

                var typeNamespace = CodeUnderstandingQueryService.NormalizeNamespace(type.ContainingNamespace);
                if (namespacePrefix != null && !typeNamespace.StartsWith(namespacePrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                var (filePath, line, column) = CodeUnderstandingQueryService.GetDeclarationPosition(type);
                entries.Add(new TypeListEntry(
                    type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    SymbolIdentity.CreateId(type),
                    filePath,
                    line,
                    column,
                    kind,
                    CodeUnderstandingQueryService.IsPartial(type),
                    type.Arity > 0 ? type.Arity : null));
            }
        }

        var ordered = entries
            .OrderBy(static item => item.Kind, StringComparer.Ordinal)
            .ThenBy(static item => item.DisplayName, StringComparer.Ordinal)
            .ThenBy(static item => item.Arity ?? 0)
            .ThenBy(static item => item.SymbolId, StringComparer.Ordinal)
            .ToArray();

        var (offset, limit) = CodeUnderstandingQueryService.NormalizePaging(request.Offset, request.Limit);
        var paged = ordered.Skip(offset).Take(limit).ToArray();
        return new ListTypesResult(paged, ordered.Length);
    }
}
