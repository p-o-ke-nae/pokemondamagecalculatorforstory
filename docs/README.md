# Documentation

## 概要

Issue #8 の bootstrap foundation 再設計に関するドキュメントの入口です。`docs/` 配下は Phase 2 の設計契約に加え、Phase 3 で実装に追従した workflow / README / guardrail / ACA skip 挙動をまとめます。リポジトリ直下の [`../ARCHITECTURE.md`](../ARCHITECTURE.md) は生成される `.NET` アプリケーションの基礎アーキテクチャ原則を定義します。

| ドキュメント | 役割 | 読み始めるタイミング |
|---|---|---|
| [`architecture.md`](architecture.md) | Issue #8 で影響を受けるモジュール、設計境界、横断ルールを把握する | 変更対象を俯瞰したいとき |
| [`designs/bootstrap-foundation.md`](designs/bootstrap-foundation.md) | bootstrap、README、workflow、guardrail、`.NET` サンプル API の設計契約を確認する | 実装・レビュー前に仕様を確認したいとき |
| [`tests/bootstrap-foundation-test-plan.md`](tests/bootstrap-foundation-test-plan.md) | 受け入れ基準と検証シナリオの対応を確認する | Step 2.3/3.x のレビュー観点を確認したいとき |
| [`../ARCHITECTURE.md`](../ARCHITECTURE.md) | 生成される `.NET` テンプレートの Clean Architecture / DDD ガイドを確認する | `.NET` 実装の基礎原則を確認したいとき |

## 更新方針

- `docs/` 配下は Issue / Phase に追従して更新する
- 相対リンクを維持し、生成後リポジトリへ転用しやすい構成を保つ
- 受け入れ基準に紐づく契約と検証観点は Phase 2 の時点で明示し、Phase 3 では実装に合わせて具体的な挙動へ更新する
