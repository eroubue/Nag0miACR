using Dalamud.Bindings.ImGui;
using Nag0mi.Gunbreaker.Data;
using PromeRotation.Timeline.Core;

namespace Nag0mi.Gunbreaker.Timeline;

/// <summary>
/// 绝枪/闪雷弹黑名单管理：添加 / 移除不希望对其使用闪雷弹的目标 DataId。
/// 使用自定义节点编辑器（DrawJobNodeEditor）复刻参考实现的增删列表。
/// </summary>
public sealed class GunbreakerShotBlacklistAction : IAction, ISerializableAction, IJobNodeDescriptor
{
    internal const string TypeKey = "gnbshotblacklist";

    private const string IdsKey = "ids";

    private readonly HashSet<uint> _blacklist = new();
    private string _newIdInput = string.Empty;

    public string NodeDisplayName => "绝枪/闪雷弹黑名单管理";

    // 自定义编辑器接管绘制，通用参数渲染会被跳过。
    public NodeParamInfo[] Params => Array.Empty<NodeParamInfo>();

    public string GetParam(string fieldName) => "";

    public void SetParam(string fieldName, string value)
    {
    }

    public bool DrawJobNodeEditor()
    {
        ImGui.Text("添加或移除不希望对其闪雷弹的目标 DataId：");

        ImGui.SetNextItemWidth(160f);
        ImGui.InputText("添加ID", ref _newIdInput, 20);
        ImGui.SameLine();
        if (ImGui.Button("添加"))
        {
            if (uint.TryParse(_newIdInput.Trim(), out var id))
            {
                _blacklist.Add(id);
                _newIdInput = string.Empty;
            }
        }

        ImGui.Separator();
        ImGui.Text("当前黑名单：");

        if (_blacklist.Count == 0)
        {
            ImGui.TextDisabled("（空）");
        }
        else
        {
            uint? toRemove = null;
            foreach (var id in _blacklist)
            {
                ImGui.Text($"ID: {id}");
                ImGui.SameLine();
                if (ImGui.Button($"移除##{id}"))
                    toRemove = id;
            }

            if (toRemove.HasValue)
                _blacklist.Remove(toRemove.Value);
        }

        ImGui.Separator();
        if (ImGui.Button("清空所有黑名单"))
            _blacklist.Clear();

        return true;
    }

    public void Execute()
    {
        var target = GunbreakerBattleData.Instance.ShotBlackList;
        target.Clear();
        foreach (var id in _blacklist)
            target.Add(id);
    }

    public ActionDto ToDto() => new()
    {
        Type = TypeKey,
        Params = new Dictionary<string, string> { [IdsKey] = string.Join(",", _blacklist) },
    };

    public static void Register(RotationNodeContext context)
    {
        ActionFactory.Register(context, TypeKey, FromDto);
    }

    private static GunbreakerShotBlacklistAction FromDto(ActionDto dto)
    {
        var action = new GunbreakerShotBlacklistAction();
        if (dto.Params == null || !dto.Params.TryGetValue(IdsKey, out var ids) || string.IsNullOrWhiteSpace(ids))
            return action;

        foreach (var part in ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (uint.TryParse(part, out var id))
                action._blacklist.Add(id);
        }

        return action;
    }
}
