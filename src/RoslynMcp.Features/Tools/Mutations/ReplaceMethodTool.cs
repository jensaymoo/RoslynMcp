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
    [Description("Use this tool when you need to replace an existing method in a loaded type without manually rewriting the full file. Provide the target method symbol id plus flat method declaration fields. Parameters must be simple C# parameter declaration strings like 'string input' without surrounding parentheses, and body must contain only the statements inside the method body without braces. The tool replaces the method structurally, formats the changed document, applies the solution, and returns the new method symbol id plus newly introduced diagnostics for the changed document.")]
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
        [Description("Optional replacement parameter declarations without surrounding parentheses. Each entry should be a simple C# parameter declaration string such as 'string input', 'int priority', or 'bool isEnabled'.")]
        IReadOnlyList<string>? parameters,
        [Description("The replacement method body content only, without outer braces. Example: 'return string.Empty;'")]
        string body)
    {
        return _refactoringService.ReplaceMethodAsync(
            targetMethodSymbolId.ToReplaceMethodRequest(name, returnType, accessibility, modifiers, parameters, body),
            cancellationToken);
    }
}
