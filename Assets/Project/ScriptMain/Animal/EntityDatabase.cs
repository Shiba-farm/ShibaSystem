using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EntityDatabase", menuName = "Animals/EntityDatabase")]
public class EntityDatabase : ScriptableObject
{
    [SerializeField] private List<AnimalSO> allAnimals = new List<AnimalSO>();
    private Dictionary<int, AnimalSO> animalLookup = new Dictionary<int, AnimalSO>();
    private Dictionary<string, AnimalSO> animalNameLookup = new Dictionary<string, AnimalSO>(System.StringComparer.OrdinalIgnoreCase);
    private Dictionary<AnimalStockType, List<AnimalSO>> animalTypeLookup = new Dictionary<AnimalStockType, List<AnimalSO>>();
    private bool isInitialized = false;

    /// <summary>
    /// ScriptableObject assets stay loaded in memory across Play Mode sessions in the Editor
    /// (this is normal Unity behaviour, and is even more likely to bite when Domain/Scene
    /// Reload is disabled under Project Settings > Editor > Enter Play Mode Settings) — so
    /// once Initialize() has run, isInitialized just stays true forever, even after allAnimals
    /// changes (e.g. you add more animals to a category between test runs). The other guards
    /// below only re-Initialize when a dictionary is completely empty, which doesn't catch
    /// "non-empty but stale". OnEnable fires whenever this asset is (re)loaded, so force a
    /// fresh rebuild on next access rather than trusting whatever ran before.
    /// </summary>
    private void OnEnable()
    {
        isInitialized = false;
    }

    public void Initialize()
    {
        animalLookup.Clear();
        animalNameLookup.Clear();
        animalTypeLookup.Clear();
        foreach (var animal in allAnimals)
        {
            if (animal != null)
            {
                if (!animalLookup.ContainsKey(animal.animalId))
                {
                    Debug.Log($"Registering Animal: {animal.animalName} with ID: {animal.animalId}");
                    animalLookup.Add(animal.animalId, animal);
                }

                if (!string.IsNullOrEmpty(animal.animalName) && !animalNameLookup.ContainsKey(animal.animalName))
                {
                    animalNameLookup.Add(animal.animalName, animal);
                }
            }
        }

        foreach (var animal in allAnimals)
        {
            if (animal != null)
            {
                if (!animalTypeLookup.ContainsKey(animal.type))
                {
                    animalTypeLookup[animal.type] = new List<AnimalSO>();
                }
                animalTypeLookup[animal.type].Add(animal);
            }
        }
        isInitialized = true;
    }

    public AnimalSO GetAnimalByID(int id)
    {
        if (!isInitialized || animalLookup.Count == 0)
            Initialize();

        if (animalLookup.TryGetValue(id, out var animal))
        {
            return animal;
        }

        Debug.LogWarning($"Animal ID {id} not found in database!");
        return null;
    }

    /// <summary>ค้นหา AnimalSO จากชื่อ (ไม่สนตัวพิมพ์เล็ก/ใหญ่) — ใช้โดย AnimalStockServerManager.BuyLiveStockServerRpc</summary>
    public AnimalSO GetAnimalByName(string animalName)
    {
        if (!isInitialized || animalNameLookup.Count == 0)
            Initialize();

        if (!string.IsNullOrEmpty(animalName) && animalNameLookup.TryGetValue(animalName, out var animal))
        {
            return animal;
        }

        Debug.LogWarning($"Animal name '{animalName}' not found in database!");
        return null;
    }

    public List<AnimalSO> GetAnimalsByType(AnimalStockType type)
    {
        if (!isInitialized || animalTypeLookup.Count == 0)
            Initialize();

        if (animalTypeLookup.TryGetValue(type, out var animals))
        {
            return animals;
        }

        Debug.LogWarning($"No animals found for type {type} in database!");
        return new List<AnimalSO>();
    }

    public List<AnimalSO> GetAllAnimals()
    {
        if (!isInitialized || allAnimals.Count == 0)
            Initialize();

        return new List<AnimalSO>(allAnimals);
    }
}
