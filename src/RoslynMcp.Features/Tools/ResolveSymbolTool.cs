using ModelContextProtocol.Server;
using RoslynMcp.Core.Contracts;
using RoslynMcp.Core.Models;
using RoslynMcp.Core;
using System.ComponentModel;

namespace RoslynMcp.Features.Tools;

public sealed class ResolveSymbolTool(ICodeUnderstandingService codeUnderstandingService) : Tool
{
    private readonly ICodeUnderstandingService _codeUnderstandingService = codeUnderstandingService ?? throw new ArgumentNullException(nameof(codeUnderstandingService));

    [McpServerTool(Name = "resolve_symbol", Title = "Resolve Symbol", ReadOnly = true, Idempotent = true)]
    [Description("Use this tool when you have a source position (file path + line + column), a qualified symbol name, or an existing symbolId and need the stable symbolId and declaration location used by other navigation tools. Qualified-name lookup can search the whole loaded solution, but project selectors help disambiguate short names or duplicate symbols across projects.")]
    public Task<ResolveSymbolResult> ExecuteAsync(CancellationToken cancellationToken,
        [Description("An existing symbol ID to look up. Provide this OR path+line+column OR qualifiedName.")]
        string? symbolId = null,
        [Description("Path to a source file. Provide this together with line and column instead of symbolId or qualifiedName.")]
        string? path = null,
        [Description("Line number (1-based) in the source file.")]
        int? line = null,
        [Description("Column number (1-based) in the source file.")]
        int? column = null,
        [Description("A fully qualified or short type/member name (e.g., System.String, MyNamespace.MyType.MyMethod, or MyMethod). Provide this instead of symbolId or path+line+column.")]
        string? qualifiedName = null,
        [Description("Optional project scope for qualifiedName lookup — path to a project that contains the symbol. Use to narrow ambiguous matches.")]
        string? projectPath = null,
        [Description("Optional project scope for qualifiedName lookup — name of a project that contains the symbol. Use to narrow ambiguous matches.")]
        string? projectName = null,
        [Description("Optional project scope for qualifiedName lookup — project ID from load_solution that contains the symbol. Use to narrow ambiguous matches.")]
        string? projectId = null
        )
        => _codeUnderstandingService.ResolveSymbolAsync(symbolId.ToResolveSymbolRequest(path, line, column, qualifiedName, projectPath, projectName, projectId), cancellationToken);
}
