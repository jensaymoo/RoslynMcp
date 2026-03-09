using RoslynMcp.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynMcp.Infrastructure.Analysis;

/// <summary>
/// Collects code metrics: cyclomatic complexity, lines of code, member counts.
/// Walks syntax trees to compute per-type and per-member metrics.
/// </summary>
internal sealed class AnalysisMetricsCollector(IRoslynSymbolIdFactory symbolIdFactory) : IAnalysisMetricsCollector
{
    private readonly IRoslynSymbolIdFactory _symbolIdFactory = symbolIdFactory ?? throw new ArgumentNullException(nameof(symbolIdFactory));

    public Task<IReadOnlyList<MetricItem>> CollectMetricsAsync(Solution solution, string scope, string? path, CancellationToken ct)
        => CollectMetricsAsync(solution.Projects, scope, path, ct);

    public async Task<IReadOnlyList<MetricItem>> CollectMetricsAsync(
        IEnumerable<Project> projects,
        string scope,
        string? path,
        CancellationToken ct)
    {
        var metrics = new List<MetricItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var scopeResolver = new AnalysisScopeResolver();

        foreach (var project in projects.OrderBy(static p => p.FilePath ?? p.Name, StringComparer.Ordinal))
        {
            ct.ThrowIfCancellationRequested();
            foreach (var document in project.Documents.OrderBy(static d => d.FilePath ?? d.Name, StringComparer.Ordinal))
            {
                ct.ThrowIfCancellationRequested();
                if (!scopeResolver.IsDocumentInScope(document, scope, path))
                    continue;

                if(await document.GetSyntaxRootAsync(ct).ConfigureAwait(false) is not { } syntaxRoot)
                    continue;

                if(await document.GetSemanticModelAsync(ct).ConfigureAwait(false) is not { } semanticModel)
                    continue;
                
                var walker = new MemberMetricWalker(semanticModel, _symbolIdFactory, ct);
                walker.Visit(syntaxRoot);

                metrics.AddRange(walker.Metrics.Where(metric => seen.Add(metric.SymbolId)));
            }
        }

        return metrics;
    }

    private sealed class MemberMetricWalker(SemanticModel semanticModel, IRoslynSymbolIdFactory symbolIdFactory, CancellationToken cancellationToken)
        : CSharpSyntaxWalker(SyntaxWalkerDepth.Node)
    {
        private readonly List<MetricItem> _metrics = new();
        private readonly HashSet<string> _seen = new(StringComparer.Ordinal);

        public IReadOnlyList<MetricItem> Metrics => _metrics;

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            CollectMetric(node);
            base.VisitMethodDeclaration(node);
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
        {
            CollectMetric(node);
            base.VisitConstructorDeclaration(node);
        }

        public override void VisitLocalFunctionStatement(LocalFunctionStatementSyntax node)
        {
            CollectMetric(node);
            base.VisitLocalFunctionStatement(node);
        }

        private void CollectMetric(SyntaxNode node)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbol = semanticModel.GetDeclaredSymbol(node, cancellationToken) as IMethodSymbol;
            if (symbol == null)
            {
                return;
            }

            if (!symbol.Locations.Any(location => location.IsInSource))
            {
                return;
            }

            var symbolId = symbolIdFactory.CreateId(symbol);
            if (!_seen.Add(symbolId))
            {
                return;
            }

            int? complexity = null;
            if (HasBody(node))
            {
                var walker = new CyclomaticComplexityWalker(cancellationToken);
                switch (node)
                {
                    case BaseMethodDeclarationSyntax method:
                        method.Body?.Accept(walker);
                        method.ExpressionBody?.Accept(walker);
                        break;

                    case LocalFunctionStatementSyntax local:
                        local.Body?.Accept(walker);
                        local.ExpressionBody?.Accept(walker);
                        break;
                }

                complexity = walker.Complexity;
            }

            var lineCount = ComputeLineCount(node);
            _metrics.Add(new MetricItem(symbolId, complexity, lineCount));
        }

        private static bool HasBody(SyntaxNode node)
            => node switch
            {
                BaseMethodDeclarationSyntax method => method.Body != null || method.ExpressionBody != null,
                LocalFunctionStatementSyntax local => local.Body != null || local.ExpressionBody != null,
                _ => false
            };

        private static int ComputeLineCount(SyntaxNode node)
        {
            var span = node.GetLocation().GetLineSpan();
            return span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
        }
    }

    private sealed class CyclomaticComplexityWalker(CancellationToken cancellationToken)
        : CSharpSyntaxWalker(SyntaxWalkerDepth.Node)
    {
        private int _count = 1;

        public int Complexity => Math.Max(_count, 1);

        public override void VisitIfStatement(IfStatementSyntax node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _count++;
            base.VisitIfStatement(node);
        }

        public override void VisitForStatement(ForStatementSyntax node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _count++;
            base.VisitForStatement(node);
        }

        public override void VisitForEachStatement(ForEachStatementSyntax node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _count++;
            base.VisitForEachStatement(node);
        }

        public override void VisitWhileStatement(WhileStatementSyntax node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _count++;
            base.VisitWhileStatement(node);
        }

        public override void VisitDoStatement(DoStatementSyntax node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _count++;
            base.VisitDoStatement(node);
        }

        public override void VisitSwitchSection(SwitchSectionSyntax node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _count += node.Labels.Count;
            base.VisitSwitchSection(node);
        }

        public override void VisitConditionalExpression(ConditionalExpressionSyntax node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _count++;
            base.VisitConditionalExpression(node);
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (node.IsKind(SyntaxKind.LogicalAndExpression) || node.IsKind(SyntaxKind.LogicalOrExpression))
            {
                _count++;
            }

            base.VisitBinaryExpression(node);
        }

        public override void VisitCatchClause(CatchClauseSyntax node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _count++;
            base.VisitCatchClause(node);
        }

        public override void VisitSwitchExpressionArm(SwitchExpressionArmSyntax node)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _count++;
            base.VisitSwitchExpressionArm(node);
        }

        public override void Visit(SyntaxNode? node)
        {
            if (node == null)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();
            base.Visit(node);
        }
    }
}
