param(
  [Parameter(Mandatory = $true)]
  [string]$ProjectName,

  [ValidateSet("dotnet8", "dotnet10", "next")]
  [string]$Framework = "dotnet8",

  [Parameter(Mandatory = $true)]
  [ValidateRange(1000, 59999)]
  [int]$PortBase,

  [string]$OldSlug = "pokenae",
  [string]$OldSolutionName = "pokenaeBaseSolution"
)

$ErrorActionPreference = "Stop"

$Utf8NoBomEncoding = [System.Text.UTF8Encoding]::new($false)

function Get-FileTextOrEmpty {
  param([string]$Path)

  if (-not [System.IO.File]::Exists($Path)) {
    return ""
  }

  return [System.IO.File]::ReadAllText($Path, $Utf8NoBomEncoding)
}

function Write-Utf8TextNoNewline {
  param(
    [string]$Path,
    [AllowEmptyString()]
    [string]$Value
  )

  [System.IO.File]::WriteAllText($Path, $Value, $Utf8NoBomEncoding)
}

function Convert-ToSlug {
  param([string]$Value)
  $v = $Value.ToLowerInvariant()
  $v = [Regex]::Replace($v, "[^a-z0-9]+", "-")
  $v = $v.Trim("-")
  if ([string]::IsNullOrWhiteSpace($v)) {
    throw "ProjectName '$Value' cannot be converted to a non-empty slug."
  }
  return $v
}

function Replace-InFile {
  param(
    [string]$Path,
    [hashtable]$Map
  )

  $content = Get-FileTextOrEmpty -Path $Path
  $updated = $content
  # 長いキーから先に置換して部分文字列の誤マッチを防止
  $sortedKeys = $Map.Keys | Sort-Object { $_.Length } -Descending
  foreach ($k in $sortedKeys) {
    $updated = $updated.Replace($k, $Map[$k])
  }

  if ($updated -ne $content) {
    Write-Utf8TextNoNewline -Path $Path -Value $updated
    return $true
  }

  return $false
}

function Set-OrAddEnvVar {
  param(
    [string]$Path,
    [string]$Name,
    [string]$Value
  )

  if (-not (Test-Path $Path)) {
    return
  }

  $content = Get-FileTextOrEmpty -Path $Path
  $pattern = "(?m)^" + [Regex]::Escape($Name) + "=.*$"
  $entry = "$Name=$Value"

  if ($content -match $pattern) {
    $updated = [Regex]::Replace($content, $pattern, $entry)
  } else {
    $separator = if ($content.EndsWith("`n") -or $content.EndsWith("`r")) { "" } else { [Environment]::NewLine }
    $updated = $content + $separator + $entry + [Environment]::NewLine
  }

  if ($updated -ne $content) {
    Write-Utf8TextNoNewline -Path $Path -Value $updated
  }
}

function Remove-EnvVar {
  param(
    [string]$Path,
    [string]$Name
  )

  if (-not (Test-Path $Path)) {
    return
  }

  $content = Get-FileTextOrEmpty -Path $Path
  $updated = [Regex]::Replace($content, "(?m)^" + [Regex]::Escape($Name) + "=.*(?:\r?\n)?", "")

  if ($updated -ne $content) {
    Write-Utf8TextNoNewline -Path $Path -Value $updated.TrimEnd("`r", "`n")
  }
}

function Copy-TemplateTree {
  param(
    [string]$Source,
    [string]$Destination
  )

  $excludedNames = @(".gitkeep", ".vs", "bin", "obj", "node_modules")

  if (-not (Test-Path $Destination)) {
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
  }

  Get-ChildItem -LiteralPath $Source -Force | Where-Object {
    $excludedNames -notcontains $_.Name
  } | ForEach-Object {
    $targetPath = Join-Path $Destination $_.Name

    if ($_.PSIsContainer) {
      Copy-TemplateTree -Source $_.FullName -Destination $targetPath
    } else {
      Copy-Item -LiteralPath $_.FullName -Destination $targetPath -Force
    }
  }
}

