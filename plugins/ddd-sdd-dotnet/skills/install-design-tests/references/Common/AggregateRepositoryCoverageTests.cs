namespace Company.BoundedContext.DesignTests.Common;

public class AggregateRepositoryCoverageTests
{
    [Fact]
    public void MissingRepositoriesReportEveryConcreteRootAndItsIdentity()
    {
        var violations = Inspect([
            typeof(Order),
            typeof(Invoice),
            typeof(AbstractRoot<>),
        ]);
        Assert.Equal(2, violations.Length);
        Assert.Contains(
            violations,
            message =>
                message.Contains(
                    "IStore<Order, Guid>",
                    StringComparison.Ordinal
                )
        );
        Assert.Contains(
            violations,
            message =>
                message.Contains(
                    "IStore<Invoice, Int32>",
                    StringComparison.Ordinal
                )
        );
    }

    [Theory]
    [InlineData(typeof(IWrongId))]
    [InlineData(typeof(IWrongAggregate))]
    [InlineData(typeof(IOpenStore<,>))]
    [InlineData(typeof(IHiddenStore))]
    [InlineData(typeof(StoreImplementation))]
    [InlineData(typeof(IStore<,>))]
    public void WrongIdsOtherAggregatesOpenGenericAndNonpublicContractsDoNotCoverARoot(
        Type candidate
    )
    {
        var message = Assert.Single(Inspect([typeof(Order), candidate]));
        Assert.Contains(
            "missing a public domain-owned aggregate repository interface",
            message,
            StringComparison.Ordinal
        );
        Assert.Contains(
            "IStore<Order, Guid>",
            message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void ClosedInheritedInterfacesCoverDirectAndIndirectAggregateRoots() =>
        Assert.Empty(
            Inspect([
                typeof(Order),
                typeof(Invoice),
                typeof(IInheritedOrders),
                typeof(IInvoices),
            ])
        );

    private static string[] Inspect(Type[] types) =>
        RepositoryInspection
            .AggregateRepositories(types, typeof(Root<>), typeof(IStore<,>))
            .ToArray();

    public abstract class Root<TId>;

    public abstract class AbstractRoot<TId> : Root<TId>;

    public sealed class Order : Root<Guid>;

    public sealed class Invoice : AbstractRoot<int>;

    public interface IStore<TAggregate, TId>;

    public interface IOrders : IStore<Order, Guid>;

    public interface IInheritedOrders : IOrders;

    public interface IInvoices : IStore<Invoice, int>;

    public interface IWrongId : IStore<Order, int>;

    public interface IWrongAggregate : IStore<Invoice, Guid>;

    public interface IOpenStore<TAggregate, TId> : IStore<TAggregate, TId>;

    private interface IHiddenStore : IStore<Order, Guid>;

    public sealed class StoreImplementation : IStore<Order, Guid>;
}
