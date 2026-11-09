using System.Reflection;
using System.Reflection.Emit;

namespace Company.BoundedContext.DesignTests.Common;

internal static class DomainEventContractInspection
{
    public static IEnumerable<string> DomainException(Type type)
    {
        if (
            !type.IsClass
            || !type.IsVisible
            || type.IsAbstract
            || type.IsGenericType
            || type.BaseType != typeof(Exception)
        )
            yield return $"{type.Name}: expected a public, concrete, non-generic DomainException class directly extending System.Exception in DddToolkit.Domain.";
    }

    public static IEnumerable<string> Interface(Type type)
    {
        if (!type.IsInterface || !type.IsVisible || type.IsGenericType)
            yield return $"{type.Name}: expected a public, non-generic IDomainEvent interface in DddToolkit.Domain.";

        foreach (
            var (name, propertyType) in new[]
            {
                ("FQDN", typeof(string)),
                ("OccurredOn", typeof(DateTimeOffset)),
            }
        )
        {
            var properties = type.GetProperties(
                    BindingFlags.Public | BindingFlags.Instance
                )
                .Where(property => property.Name == name)
                .ToArray();
            if (
                properties.Length != 1
                || properties[0].PropertyType != propertyType
                || properties[0].GetMethod
                    is not { IsPublic: true, IsStatic: false } getter
                || getter.GetParameters().Length != 0
                || properties[0].SetMethod is not null
                || properties[0].GetIndexParameters().Length != 0
            )
                yield return $"{type.Name}.{name}: expected a getter-only {propertyType.Name} {name} property declared by the interface.";
        }
    }

    public static IEnumerable<string> BaseEvent(
        Type type,
        Type domainEventInterface
    )
    {
        if (
            !type.IsClass
            || !type.IsVisible
            || !type.IsAbstract
            || type.IsSealed
            || type.IsGenericType
        )
            yield return $"{type.Name}: expected a public, abstract, non-generic, inheritable BaseDomainEvent class in DddToolkit.Domain.";

        if (!domainEventInterface.IsAssignableFrom(type))
            yield return $"{type.Name}: implement the shared IDomainEvent interface.";
    }

    public static Type[] AggregateRoots(
        Assembly domain,
        Type aggregateRootBase
    ) =>
        domain
            .GetTypes()
            .Where(type =>
                type.IsClass
                && !type.IsAbstract
                && InheritsFromAggregateRoot(type, aggregateRootBase)
            )
            .ToArray();

    public static IEnumerable<string> AggregateEventBases(
        Assembly domain,
        IEnumerable<Type> aggregateRoots,
        Type baseDomainEvent,
        Type domainEventInterface
    )
    {
        var roots = aggregateRoots.ToArray();
        foreach (var root in roots)
        {
            var expectedName = AggregateName(root) + "DomainEvent";
            var candidates = domain
                .GetTypes()
                .Where(type =>
                    type.Name == expectedName && type.IsClass && type.IsAbstract
                )
                .ToArray();
            if (candidates.Length != 1)
            {
                yield return $"{root.Name}: expected exactly one abstract {expectedName} type in the Domain assembly; found {candidates.Length}. Create the aggregate-specific abstract event base.";
                continue;
            }

            var candidate = candidates[0];
            if (
                !candidate.IsVisible
                || candidate.IsSealed
                || candidate.IsGenericType
            )
                yield return $"{candidate.Name}: expected a public, non-generic, inheritable abstract class.";
            if (candidate.BaseType != baseDomainEvent)
                yield return $"{candidate.Name}: directly inherit BaseDomainEvent so aggregate events share the common event contract.";
            if (!domainEventInterface.IsAssignableFrom(candidate))
                yield return $"{candidate.Name}: implement IDomainEvent through BaseDomainEvent.";
        }
    }

    public static Type[] AggregateEventBaseTypes(
        Assembly domain,
        IEnumerable<Type> aggregateRoots
    )
    {
        var roots = aggregateRoots.ToArray();
        return roots
            .SelectMany(root =>
            {
                var expectedName = AggregateName(root) + "DomainEvent";
                return domain
                    .GetTypes()
                    .Where(type =>
                        type.Name == expectedName
                        && type.IsClass
                        && type.IsAbstract
                    );
            })
            .Distinct()
            .ToArray();
    }

    public static Type[] ConcreteDomainEvents(
        Assembly domain,
        Type domainEventInterface
    ) =>
        domain
            .GetTypes()
            .Where(type =>
                !type.IsInterface
                && !type.IsAbstract
                && domainEventInterface.IsAssignableFrom(type)
            )
            .ToArray();

