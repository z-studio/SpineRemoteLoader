#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using Cysharp.Threading.Tasks.Internal;

namespace Cysharp.Threading.Tasks {
    public static class UniTaskObservableExtensions {
        public static UniTask<T> ToUniTask<T>(
            this IObservable<T> source,
            bool useFirstValue = false,
            CancellationToken cancellationToken = default
        ) {
            var promise = new UniTaskCompletionSource<T>();
            var disposable = new SingleAssignmentDisposable();

            var observer = useFirstValue
                ? (IObserver<T>)new FirstValueToUniTaskObserver<T>(promise, disposable, cancellationToken)
                : (IObserver<T>)new ToUniTaskObserver<T>(promise, disposable, cancellationToken);

            try {
                disposable.Disposable = source.Subscribe(observer);
            } catch (Exception ex) {
                promise.TrySetException(ex);
            }

            return promise.Task;
        }

        public static IObservable<T> ToObservable<T>(this UniTask<T> task) {
            if (task.Status.IsCompleted()) {
                try {
                    return new ReturnObservable<T>(task.GetAwaiter().GetResult());
                } catch (Exception ex) {
                    return new ThrowObservable<T>(ex);
                }
            }

            var subject = new AsyncSubject<T>();
            Fire(subject, task).Forget();
            return subject;
        }

        /// <summary>
        /// Ideally returns IObservabl[Unit] is best but Cysharp.Threading.Tasks does not have Unit so return AsyncUnit instead.
        /// </summary>
        public static IObservable<AsyncUnit> ToObservable(this UniTask task) {
            if (task.Status.IsCompleted()) {
                try {
                    task.GetAwaiter().GetResult();
                    return new ReturnObservable<AsyncUnit>(AsyncUnit.Default);
                } catch (Exception ex) {
                    return new ThrowObservable<AsyncUnit>(ex);
                }
            }

            var subject = new AsyncSubject<AsyncUnit>();
            Fire(subject, task).Forget();
            return subject;
        }

        private static async UniTaskVoid Fire<T>(AsyncSubject<T> subject, UniTask<T> task) {
            T value;

            try {
                value = await task;
            } catch (Exception ex) {
                subject.OnError(ex);
                return;
            }

            subject.OnNext(value);
            subject.OnCompleted();
        }

        private static async UniTaskVoid Fire(AsyncSubject<AsyncUnit> subject, UniTask task) {
            try {
                await task;
            } catch (Exception ex) {
                subject.OnError(ex);
                return;
            }

            subject.OnNext(AsyncUnit.Default);
            subject.OnCompleted();
        }

        private class ToUniTaskObserver<T> : IObserver<T> {
            private static readonly Action<object> s_Callback = OnCanceled;

            private readonly UniTaskCompletionSource<T> m_Promise;
            private readonly SingleAssignmentDisposable m_Disposable;
            private readonly CancellationToken m_CancellationToken;
            private readonly CancellationTokenRegistration m_Registration;

            private bool m_HasValue;
            private T m_LatestValue;

            public ToUniTaskObserver(
                UniTaskCompletionSource<T> promise,
                SingleAssignmentDisposable disposable,
                CancellationToken cancellationToken
            ) {
                m_Promise = promise;
                m_Disposable = disposable;
                m_CancellationToken = cancellationToken;

                if (m_CancellationToken.CanBeCanceled) {
                    m_Registration = m_CancellationToken.RegisterWithoutCaptureExecutionContext(s_Callback, this);
                }
            }

            private static void OnCanceled(object state) {
                var self = (ToUniTaskObserver<T>)state;
                self.m_Disposable.Dispose();
                self.m_Promise.TrySetCanceled(self.m_CancellationToken);
            }

            public void OnNext(T value) {
                m_HasValue = true;
                m_LatestValue = value;
            }

            public void OnError(Exception error) {
                try {
                    m_Promise.TrySetException(error);
                } finally {
                    m_Registration.Dispose();
                    m_Disposable.Dispose();
                }
            }

