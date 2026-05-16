# Bootstrap Foundation Test Plan

## 概要

このドキュメントは Issue #8 の受け入れ基準を、bootstrap・README・workflow・guardrail・`.NET` sample の検証シナリオへ対応付けるためのテスト計画です。特に Step 2.3 レビューで指摘された AC-007 / AC-018 / AC-019 / AC-023 は、Phase 2 の時点で検証観点と期待証跡を固定します。

| 項目 | 内容 |
|---|---|
| 対象 Issue | #8 |
| 主目的 | 受け入れ基準と検証シナリオの明示的なトレーサビリティ |
| 主な成果物 | 受け入れ基準マッピング、重点シナリオ、期待証跡 |
| 関連設計 | [Bootstrap Foundation Design](../designs/bootstrap-foundation.md) |

## Status

- Issue: #8
- Phase: 3
- Step: 3.4.5
- Detail level: implementation-aligned verification contract

## Test Areas

| Area | Objective |
|---|---|
| Bootstrap selection | 選択 framework のみが残ることを確認する |
| README contract | Base README と Generated README の責務分離を確認する |
| Workflow availability | framework ごとの branch trigger と stage availability を確認する |
| ACA toggle | `create_aca=false` でも生成成功し、ACA 自動作成のみ skip されることを確認する |
| ACA naming | `create_aca=true` では `aca_name_base` から ACA 名が自動派生され、Azure 制約違反を bootstrap / deploy 前に検知できることを確認する |
| AI assets packaging | 必須 AI assets が残り、不要 framework 資産が残らないことを確認する |
| Guardrail fallback | best-effort / warn / no-op 契約と利用者通知を確認する |
| Documents API | 匿名可 / 認証必須 / 権限必須の境界を確認する |
| Admin API | `Administrator` 限定、role hierarchy、permission inspection を確認する |
| Initial admin seed | 起動時 seed と idempotency を確認する |
| dotnet parity | dotnet8 / dotnet10 の意図差分がないことを確認する |

## Acceptance Criteria Traceability

| AC | Requirement summary | Verification scenario IDs | Expected evidence |
|---|---|---|---|
| AC-007 | `next` Generated README に `.NET` 専用セクションを含めない | `BF-AC-007` | 生成された `README.md` に EF Core migration / SQL Server Debug/Test DB / `INITIAL_ADMIN_GOOGLE_USER_ID` が存在しない |
| AC-018 | `create_aca=false` でも repository generation が成功し、ACA 自動作成のみ skip される | `BF-AC-018` | bootstrap 成功、生成物出力完了、`ACA_DEPLOY_ENABLED=false`、ACA skip メッセージ記録 |
| AC-019 | `develop` / `main` / `copilot/**` push に対し framework 別の test → build/push → deploy workflow が利用可能 | `BF-AC-019A`, `BF-AC-019B`, `BF-AC-019C` | generated `main.yml` の branch trigger / job 定義、`needs` 接続、framework 別選択結果、`next` 用 secret 契約 |
| AC-023 | guardrails 未対応時に Generated README または bootstrap logs へ未適用理由と前提条件を明示する | `BF-AC-023` | bootstrap summary / log に guardrail status と public repo / GitHub Pro+ 条件が記録され、README に手動再実行手順がある |

## Priority Verification Scenarios

### BF-AC-007: `next` Generated README Exclusion

- **Purpose**: `next` 選択時の Generated README が framework 専用責務に限定されることを確認する
- **Input**: `framework=next`
- **Checks**
  - Generated `README.md` に Next.js 用セットアップと workflow 概要がある
  - 以下の文字列または同等の手順が存在しない
    - EF Core migration
    - SQL Server Debug/Test DB
    - `INITIAL_ADMIN_GOOGLE_USER_ID`
- **Evidence**
  - 生成後 `README.md`
  - bootstrap の残存ファイル一覧

### BF-AC-018: `create_aca=false` Skip Path

- **Purpose**: ACA 自動作成の opt-out が bootstrap 全体の失敗条件にならないことを確認する
- **Input**: `create_aca=false` と任意の有効 framework
- **Checks**
  - repository generation が成功終了する
  - README / workflow / 選択 framework のテンプレート資産は生成される
  - ACA 自動作成だけが skip として記録される
  - 生成先 repository に `ACA_DEPLOY_ENABLED=false` が設定される
  - deploy job は削除されず、後から有効化できる
- **Evidence**
  - bootstrap 実行結果
  - 生成結果サマリの `ACA status`
  - generated repository variable 設定結果

