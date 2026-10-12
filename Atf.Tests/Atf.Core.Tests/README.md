# Atf.Core.Tests — kernel and artifact contract tests

This is the fast, offline quality gate for `Atf.Core`. It does not start a service, open a socket or require a token. Network and reporting boundaries are replaced with deterministic fakes or temporary files.

## Protected contracts

```mermaid
flowchart LR
    C[Configuration] --> Q[Validated settings]
    Q --> H[HTTP contracts]
    H --> E[Executor composition]
    E --> R[Resilience]
    E --> V[Evidence/reporting]
    V --> A[Final HTML artifact]
```

| Test group | Protected behavior |
|---|---|
| `ConfigurationAndResilienceTests` | module binding, validation, retry, polling and JSON formatting |
| `CoreArchitectureTests` | URL/query encoding, auth, transport, path and asset policy |
| `HttpExecutorComponentTests` | retry result, route metadata, JSON evidence and auth state |
| `ApiExecutorCompositionTests` | injected ports and exception preservation |
| `ReportingComponentTests` | class status precedence and evidence classification |
| `ReportingStructureTests` | phase sibling hierarchy and active-step evidence |
| `ReportArtifactInspectorTests` | self-contained HTML, dark token, collapsed evidence and table contracts |
| `ReportingLifecycleComponentTests` | fixture composition, identity forwarding and idempotent finalization |

## Testing rule

Tests verify observable contracts:

```text
fake transport -> ApiExecutionResult
recording reporter -> emitted evidence
temporary HTML -> ReportArtifactInspection
in-memory Extent -> node hierarchy
```

They should not assert private helper calls. For example, executor composition injects all ports and verifies the interaction contract without requiring a real `HttpClient`.

## Artifact testing

`ReportArtifactInspector` checks the file a developer opens: custom theme, shared dark token, embedded assets, absent `extent` folder, collapsed details, scrollable JSON, assertion table, technical trace, hidden timestamp column and removed inner phase rail.

This is stronger than testing a CSS string in isolation.

## Run

```powershell
dotnet test .\Atf.Tests\Atf.Core.Tests\Atf.Core.Tests.csproj --no-restore --logger "console;verbosity=normal"
```

When reporting CSS or Extent structure changes, run this project before live API tests.

