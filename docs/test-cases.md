# SrvDesk 完整测试用例

覆盖功能、性能、易用性、异常。自动化入口：`scripts/full-test.ps1`（四轮）。  
手工项标「手工」；其余由脚本反射 / CLI / 读系统状态执行。  
**本套用例不向真实系统批量写入优化项**（避免改坏本机）；写路径只测差量为 0、配置导入导出、hosts 解析、还原点失败可跳过。

约定：

| 前缀 | 类型 |
|------|------|
| F | 功能 |
| P | 性能 |
| U | 易用性 |
| E | 异常 |

---

## 一、功能测试

### F-01 程序启动与版本

| ID | 步骤 | 期望 |
|----|------|------|
| F-01.1 | 编译 Release | 成功，`bin\Release\net48\SrvDesk.exe` 存在 |
| F-01.2 | 读程序集版本 | 与 csproj `<Version>` 一致（三位） |
| F-01.3 | 构造 `MainForm`（不 Show） | 不抛异常；标题含产品名与版本 |
| F-01.4 | CLI `--help` | 进程退出码 0 |

### F-02 设置目录与脚本配方

| ID | 步骤 | 期望 |
|----|------|------|
| F-02.1 | 枚举 `SettingCatalog` 全部 `SettingHelpInfo` | 每条 Summary/Purpose/Benefit/Guide/Effect 非空 |
| F-02.2 | 对每条调用 `SettingRecipeCatalog.Get` | 均有配方（帮助面板可复制脚本） |
| F-02.3 | `Optimizer.State` 的 public bool 字段 | 除内部辅助字段外，在 Catalog 有对应帮助 |
| F-02.4 | 配方 `On`/`Off` 脚本 | 非空 |

### F-03 预设

| ID | 步骤 | 期望 |
|----|------|------|
| F-03.1 | `OptPresets.All` | 恰好 4 个：server-desktop / security / remote-work / minimal |
| F-03.2 | `Find` 大小写不敏感 | 能找到；未知 id 返回 null |
| F-03.3 | Server 桌面 `Build()` | 多数 bool 为 true；`EnableSearch=false`；UTC/HPET/F8/详细登录为 false |
| F-03.4 | 安全加固 | `DisableUac`/`DisableCad`/`RdpDisableNla`/`DisableVbs` 为 false |
| F-03.5 | 远程办公 | `EnableRdp`、`RdpGpuAccel`、`HighPerfPower` 为 true；`RdpDisableNla` 为 false |
| F-03.6 | 最小改动 | 仅少量 Server/账户项为 true，不把全部 bool 打开 |

### F-04 读系统状态

| ID | 步骤 | 期望 |
|----|------|------|
| F-04.1 | `Optimizer.Read(false)` | 返回非空 State，不抛 |
| F-04.2 | `Optimizer.Read(true)` | 返回非空 State（可较慢） |
| F-04.3 | `IsWindowsServer()` | 返回 bool，与本机 InstallationType 一致 |
| F-04.4 | 文件夹选项读入 | `ShowDriveLettersMode` 在 0–2；分组 0–4；排序 0–5 |
| F-04.5 | 任务栏搜索模式 | `TaskbarSearchMode` 为 -1 或 0–2 |

### F-05 差量应用（不改系统）

| ID | 步骤 | 期望 |
|----|------|------|
| F-05.1 | `Apply(当前, 当前)` | 错误列表为空；`LastApplyActionCount=0` |
| F-05.2 | 仅改一个内存字段再与基线比 `AnyChanged` | 对应 Tweaks 的 AnyChanged 为 true |

### F-06 配置导入导出

| ID | 步骤 | 期望 |
|----|------|------|
| F-06.1 | 导出当前状态到临时 JSON | 文件非空，含 Version=2 |
| F-06.2 | 再导入 | bool 开关与导出一致 |
| F-06.3 | 导出前把 `FolderGroupByMode`/`FolderSortByMode`/`ShowDriveLettersMode`/`TaskbarSearchMode` 设为非默认 | 导入后 int 字段保持 |
| F-06.4 | 导出含 `AutologonUser` | 可还原；**不得**写出 `AutologonPassword` |
| F-06.5 | 仅脚本覆盖、无开关的 JSON | 能加载，不抛「没有可导入的内容」 |

