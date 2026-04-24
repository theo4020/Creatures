using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreatureAgent : MonoBehaviour
{
    [Header("Reset")]
    public float spawnHeightOffset = 0.1f;

    [Header("Friction")]
    public float staticFriction  = 0.8f;
    public float dynamicFriction = 0.6f;
    public float bounciness      = 0f;

    [Header("Joints")]
    public float maxJointVelocity = 120f;
    public float jointStiffness   = 1000f;
    public float jointDamping     = 200f;
    public float jointForceLimit  = 100f;

    [Header("Fitness")]
    public float minDistForPenalty   = 0.5f;
    public float energyPenaltyWeight = 0.001f;
    public float jerkPenaltyWeight   = 0.0005f;

    [Header("Chute")]
    public float fallThreshold = 0.05f;

    // Cerveau injecté par TrainingManager
    [HideInInspector] public NeuralNetwork Brain;
    [HideInInspector] public bool IsAlive = false;

    // Corps
    private ArticulationBody       _root;
    private List<ArticulationBody> _joints = new();

    // Spawn
    private Vector3 _spawnPos;
    private float   _bodyHalfHeight;
    private float   _overrideSpawnHeight = 0f;  // 0 = auto, sinon valeur du TrainingManager
    public  void    SetSpawnHeight(float h) => _overrideSpawnHeight = h;

    // Fitness
    private Vector3  _startPos;
    private float    _fitness;
    private float    _maxDist;
    private float    _energyUsed;
    private float[]  _lastActions;
    public  float    Fitness => _fitness;
    public  float    MaxDist => _maxDist;

    // ─────────────────────────────────────────────
    // INITIALISATION
    // ─────────────────────────────────────────────
    public void Initialize()
    {
        _joints.Clear();

        // ── CAS 1 : Creature component présent (génération aléatoire) ──
        Creature creature = GetComponentInChildren<Creature>();
        if (creature != null && creature.body != null)
        {
            _root = creature.body.ab;
            CollectJoints(creature.body);
            Debug.Log($"[{name}] Mode Creature  root='{_root?.name}'  joints={_joints.Count}");
        }
        else
        {
            // ── CAS 2 : Prefab manuel (ArticulationBodies directs) ──
            var allABs = GetComponentsInChildren<ArticulationBody>();
            if (allABs.Length == 0)
            {
                Debug.LogError($"[{name}] Aucun ArticulationBody trouvé !");
                return;
            }
            _root = allABs[0];
            foreach (var ab in allABs)
                if (ab != _root) _joints.Add(ab);
            Debug.Log($"[{name}] Mode Prefab  root='{_root.name}'  joints={_joints.Count}");
        }

        // Friction sur tous les colliders
        var mat = new PhysicsMaterial("CreatureMat")
        {
            staticFriction  = staticFriction,
            dynamicFriction = dynamicFriction,
            bounciness      = bounciness,
            frictionCombine = PhysicsMaterialCombine.Average
        };
        foreach (var col in GetComponentsInChildren<Collider>())
            col.sharedMaterial = mat;

        _bodyHalfHeight = ComputeBodyHalfHeight();
        _spawnPos       = transform.position;
        _lastActions    = new float[ActionSize()];

        Debug.Log($"[{name}] obs={ObservationSize()}  actions={ActionSize()}  halfH={_bodyHalfHeight:F2}");
    }

    // Parcours récursif de l'arbre de Limbs
    private void CollectJoints(Limb limb)
    {
        foreach (var child in limb.limbs)
        {
            if (child.ab != null && child.ab != _root)
                _joints.Add(child.ab);
            CollectJoints(child);
        }
    }

    public void StartEpisode()
    {
        _fitness     = 0f;
        _maxDist     = 0f;
        _energyUsed  = 0f;
        _lastActions = new float[ActionSize()];
        IsAlive      = true;
        ResetBody();
        _startPos = _root.transform.position;
    }

    // ─────────────────────────────────────────────
    // BOUCLE PHYSIQUE
    // ─────────────────────────────────────────────
    private void FixedUpdate()
    {
        if (!IsAlive || Brain == null || _root == null) return;

        float[] obs     = CollectObservations();
        float[] actions = Brain.Activate(obs);

        float stepEnergy = 0f;
        float stepJerk   = 0f;
        int   actionIdx  = 0;

        for (int i = 0; i < _joints.Count; i++)
        {
            int dof = Mathf.Max(1, _joints[i].dofCount);
            float ax = actionIdx < actions.Length ? actions[actionIdx++] : 0f;
            float ay = dof > 1 && actionIdx < actions.Length ? actions[actionIdx++] : 0f;
            float az = dof > 2 && actionIdx < actions.Length ? actions[actionIdx++] : 0f;

            ApplySphericalTorque(_joints[i], ax, ay, az);

            stepEnergy += Mathf.Abs(ax) + Mathf.Abs(ay) + Mathf.Abs(az);

            int baseIdx = _joints.Take(i).Sum(j => Mathf.Max(1, j.dofCount));
            if (_lastActions.Length > baseIdx)
            {
                stepJerk += Mathf.Abs(ax - _lastActions[baseIdx]);
                if (dof > 1 && _lastActions.Length > baseIdx + 1) stepJerk += Mathf.Abs(ay - _lastActions[baseIdx + 1]);
                if (dof > 2 && _lastActions.Length > baseIdx + 2) stepJerk += Mathf.Abs(az - _lastActions[baseIdx + 2]);
            }
        }

        // Sauvegarde actions
        for (int i = 0; i < Mathf.Min(actions.Length, _lastActions.Length); i++)
            _lastActions[i] = actions[i];

        _energyUsed += stepEnergy;

        // Distance 2D
        Vector3 pos  = _root.transform.position;
        float   dist = Vector2.Distance(
            new Vector2(pos.x, pos.z),
            new Vector2(_startPos.x, _startPos.z)
        );
        if (dist > _maxDist) _maxDist = dist;

        // Fitness
        float energyPenalty = _maxDist >= minDistForPenalty
            ? (_energyUsed * energyPenaltyWeight + stepJerk * jerkPenaltyWeight)
            : 0f;
        _fitness = Mathf.Max(0f, _maxDist - energyPenalty);

        if (Time.frameCount % 120 == 0)
            //Debug.Log($"[{name}] dist={_maxDist:F2}  fitness={_fitness:F2}  joints={_joints.Count}");

        if (pos.y < fallThreshold)
            IsAlive = false;
    }

    // ─────────────────────────────────────────────
    // OBSERVATIONS
    // ─────────────────────────────────────────────
    private float[] CollectObservations()
    {
        var obs = new List<float>();

        if (_root == null) return new float[ObservationSize()];

        // Torse
        Vector3 localVel = _root.transform.InverseTransformDirection(_root.linearVelocity);
        obs.Add(localVel.x);
        obs.Add(localVel.z);
        obs.Add(_root.transform.up.x);
        obs.Add(_root.transform.up.y);
        obs.Add(_root.transform.up.z);
        obs.Add(_root.transform.position.y);

        // Joints : angle + vélocité par DOF réel
        foreach (var joint in _joints)
        {
            if (joint == null) continue;
            int dof = Mathf.Max(1, joint.dofCount);
            for (int d = 0; d < dof; d++)
            {
                float angle = d < joint.dofCount ? joint.jointPosition[d] / Mathf.PI : 0f;
                float vel   = d < joint.dofCount ? joint.jointVelocity[d]  / 10f     : 0f;
                obs.Add(angle);
                obs.Add(vel);
            }
        }

        return obs.ToArray();
    }

    // ─────────────────────────────────────────────
    // ACTIONS — Revolute ET Sphérique
    // ─────────────────────────────────────────────
    private void ApplySphericalTorque(ArticulationBody joint, float ax, float ay, float az)
    {
        void SetDrive(ref ArticulationDrive drive, float val)
        {
            drive.targetVelocity = val * maxJointVelocity;
            drive.stiffness      = jointStiffness;
            drive.damping        = jointDamping;
            drive.forceLimit     = jointForceLimit;
        }

        switch (joint.jointType)
        {
            case ArticulationJointType.RevoluteJoint:
                // 1 seul axe X
                var xRev = joint.xDrive;
                SetDrive(ref xRev, ax);
                joint.xDrive = xRev;
                break;

            case ArticulationJointType.SphericalJoint:
                // 3 axes X Y Z
                var xd = joint.xDrive; SetDrive(ref xd, ax); joint.xDrive = xd;
                var yd = joint.yDrive; SetDrive(ref yd, ay); joint.yDrive = yd;
                var zd = joint.zDrive; SetDrive(ref zd, az); joint.zDrive = zd;
                break;

            case ArticulationJointType.PrismaticJoint:
                var xPri = joint.xDrive;
                SetDrive(ref xPri, ax);
                joint.xDrive = xPri;
                break;
        }
    }

    // ─────────────────────────────────────────────
    // RESET
    // ─────────────────────────────────────────────
    private void ResetBody()
    {
        float y = _overrideSpawnHeight > 0f ? _overrideSpawnHeight : _bodyHalfHeight + spawnHeightOffset;
        _root.TeleportRoot(new Vector3(_spawnPos.x, y, _spawnPos.z), Quaternion.identity);

        foreach (var joint in _joints)
        {
            int dof  = Mathf.Clamp(joint.dofCount, 1, 3);
            var zero = dof switch
            {
                1 => new ArticulationReducedSpace(0f),
                2 => new ArticulationReducedSpace(0f, 0f),
                _ => new ArticulationReducedSpace(0f, 0f, 0f)
            };
            joint.jointVelocity = zero;
            joint.jointForce    = zero;
        }
    }

    private float ComputeBodyHalfHeight()
    {
        var colliders = GetComponentsInChildren<Collider>();
        if (colliders.Length == 0) return 1f;
        float rootY   = _root.transform.position.y;
        float lowestY = float.MaxValue;
        foreach (var col in colliders)
            lowestY = Mathf.Min(lowestY, col.bounds.min.y);
        return Mathf.Abs(rootY - lowestY) + 0.05f;
    }

    // Taille dynamique selon le DOF réel de chaque joint
    public int ObservationSize()
    {
        int dofs = _joints.Sum(j => Mathf.Max(1, j.dofCount));
        return 6 + dofs * 2; // angle + vélocité par DOF
    }

    public int ActionSize()
    {
        return _joints.Sum(j => Mathf.Max(1, j.dofCount));
    }
}