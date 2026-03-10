using System.ComponentModel;
using ModelContextProtocol.Server;
using RoslynMcp.Core;
using RoslynMcp.Core.Contracts;
using RoslynMcp.Core.Models;

namespace RoslynMcp.Features.Tools.Mutations;

public sealed class AddMethodTool(IRefactoringService refactoringService) : Tool
{
    private readonly IRefactoringService _refactoringService = refactoringService ?? throw new ArgumentNullException(nameof(refactoringService));

    [McpServerTool(Name = "add_method", Title = "Add Method")]
    [Description("Use this tool when you need to add a new helper, overload, or other method to an existing loaded type without rewriting the full file. Provide the target type symbol id, flat method signature fields, and a method body string. The body can include multiple statements and complex control flow as long as it is valid for that method shape. The tool inserts the method structurally, formats the changed document, applies the solution, and returns the created method symbol id plus newly introduced diagnostics for the changed document.")]
    public Task<AddMethodResult> ExecuteAsync(CancellationToken cancellationToken,
        [Description("The stable symbol id of the existing target type that should receive the new method.")]
        string targetTypeSymbolId,
        [Description("The new method name, for example 'Evaluate'.")]
        string name,
        [Description("The C# return type text, for example 'string', 'int', or 'Task<string>'.")]
        string returnType,
        [Description("The declared accessibility: public, internal, private, protected, protected_internal, or private_protected.")]
        string accessibility,
        [Description("Optional method modifiers as individual tokens, for example ['static'] or ['async']. Do not include accessibility here.")]
        IReadOnlyList<string>? modifiers,
        [Description("Optional parameter declarations without surrounding parentheses. Each entry should be a simple C# parameter declaration string such as 'string input', 'int priority', or 'CancellationToken cancellationToken'.")]
        IReadOnlyList<string>? parameters,
        [Description("The method body statements only, without outer braces. Multiple statements, local functions, lambdas, async/await, loops, and try/catch blocks are allowed when valid for the method declaration.")]
        string body)
    {
        return _refactoringService.AddMethodAsync(
            targetTypeSymbolId.ToAddMethodRequest(name, returnType, accessibility, modifiers, parameters, body),
            cancellationToken);
    }
}
