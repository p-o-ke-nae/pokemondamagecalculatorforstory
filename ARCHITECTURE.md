# Pokenae Base Solution - ヘキサゴナル & DDD アーキテクチャガイド

## 概要

PokenaeBaseSolution は、**ヘキサゴナル アーキテクチャ（ポーツ & アダプタ）** と **ドメイン駆動設計（DDD）** を採用した、スケーラブルで保守性の高い .NET テンプレートソリューションです。

このドキュメントは、本テンプレートの設計思想、プロジェクト構成、依存関係、および新規機能追加時のガイドラインを定義します。

---

## 1. アーキテクチャの3層構成

### 1.1 全体図

```
┌─────────────────────────────────────────────────────────┐
│                      WebApi 層                           │
│   (PokenaeTemplate.Web)                                 │
│   - Controllers (HTTP エンドポイント)                   │
│   - Dependency Injection (DI) 設定                      │
│   - Program.cs (アプリケーション構成)                  │
└────────────────┬────────────────────────────────────────┘
                 │ MediatR / IMediator
          ▼─────────────────────────┐
┌──────────────────────────────────────────────────────┐
│         Application 層 (Use Case)                     │
│   (PokenaeTemplate.Application)                      │
│   - Commands                   (変更操作)             │
│   - Queries                    (読み取り操作)         │
│   - Command/Query    Handlers  (オーケストレーション)│
│   - DTOs                       (データ転送オブジェクト)|
│   - Validators                 (ビジネスルール検証)    │
│   - Mappers                    (Entity ↔ DTO 変換)   │
└────────────┬──────────────────────┬───────────────────┘
             │ Port (Interface)      │ Port (Interface)
             │                       │
        ▼────┴───────┐           ▼──┴────────────┐
┌──────────────────────────────────────────────┐
│    Infrastructure 層                         │
│    (PokenaeTemplate.Infrastructure)         │
│    - DbContext (EFCore)                     │
│    - Repositories (Port 実装)                │
│    - Mappers (DB Model ↔ Entity 変換)       │
│    - Migrations (スキーマ変更)               │
└──────────────────────────────────────────────┘
             │ Dependency Inversion
             │ (Domain Port = Interface)
            ▼
┌──────────────────────────────────────────────┐
│          Domain 層 (ビジネスロジック)         │
│    (PokenaeTemplate.Domain)                 │
│ ✓ フレームワーク非依存                      │
│ ✓ エンティティ (ID, ビジネスルール)          │
│ ✓ Port Interface (Repository など)         │
│ ✓ Domain Exception (ビジネス例外)           │
│ ✓ Value Objects (拡張予定)                 │
│ ✓ Aggregates (拡張予定)                    │
│ ✓ Domain Events (拡張予定)                 │
└──────────────────────────────────────────────┘
```

### 1.2 依存関係の明確性

- **依存方向**: WebApi → Application → Infrastructure ⟶ Domain
- **Domain 層**: 外部フレームワークに依存しない（完全に独立）
- **Application 層**: Domain に依存（ビジネスロジック使用）
- **Infrastructure 層**: Domain と Application に依存（実装提供）
- **WebApi 層**: すべてに依存（コントローラー & DI 設定）

---

## 2. 各層の責務とファイル構成

### 2.1 Domain 層（ビジネスロジック）

**責務:**

- エンティティの定義と不変性保証
- ビジネスルールの実装（Create メソッド内）
- Port インターフェースの定義
- Domain Exception の定義

**ファイル構成:**

```
PokenaeTemplate.Domain/
├── Entities/
│   └── WeatherForecast.cs         ← Domain Entity (ID 有、ビジネスルール)
├── Ports/
│   └── IWeatherForecastRepository.cs  ← データアクセス抽象化
├── Exceptions/
│   └── DomainException.cs         ← ドメイン固有の例外
└── PokenaeTemplate.Domain.csproj
```

**重要な設計原則:**

- Entity の Create() メソッドでビジネスルール検証を実施
- Restore() メソッドで DB から復元
- すべてのプロパティは private set（不変性）
- ビジネス例外は DomainException から継承

---

### 2.2 Application 層（Use Case）

