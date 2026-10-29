# Typical use case flows

These flows describe application orchestration. Domain operations enforce all
business rules. Each use case changes exactly one aggregate root. Authorization
is optional and follows the documented policy; if it needs aggregate data,
perform it after fetching and before invoking the state-changing operation.

## Existing aggregate

1. Map use case input fields to the aggregate identifier and the appropriate
   parameters of the documented aggregate method.
2. When specified and possible with the available inputs, authorize the action.
3. Fetch the aggregate through the unit of work.
4. Throw a not-found exception when the aggregate does not exist.
5. Complete any authorization requiring aggregate data, then call the aggregate
   method.
6. Save the aggregate changes through the unit of work.
7. Mark the end of the unit of work using its documented completion semantics.
8. Return normally void, a simple ID, or a bundle of bare minimum information.

## Aggregate creation

1. Map use case input fields to the appropriate parameters of the documented
   aggregate constructor or factory.
2. When specified, authorize the creation action.
3. Create the aggregate through that constructor or factory.
4. Persist the new aggregate through the unit of work.
5. Mark the end of the unit of work using its documented completion semantics.
6. Return normally void, a simple ID, or a bundle of bare minimum information.

Creation does not fetch a preexisting aggregate or apply its not-found branch.
Neither flow completes the unit of work successfully when authorization,
loading, domain invocation, or persistence fails. Describe only documented behavior.