            public void OnCompleted() {
                try {
                    if (m_HasValue) {
                        m_Promise.TrySetResult(m_LatestValue);
                    } else {
                        m_Promise.TrySetException(new InvalidOperationException("Sequence has no elements"));
                    }
                } finally {
                    m_Registration.Dispose();
                    m_Disposable.Dispose();
                }
            }
        }

        private class FirstValueToUniTaskObserver<T> : IObserver<T> {
            private static readonly Action<object> s_Callback = OnCanceled;

            private readonly UniTaskCompletionSource<T> m_Promise;
            private readonly SingleAssignmentDisposable m_Disposable;
            private readonly CancellationToken m_CancellationToken;
            private readonly CancellationTokenRegistration m_Registration;

            private bool m_HasValue;

            public FirstValueToUniTaskObserver(
                UniTaskCompletionSource<T> promise,
                SingleAssignmentDisposable disposable,
                CancellationToken cancellationToken
            ) {
                m_Promise = promise;
                m_Disposable = disposable;
                m_CancellationToken = cancellationToken;

                if (m_CancellationToken.CanBeCanceled) {
                    m_Registration = m_CancellationToken.RegisterWithoutCaptureExecutionContext(s_Callback, this);
                }
            }

            private static void OnCanceled(object state) {
                var self = (FirstValueToUniTaskObserver<T>)state;
                self.m_Disposable.Dispose();
                self.m_Promise.TrySetCanceled(self.m_CancellationToken);
            }

            public void OnNext(T value) {
                m_HasValue = true;

                try {
                    m_Promise.TrySetResult(value);
                } finally {
                    m_Registration.Dispose();
                    m_Disposable.Dispose();
                }
            }

            public void OnError(Exception error) {
                try {
                    m_Promise.TrySetException(error);
                } finally {
                    m_Registration.Dispose();
                    m_Disposable.Dispose();
                }
            }

            public void OnCompleted() {
                try {
                    if (!m_HasValue) {
                        m_Promise.TrySetException(new InvalidOperationException("Sequence has no elements"));
                    }
                } finally {
                    m_Registration.Dispose();
                    m_Disposable.Dispose();
                }
            }
        }

        private class ReturnObservable<T> : IObservable<T> {
            private readonly T m_Value;

            public ReturnObservable(T value) {
                m_Value = value;
            }

            public IDisposable Subscribe(IObserver<T> observer) {
                observer.OnNext(m_Value);
                observer.OnCompleted();
                return EmptyDisposable.Instance;
            }
        }

        private class ThrowObservable<T> : IObservable<T> {
            private readonly Exception m_Value;

            public ThrowObservable(Exception value) {
                m_Value = value;
            }

            public IDisposable Subscribe(IObserver<T> observer) {
                observer.OnError(m_Value);
                return EmptyDisposable.Instance;
            }
        }
    }
}

namespace Cysharp.Threading.Tasks.Internal {
    // Bridges for Rx.

    internal class EmptyDisposable : IDisposable {
        public static EmptyDisposable Instance = new();

        EmptyDisposable() { }

        public void Dispose() { }
    }

    internal sealed class SingleAssignmentDisposable : IDisposable {
        private readonly object m_Gate = new();
        private IDisposable m_Current;
        private bool m_Disposed;

        public bool IsDisposed {
            get {
                lock (m_Gate) {
                    return m_Disposed;
                }
            }
        }

        public IDisposable Disposable {
            get => m_Current;
            set {
                var old = default(IDisposable);
                bool alreadyDisposed;

                lock (m_Gate) {
                    alreadyDisposed = m_Disposed;
                    old = m_Current;

                    if (!alreadyDisposed) {
                        if (value == null) {
                            return;
                        }

                        m_Current = value;
                    }
                }

                if (alreadyDisposed && value != null) {
                    value.Dispose();
                    return;
                }

                if (old != null) {
                    throw new InvalidOperationException("Disposable is already set");
                }
            }
        }

        public void Dispose() {
            IDisposable old = null;

            lock (m_Gate) {
                if (!m_Disposed) {
                    m_Disposed = true;
                    old = m_Current;
                    m_Current = null;
                }
            }

            if (old != null) {
                old.Dispose();
            }
        }
    }

