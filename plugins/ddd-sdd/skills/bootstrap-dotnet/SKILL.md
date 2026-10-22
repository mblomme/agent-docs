---
name: bootstrap-dotnet
description: Bootstrap a new .NET project with a Ports & Adapters architecture (Hexagonal architecture) with a domain and application layer on the inside and an infrastructure layer on the outside. The code will be driven by spec driven development. The spec and the code will both be in the same repository.
---

# Bootstrap empty .NET project

Scaffold an empty project using the architecture supplied with this prompt.
Execute the steps below in the target repository. Do not invent business
functionality or integrations. Reuse information the user has already supplied.

## 1. Establish the inputs

Before scaffolding, collect any missing required inputs in one concise question:

- **Bounded context name**: required; use it in project and namespace names.
- **Company name**: optional; offer it alongside the required inputs. If omitted,
  leave out both the company prefix and its separating dot.
- **Technology**: required; it must match a specification available in the
  source architecture's `technology-stacks/` directory. List the available
  choices rather than inventing one. This prompt implements `dotnet.md`; if
  another stack is selected, use its matching bootstrap prompt if available,
  otherwise report that this prompt only supports .NET and request guidance.
- **Topology**: required; accept exactly one of these choices:

  | Choice | Architecture specification |
  | --- | --- |
  | `single-service` | `topologies/single-service.md` |
  | `modular-monolith` | `topologies/modular-monolith.md` |
  | `microservice` | `topologies/microservices.md` |

Do not ask for adapter or host requirements at this stage.

## 2. Obtain and read the architecture

1. Use the `spec-directory` skill to create a `spec/` folder.
2. Locate the source architecture directory. When this prompt is available as
   `architecture/bootstrap/bootstrap-dotnet.md`, its containing architecture
   directory is `../` relative to the prompt's directory. Prefer that local
   source when accessible. Resolve paths relative to the source file, not the
   target repository's working directory. 
   - When nothing is provided use:https://github.com/mblomme/agent-docs/architecture
3. Place the contents of the architecture directory into `spec/architecture/`, preserving
   relative paths and dotfiles.
4. Read `spec/architecture/llms.txt` as the entry point for all architecture
   information. Read the topology overview, the selected technology and topology
   specifications, and the core layer rules. Resolve each `depends_on` path
   relative to its declaring document and read dependencies before dependents.

## 3. Scaffold the project

Preserve existing work. Inspect the target repository before generating files,
reuse compatible configuration, and report conflicts before overwriting.
Use a compatible installed .NET SDK and the repository's SDK pin when present;
report an unavailable required SDK rather than silently changing it.

- Create root-level `src/` and `tests/` directories.
- Follow the `technology-stacks/dotnet.md` specification for dotnet instructions.
- Follow the `topologies/<selected-topology>.md` specification for the topology instructions.
- Follow the documented layer responsibilities conventions in the `core-rules` folder.
- For the layer `Application` you can already scaffold the the empty directories as mentioned in the documentation of the application layer.
- For the layers `Main` and `Infrastructure` create
  other folders only when they have a current purpose.
  An empty Infrastructure project is acceptable when no adapters are
  required; do not populate it with speculative folder trees. In ´Main´ an empty `Modules/` folder is acceptable when no setup files are required; do not populate it with speculative folder trees.
- Don't create a README.md

## 4. Write a simple root AGENTS.md

Generate a short `AGENTS.md` at the target repository root with the actual
values and commands for the generated project. If one already exists, preserve
its instructions and request guidance for conflicting changes.

Include only:

- The bounded context, company when supplied, selected technology, and topology.
- A relative link to [`spec/llms.txt`](spec/architecture/llms.txt),
  identifying it as the entry point for all architecture information. Instruct
  agents to read it and the applicable specifications and dependencies before
  changing code, and to report missing or conflicting guidance.
- A brief reminder that dependencies point inward, Main owns composition,
  adapters remain independent, and adapter/setup folders are added only when
  required.
- Metion to remove .gitkeep files when the folder is no longer empty.

