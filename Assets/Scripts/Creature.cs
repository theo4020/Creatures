using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

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

    public Creature Procreate(Creature parent1, Creature parent2, GameObject creatureGo)
    {
        Creature child = creatureGo.AddComponent<Creature>();
        
        child.body = ProcreateLimb(parent1.body, parent2.body, true);

        for (int i = 0; i < child.body.nbAttachedLimb; i++)
        {
            Limb limb;
            if (i >= parent1.body.nbAttachedLimb)
            {
                limb = parent2.body.limbs[i];
            }
            else if (i >= parent2.body.nbAttachedLimb)
            {
                limb = parent1.body.limbs[i];
            }
            else
            {
                limb = ProcreateLimb(parent1.body.limbs[i], parent2.body.limbs[i], false, child.body);
            }
            child.body.limbs.Add(limb);
        }
        return child;
    }

    Limb ProcreateLimb(Limb parent1, Limb parent2, bool isBody, Limb HierarchyParent = null)
    {
        //Ne fonctionne que pour les membres attachés au body ou le body
        LimbData data = new LimbData(true, Vector3.zero);
        LimbData data1 = parent1.data;
        LimbData data2 = parent2.data;

        Limb limb;
        
        data.isBody =  isBody;
        
        //Rotation
        int random = Random.Range(1, 11);
        if (random <= 4)
        {
            data.rotation = data1.rotation;
        }
        else if (random <= 8)
        {
            data.rotation = data2.rotation;
        }
        else
        {
            data.rotation = RandomRange(data1.rotation, data2.rotation);
        }
        
        //Scale
        random = Random.Range(1, 11);
        if (random <= 4)
        {
            data.scale = data1.scale;
        }
        else if (random <= 8)
        {
            data.scale = data2.scale;
        }
        else
        {
            data.scale = Random.Range(Mathf.Min(data1.scale, data2.scale), Mathf.Max(data1.scale, data2.scale));
        }
        
        //Radius
        random = Random.Range(1, 11);
        if (random <= 4)
        {
            data.radius = data1.radius;
        }
        else if (random <= 8)
        {
            data.radius = data2.radius;
        }
        else
        {
            data.radius = Random.Range(Mathf.Min(data1.radius, data2.radius), Mathf.Max(data1.radius, data2.radius));
        }
        
        //Height
        random = Random.Range(1, 11);
        if (random <= 4)
        {
            data.height = data1.height;
        }
        else if (random <= 8)
        {
            data.height = data2.height;
        }
        else
        {
            data.height = Random.Range(Mathf.Min(data1.height, data2.height), Mathf.Max(data1.height, data2.height));
        }
        data.height = Mathf.Max(data.radius*2, data.height);
        
        //IsTopAttached
        random = Random.Range(1, 11);
        if (random <= 5)
        {
            data.isTopAttached = data1.isTopAttached;
        }
        else if (random <= 10)
        {
            data.isTopAttached = data2.isTopAttached;
        }
        
        //IsTopTaken
        random = Random.Range(1, 11);
        if (random <= 5)
        {
            data.isTopTaken = data1.isTopTaken;
        }
        else if (random <= 10)
        {
            data.isTopTaken = data2.isTopTaken;
        }

        limb = Limb.Create(data, HierarchyParent, false);
        
        //nbAttachedLimb
        random = Random.Range(1, 11);
        if (random <= 4)
        {
            limb.nbAttachedLimb = parent1.nbAttachedLimb;
        }
        else if (random <= 8)
        {
            limb.nbAttachedLimb = parent2.nbAttachedLimb;
        }
        else
        {
            limb.nbAttachedLimb = Random.Range(Mathf.Min(parent1.nbAttachedLimb, parent2.nbAttachedLimb), 
                Mathf.Max(parent1.nbAttachedLimb, parent2.nbAttachedLimb));
        }

        return limb;
    }
    
    public static Quaternion RandomRange(Quaternion a, Quaternion b)
    {
        return Quaternion.Slerp(a, b, Random.value);
    }
}
