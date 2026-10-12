# ATF reporting contract — sursă unică de adevăr

Acest document este contractul executabil pentru raportarea API. Dacă un
document istoric descrie altă ierarhie sau alte etichete, acest contract și
testele proiectului au prioritate.

## Invariantul central

Un răspuns HTTP produce exact o pereche de pași în aceeași fază:

    phase -> request -> technical trace + request/response JSON
         -> validation sibling -> Assertions N

Regulile sunt:

- phase -> request + validation sibling -> assertion table;
- validarea este fratele direct al requestului care a produs răspunsul;
- un ApiResponse publică exact o ValidationTable;
- toate regulile pentru acel response sunt rânduri: ASSERTION, EXPECTED,
  ACTUAL, RESULT;
- două răspunsuri diferite nu se combină;
- transportul este diagnostic (Info); verdictul funcțional vine din tabel;
- un request într-o fază de succes fără exact o validare agregată următoare este invalid;
- requestul își publică evidence-ul înainte de închiderea cadrului; requestul se
  închide, apoi validarea se creează ca frate direct și se închide înainte de
  următorul request;
- valorile complete rămân în modelul de aserțiuni; trunchierea se face numai la
  randare.

## Layer ownership

    Atf.Core -> Atf.Common -> Atf.Api
         ^           ^          ^
         |           |          |
         +--- Atf.Reporting.Extent adapter

Atf.Core nu referă ExtentReports și nu conține tipuri de adapter. Contractul
IAtfReportSession, IAtfReporter, IAtfScenarioScope și ReportStepScope este
renderer-neutral. Codul Extent este testat numai în
Atf.Tests/Atf.Reporting.Extent.Tests.

## Lifecycle

## Metric semantics

`ATF tests in this run` is calculated from the direct scenario cards emitted by the current execution
(`Fact` = one case; `Theory` = one case per `InlineData` row). It is not inferred
from Extent class roots, feature groups or visible scenario containers. The
secondary metadata also exposes `API test classes (Extent)` and the native
`Total tests passed`, `Total tests failed` and `Total tests skipped` values.
The native Extent cards, statuses and timeline remain the execution verdict;
the ATF metadata is explanatory navigation context.

ReportStepScope garantează închiderea idempotentă a unui pas. Cleanup-ul este
deținut de CleanupRegistry: se execută în ordine inversă, best-effort, cu
status Pass/Warning, și nu se repetă după prima execuție. O eroare de cleanup
nu înlocuiește failure-ul principal.

Retry-ul păstrează tentativa, headerul, delay-ul și motivul. CancellationToken
este transmis transportului și delay-ului; snapshot-ul de configurație este
capturat la construirea executorului.

## Security

Politica este explicită:

| Date | Implicit |
| --- | --- |
| secrete, Authorization/Bearer, API key, cookie, query secret | mascate obligatoriu |
| PII (email, phone și chei configurate) | păstrate pentru diagnostic |
| PII când PiiEnabled=true | mascate |
| business data non-secret | păstrată |
| exception evidence și body invalid JSON | trec prin redactor |

Redactorul este aplicat la limita de raportare; requestul real primește
valorile originale.

## Extent artifact

Artifactul livrat este un singur HTML autonom. Folderul extent nu este livrat.
Toate suprafețele folosesc tema dark și tokenurile centralizate din
AtfReportColors și AtfReportTypography.

Ordinea Overview este:

1. native Extent cards/charts/timeline;
2. ATF execution metadata (secondary), collapsed implicit;
3. ATF tag audit (secondary navigation metadata), collapsed implicit;
4. Tags și System/Environment, aliniate în două carduri dark;
5. test navigation și evidence.

Panourile secundare `ATF scenario identities by test class`, `Test packages` și
`Other annotations / tags` folosesc exact frame-ul vizual al panourilor `Tags`
și `System/Environment`: același header, aceeași culoare și aceeași suprafață.
Fiecare panou are un singur header deasupra unui tabel; nu se adaugă un rând
total sintetic și nu se introduce un tabel suplimentar în interiorul altui tabel.

Cardul nativ Extent `Log events` nu face parte din suprafața ATF: este eliminat
din dashboard după inițializarea chart-urilor, deoarece dublează agregarea
vizibilă și poate fi confundat cu verdictul testelor. Evenimentele Info/Pass/Fail
rămân păstrate în nodurile de test și în trace-ul fiecărui schimb HTTP.

Metadata secundară afișează `API test cases (Extent)`, `API test classes (Extent)`,
`API scenarios passed`, `API scenarios failed` și `API scenarios skipped`.
Numărul de teste este obținut din cardurile directe de scenariu Extent; class
roots și feature groups sunt context de navigație, nu cazuri suplimentare.

JSON-ul este un code block monospace cu scroll vertical intern și wrap; nu
produce scroll orizontal al paginii. Pașii, phase shells, requestul, validarea
și evidence-ul sunt collapsed implicit. Tag-urile apar o singură dată în
headerul testului și în auditul secundar.

## Definition of done

- unit tests pentru Core, Common, adapter și orchestration;
- test de artifact verifică HTML-ul final, nu doar string-uri din builder;
- live API suite generează HTML în artifacts/reports;
- artifact inspector confirmă tema, self-contained, JSON scroll, tabelul de
  assertions, technical trace și ierarhia request/validation;
- raportul generat este deschis și parcurs în browser MCP înainte de livrare.
