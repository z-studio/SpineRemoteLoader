#if ENABLE_MANAGED_JOBS
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Threading;
using Unity.Jobs;

namespace Cysharp.Threading.Tasks {
    public static partial class UnityAsyncExtensions {
        public static async UniTask WaitAsync(
            this JobHandle jobHandle,
            PlayerLoopTiming waitTiming,
            CancellationToken cancellationToken = default
        ) {
            await UniTask.Yield(waitTiming);
            jobHandle.Complete();
            cancellationToken.ThrowIfCancellationRequested(); // call cancel after Complete.
        }

        public static UniTask.Awaiter GetAwaiter(this JobHandle jobHandle) {
            var handler = JobHandlePromise.Create(jobHandle, out var token);

            {
                PlayerLoopHelper.AddAction(PlayerLoopTiming.EarlyUpdate, handler);
                PlayerLoopHelper.AddAction(PlayerLoopTiming.PreUpdate, handler);
                PlayerLoopHelper.AddAction(PlayerLoopTiming.Update, handler);
                PlayerLoopHelper.AddAction(PlayerLoopTiming.PreLateUpdate, handler);
                PlayerLoopHelper.AddAction(PlayerLoopTiming.PostLateUpdate, handler);
            }

            return new UniTask(handler, token).GetAwaiter();
        }

        // can not pass CancellationToken because can't handle JobHandle's Complete and NativeArray.Dispose.

        public static UniTask ToUniTask(this JobHandle jobHandle, PlayerLoopTiming waitTiming) {
            var handler = JobHandlePromise.Create(jobHandle, out var token);

            {
                PlayerLoopHelper.AddAction(waitTiming, handler);
            }

            return new UniTask(handler, token);
        }

        private sealed class JobHandlePromise : IUniTaskSource, IPlayerLoopItem {
            private JobHandle m_JobHandle;
            private UniTaskCompletionSourceCore<AsyncUnit> m_Core;

            // Cancellation is not supported.
            public static JobHandlePromise Create(JobHandle jobHandle, out short token) {
                // not use pool.
                var result = new JobHandlePromise();
                result.m_JobHandle = jobHandle;

                TaskTracker.TrackActiveTask(result, 3);

                token = result.m_Core.Version;
                return result;
            }

            public void GetResult(short token) {
                TaskTracker.RemoveTracking(this);
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

            public bool MoveNext() {
                if (m_JobHandle.IsCompleted | PlayerLoopHelper.IsEditorApplicationQuitting) {
                    m_JobHandle.Complete();
                    m_Core.TrySetResult(AsyncUnit.Default);
                    return false;
                }

                return true;
            }
        }
    }
}

#endif