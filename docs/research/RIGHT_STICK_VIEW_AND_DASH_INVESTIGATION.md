# SMT3HD 右スティック視点左右・LB/RBダッシュ調査

## 2026-08-18 実機検証の最終結果

Xbox Elite Series 2 + Steam Input環境では、ゲーム内操作が正常でも、Unity Input、ゲーム内部`GetPadAnalog`、Steam Input action、複数世代のXInput、HID、Windows Gaming Inputのいずれからも右スティック実値を安定取得できなかった。Windows Gaming Inputは1台を列挙したものの、左右スティック、トリガー、全ボタンが常にゼロだった。Steam Inputを無効にするとゲーム内のコントローラー操作自体が使用不能になった。

`DDS3_PADCHECK_PRESS`へのネイティブフックは複数回の起動時クラッシュを起こしたため廃止した。最終的に、reWASDで右スティック左／右を未使用文字キー`[`／`]`へ変換し、SMT3HD標準の`KEYBOARD+MOUSE`設定でFIELD/DUNGEONとPUZZLEの回転操作へ割り当てる方法で実機動作を確認した。BATTLEへ同じキーを登録しないことで戦闘操作を変更せずに実現できた。F13/F14はゲーム側の設定画面で割り当て対象外だった。

したがって、右スティック回転についてはMOD方式を採用せず、reWASDとゲーム標準設定の組み合わせを推奨結果とする。ダッシュ機能のみ、reWASDからF15を受け取る別PoCとして継続調査する。

調査日: 2026-08-18  
対象: Steam版 SMT3HD 1.0.4 / Unity IL2CPP x64  
調査範囲: 静的解析およびユーザー提供のゲームパッド設定画面確認（MOD実装、設定変更、セーブ変更なし）

## 0. 今回の対象操作

ユーザー提供画像により、対象はゲームパッド設定の `FIELD/DUNGEON` セクションにある次の2項目と確定した。

```text
視点変更（左回転） = LB
視点変更（右回転） = RB
```

これを次へ変更したい、という調査である。

```text
Right Stick Left  → 視点変更（左回転）
Right Stick Right → 視点変更（右回転）
```

同じ画面にある次の項目は別操作であり、今回の目的そのものではない。

```text
視点変更（左） = Right Stick Left
視点変更（右） = Right Stick Right
```

内部列挙でも前者は `FD_Turn_Left/Right`、後者は `FD_Camera_Left/Right` と明確に分かれている。ただしユーザーの実機観察では、後者の「視点変更（上/下/左/右）」は通常のダンジョンで機能していない可能性が高い。このため、コード上は潜在競合として扱うが、最初から抑制パッチを入れてはならない。

## 1. エグゼクティブサマリー

| 項目 | 結論 | 確度 |
|---|---|---|
| Right Stick X → 視点変更（左右回転） | **実現性 HIGH** | 右スティックXと左右回転は別々の既存論理アクションとして確認済み |
| 既存の視点ロジック再利用 | **PARTIAL** | 論理アクションとカメラ更新処理は確認。比例値を受け取る公開メソッドは未確認 |
| アナログ比例回転 | **PARTIAL** | 軸値の取得は可能だが、既存の左右回転アクションはデジタル判定 |
| VanillaでLB/RBを視点左右から解放 | **UNKNOWN** | 設定データは再割当可能な構造だが、UIから完全解除できることは未実機確認 |
| Dash | **実現性 MEDIUM** | 通常移動の中心処理と走行状態は特定。安全な速度スカラーは未特定 |

最小で安全な次の一手は、ダッシュやLB/RB抑制を含めず、通常のFIELD/DUNGEON探索中だけ右スティックXを既存の `FD_Turn_Left` / `FD_Turn_Right` 相当へ変換するログ付きPoCである。初回PoCはデジタル方式を推奨する。現在右スティックへ表示上割り当てられている `FD_Camera_Left/Right` は触らず、実際に競合が観測された場合だけ対処する。比例回転は、既存カメラ内部値への直接書込み位置を追加検証してから別段階にすべきである。

## 2. 調査資料と制約

使用した主要ファイル:

- `MelonLoader/Il2CppAssemblies/Assembly-CSharp.dll`
- `MelonLoader/Dependencies/Il2CppAssemblyGenerator/Cpp2IL/cpp2il_out/Assembly-CSharp.dll`
- `GameAssembly.dll`
- `smt3hd_Data/il2cpp_data/Metadata/global-metadata.dat`

