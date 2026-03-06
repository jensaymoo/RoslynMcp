using Is.Assertions;
using RoslynMcp.Core.Models;
using RoslynMcp.Features.Tests.ToolTests;

namespace RoslynMcp.Features.Tests;

public static class AssertionsExtensions
{
    extension(string text)
    {
        public void ShouldNotBeEmpty()
        {
            string.IsNullOrEmpty(text).IsFalse();
        }
    }
    
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

    extension(ResolvedSymbolSummary? symbol)
    {
        public void ShouldMatchResolvedSymbol(string expectedDisplayName, string expectedKind, string expectedFileName)
        {
            symbol.IsNotNull();
            symbol!.DisplayName.Is(expectedDisplayName);
            symbol.Kind.Is(expectedKind);
            symbol.FilePath.EndsWith(expectedFileName, StringComparison.OrdinalIgnoreCase).IsTrue();
            symbol.SymbolId.ShouldNotBeEmpty();
        }
    }

    extension(IReadOnlyList<CodeSmellMatch> actual)
    {
        public void ShouldMatchFindings(ExpectedCodeSmellFinding[] expected)
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
}
