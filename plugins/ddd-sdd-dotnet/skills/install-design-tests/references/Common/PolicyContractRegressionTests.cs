using Company.BoundedContext.DesignTests.Common.ContractFixtures;
using Company.BoundedContext.DesignTests.Common.PolicyFixtures;

namespace Company.BoundedContext.DesignTests.Common;

public class PolicyContractRegressionTests
{
    [Theory]
    [InlineData(typeof(IPolicy<>))]
    [InlineData(typeof(IExtended<>))]
    [InlineData(typeof(IIndirect<>))]
    [InlineData(typeof(IExtraParameters<>))]
    public void ValidPoliciesAllowExtensionsAndImpliedConstraints(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        Assert.Empty(PolicyContractInspection.Shape(type));
        Assert.Empty(
            PolicyContractInspection.EventParameter(type, typeof(IDomainEvent))
        );
        Assert.Empty(PolicyContractInspection.Execute(type));
        Assert.Empty(PolicyContractInspection.AsyncExecute(type));
    }

    [Theory]
    [InlineData(typeof(NotInterface<>))]
    [InlineData(typeof(IHidden<>))]
    [InlineData(typeof(INoParameters))]
    [InlineData(typeof(ITwoParameters<,>))]
    [InlineData(typeof(IPolicy<IDomainEvent>))]
    [InlineData(typeof(Container.INested<>))]
    public void InvalidShapesGiveRepairGuidanceWithoutCrashing(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        foreach (var inspect in Inspectors())
        {
            var message = Assert.Single(inspect(type));
            Assert.Contains(type.ToString(), message, StringComparison.Ordinal);
            Assert.Contains(
                "public, top-level generic interface",
                message,
                StringComparison.Ordinal
            );
        }
    }

    [Theory]
    [InlineData(typeof(IInvariant<>), "contravariant")]
    [InlineData(typeof(ICovariant<>), "contravariant")]
    [InlineData(typeof(IUnconstrained<>), "IDomainEvent")]
    [InlineData(typeof(IUnrelated<>), "IDomainEvent")]
    public void InvalidEventParametersExplainRequiredConstraintOrVariance(
        Type type,
        string remedy
    )
    {
        ArgumentNullException.ThrowIfNull(type);
        var message = Assert.Single(
            PolicyContractInspection.EventParameter(type, typeof(IDomainEvent))
        );
        Assert.Contains(type.ToString(), message, StringComparison.Ordinal);
        Assert.Contains(remedy, message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(typeof(IMissing<>))]
    [InlineData(typeof(IWrongInput<>))]
    [InlineData(typeof(IRefInput<>))]
    [InlineData(typeof(IOutInput<>))]
    [InlineData(typeof(IStatic<>))]
    [InlineData(typeof(IGeneric<>))]
    [InlineData(typeof(IProtected<>))]
    public void InvalidExecuteSignaturesExplainRequiredOperation(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var message = Assert.Single(PolicyContractInspection.Execute(type));
        Assert.Contains(type.ToString(), message, StringComparison.Ordinal);
        Assert.Contains(
            "public instance nongeneric Execute",
            message,
            StringComparison.Ordinal
        );
        Assert.Contains("by value", message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(typeof(ISync<>))]
    [InlineData(typeof(IValueTask<>))]
    [InlineData(typeof(IResult<>))]
    [InlineData(typeof(IAsyncOnly<>))]
    public void WrongReturnOrMissingExecuteCannotSatisfyAsyncContract(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        var messages = PolicyContractInspection.AsyncExecute(type).ToArray();
        Assert.Contains(
            messages,
            m => m.Contains("exactly Task", StringComparison.Ordinal)
        );
        Assert.All(
            messages,
            m => Assert.Contains(type.ToString(), m, StringComparison.Ordinal)
        );
    }

    [Fact]
    public void InheritedExecuteDoesNotExcuseAsyncSuffix()
    {
        var message = Assert.Single(
            PolicyContractInspection.AsyncExecute(typeof(IAsyncSuffix<>))
        );
        Assert.Contains(
            "rename ExecuteAsync to Execute",
            message,
            StringComparison.Ordinal
        );
    }

    [Fact]
    public void DiscoveryAcceptsPolicyAndReportsMissingPort()
    {
        var ns = typeof(IPolicy<>).Namespace!;
        Assert.Empty(
            PolicyContractInspection.Contracts(
                [typeof(IPolicy<>)],
                ns,
                PolicyContractInspection.Shape
            )
        );
        var message = Assert.Single(
            PolicyContractInspection.Contracts(
                [],
                ns,
                PolicyContractInspection.Shape
            )
        );
        Assert.Contains("IPolicy`1", message, StringComparison.Ordinal);
        Assert.Contains("found 0", message, StringComparison.Ordinal);
        Assert.Contains(
            "Declare public IPolicy",
            message,
            StringComparison.Ordinal
        );
    }

    [Theory]
    [InlineData("Contracts/Driving/Anything.cs", true)]
    [InlineData("Contracts\\Driving\\Ports.cs", true)]
    [InlineData("Contracts/Driven/IPolicy.cs", false)]
    [InlineData("Contracts/Driving/Nested/IPolicy.cs", false)]
    [InlineData("IPolicy.cs", false)]
    public void PlacementUsesDeclarationsRatherThanFilename(
        string path,
        bool valid
    )
    {
        var messages = Placement(
            path,
            "namespace Example.Application.Contracts.Driving; public interface IPolicy<in T> {}"
        );
        if (valid)
            Assert.Empty(messages);
        else
            Assert.Contains(
                "move",
                Assert.Single(messages),
                StringComparison.Ordinal
            );
    }

    [Theory]
    [InlineData("")]
    [InlineData("namespace Wrong; public interface IPolicy<T> {}")]
    [InlineData(
        "namespace Example.Application.Contracts.Driving; public class IPolicy<T> {}"
    )]
    [InlineData(
        "namespace Example.Application.Contracts.Driving; public interface IPolicy<T,U> {}"
    )]
    [InlineData(
        "namespace Example.Application.Contracts.Driving; public class Container { public interface IPolicy<T> {} }"
    )]
    public void InvalidDeclarationsReportMissingSource(string source) =>
        Assert.Contains(
            "missing source declaration",
            Assert.Single(Placement("Contracts/Driving/IPolicy.cs", source)),
            StringComparison.Ordinal
        );

    private static string[] Placement(string path, string source) =>
        UseCaseContractInspection
            .DrivingDeclarations(
                [(path, source)],
                "Example.Application.Contracts.Driving",
                arity: 1,
                interfaceName: "IPolicy"
            )
            .ToArray();

    private static Func<Type, IEnumerable<string>>[] Inspectors() =>
        [
            PolicyContractInspection.Shape,
            t =>
                PolicyContractInspection.EventParameter(
                    t,
                    typeof(IDomainEvent)
                ),
            PolicyContractInspection.Execute,
            PolicyContractInspection.AsyncExecute,
        ];
}
