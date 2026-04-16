# Multi-PCK Integration Design

## 1. 文档目的

本文档用于定义本项目下一阶段的核心目标：

- 不再只处理单个 `.pck`
- 而是支持导入多个 `.pck`
- 从多个来源中提取可替换卡图
- 处理重复、冲突和覆盖关系
- 最终生成一个“整合后的 portrait replacement mod”

这意味着项目的定位会从：

- `单包导入 -> 单包映射 -> 单个 mod`

升级为：

- `多包导入 -> 统一索引映射 -> 冲突决策 -> 整合 mod`

这份文档聚焦于：

- 多 `.pck` 的会话模型
- 冲突合并模型
- GUI 的交互设计
- 最终构建链路
- 分阶段实施计划

## 2. 问题重定义

### 2.1 当前单包方案解决了什么

当前项目已经具备这些能力：

- 导入单个 `.pck`
- 通过 GDRE recover 解包
- 扫描图片资源
- 基于官方卡牌索引做确定性匹配
- 在 GUI 中人工审核映射
- 生成 mod 源码树
- 从源码树 build 出最终 mod 产物

### 2.2 当前单包方案的局限

单包流程假设：

- 一个 `cardId` 最终只来自一个来源
- 当前会话里只存在一个资源包
- 不需要比较多个候选图片

但真实目标不是这样。

用户真正想做的是：

- 同时导入多个 `.pck`
- 让工具尽量自动提取其中的可用卡图
- 当多个包都提供同一张卡的 portrait 时，由用户选一张保留
- 最终输出一个“最大公约整合包”

### 2.3 新目标的本质

下一阶段的核心问题不是“能否解包”，也不是“能否 build”，而是：

- 多来源资产整合
- 同一 `cardId` 的冲突管理
- 可回溯的人工决策

因此，接下来最关键的系统能力将变成：

- 会话级多包管理
- 基于 `cardId` 的候选聚合
- 冲突可视化和人工选择

## 3. 设计原则

### 3.1 单个 `.pck` 仍然是基础单元

即使最终目标是整合多个包，也不应该跳过“单包分析”这一步。

每个 `.pck` 仍然应该单独经历：

- recover
- scan
- analyze

然后再进入统一 merge 阶段。

原因：

- 便于追踪每个来源的原始信息
- 便于后续排查问题
- 冲突界面需要知道每张图来自哪个包

### 3.2 冲突不能自动隐式覆盖

可以提供自动预选规则，但不能静默吞掉冲突。

合理策略是：

- 无冲突时自动通过
- 有冲突时进入冲突池
- GUI 提供默认选中项，但允许手工改

### 3.3 “忽略”和“待定”必须继续区分

在多包整合里，这个区分比单包更重要：

- `Ignored`
  - 用户明确不要这个候选
- `Pending`
  - 用户还没有处理

最终构建时：

- `Ignored` 不参与
- `Pending` 默认不参与，但需要构建前警告

### 3.4 源码树是内部中间产物

源码树不应再作为用户主要交互对象。

外部用户应该关心的是：

- 导入哪些包
- 冲突怎么选
- 最终 mod 产物输出到哪里

因此：

- 源码树统一写到 `cache`
- 对外只暴露最终产物目录

## 4. 总体架构升级

下一阶段建议形成五层流程：

1. `Package Import Layer`
2. `Per-Package Analysis Layer`
3. `Merge Layer`
4. `Conflict Resolution Layer`
5. `Build Layer`

### 4.1 Package Import Layer

职责：

- 导入一个或多个 `.pck`
- 为每个 `.pck` 创建独立的 recover 结果
- 保留来源标识

输入：

- 多个 `.pck`

输出：

- 一组 `ImportedPackage`

### 4.2 Per-Package Analysis Layer

职责：

- 对每个包单独做：
  - scan
  - analyze

输入：

- `ImportedPackage`

输出：

- 一组单包 `mapping_analysis_result`

### 4.3 Merge Layer

职责：

- 把多个包的映射结果合并成统一候选池
- 按 `cardId` 聚合
- 标记无冲突项与冲突项

输入：

- 多个 `mapping_analysis_result`

输出：

- `MergedReviewSession`

### 4.4 Conflict Resolution Layer

职责：

- 在 GUI 中呈现冲突组
- 让用户为每个 `cardId` 选择唯一来源
- 支持忽略候选
- 支持保留待定项

输入：

- `MergedReviewSession`

输出：

- `ResolvedReviewSession`

### 4.5 Build Layer

职责：

- 内部生成整合 mod 源码树
- 只 materialize 最终胜出的候选
- `dotnet build`
- 收集最终产物到指定目录

输入：

- `ResolvedReviewSession`
- 用户选择的产物输出目录

输出：

- 最终 `.dll/.json/.pck`

## 5. 会话模型

