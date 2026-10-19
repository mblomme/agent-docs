---
name: domain-event
description: Build or refactor DDD domain events in C# and .NET, including immutable business facts, aggregate event recording, transaction-aware dispatch, and reliable integration-event delivery.
---

# Domain events in .NET

## Workflow

2. Name the event as a past-tense business fact.
3. The data of the event should be documented in the domain model.
3. Include only domain data needed by downstream domain or application reactions.
4. Raise the event from the aggregate only.

## Rules

- Domain events are facts, not commands or integration messages.
- Keep events immutable and free of transport, persistence, and serialization concerns.
- Prefer typed IDs and value objects in event payloads.
- Do not publish events, map broker messages, or perform side effects in the domain layer.
- Avoid raising events from setters or external services that bypass aggregate behavior.

## DomainEvent structure

Each aggregate should have a base domain event class that provides the fully qualified domain name (FQDN) and timestamp. Concrete events inherit from the aggregate base and carry only the business data needed by downstream reactions.

Example structure
```text
IDomainEvent
└── BaseDomainEvent
    └── GameDomainEvent
        ├── GameCreated
        └── GameStarted
```

1. **The shared base provides metadata.**  
   - `FQDN`: an event name built from company, bounded context, aggregate, and event name.
   - `OccurredOn`: the UTC time when the event object is constructed.

2. **An aggregate-specific base supplies naming context.**  
   Fills in `company`, `bounded context`, and `aggregate name`. A creation event therefore has this FQDN:
   ```text
   Company.BoundedContext.AggregateName.DomainEventName
   ```