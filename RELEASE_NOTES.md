# CannonSoundPoolFix v1.2.0

首个公开版本。

## 功能

- 限制同一 `1 × 1 × 1` 区域内同时保留的炮声为最新 5 个。
- 主动停止最老炮口 Effect 的近程和远程 `AudioSource`，防止机炮占满全局音频 voice pool。
- 保留所有 SFX/VFX GameObject、灯光和正常炮口视觉效果。
- 将无限的炮口 Effect 原型池限制为 192 个实例，防止长期实例堆积。
- 事件驱动，无车辆或场景轮询。

## 已验证环境

- Sprocket `0.2.53.1`
- MelonLoader `0.7.2 Open-Beta`
- Windows 11 x64

## 安装

将 `CannonSoundPoolFix.dll` 放入《Sprocket》的 `Mods` 目录。

## 验证

- 用户已确认持续自动炮射击期间，其他声音可以正常播放。
- 最新炮声与炮口 VFX 保持正常。
- `Latest.log` 已确认双 AudioSource 限制分支实际触发。