function Get-DotnetGeneratedReadmeContent {
  param(
    [string]$ProjectName,
    [int]$PortBase,
    [string]$ReadmeProjectSlug
  )

  $tick = [char]96

  $lines = @(
    "# $ProjectName",
    "",
    "> 関連: [docs/copilot-cli-devcontainer.md](docs/copilot-cli-devcontainer.md)",
    "",
    "---",
    "",
    "## 概要",
    "",
    "$ProjectName の ASP.NET Core Web API ソリューションで、GitHub Copilot CLI 相当のターミナルエージェントを Docker ベースの Dev Container 内で使う標準フローを前提にしている。",
    "",
    "### 主要なポイント",
    "",
    "| 項目 | 内容 |",
    "| --- | --- |",
    "| 標準起動 | Clone → Open Folder → Reopen in Container |",
    "| 主なツール | Dotnet 8 / 10、PowerShell、GitHub CLI、Docker 利用前提 |",
    "| 保護ブランチ | ${tick}main${tick} / ${tick}develop${tick} は reviewed PR merge を前提に保護する |",
    "| 詳細手順 | ${tick}docs/copilot-cli-devcontainer.md${tick} を参照 |",
    "",
    "---",
    "",
    "## 1. Dev Container で作業を始める",
    "",
    "1. リポジトリを VS Code で開きます。",
    "2. ${tick}Reopen in Container${tick} を実行します。",
    "3. コンテナ内ターミナルで GitHub Copilot CLI 相当のエージェントへ指示します。",
    "4. build / test / GitHub 操作はコンテナ内ワークスペースで行います。",
    "",
    "## 2. ローカルの公開ポート",
    "",
    "| Environment | HTTP | HTTPS | SQL Server |",
    "| --- | ---: | ---: | --- |",
    "| Debug | $($PortBase + 80) | $($PortBase + 81) | localhost:$($PortBase + 33) |",
    "| Development | $($PortBase + 180) | $($PortBase + 181) | 非公開 |",
    "| Production | $($PortBase + 280) | $($PortBase + 281) | 非公開 |",
    "",
    "### SQL Server 接続先",
    "",
    "| 用途 | ホスト | ポート | 備考 |",
    "| --- | --- | ---: | --- |",
    "| Debug DB | localhost | $($PortBase + 33) | ${tick}.env.docker.debug${tick} の ${tick}DB_HOST_PORT${tick} |",
    "| Test DB | localhost | $($PortBase + 34) | ${tick}.env.docker.test${tick} の ${tick}TEST_DB_HOST_PORT${tick} |",
    "",
    "Debug 環境では SQL Server を localhost のみへ公開しています。SSMS などから接続する場合は上記のポートを使用してください。",
    "",
    "## 3. EF Core Migrations",
    "",
    "EF Core コマンドは、対象プロジェクトとスタートアッププロジェクトを明示して実行してください。",
    "",
    '```powershell',
    ('dotnet ef migrations add InitialCreate --project .\{0}.Infrastructure\{0}.Infrastructure.csproj --startup-project .\{0}.Web\{0}.Web.csproj' -f $ProjectName),
    ('dotnet ef database update --project .\{0}.Infrastructure\{0}.Infrastructure.csproj --startup-project .\{0}.Web\{0}.Web.csproj' -f $ProjectName),
    '```',
    "",
    ('引数なしの `dotnet ef` を使う場合は、`.\{0}.Infrastructure` へ移動してから実行してください。' -f $ProjectName),
    "",
    "## 4. Visual Studio で Infrastructure テストを実行する",
    "",
    "Infrastructure の Repository テストは SQL Server テストコンテナを使います。Visual Studio から実行する場合も、先に test DB を起動してください。",
    "test DB を起動すると localhost:$($PortBase + 34) で公開されるため、SSMS などから中身を確認できます。",
    "",
    '```powershell',
    "docker compose --env-file .env.docker.test -p $readmeProjectSlug-dotnet-test-db -f docker-compose.dotnet.test-db.yml up -d",
    '```',
    "",
    ('1. Visual Studio で `.\{0}.sln` を開きます。' -f $ProjectName),
    "2. 必要なら ${tick}表示 > ターミナル${tick} を開き、上記の ${tick}docker compose${tick} コマンドで test DB を起動します。",
    ('3. `テスト > テスト エクスプローラー` を開き、`Infrastructure` または `{0}.Tests.Infrastructure` で検索します。' -f $ProjectName),
    "4. 対象クラス、または Infrastructure 配下のテストだけを実行します。",
    "5. Repository テストを実行したあとは、必要に応じて test DB を停止します。",
    "",
    '```powershell',
    "docker compose --env-file .env.docker.test -p $readmeProjectSlug-dotnet-test-db -f docker-compose.dotnet.test-db.yml down",
    '```',
    "",
    "## 5. GitHub Actions / デプロイ",
    "",
    "- ${tick}develop${tick}、${tick}main${tick}、${tick}copilot/**${tick} への push で ${tick}test → build/push → deploy${tick} の順に GitHub Actions が実行されます。",
    "- deploy job は ${tick}ACA_DEPLOY_ENABLED=true${tick} かつ必要な repository variables / secrets が揃っている場合だけ実行されます。",
    "- bootstrap 時に ${tick}create_aca=false${tick} を選んだ場合でも workflow 自体は残り、ACA の自動作成だけが skip されます。",
    "",
    "### ブランチと ACA デプロイ先の対応",
    "",
    "| Push 先ブランチ | deploy job | 参照する variable | 入力する値 / 例 |",
    "| --- | --- | --- | --- |",
    "| ${tick}develop${tick} | ${tick}deploy-develop-aca${tick} | ${tick}ACA_APP_NAME_DEV${tick} | ${tick}<aca_name_base>-develop${tick}（例: ${tick}${ReadmeProjectSlug}-develop${tick}） |",
    "| ${tick}copilot/**${tick} | ${tick}deploy-copilot-aca${tick} | ${tick}ACA_APP_NAME_COPILOT${tick} | ${tick}<aca_name_base>-copilot${tick}（例: ${tick}${ReadmeProjectSlug}-copilot${tick}） |",
    "| ${tick}main${tick} | ${tick}deploy-main-aca${tick} | ${tick}ACA_APP_NAME_PROD${tick} | ${tick}<aca_name_base>${tick}（例: ${tick}${ReadmeProjectSlug}${tick}） |",
    "",
    "### 自動 ACA デプロイの必須 repository variables",
    "",
    "| Variable | 推奨値 | 用途 |",
    "| --- | --- | --- |",
    "| ${tick}ACA_DEPLOY_ENABLED${tick} | ${tick}true${tick} | push 時に deploy job を有効化します。${tick}false${tick} のままでは build までで停止します。 |",
    "| ${tick}AZURE_RESOURCE_GROUP${tick} | ${tick}${ReadmeProjectSlug}-rg${tick} または実際の RG 名 | ACA を配置している Azure Resource Group 名です。 |",
    "| ${tick}ACA_APP_NAME_DEV${tick} | ${tick}<aca_name_base>-develop${tick}（例: ${tick}${ReadmeProjectSlug}-develop${tick}） | ${tick}develop${tick} push のデプロイ先 ACA 名です。 |",
    "| ${tick}ACA_APP_NAME_COPILOT${tick} | ${tick}<aca_name_base>-copilot${tick}（例: ${tick}${ReadmeProjectSlug}-copilot${tick}） | ${tick}copilot/**${tick} push のデプロイ先 ACA 名です。 |",
    "| ${tick}ACA_APP_NAME_PROD${tick} | ${tick}<aca_name_base>${tick}（例: ${tick}${ReadmeProjectSlug}${tick}） | ${tick}main${tick} push のデプロイ先 ACA 名です。 |",
    "",
    "### .NET CI の DB テストで使う repository variables",
    "",
    "| Variable | 推奨値 | 用途 |",
    "| --- | --- | --- |",
    "| ${tick}CI_TEST_MSSQL_DB${tick} | ${tick}${ProjectName}${tick} | Infrastructure DB テストで使う DB 名です。 |",
    "| ${tick}CI_TEST_MSSQL_PID${tick} | ${tick}Developer${tick} | SQL Server テストコンテナの edition です。通常は変更不要です。 |",
    "| ${tick}CI_TEST_DB_HOST_PORT${tick} | ${tick}$($PortBase + 34)${tick} | CI の DB テストコンテナを公開するホストポートです。通常は bootstrap 設定値のまま使います。 |",
    "",
    "### 自動 ACA デプロイの必須 repository secrets",
    "",
    "| Secret | 値 | 用途 |",
    "| --- | --- | --- |",
    ("| {0} | {1} | Azure Service Principal JSON を使う場合の認証情報です。OIDC を使う場合は未設定で構いません。 |" -f "${tick}AZURE_CREDENTIALS${tick}", "${tick}{`"clientId`":`"...`",`"clientSecret`":`"...`",`"subscriptionId`":`"...`",`"tenantId`":`"...`"}${tick}"),
    "| ${tick}AZURE_CLIENT_ID${tick} | Microsoft Entra アプリ登録の Client ID | OIDC を使う場合に必要です。${tick}AZURE_CREDENTIALS${tick} を使う場合は不要です。 |",
    "| ${tick}AZURE_TENANT_ID${tick} | Azure tenant GUID | OIDC を使う場合に必要です。 |",
    "| ${tick}AZURE_SUBSCRIPTION_ID${tick} | Azure subscription GUID | OIDC を使う場合に必要です。 |",
    "",
    "### .NET CI の DB テストで使う repository secrets",
    "",
    "| Secret | 値 | 用途 |",
    "| --- | --- | --- |",
    "| ${tick}CI_TEST_MSSQL_SA_PASSWORD${tick} | ${tick}Sql1Password${tick} | Infrastructure DB テストで使う SA パスワードです。 |",
    "",
    "### 手入力が必要な項目",
    "",
    "| 項目 | 手入力が必要な条件 | 入力する値 |",
    "| --- | --- | --- |",
    "| ${tick}AZURE_CREDENTIALS${tick} | Service Principal JSON 認証を使い、bootstrap がこの secret を同期していない場合 | Azure Service Principal JSON |",
    "| ${tick}AZURE_CLIENT_ID${tick} / ${tick}AZURE_TENANT_ID${tick} / ${tick}AZURE_SUBSCRIPTION_ID${tick} | OIDC 認証を使い、bootstrap がこれらを同期していない場合 | Azure OIDC 用の実値 |",
    "| ${tick}ACA_DEPLOY_ENABLED${tick} / ${tick}AZURE_RESOURCE_GROUP${tick} / ${tick}ACA_APP_NAME_*${tick} | ${tick}create_aca=false${tick} で bootstrap した場合、または手動作成した ACA 名に切り替える場合 | 実際の Azure Resource Group 名と Container App 名 |",
    "| ${tick}CI_TEST_*${tick} | 通常は不要。DB 名、edition、ポート、SA パスワードを既定値から変えたい場合だけ | テスト用 SQL Server に合わせた値 |",
    "",
    "ACA を自動作成する場合は、bootstrap workflow の ${tick}aca_name_base${tick} を **1 つだけ手入力** します。そこから ${tick}<base>-aca-env${tick} / ${tick}<base>-develop${tick} / ${tick}<base>-copilot${tick} / ${tick}<base>${tick} が自動派生します。${tick}aca_name_base${tick} は 23 文字以下、英小文字・数字・${tick}-${tick} のみ、先頭は英字、末尾は英数字、${tick}--${tick} 禁止です。",
    "",
    "${tick}AZURE_CREDENTIALS${tick} を使う方法と、${tick}AZURE_CLIENT_ID${tick} / ${tick}AZURE_TENANT_ID${tick} / ${tick}AZURE_SUBSCRIPTION_ID${tick} を使う方法の **どちらか一方** を設定してください。",
    "",
    "${tick}create_aca=true${tick} で bootstrap し、${tick}aca_name_base${tick} を入力し、かつ bootstrap 実行元に Azure 認証 secret が入っていた場合は、生成後 repository 側で追加手入力なしに branch push から自動 deploy できます。${tick}CI_TEST_*${tick} は通常 bootstrap 済みです。",
    "",
    "## 6. Guardrails",
    "",
    "- bootstrap は ${tick}main${tick} / ${tick}develop${tick} の reviewed PR guardrails 適用を best-effort で試行します。",
    "- GitHub Free の private repository など ruleset API を使えない条件では、生成は継続しつつ bootstrap summary / log に未適用理由が記録されます。",
    "- 後から手動適用する場合は、repository を public にするか GitHub Pro+ 相当の条件を満たした上で次を実行してください。",
    "",
    '```powershell',
    "pwsh ./scripts/configure-github-guardrails.ps1 -Owner <owner> -Repo <repo>",
    '```'
  )

  return ($lines -join [Environment]::NewLine)
}

