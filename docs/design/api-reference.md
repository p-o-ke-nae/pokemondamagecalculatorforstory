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
| `GET` | `/api/runs` | Run 一覧取得 | 必須 |
| `POST` | `/api/runs` | Run 作成 | 必須 |
| `GET` | `/api/runs/{id}` | Run 詳細取得 | 必須 |
| `PUT` | `/api/runs/{id}` | Run 更新 | 必須（所有権チェック） |
| `DELETE` | `/api/runs/{id}` | Run 削除 | 必須（所有権チェック） |
| `GET` | `/api/runs/{runId}/battles` | Battle 一覧取得 | 必須 |
| `POST` | `/api/runs/{runId}/battles` | Battle 作成 | 必須 |
| `PUT` | `/api/runs/{runId}/battles/{id}` | Battle 更新 | 必須 |
| `DELETE` | `/api/runs/{runId}/battles/{id}` | Battle 削除 | 必須 |
| `POST` | `/api/runs/{runId}/battles/{id}/calculate` | ダメージ計算実行 | Anonymous |
| `GET` | `/api/runs/{runId}/party-state` | パーティ状態取得 | 必須 |
| `POST` | `/api/runs/{runId}/party-state` | 進捗イベント追加 | 必須 |

> 計 14 エンドポイント（ルールセット 2 + Run 5 + Battle 5 + ダメージ計算 1 + パーティ状態 1）
> `[Authorize]` は Google Access Token による認証を要求する。

---

## 1. 認証

| 項目 | 内容 |
|------|------|
| 認証方式 | Google Access Token（Bearer） |
| Anonymous エンドポイント | `GET /api/rule-sets`, `GET /api/rule-sets/{id}`, `POST .../calculate` |
| 認証必須エンドポイント | 上記以外すべて |
| 所有権チェック | Run の `PUT` / `DELETE` は Handler 内で `OwnerId` を確認する |

---

## 2. ルールセット

### GET /api/rule-sets

ルールセット一覧を取得する。

- **認証**: Anonymous
- **リクエスト本文**: なし
- **レスポンス**: `200 OK` — `RuleSetResponseDto[]`

### GET /api/rule-sets/{id}

指定 ID のルールセットを取得する。

- **認証**: Anonymous
- **パスパラメータ**: `id: Guid`
- **レスポンス**: `200 OK` — `RuleSetResponseDto` / `404 Not Found`

---

## 3. Run

### GET /api/runs

認証ユーザーの Run 一覧を取得する（他ユーザーの Run は返さない）。

- **認証**: 必須
- **レスポンス**: `200 OK` — `RunResponseDto[]`

### POST /api/runs

Run を新規作成する。

- **認証**: 必須
- **リクエスト本文**: `CreateRunRequest`
- **レスポンス**: `200 OK` — `RunResponseDto`

### GET /api/runs/{id}

指定 ID の Run を取得する。

- **認証**: 必須
- **パスパラメータ**: `id: Guid`
- **レスポンス**: `200 OK` — `RunResponseDto` / `404 Not Found`

### PUT /api/runs/{id}

Run を更新する。所有者のみ操作可能。

- **認証**: 必須（所有権チェック）
- **パスパラメータ**: `id: Guid`
- **リクエスト本文**: `UpdateRunRequest`
- **レスポンス**: `200 OK` — `RunResponseDto` / `403 Forbidden` / `404 Not Found`

### DELETE /api/runs/{id}

Run を削除する。所有者のみ操作可能。

- **認証**: 必須（所有権チェック）
- **パスパラメータ**: `id: Guid`
- **レスポンス**: `204 No Content` / `403 Forbidden` / `404 Not Found`

---

## 4. Battle

### GET /api/runs/{runId}/battles

指定 Run の Battle 一覧を取得する。

- **認証**: 必須
- **パスパラメータ**: `runId: Guid`
- **レスポンス**: `200 OK` — `BattleResponseDto[]`

### POST /api/runs/{runId}/battles

Battle を新規作成する。

- **認証**: 必須
- **パスパラメータ**: `runId: Guid`
- **リクエスト本文**: `CreateBattleRequest`
- **レスポンス**: `200 OK` — `BattleResponseDto`

### PUT /api/runs/{runId}/battles/{id}

