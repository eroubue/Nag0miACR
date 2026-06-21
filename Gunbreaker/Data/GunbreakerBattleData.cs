namespace Nag0mi.Gunbreaker.Data;

public class GunbreakerBattleData
{
    public static GunbreakerBattleData Instance { get; set; } = new();
    public HashSet<uint> ShotBlackList { get; } = new();
   

  

    public void Reset()
    {
        ShotBlackList.Clear();
    }
}