### F-07 还原点流程

| ID | 步骤 | 期望 |
|----|------|------|
| F-07.1 | `SystemRestoreHelper.IsPolicyDisabled()` | 返回 bool，不抛 |
| F-07.2 | `TryCreate("SrvDesk 测试")` | 返回 true（创建成功、策略跳过或本机未启用都算跳过，不中止应用） |
| F-07.3 | `UiPrefs.DisableRestorePointPrompt` 缺省 | 为 false（默认询问） |
| F-07.4 | 手工：应用到系统选「是」 | 日志出现创建/跳过说明；系统还原列表描述为「SrvDesk 应用前」 |

### F-08 社区 / Sophia / 文件夹选项

| ID | 步骤 | 期望 |
|----|------|------|
| F-08.1 | `CommunityTweaks.ReadInto` | 12 项字段可读 |
| F-08.2 | `SophiaGapTweaks.ReadInto` | 11 项字段可读 |
| F-08.3 | `FolderViewTweaks` 标签数组 | 分组 5、排序 6、盘符 3 |
| F-08.4 | 「运行不记历史」与「关闭应用启动跟踪」 | **不得共用同一注册表判定**（改一项不应迫使另一项同值） |

### F-09 常用软件 / 清理 / hosts

| ID | 步骤 | 期望 |
|----|------|------|
| F-09.1 | `CommonSoftwareCatalog.GetAll` | >20 项；含 AI：codex / codex-desktop / pi-agent |
| F-09.2 | `CleanupEngine.Items` | 非空；分组至少 3 个；无 WinSxS ResetBase |
| F-09.3 | `HostsFileHelper.Read` | 能读本机 hosts |
| F-09.4 | `ParseText` 合法行 | 解析出条目；垃圾行计入 SkippedLines |
| F-09.5 | `Validate` | 空 IP / 非法 IP / 空主机名有中文错误 |

### F-10 CLI

| ID | 步骤 | 期望 |
|----|------|------|
| F-10.1 | `--help` / `-h` | 退出 0 |
| F-10.2 | `--export-profile <tmp>` | 退出 0，文件存在（无需管理员） |
| F-10.3 | `--apply-preset` 缺参数 | 退出非 0 |
| F-10.4 | `--apply-preset nosuch` | 退出非 0 |
| F-10.5 | `--load-profile` 不存在的文件 | 退出非 0 |

### F-11 即时页与菜单（构造）

| ID | 步骤 | 期望 |
|----|------|------|
| F-11.1 | 构造即时对话框（系统信息、程序设置、hosts、清理等） | 不抛；标题非空 |
| F-11.2 | `AppMenuStrip` | 文件/预设/工具/视图/帮助均有条目 |
| F-11.3 | 快捷键 | 导入 Ctrl+O、导出 Ctrl+S、刷新 F5 |

### F-12 系统信息与 Autologon 只读

| ID | 步骤 | 期望 |
|----|------|------|
| F-12.1 | `SystemInfoHelper.Detect` | Summary 非空 |
| F-12.2 | `AutologonHelper.Read` | 返回对象，不抛 |

---

## 二、性能测试

阈值可按机器放宽；脚本记录耗时。本机参考（Windows Server / net48）：

| ID | 步骤 | 期望 |
|----|------|------|
| P-01 | `Optimizer.Read(false)` 连续 3 次取中位 | ≤ 4 s |
| P-02 | `Optimizer.Read(true)` 1 次 | ≤ 45 s（含 DISM 探测） |
| P-03 | `SettingRecipeCatalog` 全量 Get | ≤ 500 ms |
| P-04 | 构造 `MainForm` | ≤ 8 s |
| P-05 | `CommonSoftwareCatalog.GetAll` | ≤ 200 ms |
| P-06 | 配置导出+导入往返 | ≤ 1 s |
| P-07 | 连续 20 次 `Read(false)` 内存 | 不抛 OOM；后 10 次中位不明显劣化（< 3 倍首次） |
| P-08 | 手工：主窗切换全部左侧分类 | 无明显卡顿（目标 < 300 ms/页） |

