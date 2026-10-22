# Section guide

An aggregate defines a consistency boundary. Work through its design iteratively.

| Section | What to document |
| --- | --- |
| Bounded Context Name | The owning context, as supplied or documented in the project. This section is a local addition. |
| Name | A domain name; include a lifecycle period when meaningful. |
| Description | Responsibilities, purpose, boundary rationale, and tradeoffs against alternative designs. |
| State Transitions | Possible states and transitions, using a list or Markdown table. Consider whether complex transitions suggest misplaced process boundaries or trivial transitions suggest displaced behavior. |
| Enforced Invariants | Business conditions the aggregate consistently protects within its boundary. Many invariants may indicate local complexity. |
| Corrective Policies | Business actions that repair inconsistencies when boundaries relax invariants. Distinguish these from rules enforced immediately; numerous policies may shift complexity outside the aggregate. |
| Handled Commands | Requests the aggregate handles; connect each to resulting events where applicable. |
| Created Events | Business outcomes emitted by the aggregate; cross-check against commands to find missing behavior. |
