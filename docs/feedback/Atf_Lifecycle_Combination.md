# ATF — lifecycle, combinații și contractul raportării

Registrul activ pentru statusuri, lifecycle, assertions și randarea raportului.
Contractele stabile rămân în `docs/ATF_Reporting_Contract.md` și în
`Atf.Reporting.Extent/README.md`. Acest document nu păstrează căi temporare,
scoruri sau rezultate istorice de rulare.

Procedura obligatorie de rezolvare și matricea TODO → teste → dovadă sunt în
[`Atf_Feedback_Execution_Guide.md`](Atf_Feedback_Execution_Guide.md).

## Modelul normativ

```mermaid
flowchart TD
    Phase[SETUP / TEST BODY / TEARDOWN] --> Request[Request]
    Request --> Trace[TECHNICAL TRACE + JSON]
    Request --> Validation[Validation sibling]
    Validation --> Table[ASSERTION | EXPECTED | ACTUAL | RESULT]
    Table --> Failure[Failure record]
    Failure --> Fails[Fails: listă plată]
    Failure -. deep-link .-> Tests[Tests: nodul sursă]
```

Reguli obligatorii:

- `request` și `validation` sunt frați direcți în aceeași fază;
- transportul este evidence `Info`, nu verdict business;
- un response are un singur tabel agregat de assertions;
- precedence semantică: `Fail > Skip > Warning > Pass`;
- failure-ul de validation nu se repetă printr-un card generic în `TEST BODY`;
- `Fails` păstrează toate occurrences și navighează la nodul sursă;
- fazele, request-urile, validation-urile și evidence-ul pornesc colapsate;
- JSON-ul este code block scrollabil, cu înălțime naturală pentru payload mic;
- tab order: `Tests → Category → Fails → Overview`, cu `Overview` implicit.

## LC-01 — harness DOM automat pentru navigare și triage (rezolvat)

**Priority:** `P1` · **Owner:** `Atf.Reporting.Extent.Tests`

Raportul embed-uiește `window.__atfDomContract.inspect()` ca harness DOM
read-only. Controllerul nu schimbă navigarea, verdictul sau disclosure state;
raportează selectorii și relațiile observabile pentru `Tests`, `Fails`,
collapse, assertion tables și JSON.

**Dovezi:** `ExtentReportClientDomContractController`, testul de compoziție
`ClientBehaviourEmbedsDomContractHarness`, `ReportArtifactInspector` și
verificarea TAB MCP pe artifactul final. Fluxul verificat include click pe
failure, triage și `Open in Tests` cu deep-link către testul sursă.

## LC-06 — evidence fără regresii vizuale (rezolvat)

**Priority:** `P2` · **Owner:** `Atf.Reporting.Extent`

Computed styles sunt colectate de același harness pentru rândurile de navigare,
badge-uri, evidence JSON și overflow. Verificarea TAB MCP a confirmat `display`,
`align-items`, poziția statusului, culorile/fonturile calculate și
`overflow-x/y` pe artifactul final. Contractul static este protejat prin
`ReportArtifactInspector` și testele Extent.

## Regula de acceptare

Un TODO lifecycle se închide numai cu test de politică, test de componentă,
artifact static și verificare DOM pentru comportamentul interactiv. LC-01 și
LC-06 îndeplinesc aceste criterii; nu mai există TODO-uri active în acest
registru. Formatul aprobat al raportului este contract extern și nu se schimbă
accidental.
