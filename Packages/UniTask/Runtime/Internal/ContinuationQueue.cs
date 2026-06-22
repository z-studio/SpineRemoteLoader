#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Internal {
    internal sealed class ContinuationQueue {
        private const int k_MaxArrayLength = 0X7FEFFFFF;
        private const int k_InitialSize = 16;

        private readonly PlayerLoopTiming m_Timing;

        private SpinLock m_Gate = new(false);
        private bool m_Dequing = false;

        private int m_ActionListCount = 0;
        private Action[] m_ActionList = new Action[k_InitialSize];

        private int m_WaitingListCount = 0;
        private Action[] m_WaitingList = new Action[k_InitialSize];

        public ContinuationQueue(PlayerLoopTiming timing) {
            m_Timing = timing;
        }

        public void Enqueue(Action continuation) {
            bool lockTaken = false;

            try {
                m_Gate.Enter(ref lockTaken);

                if (m_Dequing) {
                    // Ensure Capacity
                    if (m_WaitingList.Length == m_WaitingListCount) {
                        var newLength = m_WaitingListCount * 2;

                        if ((uint)newLength > k_MaxArrayLength) {
                            newLength = k_MaxArrayLength;
                        }

                        var newArray = new Action[newLength];
                        Array.Copy(m_WaitingList, newArray, m_WaitingListCount);
                        m_WaitingList = newArray;
                    }

                    m_WaitingList[m_WaitingListCount] = continuation;
                    m_WaitingListCount++;
                } else {
                    // Ensure Capacity
                    if (m_ActionList.Length == m_ActionListCount) {
                        var newLength = m_ActionListCount * 2;

                        if ((uint)newLength > k_MaxArrayLength) {
                            newLength = k_MaxArrayLength;
                        }

                        var newArray = new Action[newLength];
                        Array.Copy(m_ActionList, newArray, m_ActionListCount);
                        m_ActionList = newArray;
                    }

                    m_ActionList[m_ActionListCount] = continuation;
                    m_ActionListCount++;
                }
            } finally {
                if (lockTaken) {
                    m_Gate.Exit(false);
                }
            }
        }

        public int Clear() {
            var rest = m_ActionListCount + m_WaitingListCount;

            m_ActionListCount = 0;
            m_ActionList = new Action[k_InitialSize];

            m_WaitingListCount = 0;
            m_WaitingList = new Action[k_InitialSize];

            return rest;
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
            {
                bool lockTaken = false;

                try {
                    m_Gate.Enter(ref lockTaken);

                    if (m_ActionListCount == 0) {
                        return;
                    }

                    m_Dequing = true;
                } finally {
                    if (lockTaken) {
                        m_Gate.Exit(false);
                    }
                }
            }

            for (var i = 0; i < m_ActionListCount; i++) {
                var action = m_ActionList[i];
                m_ActionList[i] = null;

                try {
                    action();
                } catch (Exception ex) {
                    UnityEngine.Debug.LogException(ex);
                }
            }

            {
                var lockTaken = false;

                try {
                    m_Gate.Enter(ref lockTaken);
                    m_Dequing = false;

                    var swapTempActionList = m_ActionList;

                    m_ActionListCount = m_WaitingListCount;
                    m_ActionList = m_WaitingList;

                    m_WaitingListCount = 0;
                    m_WaitingList = swapTempActionList;
                } finally {
                    if (lockTaken) {
                        m_Gate.Exit(false);
                    }
                }
            }
        }
    }
}