#if UNITASK_YOOASSET_SUPPORT
using System;
using System.Threading;
using YooAsset;

namespace Cysharp.Threading.Tasks {
    public static class AsyncOperationBaseExtensions {
        public static UniTask.Awaiter GetAwaiter(this AsyncOperationBase handle) {
            return ToUniTask(handle).GetAwaiter();
        }

        public static UniTask WithCancellation(
            this AsyncOperationBase handle,
            CancellationToken cancellationToken,
            bool cancelImmediately = false
        ) {
            return ToUniTask(handle, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static UniTask ToUniTask(
            this AsyncOperationBase handle,
            IProgress<float> progress = null,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default,
            bool cancelImmediately = false
        ) {
            if (cancellationToken.IsCancellationRequested) {
                return UniTask.FromCanceled(cancellationToken);
            }

            if (handle.IsDone) {
                return UniTask.CompletedTask;
            }

            return new UniTask(
                AsyncOperationBaseConfiguredSource.Create(
                    handle,
                    timing,
                    progress,
                    cancellationToken,
                    cancelImmediately,
                    out var token
                ),
                token
            );
        }

        private sealed class AsyncOperationBaseConfiguredSource : IUniTaskSource,
                                                                  IPlayerLoopItem,
                                                                  ITaskPoolNode<AsyncOperationBaseConfiguredSource> {
            private static TaskPool<AsyncOperationBaseConfiguredSource> s_Pool;
            private AsyncOperationBaseConfiguredSource m_NextNode;
            public ref AsyncOperationBaseConfiguredSource NextNode => ref m_NextNode;

            static AsyncOperationBaseConfiguredSource() {
                TaskPool.RegisterSizeGetter(typeof(AsyncOperationBaseConfiguredSource), () => s_Pool.Size);
            }

            private readonly Action<AsyncOperationBase> m_CompletedCallback;
            private AsyncOperationBase m_Handle;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private IProgress<float> m_Progress;
            private bool m_CancelImmediately;
            private bool m_Completed;

            private UniTaskCompletionSourceCore<AsyncUnit> m_Core;

            private AsyncOperationBaseConfiguredSource() {
                m_CompletedCallback = HandleCompleted;
            }

            public static IUniTaskSource Create(
                AsyncOperationBase handle,
                PlayerLoopTiming timing,
                IProgress<float> progress,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new AsyncOperationBaseConfiguredSource();
                }

                result.m_Handle = handle;
                result.m_Progress = progress;
                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;
                result.m_Completed = false;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var source = (AsyncOperationBaseConfiguredSource)state;
                            source.m_Core.TrySetCanceled(source.m_CancellationToken);
                        },
                        result
                    );
                }

                TaskTracker.TrackActiveTask(result, 3);
                PlayerLoopHelper.AddAction(timing, result);

                handle.Completed += result.m_CompletedCallback;

                token = result.m_Core.Version;
                return result;
            }

            private void HandleCompleted(AsyncOperationBase _) {
                if (m_Handle != null) {
                    m_Handle.Completed -= m_CompletedCallback;
                }

                if (m_Completed) {
                    return;
                }

                m_Completed = true;

                if (m_CancellationToken.IsCancellationRequested) {
                    m_Core.TrySetCanceled(m_CancellationToken);
                } else {
                    m_Core.TrySetResult(AsyncUnit.Default);
                }
            }

            public void GetResult(short token) {
                try {
                    m_Core.GetResult(token);
                } finally {
                    TryReturn();
                }
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
                if (m_Completed) {
                    return false;
                }

                if (m_CancellationToken.IsCancellationRequested) {
                    m_Completed = true;
                    m_Core.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                if (m_Handle == null || m_Handle.IsDone) {
                    m_Completed = true;
                    m_Core.TrySetResult(AsyncUnit.Default);
                    return false;
                }

                m_Progress?.Report(m_Handle.Progress);
                return true;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_Handle = default;
                m_Progress = default;
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                return s_Pool.TryPush(this);
            }
        }
    }
}
#endif