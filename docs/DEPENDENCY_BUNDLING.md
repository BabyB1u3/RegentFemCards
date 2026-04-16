# Dependency Bundling Guide

## 1. 文档目的

本文档用于明确本项目在对外发布时，哪些依赖应该随软件一起打包，哪些依赖不应该内含，以及这些判断背后的原因。

本文档的默认发布目标是：

- 用户本地已经安装 `Slay the Spire 2`
- 除游戏本体及其安装目录内容外，生成器运行和构建流程所需的其他依赖尽量全部内含
- 最终发布包应尽量支持离线使用

## 2. 结论摘要

如果希望把本项目发布成一个尽量完整的离线工具包，那么发布包里应当内含：

- 生成器程序本体
- 模板工程
- 内置数据文件
- GDRETools
- Godot / MegaDot
- `.NET SDK`
- 本地 NuGet feed 及其所需包

不应内含：

- `Slay the Spire 2` 游戏本体
- 游戏安装目录中的 `sts2.dll`
- 游戏安装目录中的 `0Harmony.dll`
- 游戏安装目录中的 `data_sts2_*` 数据目录

## 3. 必须内含的内容

以下内容属于发布包的核心组成部分。如果缺少它们，工具将无法完整运行，或者无法完成最终构建。

### 3.1 生成器程序本体

应内含：

- `PortraitModGenerator.Core`
- `PortraitModGenerator.Cli`
- `PortraitModGenerator.Gui`

原因：

- 这些项目本身就是工具的功能主体
- GUI 负责用户入口
- CLI 负责脚本化流程
- Core 负责模板生成、PCK 导入、扫描、映射和构建流程

相关代码：

- [tools/PortraitModGenerator.Core](../tools/PortraitModGenerator.Core)
- [tools/PortraitModGenerator.Cli](../tools/PortraitModGenerator.Cli)
- [tools/PortraitModGenerator.Gui](../tools/PortraitModGenerator.Gui)

### 3.2 模板工程

应内含：

- [templates/PortraitReplacementTemplate](../templates/PortraitReplacementTemplate)

原因：

- 生成器不是直接从零拼装一个 Mod，而是先实例化模板工程
- 模板目录中包含 `.csproj`、manifest、Godot 项目文件、运行时代码和默认配置结构
- 生成器运行时会直接读取模板目录

相关代码：

- [TemplateProjectGenerator.cs](../tools/PortraitModGenerator.Core/Services/TemplateProjectGenerator.cs)
- [TemplateManifestLoader.cs](../tools/PortraitModGenerator.Core/Services/TemplateManifestLoader.cs)

### 3.3 内置数据文件

应内含：

- [data/official_card_index.json](../data/official_card_index.json)

原因：

- 映射分析依赖这份官方卡牌索引来确定候选 `cardId`
- 当前流程把这份文件视为仓库内置数据，而不是用户运行时额外提供的输入

相关代码：

- [OfficialCardIndexLoader.cs](../tools/PortraitModGenerator.Core/Services/OfficialCardIndexLoader.cs)
- [MappingAnalyzer.cs](../tools/PortraitModGenerator.Core/Services/MappingAnalyzer.cs)

### 3.4 GDRETools

应内含：

- [gdre/gdre_tools.exe](../gdre/gdre_tools.exe)
- `gdre/` 目录下与其配套的运行文件

原因：

- 当前 `.pck` 导入流程依赖 GDRETools 进行 recover
- 程序会直接按路径启动该工具，而不是通过用户系统环境查找同类软件

相关代码：

- [GdrePckImporter.cs](../tools/PortraitModGenerator.Core/Services/GdrePckImporter.cs)
- [AppPaths.cs](../tools/PortraitModGenerator.Gui/AppPaths.cs)

### 3.5 Godot / MegaDot

应内含：

- 与模板兼容的 Godot / MegaDot 可执行文件

原因：

- 模板在 `Publish` 后会调用 `GodotPath` 执行 `--headless --export-pack`
- 如果不内含这部分工具，生成器只能生成源码树，无法稳定导出最终 `.pck`

相关代码：

- [templates/PortraitReplacementTemplate/src/Directory.Build.props](../templates/PortraitReplacementTemplate/src/Directory.Build.props)
- [templates/PortraitReplacementTemplate/src/__MOD_ID__.csproj](../templates/PortraitReplacementTemplate/src/__MOD_ID__.csproj)

### 3.6 `.NET SDK`

应内含：

- 与项目目标框架兼容的 `.NET SDK`

原因：

- 当前最终构建链路不是只运行一个现成可执行文件，而是会执行 `dotnet build`
- 仅内含 `.NET runtime` 不足以支持模板项目的 restore 和 build
- 如果希望用户无需额外安装 .NET，就必须把 SDK 一起打包

相关代码：

- [ModBuildService.cs](../tools/PortraitModGenerator.Core/Services/ModBuildService.cs)
- [PortraitModGenerator.Core.csproj](../tools/PortraitModGenerator.Core/PortraitModGenerator.Core.csproj)
- [PortraitModGenerator.Cli.csproj](../tools/PortraitModGenerator.Cli/PortraitModGenerator.Cli.csproj)
- [PortraitModGenerator.Gui.csproj](../tools/PortraitModGenerator.Gui/PortraitModGenerator.Gui.csproj)

## 4. 离线发布时也必须内含的内容

如果目标是让用户在没有外网的环境里也能运行“生成并构建 mod”的完整流程，那么下面这些内容也必须打包进去。

### 4.1 本地 NuGet feed

应内含：

- 模板项目依赖的全部 `nupkg`
- 本地 `NuGet.config`

原因：

