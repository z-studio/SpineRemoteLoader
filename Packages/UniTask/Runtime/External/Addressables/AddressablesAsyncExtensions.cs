// asmdef Version Defines, enabled when com.unity.addressables is imported.

#if UNITASK_ADDRESSABLE_SUPPORT

using Cysharp.Threading.Tasks.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Cysharp.Threading.Tasks {
    public static class AddressablesAsyncExtensions {
        #region AsyncOperationHandle

        public static UniTask.Awaiter GetAwaiter(this AsyncOperationHandle handle) {
            return ToUniTask(handle).GetAwaiter();
        }

        public static UniTask WithCancellation(
            this AsyncOperationHandle handle,
            CancellationToken cancellationToken,
            bool cancelImmediately = false,
            bool autoReleaseWhenCanceled = false
        ) {
            return ToUniTask(
                handle,
                cancellationToken: cancellationToken,
                cancelImmediately: cancelImmediately,
                autoReleaseWhenCanceled: autoReleaseWhenCanceled
            );
        }

        public static UniTask ToUniTask(
            this AsyncOperationHandle handle,
            IProgress<float> progress = null,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false,
            bool autoReleaseWhenCanceled = false
        ) {
            if (cancellationToken.IsCancellationRequested) {
                return UniTask.FromCanceled(cancellationToken);
            }

            if (!handle.IsValid()) {
                // autoReleaseHandle:true handle is invalid(immediately internal handle == null) so return completed.
                return UniTask.CompletedTask;
            }

            if (handle.IsDone) {
                if (handle.Status == AsyncOperationStatus.Failed) {
                    return UniTask.FromException(handle.OperationException);
                }

                return UniTask.CompletedTask;
            }

            return new UniTask(
                AsyncOperationHandleConfiguredSource.Create(
                    handle,
                    timing,
                    progress,
                    cancellationToken,
                    cancelImmediately,
                    autoReleaseWhenCanceled,
                    out var token
                ),
                token
            );
        }

        public struct AsyncOperationHandleAwaiter : ICriticalNotifyCompletion {
            private AsyncOperationHandle m_Handle;
            private Action<AsyncOperationHandle> m_ContinuationAction;

            public AsyncOperationHandleAwaiter(AsyncOperationHandle handle) {
                m_Handle = handle;
                m_ContinuationAction = null;
            }

            public bool IsCompleted => m_Handle.IsDone;

            public void GetResult() {
                if (m_ContinuationAction != null) {
                    m_Handle.Completed -= m_ContinuationAction;
                    m_ContinuationAction = null;
                }

                if (m_Handle.Status == AsyncOperationStatus.Failed) {
                    var e = m_Handle.OperationException;
                    m_Handle = default;
                    ExceptionDispatchInfo.Capture(e).Throw();
                }

                var result = m_Handle.Result;
                m_Handle = default;
            }

            public void OnCompleted(Action continuation) {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation) {
                Error.ThrowWhenContinuationIsAlreadyRegistered(m_ContinuationAction);
                m_ContinuationAction = PooledDelegate<AsyncOperationHandle>.Create(continuation);
                m_Handle.Completed += m_ContinuationAction;
            }
        }

        private sealed class AsyncOperationHandleConfiguredSource : IUniTaskSource,
                                                                    IPlayerLoopItem,
                                                                    ITaskPoolNode<AsyncOperationHandleConfiguredSource> {
            private static TaskPool<AsyncOperationHandleConfiguredSource> s_Pool;
            private AsyncOperationHandleConfiguredSource m_NextNode;
            public ref AsyncOperationHandleConfiguredSource NextNode => ref m_NextNode;

            static AsyncOperationHandleConfiguredSource() {
                TaskPool.RegisterSizeGetter(typeof(AsyncOperationHandleConfiguredSource), () => s_Pool.Size);
            }

            private readonly Action<AsyncOperationHandle> m_CompletedCallback;
            private AsyncOperationHandle m_Handle;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private IProgress<float> m_Progress;
            private bool m_AutoReleaseWhenCanceled;
            private bool m_CancelImmediately;
            private bool m_Completed;

            private UniTaskCompletionSourceCore<AsyncUnit> m_Core;

            private AsyncOperationHandleConfiguredSource() {
                m_CompletedCallback = HandleCompleted;
            }

            public static IUniTaskSource Create(
                AsyncOperationHandle handle,
                PlayerLoopTiming timing,
                IProgress<float> progress,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                bool autoReleaseWhenCanceled,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new AsyncOperationHandleConfiguredSource();
                }

                result.m_Handle = handle;
                result.m_Progress = progress;
                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;
                result.m_AutoReleaseWhenCanceled = autoReleaseWhenCanceled;
                result.m_Completed = false;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (AsyncOperationHandleConfiguredSource)state;

                            if (promise.m_AutoReleaseWhenCanceled && promise.m_Handle.IsValid()) {
                                Addressables.Release(promise.m_Handle);
                            }

                            promise.m_Core.TrySetCanceled(promise.m_CancellationToken);
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

            private void HandleCompleted(AsyncOperationHandle _) {
                if (m_Handle.IsValid()) {
                    m_Handle.Completed -= m_CompletedCallback;
                }

                if (m_Completed) {
                    return;
                }

                m_Completed = true;

                if (m_CancellationToken.IsCancellationRequested) {
                    if (m_AutoReleaseWhenCanceled && m_Handle.IsValid()) {
                        Addressables.Release(m_Handle);
                    }

                    m_Core.TrySetCanceled(m_CancellationToken);
                } else if (m_Handle.Status == AsyncOperationStatus.Failed) {
                    m_Core.TrySetException(m_Handle.OperationException);
                } else {
                    m_Core.TrySetResult(AsyncUnit.Default);
                }
            }

            public void GetResult(short token) {
                try {
                    m_Core.GetResult(token);
                } finally {
                    if (!(m_CancelImmediately && m_CancellationToken.IsCancellationRequested)) {
                        TryReturn();
                    } else {
                        TaskTracker.RemoveTracking(this);
                    }
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

                    if (m_AutoReleaseWhenCanceled && m_Handle.IsValid()) {
                        Addressables.Release(m_Handle);
                    }

                    m_Core.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                if (m_Progress != null && m_Handle.IsValid()) {
                    m_Progress.Report(m_Handle.GetDownloadStatus().Percent);
                }

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

        #endregion


        #region AsyncOperationHandle_T

        public static UniTask<T>.Awaiter GetAwaiter<T>(this AsyncOperationHandle<T> handle) {
            return ToUniTask(handle).GetAwaiter();
        }

        public static UniTask<T> WithCancellation<T>(
            this AsyncOperationHandle<T> handle,
            CancellationToken cancellationToken,
            bool cancelImmediately = false,
            bool autoReleaseWhenCanceled = false
        ) {
            return ToUniTask(
                handle,
                cancellationToken: cancellationToken,
                cancelImmediately: cancelImmediately,
                autoReleaseWhenCanceled: autoReleaseWhenCanceled
            );
        }

        public static UniTask<T> ToUniTask<T>(
            this AsyncOperationHandle<T> handle,
            IProgress<float> progress = null,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false,
            bool autoReleaseWhenCanceled = false
        ) {
            if (cancellationToken.IsCancellationRequested)
                return UniTask.FromCanceled<T>(cancellationToken);

            if (!handle.IsValid()) {
                throw new Exception("Attempting to use an invalid operation handle");
            }

            if (handle.IsDone) {
                if (handle.Status == AsyncOperationStatus.Failed) {
                    return UniTask.FromException<T>(handle.OperationException);
                }

                return UniTask.FromResult(handle.Result);
            }

            return new UniTask<T>(
                AsyncOperationHandleConfiguredSource<T>.Create(
                    handle,
                    timing,
                    progress,
                    cancellationToken,
                    cancelImmediately,
                    autoReleaseWhenCanceled,
                    out var token
                ),
                token
            );
        }

        private sealed class AsyncOperationHandleConfiguredSource<T> : IUniTaskSource<T>,
                                                                       IPlayerLoopItem,
                                                                       ITaskPoolNode<AsyncOperationHandleConfiguredSource<T>> {
            private static TaskPool<AsyncOperationHandleConfiguredSource<T>> s_Pool;
            private AsyncOperationHandleConfiguredSource<T> m_NextNode;
            public ref AsyncOperationHandleConfiguredSource<T> NextNode => ref m_NextNode;

            static AsyncOperationHandleConfiguredSource() {
                TaskPool.RegisterSizeGetter(typeof(AsyncOperationHandleConfiguredSource<T>), () => s_Pool.Size);
            }

            private readonly Action<AsyncOperationHandle<T>> m_CompletedCallback;
            private AsyncOperationHandle<T> m_Handle;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private IProgress<float> m_Progress;
            private bool m_AutoReleaseWhenCanceled;
            private bool m_CancelImmediately;
            private bool m_Completed;

            private UniTaskCompletionSourceCore<T> m_Core;

            private AsyncOperationHandleConfiguredSource() {
                m_CompletedCallback = HandleCompleted;
            }

            public static IUniTaskSource<T> Create(
                AsyncOperationHandle<T> handle,
                PlayerLoopTiming timing,
                IProgress<float> progress,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                bool autoReleaseWhenCanceled,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource<T>.CreateFromCanceled(cancellationToken, out token);
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new AsyncOperationHandleConfiguredSource<T>();
                }

                result.m_Handle = handle;
                result.m_CancellationToken = cancellationToken;
                result.m_Completed = false;
                result.m_Progress = progress;
                result.m_AutoReleaseWhenCanceled = autoReleaseWhenCanceled;
                result.m_CancelImmediately = cancelImmediately;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var promise = (AsyncOperationHandleConfiguredSource<T>)state;

                            if (promise.m_AutoReleaseWhenCanceled && promise.m_Handle.IsValid()) {
                                Addressables.Release(promise.m_Handle);
                            }

                            promise.m_Core.TrySetCanceled(promise.m_CancellationToken);
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

            private void HandleCompleted(AsyncOperationHandle<T> argHandle) {
                if (m_Handle.IsValid()) {
                    m_Handle.Completed -= m_CompletedCallback;
                }

                if (m_Completed) {
                    return;
                }

                m_Completed = true;

                if (m_CancellationToken.IsCancellationRequested) {
                    if (m_AutoReleaseWhenCanceled && m_Handle.IsValid()) {
                        Addressables.Release(m_Handle);
                    }

                    m_Core.TrySetCanceled(m_CancellationToken);
                } else if (argHandle.Status == AsyncOperationStatus.Failed) {
                    m_Core.TrySetException(argHandle.OperationException);
                } else {
                    m_Core.TrySetResult(argHandle.Result);
                }
            }

            public T GetResult(short token) {
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
                if (m_Completed) {
                    return false;
                }

                if (m_CancellationToken.IsCancellationRequested) {
                    m_Completed = true;

                    if (m_AutoReleaseWhenCanceled && m_Handle.IsValid()) {
                        Addressables.Release(m_Handle);
                    }

                    m_Core.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                if (m_Progress != null && m_Handle.IsValid()) {
                    m_Progress.Report(m_Handle.GetDownloadStatus().Percent);
                }

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

        #endregion
    }
}

#endif