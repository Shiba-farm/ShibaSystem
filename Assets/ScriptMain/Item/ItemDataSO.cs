using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "ItemDataSO", menuName = "Item & Building/ItemDataSO")]
public class ItemData : ScriptableObject 
{
    [Header("General Data")]
    public string ItemName;
    public int MaxStackSize = 99;
    public bool stackable = true;
    [Header("UI")]
    public TileBase tile;
    public Sprite image;
    public ItemType type;
    public ActionType actionType;
    public Vector2Int range = new Vector2Int(5,4);


    public virtual void Use(StatManager user){}
}

public enum ItemType
{
    Building,
    Tool,
    Food,
    Money
}

public enum ActionType
{
    Dig,
    Mine
}