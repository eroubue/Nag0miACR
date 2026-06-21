namespace Nag0mi.Gunbreaker.Data;

public sealed class GunbreakerSettings
{
    public static GunbreakerSettings Instance { get; } = new();
    public enum 工作模式枚举
    {
        高难模式 = 0,  // 专注输出，不包含自动减伤、拉怪等辅助功能
        日常模式 = 1   // 包含减伤、拉怪、盾姿管理等辅助功能
    }
    public enum 起手方式枚举
    {
        关闭 = 0,
        突进 = 1,  // Lv54+
        闪雷弹 = 2       // Lv15+
    }
    public enum 起手选择枚举
    {
        妖星起手 = 0,
        无情2g起手 = 1
    }

    public bool debug { get; set; }
    public bool ST = true;
    public 起手选择枚举 起手选择 = 起手选择枚举.妖星起手;

    public int AOE数 { get; set; } = 2;

    public int 保留子弹数 { get; set; }
    public 工作模式枚举 当前工作模式 = 工作模式枚举.高难模式;
    // ========== 起手设置 ==========
    #region 起手设置
    public 起手方式枚举 开怪方式 = 起手方式枚举.关闭;
    public int 开怪提前时间 = 500;  // 毫秒
    #endregion

    
}
