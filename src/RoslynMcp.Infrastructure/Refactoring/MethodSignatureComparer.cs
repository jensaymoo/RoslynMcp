using Microsoft.CodeAnalysis;
using RoslynMcp.Core.Models;

namespace RoslynMcp.Infrastructure.Refactoring;

internal static class MethodSignatureComparer
{
    private static readonly IReadOnlyDictionary<string, string> TypeAliasMap = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["bool"] = "bool",
        ["byte"] = "byte",
        ["char"] = "char",
        ["decimal"] = "decimal",
        ["double"] = "double",
        ["float"] = "float",
        ["int"] = "int",
        ["long"] = "long",
        ["object"] = "object",
        ["sbyte"] = "sbyte",
        ["short"] = "short",
        ["string"] = "string",
        ["uint"] = "uint",
        ["ulong"] = "ulong",
        ["ushort"] = "ushort",
        ["void"] = "void",
        ["System.Boolean"] = "bool",
        ["global::System.Boolean"] = "bool",
        ["System.Byte"] = "byte",
        ["global::System.Byte"] = "byte",
        ["System.Char"] = "char",
        ["global::System.Char"] = "char",
        ["System.Decimal"] = "decimal",
        ["global::System.Decimal"] = "decimal",
        ["System.Double"] = "double",
        ["global::System.Double"] = "double",
        ["System.Single"] = "float",
        ["global::System.Single"] = "float",
        ["System.Int32"] = "int",
        ["global::System.Int32"] = "int",
        ["System.Int64"] = "long",
        ["global::System.Int64"] = "long",
        ["System.Object"] = "object",
        ["global::System.Object"] = "object",
        ["System.SByte"] = "sbyte",
        ["global::System.SByte"] = "sbyte",
        ["System.Int16"] = "short",
        ["global::System.Int16"] = "short",
        ["System.String"] = "string",
        ["global::System.String"] = "string",
        ["System.UInt32"] = "uint",
        ["global::System.UInt32"] = "uint",
        ["System.UInt64"] = "ulong",
        ["global::System.UInt64"] = "ulong",
        ["System.UInt16"] = "ushort",
        ["global::System.UInt16"] = "ushort",
        ["System.Void"] = "void",
        ["global::System.Void"] = "void"
    };

    public static bool HasEquivalentMethod(INamedTypeSymbol typeSymbol, MethodInsertionSpec spec, IMethodSymbol? excludedMethod = null)
        => typeSymbol.GetMembers(spec.Name)
            .OfType<IMethodSymbol>()
            .Where(method => excludedMethod == null || !SymbolEqualityComparer.Default.Equals(method, excludedMethod))
            .Any(member => MatchesMethodSignature(member, spec));

    public static bool MatchesMethodSignature(IMethodSymbol method, MethodInsertionSpec spec)
    {
        if (method.MethodKind != MethodKind.Ordinary || method.Parameters.Length != spec.Parameters.Count)
        {
            return false;
        }

        for (var i = 0; i < spec.Parameters.Count; i++)
        {
            if (!string.Equals(NormalizeTypeIdentity(method.Parameters[i].Type), NormalizeTypeIdentity(spec.Parameters[i].Type), StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static string NormalizeTypeIdentity(ITypeSymbol type)
        => RemoveWhitespace(type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

    private static string NormalizeTypeIdentity(string type)
    {
        var trimmed = RemoveWhitespace(type.Trim());
        if (TypeAliasMap.TryGetValue(trimmed, out var alias))
        {
            return alias;
        }

        return trimmed.StartsWith("global::", StringComparison.Ordinal)
            ? trimmed[8..]
            : trimmed;
    }

    private static string RemoveWhitespace(string value)
        => string.Concat(value.Where(static character => !char.IsWhiteSpace(character)));
}
