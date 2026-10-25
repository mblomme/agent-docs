---
name: domain-event
description: Build or refactor DDD domain events in C# and .NET, including immutable business facts, aggregate event recording, transaction-aware dispatch, and reliable integration-event delivery.
---

# Domain events in .NET

# DDD Definition

A domain event is a discrete, immutable, and observable fact that has occurred in the domain. Domain events are raised by aggregates to notify other parts of the system about changes in state or significant occurrences. They are used to decouple different parts of the system and enable reactive behavior.

## Workflow

- Use the available common base classes to create new domain events.
- Name the event as a past-tense business fact.
- The data of the event should be available in the domain model specfication.
- Raise the event from the aggregates only.
- Domain events are facts, not commands or integration messages.
- Keep events immutable and free of transport, persistence, and serialization concerns.
- Prefer typed IDs and value objects in event payloads.
- Do not publish events, map broker messages, or perform side effects in the domain layer.
- Avoid raising events from setters or external services that bypass aggregate behavior.

## DomainEvent structure

1. **The shared base provides metadata.**  
   - `FQDN`: an event name built from company, bounded context, aggregate, and event name.
   - `OccurredOn`: the UTC time when the event object is constructed.

2. **An aggregate-specific base supplies naming context.**  
   Fills in `company`, `bounded context`, and `aggregate name`. 
   
   A creation event therefore has this FQDN:
   ```text
   Company.BoundedContext.AggregateName.DomainEventName
   ```