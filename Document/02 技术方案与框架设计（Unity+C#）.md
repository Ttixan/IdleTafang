# 技术方案与框架设计（Unity + C#）

## 1. 目标

本阶段目标是做一个**极简可玩 Demo**。

核心闭环只保留：
- 打字产能
- 能量建造
- 敌人从四周刷出并向中心移动
- 基础 UI 显示

Unity 只负责：
- 渲染
- 输入
- UI
- 场景承载

核心规则尽量保持纯 C#。

## 2. 技术选择

- Unity：2022.3 LTS
- 语言：C#
- 渲染：3D 俯视角
- UI：UGUI + TextMeshPro
- 配置：当前 Demo 先以代码常量为主
- 存档：当前阶段不实现

## 3. 架构原则

- 逻辑优先，表现其次。
- Unity 只做展示和桥接。
- 当前 Demo 优先少代码、少绑定、少对象。
- 不为未来功能提前做过重设计。

## 4. 当前分层

### 4.1 Core
职责：
- 程序入口
- 场景切换
- 输入接入
- 简单时序

当前脚本：
- `GameBootstrap`
- `GameLoop`
- `GameInput`
- `GameTimer`
- `GameStateId`
- `IGameState`

### 4.2 Gameplay
职责：
- 打字产能
- 资源变化
- 建造升级
- 刷怪
- 敌人移动

当前脚本：
- `TypingSession`
- `TypingStats`
- `ResourceWallet`
- `BuildPrototype`
- `BuildService`
- `CombatEnemy`
- `CombatWaveManager`
- `TopDownCameraRig`

### 4.3 UI
职责：
- 显示状态
- 响应按钮

当前脚本：
- `RunHudController`
- `SceneMenuView`

## 5. 当前数据流

```text
输入 -> TypingSession -> ResourceWallet -> BuildService
刷怪 -> CombatWaveManager -> CombatEnemy
状态 -> UI
```

## 6. 场景结构

- `Boot.unity`
- `MainMenu.unity`
- `Run.unity`

`Run` 场景尽量保持少对象，只保留最少可运行内容。

## 7. Demo 范围

### 7.1 打字

- 任意有效字符计为一次输入。
- 不做 prompt。
- 不做词库。
- 输入直接转能量。

### 7.2 建造

- 一个按钮。
- 消耗固定能量。
- 塔等级 +1。

### 7.3 战斗

- 定时刷一个敌人。
- 敌人从圆周刷出。
- 直线朝中心移动。
- 到中心后销毁。

### 7.4 相机

- 固定俯视角。
- 尽量不依赖复杂场景绑定。

## 8. 后续扩展方向

Demo 跑通后，再逐步补：
- 波次配置
- 更多敌人类型
- 更多塔类型
- 结算
- 存档

## 9. 结论

当前阶段只追求：
- 用最少代码做出可玩的 Demo。
- Unity 侧依赖最少化。
- 先验证核心体验，再考虑扩展性。
