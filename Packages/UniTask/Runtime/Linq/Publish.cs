using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IConnectableUniTaskAsyncEnumerable<TSource> Publish<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));

            return new Publish<TSource>(source);
        }
    }

    internal sealed class Publish<TSource> : IConnectableUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly CancellationTokenSource m_CancellationTokenSource;

        private TriggerEvent<TSource> m_Trigger;
        private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
        private IDisposable m_ConnectedDisposable;
        private bool m_IsCompleted;

        public Publish(IUniTaskAsyncEnumerable<TSource> source) {
            m_Source = source;
            m_CancellationTokenSource = new CancellationTokenSource();
        }

        public IDisposable Connect() {
            if (m_ConnectedDisposable != null) {
                return m_ConnectedDisposable;
            }

            if (m_Enumerator == null) {
                m_Enumerator = m_Source.GetAsyncEnumerator(m_CancellationTokenSource.Token);
            }

            ConsumeEnumerator().Forget();

            m_ConnectedDisposable = new ConnectDisposable(m_CancellationTokenSource);
            return m_ConnectedDisposable;
        }

        async UniTaskVoid ConsumeEnumerator() {
            try {
                try {
                    while (await m_Enumerator.MoveNextAsync()) {
                        m_Trigger.SetResult(m_Enumerator.Current);
                    }

                    m_Trigger.SetCompleted();
                } catch (Exception ex) {
                    m_Trigger.SetError(ex);
                }
            } finally {
                m_IsCompleted = true;
                await m_Enumerator.DisposeAsync();
            }
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerPublish(this, cancellationToken);
        }

        private sealed class ConnectDisposable : IDisposable {
            private readonly CancellationTokenSource m_CancellationTokenSource;

            public ConnectDisposable(CancellationTokenSource cancellationTokenSource) {
                m_CancellationTokenSource = cancellationTokenSource;
            }

            public void Dispose() {
                m_CancellationTokenSource.Cancel();
            }
        }

        private sealed class InnerPublish : MoveNextSource, IUniTaskAsyncEnumerator<TSource>, ITriggerHandler<TSource> {
            private static readonly Action<object> s_CancelDelegate = OnCanceled;

            private readonly Publish<TSource> m_Parent;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_IsDisposed;

            public InnerPublish(Publish<TSource> parent, CancellationToken cancellationToken) {
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                m_Parent = parent;
                m_CancellationToken = cancellationToken;

                if (cancellationToken.CanBeCanceled) {
                    m_CancellationTokenRegistration =
                        cancellationToken.RegisterWithoutCaptureExecutionContext(s_CancelDelegate, this);
                }

                parent.m_Trigger.Add(this);
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TSource Current { get; private set; }
            ITriggerHandler<TSource> ITriggerHandler<TSource>.Prev { get; set; }
            ITriggerHandler<TSource> ITriggerHandler<TSource>.Next { get; set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();

                if (m_Parent.m_IsCompleted) {
                    return CompletedTasks.False;
                }

                mCompletionSource.Reset();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            static void OnCanceled(object state) {
                var self = (InnerPublish)state;
                self.mCompletionSource.TrySetCanceled(self.m_CancellationToken);
                self.DisposeAsync().Forget();
            }

            public UniTask DisposeAsync() {
                if (!m_IsDisposed) {
                    m_IsDisposed = true;
                    TaskTracker.RemoveTracking(this);
                    m_CancellationTokenRegistration.Dispose();
                    m_Parent.m_Trigger.Remove(this);
                }

                return default;
            }

            public void OnNext(TSource value) {
                Current = value;
                mCompletionSource.TrySetResult(true);
            }

            public void OnCanceled(CancellationToken cancellationToken) {
                mCompletionSource.TrySetCanceled(cancellationToken);
            }

            public void OnCompleted() {
                mCompletionSource.TrySetResult(false);
            }

            public void OnError(Exception ex) {
                mCompletionSource.TrySetException(ex);
            }
        }
    }
}