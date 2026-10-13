# ATF — analiză profundă a combinațiilor de lifecycle și a raportului

**Data:** 2026-10-13  
**Tip:** analiză live consolidată + contract de raportare și refactorizare  
**Raport verificat în TAB MCP:** `C:\work\ex\.net\.tmp\atf-feedback-lifecycle-live-20261013\artifacts\reports\api-atf-report_2026-10-13_13-29-40.html`  
**Raport de referință comparat:** `C:\work\ex\.net\Atf\artifacts\reports\api-atf-report_2026-10-13_11-30-41.html`  
**Comparație:** worktree-ul ATF față de `origin/main`  
**Modificări de cod în auditul curent:** da; assertion status, failure correlation și matricea lifecycle au fost implementate și testate  
**Build/test în auditul curent:** build staging 0 warnings/0 errors; 306/306 teste offline; rularea API completă a trecut 19 Pass, 8 Skip, 0 Fail la nivel xUnit; artifactul Extent de regresie conține intenționat 13 Pass, 2 Fail, 4 Skip și 4 Warning pentru demonstrarea triage-ului

### Update după ultimul feedback

Feedback-ul rămas a fost aplicat în codul live, apoi sincronizat într-un staging separat pentru verificare:

- `Atf.Api.Tests/ApiScenarioLifecycleRunnerTests.cs` are acum o matrice de 12 combinații fără HTTP real: fail în setup/body/teardown, warning înainte și după fail, skip cu teardown warning/fail, skip în setup și warning-uri în toate fazele;
- `ValidationTable` publică explicit `ResultStatus = Fail` pentru rândul „0 assertions registered”, astfel încât fallback-ul nu mai depinde de inferența din `Passed=false`;
- `ExtentReportClientFailureFragments` construiește record-uri de triage per assertion `Fail`, nu doar per scenariu. Resolver-ul caută validation-ul și request-ul frate, păstrând cardinalitatea când există mai multe request-uri sau assertion-uri failing în aceeași fază;
- testele Extent verifică acest contract, iar suitele API unit, Common, Core și Extent au fost rulate separat în staging pentru a evita paralelismul Razor/Extent.

Rularea întregii soluții într-un singur lot paralel a expus un risc de mediu, nu un failure funcțional: 3 finalizări Extent simultane au primit `OutOfMemoryException` în compilatorul Razor. Suitea Extent izolată a trecut `111/111`; validarea oficială este secvențială pe suite, în staging. Numărul offline curent este `52 API + 82 Core + 61 Common + 111 Extent = 306/306`.

În plus, `configureFailureView()` rezolvă acum evidence-ul înainte de a publica
`data-atf-evidence-id` pe rândul Failure. Ordinea era vulnerabilă la o referință
la `evidence` înainte de declarație, ceea ce putea rupe configurarea unui failure
existent în runtime. Regresia este protejată de un test component dedicat.

### Evidența ultimului artifact verificat în TAB MCP

Artifactul complet `api-atf-report_2026-10-13_13-29-40.html` este sursa curentă de verificare, iar copia lui a fost sincronizată în `C:\work\ex\.net\Atf\artifacts\reports`. În Tab MCP au fost parcurse Overview → Fails → Tests, cu statusuri și scenarii diferite. Overview confirmă `13 Pass`, `2 Fail`, `4 Skip`, `4 Warning`; Fails listează exact cele două failure-uri deliberate: `Users API — warning → fail precedence` și `Users API — pass → teardown fail perspective`. Scenariul soft warning a fost deschis până la `TEST BODY` → request → validation, unde rândul final este `Warning`; raportul targeted `api-atf-report_2026-10-13_13-16-01.html` a fost folosit suplimentar pentru deep-link-ul exact către fiecare failure și pentru verificarea teardown-ului separat. Logurile browserului au raportat zero erori JavaScript. Ordinea meniului rămâne `Tests → Category → Fails → Overview`.

## Ordinea dorită a tab-urilor

Ordinea vizuală dorită în bara laterală este:

    1. Tests
    2. Category
    3. Fails
    4. Overview

`Overview` trebuie să fie ultimul tab din navigare, nu `Fails`.

Această cerință este independentă de view-ul inițial:

- la deschiderea raportului fără hash se deschide `Overview`;
- după refresh se păstrează `Overview` ca view implicit, dacă nu există un deep-link explicit;
- în bara laterală, icon-ul `Overview` apare ultimul;
- `Fails` rămâne înaintea lui `Overview`, ca zonă de triage înaintea sumarului global;
- un hash explicit precum `#test-id=41`, `#category-view` sau `#exception-view` are prioritate față de view-ul implicit.

### Contract de navigare

    Tests → Category → Fails → Overview
                                      ↑
                         ultimul tab vizual / view implicit

Nu trebuie folosită ordinea de adăugare din DOM ca sursă accidentală de adevăr. Ordinea trebuie declarată explicit într-o constantă de navigare și verificată în testele de artefact.

### Criterii de acceptanță pentru ordine

| Verificare | Rezultat așteptat |
|---|---|
| numărul tab-urilor | 4 |
| poziția Tests | 1 |
| poziția Category | 2 |
| poziția Fails | 3 |
| poziția Overview | 4 |
| deschidere fără hash | Overview activ |
| refresh fără hash | Overview activ |
| `#test-id=41` | Tests activ |
| `#category-view` | Category activ |
| `#exception-view` | Fails activ |
| click pe Overview | Overview activ și ultimul în meniu |
| click pe Fails | Fails activ și înainte de Overview |

