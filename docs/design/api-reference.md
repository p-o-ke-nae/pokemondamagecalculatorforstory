# API リファレンス

> 対象システム: ストーリー攻略用ポケモンダメージ計算 Web API / 管理 API
> 関連ドキュメント: [architecture-overview.md](./architecture-overview.md) | [domain-model.md](./domain-model.md) | [cqrs-handlers.md](./cqrs-handlers.md)

---

## 概要

本ドキュメントは、Phase 2 で合意された公開 API と管理 API の契約骨子を示す。

### エンドポイント要約

| 区分 | Path | 認証/認可 | 目的 |
|------|------|-----------|------|
| 公開 | `/api/rule-sets` | Anonymous | `Active` な RuleSet 一覧 |
| 公開 | `/api/rule-sets/{id}` | Anonymous | `Active` な RuleSet 詳細 |
| 管理 | `/api/admin/rule-sets*` | `ManageBusinessMasters` | RuleSet 管理 |
| 管理 | `/api/admin/user-authorizations*` | `ManageAuthorizationMasters` | ユーザー権限管理 |

> 既存の `Run` / `Battle` 系 API は本仕様変更の対象外。

---

## 1. 認証・認可

### 1-1. API 認証

| 項目 | 内容 |
|------|------|
| 公開 API | 匿名可 |
| 管理 API | 認証必須 |
| 認可方式 | policy ベース |
| UI 補足 | 管理 UI では同一 Web ホスト上で cookie/session を併用する可能性がある |

### 1-2. ポリシー

| Policy | 許可ロール |
|--------|-----------|
| `ManageBusinessMasters` | `Administrator`, `MasterEditor` |
| `ManageAuthorizationMasters` | `Administrator` |

### 1-3. Permission Catalog

| Permission | 用途 | 保持可能ロール |
|------------|------|----------------|
| `masters.view` | 管理画面/管理 API の参照系識別 | `Administrator`, `MasterEditor`, `Member` |
| `masters.rulesets.manage` | RuleSet 管理の識別 | `Administrator`, `MasterEditor` |
| `masters.user-authorizations.manage` | UserAuthorizationInfo 管理の識別 | `Administrator` |

- Phase 2 の allow/deny は上表ではなく policy + role で判定する
- `permissions` は catalog 外文字列を拒否し、role に対して過剰な値は受け付けない

---

## 2. 公開 RuleSet API

### GET /api/rule-sets

- **認証**: Anonymous
- **レスポンス**: `200 OK` — `RuleSetDto[]`
- **可視性**: `Status == Active` のみ返却

### GET /api/rule-sets/{id}

- **認証**: Anonymous
- **パス**: `id: Guid`
- **レスポンス**: `200 OK` — `RuleSetDto`
- **不可視時**: `Draft` / `Archived` / 不存在は `404 Not Found`

---

## 3. 管理 RuleSet API

### 一覧・作成

| Method | Path | 説明 | Policy |
|--------|------|------|--------|
| `GET` | `/api/admin/rule-sets` | RuleSet 一覧取得 | `ManageBusinessMasters` |
| `POST` | `/api/admin/rule-sets` | RuleSet 新規作成 | `ManageBusinessMasters` |

### 詳細・更新・削除

| Method | Path | 説明 | Policy |
|--------|------|------|--------|
| `GET` | `/api/admin/rule-sets/{id}` | RuleSet 詳細取得 | `ManageBusinessMasters` |
| `PUT` | `/api/admin/rule-sets/{id}` | RuleSet 更新 | `ManageBusinessMasters` |
| `DELETE` | `/api/admin/rule-sets/{id}` | RuleSet 削除 | `ManageBusinessMasters` |

### 契約メモ

- 管理 API は `Active` / `Draft` / `Archived` の全状態を扱う
- `DELETE` は参照中 Run がある場合 `409 Conflict`
- `Slug` 重複は `409 Conflict`
- 一覧/詳細 DTO には `isReferencedByRuns` を含める
- `POST` / `PUT` / `DELETE` は監査ログ対象とし、actor・target・変更要約・結果を記録する

---

## 4. 管理 UserAuthorizationInfo API

### 一覧・作成

