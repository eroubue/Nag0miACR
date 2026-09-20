// Portions Copyright (c) Eros001377, MIT License. Ported from ErosUI.
using PromeRotation.Data;

namespace Nag0mi.Common.Data;

// 一条自定义热键配置（Nag0miUISettings.CustomHotkeys 的元素，随职业设置 JSON 落盘）。
// Name 是面板显示名兼排序/显隐/ImGui ID 的键，必须唯一（生成时经 CustomHotkeyTargets.GenerateUniqueName 去重）。
public sealed class CustomHotkeyEntry
{
    public string Name = "";
    public uint SkillId;
    public ActionType Type = ActionType.OffGcd;   // 宿主技能类型枚举（Gcd/OffGcd）
    public CustomHotkeyTarget Target = CustomHotkeyTarget.Self;
}
