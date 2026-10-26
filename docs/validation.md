# Prototype 0 検証記録

確認日：2026-10-26（日本時間）。**Windowsアプリでの実顔追従、気にならない程度の体感遅延、実カメラでの約5分連続動作をユーザー確認済みです。5分を超える連続動作は未確認です。**

## ユーザーによる実カメラ確認

以下は自動試験からの推測ではなく、ユーザーの報告に基づく結果です。

| 項目 | 報告された結果 |
|---|---|
| Windowsアプリでの実顔追従 | 問題なし |
| 体感遅延 | 気にならない程度。遅延時間の数値測定はしていない |
| 実カメラでの約5分連続動作 | 特に違和感なく動作可能 |
| 5分を超える連続動作 | ユーザーは未確認と明示 |

ユーザー確認でGame Viewの「No cameras rendering」が判明したため、背景専用Camera（Culling Mask=Nothing）を追加しました。シーン生成コードも更新し、保存済みシーンの有効Cameraが1台、CanvasがOverlayであることを確認しました。

Pythonプレビューでは顔付近に緑の円が表示される一方、Unityに表示されないとの報告を受け、CircleMarkerにCanvasRendererが欠落していることを確認しました。既存シーン・生成コードを修正し、RequireComponentで新規追加時にも必須化しました。描画メッシュ66頂点・64三角形・マテリアル1件を確認し、修正後にユーザーから「無事に追従しました」との報告を受けました。その後、追加2件を含むUnity全12テストを実行して成功を確認しています。

## 自動確認

| 確認 | 結果・範囲 |
|---|---|
| Python依存関係 | Python 3.11.9 x64の専用venvへインストール、`pip check` 成功 |
| Pythonテスト | 7件成功。顔矩形中心/幅、画角クリップ、不正landmark、実localhost UDP往復、閉じたsocket、カメラ失敗時の解放、MediaPipe実モデルの黒画像処理 |
| Unityコンパイル・シーン生成 | Unity 6000.3.27f1のバッチ実行成功。別シーン `Prototype0.unity` を保存 |
| Unityテスト | 12件成功。JSON検証8ケース、座標変換、Play Mode内の実UDP受信/タイムアウト/再有効化/表示非表示/ポート解放、CanvasRenderer自動付与、保存済みシーンの描画部品 |
| Python→Unity通し検証 | Editorの実シーンで成功。Pythonの実UdpSenderから送信し、5か所の位置・表示状態・描画メッシュを確認 |
| 異常系 | 送信Python子プロセスの強制停止→タイムアウト→再起動、不正JSON/範囲外/必須値不足/NaN/不正UTF-8/サイズ超過からの復帰が成功 |
| 5分人工データ試験 | 300.328秒・212回の人工位置変更に成功。異常系等を合わせ235チェック成功。観測中のUnity Error/Exception/Assertは0 |
| 起動スクリプト | プロジェクト外のフォルダから通常/プレビュー両方の`--help`が成功。venv欠落時は案内と終了コード1を確認。カメラ自体は起動していない |
| Windowsビルド | Unity 6000.3.27f1、Windows x64 Development Build、Prototype0シーンのみ。Succeeded / エラー0 / 警告0。ビルド報告169,125,856 bytes |
| Windowsアプリ起動・受信 | ビルドしたexeをDirect3D 11で起動。カメラを使わない通し・異常系23チェックに成功（17.516秒）。検証中のUnity Error/Exception/Assertは0。試験後に検証アプリを終了 |
| Git除外 | ルートと将来のサブディレクトリのLibrary/Temp/Logs/Obj/Builds/UserSettings、venv、Pythonキャッシュ、秘密情報ファイル、撮影データ用フォルダを確認 |
| Unity meta | 追加したPrototype0のファイル・フォルダに必要なmetaが揃い、除外されないことを確認 |
| 管理対象監査 | 巨大な生成ファイルなし。既存ファイルを含む最大ファイルは約41KB。代表的な秘密鍵/APIトークンのパターン検査で該当なし |

今回のUnity全12テスト結果は `TestResults/unit-tests.json` です。以前のバッチ実行のXMLは `TestResults/editmode.xml`、バッチログは `Logs/` にあります。ログ・テスト出力はGit対象外です。顔画像・受信した顔座標のログは作成していません。