Ordinea trebuie verificată atât static în HTML/DOM, cât și după bootstrap-ul JavaScript. Un test care verifică doar existența icon-urilor nu este suficient; trebuie verificată poziția lor efectivă și view-ul activ.

## Audit live al ordinii și al bootstrap-ului — remediat și verificat

Defectul istoric era real: `Fails` era creat dinamic și putea ajunge după
`Overview`, deoarece ordinea statică era `Tests → Category → Overview`, iar
codul folosea `appendChild`. Remedierea este acum în codul live:

- `ensureFailureNavigation()` apelează `reconcileFailureNavigationOrder()` după
  ce construiește sau reutilizează nodurile;
- reconciler-ul mută `nav-exception` înainte de `nav-dashboard` folosind
  `menu.insertBefore(failureItem, overviewItem)`;
- operația este idempotentă: repetarea bootstrap-ului nu duplică tab-ul și nu
  schimbă ordinea deja corectă;
- `Fails` este eliminat împreună cu view-ul atunci când nu există failure-uri.

În artefactul nou, după bootstrap-ul JavaScript, DOM-ul a fost verificat în TAB
MCP și ordinea efectivă este:

    Tests → Category → Fails → Overview

Aceasta este o remediere structurală, nu una cosmetică prin CSS. `configureNavigationOrder()`
rămâne responsabil pentru conținutul raportului, iar reconciler-ul local este
responsabil strict pentru meniul lateral principal.

### Default view este corect, dar nu este legat de ordinea tab-urilor

`initialView()` alege `dashboard-view` când nu există hash, iar `start()` apelează `openInitialView()`. Asta acoperă corect cerința „Overview deschis la prima încărcare și la refresh fără deep-link”.

Totuși, această regulă este independentă de ordinea fizică a tab-urilor. Este posibil ca Overview să fie view-ul activ și să fie simultan înaintea lui Fails în DOM. Sunt două contracte care trebuie testate separat:

    default active view = dashboard-view
    visual tab order   = nav-test, nav-category, nav-exception, nav-dashboard

Un test care verifică doar `initialView() === 'dashboard-view'` nu demonstrează că tab-ul Overview este ultimul.

### Risc de bootstrap și timing

`ensureFailureNavigation()` elimină tab-ul și view-ul Fails dacă `failureScenarioRecords()` nu găsește failure-uri în momentul apelului. Descoperirea failure-urilor se face pe DOM proiectat, iar `configure()` este reluată prin timer-ele din client.

Acest model introduce o fereastră de timing:

    configure prea devreme
        ↓
    failure records încă neproiectate
        ↓
    Fails este eliminat
        ↓
    o reconfigurare ulterioară trebuie să-l recreeze

Nu este suficientă afirmația că „Fails există în cod”. Trebuie demonstrat că există în artefactul final după toate mutațiile, inclusiv când raportul este încărcat, când un failure este adăugat târziu și după refresh.

Mai există un risc de acoperire: `MutationObserver` este atașat la `.test-view`, în timp ce tab-ul și view-ul Fails sunt în afara acelui subtree. Modificările din zona de navigare nu sunt astfel observate de același mecanism care urmărește conținutul Tests.

### Ce rămâne de automatizat în profunzime

Testele din `Atf.Tests/Atf.Reporting.Extent.Tests/ReportingComponentTests.cs` verifică
contractele statice, inclusiv reconciler-ul:

- numele `Fails`;
- icon-ul `fa fa-bug`;
- `nav-exception`;
- `exception-view`;
- `Open in Tests`;
- deep-link și `failure-id`.

Încă merită adăugat un contract DOM automat pentru:

- ordinea finală a elementelor `li` din `.side-nav-menu` într-un HTML bootstrap-uit;
- exact un `nav-exception` după refresh;
- exact un view `.exception-view` după reconfigurări repetate;
- exact un toolbar/header `Fails` în view-ul de failures;
- absența lui Fails într-un raport fără failures și prezența lui într-un raport cu failures;
- păstrarea view-ului activ `dashboard-view` la refresh fără hash;
- click pe Fails urmat de click pe Overview, cu `active` și `d-none` corecte;
- regenerarea unui link `Open in Tests` după selectarea unei intrări diferite.

### Contract live propus pentru tab-uri

Contractul trebuie să fie tratat ca o listă semantică unică, indiferent dacă un tab este static sau dinamic:

    NAVIGATION_ORDER
      1. nav-test       → test-view
      2. nav-category   → category-view
      3. nav-exception  → exception-view
      4. nav-dashboard  → dashboard-view

Reguli obligatorii:

1. `nav-exception` se inserează la poziția semantică 3, nu prin `appendChild` la final.
2. Reconcilierea trebuie să fie idempotentă: repetarea ei nu duplică tab-uri, view-uri sau header-e.
3. Lipsa failure-urilor elimină împreună tab-ul, view-ul și handler-ul aferent.
4. Apariția târzie a failure-urilor reface același contract fără schimbarea ordinii celorlalte tab-uri.
5. Hash-ul are prioritate asupra view-ului implicit, dar nu schimbă ordinea meniului.
6. Overview rămâne ultimul în meniu chiar când este view-ul implicit activ.

