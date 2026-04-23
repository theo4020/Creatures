using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProceduralCapsule))]
public class Limb : MonoBehaviour
{
    public LimbData data;
    public int nbAttachedLimb = 0;
    public List<Limb> limbs = new List<Limb>();

    void Start()
    {
        Apply();
    }

    public void Apply()
    {
        transform.localPosition = data.position;
        transform.localRotation = data.rotation;
        GetComponent<ProceduralCapsule>().Apply(data);
        var mat = Resources.Load<Material>("Materials/CapsuleMat");
        GetComponent<MeshRenderer>().material = mat;
    }

    // Création depuis un script
    public static Limb Create(LimbData data, Transform parent = null)
    {
        GameObject go;
        if (data.isBody)
        {
            go = new GameObject("Body");
        }
        else
        {
            go = new GameObject("Limb");
        }

        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }

        go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();
        go.AddComponent<CapsuleCollider>();
        go.AddComponent<ProceduralCapsule>();
        
        var membre  = go.AddComponent<Limb>();
        membre.data = data;
        membre.Apply();

        return membre;
    }
    
    public void Init(LimbData data, Transform parent = null)
    {
        if (data.isBody)
        {
            gameObject.name = "Body";
        }
        else
        {
            gameObject.name = "Limb";
        }

        if (parent != null)
        {
            transform.SetParent(parent, false);
        }

        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        gameObject.AddComponent<CapsuleCollider>();
        gameObject.AddComponent<ProceduralCapsule>();
        
        Apply();
    }
}