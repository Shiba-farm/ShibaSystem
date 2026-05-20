using UnityEngine;
using MyGame.Dungeon;

public class DungeonTester : MonoBehaviour
{
    void Start()
    {
        DungeonManager.Instance.EnterDungeon();
    }

    void Update()
    {
        // กด G เพื่อ Generate ใหม่
        if (Input.GetKeyDown(KeyCode.G))
        {
            DungeonManager.Instance.ClearFloor();
            DungeonManager.Instance.EnterDungeon();
        }
    }
}