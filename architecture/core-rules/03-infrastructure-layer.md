---
id: arch-infrastructure-03
status: canonical
depends_on:
  - ../00-topology-overview.md
  - ./02-application-layer.md
summary: Defines independent driving and driven adapters, persistence mapping, outbox delivery, and vendor isolation.
---

# Layer Specification: Outside: Infrastructure Adapters

## Infrastructure Layer: Directory Responsibilities


```text
Infrastructure/
├── Driving/                         # Adapters that invoke application driving interfaces
│   ├── WebAPI/                      # HTTP/gRPC adapter
│   └── Messaging/
│       └── Kafka-Consumer/          # One broker-consumer adapter
└── Driven/                          # Adapters that implement application driven interfaces
    ├── Persistence/
    │   ├── Write/
    │   │   └── MongoDB/             # One write adapter
    │   └── Read/
    │       └── MongoDB/             # One read adapter
    └── Messaging/
        └── Kafka-Publisher/         # One publisher adapter
```

Each adapter MUST live in its own subfolder; do not place adapter files directly in `Driving/`, `Driven/`, or a grouping folder such as `Persistence/` or `Messaging/`. Use grouping folders for adapter kinds or responsibilities, and nest them further by operation, technology, or other relevant concern as the project needs. A leaf adapter folder contains that adapter's implementation and local helpers; it must not combine peer adapters.

## Infrastructure Layer: Transport and Persistence Adapters

Driving adapters parse and validate transport input, map it to application DTOs, invoke an injected driving interface, and translate results or errors into transport responses. Broker consumers map public integration events into local event DTOs. They must not instantiate or import concrete use cases.

Driven adapters implement driven interfaces. Persistence adapters keep ORM models separate from domain aggregates.

Adapters can contain technical decisions such as retries, serialization, and connection management. Business decisions, pricing, eligibility, and workflow selection belong inside the core. SDK request/response objects and database exceptions are mapped at the boundary.

## Infrastructure Layer: Adapter Independence

An adapter MUST NOT import, instantiate, inject, or invoke another adapter, whether driving or driven. Depending on a peer through an interface does not make adapter-to-adapter collaboration permissible. For example, an HTTP controller cannot call a repository adapter, and a repository adapter cannot call a broker adapter. Coordinate these operations in an application workflow through the appropriate ports; main's logical DI setup binds each port to its independent adapter.

An adapter may use its own implementation helpers, pure mappers, vendor clients, and shared technical resources such as a database pool or transaction context. These are not peer adapters: a helper must not conceal another port implementation or coordinate separate adapters. Multiple persistence adapters may use the same transaction-scoped database resource without depending on each other. The composition root and DI scopes supply that resource; adapters do not construct or resolve peer adapters.

## Infrastructure Layer: Source Dependency Matrix

| Dependency | Allowed |
| :--- | :---: |
| Same-context domain for persistence mapping | YES |
| Repository interfaces for persistence | YES |
| Application driving/driven contracts | YES |
| Adapter-local helpers, pure mappers, vendor libraries, and shared technical resources | YES |
| Another driving or driven adapter, directly or through an interface | NO |
| Concrete application use cases, queries, or policies | NO |
| Main | NO |

## Infrastructure Layer Invariants (DO NOT VIOLATE)

1. Adapters MUST translate formats and perform technical I/O without embedding business rules.
2. Vendor types MUST NOT leak into application contracts or domain code.
3. Infrastructure MUST NOT import concrete application implementations or main.
4. Persistence MUST use explicit domain mapping.
5. The relay MUST publish only committed records and preserve identity across retries.
6. Consumers MUST tolerate duplicate delivery; nontransactional external effects need explicit idempotency or coordination.
7. Adapters MUST NOT depend on or call other adapters, including through interfaces; application workflows coordinate their ports.
