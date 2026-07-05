# ezDDD.NET Release Checklist

Releases are published by the automated pipeline in `.github/workflows/publish.yml`:
creating a GitHub Release triggers tag validation, tests, a solution-level
`dotnet pack` (all five packages), a package-count check, a manual approval gate on
the `nuget` environment, the NuGet push, and attaching the `.nupkg` files to the
Release. This checklist covers what must happen before and around that trigger.

The five packages — ezDDD.Common, ezDDD.Entity, ezDDD.UseCase, ezDDD.Cqrs,
ezDDD.Core — share a single version defined in `Directory.Build.props` and are
always released together. See the [Releasing](../CONTRIBUTING.md#releasing) section
of CONTRIBUTING.md for the narrative version of this process.

---

## First-Time Setup

Must exist before the first release (see CONTRIBUTING.md "One-Time Setup"):

- [ ] NuGet account created at <https://www.nuget.org> (with 2FA enabled)
- [ ] API key generated with "Push new packages" permission, scoped to `ezDDD.*`
- [ ] GitHub Environment `nuget` created (Settings > Environments) with
      "Required reviewers" enabled
- [ ] Key stored as the `NUGET_API_KEY` secret in the `nuget` environment

---

## Per-Release Checklist

### 1. CI Green

- [ ] `Build and Test` passes on master (both Ubuntu and Windows jobs, including the
      CSharpier formatting check)

### 2. Version, Changelog, and API Baselines

- [ ] Update `<Version>` in `Directory.Build.props` — the single version source for
      all five packages
- [ ] Move `[Unreleased]` items in `CHANGELOG.md` under the new version heading with
      the release date
- [ ] Promote the public API baselines in **all five** `src/` projects
      (`EzDdd.Common`, `EzDdd.Entity`, `EzDdd.UseCase`, `EzDdd.Cqrs`, `EzDdd.Core`):
      move every entry from `PublicAPI.Unshipped.txt` into `PublicAPI.Shipped.txt`,
      keeping the `#nullable enable` header in both files — see CONTRIBUTING.md

### 3. Local Package Verification

```bash
dotnet clean && dotnet tool restore
dotnet csharpier check .
dotnet test
dotnet pack ezDDD.sln -c Release -o ./artifacts
```

- [ ] All tests pass
- [ ] Exactly **5** `.nupkg` files in `./artifacts` (test projects are
      `IsPackable=false`; `publish.yml` fails the release on any other count)
- [ ] Each package contains `lib/net8.0/EzDdd.*.dll` + `.xml`, `README.md`,
      `icon.png`, `THIRD-PARTY-NOTICES.txt`; no test assemblies
- [ ] `src/EzDdd.*/obj/Release/net8.0/*.sourcelink.json` exists (Source Link intact)
- [ ] Smoke test: install `ezDDD.Core` into a fresh console project from a local
      feed, build an aggregate with a domain event, confirm the transitive packages
      (Common, Entity, UseCase, Cqrs) resolve and IntelliSense/XML docs work

### 4. Commit and Push

```bash
git commit -m "release: prepare v{VERSION}"
git push origin master
```

Version bump, baseline promotions, and CHANGELOG go in this single commit
(per CONTRIBUTING.md).

### 5. Create the GitHub Release (this triggers publishing)

- [ ] Create a GitHub Release with tag `v{VERSION}` targeting master, pasting the
      changelog entry as notes
- [ ] The tag must exactly match `<Version>` in `Directory.Build.props` —
      `publish.yml` validates this and fails the release otherwise
- [ ] Publishing the Release starts `publish.yml`: it re-runs tests, packs all five
      packages, verifies the count is exactly 5, then **waits for manual approval**
- [ ] Approve the `nuget` environment deployment in the Actions tab; the workflow
      then pushes to NuGet and attaches the `.nupkg` files to the Release

> **Warning**: Once pushed to NuGet, a version cannot be deleted — only unlisted.

### 6. Post-Release Verification

- [ ] `publish.yml` run is green
- [ ] All five packages visible on NuGet.org (allow 5–10 minutes for indexing):
      [ezDDD.Common](https://www.nuget.org/packages/ezDDD.Common/),
      [ezDDD.Entity](https://www.nuget.org/packages/ezDDD.Entity/),
      [ezDDD.UseCase](https://www.nuget.org/packages/ezDDD.UseCase/),
      [ezDDD.Cqrs](https://www.nuget.org/packages/ezDDD.Cqrs/),
      [ezDDD.Core](https://www.nuget.org/packages/ezDDD.Core/)
- [ ] Test install `ezDDD.Core` from NuGet.org in a fresh project
- [ ] Add a fresh `[Unreleased]` section to `CHANGELOG.md`
- [ ] Monitor GitHub Issues for bug reports

---

## Rollback

NuGet packages cannot be deleted, only unlisted. All five packages version together,
so any rollback action applies to the whole set.

| Option | When to use |
|--------|-------------|
| **Unlist** | Hide from search (still downloadable by version) — use the NuGet website, all five packages |
| **Hotfix release** | Increment version in `Directory.Build.props`, fix, re-release following this checklist |
| **Deprecate** | Mark as deprecated in package metadata, publish replacement |

After rollback: notify users via GitHub Release notes, document the issue in
`CHANGELOG.md`, and plan the fix.
