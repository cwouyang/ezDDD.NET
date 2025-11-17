# Architecture Decision Records (ADR)

This directory contains Architecture Decision Records (ADRs) for the ezDDD.NET project.

---

## Table of Contents

1. [What is an ADR?](#what-is-an-adr)
2. [When to Write an ADR](#when-to-write-an-adr)
3. [ADR Format and Template](#adr-format-and-template)
4. [Maintenance Workflow and Traceability](#maintenance-workflow-and-traceability)
5. [ADR Planning](#adr-planning)
6. [ADR Index](#adr-index)
7. [Quality Assurance](#quality-assurance)

---

## What is an ADR?

An **Architecture Decision Record (ADR)** is a document that captures an important architectural decision made along with its context and consequences.

**Key principles**:
- **Immutable**: Once accepted, ADRs are not modified (except Status)
- **Contextual**: Records WHY a decision was made, not just WHAT was decided
- **Traceable**: Linked from planning documents for easy reference
- **Versioned**: Changes to decisions require new ADRs that supersede old ones

**Benefits**:
- Provides historical context for future maintainers
- Prevents repeating past discussions
- Makes implicit knowledge explicit
- Helps onboard new team members
- Documents evolution of design philosophy

---

## When to Write an ADR

Create an ADR when making decisions about:

### ✅ Requires ADR
- **Framework and tooling choices** (e.g., .NET version, test framework)
- **Core API design** (e.g., module structure, naming conventions)
- **Architectural patterns** (e.g., event sourcing, CQRS, repository pattern)
- **Dependencies** (e.g., zero third-party dependency policy, ecosystem dependencies, System.Text.Json usage)
- **Breaking changes** (e.g., API redesign, major refactoring)
- **Performance trade-offs** (e.g., reflection vs Expression Trees)
- **DDD tactical patterns** (e.g., aggregate design, domain event hierarchy)

### ⏭️ May Not Need ADR
- Minor bug fixes
- Documentation updates
- Code formatting changes
- Internal refactoring without API impact

**Rule of thumb**: If a decision from [DOTNET_PORT.md](../../DOTNET_PORT.md) is confirmed, write an ADR.

---

## ADR Format and Template

### File Naming Convention

```
NNNN-short-title.md
```

- `NNNN`: 4-digit sequence number (e.g., 0001, 0002, 0003)
- `short-title`: Kebab-case descriptive title
- Examples:
  - `0001-target-framework.md`
  - `0002-package-naming-structure.md`
  - `0006-event-sourcing-aggregate-design.md`

### ADR Template

See [ADR.template.md](ADR.template.md) for the standard template.

**Structure**:
```markdown
# ADR-NNNN: Title

## Status
[Proposed | Accepted | Deprecated | Superseded by ADR-XXXX]

## Context
What is the issue we're facing? What factors are at play?

## Decision
What decision did we make?

## Consequences

### Positive
- What benefits does this decision bring?

### Negative
- What drawbacks or limitations exist?

### Neutral
- What are the trade-offs?

## Alternatives Considered
1. Option A - Why rejected
2. Option B - Why rejected

## Related Decisions
- Related to ADR-XXXX
- Supersedes ADR-YYYY (if applicable)

## References
- Links to relevant documentation
- Discussion threads
- External resources
```

---

## Maintenance Workflow and Traceability

### Source of Truth Hierarchy

```
ADR (docs/adr/*.md)
  ↓ Referenced by
DOTNET_PORT.md
  ↓ Summarized in
CLAUDE.md
```

### 1. ADR = Single Source of Truth

- **Detailed, complete** decision records
- Once Status is `Accepted`, content is **immutable**
- To change a decision, write a **new ADR** that supersedes the old one
- File naming: `0001-target-framework.md`, `0002-package-naming.md`

### 2. DOTNET_PORT.md = Planning Document + ADR Index

- Maintains **overview** of the porting plan
- Each decision includes an **ADR link**
- Example format:

```markdown
## Core Design Decisions

### 1. Complete Reimplementation

**Status**: ✅ Accepted (See [ADR-0005](docs/adr/0005-complete-reimplementation.md))

**Summary**: We will completely reimplement ezddd in .NET rather than wrap existing libraries...
```

### 3. CLAUDE.md = Quick Reference Card

- **Most concise** summary for Claude Code
- Listed format with ADR links
- Example:

```markdown
## Key Design Decisions

> For detailed decisions, see ADR or DOTNET_PORT.md

### Confirmed Decisions

- **Target Framework**: .NET 8+ ([ADR-0001](docs/adr/0001-target-framework.md))
- **Package Naming**: ezDDD/EzDdd convention ([ADR-0002](docs/adr/0002-package-naming-structure.md))
- **Zero Dependencies**: Built-in .NET APIs only ([ADR-0004](docs/adr/0004-zero-dependency-principle.md))
```

### Standard Workflow After Decision Confirmation

```
┌─────────────────────┐
│  Confirm Decision   │
└──────────┬──────────┘
           ↓
┌─────────────────────────────────────┐
│ Step 1: Write ADR                   │
│ - Use ADR.template.md               │
│ - Set Status to "Accepted"          │
│ - Save as docs/adr/NNNN-title.md   │
└──────────┬──────────────────────────┘
           ↓
┌─────────────────────────────────────┐
│ Step 2: Update DOTNET_PORT.md       │
│ - Add summary in relevant section   │
│ - Add ADR link [ADR-NNNN](...)     │
└──────────┬──────────────────────────┘
           ↓
┌─────────────────────────────────────┐
│ Step 3: Update CLAUDE.md            │
│ - Add to "Confirmed Decisions"      │
│ - Include ADR link                  │
└──────────┬──────────────────────────┘
           ↓
┌─────────────────────────────────────┐
│ Step 4: Update docs/adr/README.md   │
│ - Add to ADR Index below            │
│ - Update status and date            │
└─────────────────────────────────────┘
```

### Preventing Conflicts

1. **Unidirectional References**: Only DOTNET_PORT.md and CLAUDE.md reference ADRs, not vice versa
2. **ADR Immutability**: Once Accepted, freeze content; changes require new ADR
3. **Explicit ADR Numbers**: Always include `[ADR-NNNN]` links in all locations
4. **Periodic Sync Check**: When confirming decisions, update all three locations simultaneously

---

## ADR Planning

**28 ADRs planned** covering all major architectural decisions. See detailed planning in:

📋 **[ADR_PLANNING.md](ADR_PLANNING.md)** - Complete ADR roadmap with priorities and dependencies

**Planning Summary**:
- **Priority 1**: Core Architecture (ADR-0001 to 0006) - Foundation (updated 2025-10-31)
  - Includes uContract.NET integration for Design by Contract
- **Priority 2**: Core DDD Patterns (ADR-0007 to 0011) - Tactical DDD
- **Priority 3**: .NET Platform Adaptations (ADR-0012 to 0015) - Platform features
- **Priority 4**: Implementation Details (ADR-0016 to 0019) - Technical choices
- **Priority 5**: CQRS Specific (ADR-0020 to 0023) - CQRS patterns
- **Priority 6**: Cross-Language Considerations (ADR-0024 to 0026) - Java vs .NET
- **Priority 7**: Testing and Quality (ADR-0027 to 0028) - Test strategy

---

## ADR Index

> This section will be updated as ADRs are created.

### Accepted

| ADR | Title | Date | Status |
|-----|-------|------|--------|
| [ADR-0001](0001-target-framework.md) | Target Framework - .NET 8 | 2025-10-31 | Accepted |
| [ADR-0002](0002-package-naming-and-structure.md) | Package Naming and Structure | 2025-10-28 | Accepted |
| [ADR-0003](0003-module-architecture-dependency-chain.md) | Module Architecture and Dependency Chain | 2025-10-31 | Accepted |
| [ADR-0004](0004-zero-third-party-dependency-principle.md) | Zero Third-Party Dependency Principle | 2025-10-31 | Accepted |
| [ADR-0005](0005-complete-reimplementation-approach.md) | Complete Reimplementation Approach | 2025-10-31 | Accepted |
| [ADR-0006](0006-ucontract-integration-design-by-contract.md) | uContract.NET Integration for Design by Contract | 2025-10-31 | Accepted |
| [ADR-0007](0007-ientity-ivalueobject-design.md) | IEntity and IValueObject Design | 2025-11-01 | Accepted |
| [ADR-0008](0008-idomain-event-hierarchy.md) | IDomainEvent Hierarchy Design | 2025-11-01 | Accepted |
| [ADR-0009](0009-aggregate-root-base-class-design.md) | AggregateRoot Base Class Design | 2025-11-01 | Accepted |
| [ADR-0010](0010-esaggregate-root-event-sourcing-implementation.md) | EsAggregateRoot Event Sourcing Implementation (R1, R2, R3 Rules) | 2025-11-01 | Accepted |
| [ADR-0011](0011-event-replay-invariant-checking.md) | Event Replay and Invariant Checking | 2025-11-01 | Accepted |
| [ADR-0012](0012-resource-management-event-bus-producers.md) | Resource Management Pattern for External Event Bus Producers | 2025-11-10 | Accepted |
| [ADR-0013](0013-transaction-boundaries-repository-pattern.md) | Transaction Boundaries in Repository Pattern | 2025-11-10 | Accepted |
| [ADR-0014](0014-domaineventdata-equality-semantics.md) | DomainEventData Equality Semantics | 2025-11-10 | Accepted |
| [ADR-0015](0015-cross-platform-dto-structure.md) | Cross-Platform DTO Structure (InternalDomainEventDto) | 2025-11-10 | Accepted |
| [ADR-0016](0016-async-await-throughout.md) | Async/Await Throughout (All I/O Operations) | 2025-11-10 | Accepted |
| [ADR-0017](0017-cqrsoutput-implementation-strategy.md) | CqrsOutput Implementation Strategy | 2025-11-17 | Accepted |
| [ADR-0018](0018-iarchive-async-method-design.md) | IArchive Async Method Design | 2025-11-17 | Accepted |

### Proposed

*(None yet)*

### Deprecated

*(None yet)*

### Superseded

*(None yet)*

---

## Quality Assurance

### ADR Maintenance Checklist

Before writing or updating any ADR, **consult the [ADR_MAINTENANCE_CHECKLIST.md](ADR_MAINTENANCE_CHECKLIST.md)** to avoid common issues:

- ✅ Generic constraint correctness (especially for aggregate types)
- ✅ Cross-reference accuracy (verify ADR numbers exist)
- ✅ Bidirectional references (if A → B, then B should mention A)
- ✅ Code example consistency across all ADRs
- ✅ Up-to-date ADR index in this README

**Key Rules for ezDDD.NET**:
1. `EsAggregateRoot<TId, TEvent>` → **MUST** have `where TEvent : InternalDomainEvent`
2. `IRepository<TAggregate, TId>` → **MUST** have `where TAggregate : AggregateRoot<TId, InternalDomainEvent>`
3. Always verify ADR numbers before referencing
4. Update bidirectional references when adding new "Related to" entries

### Automated Checks

Run these commands in `docs/adr/` before committing:

```bash
# Check for broken ADR references
grep -r "ADR-[0-9]\{4\}" *.md | grep -o "ADR-[0-9]\{4\}" | sort -u

# Verify all referenced ADRs exist
for adr in $(grep -rho "ADR-[0-9]\{4\}" *.md | sort -u); do
    file="${adr#ADR-}-*.md"
    if ! ls $file 2>/dev/null; then
        echo "Missing: $adr"
    fi
done
```

---

## Notes for Maintainers

### Adding a New ADR

1. Check [ADR_PLANNING.md](ADR_PLANNING.md) for planned ADRs
2. Copy `ADR.template.md` to `NNNN-your-title.md` (use next sequence number)
3. Fill in all sections
4. Set initial Status to "Proposed" or "Accepted"
5. Update this README's [ADR Index](#adr-index)
6. Follow the [maintenance workflow](#standard-workflow-after-decision-confirmation)

### Updating ADR Status

- **Proposed → Accepted**: Decision is finalized
- **Accepted → Deprecated**: No longer recommended but not replaced
- **Accepted → Superseded by ADR-XXXX**: Replaced by a new decision

### Best Practices

- Write ADRs **during** decision-making, not after implementation
- Keep ADRs **concise** but complete (1-3 pages max)
- Focus on **WHY**, not just WHAT
- Include **alternatives considered** to avoid future repetition
- Link to **related ADRs** to build decision graph
- Reference **Java ezddd** when comparing implementations

---

## References

- [ADR Planning Document](ADR_PLANNING.md) - Roadmap of planned ADRs
- [ADR Template](ADR.template.md) - Standard ADR format
- [ADR Maintenance Checklist](ADR_MAINTENANCE_CHECKLIST.md) - Quality guidelines
- [DOTNET_PORT.md](../../DOTNET_PORT.md) - Technical decisions and planning
- [CLAUDE.md](../../CLAUDE.md) - Development guidance
- [Java ezddd](https://gitlab.com/TeddyChen/ezddd) - Original implementation
- [uContract.NET ADRs](../../../uContract.NET/docs/adr/) - Reference implementation

---

*This README follows the ADR maintenance workflow defined above.*
*Last Updated: 2025-11-17*
