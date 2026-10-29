---
name: code-usecase
description: Create or refactor application command use cases in a hexagonal architecture for a target project. Implement specified aggregate creation or state changes through driving contracts, domain behavior, and transaction-scoped driven ports. Excludes use case specification writing and read-only queries.
---

# Hexagonal use case

Follow the target project's programming language, toolchain, and conventions.
Express these object-oriented design rules using the language's supported
constructs; do not assume a particular framework, SDK, or test library.

A use case coordinates one actor's goal in the Application layer. It maps input,
authorizes the action when specified, invokes domain behavior, persists the
result, and returns only what the caller needs. 
Business rules and invariants must exclusively belong to the domain model.

## Establish the contract

- A normal command use case changes one aggregate root. Handle bulk operations
  only when specified, with explicit atomicity and failure semantics. Do not
  silently widen a common use case into a multi-aggregate workflow. Read-only
  operations belong to queries; cross-context workflows require an explicit
  coordination design.
- Do not invent domain operations, business behavior, authorization policies,
  retry policies, or exception contracts to fill specification gaps.

## Rules

- Follow the project's language and architecture conventions. By default place
  each use case directly under `Application/` in its own action-named file;
  do not introduce a `Use-Cases/` subfolder.
- Each use case must make use of an existing base abstract class or interface for command use cases.
- A use case receives it's dependencies throug constructor injection.
- The use case input is a plain DTO with only the fields required to perform the action.
- A use case means a state change in the domain model. That means that every use case at least needs an unit of work as a constructor dependency. The unit of work provides access to the aggregate's repository and manages the transaction scope.
- A use case return void by default. If the use case needs to return a value, it should be the bare minimum needed by the caller, such as an aggregate identifier or a simple status. Do not return domain objects or collections of domain objects.
- A use case contains orchestration logic only. Keep business decisions in aggregate methods, value objects, or domain services. Map inputs into domain concepts using their validated construction methods; do not duplicate invariant checks or manipulate aggregate state directly.
- The unit of work terminates the transaction scope. Do not commit or rollback the transaction directly, and do not introduce a second commit when the unit-of-work callback already owns completion.
- All dependencies are injected through the constructor. The dependency parameters in the constructor should be ports, not concrete implementations. Do not inject infrastructure services, logging, configuration, etc. directly into the use case.