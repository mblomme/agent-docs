using Company.BoundedContext.DesignTests.Common.ContractFixtures;

namespace Company.BoundedContext.DesignTests.Common.PolicyFixtures;

public interface IPolicy<in T>
    where T : IDomainEvent
{
    Task Execute(T input);
}

public interface IExtended<in T> : IPolicy<T>
    where T : class, IDomainEvent
{
    string Description { get; }
    void Execute();
}

public interface ISpecialEvent : IDomainEvent { }

public interface IIndirect<in T>
    where T : ISpecialEvent
{
    Task Execute(T input, CancellationToken cancellationToken);
}

public interface IExtraParameters<in T>
    where T : IDomainEvent
{
#pragma warning disable CA1068 // Verify the event can appear after an additional parameter.
    Task Execute(CancellationToken token, T input);
#pragma warning restore CA1068
    void Other();
}

public class NotInterface<T> { }

internal interface IHidden<T> { }

public interface INoParameters { }

public interface ITwoParameters<T, TOther> { }

public interface IInvariant<T>
    where T : IDomainEvent { }

public interface ICovariant<out T>
    where T : IDomainEvent { }

public interface IUnconstrained<in T> { }

public interface IUnrelated<in T>
    where T : IDisposable { }

public interface IMissing<in T>
    where T : IDomainEvent { }

public interface IWrongInput<in T>
    where T : IDomainEvent
{
    Task Execute(string input);
}

public interface IRefInput<T>
    where T : IDomainEvent
{
    Task Execute(ref T input);
}

public interface IOutInput<T>
    where T : IDomainEvent
{
    Task Execute(out T input);
}

public interface IStatic<in T>
    where T : IDomainEvent
{
#pragma warning disable CA1000 // Deliberately invalid static policy operation.
    static Task Execute(T input) => Task.CompletedTask;
#pragma warning restore CA1000
}

public interface IGeneric<in T>
    where T : IDomainEvent
{
    Task Execute<TOther>(T input);
}

public interface IProtected<in T>
    where T : IDomainEvent
{
    protected Task Execute(T input);
}

public interface ISync<in T>
    where T : IDomainEvent
{
    void Execute(T input);
}

public interface IValueTask<in T>
    where T : IDomainEvent
{
    ValueTask Execute(T input);
}

public interface IResult<in T>
    where T : IDomainEvent
{
    Task<int> Execute(T input);
}

public interface IAsyncOnly<in T>
    where T : IDomainEvent
{
    Task ExecuteAsync(T input);
}

public interface IAsyncSuffix<in T> : IPolicy<T>
    where T : IDomainEvent
{
    Task ExecuteAsync(T input);
}

public static class Container
{
    public interface INested<in T>
        where T : IDomainEvent
    {
        Task Execute(T input);
    }
}
