using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using RoslynMcp.Core;
using RoslynMcp.Core.Models;

namespace RoslynMcp.Infrastructure.Refactoring;

internal sealed class AddMethodOperations
{
    private readonly RefactoringOperationOrchestrator _owner;
    private readonly MethodDeclarationBuilder _builder;
    private readonly MethodMutationTargetResolver _targetResolver;
    private readonly DocumentDiagnosticsDeltaService _diagnosticsDeltaService;

    public AddMethodOperations(RefactoringOperationOrchestrator owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _builder = new MethodDeclarationBuilder();
        _targetResolver = new MethodMutationTargetResolver(owner);
        _diagnosticsDeltaService = new DocumentDiagnosticsDeltaService();
    }

    public async Task<AddMethodResult> AddMethodAsync(AddMethodRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ct.ThrowIfCancellationRequested();

        var validationError = request.ValidateAddMethod();
        if (validationError != null)
        {
            return validationError;
        }

        try
        {
            var (solution, version, error) = await _owner.TryGetSolutionWithVersionAsync(ct).ConfigureAwait(false);
            if (solution == null)
            {
                return RefactoringOperationExtensions.CreateAddMethodErrorResult(request.TargetTypeSymbolId, error);
            }

            var (target, resolveError) = await _targetResolver.ResolveTypeAsync(request.TargetTypeSymbolId, solution, "add_method", ct).ConfigureAwait(false);
            if (target == null)
            {
                return RefactoringOperationExtensions.CreateAddMethodErrorResult(request.TargetTypeSymbolId, resolveError);
            }

            if (!_builder.TryBuild(request.Method, out var methodDeclaration, out var builderError) || methodDeclaration == null)
            {
                return RefactoringOperationExtensions.CreateAddMethodErrorResult(request.TargetTypeSymbolId, builderError);
            }

            if (MethodSignatureComparer.HasEquivalentMethod(target.TypeSymbol, request.Method))
            {
                return RefactoringOperationExtensions.CreateAddMethodErrorResult(
                    request.TargetTypeSymbolId,
                    ErrorCodes.MethodConflict,
                    $"An equivalent method '{request.Method.Name}' already exists on '{target.TypeSymbol.ToDisplayString()}'.",
                    ("targetTypeSymbolId", request.TargetTypeSymbolId),
                    ("methodName", request.Method.Name),
                    ("operation", "add_method"));
            }

            var root = await target.Document.GetSyntaxRootAsync(ct).ConfigureAwait(false);
            if (root == null)
            {
                return RefactoringOperationExtensions.CreateAddMethodErrorResult(
                    request.TargetTypeSymbolId,
                    ErrorCodes.InternalError,
                    "Failed to load the target document syntax tree.",
                    ("targetTypeSymbolId", request.TargetTypeSymbolId),
                    ("operation", "add_method"));
            }

            var updatedDeclaration = target.Declaration.AddMembers(methodDeclaration);
            var updatedRoot = root.ReplaceNode(target.Declaration, updatedDeclaration);
            var updatedDocument = target.Document.WithSyntaxRoot(updatedRoot);
            var updatedSolution = await _owner.FormatScopeAsync(updatedDocument.Project.Solution, [updatedDocument], ct).ConfigureAwait(false);

            var changedFiles = (await solution.CollectChangedFilesAsync(updatedSolution, ct).ConfigureAwait(false))
                .Select(static file => file.FilePath)
                .ToArray();

            var diagnosticsDelta = await _diagnosticsDeltaService
                .GetDeltaAsync(solution, updatedSolution, target.Document.Id, ct)
                .ConfigureAwait(false);

            var addedMethod = await ResolveAddedMethodAsync(updatedSolution, request, ct).ConfigureAwait(false);
            if (addedMethod == null)
            {
                return new AddMethodResult(
                    "failed",
                    changedFiles,
                    request.TargetTypeSymbolId,
                    null,
                    diagnosticsDelta,
                    RefactoringOperationExtensions.CreateError(
                        ErrorCodes.CreatedSymbolUnresolved,
                        "The inserted method could not be resolved after mutation.",
                        ("targetTypeSymbolId", request.TargetTypeSymbolId),
                        ("methodName", request.Method.Name),
                        ("operation", "add_method")));
            }

            var (applyVersion, versionError) = await _owner._solutionAccessor.GetWorkspaceVersionAsync(ct).ConfigureAwait(false);
            if (versionError != null)
            {
                return RefactoringOperationExtensions.CreateAddMethodErrorResult(request.TargetTypeSymbolId, versionError);
            }

            if (applyVersion != version)
            {
                return new AddMethodResult(
                    "failed",
                    Array.Empty<string>(),
                    request.TargetTypeSymbolId,
                    null,
                    new DiagnosticsDeltaInfo(Array.Empty<MutationDiagnosticInfo>(), Array.Empty<MutationDiagnosticInfo>()),
                    RefactoringOperationExtensions.CreateError(
                        ErrorCodes.WorkspaceChanged,
                        "Workspace changed during add_method execution.",
                        ("targetTypeSymbolId", request.TargetTypeSymbolId),
                        ("operation", "add_method")));
            }

            var (applied, applyError) = await _owner._solutionAccessor.TryApplySolutionAsync(updatedSolution, ct).ConfigureAwait(false);
            if (!applied)
            {
                return new AddMethodResult(
                    "failed",
                    changedFiles,
                    request.TargetTypeSymbolId,
                    null,
                    diagnosticsDelta,
                    applyError ?? RefactoringOperationExtensions.CreateError(
                        ErrorCodes.InternalError,
                        "Failed to apply add_method changes.",
                        ("targetTypeSymbolId", request.TargetTypeSymbolId),
                        ("operation", "add_method")));
            }

            return new AddMethodResult(
                "applied",
                changedFiles,
                request.TargetTypeSymbolId,
                addedMethod,
                diagnosticsDelta);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _owner._logger.LogError(ex, "AddMethod failed for {TargetTypeSymbolId}", request.TargetTypeSymbolId);
            return RefactoringOperationExtensions.CreateAddMethodErrorResult(
                request.TargetTypeSymbolId,
                ErrorCodes.InternalError,
                $"Failed to add method to '{request.TargetTypeSymbolId}': {ex.Message}",
                ("targetTypeSymbolId", request.TargetTypeSymbolId),
                ("operation", "add_method"));
        }
    }

    private async Task<AddedMethodInfo?> ResolveAddedMethodAsync(Solution solution, AddMethodRequest request, CancellationToken ct)
    {
        var symbol = await _owner.ResolveSymbolAsync(request.TargetTypeSymbolId, solution, ct).ConfigureAwait(false) as INamedTypeSymbol;
        if (symbol == null)
        {
            return null;
        }

        var method = symbol.GetMembers(request.Method.Name)
            .OfType<IMethodSymbol>()
            .FirstOrDefault(candidate => MethodSignatureComparer.MatchesMethodSignature(candidate, request.Method));
        if (method == null)
        {
            return null;
        }

        return new AddedMethodInfo(
            RefactoringSymbolIdentity.CreateId(method),
            method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
    }
}
