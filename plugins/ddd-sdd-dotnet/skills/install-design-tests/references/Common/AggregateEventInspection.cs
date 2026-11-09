using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Company.BoundedContext.DesignTests.Common;

internal static class AggregateEventInspection
{
    public static IEnumerable<string> MissingDomainEvents(
        IEnumerable<Type> aggregateRoots,
        IEnumerable<MethodInfo> eventRecordingMethods,
        Func<MethodInfo, bool> isQuery
    )
    {
        var sinks = eventRecordingMethods
            .Select(m => (m.Module.ModuleVersionId, m.MetadataToken))
            .ToHashSet();
        foreach (var root in aggregateRoots)
        {
            using var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(
                Path.GetDirectoryName(root.Assembly.Location)!
            );
            using var module = ModuleDefinition.ReadModule(
                root.Module.FullyQualifiedName,
                new ReaderParameters { AssemblyResolver = resolver }
            );
            const BindingFlags flags =
                BindingFlags.Public
                | BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.DeclaredOnly;
            var operations = root.GetMethods(flags)
                .Where(m =>
                    !m.Name.StartsWith("get_", StringComparison.Ordinal)
                    && m.Name != "Reconstitute"
                    && !isQuery(m)
                )
                .Cast<MethodBase>()
                .Concat(root.GetConstructors(flags));
            foreach (var operation in operations)
            {
                var definition = (MethodDefinition)
                    module.LookupToken(operation.MetadataToken);
                if (
                    !CanReachEventRecording(
                        definition,
                        sinks,
                        new HashSet<(Guid, int)>()
                    )
                )
                    yield return $"{root.Name}.{(operation.IsConstructor ? ".ctor" : operation.Name)}({string.Join(", ", operation.GetParameters().Select(p => p.ParameterType.Name))}): no call to a configured domain-event recording method was found. Create and record a past-tense domain event for this state change, even when it is not described in the specification, to support future extensibility.";
            }
        }
    }

    private static bool CanReachEventRecording(
        MethodDefinition method,
        HashSet<(Guid, int)> sinks,
        HashSet<(Guid, int)> visited
    )
    {
        var identity = (method.Module.Mvid, method.MetadataToken.ToInt32());
        if (sinks.Contains(identity))
            return true;
        if (!visited.Add(identity) || !method.HasBody)
            return false;
        foreach (var instruction in method.Body.Instructions)
        {
            if (
                instruction.OpCode.Code
                    is not (Code.Call or Code.Callvirt or Code.Newobj)
                || instruction.Operand is not MethodReference reference
            )
                continue;
            // Follow domain-local helpers and constructors; do not inspect framework internals.
            if (
                reference.DeclaringType.GetElementType().Scope.Name
                != method.Module.Name
            )
                continue;
            var target = reference.Resolve();
            if (
                target is not null
                && CanReachEventRecording(target, sinks, visited)
            )
                return true;
        }
        return false;
    }
}
