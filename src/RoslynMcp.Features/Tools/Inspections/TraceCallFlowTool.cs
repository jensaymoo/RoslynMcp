using System.ComponentModel;
using ModelContextProtocol.Server;
using RoslynMcp.Core;
using RoslynMcp.Core.Contracts;
using RoslynMcp.Core.Models;

namespace RoslynMcp.Features.Tools.Inspections;

public sealed class TraceCallFlowTool(IFlowTraceService flowTraceService) : Tool
{
    private readonly IFlowTraceService _flowTraceService = flowTraceService ?? throw new ArgumentNullException(nameof(flowTraceService));

    [McpServerTool(Name = "trace_call_flow", Title = "Trace Call Flow", ReadOnly = true, Idempotent = true)]
        [Description("Use this tool when you need to understand how code flows through your system — either finding what calls a specific symbol (upstream) or what a symbol calls (downstream). Results prefer hand-written source by default so generated/intermediate call edges do not overwhelm interactive traces, and transition labels now degrade explicitly to unresolved_project/project_inference_degraded when attribution is uncertain. Set includePossibleTargets=true to receive a deliberate possible-runtime-target edge set for uncertain polymorphic dispatch.")]
    public Task<TraceFlowResult> ExecuteAsync(CancellationToken cancellationToken,
        [Description("The stable symbol ID, obtained from resolve_symbol, list_types, or list_members. Provide this OR path+line+column.")]
        string? symbolId = null,
        [Description("Path to a source file. Provide this together with line and column instead of symbolId.")]
        string? path = null,
        [Description("Line number (1-based) pointing to the symbol in the source file.")]
        int? line = null,
        [Description("Column number (1-based) pointing to the symbol in the source file.")]
        int? column = null,
        [Description("Which direction to trace. upstream finds callers (who uses this). downstream finds callees (what this calls). both returns both directions. Defaults to both.")]
        string? direction = null,
        [Description("How many levels of the call chain to traverse. Defaults to 2. Use larger values for deeper analysis. Null is treated the same as omitting the parameter.")]
        int? depth = null,
        [Description("When true, also returns possible-runtime-target edges for uncertain interface or polymorphic dispatch. Direct static edges remain separate in the main edge list.")]
        bool? includePossibleTargets = null
        )
        => _flowTraceService.TraceFlowAsync(symbolId.ToTraceFlowRequest(path, line, column, direction, depth, includePossibleTargets), cancellationToken);
}