namespace Nag0mi.Common.Data;

// ACR 更新日志数据：最新版本在最前。版本号递增规则——每轮改动提升一次版本号，
// 并在此处追加对应条目（日期 / 版本 / 内容）。Version 与清单文件中的 version 保持一致。
public static class AcrChangelog
{
    public const string Version = "1.1";

    public enum LineKind { Add, Remove, Note }

    public readonly record struct Line(LineKind Kind, string Text);
    public readonly record struct Entry(string Date, string Version, Line[] Lines);

    public static readonly Entry[] Entries =
    [
        new("2026-09-20", "1.1",
        [
            new(LineKind.Add, "设置页新增「更新日志」页签"),
            new(LineKind.Add, "引入版本号递增机制，版本与更新日志统一维护"),
            new(LineKind.Add, "新增项目规范文件（注释/反射/提交工作流约定）"),
        ]),
        new("2026-09-19", "1.0",
        [
            new(LineKind.Add, "绝枪战士 ACR 首个版本"),
            new(LineKind.Note, "日随 / 高难 / 自定义 三模式循环配置"),
        ]),
    ];
}