function Get-NextGeneratedReadmeContent {
  param(
    [string]$ProjectName,
    [string]$ReadmeProjectSlug
  )

  $tick = [char]96

  $lines = @(
    "# $ProjectName",
    "",
    "> 関連: [docs/copilot-cli-devcontainer.md](docs/copilot-cli-devcontainer.md)",
    "",
    "---",
    "",
    "## 概要",
    "",
    "$ProjectName の Next.js プロジェクトで、GitHub Copilot CLI 相当のターミナルエージェントを Docker ベースの Dev Container 内で使う標準フローを前提にしている。",
    "",
    "### 主要なポイント",
    "",
    "| 項目 | 内容 |",
    "| --- | --- |",
    "| 標準起動 | Clone → Open Folder → Reopen in Container |",
    "| 主なツール | Node.js 20、PowerShell、GitHub CLI、Docker 利用前提 |",
    "| 保護ブランチ | ${tick}main${tick} / ${tick}develop${tick} は reviewed PR merge を前提に保護する |",
    "| 詳細手順 | ${tick}docs/copilot-cli-devcontainer.md${tick} を参照 |",
    "",
    "---",
    "",
    "## 1. Dev Container で作業を始める",
    "",
    "1. リポジトリを VS Code で開きます。",
    "2. ${tick}Reopen in Container${tick} を実行します。",
    "3. コンテナ内ターミナルで GitHub Copilot CLI 相当のエージェントへ指示します。",
    "4. ${tick}npm run dev${tick} や Docker Compose の操作もコンテナ内で行います。",
    "",
    "## 2. ローカルの公開ポート",
    "",
    "| Environment | HTTP |",
    "| --- | ---: |",
    "| Debug | 3000 |",
    "| Development | 3002 |",
    "| Production | 3001 |",
    "",
    "## 3. よく使う確認コマンド",
    "",
    '```powershell',
    "npm install",
    "npm run lint",
    "npm run test",
    '```',
    "",
    "## 4. GitHub Actions / デプロイ",
    "",
    "- ${tick}develop${tick}、${tick}main${tick}、${tick}copilot/**${tick} への push で ${tick}test → build/push → deploy${tick} の順に GitHub Actions が実行されます。",
    "- deploy job は ${tick}ACA_DEPLOY_ENABLED=true${tick} かつ必要な repository variables / secrets が揃っている場合だけ実行されます。",
    "- bootstrap 時に ${tick}create_aca=false${tick} を選んだ場合でも workflow 自体は残り、ACA の自動作成だけが skip されます。",
    "",
    "### ブランチと ACA デプロイ先の対応",
    "",
    "| Push 先ブランチ | deploy job | 参照する variable | 入力する値 / 例 |",
    "| --- | --- | --- | --- |",
    "| ${tick}develop${tick} | ${tick}deploy-develop-aca${tick} | ${tick}ACA_APP_NAME_DEV${tick} | ${tick}<aca_name_base>-develop${tick}（例: ${tick}${ReadmeProjectSlug}-develop${tick}） |",
    "| ${tick}copilot/**${tick} | ${tick}deploy-copilot-aca${tick} | ${tick}ACA_APP_NAME_COPILOT${tick} | ${tick}<aca_name_base>-copilot${tick}（例: ${tick}${ReadmeProjectSlug}-copilot${tick}） |",
    "| ${tick}main${tick} | ${tick}deploy-main-aca${tick} | ${tick}ACA_APP_NAME_PROD${tick} | ${tick}<aca_name_base>${tick}（例: ${tick}${ReadmeProjectSlug}${tick}） |",
    "",
    "### 自動 ACA デプロイの必須 repository variables",
    "",
    "| Variable | 推奨値 | 用途 |",
    "| --- | --- | --- |",
    "| ${tick}ACA_DEPLOY_ENABLED${tick} | ${tick}true${tick} | push 時に deploy job を有効化します。${tick}false${tick} のままでは build までで停止します。 |",
    "| ${tick}AZURE_RESOURCE_GROUP${tick} | ${tick}${ReadmeProjectSlug}-rg${tick} または実際の RG 名 | ACA を配置している Azure Resource Group 名です。 |",
    "| ${tick}ACA_APP_NAME_DEV${tick} | ${tick}<aca_name_base>-develop${tick}（例: ${tick}${ReadmeProjectSlug}-develop${tick}） | ${tick}develop${tick} push のデプロイ先 ACA 名です。bootstrap が ACA を自動作成した場合は実値が設定されます。 |",
    "| ${tick}ACA_APP_NAME_COPILOT${tick} | ${tick}<aca_name_base>-copilot${tick}（例: ${tick}${ReadmeProjectSlug}-copilot${tick}） | ${tick}copilot/**${tick} push のデプロイ先 ACA 名です。 |",
    "| ${tick}ACA_APP_NAME_PROD${tick} | ${tick}<aca_name_base>${tick}（例: ${tick}${ReadmeProjectSlug}${tick}） | ${tick}main${tick} push のデプロイ先 ACA 名です。 |",
    "| ${tick}DEV_NEXTAUTH_URL_ACA${tick} | ${tick}https://<develop-app-fqdn>${tick} | ${tick}develop${tick} デプロイ時の ${tick}NEXTAUTH_URL${tick} です。 |",
    "| ${tick}COPILOT_NEXTAUTH_URL_ACA${tick} | ${tick}https://<copilot-app-fqdn>${tick} | ${tick}copilot/**${tick} デプロイ時の ${tick}NEXTAUTH_URL${tick} です。 |",
    "| ${tick}PROD_NEXTAUTH_URL_ACA${tick} | ${tick}https://<production-app-fqdn>${tick} | ${tick}main${tick} デプロイ時の ${tick}NEXTAUTH_URL${tick} です。 |",
    "",
    "### Next.js build / deploy の必須 secrets",
    "",
    "| Secret | 値 | 用途 |",
    "| --- | --- | --- |",
    "| ${tick}DEV_API_URL${tick} | ${tick}https://<development-api-base-url>${tick} | ${tick}develop${tick} / ${tick}copilot/**${tick} build が参照する API URL です。 |",
    "| ${tick}PROD_API_URL${tick} | ${tick}https://<production-api-base-url>${tick} | ${tick}main${tick} build が参照する API URL です。 |",
    "| ${tick}DEV_GOOGLE_REDIRECT_URI${tick} | ${tick}https://<develop-app-domain>/api/auth/callback/google${tick} | ${tick}develop${tick} / ${tick}copilot/**${tick} build が参照する Google redirect URI です。 |",
    "| ${tick}PROD_GOOGLE_REDIRECT_URI${tick} | ${tick}https://<production-app-domain>/api/auth/callback/google${tick} | ${tick}main${tick} build が参照する Google redirect URI です。 |",
    "| ${tick}GOOGLE_CLIENT_ID${tick} | Google OAuth client ID | 全環境共通の Google OAuth client ID です。 |",
    "| ${tick}GOOGLE_CLIENT_SECRET${tick} | Google OAuth client secret | ACA へ secret として渡す Google OAuth client secret です。 |",
    "| ${tick}NEXTAUTH_SECRET${tick} | ${tick}32 byte 以上のランダム文字列${tick} | ACA へ secret として渡す NextAuth secret です。${tick}create_aca=true${tick} なら bootstrap が自動生成し、${tick}create_aca=false${tick} なら手動設定が必要です。 |",
    "",
    "### Azure 認証の必須 secrets",
    "",
    "| Secret | 値 | 用途 |",
    "| --- | --- | --- |",
    ("| {0} | {1} | Azure Service Principal JSON を使う場合の認証情報です。OIDC を使う場合は未設定で構いません。 |" -f "${tick}AZURE_CREDENTIALS${tick}", "${tick}{`"clientId`":`"...`",`"clientSecret`":`"...`",`"subscriptionId`":`"...`",`"tenantId`":`"...`"}${tick}"),
    "| ${tick}AZURE_CLIENT_ID${tick} | Microsoft Entra アプリ登録の Client ID | OIDC を使う場合に必要です。${tick}AZURE_CREDENTIALS${tick} を使う場合は不要です。 |",
    "| ${tick}AZURE_TENANT_ID${tick} | Azure tenant GUID | OIDC を使う場合に必要です。 |",
    "| ${tick}AZURE_SUBSCRIPTION_ID${tick} | Azure subscription GUID | OIDC を使う場合に必要です。 |",
    "",
    "### 手入力が必要な項目",
    "",
    "| 項目 | 手入力が必要な条件 | 入力する値 |",
    "| --- | --- | --- |",
    "| ${tick}DEV_API_URL${tick} / ${tick}PROD_API_URL${tick} | 常に必要 | 実際のバックエンド API の URL |",
    "| ${tick}DEV_GOOGLE_REDIRECT_URI${tick} / ${tick}PROD_GOOGLE_REDIRECT_URI${tick} | 常に必要 | Google OAuth に登録した redirect URI |",
    "| ${tick}GOOGLE_CLIENT_ID${tick} / ${tick}GOOGLE_CLIENT_SECRET${tick} | 常に必要 | Google OAuth クライアントの実値 |",
    "| ${tick}NEXTAUTH_SECRET${tick} | ${tick}create_aca=false${tick} で bootstrap した場合、または自動生成値を置き換えたい場合 | 32 byte 以上のランダム文字列 |",
    "| ${tick}AZURE_CREDENTIALS${tick} | Service Principal JSON 認証を使い、bootstrap がこの secret を同期していない場合 | Azure Service Principal JSON |",
    "| ${tick}AZURE_CLIENT_ID${tick} / ${tick}AZURE_TENANT_ID${tick} / ${tick}AZURE_SUBSCRIPTION_ID${tick} | OIDC 認証を使い、bootstrap がこれらを同期していない場合 | Azure OIDC 用の実値 |",
    "| ${tick}ACA_DEPLOY_ENABLED${tick} / ${tick}AZURE_RESOURCE_GROUP${tick} / ${tick}ACA_APP_NAME_*${tick} / ${tick}*_NEXTAUTH_URL_ACA${tick} | ${tick}create_aca=false${tick} で bootstrap した場合、または手動作成した ACA に切り替える場合 | 実際の Azure Resource Group 名、Container App 名、FQDN |",
    "",
    "ACA を自動作成する場合は、bootstrap workflow の ${tick}aca_name_base${tick} を **1 つだけ手入力** します。そこから ${tick}<base>-aca-env${tick} / ${tick}<base>-develop${tick} / ${tick}<base>-copilot${tick} / ${tick}<base>${tick} が自動派生します。${tick}aca_name_base${tick} は 23 文字以下、英小文字・数字・${tick}-${tick} のみ、先頭は英字、末尾は英数字、${tick}--${tick} 禁止です。",
    "",
    "bootstrap は ${tick}DEV_API_URL${tick} / ${tick}PROD_API_URL${tick} / ${tick}DEV_GOOGLE_REDIRECT_URI${tick} / ${tick}PROD_GOOGLE_REDIRECT_URI${tick} / ${tick}GOOGLE_CLIENT_ID${tick} / ${tick}GOOGLE_CLIENT_SECRET${tick} に ${tick}REPLACE_ME${tick} を設定します。これらは必ず手入力で実値に置き換えてください。",
    "",
    "${tick}AZURE_CREDENTIALS${tick} を使う方法と、${tick}AZURE_CLIENT_ID${tick} / ${tick}AZURE_TENANT_ID${tick} / ${tick}AZURE_SUBSCRIPTION_ID${tick} を使う方法の **どちらか一方** を設定してください。",
    "",
    "${tick}create_aca=true${tick} で bootstrap した場合は、${tick}ACA_*${tick} / ${tick}AZURE_RESOURCE_GROUP${tick} / ${tick}*_NEXTAUTH_URL_ACA${tick} / ${tick}NEXTAUTH_SECRET${tick} の大半が自動設定されます。",
    "",
    "## 5. Guardrails",
    "",
    "- bootstrap は ${tick}main${tick} / ${tick}develop${tick} の reviewed PR guardrails 適用を best-effort で試行します。",
    "- GitHub Free の private repository など ruleset API を使えない条件では、生成は継続しつつ bootstrap summary / log に未適用理由が記録されます。",
    "- 後から手動適用する場合は、repository を public にするか GitHub Pro+ 相当の条件を満たした上で次を実行してください。",
    "",
    '```powershell',
    "pwsh ./scripts/configure-github-guardrails.ps1 -Owner <owner> -Repo <repo>",
    '```'
  )

  return ($lines -join [Environment]::NewLine)
}

