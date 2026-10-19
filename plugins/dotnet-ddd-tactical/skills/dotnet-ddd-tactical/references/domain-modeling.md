# Domain modeling decisions

## Aggregates and entities

An aggregate is a consistency boundary, not the entire navigable object graph. Group state that must obey one invariant in one transaction; reference other aggregates by identity. If a rule spans aggregates, explicitly choose a shared application transaction or an eventual consistency workflow. Do not silently weaken a business invariant to fit a preferred pattern.

Entities retain identity across changes. Prefer ordinary classes for mutable entities; record-generated structural equality usually expresses the wrong semantics. Follow existing identity equality conventions and account for transient/unassigned IDs before implementing equality. Never base a mutable entity's hash code on changing business properties.

Root methods express business intent (`Confirm`, `ChangeDeliveryAddress`) rather than exposing property setters. Children may have internal mutation methods when the root owns access. Expose collection snapshots or read-only wrappers and prevent external mutation of children. Validate an entire operation before mutation; a thrown exception does not automatically restore in-memory state.

Use factories or named creation methods when construction has meaningful rules. Reconstitution restores persisted state without recording new events. Validate corrupt persisted data according to the persistence design; do not silently repair it by invoking current business transitions.

## Value objects and typed IDs

Choose a value object when a concept has domain meaning, equality semantics, or rules that deserve one implementation. Use immutable values and validate at construction. Normalize only according to confirmed domain rules; case folding an email address or rounding money is a business decision.

C# choices have specific hazards:

- A positional record with public `init` properties can permit invalid `with` copies. Use get-only properties and controlled construction when properties are interdependent.
- `default(T)` bypasses struct constructors. For a strongly typed ID or value struct, decide whether default is meaningful and reject invalid defaults at aggregate boundaries. Use a reference type when an invalid default would be hard to contain.
- Record equality compares collection members using their own equality; arrays and lists do not automatically become sequence-equal. Implement explicit sequence semantics or use a representation with the required equality.
- `IReadOnlyList<T>` does not make mutable elements immutable. Copy inputs when necessary.
- Use `decimal` for money, include currency in equality, reject mixed-currency arithmetic, and define precision and rounding from business requirements. Do not assume every currency has two decimal places.

Avoid universal `Entity<TId>` and `ValueObject` hierarchies until shared behavior justifies them. A small concrete type is often clearer.

## Services, specifications, and errors

A domain service contains business logic without a natural entity/value owner. Under this plugin's default, it is pure: application code supplies external facts and loads aggregates. Do not move all behavior to services while entities become mutable data bags.

A specification names a reusable business predicate. Introduce one when reuse or composition earns the abstraction. Keep ORM expression translation in persistence concerns; a compiled C# predicate and a database query may differ in null, casing, and provider semantics.

Use the repository's result or exception convention. Distinguish rejected business actions, malformed input, missing data, and concurrency conflicts where callers need different responses. Keep HTTP status codes and transport DTOs outside the domain.

## Behavioral checks

Use business examples such as: an empty order cannot be confirmed; a confirmed order cannot accept another line; two independently created equal values compare equal; a rejected operation leaves state and recorded events unchanged. Test the aggregate through its public behavior rather than private helpers. Domain tests should not need a database or mock broker.
