# アーキテクチャ概要

> 対象システム: ストーリー攻略用ポケモンダメージ計算 Web / 管理 UI / 管理 API
> 関連ドキュメント: [domain-model.md](./domain-model.md) | [api-reference.md](./api-reference.md) | [cqrs-handlers.md](./cqrs-handlers.md)

---

## 概要

本ドキュメントは、管理マスタ保守機能の実装後アーキテクチャを整理する。

### 主要な設計ポイント

| 項目 | 方針 |
|------|------|
| Web ホスト | 既存 `PokemonDamageCalculatorForStory.Web` に公開 API・管理 API・管理 UI・管理ログインを集約 |
| UI 方式 | 管理 UI は同一 Web プロジェクト内の server-rendered MVC (`Controllers` + Razor Views) で提供 |
| アプリケーション構造 | Hexagonal Architecture + CQRS + MediatR |
| 管理対象 | `RuleSet` と `UserAuthorizationInfo` |
| 認可モデル | `Administrator > MasterEditor > Member` |
| Permission Catalog | `masters.view`, `masters.rulesets.manage`, `masters.user-authorizations.manage` |
| 公開 RuleSet API | 匿名利用では `Active` のみ可視 |
| 管理 UI 認証 | `POST /admin/login` で Google access token を検証し、`AdminCookie` を発行 |

---

## 1. レイヤー構成

### 1-1. 依存関係

```mermaid
graph TD
    Web["Web<br/>API Controllers / MVC Pages / Auth"]
    App["Application<br/>Public Queries / Admin UseCases / DTOs / Validators"]
    Infra["Infrastructure<br/>EF Core / Repository / Logging"]
    Domain["Domain<br/>Entities / Ports / Invariants"]

    Web --> App
    Web --> Infra
    App --> Domain
    Infra --> App
    Infra --> Domain
```

### 1-2. 各層の責務

| 層 | 責務 |
|----|------|
| Web | 公開 API、`/api/admin/*`、`/admin/*` MVC ページ、cookie ログイン/ログアウト、認証/認可ポリシー適用、actor 情報の受け渡し |
| Application | 公開系 Query、管理系 Command/Query、DTO、FluentValidation、派生表示項目の組み立て、admin mutation の監査イベント生成 |
| Domain | `RuleSet` / `UserAuthorizationInfo` の不変条件、Port 定義 |
| Infrastructure | CRUD、重複判定、参照中判定、管理者件数集計、構造化アプリケーションログ出力 |

---

## 2. Web プロジェクトの配置方針

### 2-1. 同一ホスト方針

- 管理 UI は既存 Web プロジェクト配下に追加する
- 管理 UI は MVC Controller から MediatR の Command / Query を直接呼び出す
- UI から `DbContext` や Repository を直接参照しない
- 公開 API、管理 API、管理 UI の責務を Controller / UseCase で分離する

### 2-2. 実装ルート

| 種別 | ルート | 用途 |
|------|--------|------|
| UI | `/admin/login` | Google access token 入力と cookie セッション作成 |
| UI | `/admin/masters/rule-sets` | RuleSet 一覧 |
| UI | `/admin/masters/rule-sets/new` | RuleSet 新規作成 |
| UI | `/admin/masters/rule-sets/{id}` | RuleSet 詳細/編集 |
| UI | `/admin/masters/user-authorizations` | UserAuthorizationInfo 一覧 |
| UI | `/admin/masters/user-authorizations/new` | UserAuthorizationInfo 新規作成 |
| UI | `/admin/masters/user-authorizations/{googleUserId}` | UserAuthorizationInfo 詳細/編集 |
| UI | `/admin/logout` | cookie セッション破棄 |
| API | `/api/admin/rule-sets` | RuleSet 管理 API |
| API | `/api/admin/user-authorizations` | UserAuthorizationInfo 管理 API |

---

## 3. 認証・認可

### 3-1. ロール階層

| ロール | 説明 |
|--------|------|
| `Administrator` | RuleSet 管理と UserAuthorizationInfo 管理が可能 |
| `MasterEditor` | RuleSet 管理が可能 |
| `Member` | 管理機能なし |

### 3-2. ポリシー

| Policy | 認証スキーム | 許可ロール | 対象 |
|--------|--------------|-----------|------|
| `ManageBusinessMasters` | Bearer / `AdminCookie` | `Administrator`, `MasterEditor` | RuleSet 管理 UI / API |
| `ManageAuthorizationMasters` | Bearer / `AdminCookie` | `Administrator` | UserAuthorizationInfo 管理 UI / API |

### 3-3. Permission Catalog

| Permission | 説明 | 備考 |
|------------|------|------|
| `masters.view` | 管理 UI / 管理 API の参照系識別用 | 保存・表示対象 |
| `masters.rulesets.manage` | RuleSet 管理対象の識別用 | 保存・表示対象 |
| `masters.user-authorizations.manage` | UserAuthorizationInfo 管理対象の識別用 | 保存・表示対象 |

