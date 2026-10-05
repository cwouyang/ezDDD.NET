# Contributing to ezDDD.NET

## Setup

```bash
git clone https://github.com/cwouyang/ezDDD.NET.git
cd ezDDD.NET
dotnet test
```

## Development Setup

Rider configuration (one-time, per-developer):

1. Install the CSharpier plugin from the Rider marketplace; restart Rider.
2. Settings -> Tools -> CSharpier: enable "Run on save" and "Reformat code on save". Pin the plugin version to match the CLI version in `.config/dotnet-tools.json` (currently `1.2.6`).
3. Settings -> Keymap: rebind `Ctrl+Alt+L` (Reformat Code) to the CSharpier Format action so the IDE shortcut matches CI.
4. Settings -> Editor -> Code Cleanup: create an `ezDDD-safe` profile containing only "CSharpier Format". Do not run Full Cleanup on this codebase.
5. Settings -> Editor -> Code Style -> C#: enable "Load from EditorConfig" so `.editorconfig` governs per-project style.
6. Run once per clone:

   ```bash
   git config blame.ignoreRevsFile .git-blame-ignore-revs
   ```

To verify, introduce a whitespace error in any `.cs` file and trigger Rider Reformat. The result must be byte-identical to `dotnet csharpier format` output; if it differs, re-check the plugin version pin and the CSharpier-only cleanup profile.

## Code Quality Tools

### Daily Verification

Run before every commit:

```bash
dotnet tool restore
dotnet csharpier check .
dotnet build -p:ContinuousIntegrationBuild=true
dotnet test --no-build --verbosity normal
dotnet build -c Release -p:ContinuousIntegrationBuild=true
dotnet test -c Release --no-build --verbosity normal
```

Run the tests with no `DBC*` environment variable set (`DBC`, `DBC_PRE`, `DBC_POST`, `DBC_INV`, `DBC_CHECK`). uContract, a dependency of this library, reads them from the process environment to switch contract checks on or off, so a value set in your shell changes what the tests observe.

### Reproducing CI Locally

The GitHub Actions workflow runs the same steps in this order:

```bash
dotnet tool restore
dotnet csharpier check .
dotnet build -p:ContinuousIntegrationBuild=true --no-restore
dotnet test --no-build --verbosity normal
dotnet build -c Release --no-restore -p:ContinuousIntegrationBuild=true
dotnet test -c Release --no-build --verbosity normal
```

If all of them succeed locally, CI will succeed.

### Upgrading CSharpier

Land each step as its own commit so the formatting-only commit can be added to `.git-blame-ignore-revs` cleanly:

1. Bump the version in `.config/dotnet-tools.json`.
   Commit: `chore(build): bump CSharpier to <new-version>`
2. `dotnet tool restore && dotnet csharpier format .`
   Commit: `style: apply CSharpier <new-version> reformat`
3. Append the SHA of the reformat commit to `.git-blame-ignore-revs`.
   Commit: `chore: add CSharpier <new-version> reformat to blame-ignore list`

### Upgrading Roslynator.Analyzers

1. Bump the `Version` attribute on the `Roslynator.Analyzers` `PackageReference` in `Directory.Build.props`.
   Commit: `chore(build): bump Roslynator.Analyzers to <new-version>`
2. Address any new diagnostics, one logical fix group per commit.
   Commit: `refactor: fix RCSxxxx <description>`

The same two-step procedure applies to `Meziantou.Analyzer` (also referenced in `Directory.Build.props`).

## Pull Requests

- One concern per PR -- do not mix refactoring with behavioral changes
- Tests are required for all changes
- For API changes, open an issue first

## Releasing

1. Create a branch `chore/release-{version}` from an up-to-date `master`. `master` is protected, so the release preparation reaches it through a pull request
2. Update `<Version>` in `Directory.Build.props` -- the single version source for all five packages (ezDDD.Common, ezDDD.Entity, ezDDD.UseCase, ezDDD.Cqrs, ezDDD.Core)
3. Promote the public API baselines -- each of the five `src/` projects tracks its own baseline pair:
   - In every project (`src/EzDdd.Common`, `src/EzDdd.Entity`, `src/EzDdd.UseCase`, `src/EzDdd.Cqrs`, `src/EzDdd.Core`), move every entry from `PublicAPI.Unshipped.txt` to `PublicAPI.Shipped.txt`
   - Keep the `#nullable enable` header in both files; Unshipped retains only the header after promotion
   - This makes the five `PublicAPI.Shipped.txt` files the canonical record of the APIs committed at `v{version}`
4. Update `CHANGELOG.md` -- move Unreleased items under the new version heading, leave `_No changes yet._` under a fresh `## [Unreleased]`, and update the link references at the bottom
5. Run the tests in both configurations (`dotnet test` and `dotnet test -c Release`), as CI does, with no `DBC*` environment variable set (see [Daily Verification](#daily-verification))
6. Commit: `chore: Prepare release {version}` (version bump, baseline promotions, and CHANGELOG in a single commit)
7. Push the branch and open a pull request to `master`; merge it once the `build (ubuntu-latest)` and `build (windows-latest)` checks pass
8. On GitHub, create a Release with tag `v{version}` targeting the merge commit on `master` -- the tag must match `<Version>` in `Directory.Build.props`
9. The publish workflow runs automatically:
   - Validates the tag against the `Directory.Build.props` version
   - Runs tests and packs all five NuGet packages (fails if the count is not exactly 5)
   - Waits for manual approval (check the Actions tab)
10. Approve the deployment in the Actions tab
11. All five packages are published to NuGet.org and attached to the GitHub Release

### One-Time Setup

Publishing uses [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
(OIDC) -- the workflow exchanges a short-lived GitHub token for a one-hour NuGet
API key at publish time, so no long-lived API key is stored. Before the first release:

1. GitHub repo > Settings > Environments > create `nuget`
2. Enable "Required reviewers" > add yourself
3. Under "Deployment branches and tags", add a tag rule `v*` -- only release tags may deploy. The publish run is triggered by the Release, so its ref is the tag
4. On nuget.org, go to your username > **Trusted Publishing** and add a policy:
   Repository Owner `cwouyang`, Repository `ezDDD.NET`, Workflow File `publish.yml`,
   Environment `nuget`
5. Add `NUGET_USER` as an environment secret in the `nuget` environment -- your
   nuget.org username (profile name), not your email address
