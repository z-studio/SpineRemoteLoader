#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Cysharp.Threading.Tasks {
    public interface IResolvePromise {
        bool TrySetResult();
    }

    public interface IResolvePromise<T> {
        bool TrySetResult(T value);
    }

    public interface IRejectPromise {
        bool TrySetException(Exception exception);
    }

    public interface ICancelPromise {
        bool TrySetCanceled(CancellationToken cancellationToken = default);
    }

    public interface IPromise<T> : IResolvePromise<T>, IRejectPromise, ICancelPromise { }

    public interface IPromise : IResolvePromise, IRejectPromise, ICancelPromise { }

    internal class ExceptionHolder {
        private ExceptionDispatchInfo m_Exception;
        private bool m_CalledGet = false;

        public ExceptionHolder(ExceptionDispatchInfo exception) {
            m_Exception = exception;
        }

        public ExceptionDispatchInfo GetException() {
            if (!m_CalledGet) {
                m_CalledGet = true;
                GC.SuppressFinalize(this);
            }

            return m_Exception;
        }

        ~ExceptionHolder() {
            if (!m_CalledGet) {
                UniTaskScheduler.PublishUnobservedTaskException(m_Exception.SourceException);
            }
        }
    }

    [StructLayout(LayoutKind.Auto)]
    public struct UniTaskCompletionSourceCore<TResult> {
        // Struct Size: TResult + (8 + 2 + 1 + 1 + 8 + 8)

        private TResult result;
        private object error; // ExceptionHolder or OperationCanceledException
        private short version;
        private bool hasUnhandledError;
        private int completedCount; // 0: completed == false
        private Action<object> continuation;
        private object continuationState;

        [DebuggerHidden]
        public void Reset() {
            ReportUnhandledError();

            unchecked {
                version += 1; // incr version.
            }

            completedCount = 0;
            result = default;
            error = null;
            hasUnhandledError = false;
            continuation = null;
            continuationState = null;
        }

        private void ReportUnhandledError() {
            if (hasUnhandledError) {
                try {
                    if (error is OperationCanceledException oc) {
                        UniTaskScheduler.PublishUnobservedTaskException(oc);
                    } else if (error is ExceptionHolder e) {
                        UniTaskScheduler.PublishUnobservedTaskException(e.GetException().SourceException);
                    }
                } catch { }
            }
        }

        internal void MarkHandled() {
            hasUnhandledError = false;
        }

        /// <summary>Completes with a successful result.</summary>
        /// <param name="result">The result.</param>
        [DebuggerHidden]
        public bool TrySetResult(TResult result) {
            if (Interlocked.Increment(ref completedCount) == 1) {
                // setup result
                this.result = result;

                if (continuation != null
                    || Interlocked.CompareExchange(
                        ref this.continuation,
                        UniTaskCompletionSourceCoreShared.s_sentinel,
                        null
                    )
                    != null) {
                    continuation(continuationState);
                }

                return true;
            }

            return false;
        }

        /// <summary>Completes with an error.</summary>
        /// <param name="error">The exception.</param>
        [DebuggerHidden]
        public bool TrySetException(Exception error) {
            if (Interlocked.Increment(ref completedCount) == 1) {
                // setup result
                this.hasUnhandledError = true;

                if (error is OperationCanceledException) {
                    this.error = error;
                } else {
                    this.error = new ExceptionHolder(ExceptionDispatchInfo.Capture(error));
                }

                if (continuation != null
                    || Interlocked.CompareExchange(
                        ref this.continuation,
                        UniTaskCompletionSourceCoreShared.s_sentinel,
                        null
                    )
                    != null) {
                    continuation(continuationState);
                }

                return true;
            }

            return false;
        }

        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default) {
            if (Interlocked.Increment(ref completedCount) == 1) {
                // setup result
                this.hasUnhandledError = true;
                this.error = new OperationCanceledException(cancellationToken);

                if (continuation != null
                    || Interlocked.CompareExchange(
                        ref this.continuation,
                        UniTaskCompletionSourceCoreShared.s_sentinel,
                        null
                    )
                    != null) {
                    continuation(continuationState);
                }

                return true;
            }

            return false;
        }

        /// <summary>Gets the operation version.</summary>
        [DebuggerHidden]
        public short Version => version;

        /// <summary>Gets the status of the operation.</summary>
        /// <param name="token">Opaque value that was provided to the <see cref="UniTask"/>'s constructor.</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTaskStatus GetStatus(short token) {
            ValidateToken(token);

            return (continuation == null || (completedCount == 0))
                ? UniTaskStatus.Pending
                : (error == null)
                    ? UniTaskStatus.Succeeded
                    : (error is OperationCanceledException)
                        ? UniTaskStatus.Canceled
                        : UniTaskStatus.Faulted;
        }

        /// <summary>Gets the status of the operation without token validation.</summary>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UniTaskStatus UnsafeGetStatus() {
            return (continuation == null || (completedCount == 0))
                ? UniTaskStatus.Pending
                : (error == null)
                    ? UniTaskStatus.Succeeded
                    : (error is OperationCanceledException)
                        ? UniTaskStatus.Canceled
                        : UniTaskStatus.Faulted;
        }

        /// <summary>Gets the result of the operation.</summary>
        /// <param name="token">Opaque value that was provided to the <see cref="UniTask"/>'s constructor.</param>

        // [StackTraceHidden]
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult GetResult(short token) {
            ValidateToken(token);

            if (completedCount == 0) {
                throw new InvalidOperationException("Not yet completed, UniTask only allow to use await.");
            }

            if (error != null) {
                hasUnhandledError = false;

                if (error is OperationCanceledException oce) {
                    throw oce;
                } else if (error is ExceptionHolder eh) {
                    eh.GetException().Throw();
                }

                throw new InvalidOperationException("Critical: invalid exception type was held.");
            }

            return result;
        }

        /// <summary>Schedules the continuation action for this operation.</summary>
        /// <param name="continuation">The continuation to invoke when the operation has completed.</param>
        /// <param name="state">The state object to pass to <paramref name="continuation"/> when it's invoked.</param>
        /// <param name="token">Opaque value that was provided to the <see cref="UniTask"/>'s constructor.</param>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(
            Action<object> continuation,
            object state,
            short token /*, ValueTaskSourceOnCompletedFlags flags */
        ) {
            if (continuation == null) {
                throw new ArgumentNullException(nameof(continuation));
            }

            ValidateToken(token);

            /* no use ValueTaskSourceOnCOmpletedFlags, always no capture ExecutionContext and SynchronizationContext. */

            /*
                PatternA: GetStatus=Pending => OnCompleted => TrySet*** => GetResult
                PatternB: TrySet*** => GetStatus=!Pending => GetResult
                PatternC: GetStatus=Pending => TrySet/OnCompleted(race condition) => GetResult
                C.1: win OnCompleted -> TrySet invoke saved continuation
                C.2: win TrySet -> should invoke continuation here.
            */

            // not set continuation yet.
            object oldContinuation = this.continuation;

            if (oldContinuation == null) {
                continuationState = state;
                oldContinuation = Interlocked.CompareExchange(ref this.continuation, continuation, null);
            }

            if (oldContinuation != null) {
                // already running continuation in TrySet.
                // It will cause call OnCompleted multiple time, invalid.
                if (!ReferenceEquals(oldContinuation, UniTaskCompletionSourceCoreShared.s_sentinel)) {
                    throw new InvalidOperationException(
                        "Already continuation registered, can not await twice or get Status after await."
                    );
                }

                continuation(state);
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateToken(short token) {
            if (token != version) {
                throw new InvalidOperationException(
                    "Token version is not matched, can not await twice or get Status after await."
                );
            }
        }
    }

    internal static class UniTaskCompletionSourceCoreShared { // separated out of generic to avoid unnecessary duplication
        internal static readonly Action<object> s_sentinel = CompletionSentinel;

        private static void CompletionSentinel(object _) { // named method to aid debugging
            throw new InvalidOperationException("The sentinel delegate should never be invoked.");
        }
    }

    public class AutoResetUniTaskCompletionSource : IUniTaskSource,
                                                    ITaskPoolNode<AutoResetUniTaskCompletionSource>,
                                                    IPromise {
        private static TaskPool<AutoResetUniTaskCompletionSource> s_Pool;
        private AutoResetUniTaskCompletionSource m_NextNode;
        public ref AutoResetUniTaskCompletionSource NextNode => ref m_NextNode;

        static AutoResetUniTaskCompletionSource() {
            TaskPool.RegisterSizeGetter(typeof(AutoResetUniTaskCompletionSource), () => s_Pool.Size);
        }

        private UniTaskCompletionSourceCore<AsyncUnit> m_Core;
        private short m_Version;

        AutoResetUniTaskCompletionSource() { }

        [DebuggerHidden]
        public static AutoResetUniTaskCompletionSource Create() {
            if (!s_Pool.TryPop(out var result)) {
                result = new AutoResetUniTaskCompletionSource();
            }

            result.m_Version = result.m_Core.Version;
            TaskTracker.TrackActiveTask(result, 2);
            return result;
        }

        [DebuggerHidden]
        public static AutoResetUniTaskCompletionSource CreateFromCanceled(
            CancellationToken cancellationToken,
            out short token
        ) {
            var source = Create();
            source.TrySetCanceled(cancellationToken);
            token = source.m_Core.Version;
            return source;
        }

        [DebuggerHidden]
        public static AutoResetUniTaskCompletionSource CreateFromException(Exception exception, out short token) {
            var source = Create();
            source.TrySetException(exception);
            token = source.m_Core.Version;
            return source;
        }

        [DebuggerHidden]
        public static AutoResetUniTaskCompletionSource CreateCompleted(out short token) {
            var source = Create();
            source.TrySetResult();
            token = source.m_Core.Version;
            return source;
        }

        public UniTask Task {
            [DebuggerHidden]
            get => new(this, m_Core.Version);
        }

        [DebuggerHidden]
        public bool TrySetResult() {
            return m_Version == m_Core.Version && m_Core.TrySetResult(AsyncUnit.Default);
        }

        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default) {
            return m_Version == m_Core.Version && m_Core.TrySetCanceled(cancellationToken);
        }

        [DebuggerHidden]
        public bool TrySetException(Exception exception) {
            return m_Version == m_Core.Version && m_Core.TrySetException(exception);
        }

        [DebuggerHidden]
        public void GetResult(short token) {
            try {
                m_Core.GetResult(token);
            } finally {
                TryReturn();
            }
        }

        [DebuggerHidden]
        public UniTaskStatus GetStatus(short token) {
            return m_Core.GetStatus(token);
        }

        [DebuggerHidden]
        public UniTaskStatus UnsafeGetStatus() {
            return m_Core.UnsafeGetStatus();
        }

        [DebuggerHidden]
        public void OnCompleted(Action<object> continuation, object state, short token) {
            m_Core.OnCompleted(continuation, state, token);
        }

        [DebuggerHidden]
        private bool TryReturn() {
            TaskTracker.RemoveTracking(this);
            m_Core.Reset();
            return s_Pool.TryPush(this);
        }
    }

    public class AutoResetUniTaskCompletionSource<T> : IUniTaskSource<T>,
                                                       ITaskPoolNode<AutoResetUniTaskCompletionSource<T>>,
                                                       IPromise<T> {
        private static TaskPool<AutoResetUniTaskCompletionSource<T>> s_Pool;
        private AutoResetUniTaskCompletionSource<T> m_NextNode;
        public ref AutoResetUniTaskCompletionSource<T> NextNode => ref m_NextNode;

        static AutoResetUniTaskCompletionSource() {
            TaskPool.RegisterSizeGetter(typeof(AutoResetUniTaskCompletionSource<T>), () => s_Pool.Size);
        }

        private UniTaskCompletionSourceCore<T> m_Core;
        private short m_Version;

        AutoResetUniTaskCompletionSource() { }

        [DebuggerHidden]
        public static AutoResetUniTaskCompletionSource<T> Create() {
            if (!s_Pool.TryPop(out var result)) {
                result = new AutoResetUniTaskCompletionSource<T>();
            }

            result.m_Version = result.m_Core.Version;
            TaskTracker.TrackActiveTask(result, 2);
            return result;
        }

        [DebuggerHidden]
        public static AutoResetUniTaskCompletionSource<T> CreateFromCanceled(
            CancellationToken cancellationToken,
            out short token
        ) {
            var source = Create();
            source.TrySetCanceled(cancellationToken);
            token = source.m_Core.Version;
            return source;
        }

        [DebuggerHidden]
        public static AutoResetUniTaskCompletionSource<T> CreateFromException(Exception exception, out short token) {
            var source = Create();
            source.TrySetException(exception);
            token = source.m_Core.Version;
            return source;
        }

        [DebuggerHidden]
        public static AutoResetUniTaskCompletionSource<T> CreateFromResult(T result, out short token) {
            var source = Create();
            source.TrySetResult(result);
            token = source.m_Core.Version;
            return source;
        }

        public UniTask<T> Task {
            [DebuggerHidden]
            get => new(this, m_Core.Version);
        }

        [DebuggerHidden]
        public bool TrySetResult(T result) {
            return m_Version == m_Core.Version && m_Core.TrySetResult(result);
        }

        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default) {
            return m_Version == m_Core.Version && m_Core.TrySetCanceled(cancellationToken);
        }

        [DebuggerHidden]
        public bool TrySetException(Exception exception) {
            return m_Version == m_Core.Version && m_Core.TrySetException(exception);
        }

        [DebuggerHidden]
        public T GetResult(short token) {
            try {
                return m_Core.GetResult(token);
            } finally {
                TryReturn();
            }
        }

        [DebuggerHidden]
        void IUniTaskSource.GetResult(short token) {
            GetResult(token);
        }

        [DebuggerHidden]
        public UniTaskStatus GetStatus(short token) {
            return m_Core.GetStatus(token);
        }

        [DebuggerHidden]
        public UniTaskStatus UnsafeGetStatus() {
            return m_Core.UnsafeGetStatus();
        }

        [DebuggerHidden]
        public void OnCompleted(Action<object> continuation, object state, short token) {
            m_Core.OnCompleted(continuation, state, token);
        }

        [DebuggerHidden]
        private bool TryReturn() {
            TaskTracker.RemoveTracking(this);
            m_Core.Reset();
            return s_Pool.TryPush(this);
        }
    }

    public class UniTaskCompletionSource : IUniTaskSource, IPromise {
        private CancellationToken m_CancellationToken;
        private ExceptionHolder m_Exception;
        private object m_Gate;
        private Action<object> m_SingleContinuation;
        private object m_SingleState;
        private List<(Action<object>, object)> m_SecondaryContinuationList;

        private int m_IntStatus; // UniTaskStatus
        private bool m_Handled = false;

        public UniTaskCompletionSource() {
            TaskTracker.TrackActiveTask(this, 2);
        }

        [DebuggerHidden]
        internal void MarkHandled() {
            if (!m_Handled) {
                m_Handled = true;
                TaskTracker.RemoveTracking(this);
            }
        }

        public UniTask Task {
            [DebuggerHidden]
            get => new(this, 0);
        }

        [DebuggerHidden]
        public bool TrySetResult() {
            return TrySignalCompletion(UniTaskStatus.Succeeded);
        }

        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default) {
            if (UnsafeGetStatus() != UniTaskStatus.Pending) {
                return false;
            }

            m_CancellationToken = cancellationToken;
            return TrySignalCompletion(UniTaskStatus.Canceled);
        }

        [DebuggerHidden]
        public bool TrySetException(Exception exception) {
            if (exception is OperationCanceledException oce) {
                return TrySetCanceled(oce.CancellationToken);
            }

            if (UnsafeGetStatus() != UniTaskStatus.Pending) {
                return false;
            }

            m_Exception = new ExceptionHolder(ExceptionDispatchInfo.Capture(exception));
            return TrySignalCompletion(UniTaskStatus.Faulted);
        }

        [DebuggerHidden]
        public void GetResult(short token) {
            MarkHandled();

            var status = (UniTaskStatus)m_IntStatus;

            switch (status) {
                case UniTaskStatus.Succeeded:
                    return;
                case UniTaskStatus.Faulted:
                    m_Exception.GetException().Throw();
                    return;
                case UniTaskStatus.Canceled:
                    throw new OperationCanceledException(m_CancellationToken);
                default:
                case UniTaskStatus.Pending:
                    throw new InvalidOperationException("not yet completed.");
            }
        }

        [DebuggerHidden]
        public UniTaskStatus GetStatus(short token) {
            return (UniTaskStatus)m_IntStatus;
        }

        [DebuggerHidden]
        public UniTaskStatus UnsafeGetStatus() {
            return (UniTaskStatus)m_IntStatus;
        }

        [DebuggerHidden]
        public void OnCompleted(Action<object> continuation, object state, short token) {
            if (m_Gate == null) {
                Interlocked.CompareExchange(ref m_Gate, new object(), null);
            }

            var lockGate = Thread.VolatileRead(ref m_Gate);

            lock (lockGate) // wait TrySignalCompletion, after status is not pending.
            {
                if ((UniTaskStatus)m_IntStatus != UniTaskStatus.Pending) {
                    continuation(state);
                    return;
                }

                if (m_SingleContinuation == null) {
                    m_SingleContinuation = continuation;
                    m_SingleState = state;
                } else {
                    if (m_SecondaryContinuationList == null) {
                        m_SecondaryContinuationList = new List<(Action<object>, object)>();
                    }

                    m_SecondaryContinuationList.Add((continuation, state));
                }
            }
        }

        [DebuggerHidden]
        private bool TrySignalCompletion(UniTaskStatus status) {
            if (Interlocked.CompareExchange(ref m_IntStatus, (int)status, (int)UniTaskStatus.Pending)
                == (int)UniTaskStatus.Pending) {
                if (m_Gate == null) {
                    Interlocked.CompareExchange(ref m_Gate, new object(), null);
                }

                var lockGate = Thread.VolatileRead(ref m_Gate);

                lock (lockGate) // wait OnCompleted.
                {
                    if (m_SingleContinuation != null) {
                        try {
                            m_SingleContinuation(m_SingleState);
                        } catch (Exception ex) {
                            UniTaskScheduler.PublishUnobservedTaskException(ex);
                        }
                    }

                    if (m_SecondaryContinuationList != null) {
                        foreach (var (c, state) in m_SecondaryContinuationList) {
                            try {
                                c(state);
                            } catch (Exception ex) {
                                UniTaskScheduler.PublishUnobservedTaskException(ex);
                            }
                        }
                    }

                    m_SingleContinuation = null;
                    m_SingleState = null;
                    m_SecondaryContinuationList = null;
                }

                return true;
            }

            return false;
        }
    }

    public class UniTaskCompletionSource<T> : IUniTaskSource<T>, IPromise<T> {
        private CancellationToken m_CancellationToken;
        private T m_Result;
        private ExceptionHolder m_Exception;
        private object m_Gate;
        private Action<object> m_SingleContinuation;
        private object m_SingleState;
        private List<(Action<object>, object)> m_SecondaryContinuationList;

        private int m_IntStatus; // UniTaskStatus
        private bool m_Handled = false;

        public UniTaskCompletionSource() {
            TaskTracker.TrackActiveTask(this, 2);
        }

        [DebuggerHidden]
        internal void MarkHandled() {
            if (!m_Handled) {
                m_Handled = true;
                TaskTracker.RemoveTracking(this);
            }
        }

        public UniTask<T> Task {
            [DebuggerHidden]
            get => new(this, 0);
        }

        [DebuggerHidden]
        public bool TrySetResult(T result) {
            if (UnsafeGetStatus() != UniTaskStatus.Pending) {
                return false;
            }

            m_Result = result;
            return TrySignalCompletion(UniTaskStatus.Succeeded);
        }

        [DebuggerHidden]
        public bool TrySetCanceled(CancellationToken cancellationToken = default) {
            if (UnsafeGetStatus() != UniTaskStatus.Pending) {
                return false;
            }

            m_CancellationToken = cancellationToken;
            return TrySignalCompletion(UniTaskStatus.Canceled);
        }

        [DebuggerHidden]
        public bool TrySetException(Exception exception) {
            if (exception is OperationCanceledException oce) {
                return TrySetCanceled(oce.CancellationToken);
            }

            if (UnsafeGetStatus() != UniTaskStatus.Pending) {
                return false;
            }

            m_Exception = new ExceptionHolder(ExceptionDispatchInfo.Capture(exception));
            return TrySignalCompletion(UniTaskStatus.Faulted);
        }

        [DebuggerHidden]
        public T GetResult(short token) {
            MarkHandled();

            var status = (UniTaskStatus)m_IntStatus;

            switch (status) {
                case UniTaskStatus.Succeeded:
                    return m_Result;
                case UniTaskStatus.Faulted:
                    m_Exception.GetException().Throw();
                    return default;
                case UniTaskStatus.Canceled:
                    throw new OperationCanceledException(m_CancellationToken);
                default:
                case UniTaskStatus.Pending:
                    throw new InvalidOperationException("not yet completed.");
            }
        }

        [DebuggerHidden]
        void IUniTaskSource.GetResult(short token) {
            GetResult(token);
        }

        [DebuggerHidden]
        public UniTaskStatus GetStatus(short token) {
            return (UniTaskStatus)m_IntStatus;
        }

        [DebuggerHidden]
        public UniTaskStatus UnsafeGetStatus() {
            return (UniTaskStatus)m_IntStatus;
        }

        [DebuggerHidden]
        public void OnCompleted(Action<object> continuation, object state, short token) {
            if (m_Gate == null) {
                Interlocked.CompareExchange(ref m_Gate, new object(), null);
            }

            var lockGate = Thread.VolatileRead(ref m_Gate);

            lock (lockGate) // wait TrySignalCompletion, after status is not pending.
            {
                if ((UniTaskStatus)m_IntStatus != UniTaskStatus.Pending) {
                    continuation(state);
                    return;
                }

                if (m_SingleContinuation == null) {
                    m_SingleContinuation = continuation;
                    m_SingleState = state;
                } else {
                    if (m_SecondaryContinuationList == null) {
                        m_SecondaryContinuationList = new List<(Action<object>, object)>();
                    }

                    m_SecondaryContinuationList.Add((continuation, state));
                }
            }
        }

        [DebuggerHidden]
        private bool TrySignalCompletion(UniTaskStatus status) {
            if (Interlocked.CompareExchange(ref m_IntStatus, (int)status, (int)UniTaskStatus.Pending)
                == (int)UniTaskStatus.Pending) {
                if (m_Gate == null) {
                    Interlocked.CompareExchange(ref m_Gate, new object(), null);
                }

                var lockGate = Thread.VolatileRead(ref m_Gate);

                lock (lockGate) // wait OnCompleted.
                {
                    if (m_SingleContinuation != null) {
                        try {
                            m_SingleContinuation(m_SingleState);
                        } catch (Exception ex) {
                            UniTaskScheduler.PublishUnobservedTaskException(ex);
                        }
                    }

                    if (m_SecondaryContinuationList != null) {
                        foreach (var (c, state) in m_SecondaryContinuationList) {
                            try {
                                c(state);
                            } catch (Exception ex) {
                                UniTaskScheduler.PublishUnobservedTaskException(ex);
                            }
                        }
                    }

                    m_SingleContinuation = null;
                    m_SingleState = null;
                    m_SecondaryContinuationList = null;
                }

                return true;
            }

            return false;
        }
    }
}