function Get-GeneratedReadmeContent {
  param(
    [string]$ProjectName,
    [string]$Framework,
    [int]$PortBase
  )

  if ([string]::IsNullOrWhiteSpace($Framework)) {
    throw "Framework is required to generate README content."
  }

  $normalizedFramework = $Framework.Trim().ToLowerInvariant()
  $readmeProjectSlug = Convert-ToSlug -Value $ProjectName

  switch ($normalizedFramework) {
    "dotnet8" {
      return Get-DotnetGeneratedReadmeContent -ProjectName $ProjectName -PortBase $PortBase -ReadmeProjectSlug $readmeProjectSlug
    }
    "dotnet10" {
      return Get-DotnetGeneratedReadmeContent -ProjectName $ProjectName -PortBase $PortBase -ReadmeProjectSlug $readmeProjectSlug
    }
    "next" {
      return Get-NextGeneratedReadmeContent -ProjectName $ProjectName -ReadmeProjectSlug $readmeProjectSlug
    }
    default {
      throw "Unsupported framework '$Framework' for README generation. Supported values: dotnet8, dotnet10, next."
    }
  }
}

function Update-GeneratedReadme {
  param(
    [string]$Path,
    [string]$ProjectName,
    [string]$Framework,
    [int]$PortBase
  )

  $content = Get-GeneratedReadmeContent -ProjectName $ProjectName -Framework $Framework -PortBase $PortBase

  if ([string]::IsNullOrWhiteSpace($content)) {
    throw "Generated README content is empty for framework '$Framework'."
  }

  Write-Utf8TextNoNewline -Path $Path -Value $content.TrimEnd("`r", "`n")
}

