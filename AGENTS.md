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

每一轮改动在**提交推送 Git 之前**，必须依次完成以下步骤：

1. **递增版本号**：修改 `Nag0mi.json` 的 `version`，末位递增（如 `1.1` → `1.2`）。Release 标签格式为 `V{version}`（如 `V1.2`）。
2. **更新更新日志**：在 `CHANGELOG.md` 顶部添加新版本条目，包含版本号、日期与本次改动摘要。
3. **编译**：`dotnet build Nag0mi.csproj -c Release`。输出目录为 csproj 中 `OutputPath` 指定的宿主 ACR 目录。
4. **打包**：从输出目录的上级目录将 `Nag0mi/` 文件夹打包为仓库根目录的 `Nag0mi.zip`，结构与现有 zip 一致（`Nag0mi/Nag0mi.dll`、`Nag0mi/Nag0mi.deps.json`、`Nag0mi/UI/*.png`，排除 `*.pdb`）。Git Bash 示例：
   ```bash
   dotnet build Nag0mi.csproj -c Release
   cd /c/Users/ASUS/AppData/Roaming/XIVLauncherCN/pluginConfigs/PromeRotation/ACR
   zip -r /c/Users/ASUS/Documents/GitHub/Nag0miACR/Nag0mi.zip Nag0mi -x "*.pdb"
   ```
5. **更新 `Nag0mi.json`**：
   - `sha256` 改为新 `Nag0mi.zip` 的哈希（`sha256sum Nag0mi.zip`，仅取哈希值）；
   - `downloadUrl` 改为 `https://github.com/eroubue/Nag0miACR/releases/download/V{version}/Nag0mi.zip`。
6. **提交并推送**：将代码、`Nag0mi.json`、`CHANGELOG.md`、`Nag0mi.zip` 一并提交推送。
7. **发布 GitHub Release**：标签 `V{version}`，标题同标签，上传**与 sha256 对应的同一份** `Nag0mi.zip`，Release notes 使用 `CHANGELOG.md` 中本版本条目。优先使用 `gh release create V{version} Nag0mi.zip --title "V{version}" --notes "<条目>"`；无 `gh` 时在 GitHub 网页发布。

**顺序约束**：必须先完成代码修改再编译打包；`sha256` 必须对应最终上传 Release 的那一份 zip；zip 内容变更后必须重新计算哈希。

## 5. 需求与参考查询

- 需求相关源码与 PR 直接查阅本地仓库：`C:\Users\ASUS\Documents\GitHub\PromeRotation`。
- 该仓库仅作为需求与行为参考；其名称不得出现在本仓库的注释、提交信息或代码标识符中。