- 模板项目当前通过 `PackageReference` 使用外部 NuGet 包
- 如果发布包不带本地 feed，离线机器上 restore 会失败
- 即使在线，完全依赖外部源也会降低构建可重复性

当前模板依赖包括：

- `Godot.NET.Sdk`
- `Alchyr.Sts2.BaseLib`
- `Krafs.Publicizer`
- `Alchyr.Sts2.ModAnalyzers`
- `BSchneppe.StS2.PckPacker`

相关代码：

- [templates/PortraitReplacementTemplate/src/__MOD_ID__.csproj](../templates/PortraitReplacementTemplate/src/__MOD_ID__.csproj)
- [nuget.config](../nuget.config)

### 4.2 固定包版本

应随发布方案一起落实：

- 将模板中的浮动版本固定为明确版本号

原因：

- 浮动版本会导致不同时间 restore 到不同结果
- 不利于离线镜像构建
- 也不利于定位用户环境问题

当前需要重点处理的项目：

- [templates/PortraitReplacementTemplate/src/__MOD_ID__.csproj](../templates/PortraitReplacementTemplate/src/__MOD_ID__.csproj)

## 5. 建议一并内含的内容

这些内容理论上不一定是“绝对必需”，但为了减少用户配置成本，建议也随发布包一起提供。

### 5.1 工具路径配置

建议内含：

- 默认工具路径配置
- 程序内部路径发现逻辑

原因：

- 当前项目已经有基于仓库相对路径的查找逻辑
- 如果发布包目录结构固定，程序就可以开箱即用，无需用户手动配置

相关代码：

- [AppPaths.cs](../tools/PortraitModGenerator.Gui/AppPaths.cs)

### 5.2 缓存和工作目录约定

建议内含或在首次运行时自动创建：

- `cache/`
- `generated/`
- `artifacts/`

原因：

- 这些目录已经是 GUI 流程的一部分
- 统一目录结构可以减少路径配置错误

相关代码：

- [AppPaths.cs](../tools/PortraitModGenerator.Gui/AppPaths.cs)

### 5.3 发布启动器

建议内含：

- 启动脚本或 launcher

原因：

- 可统一指定内含的 `dotnet`、`godot`、`gdre`
- 可集中处理环境变量、工作目录和日志目录
- 可避免把真实工具链细节暴露给最终用户

## 6. 不应内含的内容

以下内容不建议放入发布包。

### 6.1 Slay the Spire 2 游戏本体

不应内含：

- 游戏安装文件
- 游戏可执行文件

原因：

- 不属于生成器自身的一部分
- 发布包的目标前提就是“用户本地已安装游戏”

### 6.2 游戏安装目录中的运行时依赖

不应内含：

- `sts2.dll`
- `0Harmony.dll`
- `data_sts2_windows_x86_64`
- `data_sts2_linuxbsd_x86_64`
- `data_sts2_macos_x86_64`

原因：

- 模板项目当前设计就是从用户本地游戏目录中发现这些文件
- 把这些内容打进发布包会让工具与用户真实游戏版本脱节
- 也会增加维护和分发风险

相关代码：

- [templates/PortraitReplacementTemplate/src/Sts2PathDiscovery.props](../templates/PortraitReplacementTemplate/src/Sts2PathDiscovery.props)
- [templates/PortraitReplacementTemplate/src/__MOD_ID__.csproj](../templates/PortraitReplacementTemplate/src/__MOD_ID__.csproj)

## 7. 发布包建议结构

下面是一种适合当前项目的发布包结构示意。

```text
StS2PortraitModGenerator/
  app/
    PortraitModGenerator.Gui.exe
    PortraitModGenerator.Cli.exe
    PortraitModGenerator.Core.dll
  tools/
    dotnet/
    godot/
    gdre/
  templates/
    PortraitReplacementTemplate/
  data/
    official_card_index.json
  packages/
    <local nuget feed>
  config/
    NuGet.config
  cache/
  generated/
  artifacts/
  logs/
```

说明：

- `app/` 放生成器程序本体
- `tools/dotnet/` 放内含 SDK
- `tools/godot/` 放内含 Godot 或 MegaDot
- `tools/gdre/` 放 GDRETools
- `packages/` 放离线 NuGet feed
- `cache/`、`generated/`、`artifacts/` 用作工作目录

## 8. 当前项目与发布目标的差距

仓库当前已经具备的基础：

- 仓库根 `nuget.config` 已经把 `globalPackagesFolder` 指向 `packages/`，目录中已经包含模板需要的核心 NuGet 包
- `gdre/` 已经随仓库分发 `gdre_tools.exe` 与配套的 `pck`
- GUI 通过 [AppPaths.cs](../tools/PortraitModGenerator.Gui/AppPaths.cs) 的根目录探测，能基于仓库或发布包目录开箱即用

离“完整内含、可离线、可独立分发”还差下面几项工作：

1. 让构建流程优先调用发布包自带的 `dotnet`，而不是依赖系统 PATH 中的 `dotnet`
2. 让模板发布流程优先调用发布包自带的 `godot` / `MegaDot`
3. 把模板 `__MOD_ID__.csproj` 中的浮动版本固定到具体版本号，避免重新 restore 时漂移
4. 提供发布启动器（脚本或 launcher），统一传入 `dotnet`、`godot`、`gdre` 与工作目录

## 9. 一句话总结

本项目对外发布时，应该尽量把“生成器、模板、数据、GDRE、Godot、.NET SDK、本地 NuGet feed”全部内含；而 `Slay the Spire 2` 游戏本体及其安装目录中的 dll 和数据文件，应继续由用户本地安装提供。
