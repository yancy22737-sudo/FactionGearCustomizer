# FactionGearEditor 重构方案：模块化与解耦

## 1. 现状分析
目前的 `FactionGearEditor.cs` 是一个典型的"上帝类" (God Class)，存在以下问题：
- **体积过大**：单文件超过 150KB，包含大量混合逻辑。
- **职责不清**：混合了 UI 绘制、业务逻辑、数据存取、状态管理等。
- **高耦合**：各个部分紧密缠绕，难以独立修改或测试。
- **静态状态**：大量使用静态字段存储 UI 状态，不利于扩展和维护。

## 2. 重构目标
- **模块化**：将 UI 拆分为独立的组件（Panels）。
- **低耦合**：分离 UI 表现层与业务逻辑层。
- **可维护性**：降低单文件复杂度，提高代码可读性。

## 3. 架构设计

### 3.1 状态管理 (State Management)
引入 `EditorSession` 类来管理编辑器的运行时状态，替代 `FactionGearEditor` 中的静态字段。
- **职责**：管理当前选中的派系、兵种、滚动条位置、搜索文本、过滤器设置等。
- **优势**：统一管理状态，方便在不同组件间共享。

### 3.2 UI 组件拆分 (UI Components)
将 `FactionGearEditor` 的三大面板拆分为独立的类，每个类只负责自己区域的绘制和交互。

1.  **FactionListPanel** (左侧面板)
    -   负责显示派系列表。
    -   处理派系过滤、排序和选择。
    -   依赖 `EditorSession` 获取/更新选中状态。

2.  **KindListPanel** (中间面板)
    -   负责显示选中派系的兵种列表。
    -   处理兵种搜索、过滤和选择。
    -   依赖 `EditorSession` 获取当前派系和更新选中兵种。

3.  **GearEditPanel** (右侧面板)
    -   负责显示和编辑选中兵种的装备设置。
    -   包含武器、服装、特定装备等子标签页。
    -   处理具体的增删改操作。

### 3.3 业务逻辑层 (Service Layer)
利用现有的 `FactionGearManager` 或创建新的 `FactionGearService` 来处理纯数据逻辑。
-   数据获取（获取所有武器、服装列表）。
-   数据计算（DPS 计算、属性获取）。
-   数据持久化（保存/加载设置）。

## 4. 实施步骤

### 第一阶段：基础设施准备
1.  创建 `UI/State` 目录，新建 `EditorSession.cs`。
2.  将 `FactionGearEditor` 中的状态字段（如 `selectedFactionDefName`, `scrollPos` 等）迁移到 `EditorSession`。

### 第二阶段：UI 组件提取
1.  创建 `UI/Panels` 目录。
2.  提取 `FactionListPanel`：将 `DrawLeftPanel` 及相关逻辑移动到新类。
3.  提取 `KindListPanel`：将 `DrawMiddlePanel` 及相关逻辑移动到新类。
4.  提取 `GearEditPanel`：将 `DrawRightPanel` 及相关逻辑移动到新类。

### 第三阶段：主类重构
1.  重构 `FactionGearEditor`，使其作为协调者 (Coordinator)。
2.  在 `DrawEditor` 中实例化或调用上述 Panel 类。
3.  确保所有交互通过 `EditorSession` 或回调函数正确传递。

### 第四阶段：清理与验证
1.  删除 `FactionGearEditor` 中废弃的静态字段和方法。
2.  验证所有功能（选择、过滤、保存、重置）是否正常工作。

## 5. 目录结构预览
```
FactionGearModification/
  UI/
    FactionGearEditor.cs (入口与协调)
    State/
      EditorSession.cs (状态管理)
    Panels/
      FactionListPanel.cs (左侧)
      KindListPanel.cs (中间)
      GearEditPanel.cs (右侧)
      FilterPanel.cs (过滤器组件，可选)
```
