# Prototype 1 自動検証・ビルド手順

リポジトリルートで実行します。実カメラPythonを停止し、同じポートを使うアプリがないことを確認します。出力は `TestResults/prototype1/` 以下、Git対象外です。

## 単体・Unity統合

```powershell
.\vision\.venv\Scripts\python.exe -m unittest discover -s vision/tests -v
.\vision\.venv\Scripts\python.exe vision/doctor.py --no-pause
unity command test run --caller plugin --skill unity-cli --format json -- --mode editor --filter InteractiveShowcase --async_tests true
unity command test status --caller plugin --skill unity-cli --format json
```

Unityの結果がcompletedになってから `Temp/pipeline_test_status.json` を保存します。P0/P1の両方を実行するフィルターです。時刻境界は疑似時刻、表示と受信の連携は実シーンのPlay Modeで検証します。

## Windowsビルド

EditorのPlayを停止し、対象のビルド済みアプリを終了します。

```powershell
unity command script run Tools/BuildWindows.cs --caller plugin --skill unity-cli --timeout 600 --format json -- --entry Prototype0WindowsBuild.BuildPrototype1 --timeout 600
unity command script run Tools/BuildWindows.cs --caller plugin --skill unity-cli --timeout 600 --format json -- --entry Prototype0WindowsBuild.BuildPrototype1Release --timeout 600
```

出力はそれぞれ `Builds/WindowsPrototype1/`、`Builds/WindowsPrototype1Release/`。クラス名は既存ツールとの互換性のためPrototype0WindowsBuildのままですが、入口で対象を明示します。
ビルド結果は `TestResults/prototype1/windows-build.json` と `windows-release-build.json`。共通設定・P1設定・フォントのライセンスもコピーします。

## 実アプリの人工通信

1. Development出力の `config/vision.json` だけportを5006にします。リポジトリの設定は変更しません。
2. 実行ごとに新しい観測フォルダを使い、Developmentアプリを起動します。

```powershell
$validationDirectory = Join-Path $PWD 'TestResults/prototype1/player-new'
New-Item -ItemType Directory -Path $validationDirectory | Out-Null
& .\Builds\WindowsPrototype1\InteractiveShowcase.exe --experience-validation-dir "$validationDirectory" -screen-fullscreen 0 -screen-width 960 -screen-height 540 -logFile "$validationDirectory/player.log"
```

3. アプリの起動完了と `status.json` の異なるframe値を確認してから、別のPowerShellで実行します。起動待ち失敗とシナリオ失敗は区別します。初回はビルド等と同時実行せず、起動に時間がかかる場合はログとプロセスを確認します。

```powershell
.\vision\.venv\Scripts\python.exe vision/validate_experience.py --directory TestResults/prototype1/player-new --port 5006 --soak-seconds 600
```

4. `report.json` のpassed、挨拶開始回数、実測継続時間、チェック件数を確認します。正常な顔なし・強制終了・最後の顔なし通知後の停止・不正データ6種・再起動を分けて検証します。
5. 終了後にアプリを閉じ、Development出力のvision.jsonをリポジトリの既定ファイルから戻します。検証のための背景実行と60fps制限はプローブが終了時に復元します。

## 梱包・監査

監査・commit後、未使用の版番号で実行します。

```powershell
.\vision\.venv\Scripts\python.exe Tools/audit_repository.py
.\vision\.venv\Scripts\python.exe Tools/package_release.py --prototype 1 --version 0.2.0
.\vision\.venv\Scripts\python.exe Tools/verify_package.py dist/InteractiveShowcase-0.2.0.zip
```

P0用は従来どおりprototype指定なしです。P1の設定・assembly・プローブ除外はP1を指定したときに追加検査します。Release起動確認は別に行い、Developmentの内部状態確認をReleaseの実顔受入と同一視しません。