    public static IEnumerable<string> ConcreteEventInheritance(
        IEnumerable<Type> concreteEvents,
        IEnumerable<Type> aggregateEventBases,
        Type baseDomainEvent
    )
    {
        var events = concreteEvents.ToArray();
        var validBases = aggregateEventBases.ToHashSet();
        foreach (var domainEvent in events)
        {
            if (!domainEvent.IsClass)
            {
                yield return $"{domainEvent.Name}: concrete IDomainEvent implementations must be classes that inherit an aggregate-specific abstract [AggregateName]DomainEvent; value types cannot satisfy this contract.";
                continue;
            }

            if (
                domainEvent.BaseType is null
                || !validBases.Contains(domainEvent.BaseType)
            )
            {
                var currentBase = domainEvent.BaseType?.Name ?? "<none>";
                var detail =
                    domainEvent.BaseType == baseDomainEvent
                        ? $"it directly inherits {baseDomainEvent.Name}"
                        : $"its direct base is {currentBase}";
                yield return $"{domainEvent.Name}: expected a direct base class matching an aggregate-specific abstract [AggregateName]DomainEvent; {detail}.";
            }
        }
    }

    public static IEnumerable<string> ConcreteEventsAreSealed(
        IEnumerable<Type> concreteEvents
    )
    {
        var events = concreteEvents.ToArray();
        foreach (var domainEvent in events)
            if (!domainEvent.IsSealed)
                yield return $"{domainEvent.Name}: concrete domain events must be sealed so they cannot be inherited.";
    }

    public static IEnumerable<string> FqdnFormat(
        Type baseDomainEvent,
        string? companyContext = null
    )
    {
        var violations = new List<string>();
        var fqdnProperty = baseDomainEvent.GetProperty(
            "FQDN",
            BindingFlags.Public | BindingFlags.Instance
        );
        if (
            fqdnProperty is null
            || fqdnProperty.PropertyType != typeof(string)
            || fqdnProperty.GetMethod is not { IsPublic: true, IsStatic: false }
        )
        {
            violations.Add(
                $"{baseDomainEvent.Name}.FQDN: expected a public string getter so the composed event name can be verified."
            );
            return violations;
        }

        var constructors = baseDomainEvent
            .GetConstructors(
                BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance
            )
            .Where(IsFqdnProbeConstructor)
            .ToArray();
        if (constructors.Length == 0)
        {
            violations.Add(
                $"{baseDomainEvent.Name}: expected an accessible constructor accepting aggregate and event names plus DateTimeOffset, or company, bounded-context, aggregate, and event names plus DateTimeOffset, to verify FQDN composition."
            );
            return violations;
        }

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            var stringCount = parameters.Count(parameter =>
                parameter.ParameterType == typeof(string)
            );
            if (stringCount == 2 && string.IsNullOrWhiteSpace(companyContext))
            {
                violations.Add(
                    $"{baseDomainEvent.Name}: supply the product company and bounded-context prefix to verify the two-name constructor, or expose the four-name constructor. The toolkit namespace does not identify a product context."
                );
                continue;
            }

            var components =
                stringCount == 2
                    ? new[] { "AggregateProbe", "EventProbe" }
                    : new[]
                    {
                        "CompanyProbe",
                        "BoundedContextProbe",
                        "AggregateProbe",
                        "EventProbe",
                    };
            var expected =
                stringCount == 2
                    ? $"{companyContext}.{components[0]}.{components[1]}"
                    : string.Join(".", components);
            var arguments = new object?[parameters.Length];
            var nextComponent = 0;
            for (var index = 0; index < parameters.Length; index++)
                arguments[index] =
                    parameters[index].ParameterType == typeof(string)
                        ? components[nextComponent++]
                        : DateTimeOffset.UnixEpoch;

            try
            {
                var probe = CreateFqdnProbe(
                    baseDomainEvent,
                    constructor,
                    arguments
                );
                var actual = fqdnProperty.GetValue(probe) as string;
                if (!string.Equals(actual, expected, StringComparison.Ordinal))
                    violations.Add(
                        $"{baseDomainEvent.Name}.FQDN: expected \"{expected}\" by composing company, bounded context, aggregate, and event names in that order with dots; received \"{actual ?? "<null>"}\"."
                    );
            }
            catch (Exception exception)
                when (exception
                        is ArgumentException
                            or InvalidOperationException
                            or MemberAccessException
                            or NotSupportedException
                            or TargetInvocationException
                            or TypeLoadException
                )
            {
                var cause = exception
                    is TargetInvocationException
                    {
                        InnerException: { } innerException
                    }
                    ? innerException
                    : exception;
                violations.Add(
                    $"{baseDomainEvent.Name}.FQDN: could not execute the formatting probe ({cause.GetType().Name}: {cause.Message}). Ensure the base event can be subclassed and constructed with valid name values."
                );
            }
        }