### BF-ACA-Naming: ACA Name Prompt and Validation

- **Purpose**: `aca_name_base` の未入力や制約違反を bootstrap / deploy の早い段階で検知し、Azure CLI 実行まで進めないことを確認する
- **Input**
  - `create_aca=true` かつ `aca_name_base` 未入力
  - 23 文字超過、先頭数字、末尾 `-`、`--` を含む `aca_name_base`
  - 制約を満たす有効な `aca_name_base`
- **Checks**
  - 未入力時は bootstrap が提案する `aca_name_base` と派生名付きで fail-fast する
  - 無効な `aca_name_base` は bootstrap の name validation で reject される
  - 有効な `aca_name_base` から `ACA_APP_NAME_DEV` / `ACA_APP_NAME_COPILOT` / `ACA_APP_NAME_PROD` と ACA environment 名が自動生成される
  - 生成された ACA 名は bootstrap summary と sync 先 variable に反映される
  - generated deploy workflow は `ACA_APP_NAME_*` が後から不正値に変更されても Azure CLI 実行前に reject する
- **Evidence**
  - bootstrap workflow の `Compute slug and derived ACA names` step のログ
  - 生成結果サマリの ACA 名出力
  - `.github/workflows/reusable-deploy-azure-container-apps.yml`

### BF-AC-019A: Workflow Trigger Coverage

- **Purpose**: framework ごとの workflow が必要 branch に紐づくことを確認する
- **Checks**
  - `develop` push trigger が定義される
  - `main` push trigger が定義される
  - `copilot/**` push trigger が定義される
- **Evidence**
  - `.github/workflow-templates/**` または生成後 workflow 定義

### BF-AC-019B: Workflow Stage Availability

- **Purpose**: 利用可能 workflow が test → build/push → deploy の流れを提供することを確認する
- **Checks**
  - framework ごとに `test` job がある
  - `build-main` / `build-develop` / `build-copilot` が `needs: test` で接続される
  - `deploy-main-aca` / `deploy-develop-aca` / `deploy-copilot-aca` が対応する build job の後に接続される
  - test job は失敗を無視しない
- **Evidence**
  - generated `.github/workflows/main.yml`
  - framework 別生成結果

### BF-AC-019C: Next.js Secret Contract

- **Purpose**: `next` Generated Repository の workflow が要求する secret 名と bootstrap / README の案内が一致することを確認する
- **Checks**
  - bootstrap が `DEV_API_URL` / `PROD_API_URL` / `DEV_GOOGLE_REDIRECT_URI` / `PROD_GOOGLE_REDIRECT_URI` / `GOOGLE_CLIENT_ID` / `GOOGLE_CLIENT_SECRET` を生成先 repository に設定する
  - `create_aca=false` でも上記 placeholder secrets は設定される
  - Generated README に同じ secret 名と `REPLACE_ME` 更新導線が記載される
- **Evidence**
  - bootstrap workflow の secret sync step
  - Generated `README.md`

### BF-AC-023: Guardrail Unsupported Notice

- **Purpose**: guardrail 未適用時の利用者通知が欠落しないことを確認する
- **Input**: guardrails unsupported 条件
- **Checks**
  - bootstrap summary / log に guardrails 未適用が記録される
  - 記録に public repository または GitHub Pro+ 条件が含まれる
  - Generated README に手動再実行コマンドが残る
  - repository generation 自体は成功する
- **Evidence**
  - bootstrap summary / logs
  - guardrail step outputs (`status`, `message`)
  - Generated `README.md`

## Supporting Scenario Matrix

| Scenario group | Main checks |
|---|---|
| Bootstrap selection | 選択した framework の workflow / tasks / README が残り、未選択 framework 資産と bootstrap 専用 workflow / script が除去される |
| README generation | framework 別 required section が生成され、`next` では `.NET` 専用パターンが拒否される |
| AI assets packaging | `.github/agents` / `.github/skills` が同梱され、framework 依存アセットが allowlist どおりに残る |
| Documents API | 公開 Document は匿名参照、非公開は所有者または権限保持者のみ参照、作成は認証必須 |
| Admin API | 非管理者は 403、管理者は role CRUD / hierarchy 更新 / 実効権限確認が可能 |
| Initial admin seed | `INITIAL_ADMIN_GOOGLE_USER_ID` 設定時に管理者が補完され、再起動時も重複しない |
| dotnet8 / dotnet10 parity | endpoint 群、README 必須セクション、Docker / test 前提が一致する |
