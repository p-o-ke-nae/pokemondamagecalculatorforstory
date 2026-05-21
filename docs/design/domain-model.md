# ドメインモデル設計

> 対象システム: ストーリー攻略用ポケモンダメージ計算 Domain 層
> 関連ドキュメント: [architecture-overview.md](./architecture-overview.md) | [api-reference.md](./api-reference.md)

---

## 概要

本ドキュメントは、Phase 2 で管理対象となる主要ドメインモデルの設計骨子を整理する。

### 対象エンティティ

| Entity | 役割 |
|--------|------|
| `RuleSet` | 公開利用されるダメージ計算ルールを表すマスタ |
| `UserAuthorizationInfo` | 管理 UI / 管理 API の role 情報を表す認可マスタ |

---

## 1. RuleSet

### 1-1. 役割

- 世代別ダメージ計算ルールを管理する
- 公開 API では `Active` のみ参照可能
- 管理 API では全状態を作成・更新・削除対象とする

### 1-2. 想定プロパティ

| プロパティ | 型 | 注記 |
|-----------|-----|------|
| `Id` | `Guid` | 識別子 |
| `Slug` | `string` | 一意な業務キー |
| `Generation` | `int` | 世代 |
| `Title` | `string` | 表示名 |
| `Version` | `string` | バージョン |
| `Status` | `string` | `Active` / `Draft` / `Archived` |
| `Summary` | `string` | 概要 |

### 1-3. 不変条件

- `Slug`, `Title`, `Version` は空不可
- `Generation` は 1 以上
- `Status` は `Active` / `Draft` / `Archived` のみ
- 更新時も同じ不変条件を維持する

### 1-4. 削除ルール

- 参照中の `Run` が存在する RuleSet は削除不可
- 参照中かどうかは Domain の派生状態ではなく repository 経由で判定する

---

## 2. UserAuthorizationInfo

### 2-1. 役割

- Google ユーザーの管理権限を保持する
- 管理機能の allow/deny は Phase 2 では `Role` のみで判定する
- `Permissions[]` は保存・表示対象とし、role と矛盾しない catalog 値のみ保持する

### 2-2. 想定プロパティ

| プロパティ | 型 | 注記 |
|-----------|-----|------|
| `GoogleUserId` | `string` | 識別子 |
| `Role` | `string` | `Administrator` / `MasterEditor` / `Member` |
| `Permissions` | `IReadOnlyCollection<string>` | 許可済み権限文字列 (`masters.view`, `masters.rulesets.manage`, `masters.user-authorizations.manage`) |

### 2-3. 不変条件

- `GoogleUserId` は空不可
- `Role` は定義済み 3 値のみ
- `Permissions` は null 不可
- `Permissions` は catalog 定義済み値のみ
- `Permissions` の重複は拒否する
- `Permissions` は `Role` に対応する許容範囲を超えてはならない

### 2-4. Permission Catalog と role 整合

| Role | 保持可能な Permission |
|------|------------------------|
| `Administrator` | `masters.view`, `masters.rulesets.manage`, `masters.user-authorizations.manage` |
| `MasterEditor` | `masters.view`, `masters.rulesets.manage` |
| `Member` | `masters.view` |

> これらの permission は UI 表示や将来拡張用の catalog であり、Phase 2 の認可判定自体は role-based policy が担う。

### 2-5. 保護ルール

- 最後の `Administrator` の role 変更は不可
- 最後の `Administrator` の削除は不可
- ただし `Role == Administrator` を維持する permissions-only 更新は許可対象とする

---

## 3. ロールモデル

| ロール | 管理可能範囲 |
|--------|--------------|
| `Administrator` | RuleSet, UserAuthorizationInfo |
| `MasterEditor` | RuleSet のみ |
| `Member` | 管理機能なし |

> ロール階層は認可ポリシーで利用する。Domain は role の妥当性のみを保持する。

### 3-1. role-based authorization との整合

- `Administrator > MasterEditor > Member` の階層を認可の正規ソースとする
- `Permissions[]` は role より強い権限を付与できない
- policy 判定は role を参照し、permission catalog はデータ整合性の検証対象として扱う

---

## 4. Create / Restore / Update 方針

| パターン | 用途 |
|----------|------|
| `Create(...)` | 新規作成。不変条件を検証する |
| `Restore(...)` | 永続化済みデータの復元 |
| `Update...(...)` | 既存エンティティ変更。不変条件を再検証する |

### 4-1. 例

```csharp
public sealed class RuleSet
{
    public static RuleSet Create(string slug, int generation, string title, string version, string status, string summary) { ... }
    public static RuleSet Restore(Guid id, string slug, int generation, string title, string version, string status, string summary) { ... }
    public void UpdateMetadata(string slug, int generation, string title, string version, string status, string summary) { ... }
}
```

```csharp
public sealed class UserAuthorizationInfo
{
    public static UserAuthorizationInfo Create(string googleUserId, string role, IEnumerable<string> permissions) { ... }
    public static UserAuthorizationInfo Restore(string googleUserId, string role, IEnumerable<string> permissions) { ... }
    public void Update(string role, IEnumerable<string> permissions) { ... }
}
```

---

## 5. Domain Port 方針

### 5-1. RuleSet

```csharp
public interface IRuleSetRepository
{
    Task<IReadOnlyList<RuleSet>> ListActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RuleSet>> ListAllAsync(CancellationToken ct = default);
    Task<RuleSet?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsBySlugAsync(string slug, Guid? excludingId = null, CancellationToken ct = default);
    Task<bool> IsReferencedByRunsAsync(Guid id, CancellationToken ct = default);
    Task<RuleSet> SaveAsync(RuleSet ruleSet, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
```

### 5-2. UserAuthorizationInfo

```csharp
public interface IUserAuthorizationInfoRepository
{
    Task<IReadOnlyList<UserAuthorizationInfo>> ListAllAsync(CancellationToken ct = default);
    Task<UserAuthorizationInfo?> FindByGoogleUserIdAsync(string googleUserId, CancellationToken ct = default);
    Task<bool> IsLastAdministratorAsync(string googleUserId, CancellationToken ct = default);
    Task<UserAuthorizationInfo> SaveAsync(UserAuthorizationInfo entity, CancellationToken ct = default);
    Task<bool> DeleteAsync(string googleUserId, CancellationToken ct = default);
}
```

---

## 6. Domain 外で扱う派生項目

| 項目 | 保持場所 | 理由 |
|------|----------|------|
| `isReferencedByRuns` | Application / DTO | 集計結果であり Entity の本質属性ではない |
| `isLastAdministrator` | Application / DTO | 一覧/詳細表示用の派生情報 |
| Admin audit log | Application / Infrastructure | 横断的な証跡であり Entity の本質属性ではない |

---

## 7. 実装注記

- `Permissions[]` は Phase 2 では認可判定に使わない
- permission catalog の正規値は `AppPermissions` 等に集約し、文字列リテラルの散在を避ける
- 公開 API の `Active` フィルタは Application Query 側で明示する
- repository 実装では重複判定・参照判定・最後の Administrator 判定を一貫して扱う
