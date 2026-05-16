# PokemonDamageCalculatorForStory

> 関連: [docs/copilot-cli-devcontainer.md](docs/copilot-cli-devcontainer.md)

---

## 概要

PokemonDamageCalculatorForStory の ASP.NET Core Web API ソリューションで、GitHub Copilot CLI 相当のターミナルエージェントを Docker ベースの Dev Container 内で使う標準フローを前提にしている。

### 主要なポイント

| 項目 | 内容 |
| --- | --- |
| 標準起動 | Clone → Open Folder → Reopen in Container |
| 主なツール | Dotnet 8 / 10、PowerShell、GitHub CLI、Docker 利用前提 |
| 保護ブランチ | `main` / `develop` は reviewed PR merge を前提に保護する |
| 詳細手順 | `docs/copilot-cli-devcontainer.md` を参照 |

---

## 1. Dev Container で作業を始める

1. リポジトリを VS Code で開きます。
2. `Reopen in Container` を実行します。
3. コンテナ内ターミナルで GitHub Copilot CLI 相当のエージェントへ指示します。
4. build / test / GitHub 操作はコンテナ内ワークスペースで行います。

## 2. ローカルの公開ポート

| Environment | HTTP | HTTPS | SQL Server |
| --- | ---: | ---: | --- |
| Debug | 20080 | 20081 | localhost:20033 |
| Development | 20180 | 20181 | 非公開 |
| Production | 20280 | 20281 | 非公開 |

### SQL Server 接続先

| 用途 | ホスト | ポート | 備考 |
| --- | --- | ---: | --- |
| Debug DB | localhost | 20033 | `.env.docker.debug` の `DB_HOST_PORT` |
| Test DB | localhost | 20034 | `.env.docker.test` の `TEST_DB_HOST_PORT` |

Debug 環境では SQL Server を localhost のみへ公開しています。SSMS などから接続する場合は上記のポートを使用してください。

## 3. EF Core Migrations

EF Core コマンドは、対象プロジェクトとスタートアッププロジェクトを明示して実行してください。

```powershell
dotnet ef migrations add InitialCreate --project .\PokemonDamageCalculatorForStory.Infrastructure\PokemonDamageCalculatorForStory.Infrastructure.csproj --startup-project .\PokemonDamageCalculatorForStory.Web\PokemonDamageCalculatorForStory.Web.csproj
dotnet ef database update --project .\PokemonDamageCalculatorForStory.Infrastructure\PokemonDamageCalculatorForStory.Infrastructure.csproj --startup-project .\PokemonDamageCalculatorForStory.Web\PokemonDamageCalculatorForStory.Web.csproj
```

引数なしの `dotnet ef` を使う場合は、`.\PokemonDamageCalculatorForStory.Infrastructure` へ移動してから実行してください。

## 4. Visual Studio で Infrastructure テストを実行する

Infrastructure の Repository テストは SQL Server テストコンテナを使います。Visual Studio から実行する場合も、先に test DB を起動してください。
test DB を起動すると localhost:20034 で公開されるため、SSMS などから中身を確認できます。

```powershell
docker compose --env-file .env.docker.test -p pokemondamagecalculatorforstory-dotnet-test-db -f docker-compose.dotnet.test-db.yml up -d
```

1. Visual Studio で `.\PokemonDamageCalculatorForStory.sln` を開きます。
2. 必要なら `表示 > ターミナル` を開き、上記の `docker compose` コマンドで test DB を起動します。
3. `テスト > テスト エクスプローラー` を開き、`Infrastructure` または `PokemonDamageCalculatorForStory.Tests.Infrastructure` で検索します。
4. 対象クラス、または Infrastructure 配下のテストだけを実行します。
5. Repository テストを実行したあとは、必要に応じて test DB を停止します。

```powershell
docker compose --env-file .env.docker.test -p pokemondamagecalculatorforstory-dotnet-test-db -f docker-compose.dotnet.test-db.yml down
```

## 5. GitHub Actions / デプロイ

- `develop`、`main`、`copilot/**` への push で `test → build/push → deploy` の順に GitHub Actions が実行されます。
- deploy job は `ACA_DEPLOY_ENABLED=true` かつ必要な repository variables / secrets が揃っている場合だけ実行されます。
- bootstrap 時に `create_aca=false` を選んだ場合でも workflow 自体は残り、ACA の自動作成だけが skip されます。

### ブランチと ACA デプロイ先の対応

| Push 先ブランチ | deploy job | 参照する variable | 入力する値 / 例 |
| --- | --- | --- | --- |
| `develop` | `deploy-develop-aca` | `ACA_APP_NAME_DEV` | `<aca_name_base>-develop`（例: `pokemondamagecalculatorforstory-develop`） |
| `copilot/**` | `deploy-copilot-aca` | `ACA_APP_NAME_COPILOT` | `<aca_name_base>-copilot`（例: `pokemondamagecalculatorforstory-copilot`） |
| `main` | `deploy-main-aca` | `ACA_APP_NAME_PROD` | `<aca_name_base>`（例: `pokemondamagecalculatorforstory`） |

