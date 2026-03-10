using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynMcp.Core;
using RoslynMcp.Core.Models;

namespace RoslynMcp.Infrastructure.Refactoring;

internal sealed class MethodMutationTargetResolver(RefactoringOperationOrchestrator owner)
{
    private readonly RefactoringOperationOrchestrator _owner = owner ?? throw new ArgumentNullException(nameof(owner));

    public Task<(MethodTypeMutationTarget? Target, ErrorInfo? Error)> ResolveTypeAsync(string targetTypeSymbolId, Solution solution, string operation, CancellationToken ct)
        => ResolveTypeAsyncCore(targetTypeSymbolId, solution, operation, ct);

    public async Task<(MethodDeclarationMutationTarget? Target, ErrorInfo? Error)> ResolveMethodAsync(string targetMethodSymbolId, Solution solution, string operation, CancellationToken ct)
    {
        var symbol = await _owner.ResolveSymbolAsync(targetMethodSymbolId, solution, ct).ConfigureAwait(false);
        if (symbol == null)
        {
            return (null, RefactoringOperationExtensions.CreateError(
                ErrorCodes.SymbolNotFound,
                $"Target method symbol '{targetMethodSymbolId}' could not be resolved.",
                ("targetMethodSymbolId", targetMethodSymbolId),
                ("operation", operation)));
        }

        if (symbol is not IMethodSymbol methodSymbol)
        {
            return (null, RefactoringOperationExtensions.CreateError(
                ErrorCodes.UnsupportedSymbolKind,
                "targetMethodSymbolId must resolve to a method symbol.",
                ("targetMethodSymbolId", targetMethodSymbolId),
                ("operation", operation)));
        }

        if (methodSymbol.MethodKind != MethodKind.Ordinary)
        {
            return (null, RefactoringOperationExtensions.CreateError(
                ErrorCodes.UnsupportedSymbolKind,
                "Only ordinary source methods are supported by delete_method.",
                ("targetMethodSymbolId", targetMethodSymbolId),
                ("operation", operation)));
        }

        if (methodSymbol.DeclaringSyntaxReferences.Length != 1)
        {
            return (null, RefactoringOperationExtensions.CreateError(
                ErrorCodes.TargetNotSourceEditable,
                "The target method must have exactly one source declaration to support deterministic deletion.",
                ("targetMethodSymbolId", targetMethodSymbolId),
                ("operation", operation)));
        }

        var declarationSyntax = await methodSymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(ct).ConfigureAwait(false);
        if (declarationSyntax is not MethodDeclarationSyntax declaration)
        {
            return (null, RefactoringOperationExtensions.CreateError(
                ErrorCodes.TargetNotSourceEditable,
                "The target symbol is not a source-editable method declaration.",
                ("targetMethodSymbolId", targetMethodSymbolId),
                ("operation", operation)));
        }

        var document = solution.GetDocument(declaration.SyntaxTree);
        if (document == null)
        {
            return (null, RefactoringOperationExtensions.CreateError(
                ErrorCodes.TargetNotSourceEditable,
                "The target method could not be mapped to an editable source document.",
                ("targetMethodSymbolId", targetMethodSymbolId),
                ("operation", operation)));
        }

        return (new MethodDeclarationMutationTarget(methodSymbol, declaration, document), null);
    }

    private async Task<(MethodTypeMutationTarget? Target, ErrorInfo? Error)> ResolveTypeAsyncCore(string targetTypeSymbolId, Solution solution, string operation, CancellationToken ct)
    {
        var symbol = await _owner.ResolveSymbolAsync(targetTypeSymbolId, solution, ct).ConfigureAwait(false);
        if (symbol == null)
        {
            return (null, RefactoringOperationExtensions.CreateError(
                ErrorCodes.SymbolNotFound,
                $"Target type symbol '{targetTypeSymbolId}' could not be resolved.",
                ("targetTypeSymbolId", targetTypeSymbolId),
                ("operation", operation)));
        }

        if (symbol is not INamedTypeSymbol typeSymbol)
        {
            return (null, RefactoringOperationExtensions.CreateError(
                ErrorCodes.InvalidInput,
                "targetTypeSymbolId must resolve to a named type.",
                ("targetTypeSymbolId", targetTypeSymbolId),
                ("operation", operation)));
        }

        if (typeSymbol.DeclaringSyntaxReferences.Length != 1)
        {
            return (null, RefactoringOperationExtensions.CreateError(
                ErrorCodes.TargetNotSourceEditable,
                "The target type must have exactly one source declaration to support deterministic insertion.",
                ("targetTypeSymbolId", targetTypeSymbolId),
                ("operation", operation)));
        }

        var declarationSyntax = await typeSymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(ct).ConfigureAwait(false);
        if (declarationSyntax is not TypeDeclarationSyntax declaration)
        {
            return (null, RefactoringOperationExtensions.CreateError(
                ErrorCodes.TargetNotSourceEditable,
                "The target symbol is not a source-editable type declaration.",
                ("targetTypeSymbolId", targetTypeSymbolId),
                ("operation", operation)));
        }

        var document = solution.GetDocument(declaration.SyntaxTree);
        if (document == null)
        {
            return (null, RefactoringOperationExtensions.CreateError(
                ErrorCodes.TargetNotSourceEditable,
                "The target type could not be mapped to an editable source document.",
                ("targetTypeSymbolId", targetTypeSymbolId),
                ("operation", operation)));
        }

        return (new MethodTypeMutationTarget(typeSymbol, declaration, document), null);
    }
}

internal sealed record MethodTypeMutationTarget(
    INamedTypeSymbol TypeSymbol,
    TypeDeclarationSyntax Declaration,
    Document Document);

internal sealed record MethodDeclarationMutationTarget(
    IMethodSymbol MethodSymbol,
    MethodDeclarationSyntax Declaration,
    Document Document);
