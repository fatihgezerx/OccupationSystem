using AnimationProfileSystem;
using UnityEngine;
using UnityEngine.AI;

namespace OccupationSystem
{
    /// <summary>
    /// The body an <see cref="OccupationAgent"/> uses when no other system provides one: the character's own NavMeshAgent walks it and the
    /// <see cref="ProfileAnimator"/> of the Animation Profile System shows its animations (the idle, walking and running of its Animation
    /// Profile, and the animation of each step on the activity layer). Always free: nothing else claims the character.
    /// </summary>
    internal sealed class NavMeshOccupationBody : IOccupationBody
    {
        // How far from a place the NavMesh is looked for a point to stand on.
        private const float ReachRadius = 2f;

        private readonly OccupationAgent _agent;
        private readonly NavMeshAgent _nav;
        private readonly ProfileAnimator _animator;
        private readonly Vector3 _home;

        private bool _warned;

        private NavMeshOccupationBody(OccupationAgent agent, NavMeshAgent nav, ProfileAnimator animator)
        {
            _agent = agent;
            _nav = nav;
            _animator = animator;
            _home = nav.transform.position;
        }

        /// <summary>
        /// Makes the body for an agent whose object has a NavMeshAgent and an Animator; null (with a warning) when one is missing. The
        /// object gets a Profile Animator when it has none (with a warning when it has no Animation Profile yet).
        /// </summary>
        public static NavMeshOccupationBody Create(OccupationAgent agent)
        {
            if (!agent.TryGetComponent<NavMeshAgent>(out var nav) || agent.GetComponentInChildren<Animator>() == null)
            {
                Debug.LogWarning($"[Occupation] '{agent.name}' needs a NavMeshAgent on its object and an Animator on it or under it to move by itself.", agent);
                return null;
            }

            if (!agent.TryGetComponent<ProfileAnimator>(out var animator))
            {
                animator = agent.gameObject.AddComponent<ProfileAnimator>();
            }

            if (animator.Profile == null)
            {
                Debug.LogWarning($"[Occupation] The Profile Animator of '{agent.name}' has no Animation Profile, so nothing plays while it stands, walks or does its steps. Assign one on the Profile Animator.", animator);
            }

            return new NavMeshOccupationBody(agent, nav, animator);
        }

        public Vector3 Home => _home;

        public bool IsFree => _nav != null && _nav.isActiveAndEnabled;

        public bool TryReach(Vector3 position, out Vector3 destination)
        {
            var filter = new NavMeshQueryFilter { agentTypeID = _nav.agentTypeID, areaMask = _nav.areaMask };
            if (NavMesh.SamplePosition(position, out var hit, ReachRadius, filter))
            {
                destination = hit.position;
                return true;
            }

            destination = default;
            return false;
        }

        public void Teleport(Vector3 position)
        {
            if (_nav.isOnNavMesh)
            {
                _nav.Warp(position);
            }
            else
            {
                _nav.transform.position = position;
            }
        }

        public void MoveTo(Vector3 destination, OccupationGait gait)
        {
            if (!_nav.isOnNavMesh)
            {
                if (!_warned)
                {
                    _warned = true;
                    Debug.LogWarning($"[Occupation] '{_nav.name}' is not standing on a NavMesh, so it cannot walk. Bake one (a NavMeshSurface on the ground) and place the NPC on it.", _nav);
                }

                return;
            }

            var data = _agent.Data;
            var run = gait == OccupationGait.Run;
            var walkSpeed = data != null ? data.WalkSpeed : OccupationData.DefaultWalkSpeed;
            var runSpeed = data != null ? data.RunSpeed : OccupationData.DefaultRunSpeed;
            _nav.speed = run ? runSpeed : walkSpeed;
            _nav.isStopped = false;
            _nav.SetDestination(destination);

            // The animator learns its gaits from the movement; it is told the occupation's speeds so the walk and run clips show at them
            // from the first step, and shown the chosen gait whole while the NPC speeds up and slows down.
            _animator.SetKnownSpeeds(walkSpeed, runSpeed);
            _animator.ForcedGait = run ? GaitOverride.Run : GaitOverride.Walk;
        }

        public bool HasArrived(float radius) =>
            _nav.isOnNavMesh && !_nav.pathPending && _nav.remainingDistance <= Mathf.Max(radius, _nav.stoppingDistance + 0.05f);

        public void StopMoving()
        {
            if (_nav != null && _nav.isOnNavMesh)
            {
                _nav.ResetPath();
            }

            _animator.ForcedGait = GaitOverride.Auto;
        }

        public void PlayActivity(AnimationClip clip, bool loop) => _animator.PlayActivity(clip, loop);

        public void StopActivity() => _animator.StopActivity();

        public void Release()
        {
            if (_nav != null && _nav.isOnNavMesh)
            {
                _nav.ResetPath();
            }

            if (_animator != null)
            {
                _animator.ForcedGait = GaitOverride.Auto;
                _animator.StopActivity();
            }
        }
    }
}
