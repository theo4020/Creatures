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
    public static Limb Create(LimbData data, Limb parent = null, bool isRandom = true)
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

            ab.swingYLock = ArticulationDofLock.LimitedMotion;
            ab.swingZLock = ArticulationDofLock.LimitedMotion;
            ab.twistLock = ArticulationDofLock.LimitedMotion;
            
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
            parent.AttachLimb(limb, isRandom);
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
    
    public void AttachLimb(Limb child, bool isRandom = true)
    {
        // Choisit aléatoirement l'extrémité du parent
        Vector3 attachPoint;
        if (data.isBody)
        {
            if (isRandom)
            {
                if (Random.value > 0.5f)
                {
                    attachPoint = GetTopPoint();
                    child.data.isTopAttached = true;
                }
                else
                {
                    attachPoint = GetBottomPoint();
                    child.data.isTopAttached = false;
                }
            }
            else if(child.data.isTopAttached)
            {
                attachPoint = GetTopPoint();
            }
            else
            {
                attachPoint = GetBottomPoint();
            }
        }
        else
        {
            attachPoint = data.isTopTaken ? GetBottomPoint() : GetTopPoint();
        }

        // Choisit aléatoirement quelle extrémité de l'enfant colle au parent
        // true = on attache par le haut de l'enfant, false = par le bas
        bool attachByTop;
        if (isRandom)
        {
            attachByTop = Random.value > 0.5f;
        }
        else
        {
            attachByTop = child.data.isTopTaken;
        }

        // Calcule la position du centre de l'enfant pour que son extrémité touche attachPoint
        if (attachByTop)
            // centre = attachPoint - (direction haut enfant * moitié hauteur enfant)
        {
            child.transform.position = attachPoint - child.transform.up * child.data.scale * (child.data.height * 0.5f);
            child.data.isTopTaken = true;
            child.ab.anchorPosition = new Vector3(0, child.data.height * child.data.scale * 0.5f, 0);
        }
        else
            // centre = attachPoint + (direction haut enfant * moitié hauteur enfant)
        {
            child.transform.position = attachPoint + child.transform.up * child.data.scale * (child.data.height * 0.5f);
            child.data.isTopTaken = false;
            child.ab.anchorPosition = new Vector3(0, -child.data.height * child.data.scale * 0.5f, 0);
        }
    }
}