using UnityEngine;

public class AnimalBarrelTile : Tile, IResetable
{
    [SerializeField, Tooltip("For Debugging")]
    AnimalTile animal;
    public AnimalTile Animal
    {
        get
        {
            return animal;
        }

        protected set
        {
            animal = value;
        }
    }

    public void AddAnimal(AnimalTile _animal)
    {
        if(Animal == null && _animal != null)
            Animal = _animal;
    }

    public void RemoveAnimal(AnimalTile _animal)
    {
        if (Animal == _animal && _animal != null)
            Animal = null;
    }

    /// <summary>
    /// So that when dropping off animals 
    /// we can let other shephers know that this is not availble for drop off
    /// </summary>
    public bool IsAvailable { get; set; } = true;
    public bool IsEmpty { get { return Animal == null; } }
    public bool HasAnimal { get { return Animal != null; } }

    public void ResetObject()
    {
        Animal = null;
        IsAvailable = true;
    }
}
