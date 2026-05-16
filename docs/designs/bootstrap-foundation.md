# Bootstrap Foundation Design

## 概要

このドキュメントは Issue #8 における bootstrap foundation 再設計の実装契約を定義します。framework 選択式 bootstrap、README 分離、workflow 可用性、guardrail fallback、`.NET` サンプル API の責務を、現在の実装に合わせて整理します。

| 観点 | このドキュメントで固定する内容 |
|---|---|
| Bootstrap | 生成対象の選択、残存資産、生成成功条件 |
| README | Base README と Generated README の責務、および framework 別の必須/禁止事項 |
| Workflow | `develop` / `main` / `copilot/**` 向け test → build/push → deploy 契約 |
| Guardrail | unsupported 条件でも bootstrap を失敗させない通知契約 |
| `.NET` sample | Documents API、Admin API、初回管理者 seed の設計境界 |

## Status

- Issue: #8
- Phase: 3
- Step: 3.4.5
- Document type: implementation-aligned design contract

## Scope

- framework 選択式 bootstrap の責務
- Base README と Generated README の責務分離
- AI assets の同梱方針
- guardrail best-effort / warn / no-op の扱い
- workflow availability と ACA 自動作成 toggle の契約
- `.NET` sample の Documents 系移行方針
- 管理 API、ロール階層、初回管理者 seed の概要
- dotnet8 / dotnet10 parity 方針

## Non-Goals

- Google OAuth フロー自体の再設計
- ACA 構成の全面見直し
- Next.js への認可サンプル追加

## Bootstrap and Generation

### Responsibility

- bootstrap は入力された framework を単一選択として扱う
- 生成後リポジトリには選択 framework の資産のみを残す
- workflow、VS Code tasks、README、テンプレート資産、AI assets の整合を同時に取る

### Framework Contract

| Framework | 残す主な資産 | 除去対象の代表例 |
|---|---|---|
| `next` | `templates/next/**`、Next.js 向け workflow / `.vscode` / README | `.NET` 用 `docker-compose*.yml`、EF Core 手順、管理者 seed 手順 |
| `dotnet8` | `templates/dotnet8/**`、`.NET` 向け workflow / `.vscode` / DB 関連ファイル | `templates/next/**` と Next.js 専用 workflow |
| `dotnet10` | `templates/dotnet10/**`、`.NET` 向け workflow / `.vscode` / DB 関連ファイル | `templates/next/**` と Next.js 専用 workflow |

### Implemented Behavior

- framework ごとの keep / remove / copy を bootstrap script 内の framework contract で管理する
- 選択 framework のテンプレートを repository root へ配置したあと、未選択 framework の compose / Dockerfile / task template を削除する
- `.github/workflow-templates/main.<framework>.yml` を `.github/workflows/main.yml` として昇格し、残りの reusable workflow を `.github/workflows/` へ移動する
- 生成後 repository では `scripts/bootstrap-template.ps1` と `.github/workflows/template-bootstrap.yml` を削除し、`scripts/configure-github-guardrails.ps1` は手動再実行用に残す
- 生成結果サマリに以下を含める
  - 選択 framework
  - 残した workflow / tasks / docs
  - guardrail 実行結果
  - ACA 自動作成の実施有無

## README Strategy

### Base README

- このベースリポジトリの目的、対応 framework、生成機能、運用前提のみを説明する
- generated repository 固有のセットアップ手順は持たない

### Generated README

- 生成された repository の利用開始導線を担う
- 選択 framework に応じた起動、開発、CI/CD、guardrail 状態の説明を持つ
- bootstrap script が README を framework 別に直接生成し、bootstrap workflow が required / forbidden section を検証する

### Generated README Contract by Framework

| Framework | 必須セクション | 含めてはいけない内容 |
|---|---|---|
| `next` | Dev Container 開始手順、ローカル公開ポート、確認コマンド、GitHub Actions / デプロイ、Next.js workflow 必須 secrets、Guardrails | EF Core migration、Visual Studio Infrastructure test、SQL Server Debug/Test DB、`INITIAL_ADMIN_GOOGLE_USER_ID` 管理者 seed |
| `dotnet8` / `dotnet10` | Dev Container 開始手順、ローカル公開ポート、SQL Server 接続先、EF Core Migrations、Visual Studio Infrastructure test、GitHub Actions / デプロイ、Guardrails | Next.js 専用起動手順 |