Cpp2IL 2022.1.0-pre-release.10でISILを生成し、生成ラッパーの型・シグネチャ・ネイティブRVAと照合した。`fldPlayer.fldPlayerCalc_Nml()` はCpp2ILの制御フロー復元が失敗したため、移動速度については名前だけから断定していない。

確度表記:

- **CONFIRMED**: 型情報に加え、ネイティブ命令または明確な呼出関係で確認
- **LIKELY**: 複数の構造的証拠が一致するが、実機または完全な逆コンパイルが未完
- **UNKNOWN**: 現在の静的証拠では確定不能

## 3. 既存の視点左右入力経路

### 3.1 論理アクション層 — CONFIRMED

`Il2Cpp.SIActionName` に次のフィールド専用アクションが存在する。

```text
FD_Camera_Up      = 8
FD_Camera_Down    = 9
FD_Camera_Left    = 10
FD_Camera_Right   = 11
FD_Turn_Left      = 12
FD_Turn_Right     = 13
FD_Return_Front   = 14
FD_Subjectivity   = 15
FD_Automap        = 16
```

よって、ゲームは「視点変更（上下左右）」と「視点変更（左右回転）」を別の論理操作として扱っている。設定画面との並びも一致するため、今回の対象は `FD_Turn_Left` / `FD_Turn_Right` で **CONFIRMED** と判断する。

論理入力の公開経路:

```csharp
bool SteamInputUtil.PRESS(SIActionName action);       // native RVA 0x25F9170
bool SteamInputUtil.TRIG(SIActionName action);        // native RVA 0x25F98B0
bool SteamInputUtil.REP(SIActionName action);         // native RVA 0x25F91A0
bool SteamInputAssign.IsCheck(int pad, SIActionName action, SIPressType type);
bool SteamInputAssign.padcheck(int pad, SIActionName action, SIPressType type);
```

`SIPressType.DOWN = 0`, `TRIG = 1`, `REP = 2` である。押し続け回転には `PRESS` / `DOWN` 系が対応する。

### 3.2 物理パッドマップ — CONFIRMED

`Il2Cpplibsdf_H.SDF_PADMAP`:

```text
L1 = 8
R1 = 10
L2 = 9
R2 = 11
L3 = 14
R3 = 15
```

`Il2Cpp.dds3PadManager` は次の論理マップ判定を提供する。

```csharp
bool DDS3_PADCHECK_PRESS(SDF_PADMAP map, int padNo); // native RVA 0x222BD70
bool DDS3_PADCHECK_TRIG(SDF_PADMAP map, int padNo);  // native RVA 0x222C0E0
bool DDS3_PADCHECK_REP(SDF_PADMAP map, int padNo);   // native RVA 0x222BF20
```

`fldCamera.calcCamNormal()`（native RVA `0x2020E80`）内で、L1（8）とR1（10）に対する `DDS3_PADCHECK_PRESS` 呼出しを確認した。同じ関数は方向入力、アナログ中立、カメラ補間値も処理する。

### 3.3 実際のカメラ更新 — LIKELY

フィールドカメラの中心型は `Il2Cpp.fldCamera` である。

```csharp
private static void calcCamNormal();  // RVA 0x2020E80, length 0x18C0
public static object fldCamMain();    // RVA 0x2027200, length 0x181C
```

関連する静的プロパティ:

```text
Vector2 mAxis
Vector2 mAcceleration
int     mDirection
int     mMoveLR
int     mMoveUD
float   CameraMoveLR
float   CameraMoveUD
float   CameraMoveDir
float   CameraBackDir
float   fldCamDirLockRotY
```

ネイティブコード上、`fldCamMain()` が入力を読み、`calcCamNormal()` がカメラ位置・方向と補間を更新する構造は確認できる。ただしCpp2ILのISILだけでは、L1/R1判定から最終的に書き換わる角度フィールドまでを一意にラベル付けできなかった。

したがって現在の完全な経路は次の確度である。

```text
LB/RB physical
  ↓ CONFIRMED
SDF_PADMAP_L1 / SDF_PADMAP_R1
  ↓ LIKELY（設定割当を介する場合は FD_Turn_Left / FD_Turn_Right）
fldCamera.fldCamMain()
  ↓ CONFIRMED
fldCamera.calcCamNormal()
  ↓ LIKELY
CameraMoveDir / fldCamDirLockRotY とカメラ姿勢更新
```

