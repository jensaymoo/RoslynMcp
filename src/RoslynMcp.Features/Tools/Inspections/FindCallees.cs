using System.ComponentModel;
using ModelContextProtocol.Server;
using RoslynMcp.Core;
using RoslynMcp.Core.Contracts;
using RoslynMcp.Core.Models;

namespace RoslynMcp.Features.Tools.Inspections;

public sealed class FindCalleesTool(IFlowTraceService flowTraceService) : Tool
{
    private readonly IFlowTraceService _flowTraceService = flowTraceService ?? throw new ArgumentNullException(nameof(flowTraceService));

    [McpServerTool(Name = "find_callees", Title = "Find Callees", ReadOnly = true, Idempotent = true)]
    [Description("Use this tool when you need only the immediate direct downstream callees of a symbol. This is a focused wrapper around call-flow tracing and does not traverse beyond one callee level.")]
    public Task<TraceFlowResult> ExecuteAsync(CancellationToken cancellationToken,
        [Description("The stable symbol ID, obtained from resolve_symbol, list_types, or list_members, for the symbol whose immediate direct callees you want to inspect.")]
        string? symbolId = null
        )
        => _flowTraceService.TraceFlowAsync(symbolId.ToTraceFlowRequest(null, null, null, "downstream", 1, false), cancellationToken);
}