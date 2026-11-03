using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace SourceGauge.Cli;

internal static class FanInMetricCollector
{
    private const string _metricId = "sourcegauge.fanin";

    internal static async Task<IReadOnlyList<MetricRecord>> CollectAsync(
        IReadOnlyList<Project> projects,
        string inputRoot,
        string scope)
    {
        ProjectAnalysis[] analyses = await CreateProjectAnalysesAsync(projects, inputRoot).ConfigureAwait(false);
        Dictionary<ProjectId, ProjectAnalysis> analysesById = analyses.ToDictionary(analysis => analysis.Project.Id);

        foreach (ProjectAnalysis analysis in analyses)
        {
            foreach (CallerBody caller in analysis.Callers)
            {
                foreach (IMethodSymbol calledMethod in GetCalledMethods(caller))
                {
                    if (TryResolveTarget(analysis, analysesById, calledMethod, out MetricTarget? target)
                        && target is not null
                        && !SymbolEqualityComparer.Default.Equals(NormalizeMethod(caller.Symbol), target.Symbol))
                    {
                        target.Callers.Add($"{analysis.ProjectId}|{GetMethodId(caller.Symbol)}");
                    }
                }
            }
        }

        return analyses
            .SelectMany(analysis => analysis.Targets.Values.Select(target => new MetricRecord(
                _metricId,
                scope,
                new MetricEntity(
                    target.Symbol.MethodKind == MethodKind.Constructor ? "constructor" : "method",
                    analysis.ProjectId,
                    GetMethodId(target.Symbol),
                    target.Symbol.Name),
                new MetricSource(target.SourcePath),
                target.Callers.Count,
                "count")))
            .ToArray();
    }