Keep detailed design and architecture rules in `spec/`; link to them instead
of duplicating them in `AGENTS.md`.

## 5. Verify and report

- Restore tools and packages, build the solution, run the tests, and check
  formatting using the generated configuration. Report any unavailable tools,
  restore failures, or checks that could not be completed accurately.
- Confirm `AGENTS.md` links and commands resolve to the generated files.
- Confirm omitted company names leave no leading dots, project references follow
  the architecture, and no unrequested adapter folders or template examples
  remain. Confirm the copied architecture retains its directory structure.
- Empty folders must contain a `.gitkeep` file to preserve them in source control.
- Summarize the created structure, selected inputs, verification results, and
  any unresolved issues. Include the command to run Main.


# Aggregates in .NET

The scope, boundary and invariants of an aggregate should already be defined and documented in the domain model. This skill will help you implement the aggregate in C# and .NET. Warn the user if the documentation is missing, vague or contradictory.

## Workflow

1. Keep external references to other aggregates as typed IDs unless the project has a clear exception.
2. Encapsulate child collections behind private fields and read-only views.
3. Put domain transitions on public methods of the root and keep child mutation internal to the boundary.
4. Create domain events for all documented domain events for that aggregate.
5. DomainEvents are raised by a common base class method called `RaiseDomainEvent`.
6. Check invariants in public methods before changing state by calling intent-revealing static `Ensure...` methods.


## Rules

- Declare the Aggregate and its AggregateId in the same file.
- An AggregateId must make use of a common IEntityId contract.
- An Aggregate must make use of an AggregateRoot base class.
- Only aggregate roots should have repositories.
- An repository must be build for each aggregate.
- When using entities, make sure they make sure they are built according to the entity skill.
- Mutate child entities or collections directly only through the aggregate root.
- Avoid cross-aggregate invariants that require multiple roots to be loaded for one rule.
- Use value objects for descriptive inputs and typed IDs for references.
- Only the public methods of the aggregate can raise domain events.
- Group static `Ensure...` methods in a separate static assert class per aggregate. Name each method after the domain rule it enforces, such as `OrderAsserts.EnsureOrderCanBeCancelled(order)`.
- An `Ensure...` method may delegate to a generic validation helper, but its name must explain the domain intent at the call site.
- Put static `Ensure...` methods for domain rules shared by multiple aggregates in a shared static `DomainAsserts` class.
- Create a private constructor without parameters for the aggregate root to support ORM frameworks.
- Creation of an aggregate should be done through a static factory method on the aggregate root. This means the constructor of the aggregate root should be private and the factory method should be public and static. The factory method should return a new instance of the aggregate root.


## Examples

Example of a common aggregate ID implementation:

```csharp
public interface IEntityId
{
    Guid Value { get; }
}

public static class EntityId
{
    private static readonly ConcurrentDictionary<Type, Delegate> _constructors =
        new();

    public static TId New<TId>(Guid? guid = default)
        where TId : struct, IEntityId
    {
        var factory =
            (Func<Guid, TId>)
                _constructors.GetOrAdd(typeof(TId), CreateFactory<TId>);

        Guid id = guid ?? Guid.CreateVersion7();

        return factory(id);
    }

    private static Func<Guid, TId> CreateFactory<TId>(Type type)
        where TId : struct
    {
        var constructor =
            type.GetConstructor([typeof(Guid)])
            ?? throw new InvalidOperationException(
                $"Type {type.Name} must have a constructor accepting a Guid."
            );

        var param = Expression.Parameter(typeof(Guid), "value");
        var newExpr = Expression.New(constructor, param);
        var lambda = Expression.Lambda<Func<Guid, TId>>(newExpr, param);

        return lambda.Compile();
    }
}
```

Example of a common aggregate root implementation:

```csharp
public interface IAggregateRoot
{
    long ConcurrencyVersion { get; }
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : struct, IEntityId
{
    public long ConcurrencyVersion { get; private set; }
    private readonly IList<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

    protected AggregateRoot(TId id)
        : base(id) { }

    protected AggregateRoot() { }

    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
```
