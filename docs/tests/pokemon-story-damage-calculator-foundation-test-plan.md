# Pokemon Story Damage Calculator Foundation Test Plan

## 概要

このドキュメントは Issue #1 の story damage calculator foundation に対する Phase 3 の必須テスト戦略をまとめるものです。承認済み仕様を基準に、何をユニットテストで固定し、何を API / E2E で通し、どの不足が残ってはいけないかを整理します。

| 項目 | 内容 |
|---|---|
| 対象 Issue | #1 |
| Phase / Step | Phase 2 / Step 2.3 |
| 対象設計 | [Pokemon Story Damage Calculator API Foundation](../designs/pokemon-story-damage-calculator-api-foundation.md) |
| 主目的 | 承認済み API / データ契約を満たす検証戦略を固定する |

## Status

| Field | Value |
|---|---|
| Document type | test strategy |
| Detail level | required coverage for Phase 3 |

## 1. Required Test Inventory

| Test area | Level | Main coverage |
|---|---|---|
| Domain / service unit tests | Service / Unit | verification issue 生成、中央管理 type chart、PP warning 付き damage calculation、threshold 判定、stale fingerprint 導出、metadata diff 判定 |
| Scenario API integration | API / Integration | ruleset → run → initial state(最大 6 体 party) → route → enemy group → battle → participation → progression → verify → calculations の E2E |
| Preset / pattern table integration | API / Integration | preset stable ID、IV range、EV pattern、nature pattern の保存と compare / threshold 参照の E2E |
| Share / collaboration integration | API / Integration | generic source snapshot publish、viewer/commenter/reviser policy、revision lineage、metadata diff、snapshot replay の E2E |
| Admin import integration | API / Integration | dry-run / commit、row-level error / warning、duplicate summary、master version publish、audit trail の E2E |

## 2. Required Coverage Matrix

| Area | Required behavior | Test level |
|---|---|---|
| Sample replacement | `GET /api/rulesets` から story calculator domain を返す | Integration |
| Run lifecycle | run 作成、party baseline 初期状態更新、route 作成が通る | Integration |
| Enemy modeling | custom enemy group と arbitrary/custom-group battle を作成でき、敵に type / EXP / EV yield を持てる | Integration |
| Battle participation | battle ごとの suggested party と participation mode を更新できる | Integration |
| Progression projection | route progression 取得と再計算で per-member EXP / EV / PP 状態、`progressionFingerprint`、stale 理由を返す | Integration |
| Quick search | keyword 検索で battle hit を返す | Integration |
| Route verification | missing initial state issue、participation 不整合、stale metadata と正常系 verification を返す | Unit / Integration |
| Damage calculation | damage range、中間式、centralized type effectiveness、PP warning、source references、使用 version を返す | Unit / Integration |
| Preset / pattern table | IV range input と EV / nature pattern table を stable ID 付き resource として保存・参照できる | Integration |
| Compare-patterns | ケース比較結果のみを返し、minimum threshold answer を返さない | Integration |
| Threshold-search | `hp`, `attack`, `defense`, `specialAttack`, `specialDefense`, `speed` の IV を 0..31 の整数・1 刻みで探索し、`minimum-damage-at-least`, `maximum-damage-at-most`, `action-order-at-least` を含む `allOf` 条件に対して `solved` / `no-solution` / `multiple-minimal-solutions` と `bestSolution` / `allMinimalSolutions` / `searchedRangeSummary` / `unsatisfiedConditions` を返す。`anyOf` や priority 指定は validation error | Unit / Integration |
| Progression events | `battle-result`, `item-use`, `rare-candy`, `pp-restore`, `species-change`, `move-change` を route 再計算に反映する | Unit / Integration |
| Sharing | route plan / calculation / comparison / verification を generic source として share 作成でき、viewer/commenter/reviser policy、comment 追加、revision 公開、metadata diff、share policy diff、snapshot replay が通る | Integration |
| Admin authorization | member は admin import を拒否される | Integration |
| Admin import | admin は `mode` / `sourceType` 指定の dry-run / commit、row-level error / warning、duplicate summary、import audit trail、master version set 一覧取得ができる | Integration |

## 3. Key Scenarios Fixed by Tests

### 3.1 Service-level scenarios

| Scenario ID | Assertion |
|---|---|
| `PDC-UT-01` | 初期状態なしの route verification は `initial-state.missing` issue を返す |
| `PDC-UT-02` | damage calculation は STAB / critical modifier、中央管理 type chart、PP warning を含む |
| `PDC-UT-03` | stale fingerprint は `initialStateRevision`、ordered event revision、version digest から決定論的に導出される |
| `PDC-UT-04` | threshold search は 6 種の IV を 0..31 の 1 刻みで探索し、`allOf` 条件のみを評価して状態別 response shape を返す。`anyOf` や priority 指定は validation error |
| `PDC-UT-05` | snapshot diff は metadata key の欠落自体を差分として列挙する |
| `PDC-UT-06` | progression event は `item-use` と `move-change` を含めて route state を再導出する |
| `PDC-UT-07` | snapshot metadata は `importJobIds[]`、追加 master group、share policy digest を保持する |

### 3.2 API end-to-end scenarios

| Scenario ID | Assertion |
|---|---|
| `PDC-API-01` | ruleset 一覧取得から run 作成までの foundation flow が通る |
| `PDC-API-02` | initial state、route、enemy group、battle、participation の登録後に quick search と progression / route verification が成功する |
| `PDC-API-03` | damage / compare-patterns / threshold-search が run owner 認可の下で成功し、threshold-search は最小充足解専用 shape を返す |
| `PDC-API-04` | PP 不足 warning が progression に現れても damage calculation は継続できる |
| `PDC-API-05` | preset / pattern table は stable ID で保存され、damage / compare / threshold の参照元として利用できる |
| `PDC-API-06` | route plan / calculation / comparison / verification の各 generic source を share でき、viewer/commenter/reviser policy、comment、revision publish、metadata diff、share policy diff、snapshot replay が成功する |
| `PDC-API-07` | admin import は member を拒否し、admin では dry-run / commit と row-level validation / duplicate summary / audit trail が取得できる |
| `PDC-API-08` | route reorder / event update 後に stale 判定と `progressionFingerprint` 更新が確認できる |
| `PDC-API-09` | `item-use` / `move-change` event 登録後に progression と damage 入力補助が更新される |

## 4. Minimum Non-Gaps for Phase 3

| Area | Required exit state |
|---|---|
| Validation failure matrix | 必須項目不足、不正 ID、`anyOf` threshold 要求、priority 指定、探索対象外 stat 指定、権限不足を網羅する |
| Route reorder / event update | fingerprint と revision 更新の API テストが存在する |
| Share visibility / collaboration | `owner` / `viewer` / `commenter` / `reviser` の差を検証する |
| Snapshot reproducibility | fixed version set、`importJobIds[]`、digest、metadata key 欠落 diff、snapshot replay のテストが存在する |
| Additional master groups / share policy diff | snapshot metadata に追加 master group が保持され、share policy 差分と import job 参照差分も diff できる |
| Import parser | workbook / spreadsheet 由来の row-level validation を fixture 付きで検証する |
| Import auditability | import 実行者、時刻、対象 ruleset version、影響件数を取得できる |
| Determinism | 同一入力 repeated-run と fixed version set replay を検証する |

## 5. Exit View for Phase 3

| Condition | Required assessment |
|---|---|
| Foundation API main flows | カバー済みであること |
| Threshold / stale / diff contracts | カバー済みであること |
| Share / collaboration / admin import flows | カバー済みであること |
| Reproducibility / auditability contracts | カバー済みであること |