LB/RBが常にハードコード直結なのか、通常設定では `SIActionName` を経由してL1/R1へ解決されるのかは、設定配列の実値または実機ログで最終確認が必要である。

## 4. 右スティック入力経路

### 4.1 軸の取得 — CONFIRMED

`dds3PadManager.GetPadAnalog`:

```csharp
byte GetPadAnalog(int padno, int stick_lr, int xy, int cip_no = 0);
// native RVA 0x222C1C0
```

`fldCamera.fldCamMain()` 内の実呼出し:

```text
GetPadAnalog(0, 1, 0, 1)  // Right Stick X
GetPadAnalog(0, 1, 1, 1)  // Right Stick Y
```

返値は符号付き `-1..+1` ではなく、中心 `128` の `byte` である。ネイティブ処理は `128 ± GetAnalogAdjust()` と比較して左右・上下を判定する。`dds3ConfigGamePadSteam.GetAnalogAdjust()`（RVA `0x228EE40`）が設定デッドゾーンを返す。

右スティック方向を表す割当コードも存在する。

```text
InputAssign.AssignCode.Pad_RStickUp    = 30
Pad_RStickDown  = 31
Pad_RStickLeft  = 32
Pad_RStickRight = 33
```

Steam Input側にも `SteamPad.EAnalogActionsInGameControls.IG_RSTICK = 1` がある。

### 4.2 既存利用コードと実際の有効性 — PARTIAL

右スティックは設定画面上では `FD_Camera_Up/Down/Left/Right`（表示名「視点変更（上/下/左/右）」）へ割り当てられている。`fldCamMain()` がX/Yを取得し、`fldCamera.mMoveLR` / `mMoveUD` と方向状態を作るコードも存在する。ここまでは **CONFIRMED**。

一方、ユーザーの実機観察ではこれらの「視点変更（上/下/左/右）」は通常のダンジョンで機能していない。この観察と静的コードを合わせると、設定と入力取得は残っているが、通常カメラへの最終反映が無効・未完成・特定モード限定である可能性が高い（**LIKELY**）。

従って、新規にUnityの軸名を推測したり、物理LB/RBを偽装したりする必要はない。またPoC 1では `FD_Camera_Left/Right` を抑制せず、そのまま `FD_Turn_Left/Right` を追加する。実機で二重動作が観測された場合に限り、通常探索中だけ既存横アクションを抑制する。

## 5. 推奨する右スティック視点実装

### 推奨: Right Stick Xから既存の回転アクションを追加発火

```text
dds3PadManager.GetPadAnalog(0, 1, 0, 1)
  ↓
中心128と vanilla の GetAnalogAdjust() でデッドゾーン判定
  ↓
左: FD_Turn_Left / 右: FD_Turn_Right
  ↓
fldCamera の既存通常更新
```

最初のPoCでは比例量を直接角度へ掛けず、左右のデジタル状態へ変換するのが安全である。これによりLB/RB操作と同じ回転速度、補間、イベントカメラ制御を保持できる。既存の `FD_Camera_Left/Right` は変更せず、二重動作が実際に出た場合だけ抑制範囲を決める。

物理LB/RBの偽装は不要であり、推奨しない。論理アクション層または `fldCamera` の既存分岐を対象とする。

### 比例アナログ — PARTIAL

入力の大きさ自体は次式で正規化可能である。

```text
raw = byteValue - 128
deadzone = GetAnalogAdjust()
magnitude = clamp((abs(raw) - deadzone) / (127 - deadzone), 0, 1)
signed = sign(raw) * magnitude
```

しかし、`FD_Turn_Left/Right` は `PRESS/TRIG/REP` のデジタルアクションであり、float引数を取らない。比例回転には `CameraMoveDir`、`fldCamDirLockRotY`、または `calcCamNormal()` 内部の回転加算値へ安全に量を渡す追加解析が必要になる。現時点で公開された「回転速度(float)」メソッドは確認できていない。

また、ネイティブ処理中に `Time.deltaTime` 相当を使用していることをシンボル付きで確認できていないため、フレームレート依存性は **UNKNOWN** とする。

## 6. コンテキスト安全性

`fldCamMain()` 自体がフィールドカメラのモード、イベントカメラ、UI表示、特殊カメラを多数分岐している。最も安全な方法はグローバルな `Update()` で角度を直接変更することではなく、通常フィールド処理の既存分岐内に限定することである。

