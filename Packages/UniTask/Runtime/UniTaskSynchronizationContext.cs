using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Cysharp.Threading.Tasks {
    public class UniTaskSynchronizationContext : SynchronizationContext {
        private const int k_MaxArrayLength = 0X7FEFFFFF;
        private const int k_InitialSize = 16;

        private static SpinLock s_Gate = new(false);
        private static bool s_Dequing = false;

        private static int s_ActionListCount = 0;
        private static Callback[] s_ActionList = new Callback[k_InitialSize];

        private static int s_WaitingListCount = 0;
        private static Callback[] s_WaitingList = new Callback[k_InitialSize];

        private static int s_OpCount;

        public override void Send(SendOrPostCallback d, object state) {
            d(state);
        }

        public override void Post(SendOrPostCallback d, object state) {
            var lockTaken = false;

            try {
                s_Gate.Enter(ref lockTaken);

                if (s_Dequing) {
                    // Ensure Capacity
                    if (s_WaitingList.Length == s_WaitingListCount) {
                        var newLength = s_WaitingListCount * 2;

                        if ((uint)newLength > k_MaxArrayLength) {
                            newLength = k_MaxArrayLength;
                        }

                        var newArray = new Callback[newLength];
                        Array.Copy(s_WaitingList, newArray, s_WaitingListCount);
                        s_WaitingList = newArray;
                    }

                    s_WaitingList[s_WaitingListCount] = new Callback(d, state);
                    s_WaitingListCount++;
                } else {
                    // Ensure Capacity
                    if (s_ActionList.Length == s_ActionListCount) {
                        var newLength = s_ActionListCount * 2;

                        if ((uint)newLength > k_MaxArrayLength) {
                            newLength = k_MaxArrayLength;
                        }

                        var newArray = new Callback[newLength];
                        Array.Copy(s_ActionList, newArray, s_ActionListCount);
                        s_ActionList = newArray;
                    }

                    s_ActionList[s_ActionListCount] = new Callback(d, state);
                    s_ActionListCount++;
                }
            } finally {
                if (lockTaken) {
                    s_Gate.Exit(false);
                }
            }
        }

        public override void OperationStarted() {
            Interlocked.Increment(ref s_OpCount);
        }

        public override void OperationCompleted() {
            Interlocked.Decrement(ref s_OpCount);
        }

        public override SynchronizationContext CreateCopy() {
            return this;
        }

        // delegate entrypoint.
        internal static void Run() {
            {
                bool lockTaken = false;

                try {
                    s_Gate.Enter(ref lockTaken);

                    if (s_ActionListCount == 0) {
                        return;
                    }

                    s_Dequing = true;
                } finally {
                    if (lockTaken) {
                        s_Gate.Exit(false);
                    }
                }
            }

            for (int i = 0; i < s_ActionListCount; i++) {
                var action = s_ActionList[i];
                s_ActionList[i] = default;
                action.Invoke();
            }

            {
                var lockTaken = false;

                try {
                    s_Gate.Enter(ref lockTaken);
                    s_Dequing = false;

                    var swapTempActionList = s_ActionList;

                    s_ActionListCount = s_WaitingListCount;
                    s_ActionList = s_WaitingList;

                    s_WaitingListCount = 0;
                    s_WaitingList = swapTempActionList;
                } finally {
                    if (lockTaken) {
                        s_Gate.Exit(false);
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Auto)]
        private readonly struct Callback {
            private readonly SendOrPostCallback callback;
            private readonly object state;

            public Callback(SendOrPostCallback callback, object state) {
                this.callback = callback;
                this.state = state;
            }

            public void Invoke() {
                try {
                    callback(state);
                } catch (Exception ex) {
                    UnityEngine.Debug.LogException(ex);
                }
            }
        }
    }
}