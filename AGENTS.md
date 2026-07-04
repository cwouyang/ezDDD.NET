# ezDDD.NET

Port of [Java ezddd 6.0.1](https://gitlab.com/TeddyChen/ezddd) (commit `3aac0f5`), ~99% semantic parity. Tactical DDD, CQRS, and Clean Architecture for .NET 8, with both state sourcing and event sourcing.

## Commands

- Build: `dotnet build ezDDD.sln`
- Test all: `dotnet test`
- Single test: `dotnet test --filter "FullyQualifiedName~TestName"`
- Pack: `dotnet pack`
- Format check: `dotnet tool restore && dotnet csharpier check .`

## Module Architecture

```
EzDdd.Common → EzDdd.Entity → EzDdd.UseCase → EzDdd.Cqrs → EzDdd.Core (aggregator)
```

Package IDs use `ezDDD.*` (brand), namespaces use `EzDdd.*` (.NET convention).

## Development Standards

### Workflow

Always follow: **Plan → Confirm → Execute**

1. Present approach before writing code
2. Wait for explicit user approval
3. Implement the approved plan

### Testing

**Never write implementation code before a failing test.** Follow TDD (Red → Green → Refactor). xUnit, AAA pattern, no mocking libraries.

### Code Changes

Never mix structural and behavioral changes in the same commit (Tidy First).

### Architecture Decisions

- Location: `docs/adr/`
- ADRs are the source of truth
- Check existing ADRs before making architectural suggestions

## Key Rules

- **Event sourcing correctness (R1–R3)**: construction events establish invariants, command events must hold them before and after, destruction events may break them last — enforced by `EsAggregateRoot.Apply()`; see [ADR-0011](docs/adr/0011-event-replay-invariant-checking.md).
- **Level Z analyzers**: Meziantou + Roslynator + a `.editorconfig` that promotes style suggestions to warnings; `TreatWarningsAsErrors` makes the build fail on any of them. Fix findings, don't suppress without an inline rationale.
- **PublicAPI baseline**: PublicApiAnalyzers tracks each `src/` project's API surface — new public members must be added to that project's `PublicAPI.Unshipped.txt` (promoted to `Shipped` at release; see CONTRIBUTING.md).
- **CSharpier is the only formatter**: never hand-format or run IDE cleanup profiles other than CSharpier; CI runs `dotnet csharpier check .`.

## Gotchas

- `DomainEventTypeMapper` is a process-global static registry. Tests touching it must share a consistent mapping; follow the `[Collection("DomainEventTypeMapper")]` precedent in `tests/EzDdd.Entity.Tests`.
- The `EsAggregateRoot` replay constructor intentionally calls virtual `_When()` while rebuilding state — derived classes must not depend on fields initialized in their own constructors.
- Soft-deleted aggregates (`IsDeleted == true`) are filtered by `OutboxRepository.FindByIdAsync` (returns `null`) but remain in storage so their domain events can still be relayed.
