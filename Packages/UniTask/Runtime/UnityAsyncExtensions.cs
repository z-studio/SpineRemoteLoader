#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks.Internal;
#if ENABLE_UNITYWEBREQUEST && UNITASK_WEBREQUEST_SUPPORT
using UnityEngine.Networking;
#endif

namespace Cysharp.Threading.Tasks {
    public static partial class UnityAsyncExtensions {
        #region AsyncOperation

        public static UniTask WithCancellation(
            this AsyncOperation asyncOperation,
            CancellationToken cancellationToken
        ) {
            return ToUniTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static UniTask WithCancellation(
            this AsyncOperation asyncOperation,
            CancellationToken cancellationToken,
            bool cancelImmediately
        ) {
            return ToUniTask(
                asyncOperation,
                cancellationToken: cancellationToken,
                cancelImmediately: cancelImmediately
            );
        }

        public static UniTask ToUniTask(
            this AsyncOperation asyncOperation,
            IProgress<float> progress = null,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            Error.ThrowArgumentNullException(asyncOperation, nameof(asyncOperation));

            if (cancellationToken.IsCancellationRequested) {
                return UniTask.FromCanceled(cancellationToken);
            }

            if (asyncOperation.isDone) {
                return UniTask.CompletedTask;
            }

            return new UniTask(
                AsyncOperationConfiguredSource.Create(
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

        public struct AsyncOperationAwaiter : ICriticalNotifyCompletion {
            private AsyncOperation m_AsyncOperation;
            private Action<AsyncOperation> m_ContinuationAction;

            public AsyncOperationAwaiter(AsyncOperation asyncOperation) {
                m_AsyncOperation = asyncOperation;
                m_ContinuationAction = null;
            }

            public bool IsCompleted => m_AsyncOperation.isDone;

            public void GetResult() {
                if (m_ContinuationAction != null) {
                    m_AsyncOperation.completed -= m_ContinuationAction;
                    m_ContinuationAction = null;
                    m_AsyncOperation = null;
                } else {
                    m_AsyncOperation = null;
                }
            }

            public void OnCompleted(Action continuation) {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation) {
                Error.ThrowWhenContinuationIsAlreadyRegistered(m_ContinuationAction);
                m_ContinuationAction = PooledDelegate<AsyncOperation>.Create(continuation);
                m_AsyncOperation.completed += m_ContinuationAction;
            }
        }

        private sealed class AsyncOperationConfiguredSource : IUniTaskSource,
                                                              IPlayerLoopItem,
                                                              ITaskPoolNode<AsyncOperationConfiguredSource> {
            private static TaskPool<AsyncOperationConfiguredSource> s_Pool;
            private AsyncOperationConfiguredSource m_NextNode;
            public ref AsyncOperationConfiguredSource NextNode => ref m_NextNode;

            static AsyncOperationConfiguredSource() {
                TaskPool.RegisterSizeGetter(typeof(AsyncOperationConfiguredSource), () => s_Pool.Size);
            }

            private AsyncOperation m_AsyncOperation;
            private IProgress<float> m_Progress;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;
            private bool m_Completed;

            private UniTaskCompletionSourceCore<AsyncUnit> m_Core;

            private Action<AsyncOperation> m_ContinuationAction;

            private AsyncOperationConfiguredSource() {
                m_ContinuationAction = Continuation;
            }

            public static IUniTaskSource Create(
                AsyncOperation asyncOperation,
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
                    result = new AsyncOperationConfiguredSource();
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
                            var source = (AsyncOperationConfiguredSource)state;
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
                    m_Core.TrySetResult(AsyncUnit.Default);
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
                    m_Core.TrySetResult(AsyncUnit.Default);
                }
            }
        }

        #endregion


        #region ResourceRequest

        public static ResourceRequestAwaiter GetAwaiter(this ResourceRequest asyncOperation) {
            Error.ThrowArgumentNullException(asyncOperation, nameof(asyncOperation));
            return new ResourceRequestAwaiter(asyncOperation);
        }

        public static UniTask<UnityEngine.Object> WithCancellation(
            this ResourceRequest asyncOperation,
            CancellationToken cancellationToken
        ) {
            return ToUniTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static UniTask<UnityEngine.Object> WithCancellation(
            this ResourceRequest asyncOperation,
            CancellationToken cancellationToken,
            bool cancelImmediately
        ) {
            return ToUniTask(
                asyncOperation,
                cancellationToken: cancellationToken,
                cancelImmediately: cancelImmediately
            );
        }

        public static UniTask<UnityEngine.Object> ToUniTask(
            this ResourceRequest asyncOperation,
            IProgress<float> progress = null,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            Error.ThrowArgumentNullException(asyncOperation, nameof(asyncOperation));

            if (cancellationToken.IsCancellationRequested) {
                return UniTask.FromCanceled<UnityEngine.Object>(cancellationToken);
            }

            if (asyncOperation.isDone) {
                return UniTask.FromResult(asyncOperation.asset);
            }

            return new UniTask<UnityEngine.Object>(
                ResourceRequestConfiguredSource.Create(
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

        public struct ResourceRequestAwaiter : ICriticalNotifyCompletion {
            private ResourceRequest m_AsyncOperation;
            private Action<AsyncOperation> m_ContinuationAction;

            public ResourceRequestAwaiter(ResourceRequest asyncOperation) {
                m_AsyncOperation = asyncOperation;
                m_ContinuationAction = null;
            }

            public bool IsCompleted => m_AsyncOperation.isDone;

            public UnityEngine.Object GetResult() {
                if (m_ContinuationAction != null) {
                    m_AsyncOperation.completed -= m_ContinuationAction;
                    m_ContinuationAction = null;
                    var result = m_AsyncOperation.asset;
                    m_AsyncOperation = null;
                    return result;
                } else {
                    var result = m_AsyncOperation.asset;
                    m_AsyncOperation = null;
                    return result;
                }
            }

            public void OnCompleted(Action continuation) {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation) {
                Error.ThrowWhenContinuationIsAlreadyRegistered(m_ContinuationAction);
                m_ContinuationAction = PooledDelegate<AsyncOperation>.Create(continuation);
                m_AsyncOperation.completed += m_ContinuationAction;
            }
        }

        private sealed class ResourceRequestConfiguredSource : IUniTaskSource<UnityEngine.Object>,
                                                               IPlayerLoopItem,
                                                               ITaskPoolNode<ResourceRequestConfiguredSource> {
            private static TaskPool<ResourceRequestConfiguredSource> s_Pool;
            private ResourceRequestConfiguredSource m_NextNode;
            public ref ResourceRequestConfiguredSource NextNode => ref m_NextNode;

            static ResourceRequestConfiguredSource() {
                TaskPool.RegisterSizeGetter(typeof(ResourceRequestConfiguredSource), () => s_Pool.Size);
            }

            private ResourceRequest m_AsyncOperation;
            private IProgress<float> m_Progress;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;
            private bool m_Completed;

            private UniTaskCompletionSourceCore<UnityEngine.Object> m_Core;

            private Action<AsyncOperation> m_ContinuationAction;

            private ResourceRequestConfiguredSource() {
                m_ContinuationAction = Continuation;
            }

            public static IUniTaskSource<UnityEngine.Object> Create(
                ResourceRequest asyncOperation,
                PlayerLoopTiming timing,
                IProgress<float> progress,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource<UnityEngine.Object>.CreateFromCanceled(
                        cancellationToken,
                        out token
                    );
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new ResourceRequestConfiguredSource();
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
                            var source = (ResourceRequestConfiguredSource)state;
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

            public UnityEngine.Object GetResult(short token) {
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
                    m_Core.TrySetResult(m_AsyncOperation.asset);
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
                    m_Core.TrySetResult(m_AsyncOperation.asset);
                }
            }
        }

        #endregion


#if UNITASK_ASSETBUNDLE_SUPPORT


        #region AssetBundleRequest

        public static AssetBundleRequestAwaiter GetAwaiter(this AssetBundleRequest asyncOperation) {
            Error.ThrowArgumentNullException(asyncOperation, nameof(asyncOperation));
            return new AssetBundleRequestAwaiter(asyncOperation);
        }

        public static UniTask<UnityEngine.Object> WithCancellation(
            this AssetBundleRequest asyncOperation,
            CancellationToken cancellationToken
        ) {
            return ToUniTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static UniTask<UnityEngine.Object> WithCancellation(
            this AssetBundleRequest asyncOperation,
            CancellationToken cancellationToken,
            bool cancelImmediately
        ) {
            return ToUniTask(
                asyncOperation,
                cancellationToken: cancellationToken,
                cancelImmediately: cancelImmediately
            );
        }

        public static UniTask<UnityEngine.Object> ToUniTask(
            this AssetBundleRequest asyncOperation,
            IProgress<float> progress = null,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            Error.ThrowArgumentNullException(asyncOperation, nameof(asyncOperation));

            if (cancellationToken.IsCancellationRequested) {
                return UniTask.FromCanceled<UnityEngine.Object>(cancellationToken);
            }

            if (asyncOperation.isDone) {
                return UniTask.FromResult(asyncOperation.asset);
            }

            return new UniTask<UnityEngine.Object>(
                AssetBundleRequestConfiguredSource.Create(
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

        public struct AssetBundleRequestAwaiter : ICriticalNotifyCompletion {
            private AssetBundleRequest m_AsyncOperation;
            private Action<AsyncOperation> m_ContinuationAction;

            public AssetBundleRequestAwaiter(AssetBundleRequest asyncOperation) {
                m_AsyncOperation = asyncOperation;
                m_ContinuationAction = null;
            }

            public bool IsCompleted => m_AsyncOperation.isDone;

            public UnityEngine.Object GetResult() {
                if (m_ContinuationAction != null) {
                    m_AsyncOperation.completed -= m_ContinuationAction;
                    m_ContinuationAction = null;
                    var result = m_AsyncOperation.asset;
                    m_AsyncOperation = null;
                    return result;
                } else {
                    var result = m_AsyncOperation.asset;
                    m_AsyncOperation = null;
                    return result;
                }
            }

            public void OnCompleted(Action continuation) {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation) {
                Error.ThrowWhenContinuationIsAlreadyRegistered(m_ContinuationAction);
                m_ContinuationAction = PooledDelegate<AsyncOperation>.Create(continuation);
                m_AsyncOperation.completed += m_ContinuationAction;
            }
        }

        private sealed class AssetBundleRequestConfiguredSource : IUniTaskSource<UnityEngine.Object>,
                                                                  IPlayerLoopItem,
                                                                  ITaskPoolNode<AssetBundleRequestConfiguredSource> {
            private static TaskPool<AssetBundleRequestConfiguredSource> s_Pool;
            private AssetBundleRequestConfiguredSource m_NextNode;
            public ref AssetBundleRequestConfiguredSource NextNode => ref m_NextNode;

            static AssetBundleRequestConfiguredSource() {
                TaskPool.RegisterSizeGetter(typeof(AssetBundleRequestConfiguredSource), () => s_Pool.Size);
            }

            private AssetBundleRequest m_AsyncOperation;
            private IProgress<float> m_Progress;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;
            private bool m_Completed;

            private UniTaskCompletionSourceCore<UnityEngine.Object> m_Core;

            private Action<AsyncOperation> m_ContinuationAction;

            private AssetBundleRequestConfiguredSource() {
                m_ContinuationAction = Continuation;
            }

            public static IUniTaskSource<UnityEngine.Object> Create(
                AssetBundleRequest asyncOperation,
                PlayerLoopTiming timing,
                IProgress<float> progress,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource<UnityEngine.Object>.CreateFromCanceled(
                        cancellationToken,
                        out token
                    );
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new AssetBundleRequestConfiguredSource();
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
                            var source = (AssetBundleRequestConfiguredSource)state;
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

            public UnityEngine.Object GetResult(short token) {
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
                    m_Core.TrySetResult(m_AsyncOperation.asset);
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
                    m_Core.TrySetResult(m_AsyncOperation.asset);
                }
            }
        }

        #endregion


#endif

#if UNITASK_ASSETBUNDLE_SUPPORT


        #region AssetBundleCreateRequest

        public static AssetBundleCreateRequestAwaiter GetAwaiter(this AssetBundleCreateRequest asyncOperation) {
            Error.ThrowArgumentNullException(asyncOperation, nameof(asyncOperation));
            return new AssetBundleCreateRequestAwaiter(asyncOperation);
        }

        public static UniTask<AssetBundle> WithCancellation(
            this AssetBundleCreateRequest asyncOperation,
            CancellationToken cancellationToken
        ) {
            return ToUniTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static UniTask<AssetBundle> WithCancellation(
            this AssetBundleCreateRequest asyncOperation,
            CancellationToken cancellationToken,
            bool cancelImmediately
        ) {
            return ToUniTask(
                asyncOperation,
                cancellationToken: cancellationToken,
                cancelImmediately: cancelImmediately
            );
        }

        public static UniTask<AssetBundle> ToUniTask(
            this AssetBundleCreateRequest asyncOperation,
            IProgress<float> progress = null,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            Error.ThrowArgumentNullException(asyncOperation, nameof(asyncOperation));

            if (cancellationToken.IsCancellationRequested) {
                return UniTask.FromCanceled<AssetBundle>(cancellationToken);
            }

            if (asyncOperation.isDone) {
                return UniTask.FromResult(asyncOperation.assetBundle);
            }

            return new UniTask<AssetBundle>(
                AssetBundleCreateRequestConfiguredSource.Create(
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

        public struct AssetBundleCreateRequestAwaiter : ICriticalNotifyCompletion {
            private AssetBundleCreateRequest m_AsyncOperation;
            private Action<AsyncOperation> m_ContinuationAction;

            public AssetBundleCreateRequestAwaiter(AssetBundleCreateRequest asyncOperation) {
                m_AsyncOperation = asyncOperation;
                m_ContinuationAction = null;
            }

            public bool IsCompleted => m_AsyncOperation.isDone;

            public AssetBundle GetResult() {
                if (m_ContinuationAction != null) {
                    m_AsyncOperation.completed -= m_ContinuationAction;
                    m_ContinuationAction = null;
                    var result = m_AsyncOperation.assetBundle;
                    m_AsyncOperation = null;
                    return result;
                } else {
                    var result = m_AsyncOperation.assetBundle;
                    m_AsyncOperation = null;
                    return result;
                }
            }

            public void OnCompleted(Action continuation) {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation) {
                Error.ThrowWhenContinuationIsAlreadyRegistered(m_ContinuationAction);
                m_ContinuationAction = PooledDelegate<AsyncOperation>.Create(continuation);
                m_AsyncOperation.completed += m_ContinuationAction;
            }
        }

        private sealed class AssetBundleCreateRequestConfiguredSource : IUniTaskSource<AssetBundle>,
                                                                        IPlayerLoopItem,
                                                                        ITaskPoolNode<
                                                                            AssetBundleCreateRequestConfiguredSource> {
            private static TaskPool<AssetBundleCreateRequestConfiguredSource> s_Pool;
            private AssetBundleCreateRequestConfiguredSource m_NextNode;
            public ref AssetBundleCreateRequestConfiguredSource NextNode => ref m_NextNode;

            static AssetBundleCreateRequestConfiguredSource() {
                TaskPool.RegisterSizeGetter(typeof(AssetBundleCreateRequestConfiguredSource), () => s_Pool.Size);
            }

            private AssetBundleCreateRequest m_AsyncOperation;
            private IProgress<float> m_Progress;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;
            private bool m_Completed;

            private UniTaskCompletionSourceCore<AssetBundle> m_Core;

            private Action<AsyncOperation> m_ContinuationAction;

            private AssetBundleCreateRequestConfiguredSource() {
                m_ContinuationAction = Continuation;
            }

            public static IUniTaskSource<AssetBundle> Create(
                AssetBundleCreateRequest asyncOperation,
                PlayerLoopTiming timing,
                IProgress<float> progress,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource<AssetBundle>.CreateFromCanceled(
                        cancellationToken,
                        out token
                    );
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new AssetBundleCreateRequestConfiguredSource();
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
                            var source = (AssetBundleCreateRequestConfiguredSource)state;
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

            public AssetBundle GetResult(short token) {
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
                    m_Core.TrySetResult(m_AsyncOperation.assetBundle);
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
                    m_Core.TrySetResult(m_AsyncOperation.assetBundle);
                }
            }
        }

        #endregion


#endif

#if ENABLE_UNITYWEBREQUEST && UNITASK_WEBREQUEST_SUPPORT


        #region UnityWebRequestAsyncOperation

        public static UnityWebRequestAsyncOperationAwaiter GetAwaiter(
            this UnityWebRequestAsyncOperation asyncOperation
        ) {
            Error.ThrowArgumentNullException(asyncOperation, nameof(asyncOperation));
            return new UnityWebRequestAsyncOperationAwaiter(asyncOperation);
        }

        public static UniTask<UnityWebRequest> WithCancellation(
            this UnityWebRequestAsyncOperation asyncOperation,
            CancellationToken cancellationToken
        ) {
            return ToUniTask(asyncOperation, cancellationToken: cancellationToken);
        }

        public static UniTask<UnityWebRequest> WithCancellation(
            this UnityWebRequestAsyncOperation asyncOperation,
            CancellationToken cancellationToken,
            bool cancelImmediately
        ) {
            return ToUniTask(
                asyncOperation,
                cancellationToken: cancellationToken,
                cancelImmediately: cancelImmediately
            );
        }

        public static UniTask<UnityWebRequest> ToUniTask(
            this UnityWebRequestAsyncOperation asyncOperation,
            IProgress<float> progress = null,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default(CancellationToken),
            bool cancelImmediately = false
        ) {
            Error.ThrowArgumentNullException(asyncOperation, nameof(asyncOperation));

            if (cancellationToken.IsCancellationRequested) {
                return UniTask.FromCanceled<UnityWebRequest>(cancellationToken);
            }

            if (asyncOperation.isDone) {
                if (asyncOperation.webRequest.IsError()) {
                    return UniTask.FromException<UnityWebRequest>(
                        new UnityWebRequestException(asyncOperation.webRequest)
                    );
                }

                return UniTask.FromResult(asyncOperation.webRequest);
            }

            return new UniTask<UnityWebRequest>(
                UnityWebRequestAsyncOperationConfiguredSource.Create(
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

        public struct UnityWebRequestAsyncOperationAwaiter : ICriticalNotifyCompletion {
            private UnityWebRequestAsyncOperation m_AsyncOperation;
            private Action<AsyncOperation> m_ContinuationAction;

            public UnityWebRequestAsyncOperationAwaiter(UnityWebRequestAsyncOperation asyncOperation) {
                m_AsyncOperation = asyncOperation;
                m_ContinuationAction = null;
            }

            public bool IsCompleted => m_AsyncOperation.isDone;

            public UnityWebRequest GetResult() {
                if (m_ContinuationAction != null) {
                    m_AsyncOperation.completed -= m_ContinuationAction;
                    m_ContinuationAction = null;
                    var result = m_AsyncOperation.webRequest;
                    m_AsyncOperation = null;

                    if (result.IsError()) {
                        throw new UnityWebRequestException(result);
                    }

                    return result;
                } else {
                    var result = m_AsyncOperation.webRequest;
                    m_AsyncOperation = null;

                    if (result.IsError()) {
                        throw new UnityWebRequestException(result);
                    }

                    return result;
                }
            }

            public void OnCompleted(Action continuation) {
                UnsafeOnCompleted(continuation);
            }

            public void UnsafeOnCompleted(Action continuation) {
                Error.ThrowWhenContinuationIsAlreadyRegistered(m_ContinuationAction);
                m_ContinuationAction = PooledDelegate<AsyncOperation>.Create(continuation);
                m_AsyncOperation.completed += m_ContinuationAction;
            }
        }

        private sealed class UnityWebRequestAsyncOperationConfiguredSource : IUniTaskSource<UnityWebRequest>,
                                                                             IPlayerLoopItem,
                                                                             ITaskPoolNode<
                                                                                 UnityWebRequestAsyncOperationConfiguredSource> {
            private static TaskPool<UnityWebRequestAsyncOperationConfiguredSource> s_Pool;
            private UnityWebRequestAsyncOperationConfiguredSource m_NextNode;
            public ref UnityWebRequestAsyncOperationConfiguredSource NextNode => ref m_NextNode;

            static UnityWebRequestAsyncOperationConfiguredSource() {
                TaskPool.RegisterSizeGetter(typeof(UnityWebRequestAsyncOperationConfiguredSource), () => s_Pool.Size);
            }

            private UnityWebRequestAsyncOperation m_AsyncOperation;
            private IProgress<float> m_Progress;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;
            private bool m_Completed;

            private UniTaskCompletionSourceCore<UnityWebRequest> m_Core;

            private Action<AsyncOperation> m_ContinuationAction;

            private UnityWebRequestAsyncOperationConfiguredSource() {
                m_ContinuationAction = Continuation;
            }

            public static IUniTaskSource<UnityWebRequest> Create(
                UnityWebRequestAsyncOperation asyncOperation,
                PlayerLoopTiming timing,
                IProgress<float> progress,
                CancellationToken cancellationToken,
                bool cancelImmediately,
                out short token
            ) {
                if (cancellationToken.IsCancellationRequested) {
                    return AutoResetUniTaskCompletionSource<UnityWebRequest>.CreateFromCanceled(
                        cancellationToken,
                        out token
                    );
                }

                if (!s_Pool.TryPop(out var result)) {
                    result = new UnityWebRequestAsyncOperationConfiguredSource();
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
                            var source = (UnityWebRequestAsyncOperationConfiguredSource)state;
                            source.m_AsyncOperation.webRequest.Abort();
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

            public UnityWebRequest GetResult(short token) {
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
                    m_AsyncOperation.webRequest.Abort();
                    m_Core.TrySetCanceled(m_CancellationToken);
                    return false;
                }

                if (m_Progress != null) {
                    m_Progress.Report(m_AsyncOperation.progress);
                }

                if (m_AsyncOperation.isDone) {
                    if (m_AsyncOperation.webRequest.IsError()) {
                        m_Core.TrySetException(new UnityWebRequestException(m_AsyncOperation.webRequest));
                    } else {
                        m_Core.TrySetResult(m_AsyncOperation.webRequest);
                    }

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
                } else if (m_AsyncOperation.webRequest.IsError()) {
                    m_Core.TrySetException(new UnityWebRequestException(m_AsyncOperation.webRequest));
                } else {
                    m_Core.TrySetResult(m_AsyncOperation.webRequest);
                }
            }
        }

        #endregion


#endif
    }
}