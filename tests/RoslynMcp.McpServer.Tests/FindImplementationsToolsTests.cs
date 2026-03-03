using RoslynMcp.Core;
using RoslynMcp.Core.Contracts;
using RoslynMcp.Core.Models.Common;
using RoslynMcp.Core.Models.Navigation;
using RoslynMcp.Infrastructure;
using RoslynMcp.Infrastructure.Workspace;
using RoslynMcp.McpServer.Tools;
using Is.Assertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace RoslynMcp.McpServer.Tests;

[CollectionDefinition("RoslynFindImplementations", DisableParallelization = true)]
public sealed class RoslynFindImplementationsCollectionDefinition
{
}

[Collection("RoslynFindImplementations")]
public sealed class FindImplementationsToolsTests
{
    [Fact]
    public async Task FindImplementations_ForInterface_ReturnsImplementingClasses()
    {
        var solution = CreateInterfaceImplementationSolution();
        var tool = CreateTool(solution);
        
        // Find IWorker interface symbol
        var navService = CreateNavigationService(solution);
        var search = await navService.SearchSymbolsAsync(
            new SearchSymbolsRequest("IWorker"), 
            CancellationToken.None);
        
        search.Error.IsNull();
        search.Symbols.Count.IsGreaterThan(0);
        var workerInterface = search.Symbols.First();
        workerInterface.Name.Is("IWorker");
        
        // Now find implementations
        var result = await tool.FindImplementationsAsync(
            CancellationToken.None,
            workerInterface.SymbolId);
        
        result.Error.IsNull();
        result.Symbol.IsNotNull();
        result.Symbol.Name.Is("IWorker");
        
        // Should find WorkerA and WorkerB as implementations
        (result.Implementations.Count >= 2).IsTrue();
        result.Implementations.Any(i => i.Name == "WorkerA").IsTrue();
        result.Implementations.Any(i => i.Name == "WorkerB").IsTrue();
    }

    [Fact]
    public async Task FindImplementations_ForAbstractMethod_ReturnsOverridingMethods()
    {
        var solution = CreateInterfaceImplementationSolution();
        var tool = CreateTool(solution);
        var navService = CreateNavigationService(solution);
        
        // Find the abstract Work method
        var search = await navService.SearchSymbolsAsync(
            new SearchSymbolsRequest("Work"), 
            CancellationToken.None);
        
        search.Error.IsNull();
        search.Symbols.Count.IsGreaterThan(0);
        var abstractMethod = search.Symbols.First(s => s.ContainingType?.Contains("IWorker") ?? false);
        
        var result = await tool.FindImplementationsAsync(
            CancellationToken.None,
            abstractMethod.SymbolId);
        
        result.Error.IsNull();
        result.Symbol.IsNotNull();
        
        // Should find implementing methods
        result.Implementations.Count.IsGreaterThan(0);
    }

    [Fact]
    public async Task FindImplementations_WithInvalidSymbol_ReturnsError()
    {
        var solution = CreateInterfaceImplementationSolution();
        var tool = CreateTool(solution);
        
        var result = await tool.FindImplementationsAsync(
            CancellationToken.None,
            "invalid-symbol-id");
        
        result.Error.IsNotNull();
    }

    private static Solution CreateInterfaceImplementationSolution()
    {
        var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("ImplementationProject", LanguageNames.CSharp)
            .WithMetadataReferences(new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            });

        var code = """
namespace Workers;

public interface IWorker
{
    void Work();
}

public class WorkerA : IWorker
{
    public void Work() { }
}

public class WorkerB : IWorker
{
    public void Work() { }
}
""";

        var document = project.AddDocument("Workers.cs", SourceText.From(code), filePath: "Workers.cs");
        return document.Project.Solution;
    }

    private static FindImplementationsTools CreateTool(Solution solution)
    {
        var services = new ServiceCollection();
        services.AddRoslynMcpMcpServer();
        services.AddSingleton<IRoslynSolutionAccessor>(new TestSolutionAccessor(solution));
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<FindImplementationsTools>();
    }

    private static INavigationService CreateNavigationService(Solution solution)
    {
        var services = new ServiceCollection();
        services.AddRoslynMcpMcpServer();
        services.AddSingleton<IRoslynSolutionAccessor>(new TestSolutionAccessor(solution));
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<INavigationService>();
    }

    private sealed class TestSolutionAccessor : IRoslynSolutionAccessor
    {
        private readonly Solution _solution;

        public TestSolutionAccessor(Solution solution)
        {
            _solution = solution;
        }

        public Task<(Solution? Solution, ErrorInfo? Error)> GetCurrentSolutionAsync(CancellationToken ct)
            => Task.FromResult(((Solution?)_solution, (ErrorInfo?)null));

        public Task<(bool Applied, ErrorInfo? Error)> TryApplySolutionAsync(Solution solution, CancellationToken ct)
            => Task.FromResult(((bool)true, (ErrorInfo?)null));

        public Task<(int Version, ErrorInfo? Error)> GetWorkspaceVersionAsync(CancellationToken ct)
            => Task.FromResult((1, (ErrorInfo?)null));
    }
}
