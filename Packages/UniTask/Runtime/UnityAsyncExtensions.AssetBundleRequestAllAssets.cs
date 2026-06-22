#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

#if UNITASK_ASSETBUNDLE_SUPPORT

using Cysharp.Threading.Tasks.Internal;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace Cysharp.Threading.Tasks {
    public static partial class UnityAsyncExtensions {
        public static AssetBundleRequestAllAssetsAwaiter AwaitForAllAssets(this AssetBundleRequest asyncOperation) {
            Error.ThrowArgumentNullException(asyncOperation, nameof(asyncOperation));
            return new AssetBundleRequestAllAssetsAwaiter(asyncOperation);
        }

        public static UniTask<UnityEngine.Object[]> AwaitForAllAssets(
            this AssetBundleRequest asyncOperation,
            CancellationToken cancellationToken
        ) {
            return AwaitForAllAssets(
                asyncOperation,
                null,
                PlayerLoopTiming.Update,
                cancellationToken: cancellationToken
            );
        }

        public static UniTask<UnityEngine.Object[]> AwaitForAllAssets(
            this AssetBundleRequest asyncOperation,
            CancellationToken cancellationToken,
            bool cancelImmediately
        ) {
            return AwaitForAllAssets(
                asyncOperation,
                progress: null,
                cancellationToken: cancellationToken,
                cancelImmediately: cancelImmediately
            );
        }

        public static UniTask<UnityEngine.Object[]> AwaitForAllAssets(
            this AssetBundleRequest asyncOperation,
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
                return UniTask.FromResult(asyncOperation.allAssets);
            }

            return new UniTask<UnityEngine.Object[]>(
                AssetBundleRequestAllAssetsConfiguredSource.Create(
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

        public struct AssetBundleRequestAllAssetsAwaiter : ICriticalNotifyCompletion {
            private AssetBundleRequest m_AsyncOperation;
            private Action<AsyncOperation> m_ContinuationAction;

            public AssetBundleRequestAllAssetsAwaiter(AssetBundleRequest asyncOperation) {
                m_AsyncOperation = asyncOperation;
                m_ContinuationAction = null;
            }

            public AssetBundleRequestAllAssetsAwaiter GetAwaiter() {
                return this;
            }

            public bool IsCompleted => m_AsyncOperation.isDone;

            public UnityEngine.Object[] GetResult() {
                if (m_ContinuationAction != null) {
                    m_AsyncOperation.completed -= m_ContinuationAction;
                    m_ContinuationAction = null;
                    var result = m_AsyncOperation.allAssets;
                    m_AsyncOperation = null;
                    return result;
                } else {
                    var result = m_AsyncOperation.allAssets;
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

        private sealed class AssetBundleRequestAllAssetsConfiguredSource : IUniTaskSource<UnityEngine.Object[]>,
                                                                           IPlayerLoopItem,
                                                                           ITaskPoolNode<
                                                                               AssetBundleRequestAllAssetsConfiguredSource> {
            private static TaskPool<AssetBundleRequestAllAssetsConfiguredSource> s_Pool;
            private AssetBundleRequestAllAssetsConfiguredSource m_NextNode;
            public ref AssetBundleRequestAllAssetsConfiguredSource NextNode => ref m_NextNode;

            static AssetBundleRequestAllAssetsConfiguredSource() {
                TaskPool.RegisterSizeGetter(typeof(AssetBundleRequestAllAssetsConfiguredSource), () => s_Pool.Size);
            }

            private AssetBundleRequest m_AsyncOperation;
            private IProgress<float> m_Progress;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private bool m_CancelImmediately;
            private bool m_Completed;

            private UniTaskCompletionSourceCore<UnityEngine.Object[]> m_Core;

            private Action<AsyncOperation> m_ContinuationAction;

            private AssetBundleRequestAllAssetsConfiguredSource() {
                m_ContinuationAction = Continuation;
            }

            public static IUniTaskSource<UnityEngine.Object[]> Create(
                AssetBundleRequest asyncOperation,
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
                    result = new AssetBundleRequestAllAssetsConfiguredSource();
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
                            var source = (AssetBundleRequestAllAssetsConfiguredSource)state;
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
                    m_Core.TrySetResult(m_AsyncOperation.allAssets);
                    return false;
                }

                return true;
            }

            private bool TryReturn() {
                TaskTracker.RemoveTracking(this);
                m_Core.Reset();
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
                    m_Core.TrySetResult(m_AsyncOperation.allAssets);
                }
            }
        }
    }
}

#endif