$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectSlug = Convert-ToSlug -Value $ProjectName
$projectNameLower = $ProjectName.ToLowerInvariant()
$projectWebName = "$ProjectName.Web"
$projectWebServiceName = "$projectSlug-web"

$frameworkLabelMap = @{
  "dotnet8"  = "Dotnet8"
  "dotnet10" = "Dotnet10"
  "next"     = "Next"
}

$templateProjectNameMap = @{
  "dotnet8"  = "PokenaeTemplate"
  "dotnet10" = "PokenaeTemplate"
}

$templateWebProjectNameMap = @{
  "dotnet8"  = "PokenaeTemplate.Web"
  "dotnet10" = "PokenaeTemplate.Web"
}

$templateWebServiceNameMap = @{
  "dotnet8"  = "pokenaetemplate-web"
  "dotnet10" = "pokenaetemplate-web"
}

$templateSolutionNameMap = @{
  "dotnet8"  = "PokenaeTemplate"
  "dotnet10" = "PokenaeTemplate"
}

Write-Host "[INFO] ProjectName:      $ProjectName"
Write-Host "[INFO] ProjectSlug:      $projectSlug"
Write-Host "[INFO] ProjectNameLower: $projectNameLower"
Write-Host "[INFO] Framework:        $Framework"
Write-Host "[INFO] PortBase:         $PortBase"

