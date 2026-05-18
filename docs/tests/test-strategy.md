# テスト戦略

> 対象システム: ストーリー攻略用ポケモンダメージ計算 Web API
> 関連ドキュメント: [architecture-overview.md](../design/architecture-overview.md) | [domain-model.md](../design/domain-model.md)

---

## 概要

本ドキュメントはテスト戦略とテスト対象の一覧を定義する。

### テスト構成サマリ

| 種別 | クラス | DB 接続 |
|------|-----|---------|
| ユニットテスト（Infrastructure） | 1 クラス | 不要 |
| 統合テスト（Repository / Infrastructure） | 1 クラス | SQL Server 必要 |
| 統合テスト（Controller / E2E） | 2 クラス | インメモリ DB（WebApplicationFactory） |

---

## 1. ユニットテスト

DB 接続不要。高速に実行できる。

### 対象クラスと検証内容

| テストクラス | 配置パス | テスト対象 | 検証内容 |
|------------|---------|-----------|---------|
| `DesignTimeConnectionStringResolverTests` | `Tests/Infrastructure/Data/DesignTimeConnectionStringResolverTests.cs` | `Infrastructure/Data/DesignTimeConnectionStringResolver.cs` | 環境変数・appsettings による接続文字列解決ロジック（優先度・正規化） |

---

## 2. 統合テスト（Repository）

SQL Server への接続が必要。`InfrastructureSqlServerTestDatabase` パターンを継承して実装する。

### 対象クラスと検証内容

| テストクラス | 配置パス | テスト対象 | 検証内容 |
|------------|---------|-----------|---------|
| `UserAuthorizationInfoRepositoryTests` | `Tests/Infrastructure/Repositories/UserAuthorizationInfoRepositoryTests.cs` | `Infrastructure/Repositories/UserAuthorizationInfoRepository.cs` | `FindByGoogleUserIdAsync`・Role / Permission の正確な復元 |

### 統合テスト基盤

```csharp
// 既存パターンを継承
// Tests/Infrastructure/TestSupport/InfrastructureSqlServerTestDatabase.cs
```

Docker Compose を使用してテスト用 SQL Server を起動する。

```bash
docker compose -f docker-compose.dotnet.test-db.yml up -d
```

---

## 3. 統合テスト（Controller / E2E）

`CustomWebApplicationFactory` を使用してインメモリ DB でアプリ全体を起動し、HTTP レベルでエンドポイントを検証する。SQL Server は不要。

### 対象クラスと検証内容

| テストクラス | 配置パス | テスト対象 | 検証内容 |
|------------|---------|-----------|---------|
| `RuleSetsControllerTests` | `Tests/Web/Controllers/RuleSetsControllerTests.cs` | `RuleSetsController` | `GET /api/rule-sets`（シードデータ存在確認）、`GET ./{id}`（200/404）、Swagger JSON |
| `RunsControllerTests` | `Tests/Web/Controllers/RunsControllerTests.cs` | `RunsController` | `GET /api/runs`（200）、`POST /api/runs` 認証なし（401）、`POST /api/runs` トークンあり（201） |

### CustomWebApplicationFactory

```csharp
// Tests/Web/TestSupport/CustomWebApplicationFactory.cs
// WebApplicationFactory<Program> を継承
// インメモリ DB + シードデータ（Gen6 RuleSet、テストユーザー認証）を設定
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public static readonly Guid Gen6RuleSetId = /* 固定 Guid */;
}
```

---

## 4. テスト配置規則

テストファイルは以下の規則に従って配置する。

```
PokemonDamageCalculatorForStory.Tests/
├── Infrastructure/
│   ├── Data/
│   │   └── DesignTimeConnectionStringResolverTests.cs   ← ユニットテスト
│   ├── Repositories/
│   │   └── UserAuthorizationInfoRepositoryTests.cs      ← 統合テスト（SQL Server）
│   └── TestSupport/
│       └── InfrastructureSqlServerTestDatabase.cs       ← 統合テスト基盤
└── Web/
    ├── Controllers/
    │   ├── RuleSetsControllerTests.cs                   ← 統合テスト（E2E）
    │   └── RunsControllerTests.cs                       ← 統合テスト（E2E）
    └── TestSupport/
        └── CustomWebApplicationFactory.cs               ← E2E テスト基盤
```

> 配置規則: `Tests/<TargetProject>/<相対パス>/<TargetClass>Tests.cs`

---

## 5. テスト実行コマンド

### 全テスト実行

```bash
# テスト用 DB を起動してから実行（Repository 統合テストが SQL Server を必要とする）
docker compose -f docker-compose.dotnet.test-db.yml up -d
dotnet test PokemonDamageCalculatorForStory.sln
```

### DB 不要のテストのみ実行

```bash
dotnet test PokemonDamageCalculatorForStory.sln --filter "Category!=Integration"
```

### ビルド確認後にテスト実行

```bash
dotnet build PokemonDamageCalculatorForStory.sln
dotnet test PokemonDamageCalculatorForStory.sln --no-build
```

---

## 6. 受け入れ基準との対応

| 受け入れ基準 | 対応するテスト |
|------------|-------------|
| AC-1: `dotnet build` 成功 | CI パイプライン / ローカルビルド確認 |
| AC-2: `dotnet test` 成功 | 全テスト実行 |
| AC-3: アーキテクチャ準拠（CQRS・Create/Restore） | `RuleSetsControllerTests`, `RunsControllerTests`（E2E でエンドポイント動作を確認） |
| AC-4: 認証フロー（401 / 認証あり 201） | `RunsControllerTests`（Create_Without_Token_Returns_401, Create_With_Token_Returns_201） |
| AC-5: UserAuthorizationInfo の Role/Permission 復元 | `UserAuthorizationInfoRepositoryTests` |
