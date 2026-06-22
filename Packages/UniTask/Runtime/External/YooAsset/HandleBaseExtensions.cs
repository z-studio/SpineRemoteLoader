#if UNITASK_YOOASSET_SUPPORT
#if UNITY_2020_1_OR_NEWER && !UNITY_2021
#define UNITY_2020_BUG
#endif

using System;
using System.Threading;
using YooAsset;

namespace Cysharp.Threading.Tasks {
    public static class HandleBaseExtensions {
        public static UniTask.Awaiter GetAwaiter(this HandleBase handle) {
            return ToUniTask(handle).GetAwaiter();
        }

        public static UniTask WithCancellation(
            this HandleBase handle,
            CancellationToken cancellationToken,
            bool cancelImmediately = false
        ) {
            return ToUniTask(handle, cancellationToken: cancellationToken, cancelImmediately: cancelImmediately);
        }

        public static UniTask ToUniTask(
            this HandleBase handle,
            IProgress<float> progress = null,
            PlayerLoopTiming timing = PlayerLoopTiming.Update,
            CancellationToken cancellationToken = default,
            bool cancelImmediately = false
        ) {
            if (cancellationToken.IsCancellationRequested) {
                return UniTask.FromCanceled(cancellationToken);
            }

            if (!handle.IsValid || handle.IsDone) {
                return UniTask.CompletedTask;
            }

            return new UniTask(
                HandleBaseConfiguredSource.Create(
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

        private sealed class HandleBaseConfiguredSource : IUniTaskSource,
                                                          IPlayerLoopItem,
                                                          ITaskPoolNode<HandleBaseConfiguredSource> {
            private static TaskPool<HandleBaseConfiguredSource> s_Pool;
            private HandleBaseConfiguredSource m_NextNode;
            public ref HandleBaseConfiguredSource NextNode => ref m_NextNode;

            static HandleBaseConfiguredSource() {
                TaskPool.RegisterSizeGetter(typeof(HandleBaseConfiguredSource), () => s_Pool.Size);
            }

            private readonly Action<HandleBase> m_CompletedCallback;
            private HandleBase m_Handle;
            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private IProgress<float> m_Progress;
            private bool m_CancelImmediately;
            private bool m_Completed;

            private UniTaskCompletionSourceCore<AsyncUnit> m_Core;

            private HandleBaseConfiguredSource() {
                m_CompletedCallback = HandleCompleted;
            }

            public static IUniTaskSource Create(
                HandleBase handle,
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
                    result = new HandleBaseConfiguredSource();
                }

                result.m_Handle = handle;
                result.m_Progress = progress;
                result.m_CancellationToken = cancellationToken;
                result.m_CancelImmediately = cancelImmediately;
                result.m_Completed = false;

                if (cancelImmediately && cancellationToken.CanBeCanceled) {
                    result.m_CancellationTokenRegistration = cancellationToken.RegisterWithoutCaptureExecutionContext(
                        state => {
                            var source = (HandleBaseConfiguredSource)state;
                            source.m_Core.TrySetCanceled(source.m_CancellationToken);
                        },
                        result
                    );
                }

                TaskTracker.TrackActiveTask(result, 3);
                PlayerLoopHelper.AddAction(timing, result);

                // BUG 在 Unity 2020.3.36 版本测试中, IL2Cpp 会报 如下错误
                // BUG ArgumentException: Incompatible Delegate Types. First is System.Action`1[[YooAsset.AssetHandle, YooAsset, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]] second is System.Action`1[[YooAsset.HandleBase, YooAsset, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null]]
                // BUG 也可能报的是 Action '1' Action '1' 的 InvalidCastException
                // BUG 此处不得不这么修改, 如果后续 Unity 修复了这个问题, 可以恢复之前的写法
#if UNITY_2020_BUG
                switch (handle) {
                    case AssetHandle assetHandle:
                        assetHandle.Completed += result.AssetContinuation;
                        break;
                    case SceneHandle sceneHandle:
                        sceneHandle.Completed += result.SceneContinuation;
                        break;
                    case SubAssetsHandle subAssetHandle:
                        subAssetHandle.Completed += result.SubContinuation;
                        break;
                    case RawFileHandle rawFileHandle:
                        rawFileHandle.Completed += result.RawFileContinuation;
                        break;
                    case AllAssetsHandle allAssetsHandle:
                        allAssetsHandle.Completed += result.AllAssetsContinuation;
                        break;
                }
#else
                switch (handle) {
                    case AssetHandle assetHandle:
                        assetHandle.Completed += result.m_CompletedCallback;
                        break;
                    case SceneHandle sceneHandle:
                        sceneHandle.Completed += result.m_CompletedCallback;
                        break;
                    case SubAssetsHandle subAssetHandle:
                        subAssetHandle.Completed += result.m_CompletedCallback;
                        break;
                    case RawFileHandle rawFileHandle:
                        rawFileHandle.Completed += result.m_CompletedCallback;
                        break;
                    case AllAssetsHandle allAssetsHandle:
                        allAssetsHandle.Completed += result.m_CompletedCallback;
                        break;
                }
#endif

                token = result.m_Core.Version;
                return result;
            }

#if UNITY_2020_BUG
            private void AssetContinuation(AssetHandle _) => HandleCompleted(null);
            private void SceneContinuation(SceneHandle _) => HandleCompleted(null);
            private void SubContinuation(SubAssetsHandle _) => HandleCompleted(null);
            private void RawFileContinuation(RawFileHandle _) => HandleCompleted(null);
            private void AllAssetsContinuation(AllAssetsHandle _) => HandleCompleted(null);
#endif

            private void HandleCompleted(HandleBase _) {
                RemoveCompleted();

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

            private void RemoveCompleted() {
                if (m_Handle == null || !m_Handle.IsValid) {
                    return;
                }

#if UNITY_2020_BUG
                switch (m_Handle) {
                    case AssetHandle assetHandle:
                        assetHandle.Completed -= AssetContinuation;
                        break;
                    case SceneHandle sceneHandle:
                        sceneHandle.Completed -= SceneContinuation;
                        break;
                    case SubAssetsHandle subAssetHandle:
                        subAssetHandle.Completed -= SubContinuation;
                        break;
                    case RawFileHandle rawFileHandle:
                        rawFileHandle.Completed -= RawFileContinuation;
                        break;
                    case AllAssetsHandle allAssetsHandle:
                        allAssetsHandle.Completed -= AllAssetsContinuation;
                        break;
                }
#else
                switch (m_Handle) {
                    case AssetHandle assetHandle:
                        assetHandle.Completed -= m_CompletedCallback;
                        break;
                    case SceneHandle sceneHandle:
                        sceneHandle.Completed -= m_CompletedCallback;
                        break;
                    case SubAssetsHandle subAssetHandle:
                        subAssetHandle.Completed -= m_CompletedCallback;
                        break;
                    case RawFileHandle rawFileHandle:
                        rawFileHandle.Completed -= m_CompletedCallback;
                        break;
                    case AllAssetsHandle allAssetsHandle:
                        allAssetsHandle.Completed -= m_CompletedCallback;
                        break;
                }
#endif
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

                if (m_Handle == null || !m_Handle.IsValid || m_Handle.IsDone) {
                    m_Completed = true;
                    m_Core.TrySetResult(AsyncUnit.Default);
                    return false;
                }

                if (m_Progress != null && m_Handle.IsValid) {
                    m_Progress.Report(m_Handle.Progress);
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
    }
}
#endif