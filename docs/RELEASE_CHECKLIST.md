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
- [ ] [Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
      policy added on nuget.org (your username > Trusted Publishing):
      Repository Owner `cwouyang`, Repository `ezDDD.NET`, Workflow File
      `publish.yml`, Environment `nuget`
- [ ] Repository is **public** before enabling the approval gate below --
      the "Required reviewers" environment rule needs a public repo (or GitHub
      Pro/Team/Enterprise for a private one; the API returns HTTP 422 otherwise)
- [ ] GitHub Environment `nuget` created (Settings > Environments) with
      "Required reviewers" enabled (add yourself; leave "Prevent self-review"
      off so a solo maintainer can approve their own release); its deployment rule
      is a tag rule `v*` (Deployment branches and tags), so only release tags may
      deploy — the publish run is triggered by the Release, so its ref is the tag
- [ ] `NUGET_USER` secret (your nuget.org username, not email) stored in the
      `nuget` environment

---

## Per-Release Checklist

### 1. CI Green

- [ ] `Build and Test` passes on master (both Ubuntu and Windows jobs, including the
      CSharpier formatting check)

### 2. Version, Changelog, and API Baselines

- [ ] Create the branch `chore/release-{VERSION}` from an up-to-date `master`
      (`master` is protected; the changes below reach it through a pull request)
- [ ] Update `<Version>` in `Directory.Build.props` — the single version source for
      all five packages
- [ ] Move `[Unreleased]` items in `CHANGELOG.md` under the new version heading with
      the release date, leave `_No changes yet._` under a fresh `## [Unreleased]`, and
      update the `[Unreleased]` and version link references at the bottom
- [ ] Promote the public API baselines in **all five** `src/` projects
      (`EzDdd.Common`, `EzDdd.Entity`, `EzDdd.UseCase`, `EzDdd.Cqrs`, `EzDdd.Core`):
      move every entry from `PublicAPI.Unshipped.txt` into `PublicAPI.Shipped.txt`,
      keeping the `#nullable enable` header in both files — see CONTRIBUTING.md

### 3. Local Package Verification

```bash
dotnet clean && dotnet tool restore
dotnet csharpier check .
dotnet test
dotnet test -c Release
dotnet pack ezDDD.sln -c Release -o ./artifacts
```

- [ ] All tests pass in both Debug and Release, with no `DBC*` environment variable
      set (see CONTRIBUTING.md "Daily Verification")
- [ ] Exactly **5** `.nupkg` files in `./artifacts` (test projects are
      `IsPackable=false`; `publish.yml` fails the release on any other count)
- [ ] Each package contains `lib/net8.0/EzDdd.*.dll` + `.xml`, `README.md`,
      `icon.png`, `THIRD-PARTY-NOTICES.txt`; no test assemblies
- [ ] `src/EzDdd.*/obj/Release/net8.0/*.sourcelink.json` exists (Source Link intact)
- [ ] Smoke test: install `ezDDD.Core` into a fresh console project from a local
      feed, build an aggregate with a domain event, confirm the transitive packages
      (Common, Entity, UseCase, Cqrs) resolve and IntelliSense/XML docs work

### 4. Commit and Open a Pull Request

```bash
git commit -m "chore: Prepare release {VERSION}"
git push -u origin chore/release-{VERSION}
```

Version bump, baseline promotions, and CHANGELOG go in this single commit
(per CONTRIBUTING.md).

- [ ] Open a pull request to `master`
- [ ] Merge it once `build (ubuntu-latest)` and `build (windows-latest)` pass

### 5. Create the GitHub Release (this triggers publishing)

- [ ] Create a GitHub Release with tag `v{VERSION}` targeting the merge commit on `master`, pasting the
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
- [ ] Monitor GitHub Issues for bug reports

---

## Recovery

- If a run fails before any package was pushed because of a transient problem (a flaky
  test, a runner error), re-run the failed jobs. Re-running only the `publish` job
  downloads the package artifact, which is kept for 7 days; after that, re-run all
  jobs.
- If the cause is in the repository (the tag does not match `<Version>`, a failing
  test), re-running cannot help: the run always uses the commit the tag points to.
  While nothing has been pushed, delete the Release and the tag, merge the fix through
  a pull request, and create the Release again on the new merge commit.
- The push uses `--skip-duplicate`: a package version that already exists on NuGet.org
  is skipped and the push still succeeds, so re-running a run that stopped part-way
  through the push publishes only the missing packages.
- Once any package has been pushed, do not delete the tag or the Release — a version
  number cannot be reused on NuGet.

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
