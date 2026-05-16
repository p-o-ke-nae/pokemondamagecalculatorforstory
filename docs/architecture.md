# Architecture Overview

## 概要

このドキュメントは Issue #8 の bootstrap foundation 再設計で影響を受けるモジュールと横断ルールを整理するための俯瞰図です。リポジトリ直下の [`../ARCHITECTURE.md`](../ARCHITECTURE.md) が生成される `.NET` アプリケーションの基礎アーキテクチャを扱うのに対し、本書は bootstrap・workflow・ドキュメント・テンプレート資産を含むリポジトリ全体の変更境界を定義します。

| 項目 | 内容 |
|---|---|
| 対象 Issue | #8 bootstrap foundation redesign |
| 現在の役割 | 影響モジュール一覧、責務境界、横断ルールの明示 |
| 関連設計 | [Bootstrap Foundation Design](designs/bootstrap-foundation.md) |
| 関連テスト | [Bootstrap Foundation Test Plan](tests/bootstrap-foundation-test-plan.md) |

## Status

- Phase: 3
- Step: 3.4.5
- Scope: 実装差分に追従した documentation alignment
- Detail level: implementation-aligned repository contract

## ドキュメント境界

| ドキュメント | 主な責務 | この Issue で参照する理由 |
|---|---|---|
| [`../ARCHITECTURE.md`](../ARCHITECTURE.md) | 生成される `.NET` テンプレートの Clean Architecture / DDD 原則 | `.NET` sample 再編時に守る基礎原則を確認するため |
| `architecture.md` | bootstrap・workflow・AI assets・docs を含むリポジトリ全体の影響範囲を定義 | Issue #8 の変更境界を明確にするため |

## Top-Level Areas

| Area | Responsibility | Implemented detail |
|---|---|---|
| Bootstrap workflow | 選択 framework に応じた生成物の確定 | README / workflow / tasks / compose / Dockerfile の選択と cleanup を実施 |
| Workflow availability | framework ごとの test → build/push → deploy 契約と branch trigger | `.github/workflows/main.yml` の生成後検証まで含めて保証 |
| Documentation flow | Base README と Generated README の責務分離 | framework 別 README 生成と required / forbidden section 検証を実施 |
| AI assets packaging | `.github/agents` / `.github/skills` の同梱と整理ルール | framework 非依存アセットは残し、不要 bootstrap 補助資産は cleanup する |
| Guardrail integration | `configure_copilot_guardrails` の warn / skip / no-op と利用者通知 | best-effort 適用、unsupported 条件の warning、summary 反映を実施 |
| `.NET` sample application | Documents 系サンプル、管理 API、初回管理者 seed | [Bootstrap Foundation Design](designs/bootstrap-foundation.md#net-sample-architecture) |
| Validation | bootstrap 契約、README 契約、workflow 契約、API 契約の検証 | [Bootstrap Foundation Test Plan](tests/bootstrap-foundation-test-plan.md) |

## Impact Module Map

| Module / Path | Planned responsibility |
|---|---|
| `.github/workflows/template-bootstrap.yml` | framework 単一生成、generated `main.yml` / `README.md` の契約検証、ACA / guardrail summary の出力 |
| `.github/workflow-templates/**` | `develop` / `main` / `copilot/**` 向け workflow 雛形の framework 別選択と `main.yml` への昇格元 |
| `.github/agents/**` | 生成後リポジトリに同梱するエージェント定義 |
| `.github/skills/**` | 生成後リポジトリに同梱するスキル定義 |
| `.vscode/**` | framework 別 tasks / launch / settings の残存制御 |
| `docker-compose*.yml` | framework 別のローカル開発・検証構成の残存制御 |
| `scripts/bootstrap-template.ps1` | framework 別 README 生成、port 置換、不要 workflow / Docker / script の cleanup |
| `scripts/configure-github-guardrails.ps1` | best-effort guardrail 実行、ruleset unsupported 条件の skip / warning 出力 |
| `templates/dotnet8/**` | Documents 系サンプル、管理 API、seed、認可モデル更新 |
| `templates/dotnet10/**` | dotnet8 と同一意図の横展開 |
| `templates/next/**` | Next.js 向け生成物、README、workflow 導線の最適化 |
| `README.md` | ベースリポジトリ説明への責務限定 |
| `docs/**` | 設計契約、テスト観点、レビュー用トレーサビリティ |

## Cross-Cutting Rules

- bootstrap 後の生成物には選択 framework の資産のみを残す
- Base README はベースソリューション自体の説明に限定する
- Generated README は framework 別に script 生成し、bootstrap workflow で required / forbidden section を検証する
- `next` Generated README には `.NET` 専用手順を含めず、`dotnet8` / `dotnet10` Generated README には SQL Server / EF Core / Infrastructure test 手順を含める
- AI assets は同梱するが、framework 依存資産は bootstrap script の framework contract に従って整理する
- generated `main.yml` は `develop` / `main` / `copilot/**` push で `test → build/push → deploy` を提供し、deploy は `ACA_DEPLOY_ENABLED` と関連 variables が揃う場合のみ実行する
- `create_aca=false` では生成成功を優先し、`ACA_DEPLOY_ENABLED=false` を設定した上で ACA 自動作成のみを summary 付きで skip する
- guardrail は best-effort とし、unsupported 条件では warning と `skipped` message を返して生成処理を継続する
- 生成後リポジトリでは `scripts/configure-github-guardrails.ps1` は残し、`scripts/bootstrap-template.ps1` と `.github/workflows/template-bootstrap.yml` は削除する
- `.NET` テンプレートは target framework 差分を除き構成・API・README・検証観点を揃える