**責務:**

- CQRS パターンによる Command/Query 分離
- MediatR を使用した疎結合なハンドラー実装
- DTO による外部インターフェース定義
- ビジネスロジックのオーケストレーション
- バリデーション

**ファイル構成:**

```
PokenaeTemplate.Application/
├── UseCases/
│   ├── Commands/
│   │   ├── CreateWeatherForecastCommand.cs        ← コマンド定義
│   │   └── CreateWeatherForecastCommandHandler.cs ← ハンドラー実装
│   └── Queries/
│       ├── GetAllWeatherForecastsQuery.cs
│       ├── GetAllWeatherForecastsQueryHandler.cs
│       ├── GetWeatherForecastByIdQuery.cs
│       └── GetWeatherForecastByIdQueryHandler.cs
├── DTOs/
│   ├── CreateWeatherForecastRequest.cs  ← 入力 (API リクエスト)
│   └── WeatherForecastResponseDto.cs    ← 出力 (API レスポンス, DB 鏡合わせ)
├── Validators/
│   └── CreateWeatherForecastCommandValidator.cs   ← FluentValidation
├── Mappers/
│   └── WeatherForecastMapper.cs  ← Entity ↔ DTO 拡張メソッド
└── PokenaeTemplate.Application.csproj
```

**重要な設計原則:**

#### CQRS パターン

- **Command**: データ変更操作（Create, Update, Delete）
  - `IRequest<T>` を実装
  - Handler は `HandleAsync(command, cancellationToken)` で実装

- **Query**: データ読み取り操作（Get, Search）
  - `IRequest<T>` を実装
  - 副作用なし（idempotent）

#### DTO 分離

```
CreateWeatherForecastRequest                (API入力)
  ↓
Domain.Entities.WeatherForecast.Create()   (Create パターン)
  ↓
Repository.SaveAsync()                      (永続化)
  ↓
WeatherForecastResponseDto                  (API出力 = DB鏡合わせ)
```

---

### 2.3 Infrastructure 層（実装詳細）

**責務:**

- EFCore DbContext と Migrations
- Repository パターンによる Port 実装
- DB Model の定義
- Entity ↔ DB Model マッピング
- 外部サービス統合（将来）

**ファイル構成:**

```
PokenaeTemplate.Infrastructure/
├── Data/
│   ├── AppDbContext.cs               ← EFCore DbContext
│   └── Models/
│       └── PersistedWeatherForecast.cs  ← EFCore Entity (DB テーブル)
├── Repositories/
│   └── WeatherForecastRepository.cs  ← IWeatherForecastRepository 実装
├── Mappers/
│   └── PersistedWeatherForecastMapper.cs ← DB Model ↔ Entity 拡張メソッド
└── PokenaeTemplate.Infrastructure.csproj
```

**重要な設計原則:**

#### Entity vs Model の分離

```
Domain Entity (WeatherForecast)
  - ビジネスロジック保有
  - Repository 経由アクセス

DB Model (PersistedWeatherForecast)
  - EFCore DbSet 対応
  - テーブル構造反映

API DTO (WeatherForecastResponseDto)
  - HTTP レスポンス
  - DB 構造と同等
```

#### マッピング関数（拡張メソッド）

```csharp
// Infrastructure層 Mapper
public static WeatherForecast ToDomainEntity(this PersistedWeatherForecast persisted)
{
    return WeatherForecast.Restore(
        persisted.Id, persisted.Date, persisted.TemperatureC, persisted.Summary
    );
}

// Application層 Mapper
public static WeatherForecastResponseDto ToWeatherForecastResponseDto(this WeatherForecast entity)
{
    return new WeatherForecastResponseDto { ... };
}
```

---

### 2.4 WebApi 層（インターフェース）

**責務:**

- HTTP エンドポイント提供
- リクエスト/レスポンス処理
- Dependency Injection (DI) 設定
- 認証・認可ミドルウェア設定

**ファイル構成:**

```
PokenaeTemplate.Web/
├── Controllers/
│   └── WeatherForecastController.cs  ← MediatR を使用して Command/Query 実行
├── Program.cs                        ← DI 設定およびミドルウェア構成
├── appsettings.json                  ← 接続文字列、認証設定
└── PokenaeTemplate.Web.csproj         ← プロジェクト依存関係
```

