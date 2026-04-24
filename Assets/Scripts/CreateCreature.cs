using UnityEngine;

public class CreateCreature : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Crée un GameObject vide et charge la créature dessus
        GameObject go = new GameObject("LoadedCreature");
        Creature creature = CreatureLoader.Load("creature_01", go);

        //creature = creature.Procreate(creature, creature, go);
        //CreatureSaver.Save(creature, "creature_01");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
