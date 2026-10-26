# 第三者ソフトウェアと参照元

- Unity製Windowsプレイヤー、既存Universal Render Pipeline / uGUI構成を使用します。Unityに付属するファイルと必要なデータフォルダは一緒に扱ってください。
- Python本体は同梱しません。別途インストール済みのPython 3.11 x64を利用します。
- MediaPipe / OpenCV / NumPyと間接依存は、`vision/requirements-lock.txt` に版を固定しています。ZIPにはこれらのwheelやvenvを同梱せず、初回セットアップで取得します。
- 各Pythonパッケージのライセンス・著作権表示は、インストール先venvの `Lib/site-packages` 内の各 `.dist-info` とパッケージ内のLICENSE等を参照してください。依存関係を変更・再配布する際は個別に確認してください。
- DXfesは顔landmark・座標取得の考え方の参照元です。DXfesリポジトリは変更していません。チーム制作のアプリ全体、3D素材、出所不明素材は移植していません。
- カメラ画像、録画、人物データ、外部のキャラクターや音楽素材は配布物に含めません。

本資料は依存物を識別するための記録です。第三者の権利を変更するライセンスを付与するものではありません。