PoCの許可条件候補:

- `fldCamera.fldCamMain()` が通常カメラ経路を実行中
- `fldPlayer.fldPlayerCalc_Nml()` が選択される通常移動状態
- `dds3KernelMain.UIDispCheck(...)` が入力を阻害するUI状態ではない
- イベントカメラが存在しない（`fldCamNowEveCamera(...)` / `SetEventCamera(...)`）
- プレイヤー入力禁止カウンタが0（`fldGlobalWork_t.NoInpPlCnt`）

無効化すべき状態:

- バトル
- キャンプ/各種メニュー、名前入力
- イベント/カットシーン、スクリプトカメラ
- 主観視点、カメラ方向ロック
- ワープフィールド処理 (`fldPlayerCalc_Wm*`)
- はしご、穴、ダメージ、強制移動、`fldPlayerCalc_WarpRun()`
- パズル固有の `PZL_MapRot_*` 操作

正確な単一状態ビットは未特定である。PoCでは「通常メソッド内でのみ有効」という構造的ガードを第一選択にし、広いグローバル状態の推測は避ける。

## 7. LB/RB再割当

### 設定構造と現在値 — CONFIRMED

設定項目には次が存在する。

```text
CFG_TYPE_GAMEPAD.LEFT_ROTATION  = 15
CFG_TYPE_GAMEPAD.RIGHT_ROTATION = 16
```

`SteamInputAssign.ConfigSet` は各アクションに `pad1`, `pad2`, `key1`, `key2`, `mouse`, `type` を持つ。`dds3ConfigGamePadSteam` には `ChangeKey`、`ChangeKeyDuplicate`、`ChgConfigGamePad`、`GetConfigGamePad` がある。従ってデータ構造上、左右回転は再割当対象である。

提供画像では、現在値が次の通りであることも確認した。

```text
LEFT_ROTATION  (FD_Turn_Left)  = LB
RIGHT_ROTATION (FD_Turn_Right) = RB
CAMERA_MOVEMENT_LEFT  (FD_Camera_Left)  = Right Stick Left
CAMERA_MOVEMENT_RIGHT (FD_Camera_Right) = Right Stick Right
```

### Vanilla UIから完全に解放できるか — UNKNOWN

静的解析だけでは「未割当」をUIから選べるか、LB/RBを別アクションへ移した際に元の回転割当が自動消去されるかを確定できなかった。`ChangeKeyDuplicate()` は重複解消を行い、34個の設定項目を走査するため、別操作へLB/RBを割り当てると元設定を更新する可能性は高いが、実機設定画面で確認が必要である。

確認できた重複例外には、Cancel/Return/Viewpoint switching（設定番号8/17/28）とワープフィールド系（29..33）などがある。LB/RBに関する特別なハードコード例外はこの処理では確認できなかった。

方針:

1. Vanillaで解放できるならMODはLB/RBへ触れない。
2. 解放できない場合のみ、`FD_Turn_Left/Right` の解決直後で旧肩ボタン由来を抑制する。
3. `DDS3_PADCHECK_PRESS` 全体のL1/R1を潰すパッチは、UIやイベント操作まで壊すため避ける。

## 8. 既存移動アーキテクチャ

中心型は `Il2Cpp.fldPlayer` である。

```csharp
private static int  fldPlayerCalc_Nml();        // RVA 0x205EA20
public  static int  fldPlayerCalc();            // RVA 0x206CCD0
public  static void fldPlayerCalcForUnity();
private static void fldPlayerMotion(int, int, float); // RVA 0x206FAC0
private static void fldPlayerCalc_WarpRun();
private static void fldPlayerCalc_FastStart();
private static int  fldPlayerCalc_Nml_syukan(); // RVA 0x2064C60
```

主な状態:

```text
bool playerRun
int  fldPlayerAct
int  gKeyInputDir
int  gKeyInputCnt
int  gfldPlayerHasiCnt
int  gfldPlayerAnaCnt
float fldCamKakudoGoal
```

`fldPlayerCalc()` は状態に応じて `fldPlayerCalc_Nml()` または `fldPlayerCalc_Nml_syukan()` などを呼ぶディスパッチャである。`fldPlayerCalc_Nml()` が通常フィールド移動の中心であることは呼出関係から **CONFIRMED**。

