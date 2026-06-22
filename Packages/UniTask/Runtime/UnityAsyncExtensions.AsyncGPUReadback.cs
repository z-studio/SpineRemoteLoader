#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Threading;
using UnityEngine.Rendering;

namespace Cysharp.Threading.Tasks {
    public static partial class UnityAsyncExtensions {
        #region AsyncGPUReadbackRequest

        public static UniTask<AsyncGPUReadbackRequest>.Awaiter GetAwaiter(this AsyncGPUReadbackRequest asyncOperation) {
            return ToUniTask(asyncOperation).GetAwaiter();
        }

        public static UniTask<AsyncGPUReadbackRequest> WithCancellation(
            this AsyncGPUReadbackRequest asyncOperation,
            CancellationToken cancellationToken
        ) {
            return ToUniTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static UniTask<AsyncGPUReadbackRequest> WithCancellation(
            this AsyncGPUReadbackRequest asyncOperation,
            CancellationToken cancellationToken,
            bool cancelImmediately
        ) {
            return ToUniTask(
                asyncOperation,
                cancellationToken: cancellationToken,
                cancelImmediately: cancelImmediately
            );
        }

        public static UniTask<AsyncGPUReadbackRequest> ToUniTask(
            this AsyncGPUReadbackRequest asyncOperation,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            if (asyncOperation.done) {
                return UniTask.FromResult(asyncOperation);
            }

            return new UniTask<AsyncGPUReadbackRequest>(
                AsyncGPUReadbackRequestAwaiterConfiguredSource.Create(
                    asyncOperation,
                    timing,
                    cancellationToken,
                    cancelImmediately,
                    out var token
                ),
                token
            );
        }

        private sealed class AsyncGPUReadbackRequestAwaiterConfiguredSource : IUniTaskSource<AsyncGPUReadbackRequest>,
                                                                              IPlayerLoopItem,
                                                                              ITaskPoolNode<
                                                                                  AsyncGPUReadbackRequestAwaiterConfiguredSource> {
            private static TaskPool<AsyncGPUReadbackRequestAwaiterConfiguredSource> s_Pool;
            private AsyncGPUReadbackRequestAwaiterConfiguredSource m_NextNode;
            public ref AsyncGPUReadbackRequestAwaiterConfiguredSource NextNode => ref m_NextNode;

            static AsyncGPUReadbackRequestAwaiterConfiguredSource() {
                TaskPool.RegisterSizeGetter(typeof(AsyncGPUReadbackRequestAwaiterConfiguredSource), () => s_Pool.Size);
            }

            private AsyncGPUReadbackRequest m_AsyncOperation;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;
            private UniTaskCompletionSourceCore<AsyncGPUReadbackRequest> m_Core;

            private AsyncGPUReadbackRequestAwaiterConfiguredSource() { }

            public static IUniTaskSource<AsyncGPUReadbackRequest> Create(
                AsyncGPUReadbackRequest asyncOperation,
                PlayerLoopTiming timing,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource<AsyncGPUReadbackRequest>.CreateFromCanceled(
                        cancellationToken,
                        out token
                    );
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new AsyncGPUReadbackRequestAwaiterConfiguredSource();
                }

                result.m_AsyncOperation = asyncOperation;
                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (AsyncGPUReadbackRequestAwaiterConfiguredSource)state;
                            promise.m_Core.TrySetCanceled(promise.m_CancellationToken);
                        },
                        result
                    );
                }

                TaskTracker.TrackActiveTask(result, 3);

                PlayerLoopHelper.AddAction(timing, result);

                token = result.m_Core.Version;
                return result;
            }

            public AsyncGPUReadbackRequest GetResult(short token) {
                try {
                    return m_Core.GetResult(token);
                } finally {
                    if (!(m_CancelImmediately && m_CancellationToken.IsCancellationRequested)) {
                        TryReturn();
                    } else {
                        TaskTracker.RemoveTracking(this);
                    }
                }
            }

            void IUniTaskSource.GetResult(short token) {
                GetResult(token);
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

            public bool MoveNext() {
                if (m_CancellationToken.IsCancellationRequested) {
                    m_Core.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                if (m_AsyncOperation.hasError) {
                    m_Core.TrySetException(new Exception("AsyncGPUReadbackRequest.hasError = true"));
                    return false;
                }

                if (m_AsyncOperation.done) {
                    m_Core.TrySetResult(m_AsyncOperation);
                    return false;
                }

                return true;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_AsyncOperation = default;
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                m_CancelImmediately = default;
                return s_Pool.TryPush(this);
            }
        }

        #endregion
    }
}