using System.ComponentModel;
using ModelContextProtocol.Server;
using RoslynMcp.Core;
using RoslynMcp.Core.Contracts;
using RoslynMcp.Core.Models;

namespace RoslynMcp.Features.Tools.Mutations;

public sealed class ReplaceMethodBodyTool(IRefactoringService refactoringService) : Tool
{
    private readonly IRefactoringService _refactoringService = refactoringService ?? throw new ArgumentNullException(nameof(refactoringService));

    [McpServerTool(Name = "replace_method_body", Title = "Replace Method Body")]
    [Description("Use this tool when you need a targeted logic change that preserves an existing method declaration shape. It works on block-bodied methods only, not expression-bodied ones. Provide the target method symbol id and a body string containing the replacement statements. The body can include multiple statements and complex control flow as long as it is valid for that method shape. The tool preserves the declaration shape, replaces only the body node, formats the changed document, applies the solution, and returns changed files plus newly introduced diagnostics for the changed document.")]
    public Task<ReplaceMethodBodyResult> ExecuteAsync(CancellationToken cancellationToken,
        [Description("The stable symbol id of the exact existing method whose body should be replaced. This must resolve to one source-declared, ordinary, block-bodied C# method.")]
        string targetMethodSymbolId,
        [Description("The replacement method body statements only, without outer braces. Multiple statements, local functions, lambdas, async/await, loops, and try/catch blocks are allowed when valid for the method declaration.")]
        string body)
    {
        return _refactoringService.ReplaceMethodBodyAsync(
            targetMethodSymbolId.ToReplaceMethodBodyRequest(body),
            cancellationToken);
    }
}