一方、次は未確定である。

- 通常速度を保持する単一の公開 `speed` フィールド
- 左スティック量から移動距離への正確な式
- `fldPlayerMotion(..., float)` のfloat引数が移動速度かアニメーション速度か
- 位置更新がフレーム時間で正規化されるか

`fldWmFastMove`、`fldPlayerFastSet()`、`fldPlayerCalc_FastStart()` はワールドマップ/スクリプト高速移動用とみられ、通常ダッシュへの流用は危険である。

## 9. 推奨するダッシュ実装

### 推奨: 通常移動計算内の移動量スカラーを一時倍率化

```text
通常フィールド + 手動移動 + Dash held
  ↓
fldPlayerCalc_Nml() 内で算出済みの移動量だけを倍率化
  ↓
既存の当たり判定、床判定、イベント判定、姿勢更新へ渡す
```

Transform/座標の事後加算は、壁抜け、イベント領域の飛び越し、床/坂判定の不整合を招くため避ける。

現段階で `1.5x / 1.75x / 2.0x` のいずれかを安全と断定できる証拠はない。最初の速度PoCは1.25x程度から始め、フレーム単位の衝突・イベント判定をログ確認して段階的に上げるべきである。これは最終値の推奨ではない。

### ダッシュの安全ガード

最低限、次の場合は無効化する。

- `fldPlayerCalc_Nml()` 以外の移動状態
- `playerRun == false` かつ走行アニメーションに入れない状態
- `NoInpPlCnt != 0`
- はしご (`gfldPlayerHasiCnt`)、穴 (`gfldPlayerAnaCnt`)、ダメージ
- ワープ、強制/自動移動、イベント
- 主観視点、特殊パズル、ワープフィールド

アニメーション速度を別に補正すべきかは **UNKNOWN**。移動量のみを増やすと足滑りが起きる可能性がある。倍率を衝突計算前に入れれば既存判定を再利用できるが、高倍率では薄いトリガーを1フレームで越える可能性が残る。

## 10. LBとRBのダッシュ適性

通常のパッドマップではL1/R1はいずれも独立した入力であり、設定重複処理に片方だけの特別扱いは確認できなかった。従ってフィールド通常状態に限れば両者は同程度に利用可能と **LIKELY** 判断する。

ただし `calcCamNormal()` がL1/R1の物理マップを直接参照する箇所があるため、Vanilla再割当後もカメラ側で肩ボタンに反応が残らないかは実機確認が必要である。片方だけを先にダッシュへ割り当て、視点・UI・イベントで副作用を確認するのが安全である。

## 11. 最小の安全なPoC

### PoC 1（別承認後）

```text
対象: 通常のFIELD/DUNGEON探索のみ
入力: GetPadAnalog(0, 1, 0, 1)
デッドゾーン: vanilla GetAnalogAdjust()
出力: FD_Turn_Left / FD_Turn_Right 相当のデジタル状態
既存横入力: 変更しない（実機で二重動作が出た場合のみ後続PoCで対処）
LB/RB変更: なし
Dash: なし
永続設定変更: なし
ログ: 状態遷移時のみ
```

検証項目:

- 左右方向とゲーム内表示の一致
- LB/RBと同じ回転速度・補間・復帰挙動
- メニュー、バトル、イベント、主観、ワープで無反応
- 右スティックの既存用途との競合
- デッドゾーン内でドリフトしない

### PoC 2（PoC 1合格後、さらに別承認）

```text
対象: fldPlayerCalc_Nml() の通常手動移動のみ
入力: Vanillaで解放できた肩ボタンのhold
処理: 衝突判定前の移動量へ小倍率
座標直接操作: なし
```

## 12. 未解決事項と追加確認

1. `FD_Turn_Left/Right` とL1/R1のデフォルト設定配列実値。
2. `calcCamNormal()` 内の最終角度フィールドと1フレーム加算量。
3. Vanilla設定画面で左右回転を未割当または別入力へ移せるか。
4. `fldPlayerCalc_Nml()` の移動量算出式（Cpp2IL復元失敗箇所）。
5. 通常移動とアニメーション速度の結合点。
6. 実機でのイベントカメラ、パズル、ワープフィールド時の入力抑制。

これらは最終MOD実装前に解決すべきだが、PoC 1の「右スティックを既存デジタル回転へ接続する」実現性を否定するものではない。