    internal sealed class AsyncSubject<T> : IObservable<T>, IObserver<T> {
        private object m_ObserverLock = new();

        private T m_LastValue;
        private bool m_HasValue;
        private bool m_IsStopped;
        private bool m_IsDisposed;
        private Exception m_LastError;
        private IObserver<T> m_OutObserver = EmptyObserver<T>.Instance;

        public T Value {
            get {
                ThrowIfDisposed();

                if (!m_IsStopped) {
                    throw new InvalidOperationException("AsyncSubject is not completed yet");
                }

                if (m_LastError != null) {
                    ExceptionDispatchInfo.Capture(m_LastError).Throw();
                }

                return m_LastValue;
            }
        }

        public bool HasObservers => !(m_OutObserver is EmptyObserver<T>) && !m_IsStopped && !m_IsDisposed;

        public bool IsCompleted => m_IsStopped;

        public void OnCompleted() {
            IObserver<T> old;
            T v;
            bool hv;

            lock (m_ObserverLock) {
                ThrowIfDisposed();

                if (m_IsStopped) {
                    return;
                }

                old = m_OutObserver;
                m_OutObserver = EmptyObserver<T>.Instance;
                m_IsStopped = true;
                v = m_LastValue;
                hv = m_HasValue;
            }

            if (hv) {
                old.OnNext(v);
                old.OnCompleted();
            } else {
                old.OnCompleted();
            }
        }

        public void OnError(Exception error) {
            if (error == null) {
                throw new ArgumentNullException(nameof(error));
            }

            IObserver<T> old;

            lock (m_ObserverLock) {
                ThrowIfDisposed();

                if (m_IsStopped) {
                    return;
                }

                old = m_OutObserver;
                m_OutObserver = EmptyObserver<T>.Instance;
                m_IsStopped = true;
                m_LastError = error;
            }

            old.OnError(error);
        }

        public void OnNext(T value) {
            lock (m_ObserverLock) {
                ThrowIfDisposed();

                if (m_IsStopped) {
                    return;
                }

                m_HasValue = true;
                m_LastValue = value;
            }
        }

        public IDisposable Subscribe(IObserver<T> observer) {
            if (observer == null) {
                throw new ArgumentNullException(nameof(observer));
            }

            var ex = default(Exception);
            var v = default(T);
            var hv = false;

            lock (m_ObserverLock) {
                ThrowIfDisposed();

                if (!m_IsStopped) {
                    var listObserver = m_OutObserver as ListObserver<T>;

                    if (listObserver != null) {
                        m_OutObserver = listObserver.Add(observer);
                    } else {
                        var current = m_OutObserver;

                        if (current is EmptyObserver<T>) {
                            m_OutObserver = observer;
                        } else {
                            m_OutObserver = new ListObserver<T>(
                                new ImmutableList<IObserver<T>>(new[] { current, observer })
                            );
                        }
                    }

                    return new Subscription(this, observer);
                }

                ex = m_LastError;
                v = m_LastValue;
                hv = m_HasValue;
            }

            if (ex != null) {
                observer.OnError(ex);
            } else if (hv) {
                observer.OnNext(v);
                observer.OnCompleted();
            } else {
                observer.OnCompleted();
            }

            return EmptyDisposable.Instance;
        }

        public void Dispose() {
            lock (m_ObserverLock) {
                m_IsDisposed = true;
                m_OutObserver = DisposedObserver<T>.Instance;
                m_LastError = null;
                m_LastValue = default(T);
            }
        }

        private void ThrowIfDisposed() {
            if (m_IsDisposed) {
                throw new ObjectDisposedException("");
            }
        }

        private class Subscription : IDisposable {
            private readonly object m_Gate = new();
            private AsyncSubject<T> m_Parent;
            private IObserver<T> m_UnsubscribeTarget;

            public Subscription(AsyncSubject<T> parent, IObserver<T> unsubscribeTarget) {
                m_Parent = parent;
                m_UnsubscribeTarget = unsubscribeTarget;
            }

