using System.ComponentModel;
using ModelContextProtocol.Server;
using RoslynMcp.Core;
using RoslynMcp.Core.Contracts;
using RoslynMcp.Core.Models;

namespace RoslynMcp.Features.Tools.Inspections;

public sealed class ResolveSymbolsTool(ICodeUnderstandingService codeUnderstandingService) : Tool
{
    private readonly ICodeUnderstandingService _codeUnderstandingService = codeUnderstandingService ?? throw new ArgumentNullException(nameof(codeUnderstandingService));

    [McpServerTool(Name = "resolve_symbols", Title = "Resolve Symbols", ReadOnly = true, Idempotent = true)]
    [Description("Use this tool when you need to resolve multiple symbols in one round-trip. Each entry reuses resolve_symbol semantics, including symbolId, source position, qualifiedName lookup, project scoping, readable symbol references, and structured ambiguity results.")]
    public Task<ResolveSymbolsBatchResult> ExecuteAsync(CancellationToken cancellationToken,
        [Description("The symbols to resolve. Each entry supports the same selector modes as resolve_symbol: symbolId, path+line+column, or qualifiedName with optional project scoping.")]
        IReadOnlyList<ResolveSymbolBatchEntry> entries)
        => _codeUnderstandingService.ResolveSymbolsBatchAsync(((string?)null).ToResolveSymbolsBatchRequest(entries), cancellationToken);
}