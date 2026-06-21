using PromeRotation.Data;

namespace Nag0mi.Gunbreaker.Data;

public class GunbreakerQT
{
    public const string 启用起手 = "启用起手";
    public const string 停手 = "停手";
    public const string 爆发药= "爆发药";
    public const string 爆发 = "爆发";
    public const string 突进起手 = "突进起手";
    public const string 无情 = "无情";
    public const string 子弹连 = "子弹连";
    public const string 倾泻爆发 = "倾泻爆发";
    public const string 领域 = "领域";
    public const string 弓形 = "弓形";
    public const string AOE = "AOE";
    public const string dot = "dot";
    public const string 音速破 = "音速破";
    public const string 血壤 = "血壤";
    public const string 爆发击 = "爆发击";
    public const string 狮心连 = "狮心连";
    public const string 倍攻 = "倍攻";
    public const string 闪雷弹 = "闪雷弹";
    public const string 命运之环 = "命运之环";
    public const string 仅使用爆发击卸除子弹 = "仅使用爆发击卸除子弹";
    public const string 小于3目标时不用弓形 = "小于3目标时不用弓形";
    public const string 弓形冲波允许错开无情 = "弓形冲波允许错开无情";
    public const string 优先音速破 = "优先音速破";
    public const string 落地无情 = "落地无情";
    public const string 强制盾姿 = "强制盾姿";
    public const string 自动拉怪 = "自动拉怪";
    public const string 自动减伤 = "自动减伤";
    public const string 优先狮心连 = "优先狮心连";
    public const string 血壤不延后 = "血壤不延后";
}
public static class QT
{
    public static bool QTGET(string qtName) => PromeSettings.Instance.GetQt(qtName);

    public static void QTSET(string qtName, bool qtValue) =>
        PromeSettings.Instance.SetQt(qtName, qtValue);
}
