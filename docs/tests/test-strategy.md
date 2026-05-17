# テスト戦略

> 対象システム: ストーリー攻略用ポケモンダメージ計算 Web API
> 関連ドキュメント: [architecture-overview.md](../design/architecture-overview.md) | [domain-model.md](../design/domain-model.md)

---

## 概要

本ドキュメントはテスト戦略とテスト対象の一覧を定義する。

### テスト構成サマリ

| 種別 | 数 | DB 接続 |
|------|-----|---------|
| ユニットテスト | 4 クラス | 不要 |
| 統合テスト | 3 クラス | SQL Server 必要 |

---

## 1. ユニットテスト

DB 接続不要。高速に実行できる。

### 対象クラスと検証内容

| テストクラス | 配置パス | テスト対象 | 検証内容 |
|------------|---------|-----------|---------|
| `DamageCalculatorTests` | `Tests/Domain/DamageCalculatorTests.cs` | `Domain/DamageCalculator.cs` | 計算式の正確性（境界値・STAB・タイプ相性・乱数16段階全値） |
| `RunTests` | `Tests/Domain/Entities/RunTests.cs` | `Domain/Entities/Run.cs` | Create バリデーション（空白名・空 Guid）、Restore、Update |
| `BattleTests` | `Tests/Domain/Entities/BattleTests.cs` | `Domain/Entities/Battle.cs` | Create バリデーション（Sequence ≥ 1・空 RunId）、Restore、Update |
| `CalculateDamageCommandHandlerTests` | `Tests/Application/UseCases/Commands/CalculateDamageCommandHandlerTests.cs` | `Application/.../CalculateDamageCommandHandler.cs` | Handler 入出力検証・DamageRolls 16件返却・MinDamage/MaxDamage 正確性 |

### DamageCalculatorTests の主なテストケース

| テストケース | 検証内容 |
|------------|---------|
| 通常ダメージ（標準パラメータ） | 計算結果の16段階すべてが正値 |
| STAB ありダメージ | `HasStab = true` 時に `HasStab = false` より大きい値 |
| タイプ相性 2 倍 | 通常の約 2 倍の値 |
| タイプ相性 0 倍 | 全16段階が 0 |
| 最小ダメージ（roll = 85） | `DamageRolls[0]` が最小値 |
| 最大ダメージ（roll = 100） | `DamageRolls[15]` が最大値 |
| 境界値（Level = 1） | 0 以上の値が返ること |

---

## 2. 統合テスト

SQL Server への接続が必要。既存 `InfrastructureSqlServerTestDatabase` パターンを継承して実装する。

### 対象クラスと検証内容

| テストクラス | 配置パス | テスト対象 | 検証内容 |
|------------|---------|-----------|---------|
| `RuleSetRepositoryTests` | `Tests/Infrastructure/Repositories/RuleSetRepositoryTests.cs` | `Infrastructure/Repositories/RuleSetRepository.cs` | GetAll・FindById・FindBySlug |
| `RunRepositoryTests` | `Tests/Infrastructure/Repositories/RunRepositoryTests.cs` | `Infrastructure/Repositories/RunRepository.cs` | CRUD・GetAllByOwnerId（所有者フィルタ）・削除後の FindById null |
| `BattleRepositoryTests` | `Tests/Infrastructure/Repositories/BattleRepositoryTests.cs` | `Infrastructure/Repositories/BattleRepository.cs` | CRUD・Value Object（`EnemyPokemonParams`）の OwnsOne 永続化と復元 |

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

## 3. テスト配置規則

テストファイルは以下の規則に従って配置する。

```
PokemonDamageCalculatorForStory.Tests/
├── Domain/
│   ├── DamageCalculatorTests.cs            ← ユニットテスト
│   └── Entities/
│       ├── RunTests.cs                     ← ユニットテスト
│       └── BattleTests.cs                  ← ユニットテスト
├── Application/
│   └── UseCases/
│       └── Commands/
│           └── CalculateDamageCommandHandlerTests.cs  ← ユニットテスト
├── Infrastructure/
│   └── Repositories/
│       ├── RuleSetRepositoryTests.cs       ← 統合テスト
│       ├── RunRepositoryTests.cs           ← 統合テスト
│       └── BattleRepositoryTests.cs        ← 統合テスト
└── TestSupport/
    └── InfrastructureSqlServerTestDatabase.cs  ← 統合テスト基盤（既存）
```

> 配置規則: `Tests/<TargetProject>/<相対パス>/<TargetClass>Tests.cs`

---

## 4. テスト実行コマンド

### 全テスト実行

```bash
# テスト用 DB を起動してから実行
docker compose -f docker-compose.dotnet.test-db.yml up -d
dotnet test PokemonDamageCalculatorForStory.sln
```

### ユニットテストのみ実行（DB 不要）

```bash
dotnet test PokemonDamageCalculatorForStory.sln --filter "Category!=Integration"
```

### 統合テストのみ実行

```bash
docker compose -f docker-compose.dotnet.test-db.yml up -d
dotnet test PokemonDamageCalculatorForStory.sln --filter "Category=Integration"
```

### ビルド確認後にテスト実行

```bash
dotnet build PokemonDamageCalculatorForStory.sln
dotnet test PokemonDamageCalculatorForStory.sln --no-build
```

---

## 5. 受け入れ基準との対応

| 受け入れ基準 | 対応するテスト |
|------------|-------------|
| AC-1: `dotnet build` 成功 | CI パイプライン / ローカルビルド確認 |
| AC-2: `dotnet test` 成功 | 全テスト実行 |
| AC-3: アーキテクチャ準拠（CQRS・Create/Restore） | `RunTests`, `BattleTests`, `CalculateDamageCommandHandlerTests` |
| AC-5: ダメージ計算式の正確性・16段階ロール | `DamageCalculatorTests`, `CalculateDamageCommandHandlerTests` |
