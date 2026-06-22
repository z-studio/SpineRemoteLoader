#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks.Internal;

namespace Cysharp.Threading.Tasks {
    public partial struct UniTask {
        public static UniTask<T[]> WhenAll<T>(params UniTask<T>[] tasks) {
            if (tasks.Length == 0) {
                return UniTask.FromResult(Array.Empty<T>());
            }

            return new UniTask<T[]>(new WhenAllPromise<T>(tasks, tasks.Length), 0);
        }

        public static UniTask<T[]> WhenAll<T>(IEnumerable<UniTask<T>> tasks) {
            using (var span = ArrayPoolUtil.Materialize(tasks)) {
                var promise = new WhenAllPromise<T>(span.Array, span.Length); // consumed array in constructor.
                return new UniTask<T[]>(promise, 0);
            }
        }

        public static UniTask WhenAll(params UniTask[] tasks) {
            if (tasks.Length == 0) {
                return UniTask.CompletedTask;
            }

            return new UniTask(new WhenAllPromise(tasks, tasks.Length), 0);
        }

        public static UniTask WhenAll(IEnumerable<UniTask> tasks) {
            using (var span = ArrayPoolUtil.Materialize(tasks)) {
                var promise = new WhenAllPromise(span.Array, span.Length); // consumed array in constructor.
                return new UniTask(promise, 0);
            }
        }

        private sealed class WhenAllPromise<T> : IUniTaskSource<T[]> {
            private T[] m_Result;
            private int m_CompleteCount;
            private UniTaskCompletionSourceCore<T[]> m_Core; // don't reset(called after GetResult, will invoke TrySetException.)

            public WhenAllPromise(UniTask<T>[] tasks, int tasksLength) {
                TaskTracker.TrackActiveTask(this, 3);

                m_CompleteCount = 0;

                if (tasksLength == 0) {
                    m_Result = Array.Empty<T>();
                    m_Core.TrySetResult(m_Result);
                    return;
                }

                m_Result = new T[tasksLength];

                for (var i = 0; i < tasksLength; i++) {
                    UniTask<T>.Awaiter awaiter;

                    try {
                        awaiter = tasks[i].GetAwaiter();
                    } catch (Exception ex) {
                        m_Core.TrySetException(ex);
                        continue;
                    }

                    if (awaiter.IsCompleted) {
                        TryInvokeContinuation(this, awaiter, i);
                    } else {
                        awaiter.SourceOnCompleted(
                            state => {
                                using (var t = (StateTuple<WhenAllPromise<T>, UniTask<T>.Awaiter, int>)state) {
                                    TryInvokeContinuation(t.Item1, t.Item2, t.Item3);
                                }
                            },
                            StateTuple.Create(this, awaiter, i)
                        );
                    }
                }
            }

            private static void TryInvokeContinuation(WhenAllPromise<T> self, in UniTask<T>.Awaiter awaiter, int i) {
                try {
                    self.m_Result[i] = awaiter.GetResult();
                } catch (Exception ex) {
                    self.m_Core.TrySetException(ex);
                    return;
                }

                if (Interlocked.Increment(ref self.m_CompleteCount) == self.m_Result.Length) {
                    self.m_Core.TrySetResult(self.m_Result);
                }
            }

            public T[] GetResult(short token) {
                TaskTracker.RemoveTracking(this);
                GC.SuppressFinalize(this);
                return m_Core.GetResult(token);
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
        }

        private sealed class WhenAllPromise : IUniTaskSource {
            private int m_CompleteCount;
            private int m_TasksLength;

            private UniTaskCompletionSourceCore<AsyncUnit> m_Core; // don't reset(called after GetResult, will invoke TrySetException.)

            public WhenAllPromise(UniTask[] tasks, int tasksLength) {
                TaskTracker.TrackActiveTask(this, 3);

                m_TasksLength = tasksLength;
                m_CompleteCount = 0;

                if (tasksLength == 0) {
                    m_Core.TrySetResult(AsyncUnit.Default);
                    return;
                }

                for (var i = 0; i < tasksLength; i++) {
                    UniTask.Awaiter awaiter;

                    try {
                        awaiter = tasks[i].GetAwaiter();
                    } catch (Exception ex) {
                        m_Core.TrySetException(ex);
                        continue;
                    }

                    if (awaiter.IsCompleted) {
                        TryInvokeContinuation(this, awaiter);
                    } else {
                        awaiter.SourceOnCompleted(
                            state => {
                                using (var t = (StateTuple<WhenAllPromise, UniTask.Awaiter>)state) {
                                    TryInvokeContinuation(t.Item1, t.Item2);
                                }
                            },
                            StateTuple.Create(this, awaiter)
                        );
                    }
                }
            }

            private static void TryInvokeContinuation(WhenAllPromise self, in UniTask.Awaiter awaiter) {
                try {
                    awaiter.GetResult();
                } catch (Exception ex) {
                    self.m_Core.TrySetException(ex);
                    return;
                }

                if (Interlocked.Increment(ref self.m_CompleteCount) == self.m_TasksLength) {
                    self.m_Core.TrySetResult(AsyncUnit.Default);
                }
            }

            public void GetResult(short token) {
                TaskTracker.RemoveTracking(this);
                GC.SuppressFinalize(this);
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
        }
    }
}