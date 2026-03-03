using RoslynMcp.Core;
using RoslynMcp.Core.Models.Agent;
using Microsoft.CodeAnalysis;

namespace RoslynMcp.Infrastructure.Agent.Handlers;

internal sealed class ListDependenciesHandler
{
    private readonly CodeUnderstandingQueryService _queries;

    public ListDependenciesHandler(CodeUnderstandingQueryService queries)
    {
        _queries = queries;
    }

    public async Task<ListDependenciesResult> HandleAsync(ListDependenciesRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (solution, solutionError) = await _queries.GetCurrentSolutionWithAutoBootstrapAsync(
            "Call load_solution first to list dependencies.",
            request.ProjectPath,
            ct).ConfigureAwait(false);
        if (solution == null)
        {
            return new ListDependenciesResult(
                Array.Empty<ProjectDependency>(),
                0,
                AgentErrorInfo.Normalize(solutionError, "Call load_solution first to list dependencies."),
                Array.Empty<ProjectDependencyEdge>());
        }

        if (!CodeUnderstandingQueryService.TryNormalizeDependencyDirection(request.Direction, out var direction))
        {
            return new ListDependenciesResult(
                Array.Empty<ProjectDependency>(),
                0,
                AgentErrorInfo.Create(
                    ErrorCodes.InvalidInput,
                    $"direction '{request.Direction}' is not valid.",
                    "Use 'outgoing', 'incoming', or 'both'.",
                    ("field", "direction"),
                    ("provided", request.Direction ?? string.Empty)),
                Array.Empty<ProjectDependencyEdge>());
        }

        var hasProjectPath = !string.IsNullOrWhiteSpace(request.ProjectPath);
        var hasProjectName = !string.IsNullOrWhiteSpace(request.ProjectName);
        var hasProjectId = !string.IsNullOrWhiteSpace(request.ProjectId);
        var selectorCount = (hasProjectPath ? 1 : 0) + (hasProjectName ? 1 : 0) + (hasProjectId ? 1 : 0);
        var selectorProvided = selectorCount == 1;

        if (selectorCount > 1)
        {
            return new ListDependenciesResult(
                Array.Empty<ProjectDependency>(),
                0,
                AgentErrorInfo.Create(
                    ErrorCodes.InvalidInput,
                    "Multiple project selectors provided. Provide exactly one of projectPath, projectName, or projectId.",
                    "Specify only one selector to identify the target project.",
                    ("selectors", $"projectPath:{hasProjectPath}, projectName:{hasProjectName}, projectId:{hasProjectId}")),
                Array.Empty<ProjectDependencyEdge>());
        }

        var normalizedProjectName = CodeUnderstandingQueryService.NormalizeOptional(request.ProjectName);
        if (normalizedProjectName != null)
        {
            var matchingByName = solution.Projects
                .Where(p => string.Equals(p.Name, normalizedProjectName, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matchingByName.Length > 1)
            {
                return new ListDependenciesResult(
                    Array.Empty<ProjectDependency>(),
                    0,
                    AgentErrorInfo.Create(
                        ErrorCodes.AmbiguousSymbol,
                        $"projectName '{request.ProjectName}' matched {matchingByName.Length} projects.",
                        "Use projectPath or projectId to disambiguate.",
                        ("field", "projectName"),
                        ("provided", normalizedProjectName),
                        ("matchingCount", matchingByName.Length.ToString(System.Globalization.CultureInfo.InvariantCulture))),
                    Array.Empty<ProjectDependencyEdge>());
            }
        }

        var selectedProjects = CodeUnderstandingQueryService.ResolveProjectSelector(
            solution,
            request.ProjectPath,
            request.ProjectName,
            request.ProjectId,
            selectorRequired: false,
            toolName: "list_dependencies",
            out var selectorError);

        if (selectorProvided && selectorError != null)
        {
            return new ListDependenciesResult(
                Array.Empty<ProjectDependency>(),
                0,
                selectorError,
                Array.Empty<ProjectDependencyEdge>());
        }

        var targetProject = selectorProvided ? selectedProjects[0] : null;
        var edgeByKey = new Dictionary<string, ProjectDependencyEdge>(StringComparer.Ordinal);
        var dependencyById = new Dictionary<string, ProjectDependency>(StringComparer.Ordinal);

        if (targetProject != null)
        {
            if (direction == "outgoing" || direction == "both")
            {
                foreach (var reference in targetProject.ProjectReferences.OrderBy(static r => r.ProjectId.Id.ToString(), StringComparer.Ordinal))
                {
                    var dependencyProject = solution.GetProject(reference.ProjectId);
                    if (dependencyProject == null)
                    {
                        continue;
                    }

                    AddDependencyEdge(targetProject, dependencyProject, edgeByKey, dependencyById, counterpart: dependencyProject);
                }
            }

            if (direction == "incoming" || direction == "both")
            {
                foreach (var project in solution.Projects)
                {
                    if (project.ProjectReferences.Any(r => r.ProjectId == targetProject.Id))
                    {
                        AddDependencyEdge(project, targetProject, edgeByKey, dependencyById, counterpart: project);
                    }
                }
            }
        }
        else
        {
            var allReferenceEdges = new List<(Project Source, Project Target)>();
            foreach (var project in solution.Projects)
            {
                foreach (var reference in project.ProjectReferences)
                {
                    var dependencyProject = solution.GetProject(reference.ProjectId);
                    if (dependencyProject != null)
                    {
                        allReferenceEdges.Add((project, dependencyProject));
                    }
                }
            }

            if (direction == "outgoing" || direction == "both")
            {
                foreach (var (source, target) in allReferenceEdges)
                {
                    AddDependencyEdge(source, target, edgeByKey, dependencyById, counterpart: target);
                }
            }

            if (direction == "incoming" || direction == "both")
            {
                foreach (var (source, target) in allReferenceEdges)
                {
                    AddDependencyEdge(target, source, edgeByKey, dependencyById, counterpart: source);
                }
            }
        }

        var orderedEdges = edgeByKey.Values
            .OrderBy(static edge => edge.Source.ProjectName, StringComparer.Ordinal)
            .ThenBy(static edge => edge.Source.ProjectId, StringComparer.Ordinal)
            .ThenBy(static edge => edge.Target.ProjectName, StringComparer.Ordinal)
            .ThenBy(static edge => edge.Target.ProjectId, StringComparer.Ordinal)
            .ToArray();

        var dependencies = dependencyById.Values
            .OrderBy(static dependency => dependency.ProjectName, StringComparer.Ordinal)
            .ThenBy(static dependency => dependency.ProjectId, StringComparer.Ordinal)
            .ToArray();

        return new ListDependenciesResult(dependencies, dependencies.Length, null, orderedEdges);
    }

    private static void AddDependencyEdge(
        Project source,
        Project target,
        IDictionary<string, ProjectDependencyEdge> edgeByKey,
        IDictionary<string, ProjectDependency> dependencyById,
        Project counterpart)
    {
        var sourceDependency = ToProjectDependency(source);
        var targetDependency = ToProjectDependency(target);
        var edgeKey = $"{sourceDependency.ProjectId}->{targetDependency.ProjectId}";
        edgeByKey[edgeKey] = new ProjectDependencyEdge(sourceDependency, targetDependency);

        var counterpartDependency = ToProjectDependency(counterpart);
        dependencyById[counterpartDependency.ProjectId] = counterpartDependency;
    }

    private static ProjectDependency ToProjectDependency(Project project)
        => new(project.Name, project.Id.Id.ToString());
}
