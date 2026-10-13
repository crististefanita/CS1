# Atf.Api — boundary-ul scenariilor live

`Atf.Api` este boundary-ul de acceptanță live, nu kernelul reutilizabil al
framework-ului. Este singurul proiect care trebuie să apeleze serviciul extern
configurat. Deține integrarea xUnit, intenția scenariilor, acoperirea business și modulul API din
`Atf.Api/Module`. Fostul proiect `Atf.Common` nu mai există ca boundary separat
la rădăcină.

Numele este păstrat intenționat pentru compatibilitate cu path-urile curente din
rapoarte și teste. Conceptual, conține atât modulul API, cât și scenariile sale
de acceptanță. O redenumire sau un merge de namespace trebuie tratate ca migrare
mecanică, nu combinate cu un refactor de comportament.

## Ownership curent și țintă

```text
CURRENT                         TARGET
Atf.Api/Module/Routing          Atf.Api/Module/Routing
Atf.Api/Module/Services         Atf.Api/Module/Services
Atf.Api/Module/Validation       Atf.Api/Module/Validation
Atf.Api/Tests                   Atf.Api/Tests
Atf.Tests/Atf.Api.Tests         Atf.Tests/Atf.Api.Tests
```

`Atf.Core` nu este absorbit niciodată în `Atf.Api`. Kernelul rămâne neutru față
de produs; numai vocabularul modulului API se poate apropia de scenarii.

## Scenario lifecycle

```mermaid
flowchart TD
    X[xUnit test class] --> B[ApiTestBase.RunScenarioAsync]
    B --> S[SETUP]
    S --> T[TEST BODY]
    T --> D[TEARDOWN]
    B --> Context[ApiScenarioContext]
    Context --> Users[UsersService]
    Context --> Posts[PostsService]
    Context --> Cleanup[CleanupRegistry]
    S --> Report[IAtfScenarioScope]
    T --> Report
    D --> Report
```

`ApiTestBase` pornește un scenariu sub clasa xUnit reală, creează un singur
modul/context, execută fazele opționale, rulează cleanup-ul, păstrează failure-ul
primar și îl aruncă din nou astfel încât xUnit să rămână autoritar.

Boundary de responsabilitate:

```text
Atf.Api.TestHost       fixture xUnit + bridge curent de compoziție Extent
Atf.Api                xUnit + intenția scenariului live + modul API
Atf.Core               porturi de execuție și raportare
Atf.Reporting.Extent   adaptor HTML concret
Atf.Tests              teste offline de contract pentru API, Core, modul și raportare
```

## Auditul responsabilităților — boundary live

`Atf.Api` este intenționat subțire, dar `ApiTestBase` rămâne boundary-ul cu cel
mai mare risc din host. Traduce dependențele xUnit, consumă
`IAtfApiReportSession`, creează `ApiScenarioContext`, invocă politica de lifecycle,
mapează rezultatul în scope-ul neutru al scenariului și retrimite excepția
autoritară. Extent este selectat numai de `Atf.Api.TestHost`.

| Responsabilitate | Owner curent | Owner țintă |
|---|---|---|
| xUnit constructor/output/skip translation | `ApiTestBase` | remains here; this is the host adapter |
| module/context creation | `ApiModuleFactory.FromSettings` | explicit settings/ports factory |
| precedența setup/body/teardown | `ApiScenarioLifecycleRunner` | runner injectabil cu model explicit de outcome |
| finalizarea Extent și maparea statusului | `ApiTestBase` | port de finalizare a scenariului furnizat de fixture |
| intenția de business | clasele concrete de scenariu | rămâne în `Atf.Api`; nu o muta în runner |

Regula chirurgicală este: nu muta decizii business în infrastructură și nu lăsa
excepțiile xUnit să definească modelul reutilizabil de lifecycle. Următorul
refactor trebuie să introducă un rezultat explicit `Passed | Failed | Skipped`
și să păstreze `ApiTestBase` ca singur loc care traduce rezultatul în comportament
xUnit.

