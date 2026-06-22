using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> SkipWhile<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Boolean> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new SkipWhile<TSource>(source, predicate);
        }

        public static IUniTaskAsyncEnumerable<TSource> SkipWhile<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, Boolean> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new SkipWhileInt<TSource>(source, predicate);
        }

        public static IUniTaskAsyncEnumerable<TSource> SkipWhileAwait<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<Boolean>> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new SkipWhileAwait<TSource>(source, predicate);
        }

        public static IUniTaskAsyncEnumerable<TSource> SkipWhileAwait<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, UniTask<Boolean>> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new SkipWhileIntAwait<TSource>(source, predicate);
        }

        public static IUniTaskAsyncEnumerable<TSource> SkipWhileAwaitWithCancellation<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<Boolean>> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new SkipWhileAwaitWithCancellation<TSource>(source, predicate);
        }

        public static IUniTaskAsyncEnumerable<TSource> SkipWhileAwaitWithCancellation<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, CancellationToken, UniTask<Boolean>> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new SkipWhileIntAwaitWithCancellation<TSource>(source, predicate);
        }
    }

    internal sealed class SkipWhile<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, bool> m_Predicate;

        public SkipWhile(IUniTaskAsyncEnumerable<TSource> source, Func<TSource, bool> predicate) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerSkipWhile(m_Source, m_Predicate, cancellationToken);
        }

        private class InnerSkipWhile : AsyncEnumeratorBase<TSource, TSource> {
            private Func<TSource, bool> m_Predicate;

            public InnerSkipWhile(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, bool> predicate,
                CancellationToken cancellationToken
            ) : base(source, cancellationToken) {
                m_Predicate = predicate;
            }

            protected override bool TryMoveNextCore(bool sourceHasCurrent, out bool result) {
                if (sourceHasCurrent) {
                    if (m_Predicate == null || !m_Predicate(SourceCurrent)) {
                        m_Predicate = null;
                        Current = SourceCurrent;
                        result = true;
                        return true;
                    } else {
                        result = default;
                        return false;
                    }
                }

                result = false;
                return true;
            }
        }
    }

    internal sealed class SkipWhileInt<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, int, bool> m_Predicate;

        public SkipWhileInt(IUniTaskAsyncEnumerable<TSource> source, Func<TSource, int, bool> predicate) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerSkipWhileInt(m_Source, m_Predicate, cancellationToken);
        }

        private class InnerSkipWhileInt : AsyncEnumeratorBase<TSource, TSource> {
            private Func<TSource, int, bool> m_Predicate;
            private int m_Index;

            public InnerSkipWhileInt(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, int, bool> predicate,
                CancellationToken cancellationToken
            ) : base(source, cancellationToken) {
                m_Predicate = predicate;
            }

            protected override bool TryMoveNextCore(bool sourceHasCurrent, out bool result) {
                if (sourceHasCurrent) {
                    if (m_Predicate == null || !m_Predicate(SourceCurrent, checked(m_Index++))) {
                        m_Predicate = null;
                        Current = SourceCurrent;
                        result = true;
                        return true;
                    } else {
                        result = default;
                        return false;
                    }
                }

                result = false;
                return true;
            }
        }
    }

    internal sealed class SkipWhileAwait<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, UniTask<bool>> m_Predicate;

        public SkipWhileAwait(IUniTaskAsyncEnumerable<TSource> source, Func<TSource, UniTask<bool>> predicate) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerSkipWhileAwait(m_Source, m_Predicate, cancellationToken);
        }

        private class InnerSkipWhileAwait : AsyncEnumeratorAwaitSelectorBase<TSource, TSource, bool> {
            private Func<TSource, UniTask<bool>> m_Predicate;

            public InnerSkipWhileAwait(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, UniTask<bool>> predicate,
                CancellationToken cancellationToken
            ) : base(source, cancellationToken) {
                m_Predicate = predicate;
            }

            protected override UniTask<bool> TransformAsync(TSource sourceCurrent) {
                if (m_Predicate == null) {
                    return CompletedTasks.False;
                }

                return m_Predicate(sourceCurrent);
            }

            protected override bool TrySetCurrentCore(bool awaitResult, out bool terminateIteration) {
                if (!awaitResult) {
                    m_Predicate = null;
                    Current = SourceCurrent;
                    terminateIteration = false;
                    return true;
                } else {
                    terminateIteration = false;
                    return false;
                }
            }
        }
    }

    internal sealed class SkipWhileIntAwait<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, int, UniTask<bool>> m_Predicate;

        public SkipWhileIntAwait(IUniTaskAsyncEnumerable<TSource> source, Func<TSource, int, UniTask<bool>> predicate) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerSkipWhileIntAwait(m_Source, m_Predicate, cancellationToken);
        }

        private class InnerSkipWhileIntAwait : AsyncEnumeratorAwaitSelectorBase<TSource, TSource, bool> {
            private Func<TSource, int, UniTask<bool>> m_Predicate;
            private int m_Index;

            public InnerSkipWhileIntAwait(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, int, UniTask<bool>> predicate,
                CancellationToken cancellationToken
            ) : base(source, cancellationToken) {
                m_Predicate = predicate;
            }

            protected override UniTask<bool> TransformAsync(TSource sourceCurrent) {
                if (m_Predicate == null) {
                    return CompletedTasks.False;
                }

                return m_Predicate(sourceCurrent, checked(m_Index++));
            }

            protected override bool TrySetCurrentCore(bool awaitResult, out bool terminateIteration) {
                terminateIteration = false;

                if (!awaitResult) {
                    m_Predicate = null;
                    Current = SourceCurrent;
                    return true;
                } else {
                    return false;
                }
            }
        }
    }

    internal sealed class SkipWhileAwaitWithCancellation<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, CancellationToken, UniTask<bool>> m_Predicate;

        public SkipWhileAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<bool>> predicate
        ) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerSkipWhileAwaitWithCancellation(m_Source, m_Predicate, cancellationToken);
        }

        private class InnerSkipWhileAwaitWithCancellation : AsyncEnumeratorAwaitSelectorBase<TSource, TSource, bool> {
            private Func<TSource, CancellationToken, UniTask<bool>> m_Predicate;

            public InnerSkipWhileAwaitWithCancellation(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, UniTask<bool>> predicate,
                CancellationToken cancellationToken
            ) : base(source, cancellationToken) {
                m_Predicate = predicate;
            }

            protected override UniTask<bool> TransformAsync(TSource sourceCurrent) {
                if (m_Predicate == null) {
                    return CompletedTasks.False;
                }

                return m_Predicate(sourceCurrent, cancellationToken);
            }

            protected override bool TrySetCurrentCore(bool awaitResult, out bool terminateIteration) {
                terminateIteration = false;

                if (!awaitResult) {
                    m_Predicate = null;
                    Current = SourceCurrent;
                    return true;
                } else {
                    return false;
                }
            }
        }
    }

    internal sealed class SkipWhileIntAwaitWithCancellation<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, int, CancellationToken, UniTask<bool>> m_Predicate;

        public SkipWhileIntAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, int, CancellationToken, UniTask<bool>> predicate
        ) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerSkipWhileIntAwaitWithCancellation(m_Source, m_Predicate, cancellationToken);
        }

        private class InnerSkipWhileIntAwaitWithCancellation : AsyncEnumeratorAwaitSelectorBase<TSource, TSource, bool> {
            private Func<TSource, int, CancellationToken, UniTask<bool>> m_Predicate;
            private int m_Index;

            public InnerSkipWhileIntAwaitWithCancellation(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, int, CancellationToken, UniTask<bool>> predicate,
                CancellationToken cancellationToken
            ) : base(source, cancellationToken) {
                m_Predicate = predicate;
            }

            protected override UniTask<bool> TransformAsync(TSource sourceCurrent) {
                if (m_Predicate == null) {
                    return CompletedTasks.False;
                }

                return m_Predicate(sourceCurrent, checked(m_Index++), cancellationToken);
            }

            protected override bool TrySetCurrentCore(bool awaitResult, out bool terminateIteration) {
                terminateIteration = false;

                if (!awaitResult) {
                    m_Predicate = null;
                    Current = SourceCurrent;
                    return true;
                } else {
                    return false;
                }
            }
        }
    }
}