# ── 1. テンプレートファイルをルートにコピー ──
$templateDir = Join-Path $root "templates/$Framework"
if (Test-Path $templateDir) {
  Get-ChildItem -LiteralPath $templateDir -Force | Where-Object {
    $_.Name -notin @(".gitkeep", ".vs", "bin", "obj", "node_modules")
  } | ForEach-Object {
    $dest = Join-Path $root $_.Name
    if ($_.PSIsContainer) {
      Copy-TemplateTree -Source $_.FullName -Destination $dest
    } else {
      Copy-Item -LiteralPath $_.FullName -Destination $dest -Force
    }
    Write-Host "[INFO] Copied template: $($_.Name)"
  }
} else {
  Write-Warning "Template directory not found: $templateDir"
}

# ── 2. 不要なフレームワークの compose ファイルを削除 ──
$allFrameworks = @("dotnet8", "dotnet10", "next")
$otherFrameworks = $allFrameworks | Where-Object { $_ -ne $Framework }

foreach ($other in $otherFrameworks) {
  @(
    "docker-compose.$other.yml",
    "docker-compose.$other.debug.yml",
    "docker-compose.$other.dev.yml",
    "docker-compose.$other.prod.yml"
  ) | ForEach-Object {
    $path = Join-Path $root $_
    if (Test-Path $path) {
      Remove-Item $path -Force
      Write-Host "[INFO] Removed: $_"
    }
  }

  # 不要なフレームワークの Dockerfile を削除
  $otherDockerfile = Join-Path $root "docker/Dockerfile.$other"
  if (Test-Path $otherDockerfile) {
    Remove-Item $otherDockerfile -Force
    Write-Host "[INFO] Removed: docker/Dockerfile.$other"
  }
}

# ── 3. テキスト置換マップ ──
# @{} はデフォルトで大文字小文字を区別しないため、Ordinal 比較の Dictionary を使用
$replaceMap = [System.Collections.Generic.Dictionary[string, string]]::new([StringComparer]::Ordinal)
# ソリューション名
$replaceMap[$OldSolutionName]                       = $ProjectName
$templateSolutionName = $templateSolutionNameMap[$Framework]
if ($templateSolutionName) {
  $replaceMap[$templateSolutionName] = $ProjectName
}
# プロジェクト名（PascalCase / lowercase）
$templateProjectName = $templateProjectNameMap[$Framework]
$templateWebProjectName = $templateWebProjectNameMap[$Framework]
$templateWebServiceName = $templateWebServiceNameMap[$Framework]
if ($templateProjectName) {
  $replaceMap[$templateProjectName] = $ProjectName
  $replaceMap[$templateProjectName.ToLowerInvariant()] = $projectNameLower
}
if ($templateWebProjectName) {
  $replaceMap[$templateWebProjectName] = $projectWebName
}
if ($templateWebServiceName) {
  $replaceMap[$templateWebServiceName] = $projectWebServiceName
}
# Next.js 関連スラグ
$replaceMap[$OldSlug]                               = $projectSlug
$replaceMap["pokenae-web"]                          = "$projectSlug-web"
$replaceMap["pokenae-debug"]                        = "$projectSlug-debug"
$replaceMap["pokenae-dev"]                          = "$projectSlug-dev"
$replaceMap["pokenae-prod"]                         = "$projectSlug-prod"
# Compose ファイル名リネーム（テキスト参照の更新）
$replaceMap["docker-compose.$Framework.yml"]        = "docker-compose.yml"
$replaceMap["docker-compose.$Framework.debug.yml"]  = "docker-compose.override.yml"
$replaceMap["docker-compose.$Framework.dev.yml"]    = "docker-compose.dev.yml"
$replaceMap["docker-compose.$Framework.prod.yml"]   = "docker-compose.prod.yml"
# Compose プロジェクト名からフレームワーク名を除去
$replaceMap["pokenae-$Framework"]                   = $projectSlug
# Dockerfile パスの置換（dotnet 系のみ、docker/ → プロジェクトディレクトリ）
if ($Framework -ne "next") {
  $replaceMap["./docker/Dockerfile.$Framework"] = "./$templateWebProjectName/Dockerfile"
  $replaceMap["docker/Dockerfile.$Framework"] = "$templateWebProjectName/Dockerfile"

  # docker-compose 内のデフォルト公開ポートも PortBase に合わせて更新
  $replaceMap['${APP_HTTP_PORT:-18080}'] = "`$`{APP_HTTP_PORT:-$($PortBase + 80)}"
  $replaceMap['${APP_HTTPS_PORT:-18081}'] = "`$`{APP_HTTPS_PORT:-$($PortBase + 81)}"
  $replaceMap['${DB_HOST_PORT:-18033}'] = "`$`{DB_HOST_PORT:-$($PortBase + 33)}"
  $replaceMap['${TEST_DB_HOST_PORT:-18034}'] = "`$`{TEST_DB_HOST_PORT:-$($PortBase + 34)}"
  $replaceMap['${APP_HTTP_PORT:-18180}'] = "`$`{APP_HTTP_PORT:-$($PortBase + 180)}"
  $replaceMap['${APP_HTTPS_PORT:-18181}'] = "`$`{APP_HTTPS_PORT:-$($PortBase + 181)}"
  $replaceMap['${APP_HTTP_PORT:-18280}'] = "`$`{APP_HTTP_PORT:-$($PortBase + 280)}"
  $replaceMap['${APP_HTTPS_PORT:-18281}'] = "`$`{APP_HTTPS_PORT:-$($PortBase + 281)}"
}
# タスクラベルからフレームワーク表示名を除去
$replaceMap["$($frameworkLabelMap[$Framework]) "]   = ""

