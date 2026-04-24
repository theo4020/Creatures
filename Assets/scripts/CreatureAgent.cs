using System.Collections.Generic;
using UnityEngine;

public class CreatureAgent : MonoBehaviour
{
    [Header("Reset")]
    public float spawnHeightOffset = 0.1f;  // Marge au-dessus du sol

    [Header("Friction")]
    public float staticFriction = 0.8f;
    public float dynamicFriction = 0.6f;
    public float bounciness = 0f;

    [Header("Chute")]
    public float fallThreshold = 0.05f;

    // Cerveau injecté par le TrainingManager
    [HideInInspector] public NeuralNetwork Brain;
    [HideInInspector] public bool IsAlive = false;

    // Corps
    private ArticulationBody _root;
    private List<ArticulationBody> _joints = new();
    private List<Collider> _colliders = new();

    // Spawn
    private Vector3 _spawnPos;
    private float _bodyHalfHeight;

    // Fitness
    private Vector3 _startPos;
    private float _fitness;
    public float Fitness => _fitness;

    // ─────────────────────────────────────────────
    // INITIALISATION
    // ─────────────────────────────────────────────
    public void Initialize()
    {
        // Détecte root et joints
        _root = GetComponentInChildren<ArticulationBody>();
        _joints.Clear();
        foreach (var ab in GetComponentsInChildren<ArticulationBody>())
            if (ab != _root) _joints.Add(ab);

        // Récupère tous les colliders
        _colliders.Clear();
        _colliders.AddRange(GetComponentsInChildren<Collider>());

        // Applique la friction sur chaque collider
        var mat = new PhysicsMaterial("CreatureMat")
        {
            staticFriction = staticFriction,
            dynamicFriction = dynamicFriction,
            bounciness = bounciness,
            frictionCombine = PhysicsMaterialCombine.Average,
        };
        foreach (var col in _colliders)
            col.sharedMaterial = mat;

        // Calcule la hauteur du corps pour le spawn
        _bodyHalfHeight = ComputeBodyHalfHeight();
        _spawnPos = new Vector3(transform.position.x, 0f, transform.position.z);

        Debug.Log($"[{name}] joints={_joints.Count}  colliders={_colliders.Count}  halfHeight={_bodyHalfHeight:F2}");
    }

    public void StartEpisode()
    {
        _fitness = 0f;
        IsAlive = true;
        ResetBody();
        _startPos = _root.transform.position;
    }

    // ─────────────────────────────────────────────
    // BOUCLE PHYSIQUE
    // ─────────────────────────────────────────────
    private void FixedUpdate()
    {
        if (!IsAlive || Brain == null) return;

        float[] obs = CollectObservations();
        float[] actions = Brain.Activate(obs);

        for (int i = 0; i < Mathf.Min(_joints.Count, actions.Length); i++)
            ApplyTorque(_joints[i], actions[i]);

        float dist = _root.transform.position.z - _startPos.z;
        _fitness = Mathf.Max(_fitness, dist);

        if (_root.transform.position.y < fallThreshold)
            IsAlive = false;
    }

    // ─────────────────────────────────────────────
    // OBSERVATIONS
    // ─────────────────────────────────────────────
    private float[] CollectObservations()
    {
        var obs = new List<float>();

        Vector3 localVel = _root.transform.InverseTransformDirection(_root.linearVelocity);
        obs.Add(localVel.x);
        obs.Add(localVel.z);
        obs.Add(_root.transform.up.x);
        obs.Add(_root.transform.up.y);
        obs.Add(_root.transform.up.z);
        obs.Add(_root.transform.position.y);

        foreach (var joint in _joints)
        {
            float angle = 0f, vel = 0f;
            if (joint.dofCount > 0)
            {
                angle = joint.jointPosition[0] / Mathf.PI;
                vel = joint.jointVelocity[0] / 10f;
            }
            obs.Add(angle);
            obs.Add(vel);
        }

        return obs.ToArray();
    }

    // ─────────────────────────────────────────────
    // ACTIONS
    // ─────────────────────────────────────────────
    private void ApplyTorque(ArticulationBody joint, float value)
    {
        if (joint.jointType != ArticulationJointType.RevoluteJoint) return;

        var drive = joint.xDrive;
        drive.targetVelocity = value * 360f;
        drive.stiffness = 1000f;
        drive.damping = 100f;
        drive.forceLimit = 500f;
        joint.xDrive = drive;
    }

    // ─────────────────────────────────────────────
    // RESET
    // ─────────────────────────────────────────────
    private void ResetBody()
    {
        // Spawn juste au-dessus du sol, basé sur la vraie taille du corps
        float y = _bodyHalfHeight + spawnHeightOffset;
        _root.TeleportRoot(new Vector3(_spawnPos.x, y, _spawnPos.z), Quaternion.identity);

        foreach (var joint in _joints)
        {
            var zero = joint.dofCount switch
            {
                1 => new ArticulationReducedSpace(0f),
                2 => new ArticulationReducedSpace(0f, 0f),
                _ => new ArticulationReducedSpace(0f, 0f, 0f)
            };
            joint.jointVelocity = zero;
            joint.jointForce = zero;
        }
    }

    // ─────────────────────────────────────────────
    // UTILITAIRES
    // ─────────────────────────────────────────────

    // Calcule la distance entre le centre du root et le point le plus bas des colliders
    private float ComputeBodyHalfHeight()
    {
        if (_colliders.Count == 0) return 1f;

        float rootY = _root.transform.position.y;
        float lowestY = float.MaxValue;

        foreach (var col in _colliders)
            lowestY = Mathf.Min(lowestY, col.bounds.min.y);

        return Mathf.Abs(rootY - lowestY) + 0.05f;
    }

    public int ObservationSize() => 6 + _joints.Count * 2;
    public int ActionSize() => _joints.Count;
}