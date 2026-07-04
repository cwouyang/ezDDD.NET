# ADR Maintenance Checklist

This document provides a checklist for writing and updating ADRs to ensure consistency and avoid common issues.

---

## 📋 Checklist for Writing New ADRs

### Before Writing

- [ ] Check if the decision is significant enough to warrant an ADR (see [README.md](README.md#when-to-write-an-adr))
- [ ] Review existing ADRs to avoid duplication
- [ ] Identify which existing ADRs are related to your new decision

### While Writing

#### 1. File Naming & Structure
- [ ] File name follows format: `NNNN-short-title.md` (4-digit number, kebab-case)
- [ ] Use the next sequential number (check [README.md](README.md#adr-index) for the latest)
- [ ] Copy from [ADR.template.md](ADR.template.md) to ensure all sections are included

#### 2. Status Section
- [ ] Status is set to "Proposed" or "Accepted" (not both)
- [ ] Date is formatted as `YYYY-MM-DD`
- [ ] Status Date matches the decision date

#### 3. Context Section
- [ ] **Problem Statement** clearly describes the decision being made
- [ ] **Relevant Context** includes background from related ADRs
- [ ] **Constraints** lists technical or business limitations

#### 4. Decision Section
- [ ] Decision statement is clear and unambiguous
- [ ] Code examples use **correct syntax** (especially generic constraints)
- [ ] Examples match the actual decision (not copy-paste errors)

#### 5. Consequences Section
- [ ] All three subsections present: Positive, Negative, Neutral
- [ ] Consequences are realistic and specific
- [ ] Trade-offs are clearly explained

#### 6. Alternatives Considered
- [ ] At least 2-3 alternatives documented
- [ ] Each alternative has Pros, Cons, and "Why rejected"
- [ ] Alternatives are genuinely different (not minor variations)

#### 7. **Related Decisions** (CRITICAL)
- [ ] **All referenced ADRs exist** (check README.md index)
- [ ] **Reference format is correct**: `ADR-NNNN (Brief description)`
- [ ] **Bidirectional references are added** (if A references B, update B to reference A)
- [ ] **Use "Related to"** for peer relationships
- [ ] **Use "Affects"** for downstream impacts
- [ ] **Use "Supersedes"** only when replacing an old ADR

#### 8. References Section
- [ ] All external links are valid
- [ ] Links to other project documents use relative paths
- [ ] References to the dotnet-library-design checklist (external tool) are included if applicable

---

## 🔍 Common Errors to Avoid


> **Note**: The examples below are from the **uContract.NET project** and serve as illustrations of the type of issues to watch for in your own project.

### 1. Code Example Consistency ⚠️

**WRONG**:
```csharp
public static T Old<T>(Func<T> supplier) where T : class;  // ❌ Old<T> has NO constraint
```

**CORRECT**:
```csharp
public static T Old<T>(Func<T> supplier);  // ✅ Supports both reference and value types
```

**Always verify**:
- `RequireNotNull<T>` → **HAS** `where T : class` ✅
- `EnsureNotNull<T>` → **HAS** `where T : class` ✅
- `Old<T>` → **NO** constraint ✅
- `EnsureAssignable<T>` → **NO** constraint ✅

### 2. Incorrect Cross-References ⚠️

**WRONG**:
```markdown
Related to: Decision to use System.Text.Json  # ❌ Vague, not an ADR reference
Related to: ADR-0002 (Zero-dependency)        # ❌ ADR-0002 is about naming, not dependencies
```

**CORRECT**:
```markdown
Related to: ADR-0006 (Serialization via System.Text.Json)  # ✅ Correct ADR number
Related to: ADR-0011 (Zero-dependency principle)           # ✅ Correct ADR number
```

**Before referencing an ADR**:
1. Open the ADR file to verify its title and content
2. Use the exact ADR number from README.md index
3. Add a brief description in parentheses

### 3. Missing Bidirectional References ⚠️

**Example**: If you write ADR-0015 that uses AsyncLocal (from ADR-0009):

**In ADR-0015**:
```markdown
## Related Decisions
- **Related to**: ADR-0009 (Thread safety via AsyncLocal)
```

**Also update ADR-0009**:
```markdown
## Related Decisions
...
- **Related to**: ADR-0015 (Feature X also uses AsyncLocal for thread safety)
```

**Rule**: If A → B, then B should mention A (unless B was written first and is immutable).

### 4. Outdated Code Examples ⚠️

When updating ADR-0005 (constraints), remember to update:
- ADR-0003 (API examples)
- ADR-0006 (Old<T> implementation)
- ADR-0007 (EnsureAssignable implementation)

**Verification**: Search all ADRs for the method signature you changed.

### 5. Self-References ⚠️

**WRONG**:
```markdown
# In ADR-0002
Related to: ADR-0002 (Namespace structure)  # ❌ Self-reference
```

**CORRECT**:
```markdown
# In ADR-0002
Affects: ADR-0003 (API design uses this namespace)  # ✅ References other ADR
```

---

## 🔄 Checklist for Updating Existing ADRs

### When to Update
- **Status changes** (Proposed → Accepted, Accepted → Deprecated, etc.)
- **Adding bidirectional references** (another ADR now references this one)
- **Fixing errors** (typos, broken links, incorrect info)

### When NOT to Update
- **Changing the decision itself** → Write a new ADR that supersedes the old one
- **Adding new alternatives** → Only if genuinely overlooked; otherwise write new ADR

### Update Procedure
1. [ ] Add new entry to **Revision History** table at the bottom
2. [ ] Update **Status Date** if status changed
3. [ ] If adding references, verify bidirectional links
4. [ ] Run the [ADR Consistency Check](#adr-consistency-check) below

---

## 🧪 ADR Consistency Check

Run these checks before committing any ADR changes:

### 1. Cross-Reference Validation
```bash
# In dotnet/docs/adr/ directory

# Check for broken ADR references
grep -r "ADR-[0-9]\{4\}" *.md | grep -v "^README" | cut -d: -f2 | grep -o "ADR-[0-9]\{4\}" | sort -u > referenced_adrs.txt

# Verify each referenced ADR exists
for adr in $(cat referenced_adrs.txt); do
  num=$(echo $adr | grep -o "[0-9]\{4\}")
  if [ ! -f "${num}-"*.md ]; then
    echo "❌ Broken reference: $adr (file not found)"
  fi
done
```

### 2. Generic Constraint Check
```bash
# Search for Old<T> with incorrect constraint
grep -n "Old<T>.*where T : class" *.md
# Should return NOTHING (Old<T> has no constraint)

# Search for RequireNotNull without constraint
grep -n "RequireNotNull<T>" *.md | grep -v "where T : class"
# Should return NOTHING (RequireNotNull must have constraint)
```

### 3. Bidirectional Reference Check

For each ADR, verify:
```markdown
If ADR-A references ADR-B, then:
  1. ADR-B exists
  2. ADR-B mentions ADR-A (or is older and immutable)
```

**Manual check**: For each "Related to" in your ADR, open the referenced ADR and verify it mentions yours back.

### 4. Index Update Check
```bash
# Verify README.md index is up-to-date
ls -1 [0-9][0-9][0-9][0-9]-*.md | wc -l
# Compare count with number of entries in README.md "ADR Index"
```

---

## 📝 Example: ADR Relationship Graph

> **Note**: This is an **example from the uContract.NET project** showing how to visualize ADR dependencies. Create a similar graph for your own project.

```
ADR-0001 (Framework)
  └─> ADR-0005 (NRT support)
  └─> ADR-0006 (System.Text.Json)

ADR-0002 (Naming)
  └─> ADR-0003 (API structure)

ADR-0003 (API Design)
  └─> ADR-0004 (Static config)

ADR-0004 (Configuration)
  └─> ADR-0009 (Thread safety)

ADR-0005 (Generics)
  └─> ADR-0006 (Old<T> no constraint)
  └─> ADR-0007 (EnsureAssignable no constraint)

ADR-0006 (Serialization)
  └─> ADR-0005 (No constraint enables value types)
  └─> ADR-0009 (Thread safety)
  └─> ADR-0011 (Zero deps)

ADR-0007 (Reflection)
  └─> ADR-0005 (No constraint enables value types)
  └─> ADR-0006 (Deep copy + comparison)
  └─> ADR-0009 (Thread safety)
  └─> ADR-0011 (Zero deps)

ADR-0008 (Exceptions)
  └─> ADR-0003 (Static API throws exceptions)

ADR-0009 (Thread Safety)
  └─> ADR-0003 (Static design)
  └─> ADR-0006 (Old<T> recursion guard)
  └─> ADR-0007 (EnsureAssignable recursion guard)

ADR-0010 (Testing)
  └─> ADR-0011 (Test deps allowed)

ADR-0011 (Zero Deps)
  └─> ADR-0001 (.NET 8 built-ins)
  └─> ADR-0006 (System.Text.Json)
  └─> ADR-0007 (System.Reflection)
  └─> ADR-0009 (AsyncLocal)
  └─> ADR-0010 (xUnit as dev dep)
```

**How to use this for your project**:
1. List all your ADRs with their main topics
2. Draw arrows showing dependencies (A → B means "A depends on B")
3. Use this graph to identify which ADRs should reference each other
4. Update the graph as you write new ADRs

---

## 🤖 For Claude Code / AI Assistants

When writing or reviewing ADRs, **always**:

1. **Read the ADR README.md first** to understand the current index and workflow
2. **Check this checklist** before finalizing any ADR
3. **Search for the method/concept across all ADRs** before adding examples
4. **Verify ADR numbers** by reading the actual file, not guessing
5. **Update bidirectional references** when adding new "Related to" entries
6. **Use the consistency checks** above before marking work complete

**Common mistakes to avoid**:
- ❌ Copying old ADR examples without verifying current decisions
- ❌ Referencing ADR-NNNN without checking if NNNN exists
- ❌ Adding `where T : class` to `Old<T>()` or `EnsureAssignable<T>()`
- ❌ Forgetting to update the README.md index
- ❌ Not adding bidirectional references

---

## 📚 Additional Resources

- [ADR README.md](README.md) - Main ADR documentation
- [ADR.template.md](ADR.template.md) - ADR template
- The dotnet-library-design checklist (external tool) - Design decisions reference

---

**Last Updated**: 2025-10-18

*This checklist is based on lessons learned from the initial ADR review and fixes performed on 2025-10-18.*