**重要な設計原則:**

#### Controller の責務（最小化）

```csharp
[ApiController]
public class WeatherForecastController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateWeatherForecastRequest request)
    {
        // 1. Request → Command 変換
        var command = new CreateWeatherForecastCommand { ... };

        // 2. MediatR を通して Handler 実行
        var result = await mediator.Send(command);

        // 3. Result → HTTP Response 変換
        return result.Success ? CreatedAtAction(...) : BadRequest(...);
    }
}
```

#### Dependency Injection 設定

```csharp
// Program.cs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(... Application層の アセンブリ));

builder.Services.AddScoped<IWeatherForecastRepository, WeatherForecastRepository>();
```

---

## 3. 新規機能追加ガイドライン

新しい業務ドメイン（例: `UserManagement`）を追加する場合のチェックリスト

### 3.1 Domain 層

- [ ] **Entity 定義** (`Domain/Entities/User.cs`)
  - [ ] ID プロパティ（private set）
  - [ ] Create() メソッド（ビジネスルール検証）
  - [ ] Restore() メソッド（DB 復元）
  - [ ] ビジネスメソッド実装

- [ ] **Port インターフェース** (`Domain/Ports/IUserRepository.cs`)
  - [ ] FindBy…() メソッド
  - [ ] SaveAsync() メソッド
  - [ ] DeleteAsync() メソッド

- [ ] **Domain Exception** (`Domain/Exceptions/User…Exception.cs`)
  - [ ] 業務ドメイン固有の例外定義

### 3.2 Application 層

- [ ] **DTO 定義** (`Application/DTOs/`)
  - [ ] CreateUserRequest.cs (入力)
  - [ ] UserResponseDto.cs (出力)

- [ ] **Command 定義** (`Application/UseCases/Commands/`)
  - [ ] CreateUserCommand.cs
  - [ ] CreateUserCommandHandler.cs

- [ ] **Query 定義** (`Application/UseCases/Queries/`)
  - [ ] GetAllUsersQuery.cs + Handler
  - [ ] GetUserByIdQuery.cs + Handler

- [ ] **Validator 定義** (`Application/Validators/`)
  - [ ] CreateUserCommandValidator.cs

- [ ] **Mapper** (`Application/Mappers/`)
  - [ ] UserMapper.cs に拡張メソッド追加

### 3.3 Infrastructure 層

- [ ] **DB Model** (`Infrastructure/Data/Models/PersistedUser.cs`)
  - [ ] EFCore Entity 属性設定

- [ ] **DbContext 設定** (`Infrastructure/Data/AppDbContext.cs`)
  - [ ] DbSet<PersistedUser> 追加
  - [ ] OnModelCreating() に Entity 設定追加

- [ ] **Repository 実装** (`Infrastructure/Repositories/UserRepository.cs`)
  - [ ] IUserRepository 実装

- [ ] **Mapper** (`Infrastructure/Mappers/`)
  - [ ] PersistedUserMapper.cs で DB Model ↔ Entity マッピング

### 3.4 WebApi 層

- [ ] **Controller** (`Controllers/UserController.cs`)
  - [ ] GET /user (全件取得)
  - [ ] GET /user/{id} (ID 検索)
  - [ ] POST /user (作成)
  - [ ] PUT /user/{id} (更新) ※ 必要に応じて
  - [ ] DELETE /user/{id} (削除) ※ 必要に応じて

- [ ] **DI 設定** (`Program.cs`)
  ```csharp
  builder.Services.AddScoped<IUserRepository, UserRepository>();
  ```

### 3.5 テスト（推奨）

- Tests プロジェクトは対象プロジェクトごとにフォルダを分け、対象クラスが明確なテストは本体と同じ相対パスを再現します。
- 配置ルールは `PokenaeTemplate.Tests/<TargetProject>/<相対パス>/<TargetClass>Tests.cs` を基本とし、共通補助クラスは `PokenaeTemplate.Tests/<TargetProject>/TestSupport/` に置きます。

