---
name: repository
description: Build or refactor DDD repositories in C# and .NET for aggregate persistence.
---

# Repositories in .NET

Each aggregate should have a repository interface on the same level as the aggregate root. This skill will help you implement the repository in C# and .NET.

## Workflow

1. Define only the methods the domain or application needs to load, add, save, or remove aggregate roots.
2. Use typed IDs and domain concepts in signatures.
3. Keep query names intention-revealing rather than persistence-shaped.
4. Add or update architecture tests only if the project already verifies repository boundaries.

## Rules

- Repositories belong to aggregate roots only, not child entities or value objects.
- Repository interfaces are ports, but they are the domain-layer exception: place aggregate repository interfaces in the Domain layer, not in Application contracts.
- Repository interfaces must extend a common interface.

## Example

The repository interfaces should extend a common interface.

Example of a common repository interface:

```csharp
public interface IRepository { }

public interface IRepository<TAggregateRoot, TId> : IRepository
    where TAggregateRoot : AggregateRoot<TId>
    where TId : struct, IEntityId
{
    Task<bool> Exists(TId id);
    Task<Optional<TAggregateRoot>> ById(TId id);
    Task Save(TAggregateRoot aggregateRoot);
    Task Remove(TAggregateRoot aggregateRoot);
}
```

Example of a repository:

```csharp
public interface IGameRepository : IRepository<Game, GameId>
{
    Task<IReadOnlyList<TodoList>> PurgeOutdatedGames(DateTime now);
}
```