# Pokemon Story Damage Calculator Test Plan

## 概要

このドキュメントは Issue #1 の Pokemon story damage calculator に対する Phase 2 のテスト設計骨子です。暫定確認ではなく、本実装へ移るために何をユニットテスト、統合テスト、OpenAPI 回帰、実行基盤互換性テストで固定するかを整理します。

| 項目 | 内容 |
|---|---|
| 対象 Issue | #1 |
| Phase / Step | Phase 2 / Step 2.2.5 |
| 対象設計 | [Pokemon Story Damage Calculator API Design](../designs/pokemon-story-damage-calculator-api-design.md) |
| 主目的 | 実装計画に対応するテスト観点の骨子を先に固定し、Phase 3 で詳細ケースへ展開できる状態にする |

## Status

| Field | Value |
|---|---|
| Document type | test plan skeleton |
| Detail level | Phase 2 implementation-alignment skeleton |

## 1. Planned Test Inventory

| Test area | Level | Main coverage |
|---|---|---|
| Domain / application unit tests | Unit | generation-aware type policy、stale fingerprint、threshold 判定、snapshot diff、progression event 再導出 |
| Use-case split regression | Unit / Integration | progression / verification / damage / compare / threshold の責務分離を固定する |
| Scenario API integration | API / Integration | ruleset → run → initial state(最大 6 体 party) → route → battle → progression → verification → calculations の主導線 |
| Swagger / OpenAPI regression | API / Integration | summary / description / example、canonical path と互換 alias、catalog の読みやすさ |
| Preset / pattern table integration | API / Integration | stable ID resource と compare / threshold 参照の接続 |
| Share / collaboration integration | API / Integration | generic source snapshot、policy-based visibility、revision、metadata diff、snapshot replay |
| Admin import integration | API / Integration | dry-run / commit / publish / audit trail、row-level validation |
| Test platform compatibility | Tooling | Microsoft Testing Platform と Visual Studio 2026 Test Explorer の discoverability / 実行互換 |

## 2. Planned Coverage Matrix

| Area | Required behavior | Test level |
|---|---|---|
| Run lifecycle | run 作成、初期状態更新、route 作成が分割済み use case で通る | Integration |
| Enemy / battle modeling | custom enemy group と arbitrary/custom-group battle を扱える | Integration |
| Progression projection | per-member EXP / EV / PP、warning、`progressionFingerprint` を damage と別 use case で返す | Unit / Integration |
| Route verification | missing initial state、participation 不整合、stale 理由、source reference を返す | Unit / Integration |
| Damage calculation | generation-aware type chart、STAB、critical、warning、source version を返す | Unit / Integration |
| Compare-patterns | canonical path を主契約とし、比較結果専用 response を返す | Unit / Integration |
| Threshold-search | canonical path を主契約とし、最小充足解専用 response を返す | Unit / Integration |
| OpenAPI catalog | endpoint grouping、summary / description / example、canonical / alias の区別が見える | Integration |
| Sharing | fixed version metadata、policy 差分、metadata key 欠落 diff、snapshot replay が通る | Integration |
| Admin import | row-level validation、duplicate summary、publish、audit trail が取れる | Integration |
| Tooling compatibility | `dotnet test` と Visual Studio 2026 の双方で対象テストが発見・実行できる | Tooling |

## 3. Key Scenarios to Lock Before Phase 3

### 3.1 Unit / application scenarios

| Scenario ID | Assertion |
|---|---|
| `PDC-UT-01` | route verification は authority source 欠落時に `initial-state.missing` 相当の issue を返す |
| `PDC-UT-02` | damage calculation は generation-aware type policy を参照し、中央集約の固定表へ直接依存しない |
| `PDC-UT-03` | progression projector は damage 計算なしでも EXP / EV / PP を再導出できる |
| `PDC-UT-04` | threshold-search は compare-patterns と別責務で `allOf` 条件のみを扱う |
| `PDC-UT-05` | snapshot diff は metadata key の欠落自体を差分として列挙する |
| `PDC-UT-06` | `item-use` / `move-change` を含む progression event が route state を再導出する |
| `PDC-UT-07` | use-case split 後も request / response 契約が endpoint から追跡できる |

### 3.2 API / documentation scenarios

| Scenario ID | Assertion |
|---|---|
| `PDC-API-01` | ruleset 一覧取得から run 作成までの主導線が本実装向け契約として通る |
| `PDC-API-02` | initial state、route、enemy group、battle、participation 登録後に progression / verification が成功する |
| `PDC-API-03` | damage / compare-patterns / threshold-search が run owner 認可の下で成功し、canonical path を主契約として公開する |
| `PDC-API-04` | preset / pattern table は stable ID で保存され、compare / threshold から参照できる |
| `PDC-API-05` | share snapshot は viewer/commenter/reviser policy、revision、metadata diff、snapshot replay を扱える |
| `PDC-API-06` | admin import は member を拒否し、admin では dry-run / commit / publish / audit trail を返す |
| `PDC-API-07` | `swagger/v1/swagger.json` にユースケース説明、入力例、canonical / alias の区別が現れる |
| `PDC-API-08` | endpoint catalog 再編後も OpenAPI 上で use case ごとに読みやすく並ぶ |

### 3.3 Tooling scenarios

| Scenario ID | Assertion |
|---|---|
| `PDC-TOOL-01` | Microsoft Testing Platform 構成で `dotnet test` が継続実行できる |
| `PDC-TOOL-02` | Visual Studio 2026 Test Explorer で対象テストが発見できる |
| `PDC-TOOL-03` | API / Infrastructure テストの実行方法が README / docs と矛盾しない |

## 4. Phase 3 Completion Notes to Fill

- TODO: ruleset 非対応 type / move / ability / item を validation error として固定するテストを追加する
- TODO: import dry-run / commit で ruleset 非対応要素が row-level reject になる観点を追記する
- TODO: share replay が ruleset 非対応要素で明示的に失敗する観点を追記する
- TODO: 実装後の具体的な test project / category 構成を追記する
- TODO: generation ごとの fixture / test data 戦略を追記する
- TODO: OpenAPI regression の確認対象 operation を列挙する
- TODO: MTP / Visual Studio 2026 対応に必要な package / runner / adapter 構成を実装準拠で記録する
