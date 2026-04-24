using System.Collections.Generic;
using UnityEngine;

public class Creature : MonoBehaviour
{
    public Limb body;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //CreateFromStart();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void CreateFromStart()
    {
        LimbData bodyData = new LimbData(true, Vector3.zero);
        body = Limb.Create(bodyData);
        body.nbAttachedLimb = Random.Range(0, 9);
        for (int i = 0; i < body.nbAttachedLimb; i++)
        {
            LimbData data = new LimbData(false, Vector3.zero);
            Limb limb = Limb.Create(data, body);
            limb.nbAttachedLimb = Random.Range(0, 1);
            for (int j = 0; j < limb.nbAttachedLimb; j++)
            {
                LimbData data2 = new LimbData(false, Vector3.zero);
                Limb limb2 = Limb.Create(data2, limb);
                limb.limbs.Add(limb2);
            }
            body.limbs.Add(limb);
        }
        
        CreatureSaver.Save(this, "creature_01");
    }
}
