using System;
using System.Collections.Generic;
using System.Threading;

namespace Cysharp.Threading.Tasks {
    public static class Channel {
        public static Channel<T> CreateSingleConsumerUnbounded<T>() {
            return new SingleConsumerUnboundedChannel<T>();
        }
    }

    public abstract class Channel<TWrite, TRead> {
        public ChannelReader<TRead> Reader { get; protected set; }
        public ChannelWriter<TWrite> Writer { get; protected set; }

        public static implicit operator ChannelReader<TRead>(Channel<TWrite, TRead> channel) => channel.Reader;
        public static implicit operator ChannelWriter<TWrite>(Channel<TWrite, TRead> channel) => channel.Writer;
    }

    public abstract class Channel<T> : Channel<T, T> { }

    public abstract class ChannelReader<T> {
        public abstract bool TryRead(out T item);
        public abstract UniTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default(CancellationToken));

        public abstract UniTask Completion { get; }

        public virtual UniTask<T> ReadAsync(CancellationToken cancellationToken = default(CancellationToken)) {
            if (TryRead(out var item)) {
                return UniTask.FromResult(item);
            }

            return ReadAsyncCore(cancellationToken);
        }

        async UniTask<T> ReadAsyncCore(CancellationToken cancellationToken = default(CancellationToken)) {
            if (await WaitToReadAsync(cancellationToken)) {
                if (TryRead(out var item)) {
                    return item;
                }
            }

            throw new ChannelClosedException();
        }