## Actualizarea auditului — 2026-10-13

`Atf.Api` trebuie să rămână subțire în raport cu framework-ul: exprimă intenția
scenariului și business coverage, nu implementează transport, Extent sau politica
generică de validare. Abaterile sunt findings live cu test și criteriu de
acceptare, nu evaluări statice în documentație.

### Separarea responsabilităților

```text
ApiTestBase
  -> adaptor xUnit + traducerea rezultatului final
ApiScenarioLifecycleRunner
  -> orchestrarea setup/body/teardown
ApiModule
  -> routes/services/semantic operations
Atf.Core
  -> transport/auth/retry/configuration
Atf.Reporting.Extent
  -> arbore concret și publicare HTML
```

`ApiScenarioLifecycleRunner` are deja separate `ApiScenarioPhaseRunner`,
`ApiScenarioTeardownRunner` și `ApiScenarioOutcomeResolver`. Nu se mai
extrage un alt coordinator doar pentru a obține clase mai mici. Presiunea
rămasă în `ApiTestBase` este responsabilitatea corectă a adapterului xUnit:
compoziția contextului, traducerea outcome-ului neutru și finalizarea scope-ului.

### Reguli pentru scenarii

- `ApiLifecycleAndCombinationsTests` poate păstra combinațiile de setup/body/
  teardown, dar nu trebuie să reimplementeze lifecycle-ul;
- testele de tip `test only` trebuie să afișeze numai `TEST BODY`, fără setup sau
  teardown sintetic;
- setup, body și teardown sunt faze surori sub același scenario;
- un request și validarea lui sunt frați direcți sub aceeași fază;
- failure-ul primar rămâne autoritar chiar când cleanup-ul sau reporterul eșuează;
- contractele reutilizabile trebuie testate offline, iar `Atf.Api` trebuie să
  conțină numai dovadă live de business.

`ApiScenarioLifecycleRunner` este acum un coordinator intenționat subțire.
Execuția fazelor, siguranța teardown-ului și rezolvarea rezultatului sunt seam-uri
interne separate; coordinatorul le compune și păstrează contractul de precedență:

```text
ApiScenarioLifecycleRunner
    -> ScenarioPhaseExecutor
    -> TeardownCoordinator
    -> ApiScenarioOutcomeResolver
```

Fiecare seam are acoperire de contract pentru precedența failure-urilor și
garanțiile de cleanup. Nu reintroduce politica în coordinator doar pentru a
reduce zgomotul la call-site. Un finalizer nou de scenariu este justificat doar
de un al doilea host sau de o regulă distinctă și testată; altfel este doar
indirecție fără câștig de ownership.

**P0 lifecycle închis pentru teardown custom:** runner-ul apelează acum teardown-ul
custom și cleanup-ul printr-un `finally` interior, păstrând failure-ul primar din
body/setup. `ApiScenarioLifecycleRunnerTests` conține cazul de regresie
`teardown fails + cleanup still executes`.

**P1 lifecycle semantic contract is protected:** `CleanupRegistry` reports
best-effort before executing a resource action, iar un reporter failure nu mai
poate preveni acțiunea, următorul cleanup item sau deschiderea TEARDOWN.
Rămân deschise doar hardening-ul concurenței și dovada DOM.

**Boundary-ul testelor este rezolvat:** `ApiScenarioLifecycleRunnerTests` se află
acum în `Atf.Tests/Atf.Api.Tests`, iar `Atf.Api` conține numai scenarii live de
acceptanță și fixture-ul de failure opt-in. Testele de lifecycle ale framework-ului
rulează astfel offline și nu adaugă carduri sintetice în artifactul Extent live.

### Audit OOP: `ApiTestBase` și scenariile de combinații

