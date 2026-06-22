using System;
using UnityEngine;

namespace Cysharp.Threading.Tasks.Internal {
    internal sealed class PlayerLoopRunner {
        private const int k_InitialSize = 16;

        private readonly PlayerLoopTiming m_Timing;
        private readonly object m_RunningAndQueueLock = new();
        private readonly object m_ArrayLock = new();
        private readonly Action<Exception> m_UnhandledExceptionCallback;

        private int m_Tail = 0;
        private bool m_Running = false;
        private IPlayerLoopItem[] m_LoopItems = new IPlayerLoopItem[k_InitialSize];
        private MinimumQueue<IPlayerLoopItem> m_WaitQueue = new(k_InitialSize);

        public PlayerLoopRunner(PlayerLoopTiming timing) {
            m_UnhandledExceptionCallback = ex => Debug.LogException(ex);
            m_Timing = timing;
        }

        public void AddAction(IPlayerLoopItem item) {
            lock (m_RunningAndQueueLock) {
                if (m_Running) {
                    m_WaitQueue.Enqueue(item);
                    return;
                }
            }

            lock (m_ArrayLock) {
                // Ensure Capacity
                if (m_LoopItems.Length == m_Tail) {
                    Array.Resize(ref m_LoopItems, checked(m_Tail * 2));
                }

                m_LoopItems[m_Tail++] = item;
            }
        }

        public int Clear() {
            lock (m_ArrayLock) {
                var rest = 0;

                for (var index = 0; index < m_LoopItems.Length; index++) {
                    if (m_LoopItems[index] != null) {
                        rest++;
                    }

                    m_LoopItems[index] = null;
                }

                m_Tail = 0;
                return rest;
            }
        }

        // delegate entrypoint.
        public void Run() {
            // for debugging, create named stacktrace.
#if DEBUG
            switch (m_Timing) {
                case PlayerLoopTiming.Initialization:
                    Initialization();
                    break;
                case PlayerLoopTiming.LastInitialization:
                    LastInitialization();
                    break;
                case PlayerLoopTiming.EarlyUpdate:
                    EarlyUpdate();
                    break;
                case PlayerLoopTiming.LastEarlyUpdate:
                    LastEarlyUpdate();
                    break;
                case PlayerLoopTiming.FixedUpdate:
                    FixedUpdate();
                    break;
                case PlayerLoopTiming.LastFixedUpdate:
                    LastFixedUpdate();
                    break;
                case PlayerLoopTiming.PreUpdate:
                    PreUpdate();
                    break;
                case PlayerLoopTiming.LastPreUpdate:
                    LastPreUpdate();
                    break;
                case PlayerLoopTiming.Update:
                    Update();
                    break;
                case PlayerLoopTiming.LastUpdate:
                    LastUpdate();
                    break;
                case PlayerLoopTiming.PreLateUpdate:
                    PreLateUpdate();
                    break;
                case PlayerLoopTiming.LastPreLateUpdate:
                    LastPreLateUpdate();
                    break;
                case PlayerLoopTiming.PostLateUpdate:
                    PostLateUpdate();
                    break;
                case PlayerLoopTiming.LastPostLateUpdate:
                    LastPostLateUpdate();
                    break;
                case PlayerLoopTiming.TimeUpdate:
                    TimeUpdate();
                    break;
                case PlayerLoopTiming.LastTimeUpdate:
                    LastTimeUpdate();
                    break;
                default:
                    break;
            }
#else
            RunCore();
#endif
        }

        private void Initialization() => RunCore();
        private void LastInitialization() => RunCore();
        private void EarlyUpdate() => RunCore();
        private void LastEarlyUpdate() => RunCore();
        private void FixedUpdate() => RunCore();
        private void LastFixedUpdate() => RunCore();
        private void PreUpdate() => RunCore();
        private void LastPreUpdate() => RunCore();
        private void Update() => RunCore();
        private void LastUpdate() => RunCore();
        private void PreLateUpdate() => RunCore();
        private void LastPreLateUpdate() => RunCore();
        private void PostLateUpdate() => RunCore();
        private void LastPostLateUpdate() => RunCore();
        private void TimeUpdate() => RunCore();
        private void LastTimeUpdate() => RunCore();

        [System.Diagnostics.DebuggerHidden]
        private void RunCore() {
            lock (m_RunningAndQueueLock) {
                m_Running = true;
            }

            lock (m_ArrayLock) {
                var j = m_Tail - 1;

                for (var i = 0; i < m_LoopItems.Length; i++) {
                    var action = m_LoopItems[i];

                    if (action != null) {
                        try {
                            if (!action.MoveNext()) {
                                m_LoopItems[i] = null;
                            } else {
                                continue; // next i 
                            }
                        } catch (Exception ex) {
                            m_LoopItems[i] = null;

                            try {
                                m_UnhandledExceptionCallback(ex);
                            } catch { }
                        }
                    }

                    // find null, loop from tail
                    while (i < j) {
                        var fromTail = m_LoopItems[j];

                        if (fromTail != null) {
                            try {
                                if (!fromTail.MoveNext()) {
                                    m_LoopItems[j] = null;
                                    j--;
                                    continue; // next j
                                } else {
                                    // swap
                                    m_LoopItems[i] = fromTail;
                                    m_LoopItems[j] = null;
                                    j--;
                                    goto NEXT_LOOP; // next i
                                }
                            } catch (Exception ex) {
                                m_LoopItems[j] = null;
                                j--;

                                try {
                                    m_UnhandledExceptionCallback(ex);
                                } catch { }

                                continue; // next j
                            }
                        } else {
                            j--;
                        }
                    }

                    m_Tail = i; // loop end
                    break; // LOOP END

                    NEXT_LOOP:
                    continue;
                }

                lock (m_RunningAndQueueLock) {
                    m_Running = false;

                    while (m_WaitQueue.Count != 0) {
                        if (m_LoopItems.Length == m_Tail) {
                            Array.Resize(ref m_LoopItems, checked(m_Tail * 2));
                        }

                        m_LoopItems[m_Tail++] = m_WaitQueue.Dequeue();
                    }
                }
            }
        }
    }
}