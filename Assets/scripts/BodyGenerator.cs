using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Génère un corps de créature procédural à partir d'un génome (tableau de floats).
///
/// STRUCTURE DU GÉNOME  (par paire de segments de membre) :
///   [0] longueur du segment 1  (0.2 → 0.8 m)
///   [1] rayon  du segment 1    (0.05 → 0.2 m)
///   [2] longueur du segment 2
///   [3] rayon  du segment 2
///   [4] force du joint (0 → 1, normalisée)
///   [5] limite angulaire haute (30° → 120°)
///   ... répété pour chaque membre
/// </summary>
public class BodyGenerator : MonoBehaviour
{
    [Header("Paramètres par défaut")]
    public int limbCount = 4;               // Nombre de membres
    public int segmentsPerLimb = 2;         // Segments par membre
    public Material bodyMaterial;

    // Longueur du génome = limbCount * segmentsPerLimb * GENES_PER_SEGMENT
    public const int GENES_PER_SEGMENT = 6;

    // ─────────────────────────────────────────────
    // POINT D'ENTRÉE PRINCIPAL
    // ─────────────────────────────────────────────

    /// <summary>
    /// Détruit l'ancien corps et construit un nouveau depuis le génome fourni.
    /// </summary>
    public ArticulationBody BuildFromGenome(float[] genome)
    {
        // Nettoie l'ancien corps
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        // ── Torse principal ──
        GameObject torsoGO = CreateCapsule("Torso", transform, Vector3.zero, 0.3f, 0.5f);
        ArticulationBody torsoAB = torsoGO.AddComponent<ArticulationBody>();
        torsoAB.immovable = false;

        List<ArticulationBody> allJoints = new List<ArticulationBody>();

        // ── Membres ──
        // Place 4 membres aux coins du torse (avant/arrière gauche/droite)
        Vector3[] limbAttachPoints = {
            new Vector3( 0.25f, 0f,  0.25f),   // avant-droit
            new Vector3(-0.25f, 0f,  0.25f),   // avant-gauche
            new Vector3( 0.25f, 0f, -0.25f),   // arrière-droit
            new Vector3(-0.25f, 0f, -0.25f),   // arrière-gauche
        };

        for (int i = 0; i < limbCount && i < limbAttachPoints.Length; i++)
        {
            int geneOffset = i * segmentsPerLimb * GENES_PER_SEGMENT;
            BuildLimb(torsoGO, limbAttachPoints[i], genome, geneOffset, allJoints);
        }

        // Note : l'injection dans CreatureAgent se fera
        // via Initialize() qui auto-détecte les ArticulationBodies
        return torsoAB;
    }

    // ─────────────────────────────────────────────
    // CONSTRUCTION D'UN MEMBRE
    // ─────────────────────────────────────────────
    private void BuildLimb(
        GameObject parent,
        Vector3 attachOffset,
        float[] genome,
        int geneOffset,
        List<ArticulationBody> jointList)
    {
        GameObject current = parent;
        Vector3 currentOffset = attachOffset;

        for (int s = 0; s < segmentsPerLimb; s++)
        {
            int idx = geneOffset + s * GENES_PER_SEGMENT;

            // Lit les gènes (avec clamp de sécurité)
            float length = Mathf.Lerp(0.2f, 0.8f, Mathf.Clamp01(genome[idx + 0]));
            float radius = Mathf.Lerp(0.05f, 0.2f, Mathf.Clamp01(genome[idx + 1]));
            float jointForce = Mathf.Lerp(100f, 1000f, Mathf.Clamp01(genome[idx + 4]));
            float angleLimit = Mathf.Lerp(30f, 120f, Mathf.Clamp01(genome[idx + 5]));

            // Crée le segment
            string segName = $"Limb{geneOffset / (segmentsPerLimb * GENES_PER_SEGMENT)}_Seg{s}";
            Vector3 segPos = currentOffset + Vector3.down * (length * 0.5f);
            GameObject segGO = CreateCapsule(segName, current.transform, segPos, radius, length);

            // Ajoute ArticulationBody avec joint révoluté
            ArticulationBody ab = segGO.AddComponent<ArticulationBody>();
            ab.jointType = ArticulationJointType.RevoluteJoint;

            ArticulationDrive drive = ab.xDrive;
            drive.stiffness = jointForce;
            drive.damping = jointForce * 0.1f;
            drive.forceLimit = jointForce * 2f;
            drive.lowerLimit = -angleLimit;
            drive.upperLimit = angleLimit;
            ab.xDrive = drive;

            jointList.Add(ab);

            // Prépare la prochaine itération
            current = segGO;
            currentOffset = Vector3.down * (length * 0.5f);
        }
    }

    // ─────────────────────────────────────────────
    // UTILITAIRE : création d'une capsule Unity
    // ─────────────────────────────────────────────
    private GameObject CreateCapsule(
        string name,
        Transform parent,
        Vector3 localPosition,
        float radius,
        float height)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);

        if (bodyMaterial != null)
            go.GetComponent<Renderer>().material = bodyMaterial;

        return go;
    }

    // ─────────────────────────────────────────────
    // HELPER : taille du génome pour la config actuelle
    // ─────────────────────────────────────────────
    public int GenomeSize() => limbCount * segmentsPerLimb * GENES_PER_SEGMENT;
}