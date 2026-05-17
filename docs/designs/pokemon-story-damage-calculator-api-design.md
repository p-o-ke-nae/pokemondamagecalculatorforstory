# Pokemon Story Damage Calculator API Design

## 概要

このドキュメントは Issue #1 の Pokemon story damage calculator に対する Phase 2 の設計骨子です。暫定実装ではなく、実運用を見据えた本実装へ移行するための責務分割、API 境界、テスト整合条件を整理します。

| 項目 | 内容 |
|---|---|
| 対象 Issue | #1 |
| Phase / Step | Phase 2 / Step 2.2.5 |
| 主目的 | 実装計画に追従した設計の骨子を固定し、Phase 3 で詳細を肉付けできる状態にする |
| 関連アーキテクチャ | [Architecture Overview](../architecture.md) |
| 関連テスト | [Pokemon Story Damage Calculator Test Plan](../tests/pokemon-story-damage-calculator-test-plan.md) |

## Status

| Field | Value |
|---|---|
| Document type | design skeleton |
| Detail level | Phase 2 implementation-alignment skeleton |
| Implementation stance | production implementation planned |

## 1. Design Direction Snapshot

| Area | Direction |
|---|---|
| 実装スタンス | story damage calculator を stub ではなく、本機能の実実装として再編する |
| API style | ASP.NET Core minimal API (`/api/**`) を維持しつつ、ユースケース単位で endpoint catalog を読みやすく再整理する |
| AuthN/AuthZ | Google bearer と test auth 差し替えを維持し、run owner / share policy / admin role の責務を明示する |
| Persistence | EF Core + SQL Server / InMemory を継続し、run・share・import の authority source を明確化する |
| 型配置 | public / internal を問わず、継続保守が必要な型は one-type-per-file を基本方針とする |
| テスト実行基盤 | `dotnet test` と Visual Studio 2026 の双方で扱いやすいように Microsoft Testing Platform 対応を前提に整理する |

## 2. Planned Module Boundaries

| Module | Responsibility | Planned decomposition note |
|---|---|---|
| `Rulesets` | `Ruleset` と `MasterVersionSet` の参照、公開状態、version catalog の提供 | generation / version 依存情報の入口をここに集約する |
| `Scenarios` | `RunAggregate`, `RunInitialState`, `RoutePlan`, `ProgressionEvent`, `BattleDefinition`, `EnemyGroupDefinition` を管理する | route authority と battle authority を分離し、再導出対象を明確にする |
| `Progression` | battle participation と route progression projection を扱う | damage 計算とは別 use case / 別コードパスとして維持する |
| `Calculations` | route verification、single-case damage、compare-patterns、threshold-search を扱う | 大きな service を分割し、ユースケース単位の handler / service 群へ寄せる |
| `CalculationPresets` | preset / pattern table を user-owned resource として扱う | compare-patterns / threshold-search と疎結合にする |
| `Sharing` | immutable snapshot、revision、comment、diff を扱う | generic source と fixed metadata を維持する |
| `IdentityAccess` | current user abstraction、policy 名、認可のアプリケーション境界を提供する | Web 依存を Application から切り離す |
| `AdminImports` | import dry-run / commit / publish / audit trail を扱う | OpenAPI catalog 上でも use case を分離して見せる |

## 3. Key Refactoring Policies

### 3.1 One-Type-Per-File / Decomposition

- Domain / Application / Infrastructure / Web の主要型は one-type-per-file を原則とする
- 巨大な service / endpoint 定義 / DTO 集約を、ユースケース単位のファイル群へ分割する
- 分割単位は「run 管理」「progression projection」「route verification」「damage」「compare-patterns」「threshold-search」「share」「admin import」を基本とする
- TODO(Phase 3): 実際の namespace / folder 命名と配置ルールをコード構成に合わせて追記する

### 3.2 Generation-Aware Type Handling

- type 相性は単一の固定表ではなく、ruleset / generation に応じた参照戦略で扱う
- `PokemonType` 自体はドメイン共通語彙として保ちつつ、type chart の適用は ruleset-versioned policy に委譲する
- damage explanation と verification source reference には、どの generation / type chart version を参照したかが追跡できる設計にする
- TODO(Phase 3): 対応 generation の実装済み範囲と未対応差分を明記する

