# FocusFlow: Agent-First Task Management

## 0. Company
FocusFlow is developed by **Simplifyme Inc.**, creating productivity tools that help people focus on what matters with minimal effort and distraction.

## 1. Executive Summary
**FocusFlow** is a lightweight task management service whose user interface is conversation with the user's preferred agent. People capture tasks, plan their day, and review progress through friendly text or voice exchanges in platforms such as Gemini, ChatGPT, and other agent clients.

FocusFlow provides task data and reliable operations; the connected agent handles conversation. There is no dedicated task management GUI. Open standards and portable contracts make broad platform compatibility a design goal, with support verified per client.

## 2. Core Philosophy
* **Conversation First:** Use everyday language, short replies, and helpful follow-up questions.
* **Agent Independence:** Keep tasks and business rules independent of any model or agent vendor.
* **Standards First:** Publish discoverable tools and documented APIs; minimize platform-specific adapters.
* **Intentional Planning:** Encourage daily triage, achievable focus blocks, and explicit choices about stale tasks.
* **User Control:** Explain consequential changes, allow corrections, and make progress encouraging without pressure.

## 3. Key Features

### Rapid Capture and Natural Language
* Capture tasks through the agent's text or voice capabilities: “Remind me to submit my report every Friday at 3pm.”
* Support due dates, recurrence, priorities, tags, effort estimates, and energy levels.
* Clarify ambiguous dates or task references before saving; resolve dates using the user's timezone.
* Return a concise confirmation with the saved task and schedule so the user can correct it.

### Daily Planning and Time-Blocking
* “What should I focus on today?” produces a manageable plan based on priorities, deadlines, and available time.
* Review unfinished work conversationally: do today, defer, or archive.
* Propose focus blocks and create calendar entries through an authorized calendar integration.
* Deliver reminders through supported, opted-in channels; conversation alone does not guarantee background notifications.

### Flexible Organization and Progress
* Organize work with projects, nested subtasks, tags, related tasks, and Markdown notes.
* Ask for filtered results naturally: “Show low-energy tasks that take less than 15 minutes.”
* Review completed work and optional streaks through brief conversational summaries.
* Keep task state consistent when the user switches agents or devices.

## 4. Interoperability and Standards
* **Model Context Protocol (MCP):** Expose discoverable task tools through a remote MCP server using Streamable HTTP and a documented supported protocol version. Follow the [MCP specification](https://modelcontextprotocol.io/specification/latest).
* **HTTP API and OpenAPI:** Publish a versioned JSON API described with [OpenAPI](https://www.openapis.org/what-is-openapi) for clients and integrations that cannot use MCP directly.
* **Shared Contracts:** Use explicit schemas, stable task IDs, pagination, structured errors, and consistent operations across both interfaces.
* **Authorization:** Follow MCP authorization requirements for remote access; use scoped, revocable user authorization and enforce data ownership on every operation.
* **Portable Data:** Support JSON import/export and iCalendar exchange for scheduled items; avoid vendor-specific task formats as the source of truth.
* **Client Compatibility:** Document and test connection, authorization, and tool use for each supported platform. Add thin adapters where necessary; do not assume every Gemini or ChatGPT surface supports the same integration features.

## 5. Technical Architecture
| Layer | Technology / Approach | Purpose |
| :--- | :--- | :--- |
| User interaction | External agent clients, including Gemini and ChatGPT where supported | Friendly text and voice conversation. |
| Agent integration | MCP server and OpenAPI-described HTTP API | Standard access to task capabilities. |
| Application and domain | .NET | FocusFlow domain model and domain model orhestration. |
| Persistence | PostgreSQL | Durable, user-scoped task data shared across clients. |
| Background processing | .NET workers and authorized delivery integrations | Recurrence, reminders, and calendar synchronization. |

Agents interpret requests and invoke tools; the service validates inputs and applies business rules. Keep model-specific prompts and adapters outside the domain. Hosting is service-based; local-first storage and offline synchronization are outside the initial scope.

## 6. Conversational Experience and Tool Behavior
* Offer focused operations to create, find, update, complete, archive, and restore tasks, plus plan and review a day.
* Example: “Move my report to Monday” → clarify which report if needed → save → confirm the task, full date, time, and timezone.
* Return structured results with a short human-readable summary; never claim a change succeeded before persistence succeeds.
* Support retry-safe writes and detect conflicting updates so multiple agents cannot silently duplicate or overwrite work.
* Provide previews and require confirmation for bulk deletion or broad rescheduling; keep an audit history and support undo where feasible.
* Treat task text and imported notes as user data, never as instructions granting additional tool permissions.

## 7. Success Metrics
* Task capture success rate and median time from request to saved task.
* Successful daily planning, task completion, and reminder delivery rates.
* Low clarification and correction rates without silently guessing user intent.
* Verified end-to-end operation through at least two independent agent platforms.
* Reliable cross-client consistency, data export, and recovery from failed or repeated tool calls.
