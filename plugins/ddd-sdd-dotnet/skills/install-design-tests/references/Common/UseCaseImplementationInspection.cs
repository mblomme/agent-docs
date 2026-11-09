using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Company.BoundedContext.DesignTests.Common;

internal static class UseCaseImplementationInspection
{
    internal sealed record Violation(string Rule, string Message);

    public static IEnumerable<Violation> Checkout(string assemblyName)
    {
        string? project = null;
        var errors = DomainProjectInspection
            .Project(
                AppContext.BaseDirectory,
                typeof(UseCaseImplementationInspection)
                    .Assembly.GetName()
                    .Name!,
                assemblyName,
                path => project = path,
                "Application"
            )
            .ToArray();
        foreach (var error in errors)
            yield return new("analysis", error);
        if (project is null)
            yield break;
        var compilation = ApplicationSourceCompilation.Load(
            project,
            assemblyName
        );
        foreach (
            var violation in InspectApplication(
                compilation,
                assemblyName,
                AssemblyDiscovery.ToolkitDomainAssembly(
                    AssemblyDiscovery.ProductionAssemblies(
                        typeof(UseCaseImplementationInspection).Assembly
                    )
                )
            )
        )
            yield return violation;
    }

    public static IReadOnlyList<Violation> InspectApplication(
        CSharpCompilation compilation,
        string applicationAssemblyName,
        Assembly domain
    )
    {
        var repository = AggregateContractInspection.SharedType(
            domain,
            "IRepository"
        );
        var aggregate = AggregateContractInspection.SharedType(
            domain,
            "IAggregateRoot"
        );
        return Inspect(
            compilation,
            "DddToolkit.Application.Contracts.Driving",
            repository.FullName!,
            "DddToolkit.Application.Contracts.Driven.IUnitOfWork",
            aggregate.FullName!
        );
    }

    public static IReadOnlyList<Violation> Inspect(
        CSharpCompilation compilation,
        string drivingNamespace,
        string repositoryName,
        string unitOfWorkName,
        string aggregateName
    )
    {
        var violations = new List<Violation>();
        foreach (
            var error in compilation
                .GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
        )
            violations.Add(new("analysis", error.ToString()));
        if (violations.Count != 0)
            return violations;
        var repository = compilation.GetTypeByMetadataName(repositoryName);
        var unitOfWork = compilation.GetTypeByMetadataName(unitOfWorkName);
        var aggregate = compilation.GetTypeByMetadataName(aggregateName);
        if (repository is null || unitOfWork is null || aggregate is null)
            return
            [
                new(
                    "analysis",
                    "Cannot resolve repository, aggregate, or unit-of-work contracts."
                ),
            ];
        var types = compilation
            .SyntaxTrees.SelectMany(tree =>
                tree.GetRoot()
                    .DescendantNodes()
                    .OfType<TypeDeclarationSyntax>()
                    .Select(node =>
                        compilation
                            .GetSemanticModel(tree)
                            .GetDeclaredSymbol(node)
                    )
            )
            .OfType<INamedTypeSymbol>()
            .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (
            var type in types.Where(t =>
                t.TypeKind == TypeKind.Class && !t.IsAbstract
            )
        )
        {
            var ports = type
                .AllInterfaces.Where(i =>
                    i.Name == "IUseCase"
                    && i.Arity is 1 or 2
                    && i.ContainingNamespace.ToDisplayString()
                        == drivingNamespace
                )
                .ToArray();
            if (ports.Length == 0)
                continue;
            void Report(string rule, ISymbol symbol, string message) =>
                violations.Add(
                    new(
                        rule,
                        $"{type.ToDisplayString()}: {symbol.ToDisplayString()} ({symbol.Locations.FirstOrDefault()?.GetLineSpan()}): {message}"
                    )
                );
            for (
                var current = type;
                current is not null;
                current = current.BaseType
            )
                foreach (var constructor in current.InstanceConstructors)
                foreach (
                    var parameter in constructor.Parameters.Where(p =>
                        Implements(p.Type, repository)
                    )
                )
                    Report(
                        "injection",
                        constructor,
                        "Do not inject repositories. Obtain them through IUnitOfWork.Repo<TRepository>()."
                    );
            foreach (
                var method in ports
                    .SelectMany(p => p.GetMembers("Execute"))
                    .Select(type.FindImplementationForInterfaceMember)
                    .OfType<IMethodSymbol>()
                    .Distinct<IMethodSymbol>(SymbolEqualityComparer.Default)
            )
            {
                var analyzer = new ExecutionAnalysis(
                    compilation,
                    unitOfWork,
                    aggregate,
                    (rule, message) => Report(rule, method, message)
                );
                var results = analyzer.Run(method, new State(), [], []);
                if (results.Any(state => !state.Committed))
                    Report(
                        "commit",
                        method,
                        "Every successful path must complete IUnitOfWork.Do() after its last Save, before returning success."
                    );
            }
        }
        return violations.Distinct().ToArray();
    }

