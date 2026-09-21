#!/usr/bin/env bash
# 发布脚本：版本递增、同步代码内版本号、编译、打包、更新 Nag0mi.json 与 CHANGELOG.md、提交推送、发布 Release
# 用法: ./release.sh -m "改动摘要" [-m "另一条摘要"] [-v 1.2]
#   -m  改动摘要（可多次，至少一条）。前缀 "+ " 记为新增、"- " 记为移除/修复、无前缀为说明；
#       同步写入 AcrChangelog.cs 条目与 CHANGELOG.md，并作为提交正文与 Release notes
#   -v  显式指定版本号；缺省在 Nag0mi.json 当前版本末位递增（1.1 -> 1.2）
#   版本号同步位置：Nag0mi.json、GunbreakerRotation.cs（RotationMetadata）、AcrChangelog.cs（Version 与条目）
set -euo pipefail
cd "$(dirname "$0")"
REPO_WIN=$(pwd -W)

VERSION=""
NOTES=()
while [ $# -gt 0 ]; do
  case "$1" in
    -m|--note)    NOTES+=("$2"); shift 2;;
    -v|--version) VERSION="$2"; shift 2;;
    -h|--help)    sed -n '2,7p' "$0"; exit 0;;
    *) echo "未知参数: $1" >&2; exit 1;;
  esac
done

if [ ${#NOTES[@]} -eq 0 ]; then
  echo "错误: 至少提供一条 -m \"改动摘要\"" >&2
  exit 1
fi

# 剥离 +/- 前缀，得到 CHANGELOG / 提交正文 / Release notes 用的纯文本
STRIPPED=()
for n in "${NOTES[@]}"; do
  case "$n" in
    "+ "*|"- "*) STRIPPED+=("${n:2}");;
    *) STRIPPED+=("$n");;
  esac
done
printf -v BULLETS -- '- %s\n' "${STRIPPED[@]}"

