namespace Company.BoundedContext.DesignTests.Common.UseCaseFixtures;

public interface IUseCase<TInput>
{
    Task Execute(TInput input);
}

public interface IUseCase<TInput, TOutput>
{
    Task<TOutput> Execute(TInput input);
}

public interface IQueryUseCase<TInput, TOutput>
{
    Task<TOutput> Execute(TInput input);
}
