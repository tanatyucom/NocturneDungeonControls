# Nocturne Modern Controller

『真・女神転生III NOCTURNE HD REMASTER』（Steam版）のダンジョン操作を現代的にするMelonLoader MODです。

## 機能

- 右スティック左右：ダンジョンの標準左右旋回
- 右スティック上下：ゲーム内蔵のアナログ上下カメラ
- ダンジョンのLB／RB旋回を抑止
- 戦闘中のRB（Pass）など、戦闘操作は変更しない
- LTまたはRT長押し：ダンジョン／ワールドマップで1.6倍ダッシュ
- LT＋RT同時押し：ダッシュ固定のON／OFF
- `P`長押し：キーボード用ダッシュ
- 探索中のRB：所持スキルとMPを使った全員回復シーケンス

右スティック上下は独自にカメラ座標を動かすものではありません。ゲーム内部の右スティック縦アナログ経路へ入力を渡すため、標準の球面移動、補間、上限・下限が利用されます。

## 対応コントローラー

同梱のSDL3入力Helperは、VID/PIDを特定機種へ固定していません。SDL3がゲームパッドとして認識する次のような機器を対象とします。

- Xbox系コントローラー
- DualShock／DualSense
- Nintendo Switch Proコントローラー
- 一般的なUSB／Bluetoothゲームパッド

複数の物理・仮想パッドが存在する場合は、右スティック入力を返している機器を優先します。

## 必要環境

- Steam版 SMT3HD
- MelonLoader
- Windows x64
- .NET 6 Desktop Runtime（MelonLoader環境に通常含まれます）

reWASDは必須ではありません。

## インストール

配布ZIPをゲームフォルダーへ展開し、次の構成にします。

```text
smt3hd/
  Mods/
    NocturneModernController.dll
    NocturneModernController.Helper/
      NocturneModernController.InputHelper.exe
      NocturneModernController.InputHelper.dll
      NocturneModernController.InputHelper.deps.json
      NocturneModernController.InputHelper.runtimeconfig.json
      SDL3.dll
```

## 確認済み動作

- ダンジョンで右スティック左右が旋回する
- ダンジョンで右スティック上下が標準アナログカメラとして動作する
- ダンジョンでLB／RBは旋回しない
- 戦闘でRBのPassが通常どおり使える
- 病院内およびワールドマップでダッシュが動作する
- Xbox Elite Series 2の有線・無線接続

パズルの右スティック回転ルートは実装されていますが、実プレイでの最終確認は未完了です。

## Steam録画を契機にした入力経路の発見

調査中、Steam録画開始後だけゲーム内部の右スティック縦軸が有効になる現象を確認しました。

- 録画前：`GetPadAnalog(0,1,1,1) = 128`（常に中央）
- 録画後・上：`253～255`
- 録画後・下：`0～42`

この結果から、SMT3HDには標準のアナログ上下カメラが存在するものの、通常状態では右スティック値がその経路へ届いていないと判明しました。本MODはSDL3で取得した入力をこの標準経路へ直接渡し、録画開始を必要とせず同じ挙動を再現します。

詳細は[調査記録](docs/research/RIGHT_STICK_VIEW_AND_DASH_INVESTIGATION.md)を参照してください。

## ビルド

MelonLoaderとSMT3HDの生成済みIl2Cppアセンブリが必要です。

```powershell
dotnet build .\NocturneModernController.csproj -c Release
dotnet build .\helper\NocturneModernController.InputHelper.csproj -c Release
```

## 注意

- ゲーム本体、Atlus/Segaのアセット、生成されたゲームDLLは含みません。
- MODの利用は自己責任で行ってください。
- 本プロジェクトはAtlus、Sega、Valve、SDLプロジェクトとは無関係です。
