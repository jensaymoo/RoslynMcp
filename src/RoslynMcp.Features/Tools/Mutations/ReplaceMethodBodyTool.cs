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
    [Description("Use this tool when you need to replace only the body of an existing block-bodied method in a loaded C# solution. Provide the target method symbol id and a body string containing only the statements inside the method body, without outer braces. The tool preserves the existing method declaration shape, replaces only the body node, formats the changed document, applies the solution, and returns changed files plus newly introduced diagnostics for the changed document.")]
    public Task<ReplaceMethodBodyResult> ExecuteAsync(CancellationToken cancellationToken,
        [Description("The stable symbol id of the exact existing method whose body should be replaced. This must resolve to one source-declared, ordinary, block-bodied C# method.")]
        string targetMethodSymbolId,
        [Description("The replacement method body content only, without outer braces. Example: 'return string.Empty;'")]
        string body)
    {
        return _refactoringService.ReplaceMethodBodyAsync(
            targetMethodSymbolId.ToReplaceMethodBodyRequest(body),
            cancellationToken);
    }
}