多包整合必须以“会话”为中心，而不是以“单次导入”为中心。

建议目录结构：

```text
cache/
  sessions/
    20260416_153000_merge_regent_pack/
      imports/
        package_a/
          recover/
          asset_scan_result.json
          mapping_analysis_result.json
        package_b/
          recover/
          asset_scan_result.json
          mapping_analysis_result.json
      merged/
        merged_review_session.json
      generated_src/
        MyMergedPortraitMod/
      build/
        publish.log
        result.json
```

### 5.1 ImportedPackage

建议模型字段：

- `packageId`
- `displayName`
- `sourcePckPath`
- `recoverRoot`
- `scanResultPath`
- `mappingAnalysisPath`
- `importedAt`

### 5.2 ReviewSession 的升级

当前单包 `ReviewSession` 需要升级为会话级结构，至少应包含：

- `sessionId`
- `officialCardIndexPath`
- `packages`
- `mergedCandidates`
- `conflictGroups`
- `resolvedMappings`

## 6. 数据模型建议

### 6.1 单包候选模型

保留当前 `MappingCandidate` 的核心字段，并补来源信息：

- `sourcePackageId`
- `sourcePackageName`
- `sourceAbsolutePath`
- `relativePath`
- `fileName`
- `matchedCardId`
- `canonicalName`
- `group`
- `selected`
- `ignored`
- `confidence`
- `matchReason`

### 6.2 合并候选模型

新增 `MergedMappingCandidate`：

- `cardId`
- `canonicalName`
- `group`
- `sourcePackageId`
- `sourcePackageName`
- `sourceAbsolutePath`
- `sourceRelativePath`
- `confidence`
- `isAutoSelected`
- `isIgnored`
- `isConflict`

### 6.3 冲突组模型

新增 `CardConflictGroup`：

- `cardId`
- `canonicalName`
- `group`
- `candidates`
- `selectedCandidateId`
- `resolutionState`

其中：

- `resolutionState = Resolved | Pending | Ignored`

### 6.4 最终映射模型

新增 `ResolvedMapping`：

- `cardId`
- `canonicalName`
- `group`
- `selectedSourcePackageId`
- `selectedSourceAbsolutePath`
- `selectedSourceRelativePath`

这是后续 materialize 的唯一输入。

## 7. Merge 规则设计

### 7.1 按 cardId 聚合

合并阶段的核心规则：

- 所有已经匹配到 `cardId` 的候选
- 按 `cardId` 分组

结果分成三类：

1. 无冲突
2. 多候选冲突
3. 未匹配候选

### 7.2 无冲突

同一个 `cardId` 只有一个候选时：

- 自动入选
- `resolutionState = Resolved`

### 7.3 多候选冲突

同一个 `cardId` 有多个候选时：

- 标记为冲突
- 给出一个默认选中项
- 但必须允许用户手工改

### 7.4 未匹配候选

未匹配到 `cardId` 的图片：

- 仍然保留在会话中
- 进入 `Unmatched` 队列
- 后续可人工指定 `cardId`
- 一旦指定，就重新进入 merge 过程

## 8. 默认选中策略

多候选冲突时建议先给一个“预选规则”，减轻用户负担。

第一版建议支持以下简单规则：

1. 导入顺序优先
   - 后导入覆盖前导入

2. 包优先级
   - 用户为每个包指定权重
   - 权重高的优先

3. 匹配置信度优先
   - 高置信度优先

第一版可以先做最简单的：

- 默认按导入顺序后者优先

然后在冲突界面里允许手动覆盖。

## 9. GUI 设计升级

### 9.1 当前 GUI 的问题

当前 GUI 是单包视角：

- 左边是一组图片候选
- 右边是单项审核

这对于单包修正足够，但对于多包整合不够。

### 9.2 未来 GUI 应有两个核心视图

建议新增两个视图模式：

1. `Assets`
2. `Conflicts`

#### Assets 视图

用途：

- 处理未匹配项
- 手工指定 `cardId`
- 手工 `ignore`

仍然保留现有的图片预览与右侧编辑逻辑。

#### Conflicts 视图

用途：

- 处理同一 `cardId` 的多个候选
- 对比多个来源
- 选择最终保留项

### 9.3 Conflicts 视图建议布局

建议：

- 左侧：冲突卡牌列表
- 右侧上半部分：多来源图片并排预览
- 右侧下半部分：来源信息和选择控件

每个冲突候选至少展示：

- 预览图
- 来源包名
- 原始文件名
- 匹配原因
- 置信度
- “选中此项”按钮

### 9.4 导入区升级

当前 GUI 已经支持：

- `Import PCK`

下一阶段要升级为：

- 连续导入多个 `.pck`
- 当前会话里显示已导入包列表
- 支持移除某个包并重新 merge

### 9.5 会话状态显示

GUI 顶部建议增加会话摘要：