### Matrice de validare obligatorie pentru acest defect

| Scenariu | Nav așteptat | View activ la start | Verificare suplimentară |
|---|---|---|---|
| raport fără failure | Tests, Category, Overview | Overview | fără `nav-exception`, fără `.exception-view` |
| raport cu un failure static | Tests, Category, Fails, Overview | Overview | un singur Fails și un singur view |
| failure descoperit după bootstrap | Tests, Category, Fails, Overview | Overview | reconciliere după mutație |
| refresh cu raport cu failure | Tests, Category, Fails, Overview | Overview | fără duplicate |
| `#test-id=41` | Tests, Category, Fails, Overview | Tests | testul și faza țintă sunt expandabile |
| `#exception-view` | Tests, Category, Fails, Overview | Fails | lista și details sunt sincronizate |
| click Fails → Overview | Tests, Category, Fails, Overview | Overview | Fails se ascunde, Overview se afișează |

### Verdict și scor revizuit după verificarea ultimului artifact

| Zonă | Scor | Feedback agresiv constructiv |
|---|---:|---|
| intenția UX documentată | 86 | Regula este clară și testabilă |
| default Overview | 82 | Implementat conceptual corect, dar separat de nav order |
| ordine runtime a tab-urilor | 92 | Reconciler explicit, verificat în DOM: Fails este înaintea Overview |
| determinism bootstrap | 78 | Reconcilierea este idempotentă; timer-ele și mutation timing rămân risc rezidual |
| separare responsabilități | 82 | side-nav ordering este în navigation controller; failure evidence și view projection sunt separate, dar projectorul complet rămâne de extras |
| testabilitate DOM | 68 | Smoke manual în TAB MCP și contracte statice; lipsește încă testul DOM automat |
| aliniere cu `origin/main` | 72 | Modelul vizual este apropiat, dar ordinea și reconcilierea nu sunt contractate |
| **contract navigation total** | **86** | **Remediat în runtime; următorul pas este automatizarea DOM smoke** |

Concluzia acestui audit este că problema de ordine a tab-urilor este rezolvată
în artifactul nou și verificată efectiv în DOM după bootstrap. Nu considerăm însă raportarea
completă până când ordinea, refresh-ul și scenariul fără failure nu sunt acoperite
și de un test DOM automat, nu doar de smoke manual și contracte de string.

## Contractul final agreat pentru raportare

Refactorizarea trebuie să îmbunătățească implementarea fără să schimbe formatul vizual deja agreat. „Mai OOP” nu înseamnă introducerea unei a doua interfețe de raportare; înseamnă că aceeași structură de execuție este proiectată consecvent în HTML, DOM și navigare.

### Structura vizuală care trebuie păstrată

    ┌────────────────────────────────────────────────────────────┐
    │ header global: produs, dată, status sumar                  │
    ├──────────────┬─────────────────────────────────────────────┤
    │ Tests        │ test/scenario selectat                      │
    │ Category     │                                             │
    │ Fails        │ faze: SETUP → TEST BODY → TEARDOWN          │
    │ Overview     │ request → evidence → validation → asserts  │
    └──────────────┴─────────────────────────────────────────────┘

Reguli de format:

- `Tests → Category → Fails → Overview` este ordinea fizică a tab-urilor;
- `Overview` este ultimul tab, dar este view-ul implicit la deschiderea fără hash și la refresh fără deep-link;
- `Tests` rămâne zona principală de execuție și trebuie să păstreze densitatea și comportamentul modelului Extent;
- `Category` este index de navigare, iar numele testelor sunt hyperlink-uri către testul exact din `Tests`;
- `Fails` este o proiecție de triage: un singur header `Fails`, listă compactă în stânga, details în dreapta;
- `Overview` este sumar global și nu trebuie să devină o copie a detaliilor din `Tests`;
- fazele sunt vizibile în ordinea `SETUP`, `TEST BODY`, `TEARDOWN` și sunt collapse-by-default;
- request-ul este containerul semantic al exchange-ului HTTP; response details, response JSON și validările apar în interiorul request-ului;
- validările multiple sunt siblings sub același request, nu siblings ai request-ului;
- toate assertions apar într-un tabel agregat per validation/request, cu coloanele `ASSERTION`, `EXPECTED`, `ACTUAL`, `RESULT`;
- body-urile JSON sunt code blocks scrollabile, cu înălțime naturală pentru payload-uri mici și limită configurabilă pentru payload-uri mari;
- technical trace este separat semantic de `RESPONSE JSON` și afișează doar metadata tehnică relevantă, nu duplicate ale întregului response;
- liniile verticale decorative nu trebuie să creeze un container fals în interiorul test body-ului;
- statusul unui request nu devine `Pass` înainte ca validările lui să fie evaluate.

### Model semantic agreat

    TestCase
    ├── Phase: SETUP
    │   └── Request(s)
    ├── Phase: TEST BODY
    │   └── Request
    │       ├── Technical trace
    │       ├── Response JSON
    │       └── Validation(s)
    │           └── Assertion table (all rows)
    └── Phase: TEARDOWN
        └── Request(s)

Statusul trebuie să curgă într-o singură direcție:

    raw execution evidence
        → typed phase/request/validation/assertion model
        → Extent hierarchy
        → HTML artifact
        → client-side navigation/projection

