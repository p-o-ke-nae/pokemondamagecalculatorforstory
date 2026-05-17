# Documentation

## 概要

`docs/` 配下の設計・検証ドキュメントの入口です。現在は Issue #1 の Pokemon story damage calculator foundation と、Issue #8 の bootstrap foundation の両方を管理します。Phase 3 では skeleton だった story calculator docs を、実装済みの API / テスト挙動に合わせて更新しています。

| ドキュメント | 役割 | 読み始めるタイミング |
|---|---|---|
| [`architecture.md`](architecture.md) | リポジトリ全体の設計スライス、実装状況、横断ルールを俯瞰する | 変更対象を把握したいとき |
| [`designs/pokemon-story-damage-calculator-api-foundation.md`](designs/pokemon-story-damage-calculator-api-foundation.md) | story calculator foundation の実装済み API / 制約を確認する | API や責務境界を確認したいとき |
| [`tests/pokemon-story-damage-calculator-foundation-test-plan.md`](tests/pokemon-story-damage-calculator-foundation-test-plan.md) | story calculator foundation の現在の自動テスト範囲を確認する | 実装追従の検証観点を確認したいとき |
| [`designs/bootstrap-foundation.md`](designs/bootstrap-foundation.md) | bootstrap、README、workflow、guardrail の設計契約を確認する | bootstrap 仕様を確認したいとき |
| [`tests/bootstrap-foundation-test-plan.md`](tests/bootstrap-foundation-test-plan.md) | bootstrap foundation の受け入れ観点を確認する | bootstrap 側のレビュー観点を確認したいとき |
| [`../ARCHITECTURE.md`](../ARCHITECTURE.md) | 生成される `.NET` アプリケーションの基礎アーキテクチャ原則を確認する | Clean Architecture / DDD 原則を確認したいとき |

## 更新方針

- `docs/` 配下は Issue / Phase に追従して更新する
- 相対リンクを維持し、生成後リポジトリへ転用しやすい構成を保つ
- Phase 2 では設計境界、Phase 3 では実装済み API / テスト挙動 / 既知の簡略化を明示する
