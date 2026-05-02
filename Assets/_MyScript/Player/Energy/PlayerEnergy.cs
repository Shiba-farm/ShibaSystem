using UnityEngine;
using UnityEngine.UI;

public class PlayerEnergy : MonoBehaviour
{
    public Slider energySlider;
    public float maxEnergy = 100f;
    public float currentEnergy;

    public float CurrentEnergy => currentEnergy;              // <— เพิ่ม
    public void SetEnergy(float value)                        // <— เพิ่ม
    {
        currentEnergy = Mathf.Clamp(value, 0f, maxEnergy);
        UpdateUI();
    }

    private void Start()
    {
        currentEnergy = Mathf.Clamp(currentEnergy <= 0 ? maxEnergy : currentEnergy, 0, maxEnergy);
        UpdateUI();
    }

    public void UseEnergy(float amount)
    {
        currentEnergy -= amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        UpdateUI();
    }

    public void RefillEnergy(float amount)
    {
        currentEnergy += amount;
        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (energySlider != null)
            energySlider.value = maxEnergy > 0 ? currentEnergy / maxEnergy : 0f;
    }
}
