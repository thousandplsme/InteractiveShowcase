# Prototype 0 整備・完了記録

対象：2026-10-26、Prototype 0の仕様内でAIだけで実施できる整備。
Unity 6000.3.27f1 LTS / Python 3.11.9 x64。既存URPプロジェクト、シーン、入力設定を維持します。
ガチャ・3D演出・音・視線・複数人・クラウド・常駐/自動起動は追加しません。

## 実施したタスク

| タスク | 成果物・確認方法 |
|---|---|
| 人間の確認済み項目を整理 | `validation.md` に追従方向・退出/再登場も記録 |
| セットアップを簡単にする | SetupVision.cmd / Tools/SetupVision.ps1 / vision/setup_environment.py。既存Python 3.11 x64を使い専用venvへ固定依存を入れる |
| 誤った環境の上書きを防ぐ | Python 3.13・venvでない既存フォルダは処理を拒否。既存venvを削除しない |
| 起動前診断 | CheckEnvironment.cmd / vision/doctor.py。カメラを開かずPython・固定依存・import・設定・ポートを確認 |
| 設定の集約 | config/vision.json、PythonとUnity双方の型・範囲・重複・必須項目検査。初期値を保持 |
| 開発者向け状態表示 | ReceiverのInspectorにWaiting / NoFace / Tracking / TimedOut / Error / Stopped。展示画面へのUI追加なし |
| 単体・回帰テスト | Python 11件、Unity 27件。設定、座標、UDP、タイムアウト、再受信、カメラread失敗時の解放など |
| 環境構築の再現試験 | 同じWindows PCの別venvへ依存を新規インストールし、pip check・11テスト・doctorが成功 |
| 10分の人工通信試験 | Windows Developmentアプリで600.578秒、420回の位置変更、異常系等を含む443チェックが成功。Unity Error/Exception/Assertは0 |
| 作業フォルダに依存しない起動 | プロジェクト外からStartVision / StartVisionPreviewのhelp、CheckEnvironmentを実行 |
| Windows配布用ビルド | ReleaseとDevelopmentを別出力。両方ともエラー0・警告0 |
| Releaseの機械的な起動確認 | 起動後の生存・応答・localhost受信口・エラーログ0。ValidationProbeの型が実行assemblyにないことを確認 |
| 配布パッケージ | Tools/package_release.pyでReleaseアプリ・必要なPython・操作資料を梱包。manifestにcommit・ファイルSHA-256、ZIPにもSHA-256を付ける |
| 復旧・操作手順 | distribution.md。版ごとの別フォルダ運用、設定復旧、カメラやポートの対処 |
| 初心者向け技術解説 | code-guide.md。顔中心、正規化、UDP、スレッド、座標変換、平滑化を説明 |
| Git管理対象監査 | Tools/audit_repository.py。除外規則、meta、巨大ファイル、代表的な秘密情報パターンを検査 |
| 版の固定 | 整備ソースをcommitし、Privateリポジトリのタグで固定。大容量ZIPやvenvはGit対象外 |

## 自動確認の根拠

- `TestResults/unit-tests.json`：Unity 27/27成功。
- `TestResults/repro-setup.log` / `repro-tests.log`：新規venvの構築・pip check・Python 11/11成功。
- `TestResults/setup-guards.json`：異なるPython版・venvでないフォルダの拒否。
- `TestResults/windows-build.json`：Development成功、169,131,216 bytes。
- `TestResults/windows-release-build.json`：Release成功、102,981,199 bytes（ビルド報告値）。
- `TestResults/release-smoke.json`：Release起動、localhost UDP、応答、エラーログ0、ValidationProbe除外。目視・実顔試験ではない。
- `TestResults/player-soak-final/report.json`：10分人工通信が成功。試験全体は625.079秒、443チェック。

配布版番号は **0.1.0**、タグは **v0.1.0**。出力は `dist/InteractiveShowcase-0.1.0.zip` と `.zip.sha256` です。
梱包後に `Tools/verify_package.py` で全ファイルのSHA-256・ZIPのCRC・必須ファイル・検証プローブ除外を検査し、結果を `TestResults/package-verification.json` に保存します。ソースcommitはZIP内のmanifestで識別できます。大容量ZIPをGitHubへアップロードする処理は含めません。

ビルドが生成した4件の既存URP/PlayerSettings差分は、`TestResults/final-build-generated-settings.patch` へ退避して復元しました。既存シーン一覧は変更していません。検証で起動したアプリを終了し、専用ポート設定も初期値へ戻します。

## 人間が確認済みの範囲

整備前のWindows動作確認版について、実顔追従、左右/上下の方向、顔の退出/再登場、気にならない程度の体感遅延、実カメラ約5分連続動作を確認済みです。ユーザー報告を根拠とし、自動試験からの推定ではありません。

## この整備で確認していない範囲

- 新しいRelease版での実顔の目視。共通設定の初期値は確認済み版と同じです。
- 5分を超える実カメラの連続動作。人工通信の時間を加算しません。
- 実カメラ使用中の物理的な抜き差しやPython再起動の見え方。人工プロセス停止/復帰と模擬read失敗は別に試験します。
- 別PCの初期環境、MSI GE76 RAIDER 10UG、ショーケース越し、反射・照明・角度、店頭承認・機材安全。

これらを自動試験の成功だけで合格扱いにしません。追加の演出仕様や店頭運用は次の段階で決める事項です。Prototype 1には着手しません。