```text
PokenaeTemplate.Tests/
├── Infrastructure/
│   ├── Data/
│   │   └── DesignTimeConnectionStringResolverTests.cs
│   ├── Repositories/
│   │   ├── WeatherForecastRepositoryTests.cs
│   │   └── UserAuthorizationInfoRepositoryTests.cs
│   └── TestSupport/
│       └── InfrastructureSqlServerTestDatabase.cs
└── Web/
    ├── Controllers/
    │   └── WeatherForecastControllerAuthorizationTests.cs
    └── TestSupport/
        └── CustomWebApplicationFactory.cs
```

- [ ] **Unit Tests** (Application層)
  - [ ] CreateUserCommandHandler のテスト
  - [ ] GetUserByIdQueryHandler のテスト

- [ ] **Integration Tests**
  - [ ] UserRepository + DbContext のテスト

- [ ] **E2E Tests** (Swagger UI / Postman)
  - [ ] API エンドポイント動作確認

---

## 4. 主要パターンと実装例

### 4.1 Create Use Case フロー

```
HTTP Request (POST /weather-forecast)
    ↓
WeatherForecastController.Create()
    ↓
CreateWeatherForecastCommand 生成
    ↓
MediatR.Send(command)
    ↓
CreateWeatherForecastCommandHandler.Handle()
    ├─ Validator で入力検証
    ├─ WeatherForecast.Create() 呼び出し
    │   └─ ビジネスルール検証（気温の範囲チェック等）
    ├─ Repository.SaveAsync(entity)
    │   ├─ Entity → PersistedModel 変換
    │   ├─ EFCore.SaveChangesAsync()
    │   └─ PersistedModel → Entity 復元
    └─ Entity → ResponseDto 変換
    ↓
HTTP Response (201 Created)
```

### 4.2 Query Use Case フロー

```
HTTP Request (GET /weather-forecast/123)
    ↓
WeatherForecastController.GetById()
    ↓
GetWeatherForecastByIdQuery 生成
    ↓
MediatR.Send(query)
    ↓
GetWeatherForecastByIdQueryHandler.Handle()
    ├─ Repository.FindByIdAsync()
    │   ├─ EFCore.FirstOrDefaultAsync()
    │   └─ PersistedModel → Entity 変換
    ├─ Entity が null → WeatherForecastNotFoundException
    └─ Entity → ResponseDto 変換
    ↓
HTTP Response (200 OK + JSON)
```

### 4.3 ビジネスルール検証の位置

| 検証位置                        | 責務               | 例                                |
| ------------------------------- | ------------------ | --------------------------------- |
| **DTO Validator** (Application) | 形式的検証         | 範囲外の気温値を拒否              |
| **Domain Entity**               | ビジネスルール検証 | 気温が -50℃～60℃ の範囲内チェック |
| **Repository**                  | ハードな制約       | DB ユニーク制約（試験用）         |

---

## 5. スコープ外（将来の拡張）

### 5.1 Domain Events（イベント駆動）

```csharp
// 将来: WeatherForecast Entity で発行
public partial class WeatherForecast
{
    private List<IDomainEvent> _events = new();

    public IReadOnlyCollection<IDomainEvent> Events => _events.AsReadOnly();

    public static WeatherForecast Create(...)
    {
        var entity = new WeatherForecast { ... };
        entity._events.Add(new WeatherForecastCreatedEvent(entity.Id));
        return entity;
    }
}
```

### 5.2 Specification パターン（複雑なクエリ）

```csharp
// 将来: 複雑な条件検索用
public interface ISpecification<T>
{
    IQueryable<T> Apply(IQueryable<T> query);
}

public class GetHotWeatherForecastsSpecification : ISpecification<WeatherForecast>
{
    public IQueryable<WeatherForecast> Apply(IQueryable<WeatherForecast> query)
    {
        return query.Where(w => w.TemperatureC > 30);
    }
}
```

### 5.3 Value Objects（値オブジェクト）

