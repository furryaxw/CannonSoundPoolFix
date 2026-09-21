# CannonSoundPoolFix

适用于《Sprocket》的独立 MelonLoader 模组。它会主动结束过量的旧机炮声，避免高射速武器持续占满全局音频 voice pool，同时限制炮口特效根实例无限增长。

## 主要功能

- 监听 `MuzzleFlashEffect.Setup`，不轮询车辆、场景或全局 `AudioSource`。
- 在同一个 `1 × 1 × 1` 世界空间区域内保留最新 5 个炮声。
- 第 6 个附近炮声出现时，直接停止最老记录的近程与远程 `AudioSource`，立即释放音频 voice。
- 不禁用 `SFX`、`Long range SFX`、`VFX`、`Light` 或炮口特效根节点。
- 将无限的炮口 Effect 原型池限制为 192 个实例；若游戏已有更小的正数上限，则保留游戏设置。
- 不修改任何游戏资源文件。

## 要求

- 《Sprocket》`0.2.53.1`（当前实测版本）
- MelonLoader `0.7.2 Open-Beta`，IL2CPP / net6
- Windows x64

游戏或 MelonLoader 更新后，生成的 IL2CPP 类型与运行时行为可能变化，需重新验证。

## 安装

1. 为《Sprocket》安装 MelonLoader。
2. 下载 `CannonSoundPoolFix.dll`。
3. 将 DLL 放入游戏目录下的 `Mods` 文件夹。
4. 启动游戏；MelonLoader 控制台应显示 `Cannon Sound Pool Fix v1.2.1`。

持续射击触发限制时，每个场景最多记录一次：

```text
Cannon voice limit engaged: stopped the oldest playing cannon AudioSource pair.
```

## 与 CannonSmokeSuppressor 配合

本模组只管理炮声音源与炮口 Effect 生命周期。`CannonSmokeSuppressor` 可同时安装，继续负责抑制长期堆积的烟雾输出。

## 构建

默认假设游戏安装在 `G:\Sprocket`：

```powershell
dotnet build .\CannonSoundPoolFix\CannonSoundPoolFix.csproj -c Release -p:SkipModDeploy=true
```

如果游戏位于其他路径：

```powershell
dotnet build .\CannonSoundPoolFix\CannonSoundPoolFix.csproj -c Release `
  -p:SprocketRoot="D:\Games\Sprocket" `
  -p:SkipModDeploy=true
```

省略 `SkipModDeploy` 会把构建出的 DLL 复制到 `$(SprocketRoot)\Mods`。游戏运行时请勿覆盖已加载的 DLL。

## 许可证

[GPL-3.0-only](LICENSE.txt)
