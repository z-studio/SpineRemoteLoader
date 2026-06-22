#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System.Threading;
using UnityEngine;

namespace Cysharp.Threading.Tasks.Triggers {
    public static partial class AsyncTriggerExtensions {
        public static AsyncDestroyTrigger GetAsyncDestroyTrigger(this GameObject gameObject) {
            return GetOrAddComponent<AsyncDestroyTrigger>(gameObject);
        }

        public static AsyncDestroyTrigger GetAsyncDestroyTrigger(this Component component) {
            return component.gameObject.GetAsyncDestroyTrigger();
        }
    }

    [DisallowMultipleComponent]
    public sealed class AsyncDestroyTrigger : MonoBehaviour {
        private bool m_AwakeCalled = false;
        private bool m_Called = false;
        private CancellationTokenSource m_CancellationTokenSource;

        public CancellationToken CancellationToken {
            get {
                if (m_CancellationTokenSource == null) {
                    m_CancellationTokenSource = new CancellationTokenSource();

                    if (!m_AwakeCalled) {
                        PlayerLoopHelper.AddAction(PlayerLoopTiming.Update, new AwakeMonitor(this));
                    }
                }

                return m_CancellationTokenSource.Token;
            }
        }

        private void Awake() {
            m_AwakeCalled = true;
        }

        private void OnDestroy() {
            m_Called = true;

            m_CancellationTokenSource?.Cancel();
            m_CancellationTokenSource?.Dispose();
        }

        public UniTask OnDestroyAsync() {
            if (m_Called) {
                return UniTask.CompletedTask;
            }

            var tcs = new UniTaskCompletionSource();

            // OnDestroy = Called Cancel.
            CancellationToken.RegisterWithoutCaptureExecutionContext(
                state => {
                    var tcs2 = (UniTaskCompletionSource)state;
                    tcs2.TrySetResult();
                },
                tcs
            );

            return tcs.Task;
        }

        private class AwakeMonitor : IPlayerLoopItem {
            private readonly AsyncDestroyTrigger m_Trigger;

            public AwakeMonitor(AsyncDestroyTrigger trigger) {
                m_Trigger = trigger;
            }

            public bool MoveNext() {
                if (m_Trigger.m_Called || m_Trigger.m_AwakeCalled) {
                    return false;
                }

                if (m_Trigger == null) {
                    m_Trigger.OnDestroy();
                    return false;
                }

                return true;
            }
        }
    }
}