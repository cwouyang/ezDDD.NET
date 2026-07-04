# ADR Planning Document for [PROJECT_NAME]

**Created**: YYYY-MM-DD
**Last Updated**: YYYY-MM-DD
**Status**: Planning Phase
**Total ADRs Planned**: [NUMBER]

---

## Purpose

This document provides a **roadmap for all Architecture Decision Records (ADRs)** planned for [PROJECT_NAME]. It helps coordinate ADR writing across implementation phases and manage dependencies between architectural decisions.

**Use this document to**:
- Plan ADRs before implementation begins
- Track ADR writing progress (⬜ → 🔄 → ✅)
- Organize ADRs by priority and dependencies
- Coordinate ADR writing with project phases

---

## When to Use This Template

### ✅ Create an ADR Planning Document When:
- **Large projects** with 10+ expected ADRs
- **Library ports** (e.g., Java → .NET) requiring many architectural decisions
- **Framework development** with phased implementation
- **Complex projects** where ADR dependencies need careful coordination
- **Team projects** where multiple people write ADRs and need coordination

### ⏭️ Skip ADR Planning When:
- **Small projects** with 1-3 ADRs (write ADRs on-demand)
- **Exploratory projects** where decisions emerge during development
- **Single-maintainer projects** with simple decision tracking needs

---

## ADR Planning Workflow

```
┌──────────────────────────────────┐
│ 1. Identify Decision Areas       │  ← List all major architectural topics
└───────────────┬──────────────────┘
                ↓
┌──────────────────────────────────┐
│ 2. List Planned ADRs             │  ← Create entries in this document
└───────────────┬──────────────────┘
                ↓
┌──────────────────────────────────┐
│ 3. Organize by Priority          │  ← Group into P1, P2, P3... by importance
└───────────────┬──────────────────┘
                ↓
┌──────────────────────────────────┐
│ 4. Identify Dependencies         │  ← Mark "Dependencies: ADR-XXXX"
└───────────────┬──────────────────┘
                ↓
┌──────────────────────────────────┐
│ 5. Define Writing Order by Stage │  ← Tie to implementation phases
└───────────────┬──────────────────┘
                ↓
┌──────────────────────────────────┐
│ 6. Write ADRs (use template)     │  ← Use ADR.template.md
└───────────────┬──────────────────┘
                ↓
┌──────────────────────────────────┐
│ 7. Update Progress (⬜ → 🔄 → ✅)      │  ← Mark completion in this document
└──────────────────────────────────┘
```

---

## How to Use This Template

### Step 1: Copy and Customize
1. Copy this template to your project's `docs/adr/ADR_PLANNING.md`
2. Replace `[PROJECT_NAME]` with your actual project name
3. Update metadata (Created, Last Updated dates)

### Step 2: Identify ADR Topics
- Review your project planning documents (e.g., DOTNET_PORT.md, DESIGN_CHECKLIST.md)
- List all significant architectural decisions to be made
- For each decision, create an ADR entry below

### Step 3: Organize by Priority
- **Priority 1**: Foundation decisions (must be made first)
- **Priority 2**: Core functionality decisions (depend on P1)
- **Priority 3**: Implementation details (depend on P2)
- **Priority 4+**: Optional/future decisions

### Step 4: Define Writing Stages
- Tie stages to your project phases (e.g., "Before Phase 1", "During Phase 2")
- Group ADRs that should be written together
- Consider dependencies when ordering

### Step 5: Track Progress
- Use ⬜ for "Not written yet"
- Use 🔄 for "In progress" (optional)
- Use ✅ for "Completed" (ADR file exists and accepted)

---

## Planned ADRs by Priority

> **Instructions**: Group ADRs by priority level. Add as many priority groups as needed.

### 🎯 Priority 1: [Category Name - e.g., Core Architecture]
**Why First**: [Explain why these decisions are foundational - e.g., "These establish the technical foundation for all other decisions"]

#### ADR-0001: [Short Decision Title]
- **Topic**: [Full description of what this ADR will decide]
- **Key Points**:
  - [Key consideration 1]
  - [Key consideration 2]
  - [Key consideration 3]
- **Status**: ⬜ Not written yet
- **Planning Docs Section**: "[Section name in your planning document]"
- **Dependencies**: None (foundational) _or_ ADR-XXXX
- **Related**: ADR-YYYY (optional - for cross-references without strict dependency)

#### ADR-0002: [Another Decision Title]
- **Topic**: [Description]
- **Key Points**:
  - [Point 1]
  - [Point 2]
- **Status**: ⬜ Not written yet
- **Planning Docs Section**: "[Section]"
- **Dependencies**: ADR-0001 (reason why it depends)

---

### 🏗️ Priority 2: [Category Name - e.g., Core Patterns]
**Why Second**: [Rationale - e.g., "These define the main patterns that users will interact with"]

#### ADR-00XX: [Decision Title]
- **Topic**: [Description]
- **Key Points**:
  - [Point 1]
  - [Point 2]
- **Status**: ⬜ Not written yet
- **Planning Docs Section**: "[Section]"
- **Dependencies**: ADR-0001, ADR-0002

---

### ⚙️ Priority 3: [Category Name - e.g., Platform Adaptations]
**Why Third**: [Rationale]

_[Add ADR entries following the same format]_

---

### 🔧 Priority 4+: [Additional Categories]
_[Add more priority groups as needed]_

---

## Suggested Writing Order

> **Instructions**: Group ADRs by implementation stages. Tie stages to your project's development phases.

