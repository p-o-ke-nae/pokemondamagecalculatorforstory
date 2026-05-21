# テスト戦略

> 対象システム: ストーリー攻略用ポケモンダメージ計算 Web / 管理 UI / 管理 API
> 関連ドキュメント: [architecture-overview.md](../design/architecture-overview.md) | [domain-model.md](../design/domain-model.md)

---

## 概要

本ドキュメントは、Phase 2 で追加予定の管理機能に対するテスト観点を整理する。

### 重点観点

| 観点 | 内容 |
|------|------|
| 認可 | `Administrator` / `MasterEditor` / `Member` の許可差分 |
| Permission Catalog | catalog 値制約と role 整合性 |
| 公開非退行 | 公開 RuleSet API が `Active` のみ返すこと |
| 管理整合性 | slug 重複、参照中削除、最後の Administrator 保護 |
| 監査ログ | admin mutation の成功/拒否時に構造化アプリケーションログが残ること |
| UI 導線 | 同一 Web ホスト上の管理画面遷移とエラー表示 |

---

## 1. Domain / Application テスト

| 対象 | 主な確認内容 |
|------|--------------|
| `RuleSet` | status 値、必須項目、不変条件 |
| `UserAuthorizationInfo` | role 値、permissions 重複拒否、catalog 値、role 整合性 |
| Admin Validators | request 形式、必須項目、列挙値、catalog 外 permission 拒否 |
| Public RuleSet Query | `Active` フィルタ、不可視 RuleSet の 404 化 |
| Admin audit logging service | mutation 種別ごとの payload 生成、成功/拒否結果の記録 |

---

## 2. Infrastructure 統合テスト

| 対象 | 主な確認内容 |
|------|--------------|
| RuleSet repository | CRUD、slug 重複、参照中 RuleSet 判定 |
| UserAuthorizationInfo repository | CRUD、最後の Administrator 判定 |
| Audit log logger | admin mutation 記録の出力、target/actor/result の保持 |
| 一覧取得 | `isReferencedByRuns` / `isLastAdministrator` 相当の集計が N+1 にならない前提の結果確認 |

> 一意制約や競合系の確認は InMemory DB だけで代替しない。

---

## 3. Web 統合テスト

### 3-1. 管理 API

| シナリオ | 期待結果 |
|---------|----------|
| 匿名で `/api/admin/rule-sets` | `401` |
| `Member` で `/api/admin/rule-sets` | `403` |
| `MasterEditor` で RuleSet 管理 API | 許可 |
| `MasterEditor` で UserAuthorizationInfo 管理 API | `403` |
| `Administrator` で UserAuthorizationInfo 管理 API | 許可 |
| `Member` に `masters.rulesets.manage` を保存しようとする更新 | `400` |
| catalog 外 permission を指定 | `400` |
| slug 重複作成/更新 | `409` |
| 参照中 RuleSet 削除 | `409` |
| 最後の Administrator 変更/削除 | `409` |
| RuleSet 作成成功 | 監査ログに `RuleSetCreated` が出力される |
| RuleSet 削除が参照中で失敗 | 監査ログに `Rejected` が出力される |
| UserAuthorization 更新成功 | 監査ログに `UserAuthorizationUpdated` が出力される |

### 3-2. 公開 API 非退行

| シナリオ | 期待結果 |
|---------|----------|
| `GET /api/rule-sets` | `Active` のみ返却 |
| `GET /api/rule-sets/{id}` で `Draft` | `404` |
| `GET /api/rule-sets/{id}` で `Archived` | `404` |

---

## 4. UI / E2E 観点

| 画面 | 主な確認内容 |
|------|--------------|
| RuleSet 一覧 | 表示、クライアント側検索、編集導線 |
| RuleSet 新規/編集 | 保存成功、重複/参照中エラー表示 |
| UserAuthorization 一覧 | 表示、編集導線 |
| UserAuthorization 新規/編集 | role 更新、permissions 表示、last-admin エラー表示 |
| 画面導線 | role ごとの表示可否 |

---

## 5. テストデータ方針

- `Administrator` / `MasterEditor` / `Member` の 3 種類を固定データで用意する
- `RuleSet` は `Active` / `Draft` / `Archived` を最低 1 件ずつ用意する
- 参照中 RuleSet と未参照 RuleSet を分けて用意する
- 最後の Administrator ケースを再現できるデータを用意する

---

## 6. 受け入れ基準への対応

| 受け入れ観点 | テスト種別 |
|-------------|-----------|
| 管理 UI が同一 Web に載ること | UI / E2E |
| RuleSet 管理 CRUD | Web 統合 + Infrastructure |
| UserAuthorizationInfo 管理 CRUD | Web 統合 + Infrastructure |
| role 階層の反映 | Domain / Web 統合 |
| permission catalog の明示値制約 | Domain / Application / Web 統合 |
| admin mutation の監査証跡 | Application / Infrastructure / Web 統合 |
| 公開 RuleSet API の `Active` 制限 | Application / Web 統合 |
