# API リファレンス

> 対象システム: ストーリー攻略用ポケモンダメージ計算 Web API
> 関連ドキュメント: [architecture-overview.md](./architecture-overview.md) | [domain-model.md](./domain-model.md) | [cqrs-handlers.md](./cqrs-handlers.md)

---

## 概要

本ドキュメントは全 API エンドポイントの仕様を定義する。

### エンドポイント一覧

| Method | Path | 説明 | 認証 |
|--------|------|------|------|
| `GET` | `/api/rule-sets` | ルールセット一覧取得 | Anonymous |
| `GET` | `/api/rule-sets/{id}` | ルールセット詳細取得 | Anonymous |
| `GET` | `/api/runs` | Run 一覧取得 | Anonymous |
| `POST` | `/api/runs` | Run 作成 | 必須 |
| `GET` | `/api/runs/{id}` | Run 詳細取得 | Anonymous |
| `PUT` | `/api/runs/{id}` | Run 更新 | 必須 |
| `DELETE` | `/api/runs/{id}` | Run 削除 | 必須 |
| `GET` | `/api/runs/{runId}/battles` | Battle 一覧取得 | Anonymous |
| `POST` | `/api/runs/{runId}/battles` | Battle 作成 | 必須 |
| `PUT` | `/api/runs/{runId}/battles/{id}` | Battle 更新 | 必須 |
| `DELETE` | `/api/runs/{runId}/battles/{id}` | Battle 削除 | 必須 |
| `POST` | `/api/runs/{runId}/battles/{id}/calculate` | ダメージ計算実行 | 必須 |
| `GET` | `/api/runs/{runId}/party-state` | パーティ状態取得 | Anonymous |
| `POST` | `/api/runs/{runId}/party-state` | 進捗イベント追加 | 必須 |

> 計 14 エンドポイント（ルールセット 2 + Run 5 + Battle 5 + ダメージ計算 1 + パーティ状態 1）
> `[Authorize]` は Google Access Token による認証を要求する。

---

## 1. 認証

| 項目 | 内容 |
|------|------|
| 認証方式 | Google Access Token（Bearer） |
| Anonymous エンドポイント | `GET /api/rule-sets`, `GET /api/rule-sets/{id}`, `GET /api/runs`, `GET /api/runs/{id}`, `GET /api/runs/{runId}/battles`, `GET /api/runs/{runId}/party-state` |
| 認証必須エンドポイント | `POST/PUT/DELETE /api/runs`, `POST/PUT/DELETE /api/runs/{runId}/battles`, `POST .../calculate`, `POST .../party-state` |

---

## 2. ルールセット

### GET /api/rule-sets

ルールセット一覧を取得する。

- **認証**: Anonymous
- **リクエスト本文**: なし
- **レスポンス**: `200 OK` — `RuleSetDto[]`

### GET /api/rule-sets/{id}

指定 ID のルールセットを取得する。

- **認証**: Anonymous
- **パスパラメータ**: `id: Guid`
- **レスポンス**: `200 OK` — `RuleSetDto` / `404 Not Found`

---

## 3. Run

### GET /api/runs

Run 一覧を取得する（全ユーザー分）。

- **認証**: Anonymous
- **レスポンス**: `200 OK` — `RunDto[]`

### POST /api/runs

Run を新規作成する。`OwnerUserId` は認証トークンから自動設定される。

- **認証**: 必須
- **リクエスト本文**: `CreateRunRequest`
- **レスポンス**: `201 Created` — `RunDto`

### GET /api/runs/{id}

指定 ID の Run を取得する。

- **認証**: Anonymous
- **パスパラメータ**: `id: Guid`
- **レスポンス**: `200 OK` — `RunDto` / `404 Not Found`

### PUT /api/runs/{id}

Run を更新する。

- **認証**: 必須
- **パスパラメータ**: `id: Guid`
- **リクエスト本文**: `UpdateRunRequest`
- **レスポンス**: `200 OK` — `RunDto` / `404 Not Found`

### DELETE /api/runs/{id}

Run を削除する。

- **認証**: 必須
- **パスパラメータ**: `id: Guid`
- **レスポンス**: `204 No Content` / `404 Not Found`

---

## 4. Battle

### GET /api/runs/{runId}/battles

指定 Run の Battle 一覧を取得する。

- **認証**: Anonymous
- **パスパラメータ**: `runId: Guid`
- **レスポンス**: `200 OK` — `BattleDto[]`

### POST /api/runs/{runId}/battles

Battle を新規作成する。

- **認証**: 必須
- **パスパラメータ**: `runId: Guid`
- **リクエスト本文**: `CreateBattleRequest`
- **レスポンス**: `201 Created` — `BattleDto`

### PUT /api/runs/{runId}/battles/{id}

Battle を更新する。