        public abstract IUniTaskAsyncEnumerable<T> ReadAllAsync(
            CancellationToken cancellationToken = default(CancellationToken)
        );
    }

    public abstract class ChannelWriter<T> {
        public abstract bool TryWrite(T item);
        public abstract bool TryComplete(Exception error = null);

        public void Complete(Exception error = null) {
            if (!TryComplete(error)) {
                throw new ChannelClosedException();
            }
        }
    }

    public partial class ChannelClosedException : InvalidOperationException {
        public ChannelClosedException() : base("Channel is already closed.") { }

        public ChannelClosedException(string message) : base(message) { }

        public ChannelClosedException(Exception innerException) : base("Channel is already closed", innerException) { }

        public ChannelClosedException(string message, Exception innerException) : base(message, innerException) { }
    }

    internal class SingleConsumerUnboundedChannel<T> : Channel<T> {
        private readonly Queue<T> m_Items;
        private readonly SingleConsumerUnboundedChannelReader m_ReaderSource;
        private UniTaskCompletionSource m_CompletedTaskSource;
        private UniTask m_CompletedTask;

        private Exception m_CompletionError;
        private bool m_Closed;

        public SingleConsumerUnboundedChannel() {
            m_Items = new Queue<T>();
            Writer = new SingleConsumerUnboundedChannelWriter(this);
            m_ReaderSource = new SingleConsumerUnboundedChannelReader(this);
            Reader = m_ReaderSource;
        }

        private sealed class SingleConsumerUnboundedChannelWriter : ChannelWriter<T> {
            private readonly SingleConsumerUnboundedChannel<T> m_Parent;

            public SingleConsumerUnboundedChannelWriter(SingleConsumerUnboundedChannel<T> parent) {
                m_Parent = parent;
            }

            public override bool TryWrite(T item) {
                bool waiting;

                lock (m_Parent.m_Items) {
                    if (m_Parent.m_Closed) {
                        return false;
                    }

                    m_Parent.m_Items.Enqueue(item);
                    waiting = m_Parent.m_ReaderSource.isWaiting;
                }

                if (waiting) {
                    m_Parent.m_ReaderSource.SingalContinuation();
                }

                return true;
            }

            public override bool TryComplete(Exception error = null) {
                bool waiting;

                lock (m_Parent.m_Items) {
                    if (m_Parent.m_Closed) {
                        return false;
                    }

                    m_Parent.m_Closed = true;
                    waiting = m_Parent.m_ReaderSource.isWaiting;

                    if (m_Parent.m_Items.Count == 0) {
                        if (error == null) {
                            if (m_Parent.m_CompletedTaskSource != null) {
                                m_Parent.m_CompletedTaskSource.TrySetResult();
                            } else {
                                m_Parent.m_CompletedTask = UniTask.CompletedTask;
                            }
                        } else {
                            if (m_Parent.m_CompletedTaskSource != null) {
                                m_Parent.m_CompletedTaskSource.TrySetException(error);
                            } else {
                                m_Parent.m_CompletedTask = UniTask.FromException(error);
                            }
                        }

                        if (waiting) {
                            m_Parent.m_ReaderSource.SingalCompleted(error);
                        }
                    }

                    m_Parent.m_CompletionError = error;
                }

                return true;
            }
        }

        private sealed class SingleConsumerUnboundedChannelReader : ChannelReader<T>, IUniTaskSource<bool> {
            private readonly Action<object> m_CancellationCallbackDelegate = CancellationCallback;
            private readonly SingleConsumerUnboundedChannel<T> m_Parent;

            private CancellationToken m_CancellationToken;
            private CancellationTokenRegistration m_CancellationTokenRegistration;
            private UniTaskCompletionSourceCore<bool> m_Core;
            internal bool isWaiting;

            public SingleConsumerUnboundedChannelReader(SingleConsumerUnboundedChannel<T> parent) {
                m_Parent = parent;

                TaskTracker.TrackActiveTask(this, 4);
            }

            public override UniTask Completion {
                get {
                    if (m_Parent.m_CompletedTaskSource != null) {
                        return m_Parent.m_CompletedTaskSource.Task;
                    }

                    if (m_Parent.m_Closed) {
                        return m_Parent.m_CompletedTask;
                    }

                    m_Parent.m_CompletedTaskSource = new UniTaskCompletionSource();
                    return m_Parent.m_CompletedTaskSource.Task;
                }
            }

            public override bool TryRead(out T item) {
                lock (m_Parent.m_Items) {
                    if (m_Parent.m_Items.Count != 0) {
                        item = m_Parent.m_Items.Dequeue();

                        // complete when all value was consumed.
                        if (m_Parent.m_Closed && m_Parent.m_Items.Count == 0) {
                            if (m_Parent.m_CompletionError != null) {
                                if (m_Parent.m_CompletedTaskSource != null) {
                                    m_Parent.m_CompletedTaskSource.TrySetException(m_Parent.m_CompletionError);
                                } else {
                                    m_Parent.m_CompletedTask = UniTask.FromException(m_Parent.m_CompletionError);
                                }
                            } else {
                                if (m_Parent.m_CompletedTaskSource != null) {
                                    m_Parent.m_CompletedTaskSource.TrySetResult();
                                } else {
                                    m_Parent.m_CompletedTask = UniTask.CompletedTask;
                                }
                            }
                        }
                    } else {
                        item = default;
                        return false;
                    }
                }

                return true;
            }

            public override UniTask<bool> WaitToReadAsync(CancellationToken cancellationToken) {
                if (cancellationToken.IsCancellationRequested) {
                    return UniTask.FromCanceled<bool>(cancellationToken);
                }

                lock (m_Parent.m_Items) {
                    if (m_Parent.m_Items.Count != 0) {
                        return CompletedTasks.True;
                    }

                    if (m_Parent.m_Closed) {
                        if (m_Parent.m_CompletionError == null) {
                            return CompletedTasks.False;
                        } else {
                            return UniTask.FromException<bool>(m_Parent.m_CompletionError);
                        }
                    }

                    m_CancellationTokenRegistration.Dispose();

                    m_Core.Reset();
                    isWaiting = true;

                    m_CancellationToken = cancellationToken;

                    if (m_CancellationToken.CanBeCanceled) {
                        m_CancellationTokenRegistration =
                            m_CancellationToken.RegisterWithoutCaptureExecutionContext(
                                m_CancellationCallbackDelegate,
                                this
                            );
                    }

                    return new UniTask<bool>(this, m_Core.Version);
                }
            }

            public void SingalContinuation() {
                m_Core.TrySetResult(true);
            }

            public void SingalCancellation(CancellationToken cancellationToken) {
                TaskTracker.RemoveTracking(this);
                m_Core.TrySetCanceled(cancellationToken);
            }

            public void SingalCompleted(Exception error) {
                if (error != null) {
                    TaskTracker.RemoveTracking(this);
                    m_Core.TrySetException(error);
                } else {
                    TaskTracker.RemoveTracking(this);
                    m_Core.TrySetResult(false);
                }
            }

            public override IUniTaskAsyncEnumerable<T> ReadAllAsync(CancellationToken cancellationToken = default) {
                return new ReadAllAsyncEnumerable(this, cancellationToken);
            }

            bool IUniTaskSource<bool>.GetResult(short token) {
                return m_Core.GetResult(token);
            }

            void IUniTaskSource.GetResult(short token) {
                m_Core.GetResult(token);
            }

            UniTaskStatus IUniTaskSource.GetStatus(short token) {
                return m_Core.GetStatus(token);
            }

            void IUniTaskSource.OnCompleted(Action<object> continuation, object state, short token) {
                m_Core.OnCompleted(continuation, state, token);
            }

            UniTaskStatus IUniTaskSource.UnsafeGetStatus() {
                return m_Core.UnsafeGetStatus();
            }

            private static void CancellationCallback(object state) {
                var self = (SingleConsumerUnboundedChannelReader)state;
                self.SingalCancellation(self.m_CancellationToken);
            }

            private sealed class ReadAllAsyncEnumerable : IUniTaskAsyncEnumerable<T>, IUniTaskAsyncEnumerator<T> {
                private readonly Action<object> m_CancellationCallback1Delegate = CancellationCallback1;
                private readonly Action<object> m_CancellationCallback2Delegate = CancellationCallback2;

                private readonly SingleConsumerUnboundedChannelReader m_InnerParent;
                private CancellationToken m_CancellationToken1;
                private CancellationToken m_CancellationToken2;
                private CancellationTokenRegistration m_CancellationTokenRegistration1;
                private CancellationTokenRegistration m_CancellationTokenRegistration2;

                private T m_Current;
                private bool m_CacheValue;
                private bool m_Running;

                public ReadAllAsyncEnumerable(
                    SingleConsumerUnboundedChannelReader parent,
                    CancellationToken cancellationToken
                ) {
                    m_InnerParent = parent;
                    m_CancellationToken1 = cancellationToken;
                }

                public IUniTaskAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
                    if (m_Running) {
                        throw new InvalidOperationException(
                            "Enumerator is already running, does not allow call GetAsyncEnumerator twice."
                        );
                    }

                    if (m_CancellationToken1 != cancellationToken) {
                        m_CancellationToken2 = cancellationToken;
                    }

                    if (m_CancellationToken1.CanBeCanceled) {
                        m_CancellationTokenRegistration1 =
                            m_CancellationToken1.RegisterWithoutCaptureExecutionContext(
                                m_CancellationCallback1Delegate,
                                this
                            );
                    }

                    if (m_CancellationToken2.CanBeCanceled) {
                        m_CancellationTokenRegistration2 =
                            m_CancellationToken2.RegisterWithoutCaptureExecutionContext(
                                m_CancellationCallback2Delegate,
                                this
                            );
                    }

                    m_Running = true;
                    return this;
                }

                public T Current {
                    get {
                        if (m_CacheValue) {
                            return m_Current;
                        }

                        m_InnerParent.TryRead(out m_Current);
                        return m_Current;
                    }
                }

                public UniTask<bool> MoveNextAsync() {
                    m_CacheValue = false;
                    return m_InnerParent.WaitToReadAsync(CancellationToken.None); // ok to use None, registered in ctor.
                }

                public UniTask DisposeAsync() {
                    m_CancellationTokenRegistration1.Dispose();
                    m_CancellationTokenRegistration2.Dispose();
                    return default;
                }

                private static void CancellationCallback1(object state) {
                    var self = (ReadAllAsyncEnumerable)state;
                    self.m_InnerParent.SingalCancellation(self.m_CancellationToken1);
                }

                private static void CancellationCallback2(object state) {
                    var self = (ReadAllAsyncEnumerable)state;
                    self.m_InnerParent.SingalCancellation(self.m_CancellationToken2);
                }
            }
        }
    }
}