### 3-4. 適用方針

- 認可判定は `UserAuthorizationInfo.Role` を基準に行う
- `Permissions[]` は保存・表示対象で、管理用 Command の validator で catalog 値と重複有無のみを検証する
- `Permissions[]` は role を上書きしない。allow/deny 判定は常に role-based policy を使う
- 管理 UI は `AdminCookie` 認証を使い、既存 bearer API は維持する
- bearer 認証ユーザーには `IClaimsTransformation` で DB 上の role / permissions を補完する

---

## 4. 公開系と管理系の分離

### 4-1. RuleSet

| 系統 | 用途 | 可視性 |
|------|------|--------|
| 公開 API | 計算用ルールセット参照 | `Active` のみ |
| 管理 API / UI | RuleSet の作成・更新・削除・詳細参照 | policy に従う |

### 4-2. UserAuthorizationInfo

| 系統 | 用途 | 可視性 |
|------|------|--------|
| 公開 API | なし |
| 管理 API / UI | ユーザー権限マスタ管理 | `Administrator` のみ |

---

## 5. 代表フロー

### 5-1. 公開 RuleSet 参照

```mermaid
sequenceDiagram
    participant Client
    participant Controller as RuleSetsController
    participant Query as GetAllRuleSetsQuery
    participant Repo as IRuleSetRepository

    Client->>Controller: GET /api/rule-sets
    Controller->>Query: Send
    Query->>Repo: FindAllActiveAsync()
    Repo-->>Query: Active RuleSets
    Query-->>Controller: RuleSetDto[]
    Controller-->>Client: 200 OK
```

### 5-2. 管理 UI ログイン

```mermaid
sequenceDiagram
    participant Admin
    participant Session as AdminSessionController
    participant Google as Google Token Validator
    participant Repo as IUserAuthorizationInfoRepository
    participant Cookie as AdminCookie

    Admin->>Session: POST /admin/login (accessToken)
    Session->>Google: ValidateAsync(accessToken)
    Google-->>Session: googleUserId / profile
    Session->>Repo: FindByGoogleUserIdAsync()
    Repo-->>Session: role / permissions
    Session->>Cookie: SignInAsync(AdminCookie)
    Session-->>Admin: 302 Redirect
```

### 5-3. 管理系更新

```mermaid
sequenceDiagram
    participant Admin
    participant Web as Admin API / MVC Controller
    participant Policy as Authorization Policy
    participant Command as Admin Command Handler
    participant Domain
    participant Repo as Repository
    participant Audit as Admin Audit Logger

    Admin->>Web: 管理更新要求
    Web->>Policy: policy 評価
    Policy-->>Web: 許可 / 拒否
    Web->>Command: Command + actor info
    Command->>Domain: Create / Update
    Command->>Repo: Save / Delete / Check
    Repo-->>Command: 結果
    Command->>Audit: 構造化監査イベント記録
    Command-->>Web: Admin DTO / bool
```

---

## 6. 監査ログ設計

### 6-1. 配置方針

- 対象は admin mutation Command とし、API / MVC のどちらから呼ばれても同じ handler で記録する
- 監査ログ生成の責務は Application の admin command handler に置き、Web 層は actor/context の受け渡しに留める
- 出力は Infrastructure の `IAdminAuditLogger` 実装が `ILogger` へ構造化ログ (`AdminAudit`) を出力する

### 6-2. 記録ペイロード

| 項目 | 内容 |
|------|------|
| `OccurredAtUtc` | 実行時刻 |
| `ActorGoogleUserId` | 実行ユーザーの Google User ID |
| `ActorRole` | 実行時点の role |
| `Operation` | `Create` / `Update` / `Delete` |
| `TargetType` | `RuleSet` または `UserAuthorizationInfo` |
| `TargetId` | `RuleSet.Id` または `GoogleUserId` |
| `Payload` | 変更要求の要約。業務拒否時は `reason` を含む |
| `Result` | `Succeeded` / `Rejected` |

### 6-3. 整合性メモ

- `403` の policy 拒否は handler 未到達のため監査対象外
- `409` などの業務拒否は `Result = Rejected` として記録する
- permission は payload に含めうるが、認可判定根拠は role のままとする

---

## 7. 実装上の注記

- `isReferencedByRuns` は `FindReferencedRuleSetIdsAsync()`、`isLastAdministrator` は `CountByRoleAsync()` を使って Application で組み立てる
- 一覧系クエリは `AsNoTracking` を基本とし、派生フラグ計算で N+1 を避ける
- 管理 UI 一覧画面の検索はクライアント側フィルタリングで実装している
- admin mutation の監査ログは Web 統合テストで「成功時と業務拒否時の双方で記録されること」を確認する
