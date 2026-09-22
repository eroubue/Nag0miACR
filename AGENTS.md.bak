# AGENTS.md — 项目规范

本文件约束所有在本仓库中工作的代理（Agent）与开发者，必须严格遵守。

## 1. 注释规范

- **禁止**在注释（包括 XML 文档注释、单行/多行注释）中出现"反编译"、"decompile"、"decompiled"等字样。
- **禁止**在注释中出现任何外部参考项目的名称（例如 PromeRotation 等）。需要说明来源时，使用中性描述（如"参考实现"、"上游逻辑"），不得点名。

## 2. 代码规范

- **禁止**通过反射（Reflection）引用、加载或调用外部程序集/外部项目的代码。所有依赖必须显式引用。

## 3. 工作流规范

- **每一轮改动完成后必须提交并推送 Git**：完成一组修改后，执行 `git add`、`git commit`、`git push`，不得遗留未提交的改动。

## 4. 版本与发布流程

每一轮改动在**提交推送 Git 之前**，执行发布脚本：

```bash
./release.sh -m "改动摘要" [-m "另一条摘要"] [-v 1.2]
```

- `-m`：本次改动摘要（可多次，至少一条）。前缀 `"+ "` 记为新增、`"- "` 记为移除/修复、无前缀为说明；写入 `AcrChangelog.cs` 条目与 `CHANGELOG.md`，并作为提交正文与 Release notes。
- `-v`：显式指定版本号；缺省在 `Nag0mi.json` 当前版本末位递增（如 `1.1` → `1.2`）。Release 标签格式为 `V{version}`。

**版本号同步位置**（脚本自动完成，手动改动时也必须同步）：

1. `Nag0mi.json` 的 `version`（清单）；
2. `Gunbreaker/GunbreakerRotation.cs` 中 `RotationMetadata` 的第 4 个实参（宿主识别的 ACR 版本）；
3. `Common/Data/AcrChangelog.cs` 的 `Version` 常量，并在 `Entries` 顶部追加本版本条目（最新版本在最前，设置页「更新日志」页签显示）。

脚本依次完成：计算新版本号 → 同步代码内版本号（`GunbreakerRotation.cs` 与 `AcrChangelog.cs`）→ `dotnet build Nag0mi.csproj -c Release` → 将编译输出目录打包为 `Nag0mi.zip`（`Nag0mi/` 根结构，含 dll/deps.json/UI，排除 `*.pdb`）→ 更新 `Nag0mi.json` 的 `version`/`downloadUrl`/`sha256` → 在 `CHANGELOG.md` 顶部插入本版本条目 → 提交并推送 → 发布 GitHub Release（无 `gh` 时打印手动发布所需参数）。

**约束**：`sha256` 对应最终上传 Release 的同一份 zip；脚本中断时按上述顺序手动补齐剩余步骤。

## 5. 需求与参考查询

- 需求相关源码与 PR 直接查阅本地仓库：`C:\Users\ASUS\Documents\GitHub\PromeRotation`。
- 该仓库仅作为需求与行为参考；其名称不得出现在本仓库的注释、提交信息或代码标识符中。