- 已导入包数
- 总候选数
- 无冲突自动通过数
- 冲突组数
- 未匹配数
- Ignored 数
- Pending 数

## 10. 构建流程升级

### 10.1 当前构建链

当前已经改成：

- 内部生成源码树到 `cache`
- `dotnet build`
- 收集 `.dll/.pck/.json`
- 复制到最终产物目录

这条链适合继续沿用。

### 10.2 多包整合时的 materialize 原则

构建前不再直接消费单个 `mapping_analysis_result`。

而是只消费：

- `ResolvedMapping` 集合

也就是说：

- 一个 `cardId`
- 最终只能有一个胜出来源

Materialize 时只复制这些胜出来源的图片。

### 10.3 最终产物结构

输出目录继续建议：

```text
<ArtifactDir>/
  <ModId>/
    <ModId>.dll
    <ModId>.json
    <ModId>.pck
```

源码树仍然只存在于：

- `cache/sessions/.../generated_src/`

## 11. 当前代码结构的落地建议

### 11.1 Core 新增服务

建议新增：

- `MergeMappingsService`
- `ConflictResolutionService`
- `ReviewSessionPersistenceService`

#### MergeMappingsService

职责：

- 输入多个单包 `MappingAnalysisResult`
- 输出合并后的会话模型

#### ConflictResolutionService

职责：

- 接收用户对冲突组的操作
- 更新最终 `ResolvedMapping`

#### ReviewSessionPersistenceService

职责：

- 保存和加载合并后的会话 JSON

### 11.2 GUI 新增能力

建议在现有 GUI 基础上新增：

- `Imported Packages` 面板
- `Conflicts` 视图
- `Session Summary` 面板

### 11.3 不建议马上做的内容

这一阶段不建议立刻做：

- 自动相似图像比对
- 图像内容分类
- 多包自动 dedupe 的复杂算法
- 云端卡图数据库

先把“多来源冲突 + 人工选择”打稳更重要。

## 12. 关键风险

### 12.1 一个包中可能已经存在错误映射

如果来源包文件名本身混乱：

- 合并层不能假设单包结果永远正确
- GUI 仍然必须保留手工修正能力

### 12.2 多包冲突量可能很大

如果用户一次导入很多 portrait 包：

- 冲突组可能很多
- 必须提供默认预选和过滤器

否则 GUI 会非常难用。

### 12.3 同一个包可能包含无关图片

即使做多包整合，未匹配项管理依然重要：

- 不应该因为多包模式就放弃 unmatch 审核

### 12.4 构建产物与会话状态不一致

如果用户解决冲突后没有保存会话，又直接 build：

- 需要保证 build 永远消费当前内存态的 resolved 结果
- 而不是过期文件

## 13. 推荐实施顺序

### 阶段 1：会话模型升级

目标：

- 支持一个 session 下导入多个 `.pck`
- 每个包保留独立 recover/scan/analyze 结果

完成标志：

- GUI 能连续导入多个包
- `cache` 中每个包的中间结果可独立追踪

### 阶段 2：Merge 服务

目标：

- 新增 `MergeMappingsService`
- 基于多个分析结果生成合并候选与冲突组

完成标志：

- 能产生 `merged_review_session.json`

### 阶段 3：Conflicts 视图

目标：

- GUI 可视化处理冲突
- 为每个冲突 `cardId` 选择最终来源

完成标志：

- 所有冲突组都可被手工 resolve

### 阶段 4：构建消费 resolved 结果

目标：

- build 只消费最终胜出来源

完成标志：

- 导出的 mod 不再受单包 analysis 直接影响

### 阶段 5：体验增强

目标：

- 默认预选策略
- 包优先级
- 批量忽略
- 下一条未处理项自动跳转

## 14. MVP 定义

多包整合第一版 MVP 建议限定为：

- 支持连续导入多个 `.pck`
- 每个包单独 recover/scan/analyze
- 按 `cardId` 自动 merge
- 无冲突项自动通过
- 冲突项进入人工审核
- GUI 中可为每个冲突组选择一个候选
- 未匹配项可继续手工指定 `cardId`
- 最终 build 出整合 mod

第一版可以暂时不做：

- 包优先级排序 UI
- 自动跨包相似图像判重
- 批量规则库

## 15. 结论

项目下一阶段的核心已经不再是：

- 单个 `.pck` 的导入能力
- 或单次 build 能否成功

而是：

- 多来源候选整合
- 冲突决策
- 最终唯一映射输出

因此，最正确的下一步不是继续堆单包细节，而是：

1. 让 GUI 支持一个会话里连续导入多个 `.pck`
2. 引入 `MergeMappingsService`
3. 做 `Conflicts` 视图
4. 让 build 只消费 resolved 结果

这样项目才会真正从“单包导入器”升级成“整合 mod 生成器”。