        return violations;
    }

    private static string AggregateName(Type aggregateRoot)
    {
        var genericMarker = aggregateRoot.Name.IndexOf(
            '`',
            StringComparison.Ordinal
        );
        return genericMarker < 0
            ? aggregateRoot.Name
            : aggregateRoot.Name[..genericMarker];
    }

    private static bool InheritsFromAggregateRoot(
        Type type,
        Type aggregateRootBase
    )
    {
        for (
            var current = type;
            current is not null;
            current = current.BaseType
        )
            if (
                current == aggregateRootBase
                || (
                    current.IsGenericType
                    && current.GetGenericTypeDefinition() == aggregateRootBase
                )
            )
                return true;

        return false;
    }

    private static bool IsFqdnProbeConstructor(ConstructorInfo constructor)
    {
        if (
            !constructor.IsPublic
            && !constructor.IsFamily
            && !constructor.IsFamilyOrAssembly
        )
            return false;

        var parameterTypes = constructor
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();
        var stringCount = parameterTypes.Count(type => type == typeof(string));
        return (stringCount == 2 || stringCount == 4)
            && parameterTypes.Count(type => type == typeof(DateTimeOffset)) == 1
            && parameterTypes.All(type =>
                type == typeof(string) || type == typeof(DateTimeOffset)
            );
    }

    private static object CreateFqdnProbe(
        Type baseDomainEvent,
        ConstructorInfo baseConstructor,
        object?[] arguments
    )
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("DddFqdnProbe" + Guid.NewGuid().ToString("N")),
            AssemblyBuilderAccess.Run
        );
        var module = assembly.DefineDynamicModule("DddFqdnProbe");
        var probeBuilder = module.DefineType(
            "FqdnProbe",
            TypeAttributes.Public | TypeAttributes.Sealed,
            baseDomainEvent
        );
        var parameterTypes = baseConstructor
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();
        var probeConstructor = probeBuilder.DefineConstructor(
            MethodAttributes.Public,
            CallingConventions.Standard,
            parameterTypes
        );
        var generator = probeConstructor.GetILGenerator();
        generator.Emit(OpCodes.Ldarg_0);
        for (var index = 0; index < parameterTypes.Length; index++)
            generator.Emit(OpCodes.Ldarg, index + 1);
        generator.Emit(OpCodes.Call, baseConstructor);
        generator.Emit(OpCodes.Ret);
        ImplementAbstractMethods(probeBuilder, baseDomainEvent);
        var probeType = probeBuilder.CreateType()!;
        return Activator.CreateInstance(probeType, arguments)!;
    }

    private static void ImplementAbstractMethods(
        TypeBuilder probeBuilder,
        Type baseDomainEvent
    )
    {
        foreach (
            var method in baseDomainEvent
                .GetMethods(
                    BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.Instance
                )
                .Where(method => method.IsAbstract)
        )
        {
            if (
                method.IsGenericMethodDefinition
                || method.ReturnType.IsByRef
                || method.ReturnType.IsPointer
            )
                throw new NotSupportedException(
                    $"Cannot generate an FQDN probe override for {method.Name}."
                );

            var parameterTypes = method
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();
            var overrideMethod = probeBuilder.DefineMethod(
                method.Name,
                method.Attributes & ~MethodAttributes.Abstract,
                method.CallingConvention,
                method.ReturnType,
                parameterTypes
            );
            probeBuilder.DefineMethodOverride(overrideMethod, method);
            var generator = overrideMethod.GetILGenerator();
            if (method.ReturnType == typeof(void))
            {
                generator.Emit(OpCodes.Ret);
            }
            else if (method.ReturnType.IsValueType)
            {
                var result = generator.DeclareLocal(method.ReturnType);
                generator.Emit(OpCodes.Ldloca, result);
                generator.Emit(OpCodes.Initobj, method.ReturnType);
                generator.Emit(OpCodes.Ldloc, result);
                generator.Emit(OpCodes.Ret);
            }
            else
            {
                generator.Emit(OpCodes.Ldnull);
                generator.Emit(OpCodes.Ret);
            }
        }
    }
}
