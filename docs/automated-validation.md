# 自動検証・Windowsビルドの再現手順

すべてリポジトリルートのPowerShellで実行します。Unity CLIとPipelineが導入済みの開発環境向けです。
カメラPython（main.py）を停止し、検証対象以外のUnity Playerを終了してください。試験中に顔画像を取得・保存しません。
結果には人工座標の指示と検査の合否だけを保存します。検証用ファイルは `TestResults/` 以下のGit対象外です。

## 1. 単体テスト

```powershell
./vision/.venv/Scripts/python.exe -m unittest discover -s vision/tests -v
```

Unityを開いている場合、Test RunnerのEditModeで `InteractiveShowcase.Tests` を実行します。
CLIからは以下でも実行できます。

```powershell
unity command test run --caller plugin --skill unity-cli --format json -- --mode editor --filter InteractiveShowcase.Tests --async_tests true
unity command test status --caller plugin --skill unity-cli --format json
```

Unityを閉じている場合：

```powershell
unity test . --mode EditMode --filter InteractiveShowcase.Tests --output TestResults/editmode.xml --timeout 180 --format json -- -logFile Logs/prototype0-tests.log -nographics
```

## 2. Editorの通し試験・異常系・5分試験

`Assets/Prototype0/Scenes/Prototype0.unity` を開いて保存した状態から開始します。

```powershell
unity command editor.playmode start --caller plugin --skill unity-cli --format json
unity status --until-ready --timeout 30 --format json
unity command editor set_auto_tick --caller plugin --skill unity-cli --format json -- --enable true --interval_ms 16 --persist false
unity command script eval 'InteractiveShowcase.ValidationProbe.Begin("TestResults/editor-pipeline");' --caller plugin --skill unity-cli --format json
./vision/.venv/Scripts/python.exe vision/validate_pipeline.py --directory TestResults/editor-pipeline --soak-seconds 300
unity command editor.playmode stop --caller plugin --skill unity-cli --format json
```

成功時は終了コード0、`report.json` の `passed: true` になります。失敗時は終了コード1と `failure` を確認します。
`--soak-seconds 0` で5分間の部分だけを省略できます。既定ポートは5005です。
正常位置5か所、顔なし通知と復帰、送信プロセス停止と復帰、不正データ6種類と各復帰、最後のタイムアウトを順に確認します。
連続試験では同一の送信プロセスを維持したまま人工座標を変え、5分以上にわたり位置・描画構成・フレーム進行・例外を確認します。
対象シーンのMirror X / Flip Yやマーカー構造を変更していると期待値と一致しません。既定設定の検証です。

## 3. Windowsビルド

UnityのPlayを停止した状態で実行します。既存出力と同じ場所へ再ビルドするので、起動中の検証アプリは終了してください。

```powershell
unity command script run Tools/BuildWindows.cs --caller plugin --skill unity-cli --timeout 600 --format json -- --entry Prototype0WindowsBuild.Build --timeout 600
```

- 出力：`Builds/WindowsPrototype0/InteractiveShowcase.exe`
- 対象：Windows x64 / Development Build / Prototype0シーンのみ
- ビルド結果：`TestResults/windows-build.json`
- 既存のBuild Settingsシーン一覧は維持します。

## 4. Windowsアプリの人工データ試験

Unity EditorのPlayを停止したうえで、検証アプリを起動します。

```powershell
$validationDirectory = Join-Path (Get-Location) 'TestResults/player-pipeline'
New-Item -ItemType Directory -Force $validationDirectory | Out-Null
& ./Builds/WindowsPrototype0/InteractiveShowcase.exe --vision-validation-dir "$validationDirectory" -screen-fullscreen 0 -screen-width 960 -screen-height 540 -logFile "$validationDirectory/player.log"
```

別のPowerShellを同じリポジトリルートで開きます。

```powershell
./vision/.venv/Scripts/python.exe vision/validate_pipeline.py --directory TestResults/player-pipeline
```

検証アプリは通常起動では検証ファイルを読み書きしません。上記の明示的な引数を指定した場合だけ有効になります。
試験終了後はアプリを閉じます。通常の実顔確認時は、検証用引数を付けずにexeを起動してください。

## 5. 整備後の再現性・長めの人工通信試験

既存venvに触れず、同じWindows PCの別venvへ構築できます（別PC試験ではありません）。

```powershell
.\Tools\SetupVision.ps1 -EnvironmentPath "$PWD\TestResults\repro\.venv"
.\TestResults\repro\.venv\Scripts\python.exe -m unittest discover -s vision/tests -v
.\TestResults\repro\.venv\Scripts\python.exe vision/doctor.py --no-pause
.\vision\.venv\Scripts\python.exe Tools/audit_repository.py
```

10分の人工通信はDevelopmentアプリのconfigのportを5006にしたうえで、専用の新しい検証フォルダを使って実行します。Pythonも `--port 5006` に合わせます。試験後はアプリを終了し、ビルド先のconfigを元へ戻してください。

```powershell
.\vision\.venv\Scripts\python.exe vision/validate_pipeline.py --directory TestResults/player-soak-final --port 5006 --soak-seconds 600
```

## 6. 配布用ReleaseビルドとZIP

```powershell
unity command script run Tools/BuildWindows.cs --caller plugin --skill unity-cli --timeout 600 --format json -- --entry Prototype0WindowsBuild.BuildRelease --timeout 600
# ソース監査とcommitを済ませた後に、まだ存在しない版番号で実行
.\vision\.venv\Scripts\python.exe Tools/package_release.py --version 0.1.0
.\vision\.venv\Scripts\python.exe Tools/verify_package.py dist/InteractiveShowcase-0.1.0.zip
```

Release出力は `Builds/WindowsPrototype0Release/`、報告は `TestResults/windows-release-build.json`。
梱包時にDevelopment専用ValidationProbeの型名が実行用assemblyに残っていないことを確認します。ZIPとSHA-256ファイルは `dist/` に生成され、Gitには含めません。既存の同じ版の配布フォルダ・ZIPは上書きしません。
`manifest.json` には全同梱ファイル（manifest自身を除く）のSHA-256とソースcommitを記録します。
本体exeのみでなくフォルダ一式が必要です。Release版は内部検証プローブが使えないため、Development版の自動通し試験結果とRelease版の起動確認を区別します。

## 確認範囲

内部状態と描画メッシュの自動確認は、実際の見え方・体感遅延・実カメラの長時間安定性・照明耐性の合格を意味しません。
実顔追従のユーザー確認結果と自動試験結果は `validation.md` で区別します。