    private static bool Implements(ITypeSymbol type, ITypeSymbol contract) =>
        SymbolEqualityComparer.Default.Equals(type, contract)
        || type.AllInterfaces.Any(i =>
            SymbolEqualityComparer.Default.Equals(i, contract)
        );

    private sealed class State
    {
        public HashSet<string> Saved { get; } = [];
        public Dictionary<ISymbol, ITypeSymbol> Types { get; } =
            new(SymbolEqualityComparer.Default);
        public Dictionary<CaptureId, ITypeSymbol> Captures { get; } = [];
        public Dictionary<IOperation, ITypeSymbol> Values { get; } = [];
        public bool Committed { get; set; }
        public ITypeSymbol? Returned { get; set; }

        public State Copy()
        {
            var copy = new State { Committed = Committed, Returned = Returned };
            copy.Saved.UnionWith(Saved);
            foreach (var pair in Types)
                copy.Types[pair.Key] = pair.Value;
            foreach (var pair in Captures)
                copy.Captures[pair.Key] = pair.Value;
            foreach (var pair in Values)
                copy.Values[pair.Key] = pair.Value;
            return copy;
        }

        public string Key(int block) =>
            block
            + ":"
            + Committed
            + ":"
            + string.Join(",", Saved.Order())
            + ":"
            + string.Join(
                ",",
                Types
                    .Select(p =>
                        p.Key.ToDisplayString()
                        + "="
                        + p.Value.ToDisplayString()
                    )
                    .Order()
            )
            + ":"
            + string.Join(
                ",",
                Captures
                    .Select(p =>
                        p.Key.GetHashCode() + "=" + p.Value.ToDisplayString()
                    )
                    .Order()
            )
            + ":"
            + Returned?.ToDisplayString();
    }