---

## 三、易用性测试

| ID | 步骤 | 期望 |
|----|------|------|
| U-01 | 主窗最小尺寸 | ≥ 1180×720 |
| U-02 | 标题 | 含「SrvDesk」与版本号 |
| U-03 | 左侧分组名 | 性能及安全 / 桌面外观 / 资源管理器 / 远程与网络 / 电源与后台 / 隐私与体验 / Server专属 / 账户策略 |
| U-04 | 批量开关文案 | 无空标题；同一分组内标题不重复 |
| U-05 | 帮助文案 | 不含「TODO」「FIXME」「xxx」占位 |
| U-06 | 推荐等级 | 每条 Catalog 有 Recommend |
| U-07 | 预设下拉 | 标题人类可读，含「推荐」「安全」「远程」「最小」语义 |
| U-08 | 程序设置 | 含「还原点」询问开关文案 |
| U-09 | 首次说明 | `FirstRunNoticeDialog` 文案提示备份/还原点 |
| U-10 | CLI 帮助 | 中文；列出 apply-preset / load-profile / export-profile |
| U-11 | 菜单「帮助」 | 有变更日志、操作日志、免责、隐私、许可证、检查更新 |
| U-12 | 手工：搜索框输入「还原」 | 能筛到相关项或提示无匹配 |
| U-13 | 手工：隐藏不适用项 | Server 非桌面项可收起 |
| U-14 | 还原点查看指引 | 本工具不提供列表；应能通过 `rstrui` 查看（文档/帮助一致） |

---

## 四、异常测试

| ID | 步骤 | 期望 |
|----|------|------|
| E-01 | 导入不存在的配置 | 抛/退出，中文错误，不崩溃 |
| E-02 | 导入 `{}` 或空文件 | 提示无效或没有可导入内容 |
| E-03 | 导入非 JSON | 不崩溃 |
| E-04 | 导入只有未知 key 的开关 | 忽略未知项，其余默认 |
| E-05 | `UiPrefs` 读损坏 JSON | 回落默认，不抛 |
| E-06 | hosts `Validate` 非法输入 | 返回错误串，不抛 |
| E-07 | hosts `ParseText` 空串/乱码 | 不抛；Entries 可空 |
| E-08 | `SystemRestoreHelper.TryCreate` 在未启用还原时 | 返回 true + 说明消息，不抛 |
| E-09 | CLI 无参数 | 不走 CLI（GUI）；对本测试：直接 `Main` 无参会开窗，脚本只测有参 |
| E-10 | CLI `--export-profile` 缺路径 | 退出非 0 |
| E-11 | CLI `--load-profile` 目录当文件 | 退出非 0 |
| E-12 | 非管理员 `--apply-preset minimal` | 退出非 0，提示需要管理员（若当前已是管理员则跳过本条） |
| E-13 | `FolderViewTweaks` 越界 mode | Clamp 到合法范围，不抛 |
| E-14 | `OptPresets.Find(null/空)` | 不抛（返回 null 或等价） |
| E-15 | 构造对话框失败隔离 | 单个对话框构造失败记 FAIL，不影响其它 |
| E-16 | 手工：应用到系统点「取消」 | 不写入 |
| E-17 | 手工：无管理员启动 | 状态栏提示权限不足，不闪退 |

---

## 五、四轮执行策略

| 轮次 | 重点 | 脚本块 |
|------|------|--------|
| 第 1 轮 | 功能：目录/预设/读写/配置/CLI | Round 1 |
| 第 2 轮 | 性能计时 | Round 2 |
| 第 3 轮 | 易用性：文案、菜单、尺寸 | Round 3 |
| 第 4 轮 | 异常路径 + 回归第 1 轮关键断言 | Round 4 |

每轮结束输出 PASS/FAIL 清单。发现缺陷先修再进入下一轮；第 4 轮须全绿。
