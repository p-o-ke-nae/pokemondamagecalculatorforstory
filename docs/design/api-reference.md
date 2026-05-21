# API リファレンス

> 対象システム: ストーリー攻略用ポケモンダメージ計算 Web API / 管理 API
> 関連ドキュメント: [architecture-overview.md](./architecture-overview.md) | [domain-model.md](./domain-model.md) | [cqrs-handlers.md](./cqrs-handlers.md)

---

## 概要

本ドキュメントは、実装済みの公開 API と管理 API の契約を整理する。

### エンドポイント要約

| 区分 | Path | 認証/認可 | 目的 |
|------|------|-----------|------|
| 公開 | `/api/rule-sets` | Anonymous | `Active` な RuleSet 一覧 |
| 公開 | `/api/rule-sets/{id}` | Anonymous | `Active` な RuleSet 詳細 |
| 管理 | `/api/admin/rule-sets*` | Bearer + `ManageBusinessMasters` | RuleSet 管理 |
| 管理 | `/api/admin/user-authorizations*` | Bearer + `ManageAuthorizationMasters` | ユーザー権限管理 |

> 管理 UI の cookie ログインは API とは別に `/admin/login` で提供する。

---

## 1. 認証・認可

### 1-1. API 認証

| 項目 | 内容 |
|------|------|
| 公開 API | 匿名可 |
| 管理 API | Google access token bearer 認証必須 |
| 認可方式 | role-based policy |
| UI 補足 | 管理 UI は `POST /admin/login` で access token を検証し、`AdminCookie` を発行する。未認証は login へ遷移し、認証済みだが認可不足の UI アクセスは `403 Forbidden` |

### 1-2. ポリシー

| Policy | 許可ロール |
|--------|-----------|
| `ManageBusinessMasters` | `Administrator`, `MasterEditor` |
| `ManageAuthorizationMasters` | `Administrator` |

### 1-3. Permission Catalog

| Permission | 用途 |
|------------|------|
| `masters.view` | 管理画面/管理 API の参照系識別 |
| `masters.rulesets.manage` | RuleSet 管理の識別 |
| `masters.user-authorizations.manage` | UserAuthorizationInfo 管理の識別 |

- allow/deny 判定は上表ではなく policy + role で行う
- `permissions` は catalog 値と重複有無を validator で検証する
- `permissions` の role 整合性は現行実装では API 契約として強制しない

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

| Method | Path | 正常系 | Policy |
|--------|------|--------|--------|
| `GET` | `/api/admin/rule-sets` | `200 OK` | `ManageBusinessMasters` |
| `POST` | `/api/admin/rule-sets` | `201 Created` | `ManageBusinessMasters` |

### 詳細・更新・削除

| Method | Path | 正常系 | Policy |
|--------|------|--------|--------|
| `GET` | `/api/admin/rule-sets/{id}` | `200 OK` | `ManageBusinessMasters` |
| `PUT` | `/api/admin/rule-sets/{id}` | `200 OK` | `ManageBusinessMasters` |
| `DELETE` | `/api/admin/rule-sets/{id}` | `204 NoContent` | `ManageBusinessMasters` |

### 契約メモ

- 管理 API は `Active` / `Draft` / `Archived` の全状態を扱う
- `DELETE` は参照中 Run がある場合 `409 Conflict`
- `Slug` 重複は `409 Conflict`
- 一覧/詳細 DTO には `isReferencedByRuns` を含める
- `POST` / `PUT` / `DELETE` は監査ログ対象で、成功時も業務拒否時も記録する

---

## 4. 管理 UserAuthorizationInfo API

### 一覧・作成

| Method | Path | 正常系 | Policy |
|--------|------|--------|--------|
| `GET` | `/api/admin/user-authorizations` | `200 OK` | `ManageAuthorizationMasters` |
| `POST` | `/api/admin/user-authorizations` | `201 Created` | `ManageAuthorizationMasters` |

### 詳細・更新・削除

| Method | Path | 正常系 | Policy |
|--------|------|--------|--------|
| `GET` | `/api/admin/user-authorizations/{googleUserId}` | `200 OK` | `ManageAuthorizationMasters` |
| `PUT` | `/api/admin/user-authorizations/{googleUserId}` | `200 OK` | `ManageAuthorizationMasters` |
| `DELETE` | `/api/admin/user-authorizations/{googleUserId}` | `204 NoContent` | `ManageAuthorizationMasters` |

### 契約メモ

- 対象ロールは `Administrator` / `MasterEditor` / `Member`
- `permissions` は catalog 値のみ許可し、重複は拒否する
- 最後の `Administrator` の role 変更/削除は `409 Conflict`
- 最後の `Administrator` でも permissions-only 更新は許可される
- 一覧/詳細 DTO には `isLastAdministrator` を含め、`permissions` はソート済みで返す
- `POST` / `PUT` / `DELETE` は監査ログ対象で、成功時も業務拒否時も記録する

---

## 5. DTO

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
public sealed record AdminRuleSetUpsertRequest(
    string Slug,
    int Generation,
    string Title,
    string Version,
    string Status,
    string Summary);

public sealed record AdminUserAuthorizationUpsertRequest(
    string GoogleUserId,
    string Role,
    IReadOnlyList<string> Permissions);
```

---

## 6. エラー契約

| Status | 主なケース |
|--------|------------|
| `400` | 必須項目不足、形式不正、列挙値不正、catalog 外 permission、重複 permissions |
| `401` | 未認証、無効な bearer token |
| `403` | policy 不一致、`Member`、未登録ユーザー |
| `404` | 対象なし、匿名から不可視な RuleSet |
| `409` | slug 重複、参照中 RuleSet 削除、最後の Administrator 保護 |

- エラー応答形式は既存どおり `ProblemDetails`
- 公開 API の route と `RuleSetDto` shape は維持する

---

## 7. 監査ログ契約メモ

| 対象 API | 記録タイミング | 最低記録項目 |
|----------|----------------|--------------|
| `POST /api/admin/rule-sets` | handler 完了時 | actor, operation, target, payload, result |
| `PUT /api/admin/rule-sets/{id}` | handler 完了時 | actor, operation, target, payload, result |
| `DELETE /api/admin/rule-sets/{id}` | handler 完了時 | actor, operation, target, payload, result |
| `POST /api/admin/user-authorizations` | handler 完了時 | actor, operation, target, payload, result |
| `PUT /api/admin/user-authorizations/{googleUserId}` | handler 完了時 | actor, operation, target, payload, result |
| `DELETE /api/admin/user-authorizations/{googleUserId}` | handler 完了時 | actor, operation, target, payload, result |

- 監査ログは API レスポンス契約には含めず、内部監査証跡として扱う
- 業務拒否時は `result = Rejected` とし、`payload.reason` に理由を含める
