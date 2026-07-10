using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "PerkDataSO", menuName = "Stat/PerkDataSO")]
public class PerkDataSO : ScriptableObject
{
    public int perkID;
    public string perkName;
    public Image perkIcon;
    public StatType targetStat;
    public float flatBonus;
    public float percentBonus;
}
