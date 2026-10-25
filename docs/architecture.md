# Prototype 0 アーキテクチャ

```text
Webカメラ
  → vision/main.py : OpenCV取得（目標640×480、最大約30送信/秒）
  → face_detector.py : BGR→RGB、MediaPipe Face Mesh、1人のlandmark
  → udp_sender.py : JSON / UDP、127.0.0.1:5005のみ
  → UdpVisionReceiver : バックグラウンド受信 → メインスレッド解析
  → FaceFollower : Canvas座標変換・平滑化・表示切替
  → CircleMarker : uGUIの円形メッシュ
```

## 責務と座標

`main.py` はカメラ所有・フレーム処理・送信頻度・終了時の後始末を担当します。
カメラエラー時は未検出を送って終了します。再開はPython再実行です。
`face_detector.py` は最大1人の468点から、画角にクリップした外接矩形を求めます。
外接矩形中心がx/y、横幅を画像幅で割った値がsizeです。
元画像は左右反転せず、左上=(0,0)、右下=(1,1)。値はすべて0〜1です。
不正な点、画角外だけにある矩形、幅・高さゼロは未検出とします。

```json
{"faceDetected":true,"x":0.52,"y":0.41,"size":0.28}
```

```json
{"faceDetected":false}
```

`udp_sender.py` は非ブロッキングUDPでメタデータだけを送信します。
送信失敗時は次のフレームで再試行し、連続エラーを毎フレームログ出力しません。

## 受信とライフサイクル

`UdpVisionReceiver` はloopbackアドレスのみにbindします。受信スレッドはUTF-8文字列・受信時刻を最新1件のスロットへ渡し、Unity APIに触れません。
キューを蓄積せず、メインスレッドのUpdateで最新データをJsonUtility解析します。
1024バイト超、壊れたUTF-8、壊れたJSON、必須値不足、非有限・範囲外の顔座標は無視します。
不正データはタイムアウトを延長しません。有効データは顔なし通知も含めて保持します。
時刻は受信時点の単調増加時計を使用し、OS時刻変更の影響を受けません。

最後の有効受信から0.5秒超、Receiver無効、受信停止時は未検出扱いです。
無効化時はsocketを閉じて待機受信を解除し、スレッド終了を最大250msだけ待ちます。
ポート競合・受信障害はメインスレッドのConsole通知とし、無限待機しません。

## Canvas

Overlay Canvas、基準1920×1080、Scale With Screen Size、Match 0.5。
`FaceFollower` はCanvasの現在サイズを用いて座標を変換します。X反転とY反転は独立したInspector設定です。
マーカーはCanvas直下、中心anchor/pivotで直径80。サイズ情報による拡縮は行いません。
初回・復帰時は即座に位置を合わせ、連続検出中はSmoothDamp（既定0.08秒、unscaledDeltaTime）で平滑化します。
未検出時はマーカーのみ非アクティブにし、受信とFollowerを動かし続けます。
`CircleMarker` は64分割の円を生成し、外部画像・カスタムShader・3D素材を使いません。

## 既存プロジェクトと開発ツール

Unityは既存ルートを維持し、既存SampleScene・Input設定を保持します。URPの機能設定は手動変更せず、Editorによるアセット形式更新のみ含みます。
シーン生成ツールは別シーンをadditiveで生成・保存し、元シーンの未保存内容を破棄しません。
作成済みのPrototype0シーンも上書きしません。
PipelineパッケージはEditor操作・自動検証用です。展示の顔情報通信は独立したUDPのみです。
テストはカメラを開かず、人工データで行います。実機の追従品質・照明耐性は自動テストから推定しません。
