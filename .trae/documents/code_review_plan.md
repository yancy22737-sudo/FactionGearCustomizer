# 代码审查计划：FactionGearModification Mod

## 审查概述

本次审查将深入分析 FactionGearModification 环世界 Mod 的源代码，从**代码架构、数据结构、安全性、性能、逻辑、工作流**六个维度进行全面评估。

***

## 一、代码架构审查

### 1.1 项目结构分析

* **Core**: `FactionGearCustomizerSettings.cs`, `FactionGearCustomizerMod.cs`

* **Data**: `FactionGearData.cs`, `KindGearData.cs`, `GearItem.cs`, `FactionGearPreset.cs`, `ForcedHediff.cs`, `SpecRequirementEdit.cs`, `Enums.cs`

* **Managers**: `FactionGearManager.cs`, `GearApplier.cs`

* **Patches**: `Patch_GeneratePawn.cs`

* **UI**: `FactionGearEditor.cs`, `FactionGearPreviewWindow.cs`, `PresetManagerWindow.cs`, `TexCache.cs`, `Window_ColorPicker.cs`

* **IO**: `PresetIOManager.cs`

### 1.2 审查要点

* [ ] 模块职责划分是否清晰

* [ ] 依赖关系是否合理（Core/Managers → Data → UI）

* [ ] 是否遵循 SOLID 原则

* [ ] 命名空间组织是否规范

***

## 二、数据结构审查

### 2.1 核心数据结构

#### FactionGearData

* `factionDefName`: 派系列表键

* `kindGearData`: 兵种装备数据列表

* `kindGearDataDict`: 字典索引（性能优化）

* **问题**: 字典索引与列表双重维护，存在同步风险

#### KindGearData

* 5个装备列表: `weapons`, `meleeWeapons`, `armors`, `apparel`, `others`

* 高级配置: `ForceNaked`, `ForceOnlySelected`, `ItemQuality`, `ForcedWeaponQuality`, `BiocodeWeaponChance`, `TechHediffChance` 等

* **评估**: 数据结构设计合理，支持简单模式和高级模式

#### GearItem

* `thingDefName`: 物品定义名称

* `weight`: 权重（影响生成概率）

* `cachedThingDef`: 缓存的 ThingDef 引用

* **问题**: 缓存未实现失效机制，加载/保存后可能失效

### 2.2 审查要点

* [ ] 数据序列化/反序列化安全性

* [ ] 深拷贝实现的完整性

* [ ] 空值处理和边界检查

* [ ] 版本迁移逻辑（v1 → v2）

***

## 三、安全性审查

### 3.1 潜在安全风险

#### 3.1.1 反射使用

* `FactionGearEditor.cs` 使用反射打开日志窗口

* 反射调用 `Verse.EditWindow_Log`

#### 3.1.2 文件操作

* `PresetIOManager.cs` 使用临时文件进行序列化

* `GenFilePaths.ConfigFolderPath` 路径安全性

#### 3.1.3 空值风险

* `GearItem.ThingDef` 属性访问可能返回 null

* `GearApplier.ApplyCustomGear` 中多处 null 检查

### 3.2 审查要点

* [ ] 所有 null 检查是否充分

* [ ] 反射调用是否有版本兼容性问题

* [ ] 文件路径是否正确处理特殊字符

* [ ] 敏感数据是否被正确保护

***

## 四、性能审查

### 4.1 已识别的性能优化

#### 4.1.1 缓存机制

* `FactionGearManager`: 静态缓存 `cachedAllWeapons`, `cachedAllMeleeWeapons` 等

* `GearItem`: ThingDef 缓存

* `FactionGearEditor`: 图标缓存 `iconCache`, 列表缓存

#### 4.1.2 懒加载

* UI 绘制使用 TipSignal 延迟计算 Tooltip

* 滚动视图视锥体剔除

### 4.2 潜在性能问题

#### 4.2.1 UI 绘制

* 每次绘制都重新计算边界值

* 列表排序每帧重新执行

* 大量 Text.CalcHeight 调用

#### 4.2.2 数据处理

* `FactionGearData.GetKindData` 使用 FirstOrDefault 而非字典查询

* `KindGearData.DeepCopy` 存在重复代码

### 4.3 审查要点

* [ ] 缓存策略是否合理

* [ ] 是否存在不必要的重复计算

* [ ] LINQ 查询性能

* [ ] UI 绘制性能瓶颈

***

## 五、逻辑审查

### 5.1 核心业务逻辑

#### 5.1.1 装备应用流程 (GearApplier)

1. `ApplyCustomGear` → `ApplyWeapons` / `ApplyApparel` / `ApplyHediffs`
2. 高级模式: SpecificWeapons / SpecificApparel
3. 简单模式: 旧列表系统（回退方案）

#### 5.1.2 数据加载 (FactionGearManager)

