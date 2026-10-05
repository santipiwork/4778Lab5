using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace AvoiderPlugin
{
    [DisallowMultipleComponent]
    [AddComponentMenu("AI/Avoider")]
    public sealed class Avoider : MonoBehaviour
    {
        [Header("Required")]
        [Tooltip("The object whose line of sight this agent avoids.")]
        public Transform avoidee;
        [Min(0.5f)] public float range = 10f;
        [Min(0f)] public float speed = 4f;
        public bool showGizmos = true;
        [Header("Hiding search")]
        [Min(1f)] public float searchRadius = 12f;
        [Min(0.25f)] public float sampleSpacing = 1.5f;
        [Range(16, 1000)] public int maxSamples = 400;
        [Min(0.1f)] public float checkInterval = 0.4f;
        [Tooltip("Include solid cover. Actor colliders and triggers are ignored.")]
        public LayerMask occluderMask = ~0;
        [Min(0.1f)] public float eyeHeight = 1f;
        [Min(0.1f)] public float navMeshSnapDistance = 1f;
        [Min(0f)] public float turnSpeed = 540f;

        public struct Sample
        {
            public Vector3 position;
            public bool hidden;
            public bool reachable;
        }
        private readonly List<Sample> samples = new List<Sample>();
        public IReadOnlyList<Sample> LastSamples => samples;
        public string Status { get; private set; } = "Waiting";
        public int HiddenCount { get; private set; }
        public Vector3? Destination { get; private set; }
        private NavMeshAgent agent;
        private NavMeshPath path;
        private float nextCheck;
        private int searchNumber;
        private string lastWarning;
        private bool previousUpdateRotation;

        private void OnEnable()
        {
            agent = GetComponent<NavMeshAgent>(); path = new NavMeshPath(); nextCheck = 0;
            if (agent != null) { previousUpdateRotation = agent.updateRotation; agent.updateRotation = false; }
            ValidateSetup(false);
        }
        private void OnDisable()
        {
            Stop();
            if (agent != null) agent.updateRotation = previousUpdateRotation;
        }
        public bool ValidateSetup(bool requireBakedMesh = true)
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            string warning = agent == null ? "Add a NavMeshAgent to this GameObject and bake a NavMesh." :
                avoidee == null ? "Assign an Avoidee object in the inspector." :
                avoidee == transform || avoidee.IsChildOf(transform) ? "Avoidee must be a different actor." :
                !agent.enabled ? "Enable the NavMeshAgent." :
                requireBakedMesh && !agent.isOnNavMesh ? "Agent is not on a NavMesh. Bake a NavMesh and place the agent on it." : null;
            if (warning != null)
            {
                Status = "Setup required";
                if (lastWarning != warning) Debug.LogWarning("Avoider: " + warning, this);
                lastWarning = warning; return false;
            }
            lastWarning = null; return true;
        }
        private void Update()
        {
            if (avoidee != null)
            {
                Vector3 direction = avoidee.position - transform.position; direction.y = 0;
                if (direction.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
            }
            if (Time.time < nextCheck) return;
            nextCheck = Time.time + Mathf.Max(0.1f, checkInterval);
            if (!ValidateSetup()) { Stop(); return; }
            agent.speed = Mathf.Max(0, speed);
            Vector3 separation = avoidee.position - transform.position; separation.y = 0;
            if (separation.sqrMagnitude > range * range) { Stop(); Status = "Out of range"; return; }
            if (!IsVisible(transform.position)) { Stop(); Status = "Hidden"; return; }
            // Finish a still-hidden route before sampling again, avoiding target jitter.
            if (Destination.HasValue && agent.hasPath && agent.pathStatus == NavMeshPathStatus.PathComplete &&
                agent.remainingDistance > agent.stoppingDistance + 0.15f && !IsVisible(Destination.Value))
            { Status = "Escaping"; return; }
            FindHidingSpot();
        }
        public bool IsVisible(Vector3 groundPoint)
        {
            if (avoidee == null) return false;
            Vector3 origin = avoidee.position + Vector3.up * eyeHeight;
            Vector3 delta = groundPoint + Vector3.up * eyeHeight - origin;
            if (delta.sqrMagnitude < 0.001f) return true;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, delta.normalized, delta.magnitude, occluderMask, QueryTriggerInteraction.Ignore))
            {
                Transform t = hit.collider.transform;
                if (t == transform || t.IsChildOf(transform) || t == avoidee || t.IsChildOf(avoidee)) continue;
                return false;
            }
            return true;
        }
        public void FindHidingSpot()
        {
            if (!ValidateSetup()) return;
            samples.Clear(); HiddenCount = 0;
            float radius = Mathf.Max(1, searchRadius), bestDistance = float.PositiveInfinity;
            Vector3 center = transform.position;
            Vector3? best = null;
            var sampler = new PoissonDiscSampler(radius * 2, radius * 2, Mathf.Max(0.25f, sampleSpacing), 12345 + searchNumber++);
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            foreach (Vector2 point in sampler.Samples(Mathf.Clamp(maxSamples, 16, 1000)))
            {
                Vector3 raw = center + new Vector3(point.x - radius, 0, point.y - radius);
                if ((raw - center).sqrMagnitude > radius * radius) continue;
                if (!NavMesh.SamplePosition(raw, out NavMeshHit hit, Mathf.Max(0.1f, navMeshSnapDistance), filter)) continue;
                if (Mathf.Abs(hit.position.y - center.y) > navMeshSnapDistance) continue;
                bool hidden = !IsVisible(hit.position);
                bool reachable = hidden && agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete;
                samples.Add(new Sample { position = hit.position, hidden = hidden, reachable = reachable });
                if (!reachable) continue;
                HiddenCount++;
                float distance = 0; Vector3 previous = center;
                foreach (Vector3 corner in path.corners) { distance += Vector3.Distance(previous, corner); previous = corner; }
                if (distance < bestDistance) { bestDistance = distance; best = hit.position; }
            }
            if (best.HasValue)
            {
                agent.isStopped = false;
                if (agent.SetDestination(best.Value)) { Destination = best; Status = "Escaping"; return; }
            }
            Stop(); Status = "No reachable cover";
        }
        private void Stop()
        {
            Destination = null;
            if (agent != null && agent.enabled && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); }
        }
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            Gizmos.color = new Color(1, 0.7f, 0.2f, 0.6f); Gizmos.DrawWireSphere(transform.position, range);
            foreach (Sample sample in samples)
            {
                Gizmos.color = sample.reachable ? Color.green : sample.hidden ? Color.yellow : Color.red;
                Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, sample.position + Vector3.up * 0.1f);
                Gizmos.DrawSphere(sample.position + Vector3.up * 0.1f, 0.07f);
            }
            if (Destination.HasValue) { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(Destination.Value, 0.4f); }
        }
    }
}
