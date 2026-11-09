using Company.BoundedContext.DesignTests.Common.ContractFixtures;

namespace Company.BoundedContext.DesignTests.Common.UnitOfWorkFixtures;

public interface IUnitOfWork
{
    Task Do();
    Task Save<TRepository>(IAggregateRoot aggregate)
        where TRepository : IRepository;
    TRepository Repo<TRepository>()
        where TRepository : IRepository;
}

public interface IValid
{
    Task Do();
    Task Save<TRepository>(IAggregateRoot aggregate)
        where TRepository : IRepository;
    TRepository Repo<TRepository>()
        where TRepository : IRepository;
}

public interface IInherited : IValid
{
    void Unrelated();
}

public interface IMissing { }

public interface ISynchronous
{
    void Do();
    void Save<TRepository>(IAggregateRoot aggregate)
        where TRepository : IRepository;
    IRepository Repo<TRepository>()
        where TRepository : IRepository;
}

public interface IValueTask
{
    ValueTask Do();
    ValueTask Save<TRepository>(IAggregateRoot aggregate)
        where TRepository : IRepository;
    Task<TRepository> Repo<TRepository>()
        where TRepository : IRepository;
}

public interface IWrongArguments
{
    Task Do(int argument);
    Task Save<TRepository>(ref IAggregateRoot aggregate)
        where TRepository : IRepository;
    TRepository Repo<TRepository>(int argument)
        where TRepository : IRepository;
}

public interface IWrongArity
{
    Task Do<T>();
    Task Save<TRepository, TExtra>(IAggregateRoot aggregate)
        where TRepository : IRepository;
    TRepository Repo<TRepository, TExtra>()
        where TRepository : IRepository;
}

public interface IUnconstrained
{
    Task Save<TRepository>(IAggregateRoot aggregate);
    TRepository Repo<TRepository>();
}

public interface IOverconstrained
{
    Task Save<TRepository>(IAggregateRoot aggregate)
        where TRepository : class, IRepository, new();
    TRepository Repo<TRepository>()
        where TRepository : class, IRepository, new();
}

public interface IStatic
{
#pragma warning disable CA1000 // Deliberately invalid static ports.
    static Task Do() => Task.CompletedTask;
    static Task Save<TRepository>(IAggregateRoot aggregate)
        where TRepository : IRepository => Task.CompletedTask;
    static TRepository Repo<TRepository>()
        where TRepository : IRepository => throw new NotSupportedException();
#pragma warning restore CA1000
}

public interface IProtected
{
    protected Task Do();
    protected Task Save<TRepository>(IAggregateRoot aggregate)
        where TRepository : IRepository;
    protected TRepository Repo<TRepository>()
        where TRepository : IRepository;
}

public class Concrete { }

public interface IGeneric<T> { }

internal interface IHidden { }
