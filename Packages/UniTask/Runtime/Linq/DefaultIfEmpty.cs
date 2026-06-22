using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> DefaultIfEmpty<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));

            return new DefaultIfEmpty<TSource>(source, default);
        }

        public static IUniTaskAsyncEnumerable<TSource> DefaultIfEmpty<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            TSource defaultValue
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));

            return new DefaultIfEmpty<TSource>(source, defaultValue);
        }
    }

    internal sealed class DefaultIfEmpty<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly TSource m_DefaultValue;

        public DefaultIfEmpty(IUniTaskAsyncEnumerable<TSource> source, TSource defaultValue) {
            m_Source = source;
            m_DefaultValue = defaultValue;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerDefaultIfEmpty(m_Source, m_DefaultValue, cancellationToken);
        }

        private sealed class InnerDefaultIfEmpty : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private enum EIteratingState : byte {
                Empty,
                Iterating,
                Completed
            }

            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;

            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerSource;
            private readonly TSource m_InnerDefaultValue;
            private CancellationToken m_CancellationToken;

            private EIteratingState m_IteratingState;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;

            public InnerDefaultIfEmpty(
                IUniTaskAsyncEnumerable<TSource> source,
                TSource defaultValue,
                CancellationToken cancellationToken
            ) {
                m_InnerSource = source;
                m_InnerDefaultValue = defaultValue;
                m_CancellationToken = cancellationToken;

                m_IteratingState = EIteratingState.Empty;
                TaskTracker.TrackActiveTask(this, 3);
            }

            public TSource Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();
                mCompletionSource.Reset();

                if (m_IteratingState == EIteratingState.Completed) {
                    return CompletedTasks.False;
                }

                if (m_Enumerator == null) {
                    m_Enumerator = m_InnerSource.GetAsyncEnumerator(m_CancellationToken);
                }

                m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                if (m_Awaiter.IsCompleted) {
                    MoveNextCore(this);
                } else {
                    m_Awaiter.SourceOnCompleted(s_MoveNextCoreDelegate, this);
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private static void MoveNextCore(object state) {
                var self = (InnerDefaultIfEmpty)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        self.m_IteratingState = EIteratingState.Iterating;
                        self.Current = self.m_Enumerator.Current;
                        self.mCompletionSource.TrySetResult(true);
                    } else {
                        if (self.m_IteratingState == EIteratingState.Empty) {
                            self.m_IteratingState = EIteratingState.Completed;

                            self.Current = self.m_InnerDefaultValue;
                            self.mCompletionSource.TrySetResult(true);
                        } else {
                            self.mCompletionSource.TrySetResult(false);
                        }
                    }
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