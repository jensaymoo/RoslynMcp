using Microsoft.CodeAnalysis;
using RoslynMcp.Core.Models;

namespace RoslynMcp.Infrastructure.Agent;

internal enum SourceKind
{
    HandWritten,
    Generated,
    Intermediate,
    Unknown
}

internal sealed record SourceVisibilityAssessment(
    string Visibility,
    int HandwrittenCount,
    int GeneratedCount,
    int UnknownCount)
{
    public bool HasHandwritten => HandwrittenCount > 0;

    public bool HasGenerated => GeneratedCount > 0;

    public bool HasUnknown => UnknownCount > 0;
}

internal static class SourceVisibility
{
    private static readonly string[] GeneratedFileSuffixes =
    [
        ".g.cs",
        ".g.i.cs",
        ".generated.cs",
        ".designer.cs",
        ".AssemblyAttributes.cs",
        ".AssemblyInfo.cs"
    ];

    public static bool ShouldIncludeInHumanResults(string? path)
        => ClassifyPath(path) is SourceKind.HandWritten or SourceKind.Unknown;

    public static bool ShouldIncludeInInteractiveTrace(string? path)
        => ClassifyPath(path) is SourceKind.HandWritten && !IsLikelyTestPath(path);

    public static bool IsGeneratedLike(string? path)
        => ToVisibilityBias(ClassifyPath(path)) == SourceBiases.Generated;

    public static SourceVisibilityAssessment AssessPaths(IEnumerable<string?> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);

        var handwritten = 0;
        var generated = 0;
        var unknown = 0;

        foreach (var path in paths)
        {
            switch (ToVisibilityBias(ClassifyPath(path)))
            {
                case SourceBiases.Handwritten:
                    handwritten++;
                    break;
                case SourceBiases.Generated:
                    generated++;
                    break;
                default:
                    unknown++;
                    break;
            }
        }

        return new SourceVisibilityAssessment(
            DetermineVisibility(handwritten, generated, unknown),
            handwritten,
            generated,
            unknown);
    }

    public static string DetermineResultSourceBias(IEnumerable<string?> paths)
    {
        var assessment = AssessPaths(paths);
        return assessment.Visibility;
    }

    public static SourceKind ClassifyPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return SourceKind.Unknown;
        }

        var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var fileName = Path.GetFileName(normalized);

        if (normalized.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            || normalized.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        {
            return SourceKind.Intermediate;
        }

        if (GeneratedFileSuffixes.Any(suffix => fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
        {
            return SourceKind.Generated;
        }

        return SourceKind.HandWritten;
    }

    public static bool IsLikelyTestPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalized = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        var fileName = Path.GetFileName(normalized);

        return normalized.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains($"{Path.DirectorySeparatorChar}test{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
               || fileName.EndsWith("Tests.cs", StringComparison.OrdinalIgnoreCase)
               || fileName.EndsWith("Test.cs", StringComparison.OrdinalIgnoreCase);
    }

    public static string ResolveProjectName(this ISymbol symbol, Project project)
    {
        var sourceLocation = symbol.Locations.FirstOrDefault(static location => location.IsInSource);
        if (sourceLocation?.SourceTree is not null)
        {
            var document = project.GetDocument(sourceLocation.SourceTree);
            if (document != null)
            {
                return document.Project.Name;
            }
        }

        return project.Name;
    }

    private static string DetermineVisibility(int handwritten, int generated, int unknown)
    {
        if (handwritten > 0 && generated > 0)
        {
            return SourceBiases.Mixed;
        }

        if (handwritten > 0)
        {
            return SourceBiases.Handwritten;
        }

        if (generated > 0)
        {
            return SourceBiases.Generated;
        }

        return unknown > 0 ? SourceBiases.Unknown : SourceBiases.Unknown;
    }

    private static string ToVisibilityBias(SourceKind kind)
        => kind switch
        {
            SourceKind.HandWritten => SourceBiases.Handwritten,
            SourceKind.Generated or SourceKind.Intermediate => SourceBiases.Generated,
            _ => SourceBiases.Unknown
        };
}
