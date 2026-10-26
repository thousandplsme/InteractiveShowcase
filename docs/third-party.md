# 第三者ソフトウェアと参照元

Prototype 1ではM PLUS 1p Regular Version 1.062（Copyright 2016 The M+ Project Authors、SIL OFL 1.1）を使用します。[Google Fonts公式配布](https://github.com/google/fonts/tree/main/ofl/mplus1p)から取得し、TTFは改変せず、表示文字用のTMP描画アセットを生成しています。出典・ハッシュは `Assets/Prototype1/Fonts/README.md`、配布版には `Licenses/MPLUS1p-OFL.txt` を同梱します。
TMP Essentialsに含まれるLiberation SansとEmojiOneの表示・ライセンス資料も配布版のLicensesへ同梱します。Prototype 1では独自のキャラクター素材や音楽素材は追加しません。

- Unity製Windowsプレイヤー、既存Universal Render Pipeline / uGUI構成を使用します。Unityに付属するファイルと必要なデータフォルダは一緒に扱ってください。
- Python本体は同梱しません。別途インストール済みのPython 3.11 x64を利用します。
- MediaPipe / OpenCV / NumPyと間接依存は、`vision/requirements-lock.txt` に版を固定しています。ZIPにはこれらのwheelやvenvを同梱せず、初回セットアップで取得します。
- 各Pythonパッケージのライセンス・著作権表示は、インストール先venvの `Lib/site-packages` 内の各 `.dist-info` とパッケージ内のLICENSE等を参照してください。依存関係を変更・再配布する際は個別に確認してください。
- DXfesは顔landmark・座標取得の考え方の参照元です。DXfesリポジトリは変更していません。チーム制作のアプリ全体、3D素材、出所不明素材は移植していません。
- カメラ画像、録画、人物データ、外部のキャラクターや音楽素材は配布物に含めません。

本資料は依存物を識別するための記録です。第三者の権利を変更するライセンスを付与するものではありません。
