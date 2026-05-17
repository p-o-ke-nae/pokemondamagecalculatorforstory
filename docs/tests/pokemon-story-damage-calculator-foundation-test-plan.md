# Pokemon Story Damage Calculator Foundation Test Plan

## 概要

このドキュメントは Issue #1 の story damage calculator foundation に対する現在の自動テスト範囲をまとめるものです。Phase 2 の skeleton を置き換え、`PokemonStoryServiceTests` と `StoryApiTests` が何を固定しているか、まだ未カバーの範囲は何かを整理します。

| 項目 | 内容 |
|---|---|
| 対象 Issue | #1 |
| Phase / Step | Phase 3 / Step 3.4.5 |
| 対象設計 | [Pokemon Story Damage Calculator API Foundation](../designs/pokemon-story-damage-calculator-api-foundation.md) |
| 主目的 | 実装済み foundation API と service の自動テスト範囲を記録する |

## Status

| Field | Value |
|---|---|
| Document type | implementation-aligned test plan |
| Detail level | current automated coverage |

## 1. Current Test Inventory

| Test file | Level | Main coverage |
|---|---|---|
| `PokemonStoryServiceTests.cs` | Service / Unit | verification issue 生成、damage modifier と source reference の確認 |
| `StoryApiTests.cs` | API / Integration | ruleset → run → initial state → route → enemy group → battle → search → verify → calculations の E2E |
| `StoryApiTests.cs` | API / Integration | share / comment / revision / diff と admin import / master-version-set API の E2E |

## 2. Automated Coverage Matrix

| Area | Covered behavior | Test level |
|---|---|---|
| Sample replacement | `GET /api/rulesets` から story calculator domain を返す | Integration |
| Run lifecycle | run 作成、初期状態更新、route 作成が通る | Integration |
| Enemy modeling | custom enemy group と arbitrary/custom-group battle を作成できる | Integration |
| Quick search | keyword 検索で battle hit を返す | Integration |
| Route verification | missing initial state issue と正常系 verification を返す | Unit / Integration |
| Damage calculation | damage range、applied modifiers、source references を返す | Unit / Integration |
| Compare-patterns | 1 パターン以上の比較結果を返す | Integration |
| Threshold-search | candidate が返る | Integration |
| Sharing | share 作成、comment 追加、revision 公開、diff 取得が通る | Integration |
| Admin authorization | member は admin import を拒否される | Integration |
| Admin import | admin は import job 作成と master version set 一覧取得ができる | Integration |

## 3. Key Scenarios Fixed by Tests

### 3.1 Service-level scenarios

| Scenario ID | Assertion |
|---|---|
| `PDC-UT-01` | 初期状態なしの route verification は `initial-state.missing` issue を返す |
| `PDC-UT-02` | damage calculation は STAB / critical modifier と source reference を含む |

### 3.2 API end-to-end scenarios

| Scenario ID | Assertion |
|---|---|
| `PDC-API-01` | ruleset 一覧取得から run 作成までの foundation flow が通る |
| `PDC-API-02` | initial state、route、enemy group、battle の登録後に quick search と route verification が成功する |
| `PDC-API-03` | damage / compare-patterns / threshold-search が run owner 認可の下で成功する |
| `PDC-API-04` | share、comment、revision publish、diff 取得が成功する |
| `PDC-API-05` | admin import は member を拒否し、admin では commit job を作成できる |

## 4. Current Gaps

| Area | Current status |
|---|---|
| Google access token validation | 実トークン / 外部 API 連携は未自動化 |
| SQL Server persistence | API テストは InMemory provider のみ |
| Validation failure matrix | 必須項目不足や不正 ID 参照の網羅的 API テストは未追加 |
| Route reorder / event update | fingerprint と revision 更新の API テストは未追加 |
| Share visibility | `public` のみ匿名 read を許可し、`private` / `unlisted` は owner 限定 |
| Import parser | workbook 内容の解析機能自体が未実装 |

## 5. Exit View for Current Foundation

| Condition | Current assessment |
|---|---|
| Foundation API main flows | カバー済み |
| Analysis endpoints basic flows | カバー済み |
| Share / admin flows basic paths | カバー済み |
| Production-like auth / SQL persistence | 未カバー |