`ApiTestBase` este acum un adapter xUnit subțire: construiește contextul și
delegă phase runner, skip parity, cleanup, exception precedence și finalizarea
Extent către `ApiScenarioLifecycleRunner`. Runnerul este testabil separat și
păstrează aceeași regulă de ownership pentru SETUP / TEST BODY / TEARDOWN.

`ApiLifecycleAndCombinationsTests` este util ca test de contract, dar ramifică
în interior după `resource` și construiește contracte diferite în aceeași
metodă. Asta face raportul dinamic, însă nu îl face ușor de citit. La o rundă
ulterioară, după înghețarea testelor actuale, extrageți o politică tipizată de
resursă sau separați scenariile users/posts; nu copiați ramificațiile în încă
un test. Regula de siguranță este:

```mermaid
flowchart LR
    ScenarioIntent[scenario intent] --> Runner[ScenarioLifecycleRunner]
    Runner --> PhasePolicy[setup/body/teardown policy]
    Runner --> CleanupPolicy[cleanup + failure precedence]
    Runner --> ReportPolicy[scenario finalization]
    ResourcePolicy[typed resource operation] --> Runner
```

Ramificațiile de resource rămân intenționat în scenarii pentru a demonstra
comportamentul observabil, dar lifecycle-ul propriu-zis nu mai este duplicat în
clasa de bază. Orice ramificație nouă trebuie să rămână la nivel de intenție de
scenariu, nu să reimplementeze cleanup sau politica de failure.

Ierarhia raportului este:

```text
UsersApiTests
  scenario
    SETUP
      API request
        technical trace + JSON evidence
        Validate: contract
    TEST BODY
      API request
        technical trace + JSON evidence
        Validate: contract
    TEARDOWN
      cleanup / verification
```

Fazele sunt frați direcți sub scenariu. Fazele goale nu sunt fabricate.
Verdictul scenariului este vizibil în header-ul scenariului. Pentru scenariile
cu lifecycle compozit, tabelul generic `Scenario passed` este eliminat deoarece
duplică verdictul. Deschiderea unui scenariu afișează shell-uri compacte pentru
faze; fiecare fază și pașii copil rămân colapsați până la deschidere. Executorul
ține nodul HTTP deschis cât timp publică evidence-ul response-ului; validatorul
agregat este randat apoi ca următorul sibling direct al request-ului, sub aceeași
fază. Assertions multiple rămân astfel rânduri într-un singur tabel, iar un
response HTTP ulterior primește propria pereche request/validation sibling.

### Soft assertion contract

ApiAssertions.SoftContract is intentionally explicit and should be rare. It is
for a known, non-blocking observation where the failed assertion must remain
visible without making xUnit fail:

    ApiAssertions.SoftContract(
        context.Reporter,
        response,
        "Validate: compatibility warning",
        contract => contract.Status(HttpStatusCode.Created));

The report still contains the failed row in the aggregate assertion table. The
validation node and scenario receive Warning; the Overview maps that scenario
to Others, and the affected tag increments OTHERS. This is deliberately
different from ApiAssertions.Contract, where a failed row is authoritative and
throws ApiValidationException. Configuration/evaluator exceptions are never
softened.

The Overview contract is intentionally multi-surface: the direct scenario card,
the native `Tests` counters/chart, the `Tags` OTHERS column and the secondary
execution metadata must all derive the same `Warning` population. A soft
assertion must never increment `Tests Failed`, and a reporter exception must not
turn it into an xUnit failure. Component tests now protect mixed
pass/warning/fail/skip propagation; rămâne deschis doar contractul DOM pentru
reconcilierea computed styles și interacțiunea reală din browser.

## Scenario rules

A scenario should state intent:

```csharp
await RunScenarioAsync(
    "GET users — collection contract",
    ["api", "users", "smoke", "contract"],
    async context =>
    {
        var response = await context.Users.ListAsync();
        ApiAssertions.Contract(context.Reporter, response, "Validate: user collection contract", contract => contract
            .Status(HttpStatusCode.OK)
            .ContentTypeContains("application/json")
            .JsonSchema(schemaPath));
    });
```

