# ATF — contractul de raportare și sursa unică de adevăr

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

## Ownership pe straturi

    Atf.Core -> Atf.Api/Module -> Atf.Api
         ^           ^          ^
         |           |          |
         +--- Atf.Reporting.Extent adapter

Atf.Core nu referă ExtentReports și nu conține tipuri de adapter. Contractul
IAtfReportSession, IAtfReporter, IAtfScenarioScope și ReportStepScope este
renderer-neutral. Codul Extent este testat numai în
Atf.Tests/Atf.Reporting.Extent.Tests.

## Lifecycle

ReportStepScope garantează închiderea idempotentă a unui pas. Cleanup-ul este
deținut de CleanupRegistry: se execută în ordine inversă, best-effort, cu
status Pass/Warning, și nu se repetă după prima execuție. Custom teardown-ul
intră într-un `finally` interior înainte de cleanup. O eroare de cleanup nu
înlocuiește failure-ul principal.

Un reporter defect nu trebuie să poată opri acțiunea de resource cleanup; acest
seam este protejat prin teste. Hardening-ul rămas este izolarea lock-urilor și a
operațiilor de filesystem în rulări paralele.

Retry-ul păstrează tentativa, headerul, delay-ul și motivul. CancellationToken
este transmis transportului și delay-ului; snapshot-ul de configurație este
capturat la construirea executorului.

## Semantica metricilor

`ATF tests in this run` se calculează din cardurile directe de scenariu emise de
execuția curentă (`Fact` = un caz; `Theory` = un caz pentru fiecare rând
`InlineData`). Nu se deduce din rădăcini de clase Extent, feature groups sau
containere vizibile de scenarii. Metadata secundară poate expune separat
`API test classes (Extent)` și valorile native `Total tests passed`, `Total tests
failed`, `Total tests skipped`.

Cardurile native Extent, statusurile și timeline-ul rămân autoritatea execuției;
metadata ATF este context de navigare și nu introduce un al doilea verdict.

## Securitate

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

## Artifactul Extent

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

Metadata secundară afișează `ATF tests in this run`, `API test classes (Extent)`,
`Total tests passed`, `Total tests failed` și `Total tests skipped`.
Numărul de teste este obținut din cardurile directe de scenariu Extent; class
roots și feature groups sunt context de navigație, nu cazuri suplimentare.

Ordinea semantică a tab-urilor este:

    Tests -> Category -> Fails -> Overview

`Overview` este ultima filă și se deschide implicit la bootstrap și refresh.
`Fails` apare numai când există failure-uri și conține un singur header cu o
listă plată a tuturor failure-urilor. Fiecare intrare păstrează deep-link-ul
către nodul sursă din `Tests`; gruparea pe clasă nu înlocuiește lista completă.

Un failure deținut de tabelul de validation nu se repetă ca mesaj generic în
`TEST BODY`. Request-ul păstrează evidence-ul HTTP, validation-ul păstrează
cauza contractuală, iar `Fails` doar proiectează recordul de triage.

JSON-ul este un code block monospace cu scroll vertical intern și wrap; nu
produce scroll orizontal al paginii. Pașii, phase shells, requestul, validarea
și evidence-ul sunt collapsed implicit.

Tag-urile au o taxonomie explicită: `TagOrder` doar ordonează, `SelectionTagNames`
alimentează selectorul și adnotările din zona de detalii/scenarii, iar
diagnosticele/necunoscutele rămân metadata vizibilă, dar non-selectabilă.
`AtfTagTaxonomy` este renderer-neutral; class root nu primește categorii.
Lista `Tests` din stânga este navigare pe clase și afișează numai numele clasei,
statusul și durata: nu conține tag-uri, pills, strip-uri sau atribut `tag`
populat. Tag-urile pot fi afișate în header-ele/cardurile scenariilor din dreapta
și în Overview, o singură dată, după deduplicare și ordine, fără să schimbe
Overview/Tags totals. HTML-ul static și DOM-ul hidratat folosesc aceeași politică;
hidratarea clientului nu are voie să reintroducă tag-uri în navigarea stângă.

## Criterii de finalizare

- unit tests pentru Core, Common, adapter și orchestration;
- test de artifact verifică HTML-ul final, nu doar string-uri din builder;
- live API suite generează HTML în artifacts/reports;
- artifact inspector confirmă tema, self-contained, JSON scroll, tabelul de
  assertions, technical trace și ierarhia request/validation;
- raportul generat este deschis și parcurs în TAB MCP înainte de livrare.
