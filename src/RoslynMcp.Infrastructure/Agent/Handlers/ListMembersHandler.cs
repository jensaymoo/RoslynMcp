using RoslynMcp.Core;
using RoslynMcp.Core.Models.Agent;

namespace RoslynMcp.Infrastructure.Agent.Handlers;

internal sealed class ListMembersHandler
{
    private readonly CodeUnderstandingQueryService _queries;

    public ListMembersHandler(CodeUnderstandingQueryService queries)
    {
        _queries = queries;
    }

    public async Task<ListMembersResult> HandleAsync(ListMembersRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (solution, solutionError) = await _queries.GetCurrentSolutionWithAutoBootstrapAsync(
            "Call load_solution first to select a solution before listing members.",
            request.Path,
            ct).ConfigureAwait(false);
        if (solution == null)
        {
            return new ListMembersResult(
                Array.Empty<MemberListEntry>(),
                0,
                request.IncludeInherited,
                AgentErrorInfo.Normalize(solutionError, "Call load_solution first to select a solution before listing members."));
        }

        if (!CodeUnderstandingQueryService.TryNormalizeMemberKind(request.Kind, out var normalizedMemberKind))
        {
            return new ListMembersResult(
                Array.Empty<MemberListEntry>(),
                0,
                request.IncludeInherited,
                AgentErrorInfo.Create(
                    ErrorCodes.InvalidInput,
                    "kind must be one of: method, property, field, event, ctor.",
                    "Retry list_members with a supported kind filter or omit kind.",
                    ("field", "kind"),
                    ("provided", request.Kind ?? string.Empty),
                    ("expected", "method|property|field|event|ctor")));
        }

        if (!CodeUnderstandingQueryService.TryNormalizeAccessibility(request.Accessibility, out var normalizedAccessibility))
        {
            return new ListMembersResult(
                Array.Empty<MemberListEntry>(),
                0,
                request.IncludeInherited,
                AgentErrorInfo.Create(
                    ErrorCodes.InvalidInput,
                    "accessibility must be one of: public, internal, protected, private, protected_internal, private_protected.",
                    "Retry list_members with a supported accessibility filter or omit accessibility.",
                    ("field", "accessibility"),
                    ("provided", request.Accessibility ?? string.Empty)));
        }

        if (!CodeUnderstandingQueryService.TryNormalizeBinding(request.Binding, out var normalizedBinding))
        {
            return new ListMembersResult(
                Array.Empty<MemberListEntry>(),
                0,
                request.IncludeInherited,
                AgentErrorInfo.Create(
                    ErrorCodes.InvalidInput,
                    "binding must be one of: static, instance.",
                    "Retry list_members with binding=static or binding=instance, or omit binding.",
                    ("field", "binding"),
                    ("provided", request.Binding ?? string.Empty),
                    ("expected", "static|instance")));
        }

        var typeSymbol = await _queries.ResolveTypeSymbolAsync(request, solution, ct).ConfigureAwait(false);
        if (typeSymbol.Error != null)
        {
            return new ListMembersResult(Array.Empty<MemberListEntry>(), 0, request.IncludeInherited, typeSymbol.Error);
        }

        var symbols = request.IncludeInherited
            ? CodeUnderstandingQueryService.CollectMembersWithInheritance(typeSymbol.Symbol!)
            : typeSymbol.Symbol!.GetMembers();

        var entries = symbols
            .Select(member => CodeUnderstandingQueryService.ToMemberEntry(member, normalizedMemberKind, normalizedAccessibility, normalizedBinding))
            .Where(static entry => entry != null)
            .Select(static entry => entry!)
            .OrderBy(static item => item.Kind, StringComparer.Ordinal)
            .ThenBy(static item => item.DisplayName, StringComparer.Ordinal)
            .ThenBy(static item => item.Signature, StringComparer.Ordinal)
            .ThenBy(static item => item.SymbolId, StringComparer.Ordinal)
            .ToArray();

        var (offset, limit) = CodeUnderstandingQueryService.NormalizePaging(request.Offset, request.Limit);
        var paged = entries.Skip(offset).Take(limit).ToArray();
        return new ListMembersResult(paged, entries.Length, request.IncludeInherited);
    }
}
