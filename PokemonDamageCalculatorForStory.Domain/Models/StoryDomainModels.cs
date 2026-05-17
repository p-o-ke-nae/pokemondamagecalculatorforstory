using System.Text.Json.Serialization;

namespace PokemonDamageCalculatorForStory.Domain.Models;

/// <summary>ダメージ計算で使用するルールセットを表します。</summary>
/// <param name="Id">ルールセット識別子です。</param>
/// <param name="Slug">人が参照しやすいスラッグです。</param>
/// <param name="Generation">世代名です。</param>
/// <param name="Title">タイトル名です。</param>
/// <param name="Version">ルールセット版名です。</param>
/// <param name="Status">公開状態です。</param>
/// <param name="Summary">概要説明です。</param>
public sealed record Ruleset(
    Guid Id,
    string Slug,
    string Generation,
    string Title,
    string Version,
    string Status,
    string Summary);

/// <summary>再現性のために凍結したバージョン集合を表します。</summary>
/// <param name="Id">version set 識別子です。</param>
/// <param name="RulesetId">対応するルールセット識別子です。</param>
/// <param name="Label">表示名です。</param>
/// <param name="ImportedAt">取り込み日時です。</param>
/// <param name="IsPublished">公開済みかどうかです。</param>
/// <param name="VersionCatalog">固定したルール・マスタ版情報です。</param>
/// <param name="MasterSources">参照した master source 一覧です。</param>
public sealed record MasterVersionSet(
    Guid Id,
    Guid RulesetId,
    string Label,
    DateTimeOffset ImportedAt,
    bool IsPublished,
    VersionCatalog VersionCatalog,
    IReadOnlyList<SourceReference> MasterSources);

/// <summary>計算に使用した各ルール・マスタの版情報を表します。</summary>
/// <param name="DamageRulesetVersion">ダメージ計算ルール版です。</param>
/// <param name="ExperienceRulesetVersion">経験値・進捗計算ルール版です。</param>
/// <param name="PokemonMasterVersion">ポケモンマスタ版です。</param>
/// <param name="MoveMasterVersion">技マスタ版です。</param>
/// <param name="AbilityMasterVersion">特性マスタ版です。</param>
/// <param name="ItemMasterVersion">アイテムマスタ版です。</param>
/// <param name="TypeChartVersion">タイプ相性表版です。</param>
/// <param name="NatureMasterVersion">性格マスタ版です。</param>
/// <param name="StoryEnemyMasterVersion">敵マスタ版です。</param>
/// <param name="ExperienceTableVersion">経験値テーブル版です。</param>
/// <param name="EffortValueMasterVersion">努力値マスタ版です。</param>
/// <param name="PpRuleVersion">PP ルール版です。</param>
/// <param name="AdditionalVersionKeys">追加の版キー一覧です。</param>
/// <param name="ImportJobIds">関連 import job 識別子一覧です。</param>
public sealed record VersionCatalog(
    string DamageRulesetVersion,
    string ExperienceRulesetVersion,
    string PokemonMasterVersion,
    string MoveMasterVersion,
    string AbilityMasterVersion,
    string ItemMasterVersion,
    string TypeChartVersion,
    string NatureMasterVersion,
    string StoryEnemyMasterVersion,
    string ExperienceTableVersion,
    string EffortValueMasterVersion,
    string PpRuleVersion,
    IReadOnlyList<string> AdditionalVersionKeys,
    IReadOnlyList<Guid> ImportJobIds);

