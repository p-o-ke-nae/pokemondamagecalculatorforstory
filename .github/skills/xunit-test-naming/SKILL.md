---
name: xunit-test-naming
description: 'xUnit テスト作成・更新・レビュー時に、何をテストするのかを日本語の DisplayName で明記し、DisplayName とテストメソッド名を規定フォーマットへ統一するスキル。Use when writing or reviewing C# xUnit tests, Fact, Theory, DisplayName, and test naming.'
---

# xUnit テスト命名ガイド

## 概要

このスキルは、C# の xUnit テストで DisplayName とテストメソッド名の粒度と形式を統一するためのガイドである。

| 項目 | ルール |
|------|------|
| DisplayName | 必ず日本語で `テスト対象クラス_テスト対象メソッド_テスト内容` |
| メソッド名 | 必ず `テスト対象クラス_テスト対象メソッド_テスト内容(英語)` |
| テスト内容 | 何を検証しているかが分かる具体的な振る舞い・結果を書く |
| 禁止 | `aaa`、`正常系`、`異常系`、`テスト` のような曖昧な語だけで終わらせない |

## いつ使うか

- xUnit の `Fact` または `Theory` を新規作成するとき
- 既存テストの DisplayName が未設定、または曖昧なとき
- テストレビューで命名規約のブレを直したいとき
- テストが何を保証しているかを一覧で読み取りやすくしたいとき

## 命名ルール

### 1. DisplayName は必須

- `Fact` と `Theory` には必ず `DisplayName` を付ける
- DisplayName は日本語で、テストが保証する内容を読むだけで分かるようにする

```csharp
[Fact(DisplayName = "Battle_CalculateDamage_やけど状態の物理技にやけど補正を適用する")]
public void Battle_CalculateDamage_AppliesBurnCorrectionForBurnedPhysicalMove()
{
}
```

### 2. DisplayName の形式

DisplayName は次の 3 要素をアンダースコアで連結する。

```text
テスト対象クラス_テスト対象メソッド_テスト内容
```

- `テスト対象クラス`: 実際に検証対象となるクラス名をそのまま使う
- `テスト対象メソッド`: 実際に検証対象となるメソッド名をそのまま使う
- `テスト内容`: 条件と期待結果が分かる日本語にする

良い例:

- `Battle_CalculateDamage_やけど状態の物理技にやけど補正を適用する`
- `StatusAilment_FromPrimaryAilment_まひを指定した場合にまひ状態を生成する`
- `UserAuthorizationInfoRepository_GetByUserIdAsync_存在しないユーザーIDではnullを返す`

避ける例:

- `aaa`
- `Battle_CalculateDamage_正常系`
- `CalculateDamage_ダメージ計算`

### 3. テストメソッド名の形式

テストメソッド名は次の 3 要素をアンダースコアで連結する。

```text
テスト対象クラス_テスト対象メソッド_テスト内容(英語)
```

- `テスト対象クラス`: 実際のクラス名をそのまま使う
- `テスト対象メソッド`: 実際のメソッド名をそのまま使う
- `テスト内容(英語)`: DisplayName の内容を英語で簡潔に表現する

英語部分のルール:

- PascalCase で書く
- 条件と期待結果が分かる動詞句にする
- `WorksCorrectly`、`NormalCase`、`ErrorCase` のような曖昧語を避ける
- 1 テスト 1 振る舞いを基本とする

良い例:

- `Battle_CalculateDamage_AppliesBurnCorrectionForBurnedPhysicalMove`
- `StatusAilment_FromPrimaryAilment_CreatesParalysisStatus`
- `UserAuthorizationInfoRepository_GetByUserIdAsync_ReturnsNullWhenUserIdDoesNotExist`

避ける例:

- `Battle_CalculateDamage_Test`
- `Battle_CalculateDamage_NormalCase`
- `Battle_CalculateDamage_WorksCorrectly`

### 4. テスト対象クラスとメソッドの決め方

- 直接検証しているクラス名とメソッド名を採用する
- テストクラス名の `Tests` 接尾辞は DisplayName とメソッド名に含めない
- private メソッドではなく、公開 API または振る舞いの入口になっているメソッド名を書く
- コンストラクタを対象にする場合はメソッド名を `Constructor` とする
- プロパティ getter/setter を明示したい場合は `PropertyName_get` または `PropertyName_set` とする

### 5. Theory の扱い

- `Theory` でも DisplayName は必須とする
- 1 つの `Theory` で同じ振る舞いを複数条件で確認する
- ケースごとに別の振る舞いを説明したくなる場合は `Theory` を分割する

```csharp
[Theory(DisplayName = "DamageCalculator_Calculate_急所時に補正後ダメージを返す")]
[InlineData(95, 142)]
[InlineData(100, 150)]
public void DamageCalculator_Calculate_ReturnsAdjustedDamageForCriticalHit(int attack, int expected)
{
}
```

## 手順

1. そのテストが直接保証するクラスとメソッドを特定する。
2. 条件と期待結果を 1 文の日本語で書き下ろす。
3. 日本語を `テスト対象クラス_テスト対象メソッド_テスト内容` の形式に整える。
4. 同じ内容を簡潔な英語へ変換し、`テスト対象クラス_テスト対象メソッド_テスト内容(英語)` の形式に整える。
5. `Fact` または `Theory` に DisplayName を付ける。
6. テスト本体が DisplayName とメソッド名の宣言どおりの振る舞いだけを検証しているか確認する。

## レビュー観点

- DisplayName が未設定ではないか
- DisplayName が日本語で、何をテストしているか明確か
- DisplayName が `テスト対象クラス_テスト対象メソッド_テスト内容` の 3 要素になっているか
- メソッド名が `テスト対象クラス_テスト対象メソッド_テスト内容(英語)` になっているか
- 日本語と英語で同じ振る舞いを指しているか
- `正常系` や `異常系` のような曖昧語で逃げていないか
- 1 つのテストで複数の独立した振る舞いを詰め込んでいないか

## 変換例

### 悪い例

```csharp
[Fact(DisplayName = "aaa")]
public void Gen3Policy_BurnedPhysicalMove_AppliesBurnCorrectionAfterBaseDamage()
{
}
```

### 良い例

```csharp
[Fact(DisplayName = "Battle_CalculateDamage_やけど状態の物理技にやけど補正を適用する")]
public void Battle_CalculateDamage_AppliesBurnCorrectionForBurnedPhysicalMove()
{
}
```

## 完了条件

- xUnit テストの `Fact` と `Theory` に DisplayName が付いている
- DisplayName が日本語で具体的な検証内容を表している
- テストメソッド名が英語で同じ検証内容を表している
- クラス名、メソッド名、テスト内容がアンダースコア区切りで統一されている