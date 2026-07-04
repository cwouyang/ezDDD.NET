// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Naming",
    "CA1716:Identifiers should not match keywords",
    Justification = "Port.In is the Clean Architecture in-port namespace mirrored from Java ezddd's package layout (ezddd.usecase.port.in); renaming would break the structural parity that the port maintains. Visual Basic consumers are not a target audience for this library.",
    Scope = "namespace",
    Target = "~N:EzDdd.UseCase.Port.In"
)]
