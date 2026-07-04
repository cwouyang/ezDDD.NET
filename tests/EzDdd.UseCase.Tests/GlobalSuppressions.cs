// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "The test namespace mirrors the production EzDdd.UseCase.Port.In namespace (itself mirrored from Java ezddd's package layout and suppressed for the same reason); keeping the folder structure aligned makes each test discoverable next to its subject.",
    Scope = "namespace",
    Target = "~N:EzDdd.UseCase.Tests.Port.In"
)]
