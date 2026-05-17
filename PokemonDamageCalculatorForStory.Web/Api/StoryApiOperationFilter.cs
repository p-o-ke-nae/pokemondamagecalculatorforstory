using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PokemonDamageCalculatorForStory.Web.Api;

internal sealed class StoryApiOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod?.ToUpperInvariant();
        var path = NormalizePath(context.ApiDescription.RelativePath);
        if (method is null || path is null)
        {
            return;
        }

        var key = $"{method} {path}";
        if (!TryGetDocumentation(key, out var doc))
        {
            return;
        }

        operation.Summary = doc.Summary;
        operation.Description = BuildDescription(doc);
        operation.Tags =
        [
            new OpenApiTag { Name = doc.Tag }
        ];
    }

    private static string? NormalizePath(string? relativePath)
        => relativePath?
            .Split('?', 2)[0]
            .Replace(":guid", string.Empty, StringComparison.Ordinal)
            .Trim()
            .Trim('/');

    private static string BuildDescription(StoryApiOperationDoc doc)
    {
        var lines = new List<string>
        {
            "### ユースケース",
            doc.UseCase,
            string.Empty,
            "### 入力例",
            doc.Example
        };

        if (!string.IsNullOrWhiteSpace(doc.Notes))
        {
            lines.Add(string.Empty);
            lines.Add("### 補足");
            lines.Add(doc.Notes);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static bool TryGetDocumentation(string key, out StoryApiOperationDoc doc)
    {
        doc = key switch
        {
            "GET api/rulesets" => UrlDoc("Rulesets", "ルールセット一覧を取得", "新しい攻略 run を始める前に、利用可能なタイトル・世代・ルールセットを選ぶための API です。", "GET /api/rulesets"),
            "GET api/rulesets/{rulesetId}" => UrlDoc("Rulesets", "ルールセット詳細を取得", "選択した ruleset の詳細を確認し、run 作成時に使うルールセットを確定するための API です。", "GET /api/rulesets/11111111-1111-1111-1111-111111111111"),
            "GET api/master-version-sets/{versionSetId}" => UrlDoc("Rulesets", "固定済み version set を取得", "計算再現性の根拠となる master version set を確認するための API です。", "GET /api/master-version-sets/22222222-2222-2222-2222-222222222222"),

            "GET api/runs" => UrlDoc("Runs", "自分の run 一覧を取得", "攻略メモとして保存済みの run を一覧表示し、再開対象を選ぶための API です。", "GET /api/runs"),
            "POST api/runs" => JsonDoc("Runs", "run を作成", "選んだ ruleset を固定して新しい攻略 run を開始するための API です。", """
                {
                  "rulesetId": "11111111-1111-1111-1111-111111111111",
                  "name": "Emerald Gym 2 prep"
                }
                """),
            "GET api/runs/{runId}" => UrlDoc("Runs", "run 詳細を取得", "現在の run 名、初期状態、route 一覧などの編集対象を取得するための API です。", "GET /api/runs/aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
            "PUT api/runs/{runId}" => JsonDoc("Runs", "run の基本情報を更新", "run 名や進行ステータスを更新し、草案・進行中・完了などの管理に使う API です。", """
                {
                  "name": "Emerald Gym 2 clear route",
                  "status": "active"
                }
                """),
            "GET api/runs/{runId}/initial-state" => UrlDoc("Runs", "初期状態を取得", "手持ち、所持金、PP の基準値を確認して route 再計算の入力を揃えるための API です。", "GET /api/runs/aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb/initial-state"),
            "PUT api/runs/{runId}/initial-state" => JsonDoc("Runs", "初期状態を登録・更新", "攻略開始時点の party・個体値・努力値・所持技をまとめて登録する API です。", """
                {
                  "baselineMoney": 3200,
                  "baselineParty": [
                    {
                      "slot": 1,
                      "species": "Pikachu",
                      "level": 18,
                      "experience": 5832,
                      "individualValues": { "hp": 31, "attack": 31, "defense": 31, "specialAttack": 31, "specialDefense": 31, "speed": 31 },
                      "effortValues": { "hp": 0, "attack": 0, "defense": 0, "specialAttack": 0, "specialDefense": 0, "speed": 0 },
                      "nature": "Timid",
                      "ability": "Static",
                      "combatStats": { "hp": 50, "attack": 41, "defense": 26, "specialAttack": 30, "specialDefense": 30, "speed": 40 },
                      "typing": { "primaryType": "Electric", "secondaryType": null },
                      "heldItem": "Magnet",
                      "moves": [
                        { "moveName": "Spark", "maxPp": 20, "currentPp": 10 }
                      ],
                      "memo": "Gym 2 prep",
                      "isBattleSimulatorEnabled": true
                    }
                  ],
                  "memo": "starter baseline"
                }
                """),
            "POST api/runs/{runId}/routes" => JsonDoc("Runs", "route を追加", "同じ run の中で複数の攻略分岐や比較用ルートを作る API です。", """
                {
                  "name": "Route 103",
                  "simulatedPartyMemberIds": [
                    "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
                  ]
                }
                """),
            "POST api/runs/{runId}/enemy-groups" => JsonDoc("Runs", "敵グループを追加", "任意のトレーナー戦や再戦相手を custom enemy group として登録する API です。", """
                {
                  "name": "May opening",
                  "sourceKind": "custom",
                  "members": [
                    {
                      "species": "Wingull",
                      "level": 14,
                      "hp": 36,
                      "attack": 20,
                      "defense": 18,
                      "primaryType": "Water",
                      "secondaryType": "Flying",
                      "baseExperienceYield": 64,
                      "effortValueYield": { "hp": 0, "attack": 0, "defense": 0, "specialAttack": 0, "specialDefense": 0, "speed": 1 },
                      "note": "lead"
                    }
                  ]
                }
                """),
            "PUT api/runs/{runId}/enemy-groups/{groupId}" => JsonDoc("Runs", "敵グループを更新", "enemy group の内容を編集し、battle から再利用する敵構成を更新する API です。", """
                {
                  "name": "May opening revised",
                  "sourceKind": "custom",
                  "members": [
                    {
                      "species": "Wingull",
                      "level": 15,
                      "hp": 38,
                      "attack": 21,
                      "defense": 19,
                      "primaryType": "Water",
                      "secondaryType": "Flying",
                      "baseExperienceYield": 64,
                      "effortValueYield": { "hp": 0, "attack": 0, "defense": 0, "specialAttack": 0, "specialDefense": 0, "speed": 1 },
                      "note": "rematch"
                    }
                  ]
                }
                """),
            "GET api/runs/{runId}/battles:search" => UrlDoc("Runs", "battle をキーワード検索", "route 名や敵名から battle へ素早くジャンプし、計算対象を探す API です。", "GET /api/runs/aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb/battles:search?keyword=Wingull"),

            "POST api/routes/{routeId}/events" => JsonDoc("Routes", "進行イベントを追加", "battle 結果、アイテム使用、技変更などの progression event を route に時系列追加する API です。", """
                {
                  "eventType": "battle-result",
                  "summary": "May 1 clear",
                  "linkedBattleId": "bbbbbbbb-1111-2222-3333-cccccccccccc",
                  "moneyDelta": 300,
                  "sourceReference": "story",
                  "partyDeltas": [
                    {
                      "partyMemberId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                      "experienceDelta": 0,
                      "effortValueDelta": { "hp": 0, "attack": 0, "defense": 0, "specialAttack": 0, "specialDefense": 0, "speed": 0 },
                      "ppDeltas": [
                        { "moveName": "Spark", "delta": -2 }
                      ],
                      "rareCandyLevels": 0,
                      "speciesOverride": null,
                      "abilityOverride": null,
                      "natureOverride": null,
                      "heldItemOverride": null,
                      "replaceMoves": null,
                      "notes": "Spark twice"
                    }
                  ]
                }
                """),
            "PUT api/routes/{routeId}/events/{eventId}" => JsonDoc("Routes", "進行イベントを更新", "登録済みの progression event を修正し、再計算前提を差し替える API です。", """
                {
                  "eventType": "move-change",
                  "summary": "Learn Thunderbolt",
                  "linkedBattleId": null,
                  "moneyDelta": 0,
                  "sourceReference": "tm24",
                  "partyDeltas": [
                    {
                      "partyMemberId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                      "experienceDelta": 0,
                      "effortValueDelta": { "hp": 0, "attack": 0, "defense": 0, "specialAttack": 0, "specialDefense": 0, "speed": 0 },
                      "ppDeltas": [],
                      "rareCandyLevels": 1,
                      "speciesOverride": null,
                      "abilityOverride": null,
                      "natureOverride": null,
                      "heldItemOverride": "Quick Claw",
                      "replaceMoves": [
                        { "moveName": "Thunderbolt", "maxPp": 15, "currentPp": 15 }
                      ],
                      "notes": "TM update"
                    }
                  ]
                }
                """),
            "POST api/routes/{routeId}:reorder" => JsonDoc("Routes", "進行イベント順を並べ替え", "route 上のイベント順を入れ替え、progression fingerprint と再計算順序を更新する API です。", """
                {
                  "eventIds": [
                    "11111111-2222-3333-4444-555555555555",
                    "66666666-7777-8888-9999-aaaaaaaaaaaa"
                  ]
                }
                """),
            "POST api/routes/{routeId}/battles" => JsonDoc("Routes", "battle を追加", "route 上に trainer 戦・optional 戦・任意 battle を追加する API です。", """
                {
                  "title": "May 1",
                  "sourceKind": "custom-group",
                  "battleKind": "trainer",
                  "isOptional": false,
                  "masterBattleCode": null,
                  "enemyGroupId": "dddddddd-1111-2222-3333-eeeeeeeeeeee",
                  "inlineEnemies": [],
                  "suggestedPartyMemberIds": [
                    "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"
                  ],
                  "participations": [
                    {
                      "partyMemberId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                      "participationMode": "active",
                      "shareRatio": 1.0,
                      "suggestedRole": "lead",
                      "outcomeChecklist": {
                        "sentOutAndDefeated": true,
                        "defeatedWhileInReserve": false,
                        "didNotDefeat": false,
                        "intentionalLoss": false
                      }
                    }
                  ],
                  "notes": "Custom trainer ref"
                }
                """),
            "PUT api/routes/{routeId}/battles/{battleId}" => JsonDoc("Routes", "battle を更新", "battle の敵構成や参加候補を編集し、後続の計算条件を調整する API です。", """
                {
                  "title": "May 1 revised",
                  "sourceKind": "custom-group",
                  "battleKind": "trainer",
                  "isOptional": false,
                  "masterBattleCode": null,
                  "enemyGroupId": "dddddddd-1111-2222-3333-eeeeeeeeeeee",
                  "inlineEnemies": [],
                  "suggestedPartyMemberIds": [
                    "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
                  ],
                  "participations": [],
                  "notes": "Updated trainer ref"
                }
                """),
            "POST api/routes/{routeId}:verify" => UrlDoc("Routes", "route verification を実行", "初期状態不足、enemy group 欠落、optional battle、PP warning などをまとめて検証する API です。", "POST /api/routes/aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb:verify"),
            "GET api/routes/{routeId}/verification" => UrlDoc("Routes", "route verification 結果を取得", "verification を GET で呼び出したいクライアント向けの同等 API です。", "GET /api/routes/aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb/verification"),
            "GET api/routes/{routeId}/progression" => UrlDoc("Routes", "progression projection を取得", "event 適用後のレベル・経験値・PP・simulation scope を確認する API です。", "GET /api/routes/aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb/progression"),
            "POST api/routes/{routeId}:recalculate" => UrlDoc("Routes", "route を再計算", "stale 状態の route を再計算し、最新の progression fingerprint を基準状態として確定する API です。", "POST /api/routes/aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb:recalculate"),

            "PUT api/battles/{battleId}/participation" => JsonDoc("Battles", "battle participation を更新", "どの party member が active / reserve / shared で battle に関与するかを更新する API です。", """
                {
                  "suggestedPartyMemberIds": [
                    "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"
                  ],
                  "participations": [
                    {
                      "partyMemberId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                      "participationMode": "shared",
                      "shareRatio": 0.75,
                      "suggestedRole": "lead",
                      "outcomeChecklist": {
                        "sentOutAndDefeated": true,
                        "defeatedWhileInReserve": false,
                        "didNotDefeat": false,
                        "intentionalLoss": false
                      }
                    }
                  ]
                }
                """),

            "POST api/calculations/damage" => JsonDoc("Calculations", "単体ダメージを計算", "特定 battle に対して 1 ケースのダメージレンジと補正内訳を確認する API です。", """
                {
                  "runId": "aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb",
                  "routeId": "cccccccc-1111-2222-3333-dddddddddddd",
                  "battleId": "eeeeeeee-1111-2222-3333-ffffffffffff",
                  "playerPartyMemberId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                  "presetId": "12121212-3434-5656-7878-909090909090",
                  "moveName": "Spark",
                  "movePower": 65,
                  "moveType": "Electric",
                  "attacker": { "species": "Pikachu", "level": 18, "attack": 41, "defense": 26, "primaryType": "Electric", "secondaryType": null, "heldItem": "Magnet" },
                  "defender": { "species": "Wingull", "level": 14, "attack": 20, "defense": 18, "primaryType": "Water", "secondaryType": "Flying", "heldItem": null },
                  "isCritical": true,
                  "typeEffectivenessOverride": null,
                  "additionalModifiers": [
                    { "code": "item", "label": "Magnet", "multiplier": 1.1, "sourceCode": "item.magnet", "sourceLabel": "Magnet", "sourceType": "item" }
                  ]
                }
                """),
            "POST api/calculations/compare-patterns" => JsonDoc("Calculations", "比較計算を実行", "同じ battle 条件で複数パターンの火力差を比較し、技威力や攻撃補正の差分を確認する API です。", """
                {
                  "baseCase": {
                    "runId": "aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb",
                    "routeId": "cccccccc-1111-2222-3333-dddddddddddd",
                    "battleId": "eeeeeeee-1111-2222-3333-ffffffffffff",
                    "playerPartyMemberId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    "presetId": "12121212-3434-5656-7878-909090909090",
                    "moveName": "Spark",
                    "movePower": 65,
                    "moveType": "Electric",
                    "attacker": { "species": "Pikachu", "level": 18, "attack": 41, "defense": 26, "primaryType": "Electric", "secondaryType": null, "heldItem": "Magnet" },
                    "defender": { "species": "Wingull", "level": 14, "attack": 20, "defense": 18, "primaryType": "Water", "secondaryType": "Flying", "heldItem": null },
                    "isCritical": false,
                    "typeEffectivenessOverride": null,
                    "additionalModifiers": []
                  },
                  "patterns": [
                    { "patternKey": "attack+2", "movePowerDelta": 0, "attackBonus": 2, "additionalModifiers": [] }
                  ]
                }
                """),
            "POST api/calculations/damage:compare-patterns" => JsonDoc("Calculations", "比較計算を実行（互換 alias）", "compare-patterns の互換エンドポイントです。既存クライアントを壊さずに比較計算を呼び出すための API です。", """
                {
                  "baseCase": {
                    "runId": "aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb",
                    "routeId": "cccccccc-1111-2222-3333-dddddddddddd",
                    "battleId": "eeeeeeee-1111-2222-3333-ffffffffffff",
                    "playerPartyMemberId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                    "presetId": "12121212-3434-5656-7878-909090909090",
                    "moveName": "Spark",
                    "movePower": 65,
                    "moveType": "Electric",
                    "attacker": { "species": "Pikachu", "level": 18, "attack": 41, "defense": 26, "primaryType": "Electric", "secondaryType": null, "heldItem": "Magnet" },
                    "defender": { "species": "Wingull", "level": 14, "attack": 20, "defense": 18, "primaryType": "Water", "secondaryType": "Flying", "heldItem": null },
                    "isCritical": false,
                    "typeEffectivenessOverride": null,
                    "additionalModifiers": []
                  },
                  "patterns": [
                    { "patternKey": "attack+2", "movePowerDelta": 0, "attackBonus": 2, "additionalModifiers": [] }
                  ]
                }
                """, "新規クライアントでは `/api/calculations/compare-patterns` を推奨します。"),
            "POST api/calculations/damage:search-thresholds" => JsonDoc("Calculations", "threshold-search を実行（互換 alias）", "threshold-search の互換エンドポイントです。既存クライアントを壊さずに最小 IV 探索を呼び出すための API です。", """
                {
                  "runId": "aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb",
                  "routeId": "cccccccc-1111-2222-3333-dddddddddddd",
                  "battleId": "eeeeeeee-1111-2222-3333-ffffffffffff",
                  "playerPartyMemberId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                  "presetId": "12121212-3434-5656-7878-909090909090",
                  "moveName": "Spark",
                  "movePower": 65,
                  "moveType": "Electric",
                  "searchStats": [ "attack", "speed" ],
                  "conditionMode": "allOf",
                  "conditions": [
                    { "conditionKey": "min-damage", "conditionType": "minimum-damage-at-least", "expectedValue": 20 },
                    { "conditionKey": "speed-check", "conditionType": "action-order-at-least", "expectedValue": 40 }
                  ],
                  "priorityOrder": []
                }
                """, "新規クライアントでは `/api/calculations/threshold-search` を推奨します。"),
            "POST api/calculations/threshold-search" => JsonDoc("Calculations", "threshold-search を実行", "指定条件を満たす最小 IV 組み合わせを探索し、攻略ラインを確認する API です。", """
                {
                  "runId": "aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb",
                  "routeId": "cccccccc-1111-2222-3333-dddddddddddd",
                  "battleId": "eeeeeeee-1111-2222-3333-ffffffffffff",
                  "playerPartyMemberId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
                  "presetId": "12121212-3434-5656-7878-909090909090",
                  "moveName": "Spark",
                  "movePower": 65,
                  "moveType": "Electric",
                  "searchStats": [ "attack" ],
                  "conditionMode": "allOf",
                  "conditions": [
                    { "conditionKey": "min-damage", "conditionType": "minimum-damage-at-least", "expectedValue": 20 }
                  ],
                  "priorityOrder": []
                }
                """),
            "GET api/presets" => UrlDoc("Presets", "preset 一覧を取得", "run ごとに保存した IV / EV / nature preset を一覧表示し、計算画面で選ぶための API です。", "GET /api/presets?runId=aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
            "POST api/presets" => JsonDoc("Presets", "preset を作成", "よく使う IV 範囲・努力値配分・性格候補を stable resource として保存する API です。", """
                {
                  "runId": "aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb",
                  "ivRanges": [
                    { "stat": "attack", "minimum": 20, "maximum": 31 },
                    { "stat": "speed", "minimum": 15, "maximum": 31 }
                  ],
                  "evPatterns": [
                    {
                      "patternKey": "story-default",
                      "effortValues": { "hp": 0, "attack": 36, "defense": 0, "specialAttack": 0, "specialDefense": 0, "speed": 12 },
                      "notes": "mid-game split"
                    }
                  ],
                  "naturePatterns": [
                    { "patternKey": "timid", "nature": "Timid", "notes": "speed focus" }
                  ],
                  "notes": "Story preset"
                }
                """),
            "GET api/presets/{presetId}" => UrlDoc("Presets", "preset 詳細を取得", "保存済み preset を個別取得し、編集フォームや計算再利用に使う API です。", "GET /api/presets/12121212-3434-5656-7878-909090909090"),
            "PUT api/presets/{presetId}" => JsonDoc("Presets", "preset を更新", "既存 preset の IV / EV / nature 候補を見直して revision を進める API です。", """
                {
                  "runId": "aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb",
                  "ivRanges": [
                    { "stat": "attack", "minimum": 25, "maximum": 31 }
                  ],
                  "evPatterns": [
                    {
                      "patternKey": "late-game",
                      "effortValues": { "hp": 0, "attack": 80, "defense": 0, "specialAttack": 0, "specialDefense": 0, "speed": 40 },
                      "notes": "late split"
                    }
                  ],
                  "naturePatterns": [
                    { "patternKey": "jolly", "nature": "Jolly", "notes": "speed priority" }
                  ],
                  "notes": "Updated preset"
                }
                """),

            "POST api/shares/snapshots" => JsonDoc("Shares", "snapshot を公開", "route / calculation / verification などの結果を他者レビュー用に固定公開する API です。", """
                {
                  "runId": "aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb",
                  "sourceType": "route-plan",
                  "sourceId": "cccccccc-1111-2222-3333-dddddddddddd",
                  "visibility": "public",
                  "allowedRoles": [ "viewer", "commenter", "reviser" ],
                  "summary": "Gym 2 comparison snapshot",
                  "frozenInput": { "routeId": "cccccccc-1111-2222-3333-dddddddddddd", "source": "route-plan" },
                  "frozenOutput": { "progressionFingerprint": "A1B2C3D4" }
                }
                """),
            "POST api/shares" => JsonDoc("Shares", "snapshot を公開（短縮 path）", "shares/snapshots と同じユースケースを満たす短縮 path の API です。", """
                {
                  "runId": "aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb",
                  "sourceType": "calculation",
                  "sourceId": "eeeeeeee-1111-2222-3333-ffffffffffff",
                  "visibility": "public",
                  "allowedRoles": [ "viewer", "commenter", "reviser" ],
                  "summary": "damage share",
                  "frozenInput": { "battleId": "eeeeeeee-1111-2222-3333-ffffffffffff", "source": "calculation" },
                  "frozenOutput": { "result": "damage" }
                }
                """),
            "GET api/shares/{shareId}" => UrlDoc("Shares", "snapshot を取得", "共有された snapshot 本体と current revision を読み取り、比較やレビュー画面に表示する API です。", "GET /api/shares/aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
            "GET api/shares/{shareId}/comments" => UrlDoc("Shares", "snapshot コメント一覧を取得", "共有物に紐づくレビューコメント履歴を表示する API です。", "GET /api/shares/aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb/comments"),
            "POST api/shares/{shareId}/comments" => JsonDoc("Shares", "snapshot にコメントを追加", "特定 revision にレビューコメントを追加し、攻略メモや指摘を残す API です。", """
                {
                  "revisionId": "12121212-3434-5656-7878-909090909090",
                  "body": "この時点では Thunderbolt 習得後の想定にしたいです。"
                }
                """),
            "GET api/shares/{shareId}/revisions" => UrlDoc("Shares", "revision 一覧を取得", "snapshot の revision chain をたどり、差分比較対象を選ぶための API です。", "GET /api/shares/aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb/revisions"),
            "POST api/shares/{shareId}:diff" => UrlDoc("Shares", "revision diff を取得", "2 つの revision 間で前提や結果がどう変わったかを比較する API です。", "POST /api/shares/aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb:diff?baseRevisionId=11111111-2222-3333-4444-555555555555&targetRevisionId=66666666-7777-8888-9999-aaaaaaaaaaaa"),
            "GET api/shares/{shareId}/diff" => UrlDoc("Shares", "revision diff を取得（GET）", "share diff を GET で取得したいクライアント向けの同等 API です。", "GET /api/shares/aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb/diff?baseRevisionId=11111111-2222-3333-4444-555555555555&targetRevisionId=66666666-7777-8888-9999-aaaaaaaaaaaa"),
            "POST api/shares/{shareId}:revise" => JsonDoc("Shares", "新しい revision を公開", "既存 share に対して新しい snapshot revision を追加し、比較履歴を伸ばす API です。", """
                {
                  "summary": "after rare candy"
                }
                """),
            "POST api/shares/{shareId}:publish-revision" => JsonDoc("Shares", "新しい revision を公開（互換 alias）", "revise の互換エンドポイントです。既存クライアントから revision を増やすための API です。", """
                {
                  "summary": "after rare candy"
                }
                """),

            "POST api/admin/imports" => JsonDoc("Admin", "import job を作成", "spreadsheet 取り込みを dry-run / commit で実行し、master version set を更新する管理 API です。", """
                {
                  "rulesetId": "11111111-1111-1111-1111-111111111111",
                  "workbookName": "story-route.xlsm",
                  "mode": "commit",
                  "sourceType": "spreadsheet",
                  "workbookContent": "BASE64_ENCODED_WORKBOOK"
                }
                """),
            "GET api/admin/imports/{jobId}" => UrlDoc("Admin", "import job を取得", "取り込みジョブの row-level validation、duplicate summary、audit trail を確認する API です。", "GET /api/admin/imports/aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
            "POST api/admin/import-jobs:dry-run" => JsonDoc("Admin", "dry-run import を実行", "master data を反映せずに spreadsheet の検証結果だけを確認する API です。", """
                {
                  "rulesetId": "11111111-1111-1111-1111-111111111111",
                  "workbookName": "emerald.xlsx",
                  "sourceType": "spreadsheet",
                  "workbookContent": "BASE64_ENCODED_WORKBOOK"
                }
                """),
            "POST api/admin/import-jobs" => JsonDoc("Admin", "commit import を実行", "spreadsheet から新しい master version set を作成して公開候補に進める API です。", """
                {
                  "rulesetId": "11111111-1111-1111-1111-111111111111",
                  "workbookName": "emerald.xlsx",
                  "sourceType": "spreadsheet",
                  "workbookContent": "BASE64_ENCODED_WORKBOOK"
                }
                """),
            "GET api/admin/import-jobs/{jobId}" => UrlDoc("Admin", "import job を取得（別 path）", "import-jobs 系 path でジョブ状態を確認する API です。", "GET /api/admin/import-jobs/aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
            "GET api/admin/master-version-sets" => UrlDoc("Admin", "master version set 一覧を取得", "公開済み・未公開を含む version set を一覧し、publish 対象を選ぶ API です。", "GET /api/admin/master-version-sets"),
            "POST api/admin/master-version-sets/{versionSetId}:publish" => UrlDoc("Admin", "master version set を公開", "取り込み済み version set を publish して、新規 run の固定先として使えるようにする API です。", "POST /api/admin/master-version-sets/22222222-2222-2222-2222-222222222222:publish"),

            _ => default!
        };

        return doc is not null;
    }

    private static StoryApiOperationDoc UrlDoc(string tag, string summary, string useCase, string example, string? notes = null)
        => new(tag, summary, useCase, $"```http{Environment.NewLine}{example}{Environment.NewLine}```", notes);

    private static StoryApiOperationDoc JsonDoc(string tag, string summary, string useCase, string example, string? notes = null)
        => new(tag, summary, useCase, $"```json{Environment.NewLine}{example}{Environment.NewLine}```", notes);
}

internal sealed record StoryApiOperationDoc(
    string Tag,
    string Summary,
    string UseCase,
    string Example,
    string? Notes);