### 自動 ACA デプロイの必須 repository variables

| Variable | 推奨値 | 用途 |
| --- | --- | --- |
| `ACA_DEPLOY_ENABLED` | `true` | push 時に deploy job を有効化します。`false` のままでは build までで停止します。 |
| `AZURE_RESOURCE_GROUP` | `pokemondamagecalculatorforstory-rg` または実際の RG 名 | ACA を配置している Azure Resource Group 名です。 |
| `ACA_APP_NAME_DEV` | `<aca_name_base>-develop`（例: `pokemondamagecalculatorforstory-develop`） | `develop` push のデプロイ先 ACA 名です。 |
| `ACA_APP_NAME_COPILOT` | `<aca_name_base>-copilot`（例: `pokemondamagecalculatorforstory-copilot`） | `copilot/**` push のデプロイ先 ACA 名です。 |
| `ACA_APP_NAME_PROD` | `<aca_name_base>`（例: `pokemondamagecalculatorforstory`） | `main` push のデプロイ先 ACA 名です。 |

### .NET CI の DB テストで使う repository variables

| Variable | 推奨値 | 用途 |
| --- | --- | --- |
| `CI_TEST_MSSQL_DB` | `PokemonDamageCalculatorForStory` | Infrastructure DB テストで使う DB 名です。 |
| `CI_TEST_MSSQL_PID` | `Developer` | SQL Server テストコンテナの edition です。通常は変更不要です。 |
| `CI_TEST_DB_HOST_PORT` | `20034` | CI の DB テストコンテナを公開するホストポートです。通常は bootstrap 設定値のまま使います。 |

### 自動 ACA デプロイの必須 repository secrets

| Secret | 値 | 用途 |
| --- | --- | --- |
| `AZURE_CREDENTIALS` | `{"clientId":"...","clientSecret":"...","subscriptionId":"...","tenantId":"..."}` | Azure Service Principal JSON を使う場合の認証情報です。OIDC を使う場合は未設定で構いません。 |
| `AZURE_CLIENT_ID` | Microsoft Entra アプリ登録の Client ID | OIDC を使う場合に必要です。`AZURE_CREDENTIALS` を使う場合は不要です。 |
| `AZURE_TENANT_ID` | Azure tenant GUID | OIDC を使う場合に必要です。 |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription GUID | OIDC を使う場合に必要です。 |

### .NET CI の DB テストで使う repository secrets

| Secret | 値 | 用途 |
| --- | --- | --- |
| `CI_TEST_MSSQL_SA_PASSWORD` | `Sql1Password` | Infrastructure DB テストで使う SA パスワードです。 |

### 手入力が必要な項目

| 項目 | 手入力が必要な条件 | 入力する値 |
| --- | --- | --- |
| `AZURE_CREDENTIALS` | Service Principal JSON 認証を使い、bootstrap がこの secret を同期していない場合 | Azure Service Principal JSON |
| `AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_SUBSCRIPTION_ID` | OIDC 認証を使い、bootstrap がこれらを同期していない場合 | Azure OIDC 用の実値 |
| `ACA_DEPLOY_ENABLED` / `AZURE_RESOURCE_GROUP` / `ACA_APP_NAME_*` | `create_aca=false` で bootstrap した場合、または手動作成した ACA 名に切り替える場合 | 実際の Azure Resource Group 名と Container App 名 |
| `CI_TEST_*` | 通常は不要。DB 名、edition、ポート、SA パスワードを既定値から変えたい場合だけ | テスト用 SQL Server に合わせた値 |

ACA を自動作成する場合は、bootstrap workflow の `aca_name_base` を **1 つだけ手入力** します。そこから `<base>-aca-env` / `<base>-develop` / `<base>-copilot` / `<base>` が自動派生します。`aca_name_base` は 23 文字以下、英小文字・数字・`-` のみ、先頭は英字、末尾は英数字、`--` 禁止です。

`AZURE_CREDENTIALS` を使う方法と、`AZURE_CLIENT_ID` / `AZURE_TENANT_ID` / `AZURE_SUBSCRIPTION_ID` を使う方法の **どちらか一方** を設定してください。

`create_aca=true` で bootstrap し、`aca_name_base` を入力し、かつ bootstrap 実行元に Azure 認証 secret が入っていた場合は、生成後 repository 側で追加手入力なしに branch push から自動 deploy できます。`CI_TEST_*` は通常 bootstrap 済みです。

## 6. Guardrails

- bootstrap は `main` / `develop` の reviewed PR guardrails 適用を best-effort で試行します。
- GitHub Free の private repository など ruleset API を使えない条件では、生成は継続しつつ bootstrap summary / log に未適用理由が記録されます。
- 後から手動適用する場合は、repository を public にするか GitHub Pro+ 相当の条件を満たした上で次を実行してください。

```powershell
pwsh ./scripts/configure-github-guardrails.ps1 -Owner <owner> -Repo <repo>
```