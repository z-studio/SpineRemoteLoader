using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<T> Create<T>(Func<IAsyncWriter<T>, CancellationToken, UniTask> create) {
            Error.ThrowArgumentNullException(create, nameof(create));
            return new Create<T>(create);
        }
    }

    public interface IAsyncWriter<T> {
        UniTask YieldAsync(T value);
    }

    internal sealed class Create<T> : IUniTaskAsyncEnumerable<T> {
        private readonly Func<IAsyncWriter<T>, CancellationToken, UniTask> m_Create;

        public Create(Func<IAsyncWriter<T>, CancellationToken, UniTask> create) {
            m_Create = create;
        }

        public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerCreate(m_Create, cancellationToken);
        }

        private sealed class InnerCreate : MoveNextSource, IUniTaskAsyncEnumerator<T> {
            private readonly Func<IAsyncWriter<T>, CancellationToken, UniTask> m_InnerCreate;
            private readonly CancellationToken m_CancellationToken;

            private int m_State = -1;
            private AsyncWriter m_Writer;

            public InnerCreate(
                Func<IAsyncWriter<T>, CancellationToken, UniTask> create,
                CancellationToken cancellationToken
            ) {
                m_InnerCreate = create;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public T Current { get; private set; }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);
                m_Writer.Dispose();
                return default;
            }

            public UniTask<bool> MoveNextAsync() {
                if (m_State == -2) {
                    return default;
                }

                mCompletionSource.Reset();
                MoveNext();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void MoveNext() {
                try {
                    switch (m_State) {
                        case -1: // init
                        {
                            m_Writer = new AsyncWriter(this);
                            RunWriterTask(m_InnerCreate(m_Writer, m_CancellationToken)).Forget();

                            if (Volatile.Read(ref m_State) == -2) {
                                return; // complete synchronously
                            }

                            m_State = 0; // wait YieldAsync, it set TrySetResult(true)
                            return;
                        }

                        case 0:
                            m_Writer.SignalWriter();
                            return;
                        default:
                            goto DONE;
                    }
                } catch (Exception ex) {
                    m_State = -2;
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                DONE:
                m_State = -2;
                mCompletionSource.TrySetResult(false);
                return;
            }

            private async UniTaskVoid RunWriterTask(UniTask task) {
                try {
                    await task;
                    goto DONE;
                } catch (Exception ex) {
                    Volatile.Write(ref m_State, -2);
                    mCompletionSource.TrySetException(ex);
                    return;
                }

                DONE:
                Volatile.Write(ref m_State, -2);
                mCompletionSource.TrySetResult(false);
            }

            public void SetResult(T value) {
                Current = value;
                mCompletionSource.TrySetResult(true);
            }
        }

        private sealed class AsyncWriter : IUniTaskSource, IAsyncWriter<T>, IDisposable {
            private readonly InnerCreate m_Enumerator;

            private UniTaskCompletionSourceCore<AsyncUnit> m_Core;

            public AsyncWriter(InnerCreate enumerator) {
                m_Enumerator = enumerator;
            }

            public void Dispose() {
                var status = m_Core.GetStatus(m_Core.Version);

                if (status == UniTaskStatus.Pending) {
                    m_Core.TrySetCanceled();
                }
            }

            public void GetResult(short token) {
                m_Core.GetResult(token);
            }

            public UniTaskStatus GetStatus(short token) {
                return m_Core.GetStatus(token);
            }

            public UniTaskStatus UnsafeGetStatus() {
                return m_Core.UnsafeGetStatus();
            }

            public void OnCompleted(Action<object> continuation, object state, short token) {
                m_Core.OnCompleted(continuation, state, token);
            }

            public UniTask YieldAsync(T value) {
                m_Core.Reset();
                m_Enumerator.SetResult(value);
                return new UniTask(this, m_Core.Version);
            }

            public void SignalWriter() {
                m_Core.TrySetResult(AsyncUnit.Default);
            }
        }
    }
}