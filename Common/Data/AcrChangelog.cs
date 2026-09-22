namespace Nag0mi.Common.Data;

// ACR 更新日志数据：最新版本在最前。版本号递增规则——每轮改动提升一次版本号，
// 并在此处追加对应条目（日期 / 版本 / 内容）。Version 与清单文件中的 version 保持一致。
public static class AcrChangelog
{
    public const string Version = "1.14";

    public enum LineKind { Add, Remove, Note }

    public readonly record struct Line(LineKind Kind, string Text);
    public readonly record struct Entry(string Date, string Version, Line[] Lines);

    public static readonly Entry[] Entries =
    [
        new("2026-09-22", "1.14",
        [
            new(LineKind.Remove, "修复设置悬浮窗 QT 列表图标不显示 marker 角标"),
            new(LineKind.Note, "QT 列表图标尺寸跟随 QT 面板缩放; QT 面板 100% 基准放大至旧版 120%"),
            new(LineKind.Add, "热键显隐改为图标+首字角标（跟随热键面板尺寸, 复选框同步缩放, 自定义热键带目标角标）"),
        ]),
        new("2026-09-22", "1.13",
        [
            new(LineKind.Remove, "修复背景透明度非真实不透明：宣纸贴图自带约75%透明度（冷宣），滑杆拉满仍透出游戏画面；明色模式纸底下垫冷宣实色底，滑杆值即真实不透明度"),
            new(LineKind.Note, "三窗口背景不透明度默认值改为不透明（1.0）"),
        ]),
        new("2026-09-22", "1.12",
        [
            new(LineKind.Add, "设置窗侧边栏重组：面板控制独立分组（含个性化），基础设置分组含循环设置/热键自定义"),
            new(LineKind.Add, "三窗口背景透明度逐窗可调（个性化→窗口背景）"),
            new(LineKind.Remove, "QT 显隐/默认值合并为图标双复选框列表；删除模式切换行并精简说明文字"),
        ]),
        new("2026-09-22", "1.12",
        [
            new(LineKind.Add, "时间轴「使用爆发药」行为接入本 ACR 爆发药 QT 门禁：QT 未开启时不执行并输出日志「爆发药QT未开启」"),
        ]),
        new("2026-09-22", "1.12",
        [
            new(LineKind.Add, "新增时间轴行为「绝枪/5m圆环绘制」：以玩家为圆心绘制/关闭内径4.9m、外径5.0m的圆环"),
            new(LineKind.Add, "新增时间轴条件「绝枪/检测5m有效AOE敌数」：5m内实际敌数减去将死敌数后按比较符判断"),
        ]),
        new("2026-09-22", "1.11",
        [
            new(LineKind.Remove, "修复侧边栏页签文字仍被裁剪：改为隐形按钮承载交互 + AddText 手动绘制文字（Button 会把文字裁剪到按钮矩形内），侧栏宽度跟随最宽文字自适应（修「面板控制」的「制」被裁）"),
        ]),
        new("2026-09-22", "1.10",
        [
            new(LineKind.Remove, "修复设置窗口侧边栏页签文字顶部被裁剪：行高从固定 28px 改为跟随书法字体实际字号（22px 字体超出 28px 行高被按钮裁剪）"),
            new(LineKind.Remove, "移除「开发用」页签的 Solver 状态区块"),
        ]),
        new("2026-09-22", "1.9",
        [
            new(LineKind.Remove, "删除设置窗口侧边栏顶部的标题栏（Nag0mi 绝枪战士设置文字节点），窗口拖动改为按住侧边栏页签下方的空白区"),
        ]),
        new("2026-09-22", "1.8",
        [
            new(LineKind.Remove, "修复右键拖拽排序时瓦片可拖出窗口外的问题：拖拽源格位置钳制在网格范围内（QT面板/热键面板）"),
        ]),
        new("2026-09-22", "1.7",
        [
            new(LineKind.Add, "设置窗口改为无标题栏：标题挪进侧边栏顶部节点（兼作拖动把手），「面板控制」「基础设置」改为侧边栏基础设置分组下的两个子栏，分组标题可点击跳转"),
            new(LineKind.Add, "笔触边框加粗：外壳边框 8→12px，QT/热键悬浮窗内边距加大到 16px 给笔触留空间，瓦片状态描边 4-5→6-7px"),
            new(LineKind.Remove, "修复设置窗口经控制条打开后无法拖动（PositionCondition.Always 每帧钉死位置，改为 Once）"),
        ]),
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