通し試験・異常系・5分試験の根拠は `TestResults/editor-pipeline/report.json` です。人工データの送信プロセスとUnityのフレーム進行を確認しながら実行しています。位置一致は既定Canvas設定で誤差2以内、異なるフレームの正常応答が複数回続くことを条件としています。描画メッシュの成立確認は画面の視覚的な品質評価ではありません。再現手順は [automated-validation.md](automated-validation.md) を参照してください。

Windowsビルドの根拠は `TestResults/windows-build.json`、アプリ内の自動検証結果は `TestResults/player-pipeline/report.json` です。この人工データ試験自体は画面の目視評価ではありません。その後、上記のユーザー報告によりWindowsアプリでの実顔追従も確認できました。出力フォルダ全体は293ファイル・169,319,490 bytes（約161.5 MiB）です。

再実行（Unity Editorを閉じた状態）：

```powershell
./vision/.venv/Scripts/python.exe -m unittest discover -s vision/tests -v
unity test . --mode EditMode --filter InteractiveShowcase.Tests --output TestResults/editmode.xml --timeout 180 --format json -- -logFile Logs/prototype0-tests.log -nographics
```

Unityを開いた状態ではTest Runnerから同じテストを実行できます。通信テストはEditModeテストから一時的にPlay Modeへ入り、終了時に戻ります。

## 未確認（ユーザーの目視確認待ち）

- 左右・上下それぞれの追従方向の個別記録（追従全般は問題なしとの報告済み）
- 顔が消えた際の非表示、実Pythonプロセス停止/再起動時の見え方
- 5分を超える実カメラの連続動作
- MSI GE76 RAIDER 10UG、ショーケース越し、反射・照明・設置角度、店頭での承認・機材安全

人工データによるテスト成功を、上記項目の合格とは扱いません。ユーザーが報告した追従・体感遅延・約5分の連続動作の範囲を超えて、長時間運用や店頭適性を保証しません。

## 留意点・解決済みの検証上の問題

- サンドボックス内ではPython UDP受信がタイムアウトしました。同一テストを通常のローカル通信権限で再実行して7件成功を確認しています。
- シーン生成の初回バッチ処理は、Unityの未保存Untitledシーンからadditive生成できない制約で失敗しました。バッチ専用入口で既存SampleSceneを開いてから別シーンを生成するよう修正し、成功を確認しました。
- Unity通信テストの初回はEditModeでのMonoBehaviourメッセージ呼び出し制約に当たりました。Play Mode内で検証するよう修正し、10件成功を確認しました。
- MediaPipeはfeedback tensor関連の警告を標準エラーへ出します。モデル初期化・黒画像処理は成功し、実顔追従もユーザー確認済みです。照明などの条件を変えた検出品質は未確認です。
- PipelineはEditor操作のため導入した公式の実験的パッケージです。Prototype 0の顔情報通信はこれに依存しません。
- 同じUnityバージョンでの初期読み込みにより、PC_RPAsset・ShaderGraphSettings・URPProjectSettingsのシリアライズ形式更新とSceneTemplateSettingsの生成が発生しています。
- Windows検証用ビルドを作成しました。既存Build SettingsのSampleSceneは維持し、ビルド時だけPrototype0を指定しています。
- 今回のビルドが自動生成したURP/PlayerSettingsの差分は、`TestResults/build-generated-settings.patch` に退避して元に戻しました。検証終了後はEditorのPlayを停止し、Run In Backgroundも元のfalseに戻っています。

## DXfes参照証跡

参照は読み取りのみ。コード全体・チーム制作素材のコピーはありません。

| ファイル | 確認したGit blob SHA |
|---|---|
| ai_logic.py | `096a032995f83dec61f447a4c55531d1f10df3f4` |
| demo_final.py | `bb88bc749591f83bb3599bf0f28384fbe3ec2e11` |
| demo_day.py | `c8c808e2238dc6c70c97eb0b4192994dbcf3ddb9` |
| final_tune.py | `df29e1622b67d97df34dd1c679d01406a4b4d808` |
| README.md | `ac8e340454bfb736d1d24ed906b3c293c2d33311` |
