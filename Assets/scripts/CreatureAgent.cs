using System.Collections.Generic;
using UnityEngine;

public class CreatureAgent : MonoBehaviour
{
    [Header("Reset")]
    public float spawnHeightOffset = 0.1f;  // Marge au-dessus du sol

    [Header("Friction")]
    public float staticFriction  = 0.8f;
    public float dynamicFriction = 0.6f;
    public float bounciness      = 0f;

    [Header("Joints")]
    public float maxJointVelocity = 120f;   // degrés/sec  (était 360)
    public float jointForceLimit  = 100f;   // N.m         (était 500)
    public float jointStiffness   = 1000f;
    public float jointDamping     = 200f;   // + de damping = mouvements plus fluides

    [Header("Fitness")]
    [Tooltip("Distance minimale avant que la pénalité énergie s'applique")]
    public float minDistForPenalty    = 0.5f;
    [Tooltip("Poids pénalité énergie brute (somme couples)")]
    public float energyPenaltyWeight  = 0.001f;
    [Tooltip("Poids pénalité jerk (changement brusque d'action)")]
    public float jerkPenaltyWeight    = 0.0005f;
    [Tooltip("Bonus pour rester vertical")]
    public float uprightBonusWeight   = 0.01f;

    [Header("Chute")]
    public float fallThreshold = 0.05f;

    // Cerveau injecté par le TrainingManager
    [HideInInspector] public NeuralNetwork Brain;
    [HideInInspector] public bool IsAlive = false;

    // Corps
    private ArticulationBody       _root;
    private List<ArticulationBody> _joints    = new();
    private List<Collider>         _colliders = new();

    // Spawn
    private Vector3 _spawnPos;
    private float   _bodyHalfHeight;

    // Fitness
    private Vector3 _startPos;
    private float   _fitness;
    private float   _maxDist;          // distance maximale atteinte pendant l'épisode
    private float   _energyUsed;
    private float[] _lastActions;
    public  float   Fitness  => _fitness;
    public  float   MaxDist  => _maxDist;

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
            staticFriction  = staticFriction,
            dynamicFriction = dynamicFriction,
            bounciness      = bounciness,
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
        _fitness     = 0f;
        _maxDist     = 0f;
        _energyUsed  = 0f;
        _lastActions = new float[_joints.Count];
        IsAlive      = true;
        ResetBody();
        _startPos = _root.transform.position;
    }

    // ─────────────────────────────────────────────
    // BOUCLE PHYSIQUE
    // ─────────────────────────────────────────────
    private void FixedUpdate()
    {
        if (!IsAlive || Brain == null) return;

        float[] obs     = CollectObservations();
        float[] actions = Brain.Activate(obs);

        float stepEnergy = 0f;
        float stepJerk   = 0f;
        for (int i = 0; i < Mathf.Min(_joints.Count, actions.Length); i++)
        {
            ApplyTorque(_joints[i], actions[i]);
            stepEnergy += Mathf.Abs(actions[i]);
            if (_lastActions != null && i < _lastActions.Length)
                stepJerk += Mathf.Abs(actions[i] - _lastActions[i]);
        }
        _energyUsed += stepEnergy;
        _lastActions  = actions;

        Vector3 pos  = _root.transform.position;
        float   dist = Vector2.Distance(
            new Vector2(pos.x,       pos.z),
            new Vector2(_startPos.x, _startPos.z)
        );
        if (dist > _maxDist) _maxDist = dist;

        // Pénalité active seulement une fois qu'on a avancé suffisamment
        float energyPenalty = _maxDist >= minDistForPenalty
            ? (_energyUsed * energyPenaltyWeight + stepJerk * jerkPenaltyWeight)
            : 0f;

        _fitness = Mathf.Max(0f, _maxDist - energyPenalty);

        if (Time.frameCount % 120 == 0)
            Debug.Log($"[{name}] maxDist={_maxDist:F2}  energy={_energyUsed:F1}  jerk={stepJerk:F2}  penalty={energyPenalty:F2}  fitness={_fitness:F2}");

        if (pos.y < fallThreshold)
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
                vel   = joint.jointVelocity[0]  / 10f;
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
        drive.targetVelocity = value * maxJointVelocity;
        drive.stiffness      = jointStiffness;
        drive.damping        = jointDamping;
        drive.forceLimit     = jointForceLimit;
        joint.xDrive         = drive;
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
            joint.jointForce    = zero;
        }
    }

    // ─────────────────────────────────────────────
    // UTILITAIRES
    // ─────────────────────────────────────────────

    // Calcule la distance entre le centre du root et le point le plus bas des colliders
    private float ComputeBodyHalfHeight()
    {
        if (_colliders.Count == 0) return 1f;

        float rootY   = _root.transform.position.y;
        float lowestY = float.MaxValue;

        foreach (var col in _colliders)
            lowestY = Mathf.Min(lowestY, col.bounds.min.y);

        return Mathf.Abs(rootY - lowestY) + 0.05f;
    }

    public int ObservationSize() => 6 + _joints.Count * 2;
    public int ActionSize()      => _joints.Count;
}