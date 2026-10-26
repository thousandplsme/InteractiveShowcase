# InteractiveShowcase — Prototype 0

中古PC店舗のショーケース向けインタラクティブ展示の技術検証です。
Webカメラの顔位置をPythonで検出し、同一PCのUnity Canvas上の円形マーカーを追従させます。
**実装対象はPrototype 0のみ。Windowsアプリでの実顔追従、気にならない程度の体感遅延、実カメラでの約5分連続動作をユーザー確認済みです。5分を超える連続動作と店頭環境は未確認です。**

左右・上下の追従方向と、顔の退出・再登場もユーザー確認済みです。
初回は `SetupVision.cmd`、診断は `CheckEnvironment.cmd` を使用できます。
配布版の操作・設定・復旧方法は [distribution.md](docs/distribution.md)、整備作業の結果は [completion.md](docs/completion.md) を参照してください。
整備版は `v0.1.0` として固定します。自動確認はPython 11件・Unity 27件、Windows人工通信10分（443チェック）です。実カメラの5分超確認とは区別しています。

## 構成

既存のURPプロジェクトがリポジトリ直下に作成済みのため、移動・複製せず維持しています。

```text
InteractiveShowcase/
├── Assets/Prototype0/
│   ├── Scripts/         # UdpVisionReceiver / FaceFollower / CircleMarker
│   ├── Editor/          # 新規シーン生成メニュー
│   ├── Scenes/          # Prototype0.unity
│   └── Tests/Editor/    # Unity自動テスト
├── Packages/            # 既存URP構成を保持
├── ProjectSettings/
├── StartVision.cmd        # Python通常起動（現在の作業フォルダに依存しない）
├── StartVisionPreview.cmd # カメラプレビュー付き起動
├── StartShowcase.cmd      # ReleaseアプリとPythonを起動
├── SetupVision.cmd        # Python 3.11 x64の専用venvを構築
├── CheckEnvironment.cmd   # カメラを開かない依存関係・設定・ポート診断
├── config/vision.json     # Python / Unity共通の起動設定
├── Tools/BuildWindows.cs  # Editorから検証用Windowsビルドを作成
├── vision/
│   ├── main.py
│   ├── face_detector.py
│   ├── udp_sender.py
│   ├── validate_pipeline.py # カメラを使わない通し検証
│   ├── run_vision.cmd      # 起動スクリプト共通処理
│   ├── requirements.txt
│   ├── requirements-lock.txt
│   └── tests/
└── docs/architecture.md
```

元の `Assets/Scenes/SampleScene.unity`、Input設定、URPアセットは保持しています。
新しいシーンは2D Canvasのみを使用します。将来の3D展示用にURPを維持しています。
Game Viewの背景描画用Cameraを1台配置しています（Culling Mask=Nothing）。3D演出はありません。

## 環境

- Windows / Unity **6000.3.27f1 (6.3 LTS)** / URP **17.3.0** / uGUI **2.0.0**
- Python **3.11.9 x64**（既存インストールを利用）/ Git **2.52.0.windows.1**
- MediaPipe **0.10.21** / OpenCV (`opencv-contrib-python`) **4.11.0.86** / NumPy **1.26.4**
- Editor操作・検証用：Unity CLI **1.0.0-beta.14**、`com.unity.pipeline` **0.9.1-exp.1**。Python送信・Unity実行の通信には使用しません。

