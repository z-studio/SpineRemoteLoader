// AsyncInstantiateOperation was added since Unity 2022.3.20 / 2023.3.0b7
using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;
using UnityEngine;

namespace Cysharp.Threading.Tasks {
    public static class AsyncInstantiateOperationExtensions {
        // AsyncInstantiateOperation<T> has GetAwaiter so no need to impl
        // public static UniTask<T[]>.Awaiter GetAwaiter<T>(this AsyncInstantiateOperation<T> operation) where T : Object

        public static UniTask<UnityEngine.Object[]> WithCancellation<T>(
            this AsyncInstantiateOperation asyncOperation,
            CancellationToken cancellationToken
        ) {
            return ToUniTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static UniTask<UnityEngine.Object[]> WithCancellation<T>(
            this AsyncInstantiateOperation asyncOperation,
            CancellationToken cancellationToken,
            bool cancelImmediately
        ) {
            return ToUniTask(
                asyncOperation,
                cancellationToken: cancellationToken,
                cancelImmediately: cancelImmediately
            );
        }

        public static UniTask<UnityEngine.Object[]> ToUniTask(
            this AsyncInstantiateOperation asyncOperation,
            IProgress<float> progress = null,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            Error.ThrowArgumentNullException(asyncOperation, nameof(asyncOperation));

            if (cancellationToken.IsCancellationRequested) {
                return UniTask.FromCanceled<UnityEngine.Object[]>(cancellationToken);
            }

            if (asyncOperation.isDone) {
                return UniTask.FromResult(asyncOperation.Result);
            }

            return new UniTask<UnityEngine.Object[]>(
                AsyncInstantiateOperationConfiguredSource.Create(
                    asyncOperation,
                    timing,
                    progress,
                    cancellationToken,
                    cancelImmediately,
                    out var token
                ),
                token
            );
        }

        public static UniTask<T[]> WithCancellation<T>(
            this AsyncInstantiateOperation<T> asyncOperation,
            CancellationToken cancellationToken
        )
            where T : UnityEngine.Object {
            return ToUniTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static UniTask<T[]> WithCancellation<T>(
            this AsyncInstantiateOperation<T> asyncOperation,
            CancellationToken cancellationToken,
            bool cancelImmediately
        )
            where T : UnityEngine.Object {
            return ToUniTask(
                asyncOperation,
                cancellationToken: cancellationToken,
                cancelImmediately: cancelImmediately
            );
        }

        public static UniTask<T[]> ToUniTask<T>(
            this AsyncInstantiateOperation<T> asyncOperation,
            IProgress<float> progress = null,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        )
            where T : UnityEngine.Object {
            Error.ThrowArgumentNullException(asyncOperation, nameof(asyncOperation));

            if (cancellationToken.IsCancellationRequested) {
                return UniTask.FromCanceled<T[]>(cancellationToken);
            }

            if (asyncOperation.isDone) {
                return UniTask.FromResult(asyncOperation.Result);
            }

            return new UniTask<T[]>(
                AsyncInstantiateOperationConfiguredSource<T>.Create(
                    asyncOperation,
                    timing,
                    progress,
                    cancellationToken,
                    cancelImmediately,
                    out var token
                ),
                token
            );
        }

        private sealed class AsyncInstantiateOperationConfiguredSource : IUniTaskSource<UnityEngine.Object[]>,
                                                                         IPlayerLoopItem,
                                                                         ITaskPoolNode<
                                                                             AsyncInstantiateOperationConfiguredSource> {
            private static TaskPool<AsyncInstantiateOperationConfiguredSource> s_Pool;
            private AsyncInstantiateOperationConfiguredSource m_NextNode;
            public ref AsyncInstantiateOperationConfiguredSource NextNode => ref m_NextNode;

            static AsyncInstantiateOperationConfiguredSource() {
                TaskPool.RegisterSizeGetter(typeof(AsyncInstantiateOperationConfiguredSource), () => s_Pool.Size);
            }

            private AsyncInstantiateOperation m_AsyncOperation;
            private IProgress<float> m_Progress;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;
            private bool m_Completed;

            private UniTaskCompletionSourceCore<UnityEngine.Object[]> m_Core;

            private Action<AsyncOperation> m_ContinuationAction;

            private AsyncInstantiateOperationConfiguredSource() {
                m_ContinuationAction = Continuation;
            }

            public static IUniTaskSource<UnityEngine.Object[]> Create(
                AsyncInstantiateOperation asyncOperation,
                PlayerLoopTiming timing,
                IProgress<float> progress,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource<UnityEngine.Object[]>.CreateFromCanceled(
                        cancellationToken,
                        out token
                    );
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new AsyncInstantiateOperationConfiguredSource();
                }

                result.m_AsyncOperation = asyncOperation;
                result.m_Progress = progress;
                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;
                result.m_Completed = false;

                asyncOperation.completed += result.m_ContinuationAction;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var source = (AsyncInstantiateOperationConfiguredSource)state;
                            source.m_Core.TrySetCanceled(source.m_CancellationToken);
                        },
                        result
                    );
                }

