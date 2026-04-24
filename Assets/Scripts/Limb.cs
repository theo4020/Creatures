using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ProceduralCapsule))]
public class Limb : MonoBehaviour
{
    public LimbData data;
    public int nbAttachedLimb = 0;
    public List<Limb> limbs = new List<Limb>();
    public ArticulationBody ab;

    void Start()
    {
        //Apply();
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
    public static Limb Create(LimbData data, Limb parent = null, bool attach = true)
    {
        GameObject go = new GameObject();
        go.name = data.isBody ? "Body" : "Limb";
        
        var ab = go.AddComponent<ArticulationBody>();
        
        
        if (data.isBody)
        {
            // Le body est la racine — pas de joint
            ab.immovable = false;
        }
        else
        {
            // Les membres enfants ont un joint sphérique (rotation libre dans tous les axes)
            ab.jointType = ArticulationJointType.SphericalJoint;

            // Limites de rotation optionnelles
            var drive = new ArticulationDrive
            {
                lowerLimit = -90f,
                upperLimit = 90f,
                stiffness = 1000f,
                damping = 100f,
                forceLimit = float.MaxValue,
                driveType = ArticulationDriveType.Force
            };
            
            ab.xDrive = drive;
            ab.yDrive = drive;
            ab.zDrive = drive;
            
            ab.anchorRotation = Quaternion.identity;
        }
        

        go.AddComponent<MeshFilter>();
        go.AddComponent<MeshRenderer>();
        go.AddComponent<CapsuleCollider>();
        go.AddComponent<ProceduralCapsule>();
        
        var limb  = go.AddComponent<Limb>();
        limb.data = data;
        limb.ab = ab;
        limb.Apply();
        
        if (parent != null)
        {
            go.transform.SetParent(parent.transform, false);
            if (attach)
                parent.AttachLimb(limb); // seulement à la création, pas au chargement
        }

        return limb;
    }
    
    // Dans ProceduralCapsule.cs ou Membre.cs
    public Vector3 GetTopPoint()
    {
        return transform.position + transform.up * data.scale * (data.height * 0.5f);
    }

    public Vector3 GetBottomPoint()
    {
        return transform.position - transform.up * data.scale * (data.height * 0.5f);
    }
    
    public void AttachLimb(Limb child)
    {
        // Choisit aléatoirement l'extrémité du parent
        Vector3 attachPoint;
        if (data.isBody)
        {
            attachPoint = Random.value > 0.5f ? GetTopPoint() : GetBottomPoint();
        }
        else
        {
            attachPoint = data.isTopTaken ? GetBottomPoint() : GetTopPoint();
        }

        // Choisit aléatoirement quelle extrémité de l'enfant colle au parent
        // true = on attache par le haut de l'enfant, false = par le bas
        bool attachByTop = Random.value > 0.5f;

        // Calcule la position du centre de l'enfant pour que son extrémité touche attachPoint
        if (attachByTop)
            // centre = attachPoint - (direction haut enfant * moitié hauteur enfant)
        {
            child.transform.position = attachPoint - child.transform.up * child.data.scale * (child.data.height * 0.5f);
            child.data.isTopTaken = true;
            child.ab.anchorPosition = new Vector3(0, data.height * data.scale * 0.5f, 0);
        }
        else
            // centre = attachPoint + (direction haut enfant * moitié hauteur enfant)
        {
            child.transform.position = attachPoint + child.transform.up * child.data.scale * (child.data.height * 0.5f);
            child.data.isTopTaken = false;
            child.ab.anchorPosition = new Vector3(0, -data.height * data.scale * 0.5f, 0);
        }
    }
}