using System.ComponentModel;
using ModelContextProtocol.Server;
using RoslynMcp.Core;
using RoslynMcp.Core.Contracts;
using RoslynMcp.Core.Models;

namespace RoslynMcp.Features.Tools.Mutations;

public sealed class DeleteMethodTool(IRefactoringService refactoringService) : Tool
{
    private readonly IRefactoringService _refactoringService = refactoringService ?? throw new ArgumentNullException(nameof(refactoringService));

    [McpServerTool(Name = "delete_method", Title = "Delete Method")]
    [Description("Use this tool when you need to remove an exact method declaration from a loaded solution without manually rewriting the full file, such as cleaning up disposable helpers or obsolete overloads. Provide the stable symbol id of the exact method to remove. The tool resolves that method semantically, removes its source declaration structurally, formats the changed document, applies the solution, and returns changed files plus newly introduced diagnostics for the changed document.")]
    public Task<DeleteMethodResult> ExecuteAsync(CancellationToken cancellationToken,
        [Description("The stable symbol id of the exact existing method to delete. This must resolve to one source-declared, ordinary C# method.")]
        string targetMethodSymbolId)
    {
        return _refactoringService.DeleteMethodAsync(targetMethodSymbolId.ToDeleteMethodRequest(), cancellationToken);
    }
}