### `.NET` Detailed Documentation

- `.NET` sample の API・認可ルール・初回管理者設定・migration 手順は README に直書きしすぎず、詳細ドキュメントへ分離する
- Documents 系サンプル説明のリンク元として Generated README を使う

### Next.js Secret Contract

- bootstrap は `next` 生成時に `DEV_API_URL` / `PROD_API_URL` / `DEV_GOOGLE_REDIRECT_URI` / `PROD_GOOGLE_REDIRECT_URI` / `GOOGLE_CLIENT_ID` / `GOOGLE_CLIENT_SECRET` の placeholder secrets を生成先 repository へ設定する
- `NEXTAUTH_SECRET` は `create_aca=true` なら自動生成され、`create_aca=false` なら placeholder を設定する
- Generated README は上記 secret 名を明示し、workflow 実行前に `REPLACE_ME` を実値へ置き換える運用を案内する

## Workflow and Deployment Contract

### Branch and Stage Availability

| Framework | Available stages | Required push branches |
|---|---|---|
| `next` | test → build/push → deploy | `develop`, `main`, `copilot/**` |
| `dotnet8` | test → build/push → deploy | `develop`, `main`, `copilot/**` |
| `dotnet10` | test → build/push → deploy | `develop`, `main`, `copilot/**` |

### Implemented Workflow Contract

- generated `.github/workflows/main.yml` は全 framework で `push` の `main` / `develop` / `copilot/**` を受け付ける
- `pull_request` の `closed` も `main` / `develop` に対して定義され、merge 後の test 実行を許可する
- `test` job は `push` または merged PR で実行され、失敗時は downstream build / deploy を止める
- `build-main` / `build-develop` / `build-copilot` は `needs: test` で接続される
- `deploy-main-aca` / `deploy-develop-aca` / `deploy-copilot-aca` は対応する build job の後段にあり、`ACA_DEPLOY_ENABLED=true` と必要な app / resource group variables が揃う場合だけ動く
- reusable build workflow は `.Web/Dockerfile` を優先して解決し、`.devcontainer/Dockerfile` を deploy 対象に使わない
- bootstrap workflow は `create_aca=true` の場合に `aca_name_base` の入力を求め、23 文字以下・英小文字/数字/`-`・先頭英字・末尾英数字・`--` 禁止の条件で fail-fast 検証し、`<base>-aca-env` / `<base>-develop` / `<base>-copilot` / `<base>` を自動派生する

### ACA Toggle Contract

- `create_aca=false` の場合でも repository generation 全体は成功させる
- skip 対象は ACA 自動作成のみとし、テンプレート展開・workflow 生成・README 生成は継続する
- 生成先 repository には `ACA_DEPLOY_ENABLED=false` を設定し、deploy workflow は残したまま skip 状態にする
- 生成結果サマリに `aca_status=skipped` と `ACA auto-creation skipped because create_aca=false...` を残す
- `create_aca=true` の場合は利用者が `aca_name_base` を入力し、ACA environment / app 名はそこから自動派生する
- `aca_name_base` が未入力または制約違反の場合は、bootstrap は提案値と派生結果を表示して停止する

## AI Assets Packaging

### Goal

生成後リポジトリに AI エージェント / スキルを同梱しつつ、不要な framework 依存アセットは残さない。

- `.github/agents` / `.github/skills` は同梱対象とする
- framework 非依存アセットと framework 依存アセットを区別する
- framework 依存アセットは manifest / allowlist で残存可否を判定する

## Guardrail Behavior

### Contract

- `configure_copilot_guardrails` は best-effort とする
- unsupported plan / private repository 条件では bootstrap 全体を失敗させない
- ruleset API 非対応時は warning を出しつつ `status=skipped` / `message=<理由>` を GitHub Actions output へ返す
- workflow summary では guardrail step 未実行時に `not-run` または `not-requested` を補完して表示する

### Required User-Facing Behavior

- Generated README には manual remediation の手順を残し、実行時の未適用理由は bootstrap summary / log に必ず残す
- 未適用理由には「public repository が必要」または「GitHub Pro+ 相当の条件が必要」のどちらか、または両方を含める
- 成功条件は「guardrail 未適用でも repository generation が完了すること」とする
- Generated Repository には guardrail 再実行用の `scripts/configure-github-guardrails.ps1` を残し、条件を満たしたあとで手動適用できるようにする

