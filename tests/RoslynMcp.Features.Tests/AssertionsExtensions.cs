using Is.Assertions;
using RoslynMcp.Core.Models;
using RoslynMcp.Features.Tests.ToolTests;
using Xunit;

namespace RoslynMcp.Features.Tests;

public static class AssertionsExtensions
{

    extension(ErrorInfo? error)
    {
        public void ShouldBeNone()
        {
            error.IsNull();
        }

        public void ShouldHaveCode(string expectedCode)
        {
            error.IsNotNull();
            error!.Code.Is(expectedCode);
        }
    }

    public static void ShouldNotBeEmtpy(this string text)
    {
        string.IsNullOrEmpty(text).IsFalse();
    }

    
    public static void ShouldMatchFindings(this IReadOnlyList<CodeSmellMatch> actual, ExpectedCodeSmellFinding[] expected)
    {
        actual.Count.Is(expected.Length);

        for (var i = 0; i < expected.Length; i++)
        {
            var actualFinding = actual[i];
            var expectedFinding = expected[i];

            actualFinding.Location.Line.Is(expectedFinding.Line);
            actualFinding.Location.Column.Is(expectedFinding.Column);
            actualFinding.Title.Is(expectedFinding.Title);
            actualFinding.Category.Is(expectedFinding.Category);
            actualFinding.RiskLevel.Is(expectedFinding.RiskLevel);
        }
    }
}
