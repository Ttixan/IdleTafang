# 技术方案与框架设计（Unity + C#）

## 1. 目标

- 支撑当前设计中的双阶段核心循环：工作阶段（打字产能）与休息阶段（塔防战斗）。
- 在 1–2 个月内完成可交付的 MVP：可玩、可存档、可扩展、可验证。
- 采用数据驱动方式降低硬编码比例，方便后续做数值调整与内容扩展。

## 2. 技术选型

- Unity：`2022.3 LTS`
- 语言：`C#`
- 渲染：`URP`
- 主要平台：`PC / Windows`
- UI：`UGUI`
- 配置：`ScriptableObject + CSV`
- 存档：`JSON`

### 2.1 选型原则

- MVP 阶段优先稳定与开发效率，不追求过度工程化。
- 首版优先做 Unity 原生方案，减少额外依赖。
- 仅在明确提升效率或可维护性时引入第三方库。

## 3. 总体架构

### 3.1 分层结构

- `Presentation`：UI、动画、特效、音频反馈。
- `Application`：流程调度、状态切换、用例编排。
- `Domain`：核心规则、数值模型、战斗与经济逻辑。
- `Infrastructure`：存档、配置加载、输入封装、对象池等。

### 3.2 架构原则

- 业务逻辑尽量放在纯 C# 中，减少对 `MonoBehaviour` 的直接依赖。
- 场景对象只负责表现、挂载和桥接。
- 核心规则可测试、可替换、可复用。

## 4. 游戏流程设计

### 4.1 顶层流程

- `Boot`
- `MainMenu`
- `InRun`
- `Settlement`

### 4.2 运行阶段子状态

- `WorkPhase`：打字产能获取。
- `PrepPhase`：布置与升级塔防。
- `BattlePhase`：刷怪与防守结算。
- `BattleResult`：本波结果与奖励发放。

### 4.3 状态设计目标

- 阶段边界清晰，方便后续插入新玩法。
- 每个阶段职责单一，减少耦合。
- 阶段切换由统一入口管理，避免散落在各系统中。

## 5. 目录结构

```text
Assets/
  Scripts/
    Core/
      StateMachine/
      EventBus/
      Time/
      Pool/
      Save/
    Gameplay/
      Run/
      Economy/
      Typing/
      Combat/
      Towers/
      Enemies/
      Waves/
      Progression/
    UI/
      Screens/
      HUD/
      Widgets/
    Config/
      ScriptableObjects/
    Tests/
      EditMode/
      PlayMode/
```

### 5.1 Assembly Definition

- `Game.Core`
- `Game.Gameplay`
- `Game.UI`
- `Game.Config`
- `Game.Tests`

### 5.2 目的

- 缩短编译范围。
- 控制模块依赖方向。
- 让测试代码和运行时代码分离。

## 6. 核心系统设计

### 6.1 GameLoop

职责：

- 管理运行阶段切换。
- 广播阶段开始/结束事件。
- 协调计时器、战斗、结算和 UI。

接口示例：

```csharp
public enum RunPhase
{
    Work,
    Prep,
    Battle,
    Result
}

public interface IPhaseController
{
    RunPhase Current { get; }
    void Enter(RunPhase phase);
    void Tick(float deltaTime);
}
```

### 6.2 TypingSystem

职责：

- 接收有效输入。
- 计算准确率、连击、节奏分数。
- 将打字结果转换为资源产出。

建议实现：

- 输入侧通过 `ITypingInputProvider` 封装 Unity Input System。
- 计分逻辑写在纯 C# 服务中。
- UI 只订阅结果，不直接参与规则计算。

数据示例：

```csharp
public struct TypingSnapshot
{
    public int ValidChars;
    public int TotalInputs;
    public float Accuracy;
    public int ComboLevel;
    public float RhythmScore;
}
```

### 6.3 EconomySystem

职责：

- 管理能量、金币、材料、科技点等资源。
- 处理获得、消耗、保底与奖励。
- 提供统一的资源读写接口。

接口示例：

```csharp
public enum ResourceType { Energy, Gold, Material, Tech }

public interface IEconomyService
{
    int Get(ResourceType type);
    bool TrySpend(ResourceType type, int amount, string reason);
    void Add(ResourceType type, int amount, string reason);
}
```

