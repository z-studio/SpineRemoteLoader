#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable CS0436

#if UNITASK_NETCORE || UNITY_2022_3_OR_NEWER
#define SUPPORT_VALUETASK
#endif

using Cysharp.Threading.Tasks.CompilerServices;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Cysharp.Threading.Tasks {
    internal static class AwaiterActions {
        internal static readonly Action<object> InvokeContinuationDelegate = Continuation;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Continuation(object state) {
            ((Action)state).Invoke();
        }
    }

    /// <summary>
    /// Lightweight unity specified task-like object.
    /// </summary>
    [AsyncMethodBuilder(typeof(AsyncUniTaskMethodBuilder))]
    [StructLayout(LayoutKind.Auto)]
    public readonly partial struct UniTask {
        private readonly IUniTaskSource source;
        private readonly short token;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask(IUniTaskSource source, short token) {
            this.source = source;
            this.token = token;
        }

        public UniTaskStatus Status {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                if (source == null) {
                    return UniTaskStatus.Succeeded;
                }

                return source.GetStatus(token);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Awaiter GetAwaiter() {
            return new Awaiter(this);
        }

        /// <summary>
        /// returns (bool IsCanceled) instead of throws OperationCanceledException.
        /// </summary>
        public UniTask<bool> SuppressCancellationThrow() {
            var status = Status;

            if (status == UniTaskStatus.Succeeded) {
                return CompletedTasks.False;
            }

            if (status == UniTaskStatus.Canceled) {
                return CompletedTasks.True;
            }

            return new UniTask<bool>(new IsCanceledSource(source), token);
        }

#if SUPPORT_VALUETASK

        public static implicit operator System.Threading.Tasks.ValueTask(in UniTask self) {
            if (self.source == null) {
                return default;
            }

#if (UNITASK_NETCORE && NETSTANDARD2_0)
            return self.AsValueTask();
#else
            return new System.Threading.Tasks.ValueTask(self.source, self.token);
#endif
        }

#endif

        public override string ToString() {
            if (source == null) {
                return "()";
            }

            return "(" + source.UnsafeGetStatus() + ")";
        }

        /// <summary>
        /// Memoizing inner IValueTaskSource. The result UniTask can await multiple.
        /// </summary>
        public UniTask Preserve() {
            if (source == null) {
                return this;
            } else {
                return new UniTask(new MemoizeSource(source), token);
            }
        }

        public UniTask<AsyncUnit> AsAsyncUnitUniTask() {
            if (this.source == null) {
                return CompletedTasks.AsyncUnit;
            }

            var status = this.source.GetStatus(this.token);

            if (status.IsCompletedSuccessfully()) {
                this.source.GetResult(this.token);
                return CompletedTasks.AsyncUnit;
            } else if (this.source is IUniTaskSource<AsyncUnit> asyncUnitSource) {
                return new UniTask<AsyncUnit>(asyncUnitSource, this.token);
            }

            return new UniTask<AsyncUnit>(new AsyncUnitSource(this.source), this.token);
        }

        private sealed class AsyncUnitSource : IUniTaskSource<AsyncUnit> {
            private readonly IUniTaskSource m_Source;

            public AsyncUnitSource(IUniTaskSource source) {
                m_Source = source;
            }

            public AsyncUnit GetResult(short token) {
                m_Source.GetResult(token);
                return AsyncUnit.Default;
            }

            public UniTaskStatus GetStatus(short token) {
                return m_Source.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token) {
                m_Source.OnCompleted(continuation, state, token);
            }

            public UniTaskStatus UnsafeGetStatus() {
                return m_Source.UnsafeGetStatus();
            }

            void IUniTaskSource.GetResult(short token) {
                GetResult(token);
            }
        }

        private sealed class IsCanceledSource : IUniTaskSource<bool> {
            private readonly IUniTaskSource m_Source;

            public IsCanceledSource(IUniTaskSource source) {
                m_Source = source;
            }

            public bool GetResult(short token) {
                if (m_Source.GetStatus(token) == UniTaskStatus.Canceled) {
                    return true;
                }

                m_Source.GetResult(token);
                return false;
            }

            void IUniTaskSource.GetResult(short token) {
                GetResult(token);
            }

            public UniTaskStatus GetStatus(short token) {
                return m_Source.GetStatus(token);
            }

            public UniTaskStatus UnsafeGetStatus() {
                return m_Source.UnsafeGetStatus();
            }

            public void OnCompleted(Action<object> continuation, object state, short token) {
                m_Source.OnCompleted(continuation, state, token);
            }
        }

        private sealed class MemoizeSource : IUniTaskSource {
            private IUniTaskSource m_Source;
            private ExceptionDispatchInfo m_Exception;
            private UniTaskStatus m_Status;

            public MemoizeSource(IUniTaskSource source) {
                m_Source = source;
            }

            public void GetResult(short token) {
                if (m_Source == null) {
                    if (m_Exception != null) {
                        m_Exception.Throw();
                    }
                } else {
                    try {
                        m_Source.GetResult(token);
                        m_Status = UniTaskStatus.Succeeded;
                    } catch (Exception ex) {
                        m_Exception = ExceptionDispatchInfo.Capture(ex);

                        if (ex is OperationCanceledException) {
                            m_Status = UniTaskStatus.Canceled;
                        } else {
                            m_Status = UniTaskStatus.Faulted;
                        }

                        throw;
                    } finally {
                        m_Source = null;
                    }
                }
            }

            public UniTaskStatus GetStatus(short token) {
                if (m_Source == null) {
                    return m_Status;
                }

                return m_Source.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token) {
                if (m_Source == null) {
                    continuation(state);
                } else {
                    m_Source.OnCompleted(continuation, state, token);
                }
            }

            public UniTaskStatus UnsafeGetStatus() {
                if (m_Source == null) {
                    return m_Status;
                }

                return m_Source.UnsafeGetStatus();
            }
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion {
            private readonly UniTask m_Task;

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Awaiter(in UniTask task) {
                m_Task = task;
            }

            public bool IsCompleted {
                [DebuggerHidden]
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Task.Status.IsCompleted();
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void GetResult() {
                if (m_Task.source == null) {
                    return;
                }

                m_Task.source.GetResult(m_Task.token);
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation) {
                if (m_Task.source == null) {
                    continuation();
                } else {
                    m_Task.source.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, m_Task.token);
                }
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation) {
                if (m_Task.source == null) {
                    continuation();
                } else {
                    m_Task.source.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, m_Task.token);
                }
            }

            /// <summary>
            /// If register manually continuation, you can use it instead of for compiler OnCompleted methods.
            /// </summary>
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SourceOnCompleted(Action<object> continuation, object state) {
                if (m_Task.source == null) {
                    continuation(state);
                } else {
                    m_Task.source.OnCompleted(continuation, state, m_Task.token);
                }
            }
        }
    }

    /// <summary>
    /// Lightweight unity specified task-like object.
    /// </summary>
    [AsyncMethodBuilder(typeof(AsyncUniTaskMethodBuilder<>))]
    [StructLayout(LayoutKind.Auto)]
    public readonly struct UniTask<T> {
        private readonly IUniTaskSource<T> source;
        private readonly T result;
        private readonly short token;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask(T result) {
            this.source = default;
            this.token = default;
            this.result = result;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTask(IUniTaskSource<T> source, short token) {
            this.source = source;
            this.token = token;
            this.result = default;
        }

        public UniTaskStatus Status {
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (source == null) ? UniTaskStatus.Succeeded : source.GetStatus(token);
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Awaiter GetAwaiter() {
            return new Awaiter(this);
        }

        /// <summary>
        /// Memoizing inner IValueTaskSource. The result UniTask can await multiple.
        /// </summary>
        public UniTask<T> Preserve() {
            if (source == null) {
                return this;
            } else {
                return new UniTask<T>(new MemoizeSource(source), token);
            }
        }

        public UniTask AsUniTask() {
            if (this.source == null) {
                return UniTask.CompletedTask;
            }

            var status = this.source.GetStatus(this.token);

            if (status.IsCompletedSuccessfully()) {
                this.source.GetResult(this.token);
                return UniTask.CompletedTask;
            }

            // Converting UniTask<T> -> UniTask is zero overhead.
            return new UniTask(this.source, this.token);
        }

        public static implicit operator UniTask(UniTask<T> self) {
            return self.AsUniTask();
        }

#if SUPPORT_VALUETASK

        public static implicit operator System.Threading.Tasks.ValueTask<T>(in UniTask<T> self) {
            if (self.source == null) {
                return new System.Threading.Tasks.ValueTask<T>(self.result);
            }

#if (UNITASK_NETCORE && NETSTANDARD2_0)
            return self.AsValueTask();
#else
            return new System.Threading.Tasks.ValueTask<T>(self.source, self.token);
#endif
        }

#endif

        /// <summary>
        /// returns (bool IsCanceled, T Result) instead of throws OperationCanceledException.
        /// </summary>
        public UniTask<(bool IsCanceled, T Result)> SuppressCancellationThrow() {
            if (source == null) {
                return new UniTask<(bool IsCanceled, T Result)>((false, result));
            }

            return new UniTask<(bool, T)>(new IsCanceledSource(source), token);
        }

        public override string ToString() {
            return (this.source == null) ? result?.ToString()
                : "(" + this.source.UnsafeGetStatus() + ")";
        }

        private sealed class IsCanceledSource : IUniTaskSource<(bool, T)> {
            private readonly IUniTaskSource<T> m_Source;

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IsCanceledSource(IUniTaskSource<T> source) {
                m_Source = source;
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public (bool, T) GetResult(short token) {
                if (m_Source.GetStatus(token) == UniTaskStatus.Canceled) {
                    return (true, default);
                }

                var result = m_Source.GetResult(token);
                return (false, result);
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IUniTaskSource.GetResult(short token) {
                GetResult(token);
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public UniTaskStatus GetStatus(short token) {
                return m_Source.GetStatus(token);
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public UniTaskStatus UnsafeGetStatus() {
                return m_Source.UnsafeGetStatus();
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action<object> continuation, object state, short token) {
                m_Source.OnCompleted(continuation, state, token);
            }
        }

        private sealed class MemoizeSource : IUniTaskSource<T> {
            private IUniTaskSource<T> m_Source;
            private T m_Result;
            private ExceptionDispatchInfo m_Exception;
            private UniTaskStatus m_Status;

            public MemoizeSource(IUniTaskSource<T> source) {
                m_Source = source;
            }

            public T GetResult(short token) {
                if (m_Source == null) {
                    if (m_Exception != null) {
                        m_Exception.Throw();
                    }

                    return m_Result;
                } else {
                    try {
                        m_Result = m_Source.GetResult(token);
                        m_Status = UniTaskStatus.Succeeded;
                        return m_Result;
                    } catch (Exception ex) {
                        m_Exception = ExceptionDispatchInfo.Capture(ex);

                        if (ex is OperationCanceledException) {
                            m_Status = UniTaskStatus.Canceled;
                        } else {
                            m_Status = UniTaskStatus.Faulted;
                        }

                        throw;
                    } finally {
                        m_Source = null;
                    }
                }
            }

            void IUniTaskSource.GetResult(short token) {
                GetResult(token);
            }

            public UniTaskStatus GetStatus(short token) {
                if (m_Source == null) {
                    return m_Status;
                }

                return m_Source.GetStatus(token);
            }

            public void OnCompleted(Action<object> continuation, object state, short token) {
                if (m_Source == null) {
                    continuation(state);
                } else {
                    m_Source.OnCompleted(continuation, state, token);
                }
            }

            public UniTaskStatus UnsafeGetStatus() {
                if (m_Source == null) {
                    return m_Status;
                }

                return m_Source.UnsafeGetStatus();
            }
        }

        public readonly struct Awaiter : ICriticalNotifyCompletion {
            private readonly UniTask<T> m_Task;

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Awaiter(in UniTask<T> task) {
                m_Task = task;
            }

            public bool IsCompleted {
                [DebuggerHidden]
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => m_Task.Status.IsCompleted();
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T GetResult() {
                var s = m_Task.source;

                if (s == null) {
                    return m_Task.result;
                } else {
                    return s.GetResult(m_Task.token);
                }
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnCompleted(Action continuation) {
                var s = m_Task.source;

                if (s == null) {
                    continuation();
                } else {
                    s.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, m_Task.token);
                }
            }

            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UnsafeOnCompleted(Action continuation) {
                var s = m_Task.source;

                if (s == null) {
                    continuation();
                } else {
                    s.OnCompleted(AwaiterActions.InvokeContinuationDelegate, continuation, m_Task.token);
                }
            }

            /// <summary>
            /// If register manually continuation, you can use it instead of for compiler OnCompleted methods.
            /// </summary>
            [DebuggerHidden]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SourceOnCompleted(Action<object> continuation, object state) {
                var s = m_Task.source;

                if (s == null) {
                    continuation(state);
                } else {
                    s.OnCompleted(continuation, state, m_Task.token);
                }
            }
        }
    }
}