    private sealed class ExecutionAnalysis(
        CSharpCompilation compilation,
        INamedTypeSymbol unitOfWork,
        INamedTypeSymbol aggregate,
        Action<string, string> report
    )
    {
        private readonly Dictionary<
            IMethodSymbol,
            ControlFlowGraph
        > localGraphs = new(SymbolEqualityComparer.Default);

        private bool NeedsPersistenceAnalysis(
            IMethodSymbol method,
            HashSet<IMethodSymbol> visiting
        )
        {
            if (Implements(method.ContainingType, unitOfWork))
                return method.Name is "Save" or "Do" or "Repo";
            if (
                method.ContainingType.TypeKind == TypeKind.Interface
                && method.IsAbstract
            )
                return false;
            var syntax = method
                .DeclaringSyntaxReferences.FirstOrDefault()
                ?.GetSyntax();
            if (syntax is null)
                return false;
            var result = method.ReturnType;
            if (
                result is INamedTypeSymbol
                {
                    Name: "Task" or "ValueTask",
                    TypeArguments.Length: 1
                } task
            )
                result = task.TypeArguments[0];
            if (Implements(result, aggregate) || result is ITypeParameterSymbol)
                return true;
            if (!visiting.Add(method.OriginalDefinition))
                return true;
            var model = compilation.GetSemanticModel(syntax.SyntaxTree);
            foreach (
                var call in syntax
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
            )
            {
                if (
                    model.GetOperation(call)
                    is not IInvocationOperation invocation
                )
                    return true;
                if (
                    invocation.TargetMethod.MethodKind
                        == MethodKind.DelegateInvoke
                    || NeedsPersistenceAnalysis(
                        invocation.TargetMethod,
                        new HashSet<IMethodSymbol>(
                            visiting,
                            SymbolEqualityComparer.Default
                        )
                    )
                )
                    return true;
            }
            return false;
        }

        public List<State> Run(
            IMethodSymbol method,
            State initial,
            ITypeSymbol?[] arguments,
            HashSet<IMethodSymbol> stack
        )
        {
            if (!stack.Add(method.OriginalDefinition))
            {
                report(
                    "analysis",
                    $"Cannot resolve recursive helper {method}; make persistence orchestration explicit."
                );
                return [];
            }
            var syntax = method
                .DeclaringSyntaxReferences.FirstOrDefault()
                ?.GetSyntax();
            if (syntax is null)
            {
                report(
                    "analysis",
                    $"Cannot inspect implementation {method} from source."
                );
                return [];
            }
            ControlFlowGraph? graph;
            try
            {
                graph = localGraphs.TryGetValue(
                    method.OriginalDefinition,
                    out var localGraph
                )
                    ? localGraph
                    : ControlFlowGraph.Create(
                        syntax,
                        compilation.GetSemanticModel(syntax.SyntaxTree)
                    );
            }
            catch (ArgumentException)
            {
                report(
                    "analysis",
                    $"Cannot build control flow for {method}; make persistence orchestration explicit."
                );
                return [];
            }
            if (graph is null)
            {
                report("analysis", $"Cannot build control flow for {method}.");
                return [];
            }
            foreach (var local in graph.LocalFunctions)
                localGraphs[local.OriginalDefinition] =
                    graph.GetLocalFunctionControlFlowGraph(local);
            if (syntax.DescendantNodes().OfType<CatchClauseSyntax>().Any())
                report(
                    "analysis",
                    $"Cannot prove exception-handler persistence paths in {method}; make persistence orchestration explicit."
                );
            for (
                var i = 0;
                i < arguments.Length && i < method.Parameters.Length;
                i++
            )
                if (arguments[i] is { } argument)
                    initial.Types[method.Parameters[i]] = argument;
            var pending = new Queue<(BasicBlock Block, State State)>();
            pending.Enqueue((graph.Blocks[0], initial));
            var seen = new HashSet<string>();
            var results = new List<State>();
            while (pending.TryDequeue(out var item))
            {
                if (
                    !item.Block.IsReachable
                    || !seen.Add(item.State.Key(item.Block.Ordinal))
                )
                    continue;
                if (item.Block.Kind == BasicBlockKind.Exit)
                {
                    results.Add(item.State);
                    continue;
                }
                var states = new List<State> { item.State };
                foreach (var operation in item.Block.Operations)
                    states = states
                        .SelectMany(s => Evaluate(operation, s, stack))
                        .ToList();
                if (item.Block.BranchValue is { } value)
                {
                    states = states
                        .SelectMany(s =>
                            Evaluate(
                                value,
                                s,
                                stack,
                                item.Block.FallThroughSuccessor?.Semantics
                                    == ControlFlowBranchSemantics.Return
                            )
                        )
                        .ToList();
                    if (
                        item.Block.FallThroughSuccessor?.Semantics
                        == ControlFlowBranchSemantics.Return
                    )
                        foreach (var state in states)
                            state.Returned = Resolve(value, state);
                }
                foreach (
                    var successor in new[]
                    {
                        item.Block.FallThroughSuccessor,
                        item.Block.ConditionalSuccessor,
                    }
                )
                {
                    if (
                        successor?.FinallyRegions.Length > 0
                        && syntax
                            .DescendantNodes()
                            .Any(node =>
                                node
                                    is TryStatementSyntax { Finally: not null }
                                        or UsingStatementSyntax
                                        or LocalDeclarationStatementSyntax
                                        {
                                            UsingKeyword.RawKind: not 0
                                        }
                            )
                    )
                    {
                        report(
                            "analysis",
                            $"Cannot resolve finally flow in {method}; move persistence outside try/finally."
                        );
                        continue;
                    }
                    if (successor?.Destination is not { } destination)
                        continue;
                    foreach (var state in states)
                        pending.Enqueue((destination, state.Copy()));
                }
            }
            return results;
        }

        private List<State> Evaluate(
            IOperation operation,
            State state,
            HashSet<IMethodSymbol> stack,
            bool returned = false
        )
        {
            if (
                operation
                is IAnonymousFunctionOperation
                    or IFlowAnonymousFunctionOperation
                    or ILocalFunctionOperation
            )
                return [state];
            var states = new List<State> { state };
            foreach (var child in operation.ChildOperations)
                states = states
                    .SelectMany(s =>
                        Evaluate(
                            child,
                            s,
                            stack,
                            returned && operation is IConversionOperation
                        )
                    )
                    .ToList();
            foreach (var current in states)
            {
                if (
                    operation is ISimpleAssignmentOperation assignment
                    && Resolve(assignment.Value, current) is { } assigned
                )
                {
                    var target = assignment.Target switch
                    {
                        ILocalReferenceOperation local => (ISymbol)local.Local,
                        IParameterReferenceOperation parameter =>
                            parameter.Parameter,
                        _ => null,
                    };
                    if (target is not null)
                        current.Types[target] = assigned;
                }
                if (
                    operation is IFlowCaptureOperation capture
                    && Resolve(capture.Value, current) is { } captured
                )
                    current.Captures[capture.Id] = captured;
            }
            if (operation is not IInvocationOperation invocation)
                return states;
            var targetMethod = invocation.TargetMethod;
            if (Implements(targetMethod.ContainingType, unitOfWork))
            {
                foreach (var current in states)
                {
                    if (targetMethod.Name == "Save")
                    {
                        var type =
                            invocation.Arguments.Length == 0
                                ? null
                                : Resolve(
                                    invocation.Arguments[0].Value,
                                    current
                                );
                        if (
                            type is null
                            || type.TypeKind
                                is TypeKind.Interface
                                    or TypeKind.TypeParameter
                            || !Implements(type, aggregate)
                        )
                            report(
                                "analysis",
                                $"Cannot resolve aggregate type at {invocation.Syntax.GetLocation().GetLineSpan()}; use an explicit concrete aggregate type."
                            );
                        else
                            current.Saved.Add(type.ToDisplayString());
                        current.Committed = false;
                        if (current.Saved.Count > 1)
                            report(
                                "save",
                                "One execution saves multiple aggregate types: "
                                    + string.Join(", ", current.Saved.Order())
                                    + ". Split saves into separate use cases connected by a domain event and policy."
                            );
                    }
                    if (targetMethod.Name == "Do")
                    {
                        IOperation parent = invocation;
                        while (parent.Parent is IConversionOperation)
                            parent = parent.Parent;
                        if (
                            parent.Parent is IInvocationOperation configure
                            && configure.TargetMethod.Name == "ConfigureAwait"
                        )
                            parent = configure;
                        var completed =
                            parent.Parent is IAwaitOperation || returned;
                        if (!completed)
                            report(
                                "commit",
                                "Await IUnitOfWork.Do() or return its Task directly; an unawaited commit does not complete the transaction."
                            );
                        current.Committed = completed;
                    }
                }
                return states;
            }
            if (targetMethod.MethodKind == MethodKind.DelegateInvoke)
                report(
                    "analysis",
                    $"Cannot resolve delegate invocation at {invocation.Syntax.GetLocation().GetLineSpan()}; use a source helper for persistence orchestration."
                );
            if (targetMethod.DeclaringSyntaxReferences.Length == 0)
                return states;
            // Abstract port declarations have no control flow. Their adapters
            // are outside this compilation; source helpers and default interface
            // implementations must still be inspected below.
            if (
                targetMethod.ContainingType.TypeKind == TypeKind.Interface
                && targetMethod.IsAbstract
            )
                return states;
            if (
                targetMethod.IsVirtual
                && !targetMethod.IsSealed
                && !targetMethod.ContainingType.IsSealed
            )
            {
                report(
                    "analysis",
                    $"Cannot resolve dynamic helper dispatch {targetMethod}; make persistence orchestration explicit."
                );
                return states;
            }
            // Expand source helpers only when they can reach a persistence call
            // or determine the concrete aggregate returned to one. Pure mapping
            // loops otherwise multiply identical persistence paths exponentially.
            // Unknown dispatch and recursive helpers remain fail-closed above.
            if (
                !NeedsPersistenceAnalysis(
                    targetMethod,
                    new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default)
                )
            )
                return states;
            IOperation call = invocation;
            while (call.Parent is IConversionOperation)
                call = call.Parent;
            if (
                call.Parent is IInvocationOperation configured
                && configured.TargetMethod.Name == "ConfigureAwait"
            )
                call = configured;
            if (
                targetMethod.ReturnType.Name == "Task"
                && call.Parent is not IAwaitOperation
                && !returned
            )
                report(
                    "analysis",
                    $"Cannot prove completion of unawaited helper {targetMethod}; await its Task or return it directly."
                );
            var results = new List<State>();
            foreach (var current in states)
            {
                var arguments = new ITypeSymbol?[
                    targetMethod.Parameters.Length
                ];
                foreach (var argument in invocation.Arguments)
                    if (argument.Parameter is { } parameter)
                        arguments[parameter.Ordinal] = Resolve(
                            argument.Value,
                            current
                        );
                var callerCaptures = current.Captures.ToArray();
                var callerReturn = current.Returned;
                current.Returned = null;
                foreach (
                    var result in Run(
                        targetMethod,
                        current,
                        arguments,
                        new HashSet<IMethodSymbol>(
                            stack,
                            SymbolEqualityComparer.Default
                        )
                    )
                )
                {
                    if (result.Returned is { } resultType)
                        result.Values[invocation] = resultType;
                    result.Returned = callerReturn;
                    result.Captures.Clear();
                    foreach (var pair in callerCaptures)
                        result.Captures[pair.Key] = pair.Value;
                    results.Add(result);
                }
            }
            return results;
        }

        private static ITypeSymbol? Resolve(
            IOperation operation,
            State state
        ) =>
            operation switch
            {
                IConversionOperation conversion => Resolve(
                    conversion.Operand,
                    state
                ),
                IAwaitOperation awaited => Resolve(awaited.Operation, state)
                    is INamedTypeSymbol
                    {
                        Name: "Task" or "ValueTask",
                        TypeArguments.Length: 1
                    } taskType
                    ? taskType.TypeArguments[0]
                    : Resolve(awaited.Operation, state),
                ILocalReferenceOperation local
                    when state.Types.TryGetValue(local.Local, out var type) =>
                    type,
                IParameterReferenceOperation parameter
                    when state.Types.TryGetValue(
                        parameter.Parameter,
                        out var type
                    ) => type,
                IFlowCaptureReferenceOperation capture
                    when state.Captures.TryGetValue(capture.Id, out var type) =>
                    type,
                IInvocationOperation invocation
                    when state.Values.TryGetValue(invocation, out var type) =>
                    type,
                _ => operation.Type,
            };
    }
}
