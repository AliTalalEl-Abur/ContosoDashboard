# Implementation Plan: [FEATURE]

**Branch**: `[###-feature-name]` | **Date**: [DATE] | **Spec**: [link]
**Input**: Feature specification from `/specs/[###-feature-name]/spec.md`

## Summary

[Extract the primary requirement, affected user roles, and implementation shape.
Call out any security-sensitive or storage-sensitive behavior explicitly.]

## Technical Context

**Language/Version**: C# / ASP.NET Core 9 / Blazor Server  
**Primary Dependencies**: ASP.NET Core Razor Pages + Blazor Server, Entity Framework Core, SQL Server provider, Bootstrap 5  
**Storage**: SQL Server LocalDB for application data; local filesystem outside `wwwroot` for any uploaded file content  
**Testing**: Reproducible manual validation is required; add automated tests when a practical harness exists or new logic is isolated enough to justify one  
**Target Platform**: Local developer workstation, offline-capable training environment  
**Project Type**: Single web application (`ContosoDashboard/`)  
**Performance Goals**: Define feature-specific interactive response goals and any upload/search budgets from the spec  
**Constraints**: Training-only scope, offline-first behavior, role-based authorization, no major architectural rewrites, preserve cloud-migration seams  
**Scale/Scope**: Fit the existing ContosoDashboard application and current role model without introducing parallel platforms

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- `Training-First, Production-Aware Boundaries`: The plan preserves the repo's training purpose, avoids production-only complexity by default, and documents any production follow-up separately.
- `Offline-First, Abstraction-Led Infrastructure`: The feature works locally without mandatory cloud dependencies. Any new infrastructure dependency is hidden behind an interface and configuration seam.
- `Authorization and Data Isolation by Default`: UI, service, and data-access paths define who can access which records/files and how unauthorized access is rejected.
- `Spec-Driven Vertical Slices`: The work is decomposed into independently testable user stories with clear acceptance scenarios and edge cases.
- `Verification with Reproducible Evidence`: The plan names the automated tests and/or manual validation steps that will prove the change works, including negative-path checks for security or validation rules.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
└── tasks.md
```

### Source Code (repository root)

```text
ContosoDashboard/
├── Data/
├── Models/
├── Pages/
├── Services/
├── Shared/
├── wwwroot/
├── Program.cs
└── ContosoDashboard.csproj
```

**Structure Decision**: Use the existing single-project Blazor Server structure. Put data models in `Models/`, persistence wiring in `Data/`, business rules in `Services/`, routed UI in `Pages/`, shared UI in `Shared/`, and static assets in `wwwroot/`.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., direct cloud dependency] | [current need] | [why offline-compatible abstraction was insufficient] |
| [e.g., relaxed authorization rule] | [specific constraint] | [why standard role/project isolation could not be preserved] |