            public void Dispose() {
                lock (m_Gate) {
                    if (m_Parent != null) {
                        lock (m_Parent.m_ObserverLock) {
                            var listObserver = m_Parent.m_OutObserver as ListObserver<T>;

                            if (listObserver != null) {
                                m_Parent.m_OutObserver = listObserver.Remove(m_UnsubscribeTarget);
                            } else {
                                m_Parent.m_OutObserver = EmptyObserver<T>.Instance;
                            }

                            m_UnsubscribeTarget = null;
                            m_Parent = null;
                        }
                    }
                }
            }
        }
    }

    internal class ListObserver<T> : IObserver<T> {
        private readonly ImmutableList<IObserver<T>> m_Observers;

        public ListObserver(ImmutableList<IObserver<T>> observers) {
            m_Observers = observers;
        }

        public void OnCompleted() {
            var targetObservers = m_Observers.Data;

            for (var i = 0; i < targetObservers.Length; i++) {
                targetObservers[i].OnCompleted();
            }
        }

        public void OnError(Exception error) {
            var targetObservers = m_Observers.Data;

            for (var i = 0; i < targetObservers.Length; i++) {
                targetObservers[i].OnError(error);
            }
        }

        public void OnNext(T value) {
            var targetObservers = m_Observers.Data;

            for (var i = 0; i < targetObservers.Length; i++) {
                targetObservers[i].OnNext(value);
            }
        }

        internal IObserver<T> Add(IObserver<T> observer) {
            return new ListObserver<T>(m_Observers.Add(observer));
        }

        internal IObserver<T> Remove(IObserver<T> observer) {
            var i = Array.IndexOf(m_Observers.Data, observer);

            if (i < 0) {
                return this;
            }

            if (m_Observers.Data.Length == 2) {
                return m_Observers.Data[1 - i];
            } else {
                return new ListObserver<T>(m_Observers.Remove(observer));
            }
        }
    }

    internal class EmptyObserver<T> : IObserver<T> {
        public static readonly EmptyObserver<T> Instance = new();

        EmptyObserver() { }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(T value) { }
    }

    internal class ThrowObserver<T> : IObserver<T> {
        public static readonly ThrowObserver<T> Instance = new();

        ThrowObserver() { }

        public void OnCompleted() { }

        public void OnError(Exception error) {
            ExceptionDispatchInfo.Capture(error).Throw();
        }

        public void OnNext(T value) { }
    }

    internal class DisposedObserver<T> : IObserver<T> {
        public static readonly DisposedObserver<T> Instance = new();

        DisposedObserver() { }

        public void OnCompleted() {
            throw new ObjectDisposedException("");
        }

        public void OnError(Exception error) {
            throw new ObjectDisposedException("");
        }

        public void OnNext(T value) {
            throw new ObjectDisposedException("");
        }
    }

    internal class ImmutableList<T> {
        public static readonly ImmutableList<T> Empty = new();

        private T[] m_Data;

        public T[] Data => m_Data;

        ImmutableList() {
            m_Data = new T[0];
        }

        public ImmutableList(T[] data) {
            m_Data = data;
        }

        public ImmutableList<T> Add(T value) {
            var newData = new T[m_Data.Length + 1];
            Array.Copy(m_Data, newData, m_Data.Length);
            newData[m_Data.Length] = value;
            return new ImmutableList<T>(newData);
        }

        public ImmutableList<T> Remove(T value) {
            var i = IndexOf(value);

            if (i < 0) {
                return this;
            }

            var length = m_Data.Length;

            if (length == 1) {
                return Empty;
            }

            var newData = new T[length - 1];

            Array.Copy(m_Data, 0, newData, 0, i);
            Array.Copy(m_Data, i + 1, newData, i, length - i - 1);

            return new ImmutableList<T>(newData);
        }

        public int IndexOf(T value) {
            for (var i = 0; i < m_Data.Length; ++i) {
                // ImmutableList only use for IObserver(no worry for boxed)
                if (object.Equals(m_Data[i], value)) {
                    return i;
                }
            }

            return -1;
        }
    }
}