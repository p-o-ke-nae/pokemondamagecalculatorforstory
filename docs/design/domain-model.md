# ドメインモデル設計

> 対象システム: ストーリー攻略用ポケモンダメージ計算 Domain 層
> 関連ドキュメント: [architecture-overview.md](./architecture-overview.md) | [api-reference.md](./api-reference.md)

---

## 概要

本ドキュメントは、管理マスタ保守機能で扱う主要ドメインモデルの実装内容を整理する。

### 対象エンティティ

| Entity | 役割 |
|--------|------|
| `RuleSet` | 公開利用されるダメージ計算ルールを表すマスタ |
| `UserAuthorizationInfo` | 管理 UI / 管理 API の role と permission 文字列を保持する認可マスタ |

---

## 1. RuleSet

### 1-1. 役割

- 世代別ダメージ計算ルールを管理する
- 公開 API では `Active` のみ参照可能
- 管理 API / UI では全状態を作成・更新・削除対象とする

### 1-2. プロパティ

| プロパティ | 型 | 注記 |
|-----------|-----|------|
| `Id` | `Guid` | 識別子 |
| `Slug` | `string` | 一意な業務キー |
| `Generation` | `int` | 世代 |
| `Title` | `string` | 表示名 |
| `Version` | `string` | バージョン |
| `Status` | `string` | `Active` / `Draft` / `Archived` |
| `Summary` | `string` | 概要。空文字可 |

### 1-3. 不変条件

- `Slug` は空不可、100 文字以下
- `Generation` は 1 以上
- `Title` は空不可、200 文字以下
- `Version` は空不可、50 文字以下
- `Status` は `Active` / `Draft` / `Archived` のみ
- `Summary` は 500 文字以下

### 1-4. 削除ルール

- 参照中の `Run` が存在する RuleSet は削除不可
- 参照中かどうかは Domain の派生状態ではなく repository 経由で判定する

---

## 2. UserAuthorizationInfo

### 2-1. 役割

- Google ユーザーの管理権限情報を保持する
- 管理機能の allow/deny は `Role` に基づく policy で判定する
- `Permissions[]` は保存・表示対象であり、認可判定の正規ソースではない

### 2-2. プロパティ

| プロパティ | 型 | 注記 |
|-----------|-----|------|
| `GoogleUserId` | `string` | 識別子 |
| `Role` | `string` | role 名。Domain では非空のみ保証 |
| `Permissions` | `IReadOnlyCollection<string>` | permission 文字列。空コレクション可 |

### 2-3. 不変条件

- `GoogleUserId` は空不可
- `Role` は空不可
- `Permissions` の各要素は空白不可
- `Permissions` の重複は拒否する

### 2-4. Application 層で補う制約

| 項目 | 実装場所 |
|------|----------|
| role を `Administrator` / `MasterEditor` / `Member` に限定 | admin command validator |
| permission を catalog 値に限定 | admin command validator |
| 最後の `Administrator` の role 変更/削除保護 | admin command handler + repository |

> `permissions` の role ごとの部分集合制約は、現行実装では Domain / Validator のどちらでも強制していない。

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

### 3-1. role-based authorization との整合

- `Administrator > MasterEditor > Member` の階層を認可の正規ソースとする
- `Permissions[]` は付随データとして保持する
- policy 判定は role を参照し、permission catalog は入力検証対象として扱う

---

## 4. Create / Restore / Update 方針

| パターン | 用途 |
|----------|------|
| `Create(...)` | 新規作成。不変条件を検証する |
| `Restore(...)` | 永続化済みデータの復元 |
| `Update(...)` | 既存エンティティ変更。不変条件を再検証する |

### 4-1. 例

```csharp
public sealed class RuleSet
{
    public static RuleSet Create(string slug, int generation, string title, string version, string status, string summary) { ... }
    public static RuleSet Restore(Guid id, string slug, int generation, string title, string version, string status, string summary) { ... }
    public void Update(string slug, int generation, string title, string version, string status, string summary) { ... }
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
    Task<IReadOnlyList<RuleSet>> FindAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuleSet>> FindAllActiveAsync(CancellationToken cancellationToken = default);
    Task<RuleSet?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RuleSet?> FindActiveByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(string slug, Guid? excludingId = null, CancellationToken cancellationToken = default);
    Task<bool> IsReferencedByRunsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> FindReferencedRuleSetIdsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(RuleSet ruleSet, CancellationToken cancellationToken = default);
    Task UpdateAsync(RuleSet ruleSet, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
```

### 5-2. UserAuthorizationInfo

```csharp
public interface IUserAuthorizationInfoRepository
{
    Task<IReadOnlyList<UserAuthorizationInfo>> FindAllAsync(CancellationToken cancellationToken = default);
    Task<UserAuthorizationInfo?> FindByGoogleUserIdAsync(string googleUserId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByGoogleUserIdAsync(string googleUserId, CancellationToken cancellationToken = default);
    Task<int> CountByRoleAsync(string role, CancellationToken cancellationToken = default);
    Task AddAsync(UserAuthorizationInfo userAuthorizationInfo, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserAuthorizationInfo userAuthorizationInfo, CancellationToken cancellationToken = default);
    Task DeleteAsync(string googleUserId, CancellationToken cancellationToken = default);
}
```

---

## 6. Domain 外で扱う派生項目

| 項目 | 保持場所 | 理由 |
|------|----------|------|
| `isReferencedByRuns` | Application / DTO | Run 集計結果であり Entity の本質属性ではない |
| `isLastAdministrator` | Application / DTO | `CountByRoleAsync()` に基づく表示用派生情報 |
| Admin audit log | Application / Infrastructure | 横断的な証跡であり Entity の本質属性ではない |

---

## 7. 実装注記

- `Permissions[]` は認可判定に使わない
- permission catalog の正規値は `AppPermissions` に集約する
- 公開 API の `Active` フィルタは Application Query 側で明示する
- repository 実装では重複判定・参照判定・管理者件数集計を一貫して扱う
