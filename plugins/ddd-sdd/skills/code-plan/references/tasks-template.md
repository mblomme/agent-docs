# Work Tasks: <TYPE>-XXX — Title

- **Plan:** [<TYPE>-XXX — <Title>](./plan.md)
- **Plan ID:** `<TYPE>-XXX` (`F` = Feature, `B` = Bug, `R` = Refactor)

## 1. Task Traceability Table

> Execute tasks in inside-out dependency order. Read all linked `Spec References` before starting a task.

| Task ID | Layer | Depends On | Spec References (Pointers Only) | Target Scope | Recommended Skill | Status | AcceptanceTests  | DOD | Verified |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| `<TYPE>-XXX-T-DOM-01` | Domain | — | [<spec.md#section>](../../<path>) | <Aggregate / Value Object / Policy> | `code-domain` | `TODO` |  |  |  |
| `<TYPE>-XXX-T-APP-01` | Application | `<TYPE>-XXX-T-DOM-01` | [<spec.md#section>](../../<path>) | <Use Case / Query / Port> | `code-application` | `TODO` |  |  |  |
| `<TYPE>-XXX-T-INF-01` | Infrastructure | `<TYPE>-XXX-T-APP-01` | [<spec.md#section>](../../<path>) | <Driving or Driven Adapter> | — | `TODO` |  |  |  |
| `<TYPE>-XXX-T-MAIN-01` | Main | `<TYPE>-XXX-T-INF-01` | [<spec.md#section>](../../<path>) | <Composition Root Setup Module> | — | `TODO` |  |  |  |

---

## 2. Sub-Task Definitions (Active Layers Only)
*(Omit any layer not present in the Task Traceability Table.)*

### Domain Tasks

#### `<TYPE>-XXX-T-DOM-01`: <Short Declarative Title>
- **Plan Reference:** [plan.md#domain-layer](./plan.md#domain-layer)
- **Depends On:** None
- **Spec References:**
  - [<Spec Section>](../../<path/to/spec.md#anchor>)
- **Target State (Goal):** <Declarative statement of what domain behavior and contracts must exist upon completion.>
- **Boundaries & Constraints:** <Scope boundaries, e.g., which aggregates or contracts are out of scope.>
- **Definition of Done:**

| Criterion | Result |
| :--- | :--- |
| <Concise, verifiable completion condition linked to the relevant spec where applicable> | Not evaluated |

- **Verification:**

| Scenario | Given | When | Then | Spec Rule |
| :--- | :--- | :--- | :--- | :--- |
| <Scenario name> | <Precondition / domain state> | <Domain action / command> | <Expected state change, event, or invariant rejection> | [<Rule/Anchor>](../../<path/to/spec.md#anchor>) |

### Application Tasks

#### `<TYPE>-XXX-T-APP-01`: <Short Declarative Title>
- **Plan Reference:** [plan.md#application-layer](./plan.md#application-layer)
- **Depends On:** `<TYPE>-XXX-T-DOM-01`
- **Spec References:**
  - [<Spec Section>](../../<path/to/spec.md#anchor>)
- **Target State (Goal):** <Declarative statement of the application orchestration, driving contracts, and driven ports that must exist.>
- **Boundaries & Constraints:** <Scope boundaries, e.g., rely on domain contracts from `<TYPE>-XXX-T-DOM-01`; no adapter concerns.>
- **Definition of Done:**

| Criterion | Result |
| :--- | :--- |
| <Concise, language-agnostic completion condition linked to the relevant spec where applicable> | Not evaluated |

### Infrastructure Tasks

#### `<TYPE>-XXX-T-INF-01`: <Short Declarative Title>
- **Plan Reference:** [plan.md#infrastructure-layer](./plan.md#infrastructure-layer)
- **Depends On:** `<TYPE>-XXX-T-APP-01`
- **Spec References:**
  - [<Spec Section>](../../<path/to/spec.md#anchor>)
- **Target State (Goal):** <Declarative statement of the driving or driven adapter implementation and contract translation required.>
- **Boundaries & Constraints:** <Adapter independence; depend only on application contracts and domain.>
- **Definition of Done:**

| Criterion | Result |
| :--- | :--- |
| <Concise, language-agnostic completion condition linked to the relevant spec where applicable> | Not evaluated |

### Main Tasks

#### `<TYPE>-XXX-T-MAIN-01`: <Short Declarative Title>
- **Plan Reference:** [plan.md#main-composition-root](./plan.md#main-composition-root)
- **Depends On:** `<TYPE>-XXX-T-INF-01`
- **Spec References:**
  - [<Spec Section>](../../<path/to/spec.md#anchor>)
- **Target State (Goal):** <Declarative statement of the composition root module registration and wiring required.>
- **Boundaries & Constraints:** <Construction and lifecycle wiring only; zero business or orchestration logic.>
- **Definition of Done:**

| Criterion | Result |
| :--- | :--- |
| <Concise, language-agnostic condition confirming the required wiring, linked to the relevant spec where applicable> | Not evaluated |