JavaScript-ul nu trebuie să reconstruiască adevărul execuției din „prima fază” sau „prima assertion” găsită în DOM. Dacă o informație este necesară în `Fails`, ea trebuie să fie emisă ca evidence stabil și identificabil încă din modelul de raportare.

### Refactorizare OOP recomandată, fără schimbarea formatului

Separarea responsabilităților trebuie să fie următoarea:

| Responsabilitate | Componentă conceptuală | Nu trebuie să facă |
|---|---|---|
| lifecycle | `ScenarioLifecycleOrchestrator` | HTML, CSS, DOM querying |
| verdict | `OutcomeResolver` | routing sau markup |
| phase/request evidence | `ExecutionEvidenceWriter` | grupare vizuală Fails |
| assertions | `ValidationTablePublisher` | calcularea statusului scenariului prin duplicate logic |
| failure model | `FailureRecordFactory` | căutarea `querySelector` în raportul final |
| Fails projection | `FailureIndexProjector` | mutarea cardurilor Tests |
| nav | `ReportNavigationReconciler` | discovery de failure-uri |
| artifact contract | `ReportArtifactValidator` | styling |
| client view | `ReportViewController` | inferența statusului din text colorat |

Rămâne o problemă de separare: `ExtentReportClientFailureFragments` combină încă
discovery, deduplicare, creare de DOM, routing și enrichment. Ordinea side-nav-ului
nu mai este însă o problemă funcțională: `reconcileFailureNavigationOrder()` o
repară idempotent după bootstrap. Direcția corectă rămâne extragerea unui
reconciler și a unei proiecții alimentate de records typed, nu extinderea
fragmentului client.

### Definition of done pentru formatul agreat

Formatul poate fi considerat stabil numai când același raport generat demonstrează simultan:

1. layout-ul Tests/Category/Fails/Overview în ordinea exactă;
2. Overview implicit, fără a schimba ordinea tab-urilor;
3. request → validation nested corect;
4. faze și noduri collapse-by-default;
5. toate assertions într-un singur tabel per validation;
6. request/response body în code block scrollabil, fără înălțime artificială pentru payload mic;
7. technical trace distinct și compact;
8. Fails cu un singur header și link exact către test/fază/request/validation;
9. failure, warning și skip păstrate distinct în tabel și în triage;
10. refresh și rebootstrap fără duplicate, fără orphan nodes și fără schimbare de layout.

Orice refactorizare care schimbă aceste reguli trebuie să fie tratată ca modificare de contract, nu ca simplă curățare internă.

## Verdict executiv

Raportul demonstrează că ATF are deja o bază bună pentru modelarea lifecycle-ului, dar încă nu este un contract complet de raportare.

**Scor global al evaluării anterioare: 84/100**

Pentru codul live verificat în auditul curent, contractul de ordine runtime este
**92/100**: este remediat și verificat în DOM. Nu îl notez cu 100 deoarece
lipsește încă testul DOM automat pentru refresh/rebootstrap și raportul fără
failure.

| Zonă | Scor | Verdict |
|---|---:|---|
| Separarea setup/body/teardown | 86 | Bună, cu scenarii reale și nesting vizibil |
| Precedența verdictelor | 91 | Matricea offline acoperă 12 combinații și precedence-ul Fail peste Warning |
| Assertions în tabele | 72 | Tabele utile, dar semantica CSS este ambiguă |
| Failure/Bugs triage | 88 | Tab-ul apare condiționat de failure, are icon `fa-bug`, listă și link determinist către Tests |
| Link către testul exact | 88 | `Open in Tests` a fost verificat pe artifactul nou cu phase/request/validation |
| Diagnostic tehnic | 79 | Stack trace-ul există, dar cauza este repetată pe mai multe niveluri |
| Testabilitatea raportării | 86 | Suitele offline și smoke-ul MCP acoperă fluxul; lipsește DOM smoke automat |
| OOP/SRP în reporting | 82 | Ownership-ul navigării și evidenței este mai clar; fragmentul de failure view încă poate fi redus |
| Determinism build/artefact | 86 | Artefact nou din staging, zero erori JS și navigare reconciliată după bootstrap |
| Documentare operațională | 80 | Contractele sunt mai clare, dar matricea trebuie formalizată |

### Concluzie agresivă, dar constructivă

ATF nu mai are o problemă de tipul „nu există lifecycle”. Problema actuală este mai importantă: lifecycle-ul este suficient de bogat încât raportarea trebuie să devină un model de execuție, nu doar o colecție de carduri Extent.

În forma observată înainte de ultimul fix:

- scenariile sunt raportate decent;
- fazele sunt în general nested corect;
- warnings și skips sunt vizibile;
- Bugs/Fails nu era încă o sursă completă de adevăr;
- tabelul de assertions nu are un contract distinct pentru Pass, Warning, Fail și Skip pe rând;
- aceeași eroare poate apărea la nivel de scenariu, fază și assertion fără relație explicită;
- artefactul generat putea arăta diferit în funcție de bootstrap-ul JavaScript.

## Dovezi din raportul analizat