1. `LoadDefaultPresets` → `LoadKindDefGear`
2. 从 `PawnKindDef.weaponTags` 和 `apparelTags` 读取原版配置
3. 缓存优化避免重复 DefDatabase 遍历

#### 5.1.3 预设系统

1. 保存: `SaveFromCurrentSettings` → 只保存修改过的数据
2. 导出: `ExportToBase64` → XML 序列化 → Base64 编码
3. 导入: Base64 解码 → XML 反序列化

### 5.2 审查要点

* [ ] 装备生成逻辑是否完整

* [ ] 随机权重算法正确性

* [ ] 预设覆盖/合并逻辑

* [ ] 撤销/恢复功能完整性

***

## 六、工作流审查

### 6.1 用户交互流程

#### 6.1.1 主界面布局

```
┌──────────┬──────────────────────┬─────────────────────┐
│ Factions │    Selected Gear      │   Item Library      │
│  22%     │        40%            │        38%          │
├──────────┼──────────────────────┼─────────────────────┤
│ Faction  │ [Simple/Advanced]    │ [Add All]           │
│ List     │ [Tab: Weapons/Melee] │ [Filter: Mod/Level] │
│          │ [Gear Items]         │ [Filter: Range/Dmg] │
│ Kind     │ [Stats Preview]      │ [Item List]         │
│ List     │                      │                     │
└──────────┴──────────────────────┴─────────────────────┘
```

#### 6.1.2 关键操作

* 选择派系 → 选择兵种 → 编辑装备 → 保存

* 预设管理: 创建/删除/导入/导出

* 预览功能（仅游戏中可用）

### 6.2 审查要点

* [ ] UI 状态管理是否正确

* [ ] 撤销/重做机制

* [ ] 数据持久化流程

* [ ] 错误处理和用户反馈

***

## 七、具体问题清单

### 7.1 高优先级问题

| # | 类别 | 问题描述                                | 位置                           |
| - | -- | ----------------------------------- | ---------------------------- |
| 1 | 数据 | `GearItem.cachedThingDef` 加载后可能失效   | `GearItem.cs:34-37`          |
| 2 | 逻辑 | `FactionGearData.GetKindData` 未使用字典 | `FactionGearData.cs:102-105` |
| 3 | 安全 | 反射调用 `EditWindow_Log` 可能不存在         | `FactionGearEditor.cs:213`   |
| 4 | 性能 | `DrawSimpleMode` 每帧创建新列表            | `FactionGearEditor.cs:861`   |
| 5 | 逻辑 | `DeepCopy` 与 `CopyFrom` 代码重复        | `KindGearData.cs`            |

### 7.2 中优先级问题

| #  | 类别 | 问题描述                          | 位置                             |
| -- | -- | ----------------------------- | ------------------------------ |
| 6  | 架构 | UI 代码与业务逻辑混合                  | `FactionGearEditor.cs`         |
| 7  | 性能 | 边界值每帧检查 `needCalculateBounds` | `FactionGearEditor.cs:163-167` |
| 8  | 数据 | 预设保存未保存高级配置字段                 | `FactionGearPreset.cs:96-104`  |
| 9  | 安全 | 临时文件未及时清理                     | `PresetIOManager.cs`           |
| 10 | 逻辑 | ForceIgnore 开关全局生效，缺少UI提示     | `GearApplier.cs:69`            |

### 7.3 低优先级问题

| #  | 类别 | 问题描述              | 位置                     |
| -- | -- | ----------------- | ---------------------- |
| 11 | 代码 | 硬编码 Magic Numbers | 多个文件                   |
| 12 | 代码 | 部分方法过长            | `FactionGearEditor.cs` |
| 13 | 文档 | 缺少 XML 注释         | 多个文件                   |
| 14 | 测试 | 无单元测试             | 项目级别                   |

***

## 八、审查方法

### 8.1 静态分析

* [ ] 代码语法检查

* [ ] 依赖关系分析

* [ ] 代码复杂度评估

### 8.2 动态分析

* [ ] 构建测试 (`dotnet build`)

* [ ] IL 指令分析（可选）

* [ ] 内存使用分析（游戏中）

### 8.3 对比分析

* [ ] 与 TotalControl 原版代码对比

* [ ] 与环世界 API 文档对比

***

## 九、输出产物

审查完成后，将生成以下报告：

1. **详细问题报告**: 包含每个问题的具体位置、原因、影响、建议修复方案
2. **代码质量评分**: 从可维护性、性能、安全性等维度评分
3. **重构建议**: 针对架构问题的改进方案
4. **测试建议**: 建议添加的测试用例

***

## 十、审查执行计划

### 第一阶段：静态分析 (30%)

1. 读取并分析所有源代码文件
2. 运行构建检查编译错误
3. 识别明显的问题模式

### 第二阶段：深度分析 (40%)

1. 追踪数据流和调用链
2. 分析性能热点
3. 验证业务逻辑正确性

### 第三阶段：报告生成 (30%)

1. 整理问题清单
2. 编写修复建议
3. 生成最终报告