- **認証**: 必須
- **パスパラメータ**: `runId: Guid`, `id: Guid`
- **リクエスト本文**: `UpdateBattleRequest`
- **レスポンス**: `200 OK` — `BattleDto` / `404 Not Found`

### DELETE /api/runs/{runId}/battles/{id}

Battle を削除する。

- **認証**: 必須
- **パスパラメータ**: `runId: Guid`, `id: Guid`
- **レスポンス**: `204 No Content` / `404 Not Found`

### POST /api/runs/{runId}/battles/{id}/calculate

ダメージ計算を実行し、結果を DB に保存する。

- **認証**: 必須
- **パスパラメータ**: `runId: Guid`, `id: Guid`
- **リクエスト本文**: `CalculateDamageRequest`
- **レスポンス**: `200 OK` — `CalculationResultDto`

---

## 5. パーティ状態

### GET /api/runs/{runId}/party-state

指定 Run の OwnPokemonSnapshot 一覧を取得する。

- **認証**: Anonymous
- **パスパラメータ**: `runId: Guid`
- **レスポンス**: `200 OK` — `PartyStateDto`

### POST /api/runs/{runId}/party-state

進捗イベント（OwnPokemonSnapshot）を追加する。

- **認証**: 必須
- **パスパラメータ**: `runId: Guid`
- **リクエスト本文**: `AddProgressionEventRequest`
- **レスポンス**: `200 OK` — `OwnPokemonSnapshotDto`

---

## 6. DTO 定義

### Request DTO

```csharp
record CreateRunRequest(string Name, Guid RuleSetId);
record UpdateRunRequest(string Name, string Status);

record CreateBattleRequest(string EnemyPokemon, int Sequence);
record UpdateBattleRequest(string EnemyPokemon, int Sequence);

// ダメージ計算リクエスト（フラット構造）
record CalculateDamageRequest(
    int AttackerLevel,
    int AttackStat,
    int MovePower,
    bool IsSpecialMove,     // true = 特殊技, false = 物理技
    bool HasStab,
    int DefenseStat,
    float TypeEffectiveness
);

// OwnPokemonSnapshot 登録リクエスト
record AddProgressionEventRequest(
    Guid BattleId,
    string Species,
    int Level,
    string BaseStats, // JSON 文字列（例: {"Hp":35,"Attack":55,...}）
    string IVs,       // JSON 文字列（例: {"Hp":31,"Attack":31,...}）
    string Stats,    // JSON 文字列（例: {"Hp":200,"Attack":100,...}）
    string EVs       // JSON 文字列（例: {"Hp":0,"Attack":252,...}）
);
```

### Response DTO

```csharp
record RuleSetDto(Guid Id, string Slug, int Generation, string Title, string Version, string Status, string Summary);

record RunDto(Guid Id, string OwnerUserId, Guid RuleSetId, string Name, string Status);

record BattleDto(Guid Id, Guid RunId, string EnemyPokemon, int Sequence);

// ダメージ計算結果（DB 永続化される）
record CalculationResultDto(
    Guid Id,
    Guid RunId,
    Guid BattleId,
    string AttackerParams,   // JSON 文字列（AttackerLevel, AttackStat, MovePower, IsSpecialMove, HasStab）
    string DefenderParams,   // JSON 文字列（DefenseStat, TypeEffectiveness）
    IReadOnlyList<int> DamageRolls   // 16段階 [0]=最小(85%) 〜 [15]=最大(100%)
);

record PartyStateDto(Guid RunId, IReadOnlyList<OwnPokemonSnapshotDto> Pokemon);

record OwnPokemonSnapshotDto(Guid Id, Guid BattleId, string Species, int Level, string? BaseStats, string? IVs, string Stats, string EVs);

// 既存の履歴データで種族値・個体値が未保存だった行は BaseStats / IVs が null で返る。
```

---

## 7. バリデーション

各エンドポイントのリクエストは FluentValidation によって検証される。

| Validator | 主な検証項目 |
|-----------|------------|
| `CreateRunCommandValidator` | Name 必須・200 文字以内、RuleSetId 非空 Guid、OwnerUserId 必須 |
| `UpdateRunCommandValidator` | Name 必須、Status 必須 |
| `CreateBattleCommandValidator` | EnemyPokemon 必須、Sequence ≥ 1 |
| `UpdateBattleCommandValidator` | EnemyPokemon 必須、Sequence ≥ 1 |
| `CalculateDamageCommandValidator` | RunId/BattleId 非空、AttackerLevel 1-100、AttackStat/MovePower/DefenseStat ≥ 1、TypeEffectiveness > 0 |
| `AddProgressionEventCommandValidator` | RunId/BattleId 非空（Species/Level/BaseStats/IVs/Stats/EVs の整合性は Domain で検証） |