El nu trebuie să construiască request-ul, să formeze URL-ul, să adauge header-ul
Authorization sau să creeze un nod de raport.

Testele de lifecycle folosesc:

```text
setup    -> create prerequisite and register ownership
test     -> exercise behavior under test
teardown -> explicit verification + CleanupRegistry.RunAsync
```

Suita de combinații lifecycle păstrează explicită matricea observabilă. Ea
acoperă toate formele legale de faze și cazurile de precedență ușor de regresat
în raport:

| Case | Expected scenario verdict | Expected phase evidence |
|---|---|---|
| test only | Pass | `TEST BODY` only; no synthetic setup/teardown |
| setup → test | Pass | setup and body are siblings |
| test → teardown | Pass | body and teardown are siblings |
| setup → test → teardown | Pass | all three phases are siblings |
| warning → pass | Warning | warning remains visible; no failed test counter |
| pass → warning | Warning | a later warning promotes the scenario |
| warning → fail → pass | Fail | failure is authoritative; teardown still passes |
| setup skip → teardown pass | Skip | setup is Skip; teardown is Pass |
| body skip → teardown pass | Skip | body is Skip; teardown is Pass |
| body pass → teardown warning | Warning | body is Pass; teardown is Warning |
| body pass → teardown skip | Skip | body is Pass; teardown is Skip |

Matricea este exprimată intenționat prin schimburi API reale pentru ramurile de
reușită și teardown. Ramurile skip/failure folosesc semnale deterministe de
lifecycle, astfel încât contractul raportului să poată fi testat fără să depindă
de un secret lipsă sau de o eroare remote. Fiecare schimb HTTP reușit se încheie
în continuare cu un singur nod de validare agregat deținut de request.

## Autentificare și mediu

Rutele read-only pot folosi profilul anonim. Rutele de scriere necesită profilul
bearer configurat:

```powershell
$env:ATF_API_TOKEN = '<secret supplied separately>'
dotnet test .\Atf.Api\Atf.Api.csproj --no-restore
```

Scenariile de scriere sunt omise când scrierile sunt dezactivate sau variabila
de token configurată lipsește. Raportul afișează motivul; skip nu este pass.

Autentificarea rutei este declarată central de `ApiRoutes` prin
`ApiRouteAuthentication`, not inferred from the HTTP verb or the profile name:

```text
ApiRoutes.UserCreate
  method       = POST
  path         = /public/v2/users
  auth.type    = Bearer
  auth.access  = Required
  auth.profile = default
```

Enum-urile descriu intenția rutei (`ApiAuthType` și `ApiAuthRequirement`), iar
profilul rămâne o cheie de configurare a modulului. Extent randează separat
contractul rutei (`route.auth.*`) față de profilul runtime efectiv
(`auth.profile`, `auth.mode`, `auth.token.state`), fără să randeze secrete.

**Paritatea verdictului este impusă:** proiectul de teste API folosește contractul
implicit xUnit v3 pentru skip dinamic (`SkipException.ForSkip`) după marcarea
scenariului Extent ca `Skip`. Un token de scriere lipsă este astfel `Skipped` în
runner, în rădăcina clasei și în nodul scenariului; nu este convertit niciodată
în return/Pass normal. Aceasta este o regulă de contract a testului, nu o
conveniență de raportare.

## Inventarul testelor

- `UsersApiTests`: contracte pentru colecție/item user și autentificare negativă.
- `PostsApiTests`: contracte pentru colecție/item post, relație și not-found.
- `ApiLifecycleAndCombinationsTests`: compoziția setup/body/teardown, ownership cleanup și combinații de faze.
- `ApiExecutorReportingTests`: închiderea request-step-ului și contractul de diagnostic.
- `ApiRoutesTests`: contractul catalogului de rute.
- `ApiScenarioPhaseLayoutTests`: ierarhia fazelor, ordinea request/validation sibling și contractul tabelului agregat.
- `AssertionEvidenceFormatterTests`: contractul HTML pentru assertion/evidence.

