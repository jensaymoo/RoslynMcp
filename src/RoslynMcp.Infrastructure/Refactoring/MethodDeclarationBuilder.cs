using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using RoslynMcp.Core;
using RoslynMcp.Core.Models;

namespace RoslynMcp.Infrastructure.Refactoring;

internal sealed class MethodDeclarationBuilder
{
    private static readonly Dictionary<string, SyntaxKind> AccessibilityMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["public"] = SyntaxKind.PublicKeyword,
        ["internal"] = SyntaxKind.InternalKeyword,
        ["private"] = SyntaxKind.PrivateKeyword,
        ["protected"] = SyntaxKind.ProtectedKeyword,
        ["protected_internal"] = SyntaxKind.ProtectedKeyword,
        ["private_protected"] = SyntaxKind.PrivateKeyword
    };

    private static readonly Dictionary<string, SyntaxKind[]> AccessibilityCompositeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["protected_internal"] = [SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword],
        ["private_protected"] = [SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword]
    };

    private static readonly Dictionary<string, SyntaxKind> ModifierMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["static"] = SyntaxKind.StaticKeyword,
        ["async"] = SyntaxKind.AsyncKeyword,
        ["virtual"] = SyntaxKind.VirtualKeyword,
        ["override"] = SyntaxKind.OverrideKeyword,
        ["sealed"] = SyntaxKind.SealedKeyword,
        ["new"] = SyntaxKind.NewKeyword
    };

    public bool TryBuild(MethodInsertionSpec spec, out MethodDeclarationSyntax? method, out ErrorInfo? error)
    {
        method = null;
        error = Validate(spec);
        if (error != null)
        {
            return false;
        }

        var returnType = SyntaxFactory.ParseTypeName(spec.ReturnType);
        if (HasSyntaxErrors(returnType))
        {
            error = CreateInvalidSpecError($"returnType '{spec.ReturnType}' could not be parsed.", ("field", "method.returnType"));
            return false;
        }

        var parameterNodes = new List<ParameterSyntax>(spec.Parameters.Count);
        for (var i = 0; i < spec.Parameters.Count; i++)
        {
            var parameter = spec.Parameters[i];
            var parameterType = SyntaxFactory.ParseTypeName(parameter.Type);
            if (HasSyntaxErrors(parameterType))
            {
                error = CreateInvalidSpecError(
                    $"parameter type '{parameter.Type}' for parameter '{parameter.Name}' could not be parsed.",
                    ("field", $"method.parameters[{i}].type"));
                return false;
            }

            parameterNodes.Add(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameter.Name))
                    .WithType(parameterType));
        }

        if (!TryParseBody(spec.Body, out var body, out error))
        {
            return false;
        }

        method = SyntaxFactory.MethodDeclaration(returnType, SyntaxFactory.Identifier(spec.Name))
            .WithModifiers(BuildModifiers(spec.Accessibility, spec.Modifiers))
            .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameterNodes)))
            .WithBody(body)
            .WithAdditionalAnnotations(Formatter.Annotation);

        return true;
    }

    public bool TryParseBody(string body, out BlockSyntax? block, out ErrorInfo? error)
    {
        block = ParseBodyBlock(body);
        if (block != null)
        {
            error = null;
            return true;
        }

        error = CreateInvalidSpecError(
            "method.body could not be parsed as a valid block-bodied method.",
            ("field", "method.body"));
        return false;
    }

    private static ErrorInfo? Validate(MethodInsertionSpec spec)
    {
        if (string.IsNullOrWhiteSpace(spec.Name) || !SyntaxFacts.IsValidIdentifier(spec.Name))
        {
            return CreateInvalidSpecError("method.name must be a valid C# identifier.", ("field", "method.name"));
        }

        if (string.IsNullOrWhiteSpace(spec.ReturnType))
        {
            return CreateInvalidSpecError("method.returnType must be provided.", ("field", "method.returnType"));
        }

        if (string.IsNullOrWhiteSpace(spec.Accessibility) || !AccessibilityMap.ContainsKey(spec.Accessibility))
        {
            return CreateInvalidSpecError(
                "method.accessibility must be one of: public, internal, private, protected, protected_internal, private_protected.",
                ("field", "method.accessibility"));
        }

        if (spec.Modifiers is null)
        {
            return CreateInvalidSpecError("method.modifiers must be provided.", ("field", "method.modifiers"));
        }

        if (spec.Parameters is null)
        {
            return CreateInvalidSpecError("method.parameters must be provided.", ("field", "method.parameters"));
        }

        if (spec.Body is null)
        {
            return CreateInvalidSpecError("method.body must be provided.", ("field", "method.body"));
        }

        if (!string.IsNullOrWhiteSpace(spec.Placement)
            && !string.Equals(spec.Placement, "end_of_type", StringComparison.OrdinalIgnoreCase))
        {
            return CreateInvalidSpecError("method.placement must be 'end_of_type' when provided.", ("field", "method.placement"));
        }

        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < spec.Parameters.Count; i++)
        {
            var parameter = spec.Parameters[i];
            if (parameter is null)
            {
                return CreateInvalidSpecError("method.parameters cannot contain null entries.", ("field", $"method.parameters[{i}]"));
            }

            if (string.IsNullOrWhiteSpace(parameter.Name) || !SyntaxFacts.IsValidIdentifier(parameter.Name))
            {
                return CreateInvalidSpecError(
                    $"parameter name at index {i} must be a valid C# identifier.",
                    ("field", $"method.parameters[{i}].name"));
            }

            if (!seenNames.Add(parameter.Name))
            {
                return CreateInvalidSpecError(
                    $"parameter name '{parameter.Name}' is duplicated.",
                    ("field", $"method.parameters[{i}].name"));
            }

            if (string.IsNullOrWhiteSpace(parameter.Type))
            {
                return CreateInvalidSpecError(
                    $"parameter type for '{parameter.Name}' must be provided.",
                    ("field", $"method.parameters[{i}].type"));
            }
        }

        foreach (var modifier in spec.Modifiers)
        {
            if (string.IsNullOrWhiteSpace(modifier) || !ModifierMap.ContainsKey(modifier))
            {
                return CreateInvalidSpecError(
                    $"unsupported modifier '{modifier}'. Supported modifiers: static, async, virtual, override, sealed, new.",
                    ("field", "method.modifiers"));
            }
        }

        return null;
    }

    private static SyntaxTokenList BuildModifiers(string accessibility, IReadOnlyList<string> modifiers)
    {
        var tokens = new List<SyntaxToken>();
        if (AccessibilityCompositeMap.TryGetValue(accessibility, out var compositeKinds))
        {
            tokens.AddRange(compositeKinds.Select(SyntaxFactory.Token));
        }
        else
        {
            tokens.Add(SyntaxFactory.Token(AccessibilityMap[accessibility]));
        }

        foreach (var modifier in modifiers)
        {
            tokens.Add(SyntaxFactory.Token(ModifierMap[modifier]));
        }

        return SyntaxFactory.TokenList(tokens);
    }

    private static BlockSyntax? ParseBodyBlock(string body)
    {
        var parsed = SyntaxFactory.ParseStatement("{" + Environment.NewLine + body + Environment.NewLine + "}") as BlockSyntax;

        if (parsed == null || HasSyntaxErrors(parsed))
        {
            return null;
        }

        return parsed;
    }

    private static bool HasSyntaxErrors(CSharpSyntaxNode node)
        => node.ContainsDiagnostics || node.GetDiagnostics().Any(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
           || node.DescendantTokens(descendIntoTrivia: true).Any(static token => token.IsMissing);

    private static ErrorInfo CreateInvalidSpecError(string message, params (string Key, string? Value)[] details)
        => RefactoringOperationExtensions.CreateError(
            ErrorCodes.InvalidMethodSpecification,
            message,
            [("operation", "add_method"), .. details]);
}