Raportul țintă a fost inspectat ca HTML și prin tab-ul MCP. Pentru testul cu `test-id=41`, structura relevantă este:

    ApiLifecycleAndCombinationsTests
    └── Users API — warning → fail precedence
        ├── SETUP       — Warning
        ├── TEST BODY   — Fail
        │   ├── request
        │   ├── response details
        │   ├── response JSON
        │   └── validation/assertion table — expected 201, actual 200, result Fail
        └── TEARDOWN    — Pass

Raportul conține:

- categoria `lifecycle`: 14 teste — 6 Pass, 1 Fail, 4 Skip, 3 Others;
- categoria `api`: 22 teste — 13 Pass, 1 Fail, 4 Skip, 4 Others;
- `UsersApiTests` — Warning, `PostsApiTests` — Pass, `ApiLifecycleAndCombinationsTests` — Fail;
- failure concret: `201 Created` versus `200 OK`;
- aceeași cauză proiectată în evenimentul scenariului, `TEST BODY` și validation table.

Repetarea nu este automat greșită, dar trebuie să fie intenționată. Fără un `failureId`/ `evidenceId` comun, utilizatorul poate interpreta aceeași problemă ca trei defecte diferite.

## Matricea existentă de lifecycle

| Fază / combinație | Acoperire observată | Verdict |
|---|---|---|
| body only | `Users API — test only` | Pass |
| setup → body | `USERS API — setup → test` | Pass |
| setup → body → teardown | users și posts | Pass |
| body → teardown | `POSTS API — test → teardown` | Pass |
| setup skip → teardown pass | scenariu explicit | Skip |
| body skip → teardown pass | scenariu explicit | Skip |
| body pass → teardown skip | scenariu explicit | Skip |
| warning în setup → body pass | `warning → pass precedence` | Warning |
| body pass → warning în body | `pass → warning precedence` | Warning |
| body pass → warning în teardown | `pass → teardown warning precedence` | Warning |
| warning → body fail | `warning → fail precedence` | Fail |
| cleanup teardown | resource contract | Skip/Pass pe variante |

### Combinații acoperite prin matricea offline

| Combinație lipsă | Verdict așteptat | Prioritate |
|---|---|---|
| setup fail → teardown pass | Fail | acoperit unit |
| setup fail → teardown warning | Fail + warning diagnostic | acoperit unit |
| setup fail → teardown fail | Fail primar + callback secundar | acoperit unit |
| setup warning → body fail → teardown pass | Fail | acoperit unit |
| setup warning → body pass → teardown fail | Fail | acoperit unit |
| body fail → teardown warning | Fail | acoperit unit |
| body fail → teardown fail | Fail primar + callback secundar | acoperit unit |
| body skip → teardown warning | Skip + warning | acoperit unit |
| body skip → teardown fail | Fail | acoperit unit |
| setup skip → teardown skip | Skip cu două faze | acoperit unit |
| teardown-only fail | Fail | acoperit unit |
| mai multe warnings în faze diferite | Pass + warning count | acoperit unit |
| două requests/assertions failing în aceeași fază | records distincte în Fails | acoperit în resolver JS + contract test |

Matricea nu execută HTTP și nu schimbă testele de integrare; ea izolează politica de verdict. Testele de artifact verifică separat serializarea în HTML, iar TAB MCP verifică proiectarea în DOM.

### Contractul de precedență recomandat

    setup → test body → teardown/cleanup
       │          │              │
       └──────────┴──────────────┘
                    ↓
          Fail > Skip > Warning > Pass

Regula trebuie să fie exprimată într-un singur obiect de domeniu, nu duplicată în runner, reporter, Extent adapter și JavaScript.

## Assertion tables

### Ce funcționează

- assertions sunt vizibile într-un tabel agregat;
- coloanele `ASSERTION`, `EXPECTED`, `ACTUAL`, `RESULT` sunt utile;
- result-ul are simboluri accesibile `✅`, `⚠️`, `❌`;
- expected/actual pentru failure-ul principal sunt clare;
- validation node-ul este legat de request-ul corespunzător în testul analizat.

### Defect semantic observat — rezolvat în r10

În artefactul istoric, warning-ul avea clasa `assertion-fail`. În r10:

- `Pass`, `Warning`, `Skip` și `Fail` au clase CSS distincte;
- fiecare rând are și `data-atf-assertion-status` typed;
- icon-ul și clasa rândului sunt aliniate;
- testul component acoperă toate cele patru statusuri.

Contractul este închis pentru HTML static și component reporting; computed DOM
rămâne o limitare documentată.

### Matrice minimă obligatorie

| Tabel | Rânduri | Verdict tabel | Verificare |
|---|---|---|---|
| all pass | Pass + Pass | Pass | summary și toate rândurile |
| pass + warning | Pass + Warning | Warning | warning-ul nu devine Fail |
| pass + fail | Pass + Fail | Fail | failure dominant |
| warning + fail | Warning + Fail | Fail | ambele rânduri vizibile |
| all warning | Warning + Warning | Warning | count 2 |
| all skip | Skip + Skip | Skip/Other | fără Pass fals |
| empty table | 0 rânduri | Invalid/Other | mesaj explicit |
| evaluator exception | rows parțiale + exception | Fail | rows rămân evidence |
| duplicate assertion | același nume de două ori | conform contractului | fără suprascriere |
| multiple requests | tabel independent/request | conform contractului | fără contaminare |

Modelul trebuie să aibă un status typed pe `AssertionResult`, care determină rândul, tabelul, validation-ul, request-ul, faza, scenariul și proiecția în Overview/Bugs.