Lifecycle-ul reutilizabil al validator-step-ului este testat în
`Atf.Tests/Atf.Common.Tests/ValidationAndLifecycleTests`; `Atf.Api` verifică doar
arborele orientat spre API și boundary-ul executorului. Astfel politica de raportare
rămâne testată offline, iar scenariile live duplicate nu devin teste de framework.

## Categorii

```powershell
dotnet test .\Atf.Api\Atf.Api.csproj --filter "Category=smoke"
dotnet test .\Atf.Api\Atf.Api.csproj --filter "Category=lifecycle"
dotnet test .\Atf.Api\Atf.Api.csproj --filter "Category=combination"
```

Fiecare scenariu live nou trebuie să aibă un nume semantic stabil, categorii
concentrate, o rută read-only când este posibil, ownership explicit pentru
cleanup și cel puțin un test de componentă determinist.

Nu adăuga tag-ul sau adnotarea generică `framework`. Proiectul și namespace-ul
identifică deja aceste scenarii ATF; un tag este permis numai dacă schimbă
filtrarea, politica de execuție sau navigarea raportului și are un test observabil.

## Fixture pentru randarea failure-urilor

`FailApiTests` este un fixture opt-in care eșuează intenționat pentru validarea
contractului de raportare. Acoperă patru owner-i diferiți ai failure-ului:

- setup exception;
- failure de assertion de contract (schimbul HTTP este prezent, iar tabelul de
  assertions conține un rând eșuat);
- failure direct de assertion xUnit în test body;
- excepție de teardown după finalizarea body-ului.

Fixture-ul este omis implicit și, prin urmare, nu poate înroși o rulare API
normală. Rulează-l deliberat când verifici prezentarea failure-urilor:

```powershell
$env:ATF_ENABLE_FAILURE_DEMOS = "true"
dotnet test .\Atf.Api\Atf.Api.csproj --filter "FullyQualifiedName~FailApiTests"
Remove-Item Env:ATF_ENABLE_FAILURE_DEMOS
```

Raportul așteptat conține patru scenarii eșuate, status `Fail` în Overview,
badge-uri roșii pentru failure și un rezultat `❌` în tabelul de assertions.
Cele patru cazuri dovedesc și că SETUP, TEST BODY și TEARDOWN rămân vizibile
individual și colapsate implicit. Păstrează acest fixture în afara quality
gate-ului local obișnuit.

## Acceptanța raportării

Înainte de acceptarea unei schimbări de raportare:

1. rulează `Atf.Core.Tests`;
2. rulează `Atf.Tests/Atf.Common.Tests`;
3. rulează un scenariu live reprezentativ;
4. inspectează HTML-ul generat.

Raportul trebuie să afișeze clasa xUnit în navigarea din stânga, scenariul sub ea,
verdictul scenariului în header, fazele lifecycle ca siblings, shell-uri compacte
și colapsate pentru faze, validarea sibling a fazei și fără
fără rânduri `Execution summary` pe containerele lifecycle sau pe ultimul request,
un singur technical trace pe schimb, JSON dark împachetat și scrollabil, un singur
tabel de assertions, fără folder Extent extern și fără valoarea tokenului.

## Cum se citește un test API în raport

```mermaid
flowchart TD
    Test[header scenariu\nverdict + durată + tag-uri]
    Test --> Phase[SETUP / TEST BODY / TEARDOWN]
    Phase --> Exchange[HTTP step\nmethod + path + auth + duration]
    Exchange --> Details[detalii request/response colapsate]
    Exchange --> Validation[phase-sibling validation]
    Validation --> Table["ASSERTION / EXPECTED / ACTUAL / RESULT"]
```

