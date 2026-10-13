# ATF — backlog de refactorizare

Nu există TODO-uri active în această categorie. Feedback-ul de refactorizare a
fost transformat în contracte verificabile și implementat în codul live; acest
fișier rămâne intenționat scurt și nu păstrează scoruri, capturi, timestamp-uri
sau căi temporare de artifact.

## Contracte închise

| Zonă | Contract stabil | Dovadă principală |
|---|---|---|
| RF-02 · reporting seams | `ExtentTestScope` coordonează prin `IPhaseTreeWriter`, `IReportEvidencePublisher` și `IReportStatusPolicy`; theme/client au provider și composer dedicate | `ReportingPortSeamTests`, `ReportingComponentTests`, README-ul `Atf.Reporting.Extent` |
| RF-03 · namespace boundaries | namespace-urile reporting, Core, API infrastructure și TestHost au ratchet de maximum 8 tipuri top-level directe; boundary-ul API nu referă Extent concret | `PackageBoundaryTests`, `ReportingBoundaryTests` |
| RF-04 · session isolation | lifecycle-urile suprapuse păstrează separat nodurile, statusurile, path-urile, artifactele și cleanup-ul | `OverlappingLifecycleSessionsKeepStateIsolated`, testele `ReportAssetCleanupCoordinator` |
| RF-05 · class navigation | lista `Tests` conține numai clasă/status/durată; `tag` rămâne gol și orice strip moștenit este eliminat; tag-urile sunt owner-ul scenariului din dreapta | `ReportingTagPolicyTests`, `ReportingArtifactContractTests`, verificarea DOM prin TAB MCP |

## Regula de mentenanță

Orice schimbare viitoare în aceste zone trebuie să păstreze simultan:

- separarea dintre ierarhia phase → request + validation și evidence-ul publicat;
- un singur tabel agregat pentru validările unui răspuns;
- boundary-ul renderer-neutral dintre `Atf.Api` și `Atf.Reporting.Extent`;
- izolarea completă a sesiunilor și cleanup-ului în execuții suprapuse;
- lista de clase fără pills/adnotări, cu metadata numai pe scenariul semantic;
- validarea artifactului și a interacțiunilor prin TAB MCP pentru orice schimbare
  de DOM, navigare, collapse, stil sau layout.

Procedura de verificare rămâne în
[`Atf_Feedback_Execution_Guide.md`](Atf_Feedback_Execution_Guide.md), iar
contractele stabile rămân în README-urile modulelor și în documentele permanente
din `docs/`.
