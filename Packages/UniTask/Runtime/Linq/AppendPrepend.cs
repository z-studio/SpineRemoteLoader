using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> Append<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            TSource element
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));

            return new AppendPrepend<TSource>(source, element, true);
        }

        public static IUniTaskAsyncEnumerable<TSource> Prepend<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            TSource element
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));

            return new AppendPrepend<TSource>(source, element, false);
        }
    }

    internal sealed class AppendPrepend<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly TSource m_Element;
        private readonly bool m_Append; // or prepend

        public AppendPrepend(IUniTaskAsyncEnumerable<TSource> source, TSource element, bool append) {
            m_Source = source;
            m_Element = element;
            m_Append = append;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerAppendPrepend(m_Source, m_Element, m_Append, cancellationToken);
        }

        private sealed class InnerAppendPrepend : MoveNextSource, IUniTaskAsyncEnumerator<TSource> {
            private enum EState : byte {
                None,
                RequirePrepend,
                RequireAppend,
                Completed
            }

            private static readonly Action<object> s_MoveNextCoreDelegate = MoveNextCore;

            private readonly IUniTaskAsyncEnumerable<TSource> m_InnerSource;
            private readonly TSource m_InnerElement;
            private CancellationToken m_CancellationToken;

            private EState m_State;
            private IUniTaskAsyncEnumerator<TSource> m_Enumerator;
            private UniTask<bool>.Awaiter m_Awaiter;

            public InnerAppendPrepend(
                IUniTaskAsyncEnumerable<TSource> source,
                TSource element,
                bool append,
                CancellationToken cancellationToken
            ) {
                m_InnerSource = source;
                m_InnerElement = element;
                m_State = append ? EState.RequireAppend : EState.RequirePrepend;
                m_CancellationToken = cancellationToken;

                TaskTracker.TrackActiveTask(this, 3);
            }

            public TSource Current { get; private set; }

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();
                mCompletionSource.Reset();

                if (m_Enumerator == null) {
                    if (m_State == EState.RequirePrepend) {
                        Current = m_InnerElement;
                        m_State = EState.None;
                        return CompletedTasks.True;
                    }

                    m_Enumerator = m_InnerSource.GetAsyncEnumerator(m_CancellationToken);
                }

                if (m_State == EState.Completed) {
                    return CompletedTasks.False;
                }

                m_Awaiter = m_Enumerator.MoveNextAsync().GetAwaiter();

                if (m_Awaiter.IsCompleted) {
                    s_MoveNextCoreDelegate(this);
                } else {
                    m_Awaiter.SourceOnCompleted(s_MoveNextCoreDelegate, this);
                }

                return new UniTask<bool>(this, mCompletionSource.Version);
            }

            private static void MoveNextCore(object state) {
                var self = (InnerAppendPrepend)state;

                if (self.TryGetResult(self.m_Awaiter, out var result)) {
                    if (result) {
                        self.Current = self.m_Enumerator.Current;
                        self.mCompletionSource.TrySetResult(true);
                    } else {
                        if (self.m_State == EState.RequireAppend) {
                            self.m_State = EState.Completed;
                            self.Current = self.m_InnerElement;
                            self.mCompletionSource.TrySetResult(true);
                        } else {
                            self.m_State = EState.Completed;
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