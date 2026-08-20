# Nocturne Modern Controller

『真・女神転生III NOCTURNE HD REMASTER』（Steam版）のダンジョン操作を、現代的な操作感へ近づけるための調査・実験プロジェクトです。

## 現在の結論

右スティック左右による視点回転は、MODでゲームパッドを直接読むのではなく、**reWASDとゲーム標準の場面別キーバインドを組み合わせる方法で解決**しました。

```text
reWASD:
Right Stick Left  -> [
Right Stick Right -> ]

SMT3HD (KEYBOARD+MOUSE / FIELD/DUNGEON):
[ -> 視点変更（左回転）
] -> 視点変更（右回転）

SMT3HD (KEYBOARD+MOUSE / PUZZLE):
[ -> マップ回転（左）
] -> マップ回転（右）
```

`BATTLE`にはこの2キーを割り当てないため、戦闘操作は変更されません。F13/F14も試しましたが、SMT3HDの設定画面はファンクションキーを受け付けないため、未使用の文字キーへ切り替えました。

### reWASD設定の要点

1. `smt3hd.exe`に関連付けた専用ゲームプロファイルを作成します。
2. Xbox Elite Series 2の右スティック高度設定を開きます。
3. 高ゾーンの左方向へ`[`、右方向へ`]`を割り当てます。
4. 設定をスロットへ適用します。
5. SMT3HDの`KEYBOARD+MOUSE`設定で、上記キーをFIELD/DUNGEONとPUZZLEの回転操作へ登録します。

別の未使用文字キーでも構いません。重要なのは、BATTLE側へ同じキーを登録しないことです。

## 調査で判明したこと

- ゲーム内部では`FD_Camera_Left/Right`と`FD_Turn_Left/Right`が別アクションです。
- 標準設定のLB/RB回転は`fldCamera.calcCamNormal()`内で処理されます。
- Steam Input有効時、ゲーム内の左スティックとボタンは正常でも、同一プロセスや外部ヘルパーからXInput値を取得できませんでした。
- Unity Input、ゲーム内部`GetPadAnalog`、Steam Input action、XInput 1.4/1.3/9.1.0、HID、Windows Gaming Inputを試しました。
- Windows Gaming Inputはゲームパッド1台を列挙しましたが、左右スティック、トリガー、全ボタンが常にゼロでした。
- `DDS3_PADCHECK_PRESS`へのネイティブフックは起動時クラッシュを起こしたため、採用していません。
- Steam Inputを無効にすると、この環境ではゲーム内のコントローラー操作自体が使用不能になりました。

詳細は[調査記録](docs/research/RIGHT_STICK_VIEW_AND_DASH_INVESTIGATION.md)を参照してください。

## ダッシュPoC（実機確認済み）

`src/FieldDashPatch.cs`は、LTまたは`P`を押している間だけ通常探索とワールドマップのネイティブ移動基準速度を1.6倍にします。LTとRTを同時押しすると、ダッシュ固定のON／OFFを切り替えられます。

```text
LT hold -> normal field/dungeon + world map movement x1.60
LT + RT -> dash keep ON/OFF
P hold -> keyboard/reWASD fallback
```

Steam版1.0.4の病院内とワールドマップで、LT直入力、LT＋RT固定切替、速度差、壁際、曲がり角を実機確認済みです。`fldPlayerCalc()`実行中だけ通常探索の速度定数`29`／`20`とワールドマップ専用定数`16`を1.6倍にし、直後に必ず復元します。定数が想定値と一致しないゲームバージョンでは自動的に無効化します。BATTLE、はしご、穴、ダメージ、復帰処理では通常探索側のダッシュを動作させないガードを入れています。スクリプトによる高速移動機能は流用していません。

## ビルド

MelonLoaderとSMT3HDの生成済みIl2Cppアセンブリが必要です。

```powershell
dotnet build .\NocturneModernController.csproj -c Release
```

別のゲームフォルダーを使う場合:

```powershell
dotnet build .\NocturneModernController.csproj -c Release `
  -p:GameDir="D:\SteamLibrary\steamapps\common\smt3hd"
```

出力された`NocturneModernController.dll`をゲームの`Mods`フォルダーへコピーします。

## 注意

- ゲーム本体、Atlus/Segaのアセット、生成されたゲームDLLはこのリポジトリに含めません。
- reWASDはサードパーティ製の有料ソフトウェアです。このリポジトリとは無関係です。
- MODや入力変換ツールの利用は自己責任で行ってください。