                TaskTracker.TrackActiveTask(result, 3);

                PlayerLoopHelper.AddAction(timing, result);

                token = result.m_Core.Version;
                return result;
            }

            public UnityEngine.Object[] GetResult(short token) {
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
                // Already completed
                if (m_Completed || m_AsyncOperation == null) {
                    return false;
                }

                if (m_CancellationToken.IsCancellationRequested) {
                    m_Core.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                if (m_Progress != null) {
                    m_Progress.Report(m_AsyncOperation.progress);
                }

                if (m_AsyncOperation.isDone) {
                    m_Core.TrySetResult(m_AsyncOperation.Result);
                    return false;
                }

                return true;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_AsyncOperation.completed -= m_ContinuationAction;
                m_AsyncOperation = default;
                m_Progress = default;
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                m_CancelImmediately = default;
                return s_Pool.TryPush(this);
            }

            private void Continuation(AsyncOperation _) {
                if (m_Completed) {
                    return;
                }

                m_Completed = true;

                if (m_CancellationToken.IsCancellationRequested) {
                    m_Core.TrySetCanceled(m_CancellationToken);
                } else {
                    m_Core.TrySetResult(m_AsyncOperation.Result);
                }
            }
        }

        private sealed class AsyncInstantiateOperationConfiguredSource<T> : IUniTaskSource<T[]>,
                                                                            IPlayerLoopItem,
                                                                            ITaskPoolNode<
                                                                                AsyncInstantiateOperationConfiguredSource<T>>
            where T : UnityEngine.Object {
            private static TaskPool<AsyncInstantiateOperationConfiguredSource<T>> s_Pool;
            private AsyncInstantiateOperationConfiguredSource<T> m_NextNode;
            public ref AsyncInstantiateOperationConfiguredSource<T> NextNode => ref m_NextNode;

            static AsyncInstantiateOperationConfiguredSource() {
                TaskPool.RegisterSizeGetter(typeof(AsyncInstantiateOperationConfiguredSource<T>), () => s_Pool.Size);
            }

            private AsyncInstantiateOperation<T> m_AsyncOperation;
            private IProgress<float> m_Progress;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;
            private bool m_Completed;

            private UniTaskCompletionSourceCore<T[]> m_Core;

            private Action<AsyncOperation> m_ContinuationAction;

            private AsyncInstantiateOperationConfiguredSource() {
                m_ContinuationAction = Continuation;
            }

            public static IUniTaskSource<T[]> Create(
                AsyncInstantiateOperation<T> asyncOperation,
                PlayerLoopTiming timing,
                IProgress<float> progress,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource<T[]>.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new AsyncInstantiateOperationConfiguredSource<T>();
                }

                result.m_AsyncOperation = asyncOperation;
                result.m_Progress = progress;
                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;
                result.m_Completed = false;

                asyncOperation.completed += result.m_ContinuationAction;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var source = (AsyncInstantiateOperationConfiguredSource<T>)state;
                            source.m_Core.TrySetCanceled(source.m_CancellationToken);
                        },
                        result
                    );
                }

                TaskTracker.TrackActiveTask(result, 3);

                PlayerLoopHelper.AddAction(timing, result);

                token = result.m_Core.Version;
                return result;
            }

            public T[] GetResult(short token) {
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
                // Already completed
                if (m_Completed || m_AsyncOperation == null) {
                    return false;
                }

                if (m_CancellationToken.IsCancellationRequested) {
                    m_Core.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                if (m_Progress != null) {
                    m_Progress.Report(m_AsyncOperation.progress);
                }

                if (m_AsyncOperation.isDone) {
                    m_Core.TrySetResult(m_AsyncOperation.Result);
                    return false;
                }

                return true;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
                m_AsyncOperation.completed -= m_ContinuationAction;
                m_AsyncOperation = default;
                m_Progress = default;
                m_CancellationToken = default;
                m_CancellationTokenRegistration.Dispose();
                m_CancelImmediately = default;
                return s_Pool.TryPush(this);
            }

            private void Continuation(AsyncOperation _) {
                if (m_Completed) {
                    return;
                }

                m_Completed = true;

                if (m_CancellationToken.IsCancellationRequested) {
                    m_Core.TrySetCanceled(m_CancellationToken);
                } else {
                    m_Core.TrySetResult(m_AsyncOperation.Result);
                }
            }
        }
    }
}
