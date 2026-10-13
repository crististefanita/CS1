# Atf.Core.Tests — teste pentru kernel și contractul artifactului

Acesta este proiectul de teste pentru `Atf.Core`, grupat fizic sub
`Atf.Tests/Atf.Core.Tests`. Este neutru față de renderer și nu trebuie să importe
detalii de implementare Extent.

Strategia completă test–artifact este în
[ghidul de arhitectură și raportare](../../docs/ATF_Architecture.md).

Acesta este quality gate-ul rapid și offline pentru `Atf.Core`. Nu pornește un
serviciu, nu deschide socket și nu necesită token. Boundary-urile de rețea și
raportare sunt înlocuite cu fake-uri deterministe sau fișiere temporare.

## Aliniere de audit — 2026-10-13

Testele trebuie să apere direcția de dependență și autoritatea rezultatului, nu
să repete testele adaptorului Extent.
Un reporter care aruncă este un failure de infrastructură diagnostică; nu are voie
să schimbe `ApiExecutionResult`, cauza originală sau verdictul funcțional.

Găzduiți aici doar politici renderer-neutral: timeout, cancellation, retry history,
auth state, URL/query encoding, configuration snapshot, redaction și reporting
ports. Testele pentru CSS, DOM, Extent selectors sau computed styles aparțin
adaptorului și browser contractului.

## Contracte protejate

```mermaid
flowchart LR
    C[Configuration] --> Q[Validated settings]
    Q --> H[HTTP contracts]
    H --> E[Executor composition]
    E --> R[Resilience]
    E --> V[Evidence/reporting]
    V --> A[Final HTML artifact]
```

| Grup de teste | Comportament protejat |
|---|---|
| `ConfigurationAndResilienceTests` | binding-ul modulului, validare, retry, polling și formatare JSON |
| `CoreArchitectureTests` | codificarea URL/query, auth, transport, politici de cale și asset-uri |
| `HttpExecutorComponentTests` | rezultatul retry-ului, metadata rutei, evidence JSON și starea auth |
| `ApiExecutorCompositionTests` | porturi injectate și păstrarea excepțiilor |
| `ReportingComponentTests` | precedența statusului clasei și clasificarea evidence-ului |
| `ReportingStructureTests` | ierarhia request + validation sibling deținută de fază, validare agregată, evidence al step-ului activ și suprimarea rezumatelor generice de fază |
| `ReportingLifecycleComponentTests` | compoziția fixture-ului, propagarea identității și finalizare idempotentă |

## Regula de testare

Testele verifică contracte observabile:

```text
fake transport -> ApiExecutionResult
recording reporter -> emitted evidence
temporary HTML -> ReportArtifactInspection
in-memory Extent -> node hierarchy
```

Nu trebuie să verifice apeluri către helperi privați. De exemplu, compoziția
executorului injectează toate porturile și verifică contractul de interacțiune
fără să necesite un `HttpClient` real.

### Politica adnotărilor

Tag-ul generic `framework` este interzis. Proiectul de teste și namespace-ul
identifică deja testele ATF; adnotările sunt păstrate numai când schimbă filtrarea,
politica de execuție sau navigarea raportului. `AnnotationPolicyTests` protejează
sursa scenariului și ordinea configurată a tag-urilor împotriva regresiilor.

## Boundary-ul renderer-ului

Testele Core rămân neutre față de renderer. `AtfAssertion` este contractul semantic
al rândului; formatarea HTML, selectorii Extent, tokenurile temei dark, regulile de
collapse și inspecția artifactului final sunt testate în
`Atf.Tests/Atf.Reporting.Extent.Tests`.

```mermaid
flowchart LR
    Model[In-memory Extent tree] --> Structure[phase/request/validation ownership]
    Model[Assertion/evidence model] --> Port[reporting port]
    Extent[Extent adapter] --> Html[Generated single HTML]
    Html --> Artifact[embedded assets + dark CSS]
    Browser[Browser MCP render] --> UX[collapsed state + zero horizontal overflow]
    Structure --> Acceptance[reporting acceptance]
    Artifact --> Acceptance
    UX --> Acceptance
```

Un test unitar verde nu este suficient pentru contractul final de browser:
artifactul HTML trebuie deschis și verificat după rularea API live.

`ReportArtifactInspector` este dovadă pentru artifactul static; nu îl redenumiți
și nu îl tratați ca test browser. Pentru click, disclosure, scroll și fonturi
calculate este necesar un seam DOM separat.

Aceasta este o dovadă mai puternică decât testarea izolată a unui string CSS.

## Rulare

```powershell
dotnet test .\Atf.Tests\Atf.Core.Tests\Atf.Core.Tests.csproj --no-restore --logger "console;verbosity=normal"
```

Când se schimbă CSS-ul sau structura Extent, rulează acest proiect înaintea
testelor API live.


