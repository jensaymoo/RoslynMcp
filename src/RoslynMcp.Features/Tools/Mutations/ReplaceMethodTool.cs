using System.ComponentModel;
using ModelContextProtocol.Server;
using RoslynMcp.Core;
using RoslynMcp.Core.Contracts;
using RoslynMcp.Core.Models;

namespace RoslynMcp.Features.Tools.Mutations;

public sealed class ReplaceMethodTool(IRefactoringService refactoringService) : Tool
{
    private readonly IRefactoringService _refactoringService = refactoringService ?? throw new ArgumentNullException(nameof(refactoringService));

    [McpServerTool(Name = "replace_method", Title = "Replace Method")]
    [Description("Use this tool when you need to rewrite an existing method structurally without manually editing the full file. Provide the target method symbol id plus flat replacement declaration fields and a method body string. The body can include multiple statements and complex control flow as long as it is valid for that method shape. The tool replaces the method structurally, formats the changed document, applies the solution, and returns a new method symbol id to use for later operations plus newly introduced diagnostics for the changed document.")]
    public Task<ReplaceMethodResult> ExecuteAsync(CancellationToken cancellationToken,
        [Description("The stable symbol id of the exact existing method to replace. This must resolve to one source-declared, ordinary C# method.")]
        string targetMethodSymbolId,
        [Description("The replacement method name, for example 'Evaluate'.")]
        string name,
        [Description("The replacement C# return type text, for example 'string', 'int', or 'Task<string>'.")]
        string returnType,
        [Description("The replacement accessibility: public, internal, private, protected, protected_internal, or private_protected.")]
        string accessibility,
        [Description("Optional replacement method modifiers as individual tokens, for example ['static'] or ['async']. Do not include accessibility here.")]
        IReadOnlyList<string>? modifiers,
        [Description("Optional replacement parameter declarations without surrounding parentheses. Each entry should be a simple C# parameter declaration string such as 'string input', 'int priority', or 'CancellationToken cancellationToken'.")]
        IReadOnlyList<string>? parameters,
        [Description("The replacement method body statements only, without outer braces. Multiple statements, local functions, lambdas, async/await, loops, and try/catch blocks are allowed when valid for the method declaration.")]
        string body)
    {
        return _refactoringService.ReplaceMethodAsync(
            targetMethodSymbolId.ToReplaceMethodRequest(name, returnType, accessibility, modifiers, parameters, body),
            cancellationToken);
    }
}
