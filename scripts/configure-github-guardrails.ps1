param(
  [Parameter(Mandatory = $true)]
  [string]$Owner,

  [Parameter(Mandatory = $true)]
  [string]$Repo,

  [string]$RulesetName = "Copilot CLI branch guardrails",

  [string[]]$ProtectedBranches = @("main", "develop"),

  [ValidateRange(1, 6)]
  [int]$RequiredApprovingReviewCount = 1,

  [ValidateSet("active", "disabled", "evaluate")]
  [string]$Enforcement = "active",

  [switch]$BestEffort,

  [switch]$WhatIfMode
)

$ErrorActionPreference = "Stop"

function Get-GhExecutable {
  $command = Get-Command gh -ErrorAction SilentlyContinue
  if ($null -ne $command) {
    return $command.Source
  }

  $candidates = @(
    "C:\Program Files\GitHub CLI\gh.exe",
    (Join-Path $env:LOCALAPPDATA "Programs\GitHub CLI\gh.exe")
  )

  foreach ($candidate in $candidates) {
    if (-not [string]::IsNullOrWhiteSpace($candidate) -and (Test-Path $candidate)) {
      return $candidate
    }
  }

  throw "GitHub CLI (gh) が見つかりません。先に gh をインストールしてください。"
}

function Invoke-GhApi {
  param(
    [string]$Method,
    [string]$Path,
    [string]$InputFile
  )

  $args = @(
    "api",
    "--method", $Method,
    "-H", "Accept: application/vnd.github+json",
    "-H", "X-GitHub-Api-Version: 2022-11-28",
    $Path
  )

  if (-not [string]::IsNullOrWhiteSpace($InputFile)) {
    $args += @("--input", $InputFile)
  }

  $output = & $script:GhExe @args 2>&1
  if ($LASTEXITCODE -ne 0) {
    if (
      $output -match "Upgrade to GitHub Pro or make this repository public to enable this feature" -or
      $output -match "rulesets are only available" -or
      $output -match "Repository rule enforcement is not available" -or
      $output -match "Resource not accessible by integration"
    ) {
      throw "Guardrails could not be applied because repository rulesets require a public repository or GitHub Pro+ equivalent access."
    }
    throw "gh api $Method $Path failed. $output"
  }

  return $output
}

function Set-GitHubOutput {
  param(
    [string]$Name,
    [string]$Value
  )

  if ([string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
    return
  }

  Add-Content -LiteralPath $env:GITHUB_OUTPUT -Value "$Name=$Value"
}

function Publish-GuardrailResult {
  param(
    [string]$Status,
    [string]$Message
  )

  Set-GitHubOutput -Name "status" -Value $Status
  Set-GitHubOutput -Name "message" -Value $Message
}

function Assert-GhAuth {
  if ((Test-Path Env:GH_TOKEN) -or (Test-Path Env:GITHUB_TOKEN)) {
    return
  }

  & $script:GhExe auth status 1>$null 2>$null
  if ($LASTEXITCODE -ne 0) {
    throw "gh auth login または GH_TOKEN / GITHUB_TOKEN の設定が必要です。"
  }
}

function Convert-ToRulesetRefName {
  param([string]$Branch)

  if ([string]::IsNullOrWhiteSpace($Branch)) {
    throw "ProtectedBranches には空文字を指定できません。"
  }

  $normalizedBranch = $Branch.Trim()
  if (
    $normalizedBranch.StartsWith("refs/", [System.StringComparison]::OrdinalIgnoreCase) -or
    $normalizedBranch.StartsWith("~", [System.StringComparison]::Ordinal)
  ) {
    return $normalizedBranch
  }

  return "refs/heads/$normalizedBranch"
}

try {
  $script:GhExe = Get-GhExecutable
  Assert-GhAuth

  $protectedRefNames = @(
    $ProtectedBranches |
      ForEach-Object { Convert-ToRulesetRefName -Branch $_ } |
      Select-Object -Unique
  )

  $rulesetBody = @{
    name = $RulesetName
    target = "branch"
    enforcement = $Enforcement
    conditions = @{
      ref_name = @{
        include = $protectedRefNames
        exclude = @()
      }
    }
    rules = @(
      @{
        type = "pull_request"
        parameters = @{
          allowed_merge_methods = @("squash")
          dismiss_stale_reviews_on_push = $true
          require_code_owner_review = $false
          require_last_push_approval = $false
          required_approving_review_count = $RequiredApprovingReviewCount
          required_review_thread_resolution = $true
        }
      },
      @{
        type = "non_fast_forward"
      },
      @{
        type = "deletion"
      },
      @{
        type = "required_linear_history"
      }
    )
  }

  if ($WhatIfMode) {
    $rulesetBody | ConvertTo-Json -Depth 10
    exit 0
  }

  $existingRulesets = Invoke-GhApi -Method "GET" -Path "repos/$Owner/$Repo/rulesets?includes_parents=false&targets=branch" | ConvertFrom-Json
  $existingRuleset = $existingRulesets | Where-Object { $_.name -eq $RulesetName } | Select-Object -First 1

  $tempFile = [System.IO.Path]::GetTempFileName()

  try {
    [System.IO.File]::WriteAllText(
      $tempFile,
      ($rulesetBody | ConvertTo-Json -Depth 10),
      [System.Text.UTF8Encoding]::new($false)
    )

    if ($null -ne $existingRuleset) {
      $response = Invoke-GhApi -Method "PATCH" -Path "repos/$Owner/$Repo/rulesets/$($existingRuleset.id)" -InputFile $tempFile | ConvertFrom-Json
      Write-Host "[INFO] Updated ruleset: $($response.name)"
    } else {
      $response = Invoke-GhApi -Method "POST" -Path "repos/$Owner/$Repo/rulesets" -InputFile $tempFile | ConvertFrom-Json
      Write-Host "[INFO] Created ruleset: $($response.name)"
    }

    if ($null -ne $response._links.html.href) {
      Write-Host "[INFO] Ruleset URL: $($response._links.html.href)"
    }

    Publish-GuardrailResult -Status "applied" -Message "Reviewed-PR guardrails were applied to main/develop."
  }
  finally {
    if (Test-Path $tempFile) {
      Remove-Item $tempFile -Force
    }
  }
}
catch {
  $message = $_.Exception.Message

  if ($BestEffort -and $message -match "public repository or GitHub Pro\+ equivalent access") {
    Write-Warning $message
    Publish-GuardrailResult -Status "skipped" -Message "$message Bootstrap continues without failing generation."
    exit 0
  }

  throw
}
