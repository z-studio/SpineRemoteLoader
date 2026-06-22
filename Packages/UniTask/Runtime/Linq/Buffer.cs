using Cysharp.Threading.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<IList<TSource>> Buffer<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Int32 count
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));

            if (count <= 0) {
                throw Error.ArgumentOutOfRange(nameof(count));
            }

            return new Buffer<TSource>(source, count);
        }

        public static IUniTaskAsyncEnumerable<IList<TSource>> Buffer<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Int32 count,
            Int32 skip
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));

            if (count <= 0) {
                throw Error.ArgumentOutOfRange(nameof(count));
            }

            if (skip <= 0) {
                throw Error.ArgumentOutOfRange(nameof(skip));
            }

            return new BufferSkip<TSource>(source, count, skip);
        }
    }

    internal sealed class Buffer<TSource> : IUniTaskAsyncEnumerable<IList<TSource>> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly int m_Count;

        public Buffer(IUniTaskAsyncEnumerable<TSource> source, int count) {
            m_Source = source;
            m_Count = count;
        }

        public IUniTaskAsyncEnumerator<IList<TSource>> GetAsyncEnumerator(
            CancellationToken cancellationToken = default
        ) {
            return new InnerBuffer(m_Source, m_Count, cancellationToken);
        }

        private sealed class InnerBuffer : MoveNextSource, IUniTaskAsyncEnumerator<IList<TSource>> {
            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;

            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerSource;
            private readonly int m_InnerCount;
            private CancellationToken m_CancellationToken;

            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private bool m_ContinueNext;

            private bool m_Completed;
            private List<TSource> m_Buffer;

            public InnerBuffer(IUniTaskAsyncEnumerable<TSource> source, int count, CancellationToken cancellationToken) {
                m_InnerSource = source;
                m_InnerCount = count;
                m_CancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(this, 3);
            }

            public IList<TSource> Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();

                if (m_Enumerator == null) {
                    m_Enumerator = m_InnerSource.GetAsyncEnumerator(m_CancellationToken);
                    m_Buffer = new List<TSource>(m_InnerCount);
                }

                mCompletionSource.Reset();
                SourceMoveNext();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void SourceMoveNext() {
                if (m_Completed) {
                    if (m_Buffer != null && m_Buffer.Count > 0) {
                        var ret = m_Buffer;
                        m_Buffer = null;
                        Current = ret;
                        mCompletionSource.TrySetResult(true);
                        return;
                    } else {
                        mCompletionSource.TrySetResult(false);
                        return;
                    }
                }

                try {
                    LOOP:
                    m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                    if (m_Awaiter.IsCompleted) {
                        m_ContinueNext = true;
                        MoveNextCore(this);

                        if (m_ContinueNext) {
                            m_ContinueNext = false;
                            goto LOOP; // avoid recursive
                        }
                    } else {
                        m_Awaiter.SourceOnCompleted(s_MoveNextCoreDelegate, this);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                }
            }

            private static void MoveNextCore(object state) {
                var self = (InnerBuffer)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        self.m_Buffer.Add(self.m_Enumerator.Current);

                        if (self.m_Buffer.Count == self.m_InnerCount) {
                            self.Current = self.m_Buffer;
                            self.m_Buffer = new List<TSource>(self.m_InnerCount);
                            self.m_ContinueNext = false;
                            self.mCompletionSource.TrySetResult(true);
                            return;
                        } else {
                            if (!self.m_ContinueNext) {
                                self.SourceMoveNext();
                            }
                        }
                    } else {
                        self.m_ContinueNext = false;
                        self.m_Completed = true;
                        self.SourceMoveNext();
                    }
                } else {
                    self.m_ContinueNext = false;
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_Enumerator != null) {
                    return m_Enumerator.DisposeAsync();
                }

                return default;
            }
        }
    }

    internal sealed class BufferSkip<TSource> : IUniTaskAsyncEnumerable<IList<TSource>> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly int m_Count;
        private readonly int m_Skip;

        public BufferSkip(IUniTaskAsyncEnumerable<TSource> source, int count, int skip) {
            m_Source = source;
            m_Count = count;
            m_Skip = skip;
        }

        public IUniTaskAsyncEnumerator<IList<TSource>> GetAsyncEnumerator(
            CancellationToken cancellationToken = default
        ) {
            return new InnerBufferSkip(m_Source, m_Count, m_Skip, cancellationToken);
        }

        private sealed class InnerBufferSkip : MoveNextSource, IUniTaskAsyncEnumerator<IList<TSource>> {
            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;

            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerSource;
            private readonly int m_InnerCount;
            private readonly int m_InnerSkip;
            private CancellationToken m_CancellationToken;

            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;
            private bool m_ContinueNext;

            private bool m_Completed;
            private Queue<List<TSource>> m_Buffers;
            private int m_Index = 0;

            public InnerBufferSkip(
                IUniTaskAsyncEnumerable<TSource> source,
                int count,
                int skip,
                CancellationToken cancellationToken
            ) {
                m_InnerSource = source;
                m_InnerCount = count;
                m_InnerSkip = skip;
                m_CancellationToken = cancellationToken;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public IList<TSource> Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();

                if (m_Enumerator == null) {
                    m_Enumerator = m_InnerSource.GetAsyncEnumerator(m_CancellationToken);
                    m_Buffers = new Queue<List<TSource>>();
                }

                mCompletionSource.Reset();
                SourceMoveNext();
                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private void SourceMoveNext() {
                if (m_Completed) {
                    if (m_Buffers.Count > 0) {
                        Current = m_Buffers.Dequeue();
                        mCompletionSource.TrySetResult(true);
                        return;
                    } else {
                        mCompletionSource.TrySetResult(false);
                        return;
                    }
                }

                try {
                    LOOP:
                    m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                    if (m_Awaiter.IsCompleted) {
                        m_ContinueNext = true;
                        MoveNextCore(this);

                        if (m_ContinueNext) {
                            m_ContinueNext = false;
                            goto LOOP; // avoid recursive
                        }
                    } else {
                        m_Awaiter.SourceOnCompleted(s_MoveNextCoreDelegate, this);
                    }
                } catch (Exception ex) {
                    mCompletionSource.TrySetException(ex);
                }
            }

            private static void MoveNextCore(object state) {
                var self = (InnerBufferSkip)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        if (self.m_Index++ % self.m_InnerSkip == 0) {
                            self.m_Buffers.Enqueue(new List<TSource>(self.m_InnerCount));
                        }

                        var item = self.m_Enumerator.Current;

                        foreach (var buffer in self.m_Buffers) {
                            buffer.Add(item);
                        }

                        if (self.m_Buffers.Count > 0 && self.m_Buffers.Peek().Count == self.m_InnerCount) {
                            self.Current = self.m_Buffers.Dequeue();
                            self.m_ContinueNext = false;
                            self.mCompletionSource.TrySetResult(true);
                            return;
                        } else {
                            if (!self.m_ContinueNext) {
                                self.SourceMoveNext();
                            }
                        }
                    } else {
                        self.m_ContinueNext = false;
                        self.m_Completed = true;
                        self.SourceMoveNext();
                    }
                } else {
                    self.m_ContinueNext = false;
                }
            }

            public UniTask DisposeAsync() {
                TaskTracker.RemoveTracking(this);

                if (m_Enumerator != null) {
                    return m_Enumerator.DisposeAsync();
                }

                return default;
            }
        }
    }
}