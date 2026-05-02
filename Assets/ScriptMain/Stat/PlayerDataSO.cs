using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatDataSO", menuName = "Stat/PlayerStatDataSO")]
public class PlayerStatDataSO : ScriptableObject
{
    [Header("Max value")]
    public float maxHealth;
    public float moveSpeed;
    public float maxStamina;
    public float maxEnergy;

    [Header("Drainning value")]
    public float energyDrainRunning;
    public float energyDrainNormal;
    public float staminaDrainRunning;
    
    [Header("Regen value")]
    public float staminaRegenNormal;
}   
