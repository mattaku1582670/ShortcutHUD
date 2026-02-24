# ShortcutHUD (WPF / .NET 10)

ショートカット可視化用の常時最前面HUDアプリです。  
オフライン・閉域環境で動作し、`shortcuts.json` をローカル読み込みします。

## 主な機能
- 細いヘッダ帯HUD（常時最前面）
- ホバーでPopup展開（200ms遅延で自動クローズ）
- ピン留めON/OFF（設定保存）
- 右クリックContextMenu
  - ピン切り替え
  - 透明度スライダー（20%〜100%）
  - ショートカット再読込
  - 終了
- JSONショートカット表示（カテゴリ + 項目）
- 項目クリックで`keys`をクリップボードコピー
- 検索フィルタ（name/keys/note 部分一致）
- 設定保存先: `%LOCALAPPDATA%\ShortcutHUD\settings.json`

## ビルド・実行
1. Visual Studio 2022 で `ShortcutHUD.csproj` を開く
2. 構成 `Debug` または `Release` でビルド
3. 初回実行前に、出力フォルダ（`bin\...\net10.0-windows\`）へ `shortcuts.json` を配置
4. 実行

## 単体EXE配布（例）
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

生成物例:
- `bin\Release\net10.0-windows\win-x64\publish\ShortcutHUD.exe`
- 同じフォルダに `shortcuts.json` を配置して運用
