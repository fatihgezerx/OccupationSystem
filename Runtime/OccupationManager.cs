using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace OccupationSystem
{
    /// <summary>
    /// The one place that runs every <see cref="OccupationAgent"/>: a registry of the agents and a single UniTask loop for all of them
    /// (no <c>Update</c> on any character, no coroutine). The loop wakes a few times a second and thinks only the agents whose turn it
    /// is, each every <see cref="ThinkSeconds"/> seconds, so a crowd of NPCs does not all think in the same frame. It starts with the
    /// first agent and ends with the last, and it runs on scaled time: a paused game stops the day.
    /// </summary>
    public static class OccupationManager
    {
        /// <summary>Seconds between two thoughts of one agent.</summary>
        public const float ThinkSeconds = 0.25f;

        // How often the loop wakes, in milliseconds.
        private const int WakeMilliseconds = 100;

        private static readonly List<OccupationAgent> Agents = new List<OccupationAgent>();

        private static CancellationTokenSource _loopCts;
        private static bool _loopRunning;
        private static int _generation;

        /// <summary>The enabled agents. Entries can be null for a moment while one is being removed; do not modify.</summary>
        public static IReadOnlyList<OccupationAgent> All => Agents;

        /// <summary>Stops the loop and forgets every agent. Safe to call at any time.</summary>
        public static void Shutdown()
        {
            _generation++;
            _loopCts?.Cancel();
            _loopCts?.Dispose();
            _loopCts = null;
            _loopRunning = false;
            Agents.Clear();
        }

        internal static void Add(OccupationAgent agent)
        {
            if (agent == null || Agents.Contains(agent))
            {
                return;
            }

            Agents.Add(agent);

            if (_loopRunning)
            {
                return;
            }

            _loopRunning = true;
            _loopCts?.Dispose();
            _loopCts = new CancellationTokenSource();
            RunLoopAsync(_generation, _loopCts.Token).Forget();
        }

        internal static void Remove(OccupationAgent agent)
        {
            // The loop may be walking the list right now, so leave a hole instead of shifting it.
            var index = Agents.IndexOf(agent);
            if (index >= 0)
            {
                Agents[index] = null;
            }
        }

        private static async UniTaskVoid RunLoopAsync(int generation, CancellationToken token)
        {
            try
            {
                while (true)
                {
                    var now = Time.timeAsDouble;
                    for (var i = 0; i < Agents.Count; i++)
                    {
                        var agent = Agents[i];
                        if (agent != null)
                        {
                            agent.Step(now);
                        }
                    }

                    Compact();
                    if (Agents.Count == 0)
                    {
                        return;
                    }

                    var canceled = await UniTask.Delay(WakeMilliseconds, DelayType.DeltaTime, PlayerLoopTiming.Update, token)
                        .SuppressCancellationThrow();
                    if (canceled)
                    {
                        return;
                    }
                }
            }
            finally
            {
                if (generation == _generation)
                {
                    _loopRunning = false;
                }
            }
        }

        // Removes the holes left by disabled agents, keeping the order.
        private static void Compact()
        {
            var write = 0;
            for (var read = 0; read < Agents.Count; read++)
            {
                var agent = Agents[read];
                if (agent != null)
                {
                    Agents[write++] = agent;
                }
            }

            Agents.RemoveRange(write, Agents.Count - write);
        }

        // Keeps stale state out when "Enter Play Mode Options" skips the domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Shutdown();
    }
}
