# Prototype 0 検証記録

確認日：2026-10-26（日本時間）。**実装と下記自動確認は完了。実カメラの受入試験は未確認です。**

追記：ユーザー確認でGame Viewの「No cameras rendering」が判明したため、背景専用Camera（Culling Mask=Nothing）を追加しました。シーン生成コードも更新し、Editor再コンパイルはエラー0・警告0。保存済みシーンの有効Cameraが1台、CanvasがOverlayであることを確認しました。修正後の画面表示・追従は目視確認待ちです。

## 自動確認

| 確認 | 結果・範囲 |
|---|---|
| Python依存関係 | Python 3.11.9 x64の専用venvへインストール、`pip check` 成功 |
| Pythonテスト | 7件成功。顔矩形中心/幅、画角クリップ、不正landmark、実localhost UDP往復、閉じたsocket、カメラ失敗時の解放、MediaPipe実モデルの黒画像処理 |
| Unityコンパイル・シーン生成 | Unity 6000.3.27f1のバッチ実行成功。別シーン `Prototype0.unity` を保存 |
| Unityテスト | 10件成功。JSON検証8ケース、反転を含む座標変換、Play Mode内の実UDP受信/タイムアウト/再有効化/マーカー表示非表示/ポート解放 |
| Git除外 | ルートと将来のサブディレクトリのLibrary/Temp/Logs/Obj/Builds/UserSettings、venv、Pythonキャッシュ、秘密情報ファイル、撮影データ用フォルダを確認 |
| Unity meta | 追加したPrototype0のファイル・フォルダに必要なmetaが揃い、除外されないことを確認 |
| 管理対象監査 | 巨大な生成ファイルなし。既存ファイルを含む最大ファイルは約41KB。代表的な秘密鍵/APIトークンのパターン検査で該当なし |

UnityテストのXMLは `TestResults/editmode.xml`、バッチログは `Logs/` に生成されます。ログ・テスト出力はGit対象外です。顔画像・顔情報のログは作成していません。

再実行（Unity Editorを閉じた状態）：

```powershell
./vision/.venv/Scripts/python.exe -m unittest discover -s vision/tests -v
unity test . --mode EditMode --filter InteractiveShowcase.Tests --output TestResults/editmode.xml --timeout 180 --format json -- -logFile Logs/prototype0-tests.log -nographics
```

Unityを開いた状態ではTest Runnerから同じテストを実行できます。通信テストはEditModeテストから一時的にPlay Modeへ入り、終了時に戻ります。

## 未確認（ユーザーの目視確認待ち）

- 実Webカメラの取得と実顔検出、Python→Unityを通した連続追従
- 左右・上下の実際の見え方、円形マーカーの描画、自然さ・揺れ・体感遅延
- 顔が消えた際の非表示、実Pythonプロセス停止/再起動時の見え方
- 実カメラを用いた約5分連続動作
- MSI GE76 RAIDER 10UG、ショーケース越し、反射・照明・設置角度、店頭での承認・機材安全

人工データによるテスト成功を、上記受入項目の合格とは扱いません。現時点で単体テストはPython送信側とUnity受信側をそれぞれ実UDPで確認しており、実カメラを含む通し確認はしていません。

## 留意点・解決済みの検証上の問題

- サンドボックス内ではPython UDP受信がタイムアウトしました。同一テストを通常のローカル通信権限で再実行して7件成功を確認しています。
- シーン生成の初回バッチ処理は、Unityの未保存Untitledシーンからadditive生成できない制約で失敗しました。バッチ専用入口で既存SampleSceneを開いてから別シーンを生成するよう修正し、成功を確認しました。
- Unity通信テストの初回はEditModeでのMonoBehaviourメッセージ呼び出し制約に当たりました。Play Mode内で検証するよう修正し、10件成功を確認しました。
- MediaPipeはfeedback tensor関連の警告を標準エラーへ出します。モデル初期化と黒画像処理は成功しています。実顔検出の品質は未確認です。
- PipelineはEditor操作のため導入した公式の実験的パッケージです。Prototype 0の顔情報通信はこれに依存しません。
- 同じUnityバージョンでの初期読み込みにより、PC_RPAsset・ShaderGraphSettings・URPProjectSettingsのシリアライズ形式更新とSceneTemplateSettingsの生成が発生しています。
- スタンドアロンビルドは今回作成していません。既存Build SettingsのSampleSceneは維持しています。

## DXfes参照証跡

参照は読み取りのみ。コード全体・チーム制作素材のコピーはありません。

| ファイル | 確認したGit blob SHA |
|---|---|
| ai_logic.py | `096a032995f83dec61f447a4c55531d1f10df3f4` |
| demo_final.py | `bb88bc749591f83bb3599bf0f28384fbe3ec2e11` |
| demo_day.py | `c8c808e2238dc6c70c97eb0b4192994dbcf3ddb9` |
| final_tune.py | `df29e1622b67d97df34dd1c679d01406a4b4d808` |
| README.md | `ac8e340454bfb736d1d24ed906b3c293c2d33311` |
