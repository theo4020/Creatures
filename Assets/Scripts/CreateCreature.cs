using UnityEngine;

public class CreateCreature : MonoBehaviour
{
    public string name1 = "";
    public string name2 = "";
    public string nameChild = "";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Crée un GameObject vide et charge la créature dessus
        GameObject go1 = new GameObject(name1);
        Creature creature1 = CreatureLoader.Load(name1, go1);
        
        GameObject go2 = new GameObject(name2);
        Creature creature2 = CreatureLoader.Load(name2, go2);

        GameObject go3 = new GameObject(nameChild);
        Creature child = creature2.Procreate(creature1, creature2, go3);
        CreatureSaver.Save(child, nameChild);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
