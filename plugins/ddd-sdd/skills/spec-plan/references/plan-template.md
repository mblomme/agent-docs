# Work Plan: <TYPE>-XXX — <Title>

- **Plan ID:** `<TYPE>-XXX` (`F` = Feature, `B` = Bug, `R` = Refactor)
- **Work Type:** `<Feature | Bug | Refactor>`
- **Tasks Breakdown:** [tasks.md](./tasks.md)

## 1. Intent & Scope Boundary
- **Goal:** <1–2 sentences stating the observable outcome this feature, bug fix, or refactor achieves.>
- **Product spec references:**
  - [<Spec Document Title 1>](../../product/<some_spec.md#anchor>)
  - [<Spec Document Title 2>](../../product/<some_spec.md>)
- **Technical spec references:**
  - [00-topology-overview.md](../../technical/architecture/00-topology-overview.md)
  - [<Applicable Layer / Topology Spec>](../../technical/architecture/<path.md>)
- **Out of Scope (Non-Goals):**
  - <Explicitly list adjacent concepts, aggregates, workflows, or adapters that are excluded from this plan.>

## 2. Layer Impact Summary (Inside-Out)
| Layer | Impacted? | Rationale & Contract Boundary |
| :--- | :---: | :--- |
| **1. Domain** | `Yes / No` | <Brief statement of which domain concepts/contracts are affected, or why no domain changes are needed.> |
| **2. Application** | `Yes / No` | <Brief statement of which workflows/ports are affected, or why no application changes are needed.> |
| **3. Infrastructure** | `Yes / No` | <Brief statement of which driving/driven adapters are affected, or why no infrastructure changes are needed.> |
| **4. Main** | `Yes / No` | <Brief statement of which composition-root setup modules are affected, or why no wiring changes are needed.> |

## 3. Declarative Layer Goals (Active Layers Only)
*(Omit any layer marked `No` in Section 2. Do not copy rules from `/spec`; link to them.)*

### Domain Layer
- **Target State:** <Declarative description of the domain model capabilities and public contracts that must exist per the linked specifications.>
- **Spec Pointers:** [<Spec Link>](../../<path/to/spec.md#anchor>)

### Application Layer
- **Target State:** <Declarative description of the use cases, queries, policies, and driving/driven port contracts that must exist.>
- **Spec Pointers:** [<Spec Link>](../../<path/to/spec.md#anchor>)

### Infrastructure Layer
- **Target State:** <Declarative description of the driving and driven adapter implementations and boundary translations required.>
- **Spec Pointers:** [<Spec Link>](../../<path/to/spec.md#anchor>)

### Main (Composition Root)
- **Target State:** <Declarative description of the dependency registration and module activation required.>
- **Spec Pointers:** [<Spec Link>](../../<path/to/spec.md#anchor>)