## Bugs / Failures — problema majoră

### Observația în raportul țintă

Raportul de referință de la 11:30 este un artefact generat înainte de ultimul fix: are `Tests Failed = 1`, dar nu are `nav-exception`. El nu poate valida layout-ul implementării curente.

În raportul regenerat și deschis în TAB MCP, `Fails` apare când există failure, iar failure-demo produce patru rânduri de triage. Verificarea DOM a confirmat `id="nav-exception"`, icon `fa fa-bug`, view `.exception-view` vizibil și zero erori JavaScript.

Problema de aliniere era reală și independentă de date: `.exception-view .test-list-tools` avea aproximativ 10.7 px înălțime, iar wrapper-ul listei era împins de un `ul.pull-left` flotant la aproximativ 272 px lățime. Tab-ul Tests are aproximativ 41.9 px header și 386.5 px listă. Fixul limitează `clear`, `min-height`, `float` și `width` strict la `.exception-view`, astfel încât Failure/Bugs să folosească aceeași geometrie ca Tests.

După fix, măsurătorile din TAB MCP sunt aliniate: header-ul Fails ocupă același rând de 42 px, lista începe sub header și folosește lățimea completă a pane-ului din stânga; detaliile rămân în pane-ul din dreapta.

### Cerința de UX pentru tab-ul Fails

Tab-ul trebuie să aibă **un singur header vizual**, de exemplu `Fails`. Header-ul trebuie să fie în toolbar-ul pane-ului din stânga și să existe o singură dată.

Nu este recomandat să apară simultan:

- `Fails` în navigarea laterală;
- încă un header `Fails` în listă;
- încă un titlu de failure în fiecare container;
- încă un header repetat în zona de detalii.

Contractul vizual recomandat este:

    Fails                                      <- singurul header al tab-ului
    ┌──────────────────────┬─────────────────────────────────────┐
    │ grouped failure rows │ selected failure details             │
    │ type / count / status│ phase, request, validation, cause    │
    └──────────────────────┴─────────────────────────────────────┘

### Lista din stânga trebuie să urmeze `origin/main`

Pentru consistență, lista din stânga trebuie să fie cât mai apropiată de proiecția Extent din `origin/main`, nu un al doilea mini-raport:

| Element | Comportament dorit |
|---|---|
| toolbar | un singur titlu `Fails` |
| listă | scroll vertical, aceeași densitate ca Tests/Exceptions din origin |
| rând | identitate principală + count + status compact |
| grupare | aceeași cauză/exception type agregată, fără duplicate vizuale |
| selecție | un rând activ, details în pane-ul din dreapta |
| detalii | phase/request/validation/assertion/cause și link către Tests |
| status | Fail dominant; Warning și Skip separate, nu amestecate în `Others` |
| text | titlu pe o linie când este posibil, meta pe linia secundară |

Recomandarea este să păstrăm în stânga modelul nativ de listă — similar cu `Exception` din Extent — și să mutăm schema completă în dreapta. Stânga trebuie să răspundă rapid la întrebarea „ce a eșuat și de câte ori?”, nu să repete întregul diagnostic.

Forma recomandată pentru un rând:

    System.InvalidOperationException
    4 failing records                         [Fail]

La selectare, dreapta poate afișa:

    Users API — warning → fail precedence
    PHASE       TEST BODY
    REQUEST     GET /public/v2/users
    VALIDATION  Validate: status=201
    ASSERTION   HTTP status → 201 Created → 200 OK
    CAUSE       ApiValidationException
    [Open in Tests]

Dacă alegerea de produs este listă per test, păstrați tot stilul origin în stânga și folosiți doar:

    Users API — warning → fail precedence
    ApiLifecycleAndCombinationsTests · TEST BODY · 1 failure   [Fail]

În ambele variante, **nu trebuie afișat un bloc schematic mare în fiecare rând din stânga**. Asta rupe densitatea Extent și creează impresia că tab-ul are mai multe nivele de navigare decât are în realitate.

### Gap față de origin/main și remedierea curentă

`origin/main` oferă o convenție vizuală clară: toolbar, listă compactă în stânga, detalii pentru elementul selectat în dreapta. Implementarea anterioară introducea o proiecție custom care amesteca trei responsabilități în lista din stânga:

1. identificarea failure-ului;
2. prezentarea schematică a evidence-ului;
3. acțiunea de navigare către Tests.

Riscurile observate au fost tratate astfel:

- header-ul este acum unic și se numește `Fails` în toolbar-ul view-ului;
- geometria listei este izolată și aliniată cu Tests prin `clear: both`, `min-height: 42px` și `width: 100%`;
- detaliile rămân în pane-ul din dreapta, iar stânga păstrează rânduri compacte;
- fiecare failure record are link `Open in Tests` și evidence phase/request/validation/assertion/cause;
- fiecare record de scenariu primește acum `failureId` determinist și îl propagă
  către rândul Fails, acțiune și deep-link.

**Feedback:** la nivel vizual și informațional, tab-ul Fails este acum o proiecție coerentă a modelului Extent, nu o pagină paralelă. Corelarea scenariu → Fails → Tests este protejată prin `failureId` determinist. Factory-ul și projectorul sunt acum delimitate explicit în fragmentul JS și au contract unit-test; rămâne P1 înlocuirea obiectelor structurale JS cu `FailureRecord`/`EvidenceId` typed în modelul de raportare.

