# テスト戦略

> 対象システム: ストーリー攻略用ポケモンダメージ計算 Web API / 管理 API
> 関連ドキュメント: [architecture-overview.md](../design/architecture-overview.md) | [domain-model.md](../design/domain-model.md)

---

## 概要

本ドキュメントは、実装済みの管理マスタ保守機能に対するテスト観点を整理する。

### 重点観点

| 観点 | 内容 |
|------|------|
| 認可 | `Administrator` / `MasterEditor` / `Member` の許可差分 |
| 認証 | bearer 管理 API の認証/認可 |
| Permission Catalog | catalog 値制約と重複拒否 |
| 公開非退行 | 公開 RuleSet API が `Active` のみ返すこと |
| 管理整合性 | slug 重複、参照中削除、最後の Administrator 保護 |
| 監査ログ | admin mutation の成功/拒否時に監査記録が残ること |
| API 契約 | 管理 API の応答コード、競合時の `ProblemDetails`、公開 API 非退行 |

---

## 1. Domain / Application テスト

| 対象 | 主な確認内容 |
|------|--------------|
| `RuleSet` | status 値、必須項目、長さ制約 |
| `UserAuthorizationInfo` | GoogleUserId 必須、role 必須、permissions 重複拒否 |
| Admin Validators | request 形式、必須項目、列挙値、catalog 外 permission 拒否 |
| Public RuleSet Query | `Active` フィルタ、不可視 RuleSet の 404 化 |
| Admin command handlers | slug 重複、最後の Administrator 保護、監査 logger 呼び出し |

---

## 2. Infrastructure 統合テスト

| 対象 | 主な確認内容 |
|------|--------------|
| RuleSet repository | CRUD、slug 重複確認、参照中 RuleSet 判定 |
| UserAuthorizationInfo repository | CRUD、permissions 復元、管理者件数集計 |
| Audit logger | `AdminAudit` 構造化ログ出力 |
| 一覧取得 | `isReferencedByRuns` / `isLastAdministrator` 計算に必要な集計結果 |

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
| catalog 外 permission を指定 | `400` |
| duplicate permission を指定 | `400` |
| slug 重複作成/更新 | `409` |
| 参照中 RuleSet 削除 | `409` |
| 最後の Administrator の role 変更/削除 | `409` |
| 最後の Administrator の permissions-only 更新 | `200` |
| RuleSet 作成成功 | 監査ログに `Succeeded` が記録される |
| RuleSet 削除が参照中で失敗 | 監査ログに `Rejected` が記録される |
| UserAuthorization 更新成功 | 監査ログに `Succeeded` が記録される |

### 3-2. 公開 API 非退行

| シナリオ | 期待結果 |
|---------|----------|
| `GET /api/rule-sets` | `Active` のみ返却 |
| `GET /api/rule-sets/{id}` で `Draft` | `404` |
| `GET /api/rule-sets/{id}` で `Archived` | `404` |

## 4. API 観点

| 対象 | 主な確認内容 |
|------|--------------|
| RuleSet 管理 API | 一覧、詳細、作成、更新、削除、`409` 系競合、監査ログ |
| UserAuthorization 管理 API | 一覧、詳細、作成、更新、削除、最後の Administrator 保護、監査ログ |
| 公開 RuleSet API | `Active` のみ返却、非公開状態の `404` |
| エラー契約 | `400` / `401` / `403` / `404` / `409` の `ProblemDetails` 応答 |

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
| RuleSet 管理 CRUD | Web 統合 + Infrastructure |
| UserAuthorizationInfo 管理 CRUD | Web 統合 + Infrastructure |
| role 階層の反映 | Web 統合 |
| permission catalog の明示値制約 | Domain / Application / Web 統合 |
| admin mutation の監査証跡 | Application / Web 統合 |
| 公開 RuleSet API の `Active` 制限 | Application / Web 統合 |