# ── 4. テキスト置換の実行 ──
$includeExtensions = @(
  ".yml", ".yaml", ".json", ".md", ".env", ".txt", ".props", ".targets",
  ".csproj", ".sln", ".ps1", ".sh", ".cs", ".dcproj", ".http"
)

$files = Get-ChildItem -Path $root -Recurse -File | Where-Object {
  $_.FullName -notmatch "\\.git\\" -and
  $_.FullName -notmatch "\\node_modules\\" -and
  $_.FullName -notmatch "\\.next\\" -and
  $_.FullName -notmatch "\\bin\\" -and
  $_.FullName -notmatch "\\obj\\" -and
  ($includeExtensions -contains $_.Extension.ToLowerInvariant() -or $_.Name -like ".env*" -or $_.Name -like "Dockerfile*")
}

$changedCount = 0
foreach ($f in $files) {
  if (Replace-InFile -Path $f.FullName -Map $replaceMap) {
    $changedCount++
  }
}

Write-Host "[INFO] Updated files: $changedCount"

if ($Framework -ne "next") {
  $debugEnvPath = Join-Path $root ".env.docker.debug"
  $developmentEnvPath = Join-Path $root ".env.docker.development"
  $productionEnvPath = Join-Path $root ".env.docker.production"
  $testEnvPath = Join-Path $root ".env.docker.test"

  Set-OrAddEnvVar -Path $debugEnvPath -Name "APP_HTTP_PORT" -Value ($PortBase + 80)
  Set-OrAddEnvVar -Path $debugEnvPath -Name "APP_HTTPS_PORT" -Value ($PortBase + 81)
  Set-OrAddEnvVar -Path $debugEnvPath -Name "DB_HOST_PORT" -Value ($PortBase + 33)

  Set-OrAddEnvVar -Path $developmentEnvPath -Name "APP_HTTP_PORT" -Value ($PortBase + 180)
  Set-OrAddEnvVar -Path $developmentEnvPath -Name "APP_HTTPS_PORT" -Value ($PortBase + 181)
  Remove-EnvVar -Path $developmentEnvPath -Name "DB_HOST_PORT"

  Set-OrAddEnvVar -Path $productionEnvPath -Name "APP_HTTP_PORT" -Value ($PortBase + 280)
  Set-OrAddEnvVar -Path $productionEnvPath -Name "APP_HTTPS_PORT" -Value ($PortBase + 281)
  Remove-EnvVar -Path $productionEnvPath -Name "DB_HOST_PORT"

  if (Test-Path $testEnvPath) {
    Set-OrAddEnvVar -Path $testEnvPath -Name "TEST_DB_HOST_PORT" -Value ($PortBase + 34)
  }

  Write-Host "[INFO] Applied PortBase offsets to .env.docker.* files"
}

$readmePath = Join-Path $root "README.md"
if (Test-Path $readmePath) {
  Update-GeneratedReadme -Path $readmePath -ProjectName $ProjectName -Framework $Framework -PortBase $PortBase
  Write-Host "[INFO] Updated README.md"
}

# ── 5. docker-compose ファイルの物理リネーム ──
$composeRenames = [ordered]@{
  "docker-compose.$Framework.yml"       = "docker-compose.yml"
  "docker-compose.$Framework.debug.yml" = "docker-compose.override.yml"
  "docker-compose.$Framework.dev.yml"   = "docker-compose.dev.yml"
  "docker-compose.$Framework.prod.yml"  = "docker-compose.prod.yml"
}

foreach ($entry in $composeRenames.GetEnumerator()) {
  $srcPath = Join-Path $root $entry.Key
  $dstPath = Join-Path $root $entry.Value
  if (Test-Path $srcPath) {
    Move-Item -LiteralPath $srcPath -Destination $dstPath -Force
    Write-Host "[INFO] Renamed: $($entry.Key) -> $($entry.Value)"
  }
}

# ── 6. tasks.json から不要なフレームワークのタスクを除去 ──
$tasksPath = Join-Path $root ".vscode/tasks.json"
if (Test-Path $tasksPath) {
  $tasksJson = Get-Content $tasksPath -Raw -Encoding UTF8 | ConvertFrom-Json
  $filteredTasks = @()

  foreach ($task in $tasksJson.tasks) {
    $keep = $true
    foreach ($other in $otherFrameworks) {
      $otherLabel = $frameworkLabelMap[$other]
      if ($task.label -match $otherLabel) {
        $keep = $false
        break
      }
    }

    if ($keep) {
      # "All Down" タスクから不要なフレームワークのコマンドを除去
      if ($task.label -match "All Down" -and $task.command) {
        $commands = $task.command -split ";\s*"
        $cleanedCommands = @()
        foreach ($cmd in $commands) {
          $shouldKeep = $true
          foreach ($other in $otherFrameworks) {
            if ($cmd -match $other) {
              $shouldKeep = $false
              break
            }
          }
          if ($shouldKeep -and $cmd.Trim()) {
            $cleanedCommands += $cmd.Trim()
          }
        }
        $task.command = $cleanedCommands -join "; "
      }
      $filteredTasks += $task
    }
  }

  $tasksJson.tasks = $filteredTasks
  Write-Utf8TextNoNewline -Path $tasksPath -Value ($tasksJson | ConvertTo-Json -Depth 10)
  Write-Host "[INFO] Cleaned up tasks.json"
}

