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

**Rule of thumb**: If an architectural decision is confirmed, write an ADR.

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
  - `0002-package-naming-and-structure.md`
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
  ↓ Summarized in
AGENTS.md
```

### 1. ADR = Single Source of Truth

- **Detailed, complete** decision records
- Once Status is `Accepted`, content is **immutable**
- To change a decision, write a **new ADR** that supersedes the old one
- File naming: `0001-target-framework.md`, `0002-package-naming.md`

### 2. AGENTS.md = Quick Reference Card

- **Most concise** summary of the rules that affect day-to-day development
- References ADRs for the full rationale (e.g. the R1–R3 event sourcing rules point to ADR-0011)

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
│ Step 2: Update docs/adr/README.md   │
│ - Add to ADR Index below            │
│ - Update status and date            │
└──────────┬──────────────────────────┘
           ↓
┌─────────────────────────────────────┐
│ Step 3: Update AGENTS.md if the     │
│ decision changes day-to-day rules   │
└─────────────────────────────────────┘
```

### Preventing Conflicts

1. **Unidirectional References**: Guidance documents reference ADRs; ADRs do not depend on guidance documents
2. **ADR Immutability**: Once Accepted, freeze content; changes require new ADR
3. **Explicit ADR Numbers**: Always include `[ADR-NNNN]` links in all locations
4. **Periodic Sync Check**: When confirming decisions, update the ADR, this index, and AGENTS.md together

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
| [ADR-0019](0019-iinquiry-iprojection-independence.md) | IInquiry and IProjection Independence from IUseCase | 2025-11-17 | Accepted |
| [ADR-0021](0021-generic-variance-annotations.md) | Generic Variance Annotations for CQRS Interfaces | 2025-11-18 | Accepted |
| [ADR-0022](0022-read-model-design-patterns.md) | Read Model Design Patterns | 2025-11-18 | Accepted |
| [ADR-0023](0023-archive-idempotency-requirements.md) | Archive Idempotency Requirements | 2025-11-18 | Accepted |
| [ADR-0024](0024-ireconciler-interface-system-reconciliation.md) | IReconciler Interface for System State Reconciliation | 2026-01-07 | Accepted |
| [ADR-0025](0025-messageproducer-refactoring-java-4-1-0-alignment.md) | MessageProducer Refactoring - Java 4.1.0 Alignment | 2026-01-07 | Accepted (Amended by [ADR-0029](0029-messageproducer-removal-gateway-deferral.md)) |
| [ADR-0026](0026-service-layer-pattern.md) | Service Layer Pattern for Complex Business Logic | 2026-01-08 | Accepted |
| [ADR-0027](0027-thread-null-safety-review.md) | Thread Safety and Null Safety Review (Java 4.1.0 Sync - Stage S5) | 2026-01-08 | Accepted |
| [ADR-0028](0028-reactor-hierarchy-projector-notifier-genericization.md) | Reactor Type Hierarchy and Projector/Notifier Genericization | 2026-07-04 | Accepted |
| [ADR-0029](0029-messageproducer-removal-gateway-deferral.md) | MessageProducer Removal from Core & Gateway Package Deferral | 2026-07-04 | Accepted |

### Proposed

*(None yet)*

### Deprecated

*(None yet)*

### Superseded

| ADR | Title | Date | Status |
|-----|-------|------|--------|
| [ADR-0020](0020-iprojector-lifecycle-management.md) | IProjector Lifecycle Management Integration | 2025-11-18 | Superseded by [ADR-0028](0028-reactor-hierarchy-projector-notifier-genericization.md) |

---

## Quality Assurance

### ADR Quality Checklist

Before writing or updating any ADR, check for these common issues:

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

1. Copy `ADR.template.md` to `NNNN-your-title.md` (use next sequence number)
2. Fill in all sections
3. Set initial Status to "Proposed" or "Accepted"
4. Update this README's [ADR Index](#adr-index)
5. Follow the [maintenance workflow](#standard-workflow-after-decision-confirmation)

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

- [ADR Template](ADR.template.md) - Standard ADR format
- [AGENTS.md](../../AGENTS.md) - Development guidance
- [Java ezddd](https://gitlab.com/TeddyChen/ezddd) - Original implementation
- [uContract.NET ADRs](https://github.com/cwouyang/uContract.NET/tree/master/docs/adr) - Reference implementation

---

*This README follows the ADR maintenance workflow defined above.*
*Last Updated: 2026-07-04*