## `.NET` Sample Architecture

### Domain Direction

- `WeatherForecast` 中心の説明から `Documents` 系サンプルへ寄せる
- 認可モデルは `User / Role / RoleHierarchy / EffectivePermission` を中心に再編する
- ロール階層は推移的包含、循環禁止を前提とする

### Application Direction

- Document CRUD
- User authorization-state read
- User authorization-state update
- Role CRUD
- Role hierarchy update
- Permission inspection
- Initial admin seed

### Infrastructure Direction

- EF Core / SQL Server の正規化テーブルへ移行する
- seed は migration 後の起動時に実行し、idempotent とする
- `INITIAL_ADMIN_GOOGLE_USER_ID` 未設定時は no-op を許容する

### Web Direction

- Google access token 認証は維持する
- 管理 API は `Administrator` 限定とする
- Swagger は Google Bearer 入力前提を維持する

## Documents API Migration

### Capability to Endpoint Mapping

| Capability | Endpoint | Authorization summary |
|---|---|---|
| Document 一覧参照 | `GET /api/documents` | 匿名可。公開 Document のみ返す |
| Document 詳細参照 | `GET /api/documents/{id}` | 公開 Document は匿名可。非公開は所有者または権限保持者のみ |
| Document 作成 | `POST /api/documents` | 認証必須 |
| Document 更新 | `PUT /api/documents/{id}` | 所有者または更新権限保持者 |
| Document 削除 | `DELETE /api/documents/{id}` | 所有者または削除権限保持者 |

## Admin API and Role Hierarchy

### Capability to Endpoint Mapping

| Capability | Endpoint | Notes |
|---|---|---|
| ユーザー認可状態参照 | `GET /api/admin/authorization/users/{googleUserId}` | 割当 role、継承 role、`effectivePermissions` を返す |
| ユーザー認可状態更新 | `PUT /api/admin/authorization/users/{googleUserId}/roles` | 更新対象は role set のみ。Google identity や profile 属性更新は対象外 |
| Role 一覧参照 | `GET /api/admin/authorization/roles` | Role 管理用一覧 |
| Role 作成 | `POST /api/admin/authorization/roles` | 新規 role 作成 |
| Role 更新 | `PUT /api/admin/authorization/roles/{roleName}` | permission 集合を更新 |
| Role 削除 | `DELETE /api/admin/authorization/roles/{roleName}` | 循環・参照制約を満たす場合のみ削除 |
| Role hierarchy 更新 | `PUT /api/admin/authorization/roles/{roleName}/children` | child role の再設定。循環禁止 |
| 自分の実効権限確認 | `GET /api/authorization/me/effective-permissions` | 認証済み principal の `assignedRoles` と `effectivePermissions` を返す |

- 管理 API はすべて `Administrator` 限定
- permission inspection は少なくとも `effectivePermissions` を返す
- role hierarchy は循環禁止

### Initial Administrator Seed

- `INITIAL_ADMIN_GOOGLE_USER_ID` を起点に初回管理者を補完する
- 起動順は migration → seed → request handling を基本とする
- 再実行時に重複作成しない

## dotnet8 / dotnet10 Parity

### Principle

- target framework 差分を除き、構成・API・テスト観点・README の意図を揃える

### Areas Requiring Parity

- Domain / Application / Infrastructure / Web の責務
- Documents API / Admin API の endpoint 群
- Docker Compose と DB 初期化前提
- build / test 手順
- README 導線
- workflow / bootstrap 契約

## Traceability

| Topic | Source |
|---|---|
| bootstrap 単一選択 | Architect Step 2.1 / Developer Step 2.2 |
| README 責務分離 | Architect Step 2.1 / Developer Step 2.2 |
| workflow availability / ACA toggle | Reviewer Step 2.3 / Developer Step 2.2 |
| AI assets 同梱 | Architect Step 2.1 / Developer Step 2.2 |
| guardrail best-effort | Product Manager update / Architect Step 2.1 / Developer Step 2.2 |
| Documents 系移行 | Architect Step 2.1 / Developer Step 2.2 |
| 管理 API / role hierarchy / seed | Architect Step 2.1 / Developer Step 2.2 |
| dotnet8 / dotnet10 parity | [`../ARCHITECTURE.md`](../../ARCHITECTURE.md) / Architect Step 2.1 / Developer Step 2.2 |