### 6.4 CombatSystem

职责：

- 管理敌人生成、路径移动、受击和死亡。
- 管理塔的目标选择、攻击、冷却和效果。
- 输出战斗结果与奖励建议。

MVP 约束：

- 先使用固定路径。
- 先实现少量敌人类型与基础塔型。
- 先完成结算闭环，再扩展复杂行为。

### 6.5 WaveSystem

职责：

- 读取波次配置。
- 控制刷怪节奏。
- 提供每波结算结果。

建议：

- 波次配置独立于战斗实现。
- 波次数据通过配置表调整，不写死在代码里。

### 6.6 SaveSystem

职责：

- 保存局外成长、解锁状态与设置项。
- 支持自动存档与手动存档。
- 处理版本字段，便于后续迁移。

接口示例：

```csharp
public interface ISaveService
{
    void Save(PlayerProfile profile);
    PlayerProfile LoadOrCreate();
}
```

## 7. 数据与配置

### 7.1 配置类型

- `TowerConfig`
- `EnemyConfig`
- `WaveConfig`
- `ProgressionConfig`

### 7.2 管理方式

- 策划侧使用 CSV 维护主表。
- 导入工具生成或更新 ScriptableObject。
- 运行时只读配置，不直接改动原始表。

### 7.3 配置要求

- 所有关键配置必须有唯一 ID。
- 所有数值字段必须支持校验。
- 导入时输出错误日志，避免脏数据进入游戏。

## 8. 场景与预制体规划

### 8.1 场景

- `Boot.unity`
- `MainMenu.unity`
- `Run.unity`

### 8.2 Run 场景结构

- `SystemsRoot`
  - `GameLoopController`
  - `EconomyController`
  - `CombatController`
  - `WaveController`
- `MapRoot`
  - `Path`
  - `BuildSlots`
- `UIRoot`
  - `HUD`
  - `PhasePanel`
  - `SettlementPanel`

### 8.3 预制体

- `Towers/`
- `Enemies/`
- `Projectiles/`
- `VFX/`
- `UI/`

## 9. 存档方案

- 使用本地 JSON 存储，路径为 `Application.persistentDataPath`。
- 存档内容包括：局外成长、解锁状态、设置、历史记录。
- 存档结构必须保留版本号，便于后续升级。

## 10. 性能与工程规范

### 10.1 MVP 性能目标

- 目标帧率：`60 FPS`
- 首版同屏敌人数：`50–100`

### 10.2 工程规范

- 避免在高频 `Update` 中频繁查找组件。
- 复用敌人、子弹、伤害飘字等对象。
- 关键逻辑避免高频 LINQ 和字符串拼接。
- 开发版保留详细日志，发布版关闭冗余日志。

### 10.3 优化优先级

- 先保证逻辑正确。
- 再优化对象分配。
- 最后再做渲染与特效层优化。

## 11. 测试策略

### 11.1 EditMode 测试

- 打字计分公式正确性。
- 资源增减与保底规则。
- 配置读取与字段校验。

### 11.2 PlayMode 测试

- 阶段切换是否按时触发。
- 塔是否能正常攻击与结算。
- 战斗胜负与奖励是否正确。

## 12. 第一个可玩版本拆解

### 第 1 周

- 状态机骨架。
- Run 场景。
- 计时器与输入接入。

### 第 2 周

- TypingSystem 最小实现。
- 资源变化与 UI 显示。
- 基础结算流程。

### 第 3 周

- 1 条路径、1 种塔、1 种敌人。
- 波次系统。
- 战斗胜负闭环。

### 第 4 周

- 局外成长。
- 本地存档。
- 首轮平衡调整。

## 13. 待确认事项

- 首版是否保留“工作 / 休息”作为唯一主循环。
- 首版输入是否只支持游戏内输入框。
- 休息阶段时长最终采用 3 分钟还是 5 分钟。
- 局外成长更偏数值强化还是内容解锁。

## 14. 当前结论

- 先做闭环，再做扩展。
- 先做配置化，再做复杂表现。
- 先做可测试规则，再接入场景与 UI。
