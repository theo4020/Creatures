using System.Collections.Generic;
using UnityEngine;

public class Creature : MonoBehaviour
{
    public Limb body;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CreateFromStart();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void CreateFromStart()
    {
        LimbData bodyData = new LimbData(true);
        body = gameObject.AddComponent<Limb>();
        body.Init(bodyData);
        body.nbAttachedLimb = Random.Range(0, 11);
        for (int i = 0; i < body.nbAttachedLimb; i++)
        {
            LimbData data = new LimbData(false);
            Limb limb = gameObject.AddComponent<Limb>();
            limb.Init(data, body.transform);
            limb.nbAttachedLimb = Random.Range(0, 11);
            for (int j = 0; j < limb.nbAttachedLimb; j++)
            {
                LimbData data2 = new LimbData(false);
                Limb limb2 = gameObject.AddComponent<Limb>();
                limb2.Init(data2, limb.transform);
                limb.limbs.Add(limb2);
            }
            body.limbs.Add(limb);
        }
    }
}
