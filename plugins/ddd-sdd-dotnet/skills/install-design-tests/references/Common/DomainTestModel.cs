using System.Reflection;

namespace Company.BoundedContext.DesignTests.Common;

internal static class DomainTestModel
{
    public static Assembly[] Production =>
        AssemblyDiscovery.ProductionAssemblies(
            typeof(DomainTestModel).Assembly
        );
    public static Assembly DomainAssembly =>
        AssemblyDiscovery.DomainAssembly(Production);
    public static Assembly ToolkitDomainAssembly =>
        AssemblyDiscovery.ToolkitAssembly(Production, "Domain");
    public static Type[] Entities =>
        DomainDiscovery.Entities(DomainAssembly.GetTypes(), Shared("Entity`1"));
    public static AggregateDefinition[] Aggregates =>
        DomainDiscovery.Aggregates(
            Entities.Concat(ConcreteAggregateRoots).Distinct(),
            Shared("AggregateRoot`1")
        );
    public static Type[] ConcreteAggregateRoots =>
        DomainDiscovery.AggregateRoots(
            DomainAssembly.GetTypes(),
            Shared("AggregateRoot`1")
        );
    public static Type[] ConcreteEntities => ConcreteTypes("Entity`1");
    public static Type[] ConcreteValueObjects => ConcreteTypes("ValueObject");
    public static Type[] ConcreteDomainEvents => ConcreteTypes("IDomainEvent");

    private static Type[] ConcreteTypes(string sharedName)
    {
        var assembly = DomainAssembly;
        var marker = Shared(sharedName);

        return assembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract
                && !type.IsInterface
                && (
                    marker.IsGenericTypeDefinition
                        ? DomainDiscovery.Inherits(type, marker)
                        : marker.IsAssignableFrom(type)
                )
            )
            .ToArray();
    }

    private static Type Shared(string name) =>
        AggregateContractInspection.SharedType(ToolkitDomainAssembly, name);
}