Battle を更新する。

- **認証**: 必須
- **パスパラメータ**: `runId: Guid`, `id: Guid`
- **リクエスト本文**: `UpdateBattleRequest`
- **レスポンス**: `200 OK` — `BattleResponseDto` / `404 Not Found`

### DELETE /api/runs/{runId}/battles/{id}

Battle を削除する。

- **認証**: 必須
- **パスパラメータ**: `runId: Guid`, `id: Guid`
- **レスポンス**: `204 No Content` / `404 Not Found`

### POST /api/runs/{runId}/battles/{id}/calculate

ダメージ計算を実行する。**DB への書き込みは行わない**（ステートレス計算）。

- **認証**: Anonymous
- **パスパラメータ**: `runId: Guid`, `id: Guid`
- **リクエスト本文**: `CalculateDamageRequest`
- **レスポンス**: `200 OK` — `CalculateDamageResponseDto`

---

## 5. パーティ状態

### GET /api/runs/{runId}/party-state

指定 Run のパーティ状態（OwnPokemonSnapshot 一覧）を取得する。

- **認証**: 必須
- **パスパラメータ**: `runId: Guid`
- **レスポンス**: `200 OK` — `PartyStateResponseDto`

### POST /api/runs/{runId}/party-state

進捗イベントを追加する。

- **認証**: 必須
- **パスパラメータ**: `runId: Guid`
- **リクエスト本文**: `AddProgressionEventRequest`
- **レスポンス**: `200 OK` — `PartyStateResponseDto`

---

## 6. DTO 定義

### Request DTO

```csharp
record CreateRunRequest(string Name, Guid RuleSetId);
record UpdateRunRequest(string Name, string Status);

record CreateBattleRequest(int Sequence, EnemyPokemonParamsDto EnemyPokemon);
record UpdateBattleRequest(int Sequence, EnemyPokemonParamsDto EnemyPokemon);

record CalculateDamageRequest(AttackerParamsDto Attacker, DefenderParamsDto Defender);

record AddProgressionEventRequest(string PokemonId, string EventType, object? EventData);
```

### 共有 DTO

```csharp
record EnemyPokemonParamsDto(
    string Species,
    int Level,
    int Hp,
    int Attack,
    int Defense,
    int SpAtk,
    int SpDef,
    int Speed,
    string Type1,
    string? Type2
);

record AttackerParamsDto(
    int Level,
    int Attack,
    int MovePower,
    string MoveCategory,   // "Physical" or "Special"
    bool HasStab,
    decimal TypeEffectiveness
);

record DefenderParamsDto(int Defense);
```

### Response DTO

```csharp
record RuleSetResponseDto(Guid Id, string Slug, int Generation, string Title, string Version, string Status);

record RunResponseDto(Guid Id, string OwnerId, Guid RuleSetId, string Name, string Status);

record BattleResponseDto(Guid Id, Guid RunId, int Sequence, EnemyPokemonParamsDto EnemyPokemon);

record CalculateDamageResponseDto(
    AttackerParamsDto AttackerParams,
    DefenderParamsDto DefenderParams,
    int[] DamageRolls,    // 16段階 [0]=最小(85/100) 〜 [15]=最大(100/100)
    int MinDamage,
    int MaxDamage
);

record PartyStateResponseDto(Guid RunId, OwnPokemonSnapshotDto[] Snapshots);
```

---

## 7. バリデーション

各エンドポイントのリクエストは FluentValidation によって検証される。

| Validator | 主な検証項目 |
|-----------|------------|
| `CreateRunCommandValidator` | Name 必須・256 文字以内、RuleSetId 非空 Guid |
| `UpdateRunCommandValidator` | Name 必須、Status 有効値（Active / Completed / Archived） |
| `CreateBattleCommandValidator` | Sequence ≥ 1、EnemyPokemon 各値必須・範囲チェック |
| `UpdateBattleCommandValidator` | 同上 |
| `CalculateDamageCommandValidator` | Level 1-100、Attack/Defense ≥ 1、MovePower ≥ 1、MoveCategory 有効値、TypeEffectiveness 有効倍率 |
| `AddProgressionEventCommandValidator` | PokemonId 必須、EventType 有効値 |
