---
name: repository
description: Build or refactor DDD repositories in C# and .NET for aggregate persistence.
---

# Repositories in .NET

Each aggregate should have a repository interface on the same level as the aggregate root. This skill will help you implement the repository in C# and .NET.

## DDD Definition

A repository is a domain-layer abstraction that provides access to aggregate roots and encapsulates the logic for retrieving and persisting them. Repositories are responsible for managing the lifecycle of aggregates, including loading, saving, and removing them from the underlying data store. They provide a collection-like interface for working with aggregates while hiding the details of data access and storage.

## Workflow

- Make use of the common base classes for repositories and repository interfaces.
- Define only the methods the domain or application needs to load, add, save, or remove aggregate roots.
- Use typed IDs and domain concepts in signatures.
- Repositories belong to aggregate roots only, not child entities or value objects.
- Repository interfaces are ports, but they are the domain-layer exception: place aggregate repository interfaces in the Domain layer, not in Application contracts.