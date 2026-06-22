#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Threading;
using UnityEngine;

namespace Cysharp.Threading.Tasks.Triggers {
    public abstract class AsyncTriggerBase<T> : MonoBehaviour, IUniTaskAsyncEnumerable<T> {
        private TriggerEvent<T> m_TriggerEvent;

        protected internal bool calledAwake;
        protected internal bool calledDestroy;

        private void Awake() {
            calledAwake = true;
        }

        private void OnDestroy() {
            if (calledDestroy) {
                return;
            }

            calledDestroy = true;

            m_TriggerEvent.SetCompleted();
        }

        internal void AddHandler(ITriggerHandler<T> handler) {
            if (!calledAwake) {
                PlayerLoopHelper.AddAction(PlayerLoopTiming.Update, new AwakeMonitor(this));
            }

            m_TriggerEvent.Add(handler);
        }

        internal void RemoveHandler(ITriggerHandler<T> handler) {
            if (!calledAwake) {
                PlayerLoopHelper.AddAction(PlayerLoopTiming.Update, new AwakeMonitor(this));
            }

            m_TriggerEvent.Remove(handler);
        }

        protected void RaiseEvent(T value) {
            m_TriggerEvent.SetResult(value);
        }

        public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new AsyncTriggerEnumerator(this, cancellationToken);
        }

        private sealed class AsyncTriggerEnumerator : MoveNextSource, IUniTaskAsyncEnumerator<T>, ITriggerHandler<T> {
            private static Action<object> s_CancellationCallback = CancellationCallback;

            private readonly AsyncTriggerBase<T> m_Parent;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_Registration;
            private bool m_Called;
            private bool m_IsDisposed;

            public AsyncTriggerEnumerator(AsyncTriggerBase<T> parent, CancellationToken cancellationToken) {
                m_Parent = parent;
                m_CancellationToken = cancellationToken;
            }

            public void OnCanceled(CancellationToken cancellationToken = default) {
                mCompletionSource.TrySetCanceled(cancellationToken);
            }

            public void OnNext(T value) {
                Current = value;
                mCompletionSource.TrySetResult(true);
            }

            public void OnCompleted() {
                mCompletionSource.TrySetResult(false);
            }

            public void OnError(Exception ex) {
                mCompletionSource.TrySetException(ex);
            }

            static void CancellationCallback(object state) {
                var self = (AsyncTriggerEnumerator)state;
                self.DisposeAsync().Forget(); // sync

                self.mCompletionSource.TrySetCanceled(self.m_CancellationToken);
            }

            public T Current { get; private set; }
            ITriggerHandler<T> ITriggerHandler<T>.Prev { get; set; }
            ITriggerHandler<T> ITriggerHandler<T>.Next { get; set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();
                mCompletionSource.Reset();

                if (!m_Called) {
                    m_Called = true;

                    TaskTracker.TrackActiveTask(this, 3);
                    m_Parent.AddHandler(this);

                    if (m_CancellationToken.CanBeCanceled) {
                        m_Registration =
                            m_CancellationToken.RegisterWithoutCaptureExecutionContext(s_CancellationCallback, this);
                    }
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            public UniTask DisposeAsync() {
                if (!m_IsDisposed) {
                    m_IsDisposed = true;
                    TaskTracker.RemoveTracking(this);
                    m_Registration.Dispose();
                    m_Parent.RemoveHandler(this);
                }

                return default;
            }
        }

        private class AwakeMonitor : IPlayerLoopItem {
            private readonly AsyncTriggerBase<T> m_Trigger;

            public AwakeMonitor(AsyncTriggerBase<T> trigger) {
                m_Trigger = trigger;
            }

            public bool MoveNext() {
                if (m_Trigger.calledAwake) {
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

    public interface IAsyncOneShotTrigger {
        UniTask OneShotAsync();
    }

    public partial class AsyncTriggerHandler<T> : IAsyncOneShotTrigger {
        UniTask IAsyncOneShotTrigger.OneShotAsync() {
            m_Core.Reset();
            return new UniTask((IUniTaskSource)this, m_Core.Version);
        }
    }

    public sealed partial class AsyncTriggerHandler<T> : IUniTaskSource<T>, ITriggerHandler<T>, IDisposable {
        private static Action<object> s_CancellationCallback = CancellationCallback;

        private readonly AsyncTriggerBase<T> m_Trigger;

        private CancellationToken m_CancellationToken;
        private CancellationTokenRegistration m_Registration;
        private bool m_IsDisposed;
        private bool m_CallOnce;

        private UniTaskCompletionSourceCore<T> m_Core;

        internal CancellationToken CancellationToken => m_CancellationToken;

        ITriggerHandler<T> ITriggerHandler<T>.Prev { get; set; }
        ITriggerHandler<T> ITriggerHandler<T>.Next { get; set; }

        internal AsyncTriggerHandler(AsyncTriggerBase<T> trigger, bool callOnce) {
            if (m_CancellationToken.IsCancellationRequested) {
                m_IsDisposed = true;
                return;
            }

            m_Trigger = trigger;
            m_CancellationToken = default;
            m_Registration = default;
            m_CallOnce = callOnce;

            trigger.AddHandler(this);

            TaskTracker.TrackActiveTask(this, 3);
        }

        internal AsyncTriggerHandler(AsyncTriggerBase<T> trigger, CancellationToken cancellationToken, bool callOnce) {
            if (cancellationToken.IsCancellationRequested) {
                m_IsDisposed = true;
                return;
            }

            m_Trigger = trigger;
            m_CancellationToken = cancellationToken;
            m_CallOnce = callOnce;

            trigger.AddHandler(this);

            if (cancellationToken.CanBeCanceled) {
                m_Registration = cancellationToken.RegisterWithoutCaptureExecutionContext(s_CancellationCallback, this);
            }

            TaskTracker.TrackActiveTask(this, 3);
        }

        private static void CancellationCallback(object state) {
            var self = (AsyncTriggerHandler<T>)state;
            self.Dispose();

            self.m_Core.TrySetCanceled(self.m_CancellationToken);
        }

        public void Dispose() {
            if (!m_IsDisposed) {
                m_IsDisposed = true;
                TaskTracker.RemoveTracking(this);
                m_Registration.Dispose();
                m_Trigger.RemoveHandler(this);
            }
        }

        T IUniTaskSource<T>.GetResult(short token) {
            try {
                return m_Core.GetResult(token);
            } finally {
                if (m_CallOnce) {
                    Dispose();
                }
            }
        }

        void ITriggerHandler<T>.OnNext(T value) {
            m_Core.TrySetResult(value);
        }

        void ITriggerHandler<T>.OnCanceled(CancellationToken cancellationToken) {
            m_Core.TrySetCanceled(cancellationToken);
        }

        void ITriggerHandler<T>.OnCompleted() {
            m_Core.TrySetCanceled(CancellationToken.None);
        }

        void ITriggerHandler<T>.OnError(Exception ex) {
            m_Core.TrySetException(ex);
        }

        void IUniTaskSource.GetResult(short token) {
            ((IUniTaskSource<T>)this).GetResult(token);
        }

        UniTaskStatus IUniTaskSource.GetStatus(short token) {
            return m_Core.GetStatus(token);
        }

        UniTaskStatus IUniTaskSource.UnsafeGetStatus() {
            return m_Core.UnsafeGetStatus();
        }

        void IUniTaskSource.OnCompleted(Action<object> continuation, object state, short token) {
            m_Core.OnCompleted(continuation, state, token);
        }
    }
}