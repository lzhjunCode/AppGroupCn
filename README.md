> [!NOTE]
> 只从本官方仓库下载：[github.com/iandiv/AppGroup](https://github.com/iandiv/AppGroup)。

> [!NOTE]
> Download only from this official repository: [github.com/iandiv/AppGroup](https://github.com/iandiv/AppGroup). 

<div align="center">

<img width="200" alt="AppGroup logo: Windows taskbar app group launcher" src="https://github.com/user-attachments/assets/f952f7fa-2c12-41e1-ba7c-b3b6ea47c586" />

# AppGroup：Windows 任务栏应用分组与启动器

# AppGroup: Windows Taskbar App Groups & Launcher

**把应用整理成组，固定到 Windows 任务栏；弹出的面板在观感和交互上都像是 Windows 11 的一部分。**

**Organize your apps into groups you can pin to the Windows taskbar, in a popup that looks and feels like part of Windows 11.**

[![Latest Release](https://img.shields.io/github/v/release/iandiv/AppGroup?style=for-the-badge&label=Latest&color=0078D4)](https://github.com/iandiv/AppGroup/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/iandiv/AppGroup/total?style=for-the-badge&label=Downloads&color=2EA44F)](https://github.com/iandiv/AppGroup/releases)
[![Stars](https://img.shields.io/github/stars/iandiv/AppGroup?style=for-the-badge&label=Stars&color=F5B301)](https://github.com/iandiv/AppGroup/stargazers)
[![License: MIT](https://img.shields.io/github/license/iandiv/AppGroup?style=for-the-badge&color=6E7781)](LICENSE)
![Platform: Windows 10 | 11](https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D4?style=for-the-badge&logo=windows&logoColor=white)

**[⬇ 下载（安装版，内置运行时）](https://github.com/iandiv/AppGroup/releases/latest)** · [快速开始](#quick-start) · [演示](#see-it-in-action) · [常见问题](#faq) · [获取帮助](#help-and-support)

**[⬇ Download (Setup, Bundled)](https://github.com/iandiv/AppGroup/releases/latest)** · [Quick start](#quick-start) · [Demo](#see-it-in-action) · [FAQ](#faq) · [Get help](#help-and-support)

<p align="center">
  
<img  width="310" alt="An AppGroup popup designed in the style of the Windows 11 Quick Settings panel"  src="https://github.com/user-attachments/assets/6cd38b41-ab70-4667-b8e5-70700f4eb89d" />

<img width="300" alt="The Windows 11 Quick Settings panel"  src="https://github.com/user-attachments/assets/52193197-6552-4b8c-8716-7b7c5d7ebb86" />


</p>
<p align="center"><em>左侧为 AppGroup 弹窗，设计灵感来自右侧的 Windows 11 快速设置。</em></p>
<p align="center"><em>An AppGroup popup (left), inspired by the design of Windows 11 Quick Settings (right).</em></p>

</div>

AppGroup 是一款免费、开源（MIT 许可）的 Windows 任务栏整理器与应用启动器。它保留 Windows 任务栏熟悉的用法：你固定一个快捷方式，AppGroup 负责提供弹窗和分组内容。它基于 WinUI 3 构建，弹窗在浅色与深色模式下都被设计成像是 Windows 11 的原生组成部分，与开始菜单、快速设置和通知面板风格一致。

AppGroup is a free, open-source (MIT) taskbar organizer and app launcher for Windows. It keeps the familiar Windows taskbar workflow: you pin a shortcut, and AppGroup supplies the popup and the group's contents. Built with WinUI 3, its popups are designed to look and feel like a part of Windows 11, matching the Start menu and the Quick Settings and notification panels, in both Light and Dark mode.

## 为什么选择 AppGroup？

## Why AppGroup?

任务栏挤满图标会拖慢你的操作。用 AppGroup，你可以做到例如：

A crowded taskbar slows you down. With AppGroup you can, for example:

- 固定**一个工作分组**（编辑器、浏览器、聊天、笔记）；
- **一个游戏分组**（启动器和 Steam 快捷方式）；
- **一个创意应用分组**；

- pin **one group for work** (editor, browser, chat, notes),
- **one for games** (launchers and Steam shortcuts),
- **one for creative apps**,

之后从任务栏的单个图标打开你需要的那一组，而不必在一长排图标里翻找。一个分组还可以用**全部启动（Launch All）**一次性打开组内所有应用。

then open the group you need from a single taskbar icon instead of hunting through a long row of icons. A group can also open all of its apps at once with **Launch All**.

而且它一点也不像"外挂"：AppGroup 遵循 Windows 11 的设计语言（圆角、Mica/亚克力质感表面、强调色），能和系统其余部分融为一体。

And it doesn't look bolted on: AppGroup follows Windows 11 design (rounded corners, Mica/acrylic-style surfaces, accent colors) so it blends in with the rest of your system.

## 快速开始

## Quick start

1. **[下载安装版（内置运行时）](https://github.com/iandiv/AppGroup/releases/latest)** 并运行安装程序。它自带 .NET 8，无需另外安装任何东西。（不想用安装版？可用同一页面的**便携版（内置运行时）**。）
2. **创建分组并添加应用。** 点击 AppGroup 中的 **+**，为分组命名，然后点击组内的 **+**，或从开始菜单或 `shell:AppsFolder` 把应用拖进来。
3. **把分组图标拖到任务栏上。** 点击它即可打开弹窗并启动应用。

1. **[Download Setup (Bundled)](https://github.com/iandiv/AppGroup/releases/latest)** and run the installer. It includes .NET 8, so there is nothing else to install. (Prefer no installer? Use **Portable (Bundled)** from the same page.)
2. **Create a group and add apps.** Click **+** in AppGroup, name the group, then click **+** inside it or drag apps in from the Start Menu or `shell:AppsFolder`.
3. **Drag the group icon onto the taskbar.** Click it to open the popup and launch an app.

## 分组是怎么工作的

## How a group works

AppGroup 会**为每个分组创建一个快捷方式**。你固定这个快捷方式后，任务栏仍由 Windows 照常管理；AppGroup 负责绘制弹窗并保存组内内容（应用、快捷方式、子分组）。AppGroup 不会替换任务栏，也不会在任务栏上创建真正的文件夹。

AppGroup creates a **shortcut for each group**. When you pin that shortcut, Windows keeps managing the taskbar as usual; AppGroup draws the popup and holds the group's contents (apps, shortcuts, and subgroups). AppGroup does not replace the taskbar and does not create real folders on it.

## 实际效果

## See it in action

<p align="center">
  <img src="https://github.com/user-attachments/assets/39d02528-0cda-43f3-abc4-7a2567140c58" width="300" alt="AppGroup main window listing the user's app groups">
  <img src="https://github.com/user-attachments/assets/73703278-b4c8-4b93-a4cb-5c8cf49ae2a8" width="300" alt="AppGroup group editor showing a group's apps and icon options">
  <img src="https://github.com/user-attachments/assets/4ef0825d-506e-49be-9f1b-5f66faf4ad8f" width="300" alt="AppGroup popup showing the apps in a group opened from the taskbar">
</p>
<p align="center"><em>从左到右：分组列表、分组编辑器，以及从任务栏打开的分组弹窗。</em></p>
<p align="center"><em>Left to right: the group list, the group editor, and a group's popup.</em></p>

https://github.com/user-attachments/assets/6d37560f-16ea-45a9-b8b2-9d94bced0ff2

*演示视频：AppGroup 的实际使用。*

*Demo video: AppGroup in use.*

<h3 align="center">把分组固定到任务栏或桌面</h3>

<h3 align="center">Pin a group to the taskbar or desktop</h3>

<p align="center">
  <img width="270" alt="Animation of dragging an AppGroup group icon onto the Windows taskbar to pin it" src="https://github.com/user-attachments/assets/b54ac465-929d-46d1-98c4-2681aaed5995" />
  <img width="350" alt="Animation of dragging an AppGroup group icon onto the Windows desktop to create a shortcut" src="https://github.com/user-attachments/assets/a8994310-76e4-44fd-ad1e-d8ff3e9f2521" />
</p>
<p align="center"><em>把分组图标拖到任务栏（左）或桌面（右）。</em></p>
<p align="center"><em>Drag a group icon onto the taskbar (left) or the desktop (right).</em></p>

<h3 align="center">子分组</h3>

<h3 align="center">Subgroups</h3>

<p align="center">
  <img width="550" alt="An AppGroup popup with a subgroup opened inside a group" src="https://github.com/user-attachments/assets/90d80744-6a6e-4a99-8fe8-a5469e401f39" />
</p>
<p align="center"><em>打开组中组。</em></p>
<p align="center"><em>Open a group inside a group.</em></p>

<h3 align="center">系统托盘</h3>

<h3 align="center">System tray</h3>

<p align="center">
  <img width="250" alt="AppGroup running in the Windows system tray" src="https://github.com/user-attachments/assets/6cd38b41-ab70-4667-b8e5-70700f4eb89d" />
</p>
<p align="center"><em>AppGroup 可以驻留在系统托盘。</em></p>
<p align="center"><em>AppGroup can live in the system tray.</em></p>

<h3 align="center">看起来就是 Windows 11 的一部分</h3>

<h3 align="center">Looks like part of Windows 11</h3>

<p align="center">
  <img width="250" alt="AppGroup Default layout with the group name header at the bottom" src="https://github.com/user-attachments/assets/4129bc50-9bf6-4e26-9282-bd1d32461632" />
  <img width="250" alt="AppGroup Default layout with the group name header at the top" src="https://github.com/user-attachments/assets/85db9020-94ac-491b-b7e3-d18429db97b1" />
  <img width="250" alt="AppGroup Card layout styled like the Windows 11 Quick Settings panel, header at the bottom" src="https://github.com/user-attachments/assets/f9f0cfb8-b5cd-4274-b498-0c61527511da" />
  <img width="250" alt="AppGroup Card layout styled like the Windows 11 Quick Settings panel, header at the top" src="https://github.com/user-attachments/assets/b0afdd7f-4bfe-4c17-a031-f6721f86c020" />
</p>
<p align="center"><em>Card 布局，风格仿照 Windows 11 快速设置。默认布局和 Card 布局都支持标题栏放在顶部或底部。</em></p>
<p align="center"><em>The Card layout, styled after Windows 11 Quick Settings. Default and Card layouts both work with the header at the top or bottom.</em></p>

<h3 align="center">带预览的导入</h3>

<h3 align="center">Import with preview</h3>

https://github.com/user-attachments/assets/ed518124-53a7-40c4-97cb-42d7ac331d63

<p align="center"><em>从 <code>.agz</code> 文件或 TaskbarGroups 导入之前，可先预览分组。</em></p>
<p align="center"><em>Preview groups before importing from an <code>.agz</code> file or TaskbarGroups.</em></p>

## 功能特性

## Features

### 分组

### Groups

- 创建、编辑、删除和复制分组
- **子分组：** 把某个分组作为条目放进另一个分组；它会以独立的弹窗打开
- 通过拖放调整分组和应用的顺序，或按字母顺序排列分组（会禁用手动排序）
- 把分组图标拖到任务栏或桌面即可固定该分组的快捷方式
- 跳转列表操作：**编辑此分组** 和 **全部启动**（可在设置中开关）

- Create, edit, delete, and duplicate groups
- **Subgroups:** add a group as an item inside another group; it opens as its own popup
- Reorder groups and apps by drag and drop, or sort groups alphabetically (disables manual reordering)
- Pin a group shortcut by dragging its icon to the taskbar or desktop
- Jumplist actions: **Edit this Group** and **Launch All** (toggle in Settings)

### 外观

### Appearance

- **原生 Windows 11 观感：** 弹窗风格仿照开始菜单和快速设置，支持浅色与深色模式以及强调色
- **图标样式：** 单个图标，或由各应用图标自动生成的网格图标
- **布局：** 默认或 Card；网格列数可调
- 强调色的分组背景；显示或隐藏分组名称标题（顶部或底部）
- 深色与浅色模式，主窗口和弹窗可分别设置
- 用 `.exe` 文件作为自定义图标；自定义提示文字；为单个应用设置启动参数
- 应用名称标签（底部或右侧）；窗口与内容动画可开关

- **Native Windows 11 look:** popups styled after the Start menu and Quick Settings, with Light and Dark mode and accent colors
- **Icon styles:** single icon, or an auto-generated grid icon from the apps' icons
- **Layouts:** Default or Card; adjustable grid columns
- Accent-colored group backgrounds; show or hide group name headers (Top or Bottom)
- Dark and Light mode, set independently for the main window and popup
- `.exe` files as custom icons; custom tooltips; per-app launch arguments
- App labels (Bottom or Right); toggleable window and content animations

### 应用与快捷方式

### Apps and shortcuts

- 传统应用和 UWP 应用，可从开始菜单或 `shell:AppsFolder` 拖入
- PWA 应用（通过快捷方式）和 Steam 快捷方式（`.url`）
- 不带箭头角标的 `.lnk` 快捷方式（在支持的环境中）
- 以管理员身份运行应用

- Regular and UWP apps, dragged from the Start Menu or `shell:AppsFolder`
- PWA apps (via shortcuts) and Steam shortcuts (`.url`)
- `.lnk` shortcuts without the arrow overlay (where supported)
- Run apps as Administrator

### 导入、导出与系统集成

### Import, export, and system

- `.agz`（AppGroupZip）文件，用于导入、导出、备份和分享；导入前可预览并挑选分组；导入采用追加方式，绝不覆盖原有内容
- 从 **TaskbarGroups** 导入（[迁移步骤](#coming-from-taskbargroups-or-taskbargroupsfluent)）
- 支持系统托盘、开机自启、任务栏图标灰度选项
- 支持位于顶部、底部、左侧或右侧的任务栏
- 图标缓存以加快加载；分组图标变化时任务栏图标会自动刷新
- 内置更新提醒

- `.agz` (AppGroupZip) files for import, export, backup, and sharing; preview and select groups before importing; imports are appended, never overwriting
- Import from **TaskbarGroups** ([migration steps](#coming-from-taskbargroups-or-taskbargroupsfluent))
- System tray support, run at startup, grayscale taskbar icon option
- Supports taskbars positioned at the top, bottom, left, or right
- Icon cache for faster loading; taskbar icons refresh when group icons change
- Built-in update notifier

## 安装

## Installation

**系统要求：** Windows 10 版本 1809（内部版本 17763）或更高，包括 Windows 11。**构建技术：** WinUI 3 与 .NET 8。

**Requires:** Windows 10 version 1809 (build 17763) or later, including Windows 11. **Built with:** WinUI 3 and .NET 8.

| 版本 | 安装程序 | 含 .NET 8 | 适用场景 |
|---|---|---|---|
| **安装版（内置运行时）** | ✅ | ✅ | 大多数用户（推荐） |
| 安装版 | ✅ | ❌ | 已安装 .NET 8 的机器 |
| **便携版（内置运行时）** | ❌ | ✅ | 从文件夹直接运行，无需安装 |
| 便携版 | ❌ | ❌ | 已安装 .NET 8 的便携使用 |

| Variant | Installer | Includes .NET 8 | Best for |
|---|---|---|---|
| **Setup (Bundled)** | ✅ | ✅ | Most users (recommended) |
| Setup | ✅ | ❌ | Machines that already have .NET 8 |
| **Portable (Bundled)** | ❌ | ✅ | Running from a folder, no install |
| Portable | ❌ | ❌ | Portable use with .NET 8 already installed |

从[最新版本](https://github.com/iandiv/AppGroup/releases/latest)下载。**安装版：** 运行 `.exe`。**便携版：** 解压 `.zip` 并运行 `AppGroup.exe`。

Download from the [latest release](https://github.com/iandiv/AppGroup/releases/latest). **Setup:** run the `.exe`. **Portable:** extract the `.zip` and run `AppGroup.exe`.

> [!NOTE]
> 自 v1.6.0 起，便携版把分组保存在自己的文件夹内（标准安装使用 `AppData\Local\AppGroup\Groups`）。如果升级后看不到原有分组，请从旧版本把它们导出为 `.agz` 文件，再导入到便携版中。

> [!NOTE]
> Since v1.6.0, portable versions store groups in their own folder (standard installs use `AppData\Local\AppGroup\Groups`). If your groups don't appear after upgrading, export them as an `.agz` file from the old version and import it into the portable one.

## 使用方法

## How to Use

### 添加应用

### Add apps

点击分组内的 **+**，或直接拖放。传统应用和 UWP 应用可以从以下位置拖入：
- **Windows 开始菜单**
- **`shell:AppsFolder`：** 按 `Win + R`，输入 `shell:AppsFolder` 后回车，即可看到所有已安装的 Win32 和 UWP 应用

Click **+** inside a group, or drag and drop. Regular and UWP apps can be dragged from:
- **The Windows Start Menu**
- **`shell:AppsFolder`:** press `Win + R`, type `shell:AppsFolder`, and press Enter to see all installed Win32 and UWP apps

### 自定义分组

### Customize a group

- **标题栏：** 启用*显示标题*，选择顶部或底部
- **布局：** 默认或 Card
- **图标样式：** *常规*（任选图标文件）或*网格*（由应用图标自动生成）
- **网格列数**与**标签**（*显示标签*，底部或右侧）

- **Header:** enable *Show Header*, choose Top or Bottom
- **Layout:** Default or Card
- **Icon style:** *Regular* (pick any icon file) or *Grid* (auto-generated from app icons)
- **Grid columns** and **Labels** (*Show Labels*, Bottom or Right)

### 把分组固定到任务栏

### Pin a group to the taskbar

- **方式 A：** 把分组图标拖到任务栏上。
- **方式 B：** 点击 **⋮** → **打开文件所在位置**，然后右键快捷方式 → **固定到任务栏**。

- **Option A:** drag the group icon onto the taskbar.
- **Option B:** click **⋮** → **Open File Location**, then right-click the shortcut → **Pin to Taskbar**.

### 使用子分组

### Use subgroups

把已有分组作为条目加入另一个分组。选中它会打开它自己的弹窗。

Add an existing group as an item inside another group. Selecting it opens its own popup.

### 备份或分享分组（`.agz`）

### Back up or share groups (`.agz`)

1. 点击 **⋮** → **导出**，把分组保存为 `.agz` 文件。
2. 要还原：**⋮** → **导入** → **`.agz`**，在预览中勾选或取消分组，然后确认。选中的分组会追加到你现有的配置中。

1. Click **⋮** → **Export** to save groups as an `.agz` file.
2. To restore: **⋮** → **Import** → **`.agz`**, check or uncheck groups in the preview, and confirm. Selected groups are appended to your setup.

## 从 TaskbarGroups 或 TaskbarGroupsFluent 迁移过来？

## Coming from TaskbarGroups or TaskbarGroupsFluent?

你可以迁移已有的分组：

You can migrate your existing groups:

1. 点击 **⋮** → **导入** → **TaskbarGroups**。
2. 选择**包含** `config` 的那个文件夹，然后点击**选择文件夹**。
   - **TaskbarGroupsFluent：** 在地址栏粘贴 `%APPDATA%\Jack Schierbeck` 并回车，然后选择 `taskbar-groups` 文件夹。
   - **TaskbarGroups：** 找到你的 TaskbarGroups 文件夹。
3. 挑选要包含的分组并确认。它们会追加到你现有的分组中。

1. Click **⋮** → **Import** → **TaskbarGroups**.
2. Select the folder that **contains** `config`, then click **Select Folder**.
   - **TaskbarGroupsFluent:** paste `%APPDATA%\Jack Schierbeck` into the address bar, press Enter, then select the `taskbar-groups` folder.
   - **TaskbarGroups:** locate your TaskbarGroups folder.
3. Pick the groups to include and confirm. They are appended to your existing groups.

> [!NOTE]
> 请选择父文件夹，而不是 `config` 文件夹本身。目前还不支持直接选择 `config`。

> [!NOTE]
> Select the parent folder, not the `config` folder itself. Selecting `config` directly isn't supported yet.

## 安全性与 VirusTotal 报告

## Security and VirusTotal reports

以下 VirusTotal 报告仅针对 **v1.6.0** 版本的文件。每个文件被扫描时，没有任何安全厂商将其标记为威胁。VirusTotal 的结果只是当时的快照：它们不构成安全保证或认证，部分引擎可能超时或分析失败，也并不适用于之后发布的版本。

These VirusTotal reports cover the **v1.6.0** release files only. At the time each file was scanned, no security vendor flagged it. VirusTotal results are snapshots: they are not a guarantee or certification of safety, some engines may time out or fail to analyze a file, and they do not apply to later versions.

- [Portable (.zip) 便携版](https://www.virustotal.com/gui/file/5b8272f6deed5b14b52192e090a1cd8c244f22b5e4b7849b3ada39582c555bf9)
- [Portable (Bundled .zip) 便携版（内置运行时）](https://www.virustotal.com/gui/file/9a98a8c7101eff049f177e75ff7459421fd7b8458fab5ea4524441c53e0e8685)
- [Setup (.exe) 安装版](https://www.virustotal.com/gui/file/3388223e02bf595ac417dba011ccd4ab555b5bbcb058bc3f506e0a56e757a5a4)
- [Setup (Bundled .exe) 安装版（内置运行时）](https://www.virustotal.com/gui/file/a6cb1a9e88be7350702eeab3c77ae473827e5216aec817611bc0b11387a35845)

源代码是公开的。请只从本仓库的 [Releases 页面](https://github.com/iandiv/AppGroup/releases)下载。

The source code is public. Download only from this repository's [Releases page](https://github.com/iandiv/AppGroup/releases).

## 常见问题

## FAQ

### 如何在 Windows 任务栏上给应用分组？

在 AppGroup 中创建一个分组，加入你的应用，然后把分组图标拖到任务栏上。点击已固定的快捷方式会打开一个弹窗，你可以从中启动组内的任意应用。

### How do I group apps on the Windows taskbar?

Create a group in AppGroup, add your apps, then drag the group icon onto the taskbar. Clicking the pinned shortcut opens a popup where you can launch any app in the group.

### AppGroup 看起来像 Windows 11 的原生功能吗？

是的。这是它的核心设计目标。弹窗基于 WinUI 3 构建，并按开始菜单、快速设置等 Windows 11 原生界面的风格来设计。

### Does AppGroup look like it belongs in Windows 11?

Yes. That's a core design goal. The popup is built with WinUI 3 and styled to match native Windows 11 surfaces such as the Start menu and Quick Settings.

### AppGroup 会修改 Windows 任务栏吗？

不会。AppGroup 只是创建一个由你固定的分组快捷方式。任务栏仍由 Windows 管理；AppGroup 只负责弹窗和分组内容。

### Does AppGroup modify the Windows taskbar?

No. AppGroup creates a group shortcut that you pin. Windows still manages the taskbar; AppGroup provides the popup and group contents.

### AppGroup 免费吗？

是的。它基于 [MIT 许可证](LICENSE)免费开源。

### Is AppGroup free?

Yes. It is free and open source under the [MIT License](LICENSE).

### 支持哪些 Windows 版本？

Windows 10 版本 1809（内部版本 17763）或更高，包括 Windows 11。具体版本的要求请查看[发行说明](https://github.com/iandiv/AppGroup/releases)。

### Which Windows versions are supported?

Windows 10 version 1809 (build 17763) or later, including Windows 11. Check the [release notes](https://github.com/iandiv/AppGroup/releases) for requirements specific to a release.

### 可以添加"计算器""设置"这类 UWP / Microsoft Store 应用吗？

可以。从开始菜单或 `shell:AppsFolder` 把它们拖进分组即可。

### Can I add UWP / Microsoft Store apps like Calculator or Settings?

Yes. Drag them from the Start Menu or `shell:AppsFolder` into a group.

### 可以一次性启动分组内的所有应用吗？

可以，使用跳转列表中的**全部启动**操作（可在设置中开关）。

### Can I launch all apps in a group at once?

Yes, with the **Launch All** jumplist action (it can be toggled in Settings).

### 可以把一个分组放进另一个分组吗？

可以。把已有分组作为条目加入另一个分组；它会以独立的弹窗打开。

### Can I put one group inside another?

Yes. Add an existing group as an item in another group; it opens as its own popup.

### 可以从 TaskbarGroups 迁移吗？

可以。参见[从 TaskbarGroups 或 TaskbarGroupsFluent 迁移](#coming-from-taskbargroups-or-taskbargroupsfluent)。

### Can I migrate from TaskbarGroups?

Yes. See [Coming from TaskbarGroups or TaskbarGroupsFluent?](#coming-from-taskbargroups-or-taskbargroupsfluent).

### 如何备份或分享我的分组？

把它们导出为 `.agz`（AppGroupZip）文件，然后在另一台电脑上导入。

### How do I back up or share my groups?

Export them as an `.agz` (AppGroupZip) file and import it on another PC.

### 有便携版吗？

有：便携版和便携版（内置运行时）的 `.zip` 构建。自 v1.6.0 起，它们把分组保存在自己的文件夹中。

### Is there a portable version?

Yes: Portable and Portable (Bundled) `.zip` builds. Since v1.6.0 they keep groups in their own folder.

### AppGroup 安全吗？

源代码是公开的，v1.6.0 版本的文件在扫描时没有任何厂商在 VirusTotal 上报毒。但这只是针对这些具体文件的快照，并非安全性的证明。参见[安全性与 VirusTotal 报告](#security-and-virustotal-reports)，并只从本仓库下载。

### Is AppGroup safe?

The source code is public, and the v1.6.0 release files had no vendor detections on VirusTotal when scanned. That is a snapshot for those specific files, not proof of safety. See [Security and VirusTotal reports](#security-and-virustotal-reports), and download only from this repository.

## 帮助与支持

## Help and support

- **报告 Bug 或功能建议：** [GitHub Issues](https://github.com/iandiv/AppGroup/issues)
- **提问与讨论：** [GitHub Discussions](https://github.com/iandiv/AppGroup/discussions)
- **每个版本的变化：** [发行说明](https://github.com/iandiv/AppGroup/releases)
- **支持开发：** [在 Ko-fi 上捐赠](https://ko-fi.com/iandiv/tip)。给仓库点个 Star 也能帮助更多人发现它。⭐

- **Bug or feature request:** [GitHub Issues](https://github.com/iandiv/AppGroup/issues)
- **Questions and discussion:** [GitHub Discussions](https://github.com/iandiv/AppGroup/discussions)
- **What changed in each version:** [Release notes](https://github.com/iandiv/AppGroup/releases)
- **Support development:** [Donate on Ko-fi](https://ko-fi.com/iandiv/tip). Starring the repo also helps others find it. ⭐

<a href="https://ko-fi.com/iandiv/tip" target="_blank">
  <img src="https://github.com/user-attachments/assets/2e1376d4-d3a5-4ac4-95fc-e5aa512a1704" width="400" alt="Support AppGroup on Ko-fi">
</a>

## 许可证

## License

MIT。参见 [LICENSE](LICENSE)。

MIT. See [LICENSE](LICENSE).