### Probleme de responsabilitate în implementarea actuală

1. `failureScenarioRecords()` filtrează în continuare scenariile cu verdict final
   Fail — aceasta este intenționat limita tab-ului Fails; Warning și Skip rămân
   în Overview/Tests, nu sunt promovate artificial în Bugs.
   - În interiorul scenariului, `failureEvidenceRecords()` creează acum câte un
     record pentru fiecare assertion row `Fail`.
   - Un scenariu fără tabel de assertions păstrează un fallback de scenariu,
     deci setup/teardown exceptions nu dispar din triage.
   - Fiecare record primește un ordinal stabil în `failureId`, fără să piardă
     request-urile failing multiple din aceeași fază.

2. `failureEvidence()` nu mai alege orbește primul request când are un rând
   de assertion ca ancoră.
   - Resolver-ul urcă la validation, fază și request-ul frate precedent.
   - Fallback-ul „prima fază / primul row” există doar pentru HTML legacy fără
     structura semantică nouă.

3. `createFailureViewItem()` produce propriul list item, în timp ce `configureFailureView()` caută rânduri native `.tag-test-status`.
   - Există încă două contracte DOM pentru același tab.
   - Factory-ul de record și projectorul de index sunt separate, dar construcția DOM și enrichment-ul trebuie unificate ulterior într-un singur boundary.

4. failure indexul și Tests tab-ul folosesc acum `failureId` pentru corelarea
   scenariului și a cardului; acesta nu este încă un `EvidenceId` typed pe fază,
   request și assertion.

5. aceeași excepție poate fi încă prezentată la nivel de scenariu, body și
   validation; deduplicarea/cardinalitatea multi-phase rămâne P1.

### Contractul recomandat pentru Bugs / Failures

Fiecare rând trebuie să fie un `FailureRecord`, nu doar un test:

    FailureRecord
    ├── failureId
    ├── scenarioId / testId
    ├── testClass / scenarioName
    ├── phase: Setup | TestBody | Teardown
    ├── requestId / request summary
    ├── validationId / validation title
    ├── assertion summary
    ├── expected / actual
    ├── status: Fail | Warning | Skip
    ├── primary / secondary
    ├── exception type + message
    └── deep link: #test-id=...&phase=...&request=...&validation=...

Tab-ul trebuie să:

- includă întotdeauna Fail;
- separe explicit Warning și Skip;
- păstreze fiecare failure separat;
- aibă un singur header `Fails`;
- păstreze lista din stânga compactă, scrollabilă și apropiată de `origin/main`;
- grupeze failures fără să piardă records individuale în detaliile din dreapta;
- aibă link către testul exact;
- poată deschide faza și request-ul țintă;
- refolosească același `failureId` pentru aceeași cauză proiectată la mai multe nivele;
- nu depindă de căutarea „primei” faze sau „primei” assertion.

## Comparația cu origin/main

`origin/main` și branch-ul `main` indică același commit de bază: `c52f0f93307cfd41c452e0b3868a0c41f3ab78d3`. Diferențele analizate sunt în worktree-ul local și sunt în mare parte necomise.

Față de baseline:

- lifecycle runner-ul a fost extras în componente dedicate;
- testele de lifecycle au fost extinse cu skip/warning/failure precedence;
- reporting theme-ul a fost împărțit în fragmente pentru overview, failure navigation, disclosure și navigation order;
- au fost adăugate teste pentru lifecycle și reporting;
- raportarea încearcă să adauge failure navigation dinamic.

Riscul rămas este sincronizarea între:

    origin/main → worktree refactor → generated HTML → DOM bootstrap → UX

Nu există încă un test unic care demonstrează că același contract este păstrat la toate aceste niveluri.

## Teste adăugate și feedback rămas

### P0 — lifecycle domain tests — acoperit

Matricea `[Theory]` din `ApiScenarioLifecycleRunnerTests` acoperă:

- setup fail / body not run / teardown pass;
- setup fail / teardown fail;
- setup warning / body fail / teardown pass;
- body fail / teardown warning;
- body fail / teardown fail;
- body skip / teardown warning;
- body skip / teardown fail;
- setup skip / teardown skip;
- teardown-only fail;
- multiple warnings across phases;
- multiple requests failing în aceeași fază.

Verifică outcome-ul, statusul fiecărei faze executate, precedența failure-ului
primar, callback-ul pentru failure-ul de teardown, skip count și warning count.

### P0 — assertion table contract tests — acoperit

Construiți tabele fără HTTP real pentru:

- Pass + Pass;
- Pass + Warning;
- Pass + Fail;
- Warning + Fail;
- toate warnings;
- toate skips;
- zero rows;
- exception după rows parțiale;
- duplicate assertion names;
- două tabele pe requests diferite.

Verificarea existentă acoperă clasele, icon-ul, aria-label-ul, verdictul
agregat, expected/actual, row count-ul, duplicate names și explicitul
`ResultStatus=Fail` pentru tabela goală.

### P0 — final artifact contract tests — acoperit static

Pentru un raport minimal generat în test:

