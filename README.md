# pokenaeBaseSolution

> 関連: [docs/copilot-cli-devcontainer.md](docs/copilot-cli-devcontainer.md), [ARCHITECTURE.md](ARCHITECTURE.md)

---

## 概要

pokenaeBaseSolution は、dotnet8 / dotnet10 / Next.js のテンプレートと ACA 関連ワークフローを持つ基盤リポジトリであり、GitHub Copilot CLI 相当のターミナルエージェントを Docker ベースの Dev Container 内で使う標準開発フローを提供する。

### 主要なポイント

| 項目 | 内容 |
|---|---|
| 開発開始 | Clone → Open Folder → Reopen in Container |
| コンテナ内ツール | Dotnet 8 / 10、Node.js 20、PowerShell、GitHub CLI、Docker 利用前提 |
| Copilot 作業場所 | VS Code のコンテナ内ワークスペース |
| ブランチ保護 | `main` / `develop` は reviewed PR merge を前提に保護 |
| 詳細手順 | `docs/copilot-cli-devcontainer.md` を参照 |

---

## 1. GitHub Copilot CLI の隔離開発

実装者は、GitHub からクローンしたリポジトリを VS Code で開き、`.devcontainer/` を使って Docker ベースの隔離環境へ入ってから作業する。

1. リポジトリをクローンする。
2. VS Code でフォルダを開く。
3. `Reopen in Container` を実行する。
4. コンテナ内ターミナルから GitHub Copilot CLI 相当のエージェントへ指示する。

保護ブランチの guardrail は次のスクリプトで適用できる。

```powershell
pwsh ./scripts/configure-github-guardrails.ps1 -Owner <owner> -Repo <repo>
```

## 2. サポートする生成 framework

| Framework | 生成後の主な構成 | 詳細導線 |
|---|---|---|
| `next` | Next.js アプリ、framework 専用 `main.yml`、framework 専用 README、共通 AI assets | Generated Repository の `README.md` と `docs/` |
| `dotnet8` | ASP.NET Core テンプレート、framework 専用 `main.yml`、framework 専用 README、共通 AI assets | Generated Repository の `README.md` と `docs/` |
| `dotnet10` | ASP.NET Core テンプレート、framework 専用 `main.yml`、framework 専用 README、共通 AI assets | Generated Repository の `README.md` と `docs/` |

## 3. ベースリポジトリが保証する生成契約

- bootstrap 後の生成物には、**選択した framework の資産のみ**を残す。
- Generated `README.md` は framework 別に生成し、`next` には `.NET` 専用手順を含めない。
- generated `.github/workflows/main.yml` は `develop` / `main` / `copilot/**` に対して `test → build/push → deploy` を提供する。
- `create_aca=false` の場合でも repository 生成は成功し、ACA 自動作成だけを skip する。
- `create_aca=true` の場合、bootstrap workflow は `aca_name_base` の入力を求め、23 文字以下・英小文字/数字/`-`・先頭英字・末尾英数字・`--` 禁止の条件で fail-fast 検証したうえで ACA environment / app 名を自動派生する。
- guardrail 適用は best-effort とし、unsupported 条件では warning を残して生成を継続する。

## 4. Generated Repository 側の詳細ドキュメント

生成後リポジトリのセットアップや framework 固有の運用手順は、ベース README ではなく generated repository 側の `README.md` と `docs/` を参照する。

| ドキュメント | 用途 |
|---|---|
| `README.md` | 選択 framework の起動手順、workflow、guardrail 状態、必要 secrets / variables の確認 |
| `docs/copilot-cli-devcontainer.md` | Dev Container ベースの開発フロー |
| `docs/README.md` | 生成物に含まれる docs の入口 |
| `docs/designs/bootstrap-foundation.md` | bootstrap / workflow / README / guardrail の契約 |
| `docs/tests/bootstrap-foundation-test-plan.md` | 受け入れ基準と検証観点 |

## 5. このベースリポジトリで確認すべき設計資料

- [ARCHITECTURE.md](ARCHITECTURE.md)
- [docs/README.md](docs/README.md)
- [docs/architecture.md](docs/architecture.md)
- [docs/designs/bootstrap-foundation.md](docs/designs/bootstrap-foundation.md)
- [docs/tests/bootstrap-foundation-test-plan.md](docs/tests/bootstrap-foundation-test-plan.md)

## 6. Guardrail の手動適用

bootstrap 実行時に ruleset API が使えなかった場合でも、生成後リポジトリには手動再実行用スクリプトを残す。

```powershell
pwsh ./scripts/configure-github-guardrails.ps1 -Owner <owner> -Repo <repo>
```