### 3.3 Use-Case Split from Large Service

| Current pressure point | Split direction |
|---|---|
| progression と damage が同じ大きな service に集まりやすい | `StoryProgressionProjector` と damage kernel を別 use case として分離する |
| compare-patterns と threshold-search の責務が混ざりやすい | compare は比較のみ、threshold-search は最小充足解探索のみに限定する |
| Web endpoint 定義が肥大化しやすい | endpoint mapping を capability 単位の extension / catalog に分ける |
| import / share / preset まわりの DTO が混在しやすい | resource ごとに request / response を分離し、相互参照は ID 契約でつなぐ |

## 4. Planned API / Documentation Shape

| Capability | Direction | Documentation note |
|---|---|---|
| rulesets / master versions | run 作成時の version authority を明示する | seed 前提ではなく version catalog 契約として記載する |
| runs / initial state / routes | 最大 6 体 party、ordered event、stale / fingerprint の authority を整理する | derived read model を authority と書かない |
| progression | participation、EXP / EV / PP、warning、recalculate を damage から分離する | 「damage の補助」ではなく独立 use case として記述する |
| damage / compare / threshold | canonical path と互換 alias を整理し、責務分離を明示する | OpenAPI では canonical path を主表記とする |
| sharing | fixed version metadata、policy-based visibility、revision / diff を整理する | metadata key 欠落も diff 対象と明記する |
| admin imports | dry-run / commit / publish / audit trail を独立 capability として扱う | row-level validation と source metadata を公開契約に含める |

### OpenAPI Catalog Readability

- endpoint は use case ごとにタグ / catalog / summary を揃える
- operation summary / description には「何に使うか」と「最低限の入力例」を含める
- canonical path と compatibility alias は、同列ではなく「主契約」と「互換」の関係で記述する
- TODO(Phase 3): 実際の Swagger 画面構成と example payload の差し込み位置を追記する

## 5. Testing and Tooling Alignment

| Topic | Required direction |
|---|---|
| Unit / application tests | generation-aware type handling、progression / damage 分離、threshold-search 専用責務を個別に固定する |
| API integration tests | ruleset → run → route → progression / calculation / share / import の主導線を維持する |
| OpenAPI regression | summary / description / example / canonical path 表記を検証対象に含める |
| Test platform | Microsoft Testing Platform と Visual Studio 2026 Test Explorer の両方で発見・実行しやすい構成を優先する |
| Documentation sync | Phase 3 では実装差分に合わせて resource 名、endpoint 名、テスト観点を更新する |

## 6. Ruleset Support Boundary

| Resource kind | Support rule | Required behavior |
|---|---|---|
| Pokemon type | ruleset / generation / type chart version で利用可能な型のみ使用可能 | 非対応型を含む calculation / verification / run update / share replay は validation error とし、内部で別型へ正規化しない |
| Move | ruleset が許可する世代・版に存在する move のみ使用可能 | 非対応 move は request validation error、import では row-level reject として扱う |
| Ability | ruleset が許可する世代・版に存在する ability のみ使用可能 | 非対応 ability は request validation error、import では row-level reject として扱う |
| Item | ruleset が許可する世代・版に存在する item のみ使用可能 | 非対応 item は request validation error、import では row-level reject として扱う |

- UI や OpenAPI は、ruleset 非対応要素を「選べない / 通らない」契約で表現する
- import dry-run / commit は、ruleset 非対応要素を warning ではなく validation error として返す
- explanation / problem details には、どの ruleset / generation / version catalog 判定で拒否したかを追跡できる情報を残す
- share snapshot replay でも、作成時 ruleset で有効だった要素が再生先 ruleset で無効なら replay 不可として明示的に失敗させる

## 7. Phase 3 Fill-In Checklist

- TODO: 実装後の endpoint 一覧と auth 境界を確定する
- TODO: generation-aware type policy の実装済み範囲を記録する
- TODO: use-case ごとの主要 request / response 型を列挙する
- TODO: OpenAPI catalog のスクリーンショットまたは確認観点を追記する
- TODO: Microsoft Testing Platform / Visual Studio 2026 互換に必要な package / runner 構成を実装準拠で追記する