- un request node pentru fiecare exchange;
- validation node nested sub request;
- request-ul nu primește Pass înainte de validation;
- toate assertions în validation table;
- failureId stabil;
- failure index cu phase/request/validation/assertion;
- hyperlink către testul corect;
- refresh idempotent;
- overview/test totals consistente;
- fără orphan validation nodes.

Contractul HTML static este acoperit de `FinalArtifactContractTests`, iar failure
correlation este verificată prin `ReportingComponentTests`. Testul de artifact
publică acum simultan două validation nodes failing și verifică faptul că ambele
request-uri și ambele validation nodes rămân distincte în HTML, împreună cu
infrastructura `failureEvidenceRecords` și `data-atf-evidence-id`. În plus,
`ApiLifecycleAndCombinationsTests` generează acum o perspectivă separată
`pass → teardown fail`, astfel încât failure indexul să distingă clar un failure
de contract în body de un failure de cleanup în teardown.

### P1 — browser smoke / DOM behavior — verificat manual în TAB MCP

1. deschide raportul fără hash;
2. confirmă Overview by default;
3. click Tests;
4. selectează `ApiLifecycleAndCombinationsTests`;
5. expandă scenariul failing;
6. expandă request-ul;
7. expandă validation table;
8. verifică toate rândurile;
    9. click Fails;
10. click `Open in Tests`;
11. verifică faza și request-ul;
12. refresh și repetă pentru duplicate.

Pașii au fost executați pe artifactul staging curent cu TAB MCP: Overview,
Fails, `Open in Tests`, test/scenario, request, validation, assertion table și
deep-link. Nu există erori JavaScript; alinierea măsurată este 42 px header și
386.5 px listă atât pentru Tests, cât și pentru Fails.

### P1 — failure index tests — acoperit pentru cardinalitate statică

Runner-ul testează separat failure în setup, body, teardown, warning + failure
și skip + teardown failure. `PassingBodyThenTeardownFailureIsVisible` adaugă
explicit combinația body/validation pass → teardown fail. Resolver-ul JS testează
contractul pentru două assertions failing prin string contract, iar
`FinalArtifactContractTests` verifică un scenariu cu două validation nodes failing
și cardinalitatea minimă a request/validation nodes. Rămâne doar un smoke DOM
automat care să ruleze bootstrap-ul în browser headless și să numere record-urile
Fails după refresh.

## Refactorizare OOP recomandată

| Componentă | Responsabilitate unică |
|---|---|
| `ApiScenarioLifecycleRunner` | orchestration setup/body/teardown |
| `ApiScenarioOutcomeResolver` | precedența verdictului |
| `ApiScenarioPhaseRunner` | execuția și închiderea unei faze |
| `ValidationTableRunner` | colectarea și evaluarea assertions |
| `FailureRecordFactory` | execution evidence → failure records |
| `FailureIndexProjector` | failure records → Bugs view |
| `ReportArtifactContractValidator` | verificare HTML static |
| `ReportDomContractSmoke` | verificare DOM după bootstrap |
| `ReportNavigationLinkBuilder` | link test/phase/request |
| `ReportStatusMapper` | mapare typed → UI/CSS |

Nu este recomandat ca `ExtentReportClientFailureFragments` să dețină simultan discovery, deduplicare, evidence extraction, DOM construction și routing.

## Ordinea corectă pentru următorul increment

1. formalizați `FailureRecord` și `EvidenceId` typed pe fază/request/validation;
2. unificați DOM construction și enrichment sub projectorul de index; ownership-ul
   side-nav-ului este deja mutat în `ExtentReportClientNavigationController`;
3. adăugați smoke DOM automat pentru două validation nodes failing și un teardown
   failure după bootstrap și refresh;
4. faceți reconcilierea failure index idempotentă la refresh/mutation în acel smoke test;
5. abia apoi optimizați deduplicarea vizuală și gruparea după exception type.

## Criteriul de „gata”

Task-ul nu trebuie considerat închis până când un raport nou demonstrează în același artefact:

- setup/body/teardown cu Pass, Warning, Fail și Skip;
- precedence corectă pentru toate combinațiile;
- assertion table cu Pass, Warning, Fail și Skip;
- tabel mixt Pass + Warning + Fail;
- Bugs/Failures prezent când există failures; absent când raportul nu are failure;
- tab-ul Fails are un singur header, este aliniat geometric cu Tests și folosește aceeași densitate/scroll în lista din stânga;
- un singur header `Fails`, fără header duplicat în listă sau details;
- lista din stânga compactă și scrollabilă, cu densitate apropiată de `origin/main`;
- un singur rând activ în stânga și details proiectate în dreapta;
- fiecare failure cu phase/request/validation/assertion;
- hyperlink către testul exact;
- expand/collapse după navigare;
- refresh idempotent;
- Overview și Tests cu totaluri consistente;
- fără duplicate failure records;
- fără orphan validation nodes;
- fără failure ascuns doar de DOM projection.

## Concluzie

ATF este într-o zonă bună pentru continuarea refactorizării, iar defectul vizual al tab-ului Fails este rezolvat și verificat în artefactul nou. Raportarea trebuie în continuare tratată ca un produs cu un model de domeniu propriu. Cel mai mare risc rămas este diferența dintre **execution truth**, **HTML truth** și **DOM truth** pentru agregări cu mai multe failure records.

Până la închiderea matricei de mai sus, orice verdict „raportarea este completă” ar fi prematur.