# ── 7. クリーンアップ ──
# 選択フレームワークの Dockerfile を docker/ から削除（テンプレートのものを使用）
$selectedDockerfile = Join-Path $root "docker/Dockerfile.$Framework"
if (Test-Path $selectedDockerfile) {
  Remove-Item $selectedDockerfile -Force
  Write-Host "[INFO] Removed: docker/Dockerfile.$Framework"
}

if ($Framework -ne "next") {
  $nextEntrypoint = Join-Path $root "docker/entrypoint.sh"
  if (Test-Path $nextEntrypoint) {
    Remove-Item $nextEntrypoint -Force
    Write-Host "[INFO] Removed: docker/entrypoint.sh"
  }
}

# docker/ ディレクトリが空なら削除
$dockerDir = Join-Path $root "docker"
if ((Test-Path $dockerDir) -and -not (Get-ChildItem -Path $dockerDir -Force)) {
  Remove-Item $dockerDir -Force
  Write-Host "[INFO] Removed empty docker directory"
}

if ($Framework -eq "next") {
  $dotnetTestCompose = Join-Path $root "docker-compose.dotnet.test-db.yml"
  if (Test-Path $dotnetTestCompose) {
    Remove-Item $dotnetTestCompose -Force
    Write-Host "[INFO] Removed: docker-compose.dotnet.test-db.yml"
  }
}

# templates ディレクトリの削除
$templatesDir = Join-Path $root "templates"
if (Test-Path $templatesDir) {
  Remove-Item $templatesDir -Recurse -Force
  Write-Host "[INFO] Removed templates directory"
}

# scripts ディレクトリから bootstrap 自身のみを削除し、guardrail 再実行用スクリプトは残す
$bootstrapScriptPath = Join-Path $root "scripts/bootstrap-template.ps1"
if (Test-Path $bootstrapScriptPath) {
  Remove-Item $bootstrapScriptPath -Force
  Write-Host "[INFO] Removed scripts/bootstrap-template.ps1"
}

$scriptsDir = Join-Path $root "scripts"
if ((Test-Path $scriptsDir) -and -not (Get-ChildItem -Path $scriptsDir -Force)) {
  Remove-Item $scriptsDir -Force
  Write-Host "[INFO] Removed empty scripts directory"
}

$bootstrapWorkflowPath = Join-Path $root ".github/workflows/template-bootstrap.yml"
if (Test-Path $bootstrapWorkflowPath) {
  Remove-Item $bootstrapWorkflowPath -Force
  Write-Host "[INFO] Removed .github/workflows/template-bootstrap.yml"
}

$reusableNextBuildWorkflow = Join-Path $root ".github/workflows/reusable-build-push.yml"
$reusableDotnetBuildWorkflow = Join-Path $root ".github/workflows/reusable-build-push-dotnet.yml"

if ($Framework -eq "next") {
  if (Test-Path $reusableDotnetBuildWorkflow) {
    Remove-Item $reusableDotnetBuildWorkflow -Force
    Write-Host "[INFO] Removed .github/workflows/reusable-build-push-dotnet.yml"
  }
} else {
  if (Test-Path $reusableNextBuildWorkflow) {
    Remove-Item $reusableNextBuildWorkflow -Force
    Write-Host "[INFO] Removed .github/workflows/reusable-build-push.yml"
  }
}

# ── 8. ファイル・ディレクトリのリネーム ──
# Solution ファイル
Get-ChildItem -Path $root -Recurse -Filter "*.sln" -File | Where-Object {
  $_.BaseName -match [Regex]::Escape($OldSolutionName) -or
  ($templateSolutionName -and $_.BaseName -match [Regex]::Escape($templateSolutionName))
} | ForEach-Object {
  $newBaseName = $_.BaseName
  if ($newBaseName -match [Regex]::Escape($OldSolutionName)) {
    $newBaseName = $newBaseName -replace [Regex]::Escape($OldSolutionName), $ProjectName
  }
  if ($templateSolutionName -and $newBaseName -match [Regex]::Escape($templateSolutionName)) {
    $newBaseName = $newBaseName -replace [Regex]::Escape($templateSolutionName), $ProjectName
  }
  $newName = $newBaseName + ".sln"
  $newPath = Join-Path $_.Directory.FullName $newName
  if ($newPath -ne $_.FullName) {
    Move-Item -LiteralPath $_.FullName -Destination $newPath -Force
    Write-Host "[INFO] Renamed solution: $($_.Name) -> $newName"
  }
}

# .NET プロジェクトファイル
$renameTargets = @("*.csproj", "*.dcproj", "*.http")
foreach ($pattern in $renameTargets) {
  Get-ChildItem -Path $root -Recurse -Filter $pattern -File | Where-Object {
    $templateProjectName -and $_.BaseName -match [Regex]::Escape($templateProjectName)
  } | ForEach-Object {
    $newName = $_.Name -replace [Regex]::Escape($templateProjectName), $ProjectName
    $newPath = Join-Path $_.Directory.FullName $newName
    if ($newPath -ne $_.FullName) {
      Move-Item -LiteralPath $_.FullName -Destination $newPath -Force
      Write-Host "[INFO] Renamed file: $($_.Name) -> $newName"
    }
  }
}

# テンプレートのプロジェクトディレクトリ（深い順にリネーム）
Get-ChildItem -Path $root -Recurse -Directory | Where-Object {
  $templateProjectName -and (
    $_.Name -eq $templateProjectName -or
    $_.Name.StartsWith("$templateProjectName.", [System.StringComparison]::Ordinal)
  )
} | Sort-Object { $_.FullName.Length } -Descending | ForEach-Object {
  $newName = $_.Name -replace ('^' + [Regex]::Escape($templateProjectName)), $ProjectName
  $newPath = Join-Path $_.Parent.FullName $newName
  if ($newPath -ne $_.FullName) {
    Move-Item -LiteralPath $_.FullName -Destination $newPath -Force
    Write-Host "[INFO] Renamed directory: $($_.FullName) -> $newPath"
  }
}

Write-Host "[SUCCESS] bootstrap-template.ps1 completed."
