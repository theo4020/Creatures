using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

/// <summary>
/// Version ML-Agents de CreatureAgent (PPO).
/// Remplace CreatureAgent.cs sur le prefab pour utiliser ML-Agents.
///
/// SETUP :
///   1. Retirer CreatureAgent.cs du prefab
///   2. Ajouter ce script + BehaviorParameters + DecisionRequester
///   3. BehaviorParameters :
///        - Behavior Name    : Creature
///        - Space Size       : 14  (6 + joints*2, pour 4 joints)
///        - Continuous Act.  : 4   (nb joints)
///        - Behavior Type    : Default
///   4. DecisionRequester : Decision Period = 5
///   5. pip install mlagents
///   6. mlagents-learn config.yaml --run-id=run1
///   7. Play ▶
/// </summary>
public class CreatureAgentML : Agent
{
    [Header("Joints")]
    public float maxJointVelocity = 120f;
    public float jointStiffness   = 1000f;
    public float jointDamping     = 200f;
    public float jointForceLimit  = 100f;

    [Header("Fitness")]
    public float energyPenaltyWeight = 0.005f;
    public float fallThreshold       = 0.05f;
    public float spawnHeightOffset   = 0.1f;

    private ArticulationBody       _root;
    private List<ArticulationBody> _joints    = new();
    private List<Collider>         _colliders = new();

    private Vector3 _startPos;
    private float   _maxDist;
    private float   _energyUsed;
    private float[] _lastActions;
    private float   _bodyHalfHeight;

    // ─────────────────────────────────────────────
    // INIT
    // ─────────────────────────────────────────────
    public override void Initialize()
    {
        _root = GetComponentInChildren<ArticulationBody>();

        _joints.Clear();
        foreach (var ab in GetComponentsInChildren<ArticulationBody>())
            if (ab != _root) _joints.Add(ab);

        _colliders.AddRange(GetComponentsInChildren<Collider>());

        // Friction
        var mat = new PhysicsMaterial("CreatureMatML")
        {
            staticFriction  = 0.8f,
            dynamicFriction = 0.6f,
            bounciness      = 0f,
            frictionCombine = PhysicsMaterialCombine.Average
        };
        foreach (var col in _colliders)
            col.sharedMaterial = mat;

        _bodyHalfHeight = ComputeBodyHalfHeight();
        _lastActions    = new float[_joints.Count];

        Debug.Log($"[CreatureAgentML] joints={_joints.Count}  halfHeight={_bodyHalfHeight:F2}");
        Debug.Log($"[CreatureAgentML] BehaviorName={GetComponent<Unity.MLAgents.Policies.BehaviorParameters>()?.BehaviorName}  BehaviorType={GetComponent<Unity.MLAgents.Policies.BehaviorParameters>()?.BehaviorType}");
    }

    public override void OnEpisodeBegin()
    {
        _maxDist    = 0f;
        _energyUsed = 0f;
        _lastActions = new float[_joints.Count];
        ResetBody();
        _startPos = _root.transform.position;
    }

    // ─────────────────────────────────────────────
    // OBSERVATIONS
    // ─────────────────────────────────────────────
    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 localVel = _root.transform.InverseTransformDirection(_root.linearVelocity);
        sensor.AddObservation(localVel.x);
        sensor.AddObservation(localVel.z);
        sensor.AddObservation(_root.transform.up);
        sensor.AddObservation(_root.transform.position.y);

        foreach (var joint in _joints)
        {
            float angle = 0f, vel = 0f;
            if (joint.dofCount > 0)
            {
                angle = joint.jointPosition[0] / Mathf.PI;
                vel   = joint.jointVelocity[0]  / 10f;
            }
            sensor.AddObservation(angle);
            sensor.AddObservation(vel);
        }
    }

    // ─────────────────────────────────────────────
    // ACTIONS
    // ─────────────────────────────────────────────
    public override void OnActionReceived(ActionBuffers actions)
    {
        float stepEnergy = 0f;
        int count = Mathf.Min(_joints.Count, actions.ContinuousActions.Length);

        for (int i = 0; i < count; i++)
        {
            float torque = actions.ContinuousActions[i];
            ApplyTorque(_joints[i], torque);
            stepEnergy += Mathf.Abs(torque);
            if (i < _lastActions.Length)
                stepEnergy += Mathf.Abs(torque - _lastActions[i]) * 0.5f;
            _lastActions[i] = torque;
        }
        _energyUsed += stepEnergy;

        // Distance 2D
        Vector3 pos  = _root.transform.position;
        float   dist = Vector2.Distance(
            new Vector2(pos.x, pos.z),
            new Vector2(_startPos.x, _startPos.z)
        );
        if (dist > _maxDist) _maxDist = dist;

        // Récompenses
        float forwardReward = dist * 0.01f;
        float uprightBonus  = Mathf.Clamp01(Vector3.Dot(_root.transform.up, Vector3.up)) * 0.001f;
        float energyPenalty = stepEnergy * energyPenaltyWeight;

        AddReward(forwardReward + uprightBonus - energyPenalty);

        // Chute
        if (pos.y < fallThreshold)
        {
            AddReward(-1f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        for (int i = 0; i < actions.Length; i++)
        {
            float phase = i * (Mathf.PI / 2f);
            actions[i] = Mathf.Sin(Time.time * 2f + phase);
        }
    }

    // ─────────────────────────────────────────────
    // UTILITAIRES
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

    private void ResetBody()
    {
        float y = _bodyHalfHeight + spawnHeightOffset;
        _root.TeleportRoot(
            new Vector3(transform.position.x, y, transform.position.z),
            Quaternion.identity
        );
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

    private float ComputeBodyHalfHeight()
    {
        if (_colliders.Count == 0) return 1f;
        float rootY   = _root.transform.position.y;
        float lowestY = float.MaxValue;
        foreach (var col in _colliders)
            lowestY = Mathf.Min(lowestY, col.bounds.min.y);
        return Mathf.Abs(rootY - lowestY) + 0.05f;
    }
}