// DungeonEnums.cs
// ประเภทต่างๆ ที่ใช้ในระบบ Dungeon

namespace MyGame.Dungeon
{
    /// <summary>ประเภทของ Tile บนผัง Dungeon</summary>
    public enum DungeonTileType
    {
        Wall  = 0,
        Floor = 1,
    }

    /// <summary>ประเภท Object ที่จะ Spawn บน Floor</summary>
    public enum DungeonObjectType
    {
        None        = 0,
        PlayerSpawn = 1,
        Ladder      = 2,   // บันไดลงชั้นถัดไป
        Ore         = 3,   // แร่ที่ขุดได้
        Enemy       = 4,   // ศัตรู
        Rock        = 5,   // หินกีดขวาง (ทุบได้)
    }
}