    private static async Task<ProjectAnalysis[]> CreateProjectAnalysesAsync(
        IReadOnlyList<Project> projects,
        string inputRoot)
    {
        ProjectAnalysis[] analyses = new ProjectAnalysis[projects.Count];
        Dictionary<string, int> contextsPerProject = projects
            .GroupBy(project => Path.GetFullPath(project.FilePath ?? project.Name), GetPathComparer())
            .ToDictionary(group => group.Key, group => group.Count(), GetPathComparer());

        for (int projectIndex = 0; projectIndex < projects.Count; projectIndex++)
        {
            Project project = projects[projectIndex];
            string projectPath = Path.GetFullPath(project.FilePath ?? throw new InvalidOperationException("Loaded project has no file path."));
            string projectId = NormalizeSeparators(Path.GetRelativePath(inputRoot, projectPath));
            if (contextsPerProject[projectPath] > 1)
            {
                projectId = $"{projectId}#{GetTargetFramework(project)}";
            }

            Compilation compilation = await project.GetCompilationAsync().ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Could not create a compilation for '{projectPath}'.");
            ProjectAnalysis analysis = new(project, compilation, projectId);
            analyses[projectIndex] = analysis;

            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                SyntaxNode root = await syntaxTree.GetRootAsync().ConfigureAwait(false);
                if (IsGenerated(syntaxTree, root))
                {
                    continue;
                }

                SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree);
                foreach (SyntaxNode declaration in root.DescendantNodes().Where(IsMethodDeclaration))
                {
                    IMethodSymbol? declaredMethod = semanticModel.GetDeclaredSymbol(declaration) as IMethodSymbol;
                    if (declaredMethod is null || IsGenerated(declaredMethod))
                    {
                        continue;
                    }

                    IMethodSymbol method = NormalizeMethod(declaredMethod);
                    if (IsEligibleTarget(method))
                    {
                        string sourcePath = GetSourcePath(method, inputRoot);
                        analysis.Targets.TryAdd(GetMethodId(method), new MetricTarget(method, sourcePath));
                    }

                    if (IsEligibleCaller(method) && HasExecutableBody(declaration))
                    {
                        analysis.Callers.Add(new CallerBody(method, semanticModel, declaration));
                    }
                }
            }
        }

        return analyses;
    }

    private static bool IsMethodDeclaration(SyntaxNode node)
    {
        return node is MethodDeclarationSyntax or ConstructorDeclarationSyntax;
    }

    private static bool HasExecutableBody(SyntaxNode declaration)
    {
        return declaration switch
        {
            MethodDeclarationSyntax method => method.Body is not null || method.ExpressionBody is not null,
            ConstructorDeclarationSyntax constructor => constructor.Body is not null,
            _ => false
        };
    }

    private static bool IsEligibleTarget(IMethodSymbol method)
    {
        return !method.IsImplicitlyDeclared
            && (method.MethodKind == MethodKind.Ordinary
                || (method.MethodKind == MethodKind.Constructor && !method.IsStatic));
    }

    private static bool IsEligibleCaller(IMethodSymbol method)
    {
        return !method.IsImplicitlyDeclared
            && (method.MethodKind == MethodKind.Ordinary
                || method.MethodKind is MethodKind.Constructor or MethodKind.StaticConstructor);
    }

    private static bool IsGenerated(SyntaxTree syntaxTree, SyntaxNode root)
    {
        string fileName = Path.GetFileName(syntaxTree.FilePath);
        if (fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".assemblyinfo.cs", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string leadingTrivia = root.GetFirstToken(includeZeroWidth: true).LeadingTrivia.ToFullString();
        return leadingTrivia.Contains("<auto-generated", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGenerated(ISymbol symbol)
    {
        for (ISymbol? current = symbol; current is not null; current = current.ContainingType)
        {
            if (current.GetAttributes().Any(attribute =>
                attribute.AttributeClass?.ToDisplayString() is
                    "System.CodeDom.Compiler.GeneratedCodeAttribute" or
                    "System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<IMethodSymbol> GetCalledMethods(CallerBody caller)
    {
        if (caller.Declaration is ConstructorDeclarationSyntax constructor && constructor.Initializer is not null)
        {
            if (caller.SemanticModel.GetSymbolInfo(constructor.Initializer).Symbol is IMethodSymbol initializerMethod)
            {
                yield return initializerMethod;
            }
        }

        SyntaxNode? body = caller.Declaration switch
        {
            MethodDeclarationSyntax method => method.Body ?? (SyntaxNode?)method.ExpressionBody?.Expression,
            ConstructorDeclarationSyntax constructorDeclaration => constructorDeclaration.Body,
            _ => null
        };
        if (body is null)
        {
            yield break;
        }

        foreach (SyntaxNode node in body.DescendantNodesAndSelf(
            descendant => descendant is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax))
        {
            IOperation? operation = node switch
            {
                InvocationExpressionSyntax => caller.SemanticModel.GetOperation(node),
                ObjectCreationExpressionSyntax => caller.SemanticModel.GetOperation(node),
                ImplicitObjectCreationExpressionSyntax => caller.SemanticModel.GetOperation(node),
                _ => null
            };

            switch (operation)
            {
                case IInvocationOperation invocation:
                    yield return invocation.TargetMethod;
                    break;
                case IObjectCreationOperation { Constructor: not null } objectCreation:
                    yield return objectCreation.Constructor;
                    break;
            }
        }
    }

    private static bool TryResolveTarget(
        ProjectAnalysis callerAnalysis,
        IReadOnlyDictionary<ProjectId, ProjectAnalysis> analysesById,
        IMethodSymbol calledMethod,
        out MetricTarget? target)
    {
        IMethodSymbol method = NormalizeMethod(calledMethod);
        if (SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, callerAnalysis.Compilation.Assembly)
            && TryFindTarget(callerAnalysis, method, out target))
        {
            return true;
        }

        foreach (ProjectAnalysis referencedAnalysis in GetReferencedAnalyses(callerAnalysis, analysesById))
        {
            if (!SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, referencedAnalysis.Compilation.Assembly))
            {
                continue;
            }

            if (TryFindTarget(referencedAnalysis, method, out target))
            {
                return true;
            }
        }

        target = null;
        return false;
    }

    private static IEnumerable<ProjectAnalysis> GetReferencedAnalyses(
        ProjectAnalysis analysis,
        IReadOnlyDictionary<ProjectId, ProjectAnalysis> analysesById)
    {
        Queue<ProjectId> pendingReferences = new(analysis.Project.ProjectReferences.Select(reference => reference.ProjectId));
        HashSet<ProjectId> visited = [];

        while (pendingReferences.TryDequeue(out ProjectId? projectId))
        {
            if (!visited.Add(projectId) || !analysesById.TryGetValue(projectId, out ProjectAnalysis? referencedAnalysis))
            {
                continue;
            }

            yield return referencedAnalysis;
            foreach (ProjectReference reference in referencedAnalysis.Project.ProjectReferences)
            {
                pendingReferences.Enqueue(reference.ProjectId);
            }
        }
    }

    private static bool TryFindTarget(ProjectAnalysis analysis, IMethodSymbol method, out MetricTarget? target)
    {
        return analysis.Targets.TryGetValue(GetMethodId(method), out target);
    }

    private static IMethodSymbol NormalizeMethod(IMethodSymbol method)
    {
        IMethodSymbol normalized = (method.ReducedFrom ?? method).OriginalDefinition;
        return normalized.PartialDefinitionPart ?? normalized;
    }

    private static string GetMethodId(IMethodSymbol method)
    {
        string documentationId = NormalizeMethod(method).GetDocumentationCommentId()
            ?? NormalizeMethod(method).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return documentationId.StartsWith("M:", StringComparison.Ordinal) ? documentationId[2..] : documentationId;
    }

    private static string GetSourcePath(IMethodSymbol method, string inputRoot)
    {
        string sourcePath = method.Locations.First(location => location.IsInSource).SourceTree!.FilePath;
        return NormalizeSeparators(Path.GetRelativePath(inputRoot, Path.GetFullPath(sourcePath)));
    }

    private static string GetTargetFramework(Project project)
    {
        string? outputFilePath = project.OutputFilePath;
        string? outputDirectory = outputFilePath is null ? null : Path.GetDirectoryName(outputFilePath);
        string? targetFramework = outputDirectory is null ? null : Path.GetFileName(outputDirectory);
        if (string.IsNullOrWhiteSpace(targetFramework))
        {
            throw new InvalidOperationException($"Could not determine target framework for '{project.FilePath}'.");
        }

        return targetFramework;
    }

    private static StringComparer GetPathComparer()
    {
        return OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
    }

    private static string NormalizeSeparators(string path)
    {
        return path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }

    private sealed class ProjectAnalysis(Project project, Compilation compilation, string projectId)
    {
        internal Project Project { get; } = project;

        internal Compilation Compilation { get; } = compilation;

        internal string ProjectId { get; } = projectId;

        internal Dictionary<string, MetricTarget> Targets { get; } = new(StringComparer.Ordinal);

        internal List<CallerBody> Callers { get; } = [];
    }

    private sealed record CallerBody(IMethodSymbol Symbol, SemanticModel SemanticModel, SyntaxNode Declaration);

    private sealed class MetricTarget(IMethodSymbol symbol, string sourcePath)
    {
        internal IMethodSymbol Symbol { get; } = symbol;

        internal string SourcePath { get; } = sourcePath;

        internal HashSet<string> Callers { get; } = new(StringComparer.Ordinal);
    }
}
