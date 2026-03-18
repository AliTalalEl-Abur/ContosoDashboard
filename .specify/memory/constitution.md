<!--
Sync Impact Report
- Version change: template -> 1.0.0
- Modified principles:
	- Template Principle 1 -> I. Training-First, Production-Aware Boundaries
	- Template Principle 2 -> II. Offline-First, Abstraction-Led Infrastructure
	- Template Principle 3 -> III. Authorization and Data Isolation by Default
	- Template Principle 4 -> IV. Spec-Driven Vertical Slices
	- Template Principle 5 -> V. Verification with Reproducible Evidence
- Added sections:
	- Repository Guardrails
	- Delivery Workflow
- Removed sections:
	- None
- Templates requiring updates:
	- ✅ updated .specify/templates/plan-template.md
	- ✅ updated .specify/templates/spec-template.md
	- ✅ updated .specify/templates/tasks-template.md
	- ⚠ pending .specify/templates/commands/*.md (directory not present in this repository)
- Follow-up TODOs:
	- None
-->

# ContosoDashboard Constitution

## Core Principles

### I. Training-First, Production-Aware Boundaries
ContosoDashboard MUST remain a training application first. Features MUST optimize for
clarity, teachable architecture, and local reproducibility before production-scale
complexity. Any production-only concern that is intentionally deferred MUST be called
out explicitly in the spec, plan, or README update rather than silently implied.

Rationale: The repository exists to teach spec-driven development on a realistic but
bounded codebase. Hidden production assumptions would make feature work harder to
evaluate and easier to misapply.

### II. Offline-First, Abstraction-Led Infrastructure
The application MUST run in a local, offline-capable environment without mandatory
cloud dependencies. New infrastructure concerns such as file storage, messaging, or
external integrations MUST be introduced behind explicit interfaces and dependency
injection seams so a production implementation can be swapped without rewriting
business logic or UI flows.

When a feature stores files or similar artifacts, it MUST use generated unique paths,
persist files before committing metadata, and keep private assets outside `wwwroot`
unless a reviewed exception is documented.

Rationale: The training environment must work on any learner machine, while still
teaching the abstraction patterns required for later Azure migration.

### III. Authorization and Data Isolation by Default
Every feature MUST enforce authorization and data isolation across all relevant layers:
page routing, UI actions, service methods, and data access. Access to records, files,
or project-scoped data MUST be granted only through explicit role, ownership, or
membership checks. Direct object references by identifier MUST never bypass these
checks.

Security-sensitive changes MUST define negative-path behavior for unauthorized access,
invalid input, and partial failures.

Rationale: The repository already teaches mock authentication, RBAC, and IDOR
prevention. New work must strengthen those lessons, not route around them.

### IV. Spec-Driven Vertical Slices
Work MUST begin with a spec that defines prioritized user stories, independent test
scenarios, edge cases, functional requirements, and measurable success criteria.
Implementation plans MUST map the feature to the actual ContosoDashboard structure and
identify how each story can be delivered without depending on unfinished lower-priority
slices.

If a story cannot be validated independently, the plan MUST explain the shared
foundation that blocks it and keep that foundation as small as possible.

Rationale: The purpose of the repository is to train the full specification-to-delivery
workflow, not just ad hoc code changes.

### V. Verification with Reproducible Evidence
Every change MUST include verification evidence proportional to its risk. Automated
tests SHOULD be added when the code can be exercised through a practical harness or
when the feature introduces logic that is easy to isolate. Manual verification steps
MUST always be documented for the primary user flow and for critical failure cases,
especially around authorization, validation, and storage behavior.

No feature is complete until the evidence needed to reproduce its correctness is
written down in the spec, plan, tasks, or supporting documentation.

Rationale: This repository currently relies heavily on manual execution and review.
Reproducible evidence keeps training outcomes concrete even when automated coverage is
incremental.

## Repository Guardrails

- The canonical application stack is ASP.NET Core 9, Blazor Server, Razor Pages,
	Entity Framework Core, SQL Server LocalDB, and Bootstrap.
- Features MUST fit within the existing single-project application unless the plan
	documents a compelling reason to expand the structure.
- Mock authentication remains the training default. New features MUST integrate with
	the existing claims- and role-based model rather than introducing alternate login
	mechanisms.
- Repository changes SHOULD preserve existing service-layer separation between UI,
	business rules, and persistence.
- New dependencies require a documented reason, especially if they add operational
	overhead, online requirements, or a second way to solve an existing problem.

## Delivery Workflow

1. Start from the governing artifact that defines the need: stakeholder document,
	 README guidance, or explicit user request.
2. Produce or update the feature spec with user stories, access rules, operational
	 constraints, and verification evidence.
3. Produce the implementation plan and pass the Constitution Check before coding.
4. Break the work into tasks that preserve independent delivery of each user story and
	 include both implementation and validation work.
5. During review, verify the resulting change still satisfies offline execution,
	 authorization rules, and training-focused documentation.

## Governance

This constitution overrides informal project habits when the two conflict. Changes to
the constitution MUST be made explicitly in `.specify/memory/constitution.md`, include
an updated Sync Impact Report, and review affected templates before the amendment is
considered complete.

Versioning policy for this constitution follows semantic versioning:

- MAJOR: Removes or materially redefines a governing principle.
- MINOR: Adds a principle, adds a mandatory section, or materially expands governance.
- PATCH: Clarifies wording without changing expected behavior.

Every implementation plan, task breakdown, and review under Spec Kit MUST check for
compliance with these principles. Any justified exception MUST be documented in the
plan's Complexity Tracking section with the simpler alternative that was rejected.

**Version**: 1.0.0 | **Ratified**: 2026-03-18 | **Last Amended**: 2026-03-18