| Method | Path | 説明 | Policy |
|--------|------|------|--------|
| `GET` | `/api/admin/user-authorizations` | ユーザー権限一覧取得 | `ManageAuthorizationMasters` |
| `POST` | `/api/admin/user-authorizations` | ユーザー権限新規作成 | `ManageAuthorizationMasters` |

### 詳細・更新・削除

| Method | Path | 説明 | Policy |
|--------|------|------|--------|
| `GET` | `/api/admin/user-authorizations/{googleUserId}` | 詳細取得 | `ManageAuthorizationMasters` |
| `PUT` | `/api/admin/user-authorizations/{googleUserId}` | 更新 | `ManageAuthorizationMasters` |
| `DELETE` | `/api/admin/user-authorizations/{googleUserId}` | 削除 | `ManageAuthorizationMasters` |

### 契約メモ

- 対象ロールは `Administrator` / `MasterEditor` / `Member`
- `permissions` は以下 catalog のみ許可: `masters.view`, `masters.rulesets.manage`, `masters.user-authorizations.manage`
- `permissions` は role 整合性を満たす組み合わせのみ許可する
- 最後の `Administrator` の変更/削除は `409 Conflict`
- 一覧/詳細 DTO には `isLastAdministrator` を含める
- `POST` / `PUT` / `DELETE` は監査ログ対象とし、変更成功/業務拒否の双方を記録する

---

## 5. DTO 骨子

### 公開 DTO

```csharp
public sealed record RuleSetDto(
    Guid Id,
    string Slug,
    int Generation,
    string Title,
    string Version,
    string Status,
    string Summary);
```

### 管理 DTO

```csharp
public sealed record AdminRuleSetDto(
    Guid Id,
    string Slug,
    int Generation,
    string Title,
    string Version,
    string Status,
    string Summary,
    bool IsReferencedByRuns);

public sealed record AdminUserAuthorizationDto(
    string GoogleUserId,
    string Role,
    IReadOnlyList<string> Permissions,
    bool IsLastAdministrator);
```

### 管理 Request DTO

```csharp
public sealed record CreateRuleSetRequest(
    string Slug,
    int Generation,
    string Title,
    string Version,
    string Status,
    string Summary);

public sealed record UpdateRuleSetRequest(
    string Slug,
    int Generation,
    string Title,
    string Version,
    string Status,
    string Summary);

public sealed record CreateUserAuthorizationRequest(
    string GoogleUserId,
    string Role,
    IReadOnlyList<string> Permissions);

public sealed record UpdateUserAuthorizationRequest(
    string Role,
    IReadOnlyList<string> Permissions);
```

---

## 6. エラー契約

| Status | 主なケース |
|--------|------------|
| `400` | 必須項目不足、形式不正、列挙値不正、catalog 外 permission、role 非整合 permission、重複 permissions |
| `401` | 未認証、無効な認証情報 |
| `403` | policy 不一致、`Member`、未登録ユーザー |
| `404` | 対象なし、匿名から不可視な RuleSet |
| `409` | slug 重複、参照中 RuleSet 削除、最後の Administrator 保護 |

- エラー応答形式は既存どおり `ProblemDetails`
- 公開 API の route と `RuleSetDto` shape は維持する

---

## 7. 監査ログ契約メモ

| 対象 API | 記録タイミング | 最低記録項目 |
|----------|----------------|--------------|
| `POST /api/admin/rule-sets` | handler 完了時 | actor, action, target, payload, result |
| `PUT /api/admin/rule-sets/{id}` | handler 完了時 | actor, action, target, payload, result |
| `DELETE /api/admin/rule-sets/{id}` | handler 完了時 | actor, action, target, payload, result |
| `POST /api/admin/user-authorizations` | handler 完了時 | actor, action, target, payload, result |
| `PUT /api/admin/user-authorizations/{googleUserId}` | handler 完了時 | actor, action, target, payload, result |
| `DELETE /api/admin/user-authorizations/{googleUserId}` | handler 完了時 | actor, action, target, payload, result |

- 監査ログは API レスポンス契約には含めず、内部監査証跡として扱う
- 検証は Web 統合テストまたは Application 統合テストで、記録内容まで確認する