Interpretarea trebuie făcută în această ordine: întâi verdictul testului, apoi
operația HTTP, apoi trace-ul tehnic, apoi body-urile JSON, iar la final tabelul de
assertions. `Info` înseamnă evidence; `Pass`/`Fail` din tabelul agregat este verdict
funcțional. Nodurile sunt colapsate implicit pentru ca raportul să rămână scanabil.

## Matricea de acoperire live

| Familie | Dovadă curentă | Următoarea dovadă |
|---|---|---|
| users | item, collection, auth lipsă, lifecycle | persistența câmpului replace/patch, schema pentru eroare de input invalid |
| posts | item, collection, item necunoscut, relație user | create/delete nested, colecție goală, parent-not-found |
| lifecycle | setup/body/teardown și combinații | cleanup la cancellation/timeout și failure primar versus teardown |
| reziliență | teste offline ale politicii retry | simulare sigură de `429` printr-un profil de transport fake |
| raport | HTML Extent real generat după rulare live | assertions automate DOM pentru Overview/overflow |

Baseline-ul Extent curent trebuie citit din cardurile directe ale scenariilor din
raportul generat, nu copiat în acest README. Assembly-ul xUnit conține și teste
offline de contract API, de aceea totalul runner-ului este intenționat mai mare;
aceste teste unitare nu sunt scenarii API suplimentare în arborele de navigare
Extent. Overview-ul nativ este reconciliat din aceleași carduri directe, fără a
număra containerele-rădă ale claselor.

## Backlog live după audit

Primul rând este păstrat ca dovadă de închidere a regresiei de reporter; nu mai
este un test live lipsă. Rândurile `P0`/`P1`/`P2` sunt acoperirea business și
resilience care încă merită adăugată în scenariile reale.

| Prioritate | Scenariu | Motiv |
|---|---|---|
| closed | reporter failure în `StartStep`, tabel, verdict și `CompleteStep` | protejat offline prin `ValidationReportPublisher` și `CleanupRegistry`; păstrează regresia component |
| P0 | invalid JSON / invalid error envelope | verifică dacă diagnosticul rămâne într-un tabel, fără excepții obscure |
| P0 | timeout și cancellation cu cleanup | confirmă că resursele nu rămân după întrerupere |
| P1 | post create/update/delete cu auth validă | completează lifecycle-ul real pentru al doilea resource |
| P1 | empty collection, pagination, filter și sort | protejează contractele de colecție, nu doar statusul `200` |
| P1 | correlation id și rate-limit headers | face triage-ul operațional reproductibil |
| P2 | profile matrix pe environment | verifică aceeași suită cu base URL/auth diferite, fără modificarea codului |
| P2 | failure demo separat de quality gate | exit code-ul intenționat nu trebuie confundat cu sănătatea suitei normale |

Fiecare scenariu nou trebuie să aibă întâi test component offline pentru request,
route și validator, apoi un singur test live reprezentativ. Nu se duplică testele
de framework în `Atf.Api`.

Layer-ul live este intenționat subțire. Un scenariu nou trebuie introdus numai
când adaugă acoperire business; un defect de kernel sau modul aparține în
`Atf.Tests`, astfel încât să poată rula fără serviciu.

## Ownership-ul failure-ului

```mermaid
sequenceDiagram
    participant X as xUnit
    participant B as ApiTestBase
    participant E as Executor
    participant V as Validator
    participant C as Cleanup
    X->>B: pornește scenariul
    B->>E: execută request
    E-->>B: rezultat transport
    B->>V: validează contractul
    V-->>B: un tabel / excepție
    B->>C: teardown întotdeauna când există ownership
    C-->>B: pass sau warning
    B-->>X: failure original sau skip
```

Cauza originală a failure-ului de scenariu/contract rămâne autoritară. Un warning
de cleanup trebuie să fie vizibil în Extent, dar nu trebuie să șteargă rădăcina
problemei.


