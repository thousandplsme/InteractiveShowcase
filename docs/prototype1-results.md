# Prototype 1 実装・検証結果

実施日：2026-10-27。対象：円の色変更・挨拶・退出後の再受付。
Unity 6000.3.27f1 LTS / Python 3.11.9 x64。P0のシーン・顔検出・通信仕様・設定契約とv0.1.0配布物を維持しています。

## 実装

- `ExperienceStateMachine`：疑似時刻でも試験可能な5状態。1秒検出→2秒挨拶→1秒退出で再受付。
- `ExperienceController`：受信状態を読み、円の色と挨拶表示を制御。円の位置・可視性は既存Followerが担当。
- `ExperienceSettings` と `config/experience.json`：3つの時間を0.1〜10秒で調整。不正・欠落時は挨拶を停止し、設定エラーを通知。
- `Prototype1.unity`：横長Canvas、案内・挨拶・プライバシー説明。M PLUS 1p RegularのTMPアセットを使用し、全表示文字を確認。出典・版・ハッシュとOFLを同梱。
- TMPの文字描画に必要な標準Essentialsリソースを導入。配布元の標準フォント・シェーダーを含むため追加ファイル数は多いものの、Packagesのレンダリングパイプラインは変更していません。
- `StartPrototype1.cmd`：P1専用のWindows起動入口。P0の起動入口は保持。
- P1専用Development／Release出力、梱包対象の明示指定、P1設定・ライセンス・検証プローブ除外の監査を追加。

通常の退出では挨拶を表示し切り、再登場しても期限は延長しません。挨拶中に退出条件が一度成立しても、終了前に戻れば退出待ちになります。通信途絶は通常退出とは区別し、挨拶とタイマーをリセットします。

## 自動検証

| 計画ID | 実施結果・根拠 |
|---|---|
| S01〜S21 | 状態・時間境界・短い退出・同時成立・リセット・再発火防止をEditModeで確認 |
| C01〜C03 | 型・有限値・範囲・不足・重複・不正JSONを確認 |
| C04 | 実シーンで設定ファイル欠落・排他的ロックを試し、エラー通知・挨拶停止・追従継続・復旧を確認 |
| C05、B01 | 既存Python 11件成功、doctor成功。P0 Unity 27件を含む全78件成功 |
| U01〜U09 | 実シーンの表示・色・退出・再有効化・シーン再ロード・timeScale=0・受信状態対応。追加した実ポート競合と文字領域の2テスト再実行も成功 |
| E01〜E08 | Editorで34チェック成功（30.157秒、挨拶開始11回）。正常な顔なしと強制終了を分離し、不正データ6種・最後のNoFace後の停止と再起動も確認 |
| E09 | Windows Developmentで601.125秒・186サイクル成功。異常系を合わせ592チェック、挨拶開始197回が期待値と一致。全体632.937秒、観測エラー0 |
| B02 | P0ソース・既存シーン・Packages/Input設定に意図的変更なし。v0.1.0 ZIPの196ファイルを再検証し、SHA-256一致 |
| B03 | Development：Succeeded、エラー0・警告0、172,220,643 bytes。Release：Succeeded、エラー0・警告0、106,047,451 bytes（いずれもビルド報告値） |
| B04〜B07 | プロジェクト外からStartPrototype1.cmd --helpを実行し、カメラなしで正しいexeと受信ポートが起動することを確認。P1設定2種・ライセンス・assembly・プローブ除外を梱包ツールで検査。ZIP全件検査はpackage-verification.jsonに保存 |
| B08 | Release起動・応答・127.0.0.1ポート確保、起動エラーログ0、ExperienceValidationProbe除外を確認。実顔試験ではない |
| B09 | Git候補のmeta・除外規則・代表的な秘密情報パターン・大容量ファイルを監査。最大候補約2.3MB（TMP標準フォントアセット） |

53シナリオは要件の分類であり、テスト関数数とは一致しません。78件はパラメーター展開とP0回帰を含むUnity Test Runnerの件数です。追加検査は変更した実シーンテスト2件だけを再実行し、いずれも成功しました。

### 根拠ファイル（Git対象外）

- `TestResults/prototype1/unit-tests.json`：全78件。
- `TestResults/prototype1/scene-tests-final.json`：追加した競合・レイアウト検査を含む2件。
- `TestResults/prototype1-python.log`：Python 11件。
- `TestResults/prototype1/editor/report.json`：Editor通し34チェック。
- `TestResults/prototype1/player/report.json`：Windows人工試験。
- `TestResults/prototype1/windows-build.json` / `windows-release-build.json`：ビルド報告。
- `TestResults/prototype1/release-smoke.json`：Release起動確認。
- `TestResults/prototype1/greeting.png` / `greeting-frame.json`：人工入力で挨拶が出たEditor画面（frame 642）。文字・緑の円が表示されたことを確認。実顔・店頭の視認性評価ではない。
- `TestResults/prototype1/package-verification.json`：梱包後のCRC／SHA-256検査結果。
- `TestResults/prototype1/launcher.json`：プロジェクト外からの起動入口確認。

配布版は **0.2.0（Prototype 1）**。ZIPは `dist/InteractiveShowcase-0.2.0.zip`、版の識別はZIP内manifestのprototype・version・sourceCommitと添付SHA-256を使用します。P0のv0.1.0は保持します。
ビルドが自動変更した4件のURP/PlayerSettingsは差分を保存して元へ戻しました。試験終了時は自分で起動したアプリ・送信子プロセスを終了し、検証用ポート設定、EditorのPlay、背景実行、timeScaleを元へ戻します。

### 試験中に遭遇した事項

初回のDevelopmentアプリ起動で30秒の準備待ちを超え、最初の人工試験は観測ファイルがないため失敗しました。別記録 `player/initial-startup-timeout.json` を残し、起動完了とフレーム進行を確認してから再実行しています。初回起動時間の上限は保証しません。
Editorの再コンパイル直後にPipelineが一時切断したため、readyを待って追加試験を実行しました。画面保存用の一時スクリプトのコールバック型も修正しました。これらを製品の試験合格として数えていません。

## 人間確認待ち

P0について報告された手順に沿う起動・検証の完了は記録済みですが、P1の実顔受入へは転用しません。
P1では実顔での1秒待ち・2秒表示・退出と再登場、日本語の読みやすさ、Python停止／復帰の見え方を [操作手順](prototype1-operation.md) に沿って確認してください。
5分超の実カメラ動作、別PC、MSI実機、店頭距離・反射・照明・注目効果、店舗の正式な承認は未確認です。

抽選、おみくじ結果、スロットの回転、3D、音、複数人、個人識別、画像保存・録画・クラウド送信、自動常駐は追加していません。
