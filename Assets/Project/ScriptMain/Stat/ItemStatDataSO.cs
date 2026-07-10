using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemStatDataSO", menuName = "Items/ItemStatDataSO")]
public class ItemStatDataSO : ScriptableObject
{
    [System.Serializable]
    public struct StatModifier
    {
        public StatType Type;
        public float Amount; 
    }

    public List<StatModifier> itemStats;
}