```csharp
// 将来:プリミティブ型を型安全にラップ
public record Temperature(int Celsius)
{
    public int Fahrenheit => 32 + (int)(Celsius / 0.5556);

    public static Temperature Create(int celsius)
    {
        if (celsius < -50 || celsius > 60)
            throw new ArgumentException("Invalid temperature");
        return new Temperature(celsius);
    }
}
```

### 5.4 MediatR Validation Pipeline

```csharp
// 将来: FluentValidation の自動バリデーション
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, ..., next)
    {
        var validator = /* IValidator<TRequest> */;
        var result = await validator.ValidateAsync(request);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
        return await next();
    }
}
```

---

## 6. ベストプラクティス

### 6.1 Entity 設計

✅ 推奨:

```csharp
public class WeatherForecast
{
    public int Id { get; private set; }  // private set でオブジェクト指向

    public static WeatherForecast Create(DateOnly date, int tempC, string? summary)
    {
        // ビジネスロジック
        return new WeatherForecast { ... };
    }
}
```

❌ 非推奨:

```csharp
public class WeatherForecast
{
    public int Id { get; set; }  // public set は不変性破壊
    public WeatherForecast() { }  // パラメータレスコンストラクタ
}
```

### 6.2 Repository 使用

✅ 推奨:

```csharp
var entity = WeatherForecast.Create(date, tempC, summary);
var saved = await repository.SaveAsync(entity);  // Entity で操作
```

❌ 非推奨:

```csharp
var dto = new WeatherForecastDto { ... };  // DTO で DB 操作
await repository.SaveAsync(dto);
```

### 6.3 DTO 設計

✅ 推奨:

```csharp
public class WeatherForecastResponseDto
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public int TemperatureF { get; set; }
    public string? Summary { get; set; }
}
```

❌ 非推奨（DB テーブル構造をそのまま公開）:

```csharp
public class WeatherForecastResponseDto
{
    // 内部的な情報も含む
    public int InternalId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedByUserId { get; set; }
}
```

### 6.4 例外処理

✅ 推奨（Domain Exception をキャッチ）:

```csharp
try
{
    var entity = WeatherForecast.Create(tempC, ...);
}
catch (WeatherForecastNotFoundException ex)
{
    return new Response { Success = false, Message = ex.Message };
}
```

❌ 非推奨（汎用 Exception）:

```csharp
try { ... }
catch (Exception ex)
{
    logger.Log(ex);
}
```

---

## 7. Docker デプロイ

### 7.1 マルチステージビルド

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Build stage

FROM mcr.microsoft.com/dotnet/aspnet:8.0
# Runtime stage
```

### 7.2 環境別設定

```
appsettings.json                (全環境共通)
appsettings.Development.json     (local dev)
appsettings.Production.json      (production)
```

**Docker Compose 実行:**

```bash
# Development
docker compose -f docker-compose.dotnet8.yml -f docker-compose.dotnet8.dev.yml up

# Production
docker compose -f docker-compose.dotnet8.yml -f docker-compose.dotnet8.prod.yml up --no-build
```

---

## 8. トラブルシューティング

| 問題                                          | 原因                    | 解決方法                                                            |
| --------------------------------------------- | ----------------------- | ------------------------------------------------------------------- |
| "AppDbContext が見つかりません"               | using 足りない          | `using PokenaeTemplate.Infrastructure.Data;` 追加                   |
| "IWeatherForecastRepository がない"           | DI 未登録               | `Program.cs` に `AddScoped<IWeatherForecastRepository, ...>()` 追加 |
| "ToWeatherForecastResponseDto が見つからない" | Mapper using や実装なし | Mapper クラスを作成・using 確認                                     |
| Migration エラー                              | EFCore ファイルシステム | `dotnet ef database update` 実行                                    |

---

## 9. 参考資料

- [ヘキサゴナル アーキテクチャ (Ports & Adapters)](https://alistair.cockburn.us/hexagonal-architecture/)
- [Clean Architecture - Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design - Evans](https://www.domainlanguage.com/ddd/)
- [CQRS Pattern - Microsoft](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [MediatR GitHub](https://github.com/jbogard/MediatR)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)

---

**ドキュメント作成日**: 2026年3月21日  
**対応テンプレート**: dotnet8, dotnet10  
**ステータス**: 本番運用対応
