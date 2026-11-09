using System.Reflection;

namespace Company.BoundedContext.DesignTests.Common;

internal static class RepositoryInspection
{
    public static IEnumerable<string> AggregateRepositories(
        IEnumerable<Type> domainTypes,
        Type aggregateBase,
        Type typedRepository
    )
    {
        ArgumentNullException.ThrowIfNull(domainTypes);
        ArgumentNullException.ThrowIfNull(aggregateBase);
        ArgumentNullException.ThrowIfNull(typedRepository);
        var types = domainTypes.ToArray();
        var repositories = types
            .Where(type =>
                type.IsInterface
                && type.IsVisible
                && !type.ContainsGenericParameters
            )
            .ToArray();
        foreach (
            var root in DomainDiscovery.AggregateRoots(types, aggregateBase)
        )
        {
            var inherited = root.BaseType;
            while (
                inherited is not null
                && !(
                    inherited.IsGenericType
                    && inherited.GetGenericTypeDefinition() == aggregateBase
                )
            )
                inherited = inherited.BaseType;
            var id = inherited!.GetGenericArguments()[0];
            var required = typedRepository.MakeGenericType(root, id);
            if (
                !repositories.Any(repository =>
                    repository.Assembly == root.Assembly
                    && repository.GetInterfaces().Contains(required)
                )
            )
                yield return $"{root.FullName}: missing a public domain-owned aggregate repository interface extending {typedRepository.Name.Split('`')[0]}<{root.Name}, {id.Name}>. Declare it in its own file beside the aggregate. Carefully read the domain-layer specification again for correct folder locations";
        }
    }

    public static bool IsRepositoryInterface(Type type) =>
        type.IsInterface
        && type.GetInterfaces()
            .Append(type)
            .Any(port => port.Name.Split('`')[0] == "IRepository");

    public static IEnumerable<string> RepositoryInterfaces(
        Assembly domain,
        IEnumerable<Assembly> assemblies,
        Func<Type, bool> isRepositoryInterface
    )
    {
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(isRepositoryInterface);
        foreach (
            var type in assemblies
                .Distinct()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsInterface && isRepositoryInterface(t))
        )
            if (
                type.Assembly != domain
                && !(
                    type.Assembly.GetName().Name == "DddToolkit.Domain"
                    && type.Namespace == "DddToolkit.Domain"
                    && type.Name is "IRepository" or "IRepository`2"
                )
            )
                yield return $"{type.Name}: repository interface declared in {type.Assembly.GetName().Name}; expected domain assembly {domain.GetName().Name}.";
    }

    public static IEnumerable<string> Repositories(
        Assembly domain,
        IEnumerable<Type> contracts
    )
    {
        ArgumentNullException.ThrowIfNull(domain);
        ArgumentNullException.ThrowIfNull(contracts);
        var ports = contracts.ToArray();
        foreach (var type in domain.GetTypes().Where(t => t.IsClass))
        foreach (var implemented in type.GetInterfaces())
            if (
                ports.Any(port =>
                    port == implemented
                    || port.IsGenericTypeDefinition
                        && implemented.IsGenericType
                        && implemented.GetGenericTypeDefinition() == port
                )
            )
                yield return $"{type.Name}: repository implementation in domain assembly {domain.GetName().Name}; implements {implemented.Name}";
    }
}