### Stage 1: [Phase Name - e.g., Before Phase 1 Implementation]
Write ADRs **0001-00XX** to [purpose - e.g., establish foundation]:
- ADR-0001: [Title]
- ADR-0002: [Title]
- [List all ADRs in this stage]

**Timeline**: [When - e.g., "Before starting Phase 1 (Module X)"]
**Rationale**: [Why these should be written together]

---

### Stage 2: [Phase Name - e.g., During Phase 2 Implementation]
Write ADRs **00YY-00ZZ** to [purpose]:
- ADR-00YY: [Title]
- [List ADRs]

**Timeline**: [When]
**Rationale**: [Why]

---

### Stage 3+: [Additional Stages]
_[Add more stages as needed]_

---

## Progress Tracking

| Priority | ADR Range | Total | Completed | Progress |
|----------|-----------|-------|-----------|----------|
| P1       | 0001-00XX | [N]   | 0         | 0%       |
| P2       | 00YY-00ZZ | [N]   | 0         | 0%       |
| P3       | ...       | [N]   | 0         | 0%       |
| **Total** |           | **[TOTAL]** | **0** | **0%** |

_Update this table as ADRs are completed._

---

## Consolidation Opportunities

> **Optional Section**: Identify ADRs that might be merged to reduce duplication.

As you write ADRs, you may discover opportunities to consolidate:

### Example: Merging Platform-Specific ADRs
- ADR-00XX, ADR-00YY, ADR-00ZZ → Merge into "ADR-00XX: [Unified Topic]"
- Rationale: [Why merging makes sense]

_Document potential mergers here, decide during writing phase._

---

## Quality Checklist

Before marking any ADR as "Accepted", verify it meets quality standards.

**→ See [ADR_MAINTENANCE_CHECKLIST.md](ADR_MAINTENANCE_CHECKLIST.md) for the complete checklist.**

**Quick reference** (full checklist in maintenance doc):
- ✅ Context clearly explains the problem
- ✅ Decision is stated unambiguously
- ✅ At least 2 alternatives documented
- ✅ Consequences analyzed (positive/negative/neutral)
- ✅ Related ADRs cross-referenced
- ✅ All three locations updated: ADR file → Project docs → CLAUDE.md

---

## Maintenance

### When to Update This Document
- **New ADRs identified**: Add entries with ⬜ status
- **Priorities change**: Reorganize priority groups
- **ADRs merged or split**: Update numbering and entries
- **Writing progress**: Update status (⬜ → 🔄 → ✅)
- **Dependencies clarified**: Update "Dependencies" and "Related" fields

### Changelog Template
```markdown
**Changelog**:
- YYYY-MM-DD: Initial planning, [N] ADRs planned
- YYYY-MM-DD: Added ADR-XXXX ([Topic]), updated total to [N+1]
- YYYY-MM-DD: Completed ADR-0001 through ADR-000X (Stage 1)
```

---

## How to Customize for Your Project

### 1. Adjust Priority Groups
- **Small projects** (5-10 ADRs): Use 2-3 priority groups
- **Medium projects** (10-20 ADRs): Use 3-5 priority groups
- **Large projects** (20+ ADRs): Use 5-7 priority groups

### 2. Customize Stages
- Tie stages to your project's development phases
- Examples:
  - **Phase-based**: "Before Phase 1", "During Phase 2-3", "Before Phase 4"
  - **Milestone-based**: "Before MVP", "Before Beta", "Before v1.0"
  - **Feature-based**: "Core Features", "Advanced Features", "Optimization"

### 3. Adapt the Entry Format
Add custom fields if helpful:
- **Estimated Effort**: [S/M/L]
- **Assigned To**: [Person/Team]
- **Target Date**: YYYY-MM-DD
- **Blockers**: [List any blockers]

### 4. Integration with Project Docs
- Reference your project's planning documents in "Planning Docs Section"
- Examples: DOTNET_PORT.md, DESIGN.md, ARCHITECTURE.md, CLAUDE.md

---

## References and Examples

### Templates and Checklists
- **[ADR.template.md](ADR.template.md)** - Use this to write each ADR
- **[ADR_MAINTENANCE_CHECKLIST.md](ADR_MAINTENANCE_CHECKLIST.md)** - Quality assurance checklist
- **[README.md](README.md)** - ADR workflow and index

### Real-World Example
- **[ezDDD.NET ADR Planning](https://github.com/cwouyang/ezDDD.NET/blob/master/docs/adr/ADR_PLANNING.md)** _(if public)_
  - Complete example with 28 planned ADRs across 7 priority groups
  - Shows how to organize a library port project with 6 implementation stages
  - Demonstrates dependency tracking and consolidation opportunities

### Additional Resources
- [ADR GitHub Organization](https://adr.github.io/) - ADR best practices
- [Michael Nygard's ADR article](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions) - Original ADR proposal

---

## Notes for Maintainers

### ADR Numbering
- Use 4-digit sequential numbers: 0001, 0002, 0003...
- Reserve number ranges for priority groups if helpful (e.g., 0001-0010 for P1)
- Maintain sequence even if ADRs are written out of order

### Handling Changes
- **Decision changes**: Don't modify existing ADR, write new ADR that supersedes it
- **Planning changes**: Update this document freely (it's mutable, unlike ADRs)
- **Priority shifts**: Reorganize groups, but don't renumber existing ADRs

### Coordination with Team
- Review this planning document in team meetings
- Assign ADRs to team members via custom fields or separate tracking
- Keep this document in version control alongside ADRs

---

**Last Updated**: YYYY-MM-DD

_This ADR Planning template is part of the project-template ADR infrastructure. For methodology and workflow, see [README.md](README.md)._