/// <summary>ユーザーが管理する攻略 run 全体を表します。</summary>
/// <param name="Id">run 識別子です。</param>
/// <param name="OwnerUserId">所有ユーザー識別子です。</param>
/// <param name="RulesetId">選択したルールセット識別子です。</param>
/// <param name="MasterVersionSetId">固定した version set 識別子です。</param>
/// <param name="Name">run 名です。</param>
/// <param name="Status">状態です。</param>
/// <param name="InitialState">初期状態です。</param>
/// <param name="Routes">run 配下のルート一覧です。</param>
/// <param name="EnemyGroups">ユーザー定義 enemy group 一覧です。</param>
/// <param name="CreatedAt">作成日時です。</param>
/// <param name="UpdatedAt">更新日時です。</param>
public sealed record RunAggregate(
    Guid Id,
    string OwnerUserId,
    Guid RulesetId,
    Guid MasterVersionSetId,
    string Name,
    string Status,
    RunInitialState? InitialState,
    IReadOnlyList<RoutePlan> Routes,
    IReadOnlyList<EnemyGroupDefinition> EnemyGroups,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>run の初期状態を表します。</summary>
/// <param name="Revision">改訂番号です。</param>
/// <param name="BaselineMoney">初期所持金です。</param>
/// <param name="BaselineParty">初期手持ち一覧です。</param>
/// <param name="Memo">任意メモです。</param>
public sealed record RunInitialState(
    int Revision,
    int BaselineMoney,
    IReadOnlyList<PartyMemberDefinition> BaselineParty,
    string? Memo);

/// <summary>初期手持ちまたは再計算の基点となるポケモン状態を表します。</summary>
/// <param name="PartyMemberId">手持ちメンバー識別子です。</param>
/// <param name="Slot">手持ちスロット番号です。</param>
/// <param name="Species">種族名です。</param>
/// <param name="Level">初期レベルです。</param>
/// <param name="Experience">初期経験値です。</param>
/// <param name="IndividualValues">個体値です。</param>
/// <param name="EffortValues">努力値です。</param>
/// <param name="Nature">性格です。</param>
/// <param name="Ability">特性です。</param>
/// <param name="CombatStats">戦闘に使う実数値です。</param>
/// <param name="Typing">タイプ情報です。</param>
/// <param name="HeldItem">所持アイテムです。</param>
/// <param name="Moves">技と PP 状態です。</param>
/// <param name="Memo">任意メモです。</param>
/// <param name="IsBattleSimulatorEnabled">主対象として計算するかどうかです。</param>
public sealed record PartyMemberDefinition(
    Guid PartyMemberId,
    int Slot,
    string Species,
    int Level,
    int Experience,
    StatValues IndividualValues,
    StatValues EffortValues,
    string Nature,
    string Ability,
    CombatStatSnapshot CombatStats,
    PokemonTypeSlot Typing,
    string? HeldItem,
    IReadOnlyList<MoveState> Moves,
    string? Memo,
    bool IsBattleSimulatorEnabled);

/// <summary>整数値の能力セットを表します。</summary>
/// <param name="Hp">HP です。</param>
/// <param name="Attack">攻撃です。</param>
/// <param name="Defense">防御です。</param>
/// <param name="SpecialAttack">特攻です。</param>
/// <param name="SpecialDefense">特防です。</param>
/// <param name="Speed">素早さです。</param>
public sealed record StatValues(
    int Hp,
    int Attack,
    int Defense,
    int SpecialAttack,
    int SpecialDefense,
    int Speed);

/// <summary>戦闘で使用する実数値スナップショットです。</summary>
/// <param name="Hp">HP 実数値です。</param>
/// <param name="Attack">攻撃実数値です。</param>
/// <param name="Defense">防御実数値です。</param>
/// <param name="SpecialAttack">特攻実数値です。</param>
/// <param name="SpecialDefense">特防実数値です。</param>
/// <param name="Speed">素早さ実数値です。</param>
public sealed record CombatStatSnapshot(
    int Hp,
    int Attack,
    int Defense,
    int SpecialAttack,
    int SpecialDefense,
    int Speed);

/// <summary>ポケモンのタイプ組み合わせを表します。</summary>
/// <param name="PrimaryType">主タイプです。</param>
/// <param name="SecondaryType">副タイプです。</param>
public sealed record PokemonTypeSlot(
    PokemonType PrimaryType,
    PokemonType? SecondaryType);

/// <summary>技と PP 状態を表します。</summary>
/// <param name="MoveName">技名です。</param>
/// <param name="MaxPp">最大 PP です。</param>
/// <param name="CurrentPp">現在 PP です。</param>
public sealed record MoveState(
    string MoveName,
    int MaxPp,
    int CurrentPp);

/// <summary>run 配下の攻略ルートを表します。</summary>
/// <param name="Id">route 識別子です。</param>
/// <param name="RunId">親 run 識別子です。</param>
/// <param name="Name">route 名です。</param>
/// <param name="IsStale">再計算が必要な状態かどうかです。</param>
/// <param name="ProgressionFingerprint">authority から導出した fingerprint です。</param>
/// <param name="Events">ordered progression event 一覧です。</param>
/// <param name="Battles">route に紐づく battle 一覧です。</param>
/// <param name="SimulatedPartyMemberIds">主対象として扱う party member 識別子一覧です。</param>
/// <param name="LastVerifiedAt">直近検証日時です。</param>
public sealed record RoutePlan(
    Guid Id,
    Guid RunId,
    string Name,
    bool IsStale,
    string ProgressionFingerprint,
    IReadOnlyList<ProgressionEvent> Events,
    IReadOnlyList<BattleDefinition> Battles,
    IReadOnlyList<Guid> SimulatedPartyMemberIds,
    DateTimeOffset? LastVerifiedAt);

/// <summary>攻略進行イベントを表します。</summary>
/// <param name="Id">event 識別子です。</param>
/// <param name="Sequence">順序番号です。</param>
/// <param name="EventType">イベント種別です。</param>
/// <param name="Summary">表示用概要です。</param>
/// <param name="LinkedBattleId">紐付く battle 識別子です。</param>
/// <param name="MoneyDelta">所持金変化量です。</param>
/// <param name="SourceReference">由来参照です。</param>
/// <param name="PartyDeltas">ポケモンごとの進捗差分です。</param>
/// <param name="Revision">イベント改訂番号です。</param>
public sealed record ProgressionEvent(
    Guid Id,
    int Sequence,
    string EventType,
    string Summary,
    Guid? LinkedBattleId,
    int MoneyDelta,
    string? SourceReference,
    IReadOnlyList<PartyProgressionDelta> PartyDeltas,
    int Revision);

/// <summary>ポケモンごとの差分イベントを表します。</summary>
/// <param name="PartyMemberId">対象 party member 識別子です。</param>
/// <param name="ExperienceDelta">経験値差分です。</param>
/// <param name="EffortValueDelta">努力値差分です。</param>
/// <param name="PpDeltas">PP 差分です。</param>
/// <param name="RareCandyLevels">ふしぎなアメ等で増えるレベル数です。</param>
/// <param name="SpeciesOverride">種族変更後の種族名です。</param>
/// <param name="AbilityOverride">変更後の特性です。</param>
/// <param name="NatureOverride">変更後の性格です。</param>
/// <param name="HeldItemOverride">変更後の所持アイテムです。</param>
/// <param name="ReplaceMoves">技構成の置換結果です。</param>
/// <param name="Notes">差分メモです。</param>
public sealed record PartyProgressionDelta(
    Guid PartyMemberId,
    int ExperienceDelta,
    StatValues EffortValueDelta,
    IReadOnlyList<MovePpDelta> PpDeltas,
    int RareCandyLevels,
    string? SpeciesOverride,
    string? AbilityOverride,
    string? NatureOverride,
    string? HeldItemOverride,
    IReadOnlyList<MoveState>? ReplaceMoves,
    string? Notes);

/// <summary>技ごとの PP 差分を表します。</summary>
/// <param name="MoveName">技名です。</param>
/// <param name="Delta">PP 差分です。消費は負数、回復は正数です。</param>
public sealed record MovePpDelta(
    string MoveName,
    int Delta);

/// <summary>ユーザー定義の enemy group を表します。</summary>
/// <param name="Id">group 識別子です。</param>
/// <param name="RunId">親 run 識別子です。</param>
/// <param name="Name">group 名です。</param>
/// <param name="SourceKind">作成由来です。</param>
/// <param name="Members">敵一覧です。</param>
public sealed record EnemyGroupDefinition(
    Guid Id,
    Guid RunId,
    string Name,
    string SourceKind,
    IReadOnlyList<EnemyCombatant> Members);

/// <summary>戦闘対象の敵情報を表します。</summary>
/// <param name="Species">種族名です。</param>
/// <param name="Level">レベルです。</param>
/// <param name="Hp">HP 実数値です。</param>
/// <param name="Attack">攻撃実数値です。</param>
/// <param name="Defense">防御実数値です。</param>
/// <param name="Typing">タイプ情報です。</param>
/// <param name="BaseExperienceYield">基礎経験値です。</param>
/// <param name="EffortValueYield">努力値獲得量です。</param>
/// <param name="Note">補足メモです。</param>
public sealed record EnemyCombatant(
    string Species,
    int Level,
    int Hp,
    int Attack,
    int Defense,
    PokemonTypeSlot Typing,
    int BaseExperienceYield,
    StatValues EffortValueYield,
    string? Note);

/// <summary>route 上の battle 定義を表します。</summary>
/// <param name="Id">battle 識別子です。</param>
/// <param name="RouteId">親 route 識別子です。</param>
/// <param name="Title">表示名です。</param>
/// <param name="SourceKind">master / custom-group / arbitrary などの由来です。</param>
/// <param name="BattleKind">trainer / optional-trainer / arbitrary などの戦闘種別です。</param>
/// <param name="IsOptional">任意戦闘かどうかです。</param>
/// <param name="MasterBattleCode">master 参照コードです。</param>
/// <param name="EnemyGroupId">enemy group 参照識別子です。</param>
/// <param name="InlineEnemies">battle に直接埋め込む敵一覧です。</param>
/// <param name="SuggestedPartyMemberIds">入力補助として提示する party member 識別子です。</param>
/// <param name="Participations">ポケモンごとの参加計画です。</param>
/// <param name="Notes">補足メモです。</param>
public sealed record BattleDefinition(
    Guid Id,
    Guid RouteId,
    string Title,
    string SourceKind,
    string BattleKind,
    bool IsOptional,
    string? MasterBattleCode,
    Guid? EnemyGroupId,
    IReadOnlyList<EnemyCombatant> InlineEnemies,
    IReadOnlyList<Guid> SuggestedPartyMemberIds,
    IReadOnlyList<BattleParticipationPlan> Participations,
    string? Notes);

/// <summary>battle に対するポケモン別参加計画を表します。</summary>
/// <param name="PartyMemberId">対象 party member 識別子です。</param>
/// <param name="ParticipationMode">参加形態です。</param>
/// <param name="ShareRatio">shared 時の配分比率です。</param>
/// <param name="SuggestedRole">入力補助の役割表示です。</param>
/// <param name="OutcomeChecklist">手動上書き結果です。</param>
public sealed record BattleParticipationPlan(
    Guid PartyMemberId,
    string ParticipationMode,
    decimal ShareRatio,
    string? SuggestedRole,
    ManualOutcomeChecklist OutcomeChecklist);

/// <summary>battle 結果の手動チェック項目を表します。</summary>
/// <param name="SentOutAndDefeated">対面参加して倒したかどうかです。</param>
/// <param name="DefeatedWhileInReserve">控え扱いで倒したかどうかです。</param>
/// <param name="DidNotDefeat">倒していないことを示すかどうかです。</param>
/// <param name="IntentionalLoss">意図的に負けたかどうかです。</param>
public sealed record ManualOutcomeChecklist(
    bool SentOutAndDefeated,
    bool DefeatedWhileInReserve,
    bool DidNotDefeat,
    bool IntentionalLoss);

/// <summary>battle quick search のヒット結果です。</summary>
/// <param name="BattleId">battle 識別子です。</param>
/// <param name="RouteId">route 識別子です。</param>
/// <param name="RouteName">route 名です。</param>
/// <param name="Title">battle 表示名です。</param>
/// <param name="BattleKind">戦闘種別です。</param>
/// <param name="EnemySummary">敵概要です。</param>
public sealed record BattleSearchHit(
    Guid BattleId,
    Guid RouteId,
    string RouteName,
    string Title,
    string BattleKind,
    string EnemySummary);

/// <summary>route 全体検証結果です。</summary>
/// <param name="RouteId">route 識別子です。</param>
/// <param name="VerifiedAt">検証日時です。</param>
/// <param name="Summary">要約です。</param>
/// <param name="Issues">解消が必要な問題一覧です。</param>
/// <param name="Warnings">警告一覧です。</param>
/// <param name="SourceReferences">根拠参照一覧です。</param>
public sealed record RouteVerificationResult(
    Guid RouteId,
    DateTimeOffset VerifiedAt,
    string Summary,
    IReadOnlyList<VerificationMessage> Issues,
    IReadOnlyList<VerificationMessage> Warnings,
    IReadOnlyList<SourceReference> SourceReferences);

/// <summary>進捗再計算結果を表します。</summary>
/// <param name="RouteId">route 識別子です。</param>
/// <param name="CalculatedAt">再計算日時です。</param>
/// <param name="PartyMembers">ポケモン別投影結果です。</param>
/// <param name="Warnings">進捗警告です。</param>
/// <param name="ProgressionFingerprint">再計算に用いた fingerprint です。</param>
/// <param name="SourceReferences">根拠参照一覧です。</param>
public sealed record RouteProgressionProjection(
    Guid RouteId,
    DateTimeOffset CalculatedAt,
    IReadOnlyList<PartyMemberProjection> PartyMembers,
    IReadOnlyList<VerificationMessage> Warnings,
    string ProgressionFingerprint,
    IReadOnlyList<SourceReference> SourceReferences);

/// <summary>ポケモン別の現在進捗投影を表します。</summary>
/// <param name="PartyMemberId">party member 識別子です。</param>
/// <param name="Slot">手持ちスロット番号です。</param>
/// <param name="Species">現在種族名です。</param>
/// <param name="Level">現在レベルです。</param>
/// <param name="Experience">現在経験値です。</param>
/// <param name="EffortValues">現在努力値です。</param>
/// <param name="Moves">現在技と PP です。</param>
/// <param name="SimulationScope">simulation 対象種別です。</param>
/// <param name="IsBattleSimulatorEnabled">主対象かどうかです。</param>
/// <param name="Warnings">このポケモンに紐づく警告です。</param>
public sealed record PartyMemberProjection(
    Guid PartyMemberId,
    int Slot,
    string Species,
    int Level,
    int Experience,
    StatValues EffortValues,
    IReadOnlyList<MoveState> Moves,
    string SimulationScope,
    bool IsBattleSimulatorEnabled,
    IReadOnlyList<VerificationMessage> Warnings);

/// <summary>検証メッセージです。</summary>
/// <param name="Code">メッセージコードです。</param>
/// <param name="Message">本文です。</param>
/// <param name="BattleId">関連 battle 識別子です。</param>
public sealed record VerificationMessage(
    string Code,
    string Message,
    Guid? BattleId);

/// <summary>単一ケースダメージ計算要求です。</summary>
/// <param name="RunId">run 識別子です。</param>
/// <param name="RouteId">route 識別子です。</param>
/// <param name="BattleId">battle 識別子です。</param>
/// <param name="PlayerPartyMemberId">自ポケモン識別子です。</param>
/// <param name="MoveName">技名です。</param>
/// <param name="MovePower">技威力です。</param>
/// <param name="MoveType">技タイプです。</param>
/// <param name="Attacker">攻撃側状態です。</param>
/// <param name="Defender">防御側状態です。</param>
/// <param name="IsCritical">急所判定です。</param>
/// <param name="TypeEffectivenessOverride">タイプ相性上書き倍率です。</param>
/// <param name="AdditionalModifiers">追加補正一覧です。</param>
public sealed record DamageCalculationRequest(
    Guid RunId,
    Guid RouteId,
    Guid BattleId,
    Guid? PlayerPartyMemberId,
    string MoveName,
    int MovePower,
    PokemonType MoveType,
    CombatantSnapshot Attacker,
    CombatantSnapshot Defender,
    bool IsCritical,
    decimal? TypeEffectivenessOverride,
    IReadOnlyList<DamageModifier> AdditionalModifiers);

/// <summary>計算時点の戦闘参加者状態です。</summary>
/// <param name="Species">種族名です。</param>
/// <param name="Level">レベルです。</param>
/// <param name="Attack">攻撃実数値です。</param>
/// <param name="Defense">防御実数値です。</param>
/// <param name="PrimaryType">主タイプです。</param>
/// <param name="SecondaryType">副タイプです。</param>
/// <param name="HeldItem">所持アイテムです。</param>
public sealed record CombatantSnapshot(
    string Species,
    int Level,
    int Attack,
    int Defense,
    PokemonType PrimaryType,
    PokemonType? SecondaryType,
    string? HeldItem);

/// <summary>ダメージ補正要素です。</summary>
/// <param name="Code">補正コードです。</param>
/// <param name="Label">表示名です。</param>
/// <param name="Multiplier">倍率です。</param>
/// <param name="Source">補正根拠です。</param>
public sealed record DamageModifier(
    string Code,
    string Label,
    decimal Multiplier,
    SourceReference Source);

/// <summary>単一ケース計算結果です。</summary>
/// <param name="MinimumDamage">最小ダメージです。</param>
/// <param name="MaximumDamage">最大ダメージです。</param>
/// <param name="AppliedModifiers">適用補正一覧です。</param>
/// <param name="Explanation">説明文です。</param>
/// <param name="FormulaTrace">計算トレースです。</param>
/// <param name="Warnings">警告一覧です。</param>
/// <param name="SourceReferences">参照一覧です。</param>
public sealed record DamageCalculationResult(
    int MinimumDamage,
    int MaximumDamage,
    IReadOnlyList<DamageModifier> AppliedModifiers,
    IReadOnlyList<string> Explanation,
    IReadOnlyList<string> FormulaTrace,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<SourceReference> SourceReferences);

/// <summary>比較計算結果です。</summary>
/// <param name="PatternKey">比較キーです。</param>
/// <param name="InputSummary">入力概要です。</param>
/// <param name="Result">計算結果です。</param>
/// <param name="ExplanationDelta">ベースとの差分説明です。</param>
public sealed record ComparisonPatternResult(
    string PatternKey,
    string InputSummary,
    DamageCalculationResult Result,
    IReadOnlyList<string> ExplanationDelta);

/// <summary>しきい値探索要求です。</summary>
/// <param name="RunId">run 識別子です。</param>
/// <param name="RouteId">route 識別子です。</param>
/// <param name="BattleId">battle 識別子です。</param>
/// <param name="PlayerPartyMemberId">自ポケモン識別子です。</param>
/// <param name="MoveName">技名です。</param>
/// <param name="MoveType">技タイプです。</param>
/// <param name="MovePowerRangeStart">探索開始威力です。</param>
/// <param name="MovePowerRangeEnd">探索終了威力です。</param>
/// <param name="MaximumAttackBonus">探索する攻撃補正最大値です。</param>
/// <param name="TargetMinimumDamage">達成したい最小ダメージです。</param>
public sealed record ThresholdSearchRequest(
    Guid RunId,
    Guid RouteId,
    Guid BattleId,
    Guid? PlayerPartyMemberId,
    string MoveName,
    PokemonType MoveType,
    int MovePowerRangeStart,
    int MovePowerRangeEnd,
    int MaximumAttackBonus,
    int TargetMinimumDamage);

/// <summary>しきい値探索候補です。</summary>
/// <param name="MovePower">候補威力です。</param>
/// <param name="AttackBonus">攻撃補正です。</param>
/// <param name="MinimumDamage">達成最小ダメージです。</param>
/// <param name="Reason">採用または棄却理由です。</param>
public sealed record ThresholdCandidate(
    int MovePower,
    int AttackBonus,
    int MinimumDamage,
    string Reason);

/// <summary>しきい値探索結果です。</summary>
/// <param name="TargetMinimumDamage">目標最小ダメージです。</param>
/// <param name="Solved">条件を満たす候補が存在するかどうかです。</param>
/// <param name="Candidates">条件を満たした候補です。</param>
/// <param name="Rejected">棄却候補です。</param>
/// <param name="SourceReferences">根拠参照です。</param>
public sealed record ThresholdSearchResult(
    int TargetMinimumDamage,
    bool Solved,
    IReadOnlyList<ThresholdCandidate> Candidates,
    IReadOnlyList<ThresholdCandidate> Rejected,
    IReadOnlyList<SourceReference> SourceReferences);

/// <summary>共有された immutable snapshot を表します。</summary>
/// <param name="Id">share 識別子です。</param>
/// <param name="OwnerUserId">公開者識別子です。</param>
/// <param name="RunId">元 run 識別子です。</param>
/// <param name="RouteId">元 route 識別子です。</param>
/// <param name="Visibility">公開範囲です。</param>
/// <param name="BaseRevisionId">基底 revision 識別子です。</param>
/// <param name="CurrentRevisionId">最新 revision 識別子です。</param>
/// <param name="FixedVersionCatalog">共有時に固定した版情報です。</param>
/// <param name="Revisions">revision 一覧です。</param>
/// <param name="Comments">comment 一覧です。</param>
/// <param name="PublishedAt">公開日時です。</param>
public sealed record SharedRouteSnapshot(
    Guid Id,
    string OwnerUserId,
    Guid RunId,
    Guid RouteId,
    string Visibility,
    Guid BaseRevisionId,
    Guid CurrentRevisionId,
    VersionCatalog FixedVersionCatalog,
    IReadOnlyList<RouteRevision> Revisions,
    IReadOnlyList<ShareComment> Comments,
    DateTimeOffset PublishedAt);

/// <summary>共有 revision を表します。</summary>
/// <param name="Id">revision 識別子です。</param>
/// <param name="ParentRevisionId">親 revision 識別子です。</param>
/// <param name="Summary">変更概要です。</param>
/// <param name="SnapshotJson">スナップショット本文です。</param>
/// <param name="SnapshotDigest">比較用 digest です。</param>
/// <param name="AuthorUserId">作成者識別子です。</param>
/// <param name="CreatedAt">作成日時です。</param>
public sealed record RouteRevision(
    Guid Id,
    Guid? ParentRevisionId,
    string Summary,
    string SnapshotJson,
    string SnapshotDigest,
    string AuthorUserId,
    DateTimeOffset CreatedAt);

/// <summary>共有コメントです。</summary>
/// <param name="Id">comment 識別子です。</param>
/// <param name="ShareId">share 識別子です。</param>
/// <param name="RevisionId">revision 識別子です。</param>
/// <param name="AuthorUserId">投稿者識別子です。</param>
/// <param name="Body">本文です。</param>
/// <param name="CreatedAt">作成日時です。</param>
public sealed record ShareComment(
    Guid Id,
    Guid ShareId,
    Guid? RevisionId,
    string AuthorUserId,
    string Body,
    DateTimeOffset CreatedAt);

/// <summary>revision 間 diff を表します。</summary>
/// <param name="ShareId">share 識別子です。</param>
/// <param name="BaseRevisionId">基準 revision 識別子です。</param>
/// <param name="TargetRevisionId">比較先 revision 識別子です。</param>
/// <param name="Summary">差分概要です。</param>
/// <param name="ChangedFields">変更された項目一覧です。</param>
/// <param name="ChangedVersions">変更された版情報一覧です。</param>
public sealed record ShareDiffResult(
    Guid ShareId,
    Guid BaseRevisionId,
    Guid TargetRevisionId,
    string Summary,
    IReadOnlyList<string> ChangedFields,
    IReadOnlyList<string> ChangedVersions);

/// <summary>import job を表します。</summary>
/// <param name="Id">job 識別子です。</param>
/// <param name="RulesetId">対象ルールセット識別子です。</param>
/// <param name="WorkbookName">対象 workbook 名です。</param>
/// <param name="Mode">dry-run / commit などの実行モードです。</param>
/// <param name="Status">状態です。</param>
/// <param name="SubmittedByUserId">実行者識別子です。</param>
/// <param name="Summary">結果概要です。</param>
/// <param name="Messages">詳細メッセージ一覧です。</param>
/// <param name="PublishedMasterVersionSetId">生成された version set 識別子です。</param>
/// <param name="CreatedAt">作成日時です。</param>
/// <param name="CompletedAt">完了日時です。</param>
public sealed record ImportJob(
    Guid Id,
    Guid RulesetId,
    string WorkbookName,
    string Mode,
    string Status,
    string SubmittedByUserId,
    string Summary,
    IReadOnlyList<string> Messages,
    Guid? PublishedMasterVersionSetId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

/// <summary>根拠参照を表します。</summary>
/// <param name="Code">参照コードです。</param>
/// <param name="Label">表示名です。</param>
/// <param name="ReferenceType">参照種別です。</param>
public sealed record SourceReference(
    string Code,
    string Label,
    string ReferenceType);

/// <summary>JSON 永続化に使う route snapshot 専用モデルです。</summary>
/// <param name="Route">snapshot 化した route です。</param>
/// <param name="InitialState">snapshot 時の初期状態です。</param>
/// <param name="EnemyGroups">snapshot 時の enemy group です。</param>
/// <param name="ProgressionProjection">snapshot 時の進捗投影です。</param>
/// <param name="VersionCatalog">snapshot 時の版情報です。</param>
public sealed record RouteSnapshotDocument(
    RoutePlan Route,
    RunInitialState? InitialState,
    IReadOnlyList<EnemyGroupDefinition> EnemyGroups,
    RouteProgressionProjection? ProgressionProjection,
    VersionCatalog? VersionCatalog);

/// <summary>ポケモンタイプを表します。</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PokemonType
{
    /// <summary>ノーマルです。</summary>
    Normal,

    /// <summary>ほのおです。</summary>
    Fire,

    /// <summary>みずです。</summary>
    Water,

    /// <summary>でんきです。</summary>
    Electric,

    /// <summary>くさです。</summary>
    Grass,

    /// <summary>こおりです。</summary>
    Ice,

    /// <summary>かくとうです。</summary>
    Fighting,

    /// <summary>どくです。</summary>
    Poison,

    /// <summary>じめんです。</summary>
    Ground,

    /// <summary>ひこうです。</summary>
    Flying,

    /// <summary>エスパーです。</summary>
    Psychic,

    /// <summary>むしです。</summary>
    Bug,

    /// <summary>いわです。</summary>
    Rock,

    /// <summary>ゴーストです。</summary>
    Ghost,

    /// <summary>ドラゴンです。</summary>
    Dragon,

    /// <summary>あくです。</summary>
    Dark,

    /// <summary>はがねです。</summary>
    Steel,

    /// <summary>フェアリーです。</summary>
    Fairy
}