MediaPipeの既存Face Mesh Solutions APIを使うためバージョンを固定します。
[配布ファイル](https://pypi.org/project/mediapipe/0.10.21/#files)のあるPython 3.11を使用し、PATH上のPython 3.13にそのままインストールしないでください。
OpenCVパッケージの重複を避けるため、`opencv-python` を追加インストールしません。
MediaPipeの間接依存としてJAX・Matplotlib・sounddevice等が入りますが、アプリでは音声取得や音声出力を行いません。

## Windowsでのセットアップ

以下はリポジトリルートでPowerShellから実行します。仮想環境のactivateは不要です。

`SetupVision.cmd` でも以下と同じ固定依存関係を構築できます。既存venvを削除せず、Pythonの版が異なる場合やvenv以外のフォルダがある場合は停止します。再実行時も同じ固定一覧を使用します。

```powershell
# Python Launcherで3.11が見つかる環境
py -3.11 -m venv vision/.venv
# Launcherで見つからない場合、既存のPython 3.11を直接指定
# & "$env:LOCALAPPDATA/Programs/Python/Python311/python.exe" -m venv vision/.venv
./vision/.venv/Scripts/python.exe -m pip install -r vision/requirements-lock.txt
./vision/.venv/Scripts/python.exe -m pip check
```

`requirements.txt` は主要依存の固定値、`requirements-lock.txt` は検証時の間接依存を含む固定一覧です。
初回セットアップ時はパッケージ取得にインターネットが必要です。検出モデルはMediaPipeパッケージに含まれ、実行中のモデルダウンロードやクラウド推論は行いません。

## 実行

1. Unity Hubから**このリポジトリルート**をUnity 6000.3.27f1で開きます。
2. `Assets/Prototype0/Scenes/Prototype0.unity` を開いてPlayします。
   シーンがない場合のみ `InteractiveShowcase > Create Prototype 0 Scene` を実行します。既存シーンは上書きしません。
3. 別のPowerShellでPythonを起動します。

```powershell
./vision/.venv/Scripts/python.exe vision/main.py --camera 0
```

セットアップ済みなら、ルートの **`StartVision.cmd` をダブルクリック**しても起動できます。
映像を確認する場合は **`StartVisionPreview.cmd`** を使います。どちらも自動的にプロジェクトのフォルダへ移動し、専用venvのPythonを選びます。venvがない場合はセットアップ案内を表示して終了します。エラー時はメッセージを読めるようウィンドウを保持します。

PowerShellの現在位置にかかわらず、絶対パスでも起動できます。

```powershell
& "C:\Users\25111103\Documents\GitHub\InteractiveShowcase\StartVisionPreview.cmd" --camera 0
```

別のカメラは `--camera 1` 等で指定します。`--preview` を付けると保存せずにカメラ映像と検出中心を一時表示します（**プレビューは左右反転なし**）。終了はCtrl+C、プレビューありではQも使えます。
Unityを先に起動してもPythonを先に起動しても構いません。既定では両者が `config/vision.json` を読みます。Pythonの `--camera` / `--port` はその起動だけの上書きです。`--port` を変更する場合はUnity側の設定も一致させてください。

通常は `config/vision.json` で調整し、両方を再起動します。Editorではリポジトリ直下、Windowsアプリではexeと同じフォルダのconfigを読みます。Inspectorで個別調整する場合のみ、対象コンポーネントのUse Shared SettingsをOFFにしてください。

| 設定 | 既定 | 意味 |
|---|---|---|
| mirrorX | true | 鏡のように左右反転。生カメラ映像と同じ座標方向にするならfalse |
| flipY | true | 画像の下向きYをCanvasの上向きYへ変換 |
| smoothTime | 0.08秒 | 追従の平滑化。大きいほど緩やか |
| timeoutSeconds | 0.5秒 | 有効データが来なくなったら非表示 |

EditorのReceiverのInspectorには受信状態を表示します。Waiting=有効データ待ち、NoFace=通信中だが顔なし、Tracking=追従中、TimedOut=通信途絶、Error=設定/受信エラー、Stopped=停止です。人物情報や受信座標のログは追加しません。

最初の検出・再検出時は顔位置へ即座に表示し、継続検出中のみ補間します。
顔サイズは送信・保持しますがPrototype 0のマーカーは直径80の固定サイズです。
UI操作がないためEventSystemやGraphicRaycasterは不要です。

## 自動テスト

カメラを開かず、人工座標・黒画像・localhost UDPで確認します。

```powershell
./vision/.venv/Scripts/python.exe -m unittest discover -s vision/tests -v
```

Unityは `Window > General > Test Runner > EditMode` で `InteractiveShowcase.Tests` を実行します。
実UDP受信、タイムアウト、不正データ、再有効化、ポート解放、座標変換を検査します。
テストは一時ポートを利用し、通常の5005へ割り込みません。
検証結果と未確認事項は [docs/validation.md](docs/validation.md) に記載します。

人工データの通し試験・異常系・5分試験とWindowsビルドの再現手順は [docs/automated-validation.md](docs/automated-validation.md) に記載します。
この試験はカメラを開きません。通常の顔検出Pythonは停止し、Unity EditorとWindowsアプリの受信を同時に起動しないでください（どちらも既定ポート5005を使用します）。

## Windows用の検証アプリ

出力先は `Builds/WindowsPrototype0/InteractiveShowcase.exe` です。作成済みビルドがある場合は、このexeを起動してから `StartVision.cmd` または `StartVisionPreview.cmd` を実行します。
別PCへ渡す場合はexeだけでなく **`WindowsPrototype0` フォルダ全体**が必要です。Python環境もそのPCで別途セットアップします。
これは検証専用のDevelopment Buildです。配布用Release版は `Builds/WindowsPrototype0Release/`、Pythonや操作説明をまとめたZIPは `dist/` に生成します。`StartShowcase.cmd` はRelease版と、そのexe横の設定を使うPythonを起動します。再ビルド・梱包手順は前述の自動検証手順を参照してください。

初心者向けの仕組み・コード解説は [code-guide.md](docs/code-guide.md) を参照してください。

## 人間による受入試験（開発用PC）

| 項目 | 期待結果 |
|---|---|
| 顔を映す | 円形マーカー表示 |
| 左右・上下へ移動 | 設定した方向へ自然に追従。過度な揺れや遅れがない |
| 顔を画角外へ | 未検出パケット受信後に非表示 |
| 約5分連続 | 異常終了や顕著な遅延増加がない |
| Pythonを停止 | Unityが固まらず、遅くとも最後の有効受信から約0.5秒で非表示 |
| Pythonを再起動 | Unityを再起動せず追従復帰 |

Unity Editorのバックグラウンド停止や一時停止中は表示更新されません。確認時はPlay中のGameビューを前面にしてください。
カメラを他アプリが使用中の場合はそのアプリを閉じ、Windowsのカメラ利用権限も確認します。
カメラのオープン・取得に失敗した場合はメッセージを出して終了します。再接続後にPythonを再起動してください。
ポート競合はUnity Consoleに通知されます。もう一つのReceiverやアプリを停止し、Playを再開始してください。

## DXfesとの関係・権利範囲

[DXfes / AttenCheck](https://github.com/thousandplsme/DXfes) の顔検知技術を発展させた別プロジェクトです。
指定された `ai_logic.py`、`demo_final.py`、`demo_day.py`、`final_tune.py`、`README.md` を参照しました。
参照元READMEでは「顔検知・AI機能＝本人」「アプリ枠組み・3D素材＝チームメンバー」と記載されています。

再利用した考え方はOpenCV取得、BGR→RGB変換、MediaPipe最大1人のlandmark取得です。顔中心・サイズ計算、UDP通信、Unity表示は本プロジェクト向けに実装しました。
`ai_logic.py` のスコア処理には未定義の `abs_pitch` / `abs_roll` があり、そのまま実行すると問題が生じるため移植しません。顔向き計算も実角度としての精度が未確認なので対象外です。
チーム制作のアプリ枠組み、3Dモデル、画像、発表資料は移植していません。混在ファイル全体の単独著作性は未確認であり、将来丸ごと移植する場合には権利確認が必要です。DXfesへの変更はありません。

## プライバシー・制限

- 顔画像の保存・録画・個人識別・年齢/性別/感情推定・クラウド送信を行いません。
- カメラ画像はメモリ上で処理し、UDPでは検出有無と正規化座標・サイズのみをlocalhostへ送信します。
- 顔座標・画像をログへ出力しません。`--preview` はローカルの一時表示のみです。
- 最大1人。複数人が映った際の対象人物の固定や個人追跡は保証しません。
- UDPは到達保証がなく、途絶時は非表示とします。正常復帰は次の有効パケットで行います。
- ガチャ、運勢、JACKPOT、キャラクター、3D演出、Particle/Shader、音、視線・瞬き操作、GPU計測、FPS表示、自動起動、外部APIは対象外です。
- MSI GE76 RAIDER 10UG向けの最適化・実機検証は行っていません。
- 店頭導入前にカメラ・ガラス越し検出・反射/照明/角度、店舗責任者の撮影/案内方法承認、機材安全を別途確認してください。

UnityはVisible Meta Files / Force Textです。`.meta` を管理し、Unityキャッシュ、仮想環境、ビルド、テスト出力、秘密鍵・`.env`・撮影データは除外します。
既存URPアセット等にはUnity 6000.3.27f1によるシリアライズ形式の更新が含まれます。レンダリング機能の追加やパイプライン変更はしていません。
`.gitignore` はGitHub公式の [Unity](https://github.com/github/gitignore/blob/main/Unity.gitignore) / [Python](https://github.com/github/gitignore/blob/main/Python.gitignore) テンプレートを参考にしています。
Git LFSはPrototype 0では使用せず、大きな3Dアセットを追加する時点で再検討します。