# 1. 计算新版本号
CUR=$(sed -n 's/^\s*"version": "\([^"]*\)".*/\1/p' Nag0mi.json | head -1)
[ -n "$CUR" ] || { echo "错误: 无法从 Nag0mi.json 读取 version" >&2; exit 1; }
if [ -z "$VERSION" ]; then
  IFS='.' read -ra PARTS <<< "$CUR"
  LAST=$((${#PARTS[@]} - 1))
  [[ ${PARTS[$LAST]} =~ ^[0-9]+$ ]] || { echo "错误: 版本末位非数字（$CUR），请用 -v 显式指定" >&2; exit 1; }
  PARTS[$LAST]=$(( ${PARTS[$LAST]} + 1 ))
  VERSION=$(IFS='.'; echo "${PARTS[*]}")
fi
TAG="V$VERSION"
DATE=$(date +%F)
echo "==> 版本: $CUR -> $VERSION ($TAG)"

# 2. 同步代码内版本号：GunbreakerRotation.cs 元数据 + AcrChangelog.cs（Version 与条目）
python - "$VERSION" "$DATE" "${NOTES[@]}" <<'PY'
import re, sys

ver, date, notes = sys.argv[1], sys.argv[2], sys.argv[3:]

def esc(s):
    return s.replace("\\", "\\\\").replace('"', '\\"')

def edit(path, fn):
    raw = open(path, encoding="utf-8", newline="").read()
    crlf = "\r\n" in raw
    out = fn(raw.replace("\r\n", "\n"))
    open(path, "w", encoding="utf-8", newline="").write(out.replace("\n", "\r\n") if crlf else out)

# RotationMetadata 第 4 个实参为版本号
def bump_meta(text):
    new, n = re.subn(
        r'(\[RotationMetadata\(\(uint\)Job\.GNB, "绝枪战士", "Nag0mi", )"[^"]*"',
        lambda m: m.group(1) + f'"{ver}"', text)
    if n != 1:
        sys.exit("GunbreakerRotation.cs 版本号替换失败")
    return new
edit("Gunbreaker/GunbreakerRotation.cs", bump_meta)

# AcrChangelog.cs：Version 常量更新 + Entries 顶部插入新条目（最新版本在最前）
lines = []
for note in notes:
    if note.startswith("+ "):   kind, txt = "Add", note[2:]
    elif note.startswith("- "): kind, txt = "Remove", note[2:]
    else:                       kind, txt = "Note", note
    lines.append(f'            new(LineKind.{kind}, "{esc(txt)}"),')
entry = (f'        new("{date}", "{ver}",\n'
         f'        [\n' + "\n".join(lines) + '\n        ]),\n')

def bump_changelog(text):
    text, n = re.subn(r'public const string Version = "[^"]*";',
                      f'public const string Version = "{ver}";', text)
    if n != 1:
        sys.exit("AcrChangelog.cs Version 常量替换失败")
    m = re.search(r'public static readonly Entry\[\] Entries =\n    \[\n', text)
    if not m:
        sys.exit("AcrChangelog.cs Entries 数组定位失败")
    return text[:m.end()] + entry + text[m.end():]
edit("Common/Data/AcrChangelog.cs", bump_changelog)

print(f"code version synced: {ver} (+{len(notes)} changelog lines)")
PY

# 3. 编译
echo "==> 编译 (Release)"
dotnet build Nag0mi.csproj -c Release

# 4. 打包：输出目录（...\ACR\Nag0mi）整体打为 Nag0mi/ 根结构 zip，排除 *.pdb
OUT_PATH=$(sed -n 's:.*<OutputPath>\(.*\)</OutputPath>.*:\1:p' Nag0mi.csproj | head -1 | tr -d '\r')
[ -n "$OUT_PATH" ] || { echo "错误: 无法从 Nag0mi.csproj 读取 OutputPath" >&2; exit 1; }
rm -f Nag0mi.zip
echo "==> 打包 $OUT_PATH -> Nag0mi.zip"
python - "$OUT_PATH" "$REPO_WIN/Nag0mi.zip" <<'PY'
import os, sys, zipfile
src, dst = sys.argv[1], sys.argv[2]
if not os.path.isdir(src):
    sys.exit(f"输出目录不存在: {src}")
base = os.path.dirname(src)  # 使 zip 内路径以 Nag0mi/ 为根
n = 0
with zipfile.ZipFile(dst, "w", zipfile.ZIP_DEFLATED) as z:
    for dirpath, _, files in os.walk(src):
        for name in sorted(files):
            if name.lower().endswith(".pdb"):
                continue
            p = os.path.join(dirpath, name)
            z.write(p, os.path.relpath(p, base).replace(os.sep, "/"))
            n += 1
print(f"packed {n} files -> {dst}")
PY

# 5. 更新 Nag0mi.json：version / downloadUrl / sha256
SHA=$(sha256sum Nag0mi.zip | cut -d' ' -f1)
URL="https://github.com/eroubue/Nag0miACR/releases/download/$TAG/Nag0mi.zip"
sed -i \
  -e "s#^\(\s*\)\"version\": \"[^\"]*\"#\1\"version\": \"$VERSION\"#" \
  -e "s#^\(\s*\)\"downloadUrl\": \"[^\"]*\"#\1\"downloadUrl\": \"$URL\"#" \
  -e "s#^\(\s*\)\"sha256\": \"[^\"]*\"#\1\"sha256\": \"$SHA\"#" \
  Nag0mi.json
grep -q "\"version\": \"$VERSION\"" Nag0mi.json || { echo "错误: version 写入失败" >&2; exit 1; }
grep -q "\"sha256\": \"$SHA\"" Nag0mi.json || { echo "错误: sha256 写入失败" >&2; exit 1; }
echo "==> Nag0mi.json 已更新 (sha256=$SHA)"

# 6. 更新 CHANGELOG.md：新条目插入首个版本标题之前
printf -v ENTRY '## %s（%s）\n\n%s' "$VERSION" "$DATE" "$BULLETS"
python - "$ENTRY" <<'PY'
import re, sys
entry = sys.argv[1]
path = "CHANGELOG.md"
text = open(path, encoding="utf-8").read()
m = re.search(r"(?m)^## ", text)
new = (text[:m.start()] + entry + "\n" + text[m.start():]) if m \
    else (text.rstrip("\n") + "\n\n" + entry)
open(path, "w", encoding="utf-8", newline="\n").write(new)
PY
echo "==> CHANGELOG.md 已添加 $VERSION 条目"

# 7. 提交并推送
echo "==> 提交并推送"
git add -A
git commit -m "发布 $TAG" -m "$BULLETS"
git push

# 8. 发布 GitHub Release（sha256 对应的同一份 zip）
if command -v gh >/dev/null 2>&1; then
  echo "==> 发布 Release $TAG"
  gh release create "$TAG" Nag0mi.zip --title "$TAG" --notes "$BULLETS"
  echo "==> 完成: $TAG 已发布"
else
  cat <<EOF
==> 未检测到 gh CLI，请手动发布 Release：
    地址:  https://github.com/eroubue/Nag0miACR/releases/new
    标签:  $TAG（基于 main 最新提交新建）
    标题:  $TAG
    附件:  $REPO_WIN/Nag0mi.zip
    备注:
$BULLETS
EOF
fi
