namespace Nag0mi.Common.Data;

// ACR 更新日志数据：最新版本在最前。版本号递增规则——每轮改动提升一次版本号，
// 并在此处追加对应条目（日期 / 版本 / 内容）。Version 与清单文件中的 version 保持一致。
public static class AcrChangelog
{
    public const string Version = "1.6";

    public enum LineKind { Add, Remove, Note }

    public readonly record struct Line(LineKind Kind, string Text);
    public readonly record struct Entry(string Date, string Version, Line[] Lines);

    public static readonly Entry[] Entries =
    [
        new("2026-09-22", "1.6",
        [
            new(LineKind.Remove, "修复明色模式瓦片状态描边染色不显示（状态色改经白笔触贴图乘法染色）"),
            new(LineKind.Remove, "修复笔触边框过淡几乎不可见（叠画两遍压实描边）"),
            new(LineKind.Remove, "修复 QT/热键面板悬浮提示墨字叠深底不可读（改白字 + 近黑实底）"),
            new(LineKind.Note, "设置面板灰色字体明色模式统一改黑（辅助/禁用文字与硬编码灰字）"),
            new(LineKind.Remove, "修复控制条自动攻击关闭图标在深墨锭底上不可辨（改压暗缟羽）"),
        ]),
        new("2026-09-21", "1.5",
        [
            new(LineKind.Add, "水墨主题：全部界面改为宣纸底/笔触边框/血红主色，支持明(冷宣)/暗(暖宣)手动切换（设置页「基础设置」勾选暗色模式，即改即存、下一帧全窗口生效）"),
            new(LineKind.Add, "设置窗：山水装饰层、侧边栏激活项血红竖条+红字、手绘笔触勾选框、Ma Shan Zheng 书法标题字体（随包内置 GB2312 一级子集, SIL OFL）"),
            new(LineKind.Add, "QT/热键面板：瓦片底铺宣纸，状态描边改为笔触边框按状态色染色（启用青/关闭血红）"),
            new(LineKind.Note, "控制条：墨锭形态（京元深色实体底+缟羽图标），不铺宣纸"),
            new(LineKind.Remove, "移除热键面板贴图缺失的文字角标降级分支（贴图随包内置恒存在）"),
        ]),
        new("2026-09-21", "1.4",
        [
            new(LineKind.Add, "设置窗口新增「个性化」页：设置窗口/QT面板/热键面板可分别启用自定义字体（系统/游戏/字体文件）与自定义背景图（含透明度）"),
            new(LineKind.Add, "基础设置页新增锁定 QT 面板位置与锁定热键面板位置开关（防战斗误移）"),
        ]),
        new("2026-09-21", "1.3",
        [
            new(LineKind.Note, "冷却计时秒数移到格子左下角"),
            new(LineKind.Remove, "修复冷却倒计时圈方向：弧随剩余时间从顶部顺时针消减"),
            new(LineKind.Add, "冷却中整格加 45% 压暗遮罩，倒计时状态更明显"),
        ]),
        new("2026-09-21", "1.2",
        [
            new(LineKind.Note, "热键角标尺寸/透明度调整：充能角标右下角占格子 1/16 面积"),
            new(LineKind.Note, "数字/职能角标左上角占约 1/3 面积，以 70% 不透明度绘制"),
        ]),
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
