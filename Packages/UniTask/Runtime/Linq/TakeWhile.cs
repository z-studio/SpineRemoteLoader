using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> TakeWhile<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Boolean> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new TakeWhile<TSource>(source, predicate);
        }

        public static IUniTaskAsyncEnumerable<TSource> TakeWhile<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, Boolean> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new TakeWhileInt<TSource>(source, predicate);
        }

        public static IUniTaskAsyncEnumerable<TSource> TakeWhileAwait<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, UniTask<Boolean>> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new TakeWhileAwait<TSource>(source, predicate);
        }

        public static IUniTaskAsyncEnumerable<TSource> TakeWhileAwait<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, UniTask<Boolean>> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new TakeWhileIntAwait<TSource>(source, predicate);
        }

        public static IUniTaskAsyncEnumerable<TSource> TakeWhileAwaitWithCancellation<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<Boolean>> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new TakeWhileAwaitWithCancellation<TSource>(source, predicate);
        }

        public static IUniTaskAsyncEnumerable<TSource> TakeWhileAwaitWithCancellation<TSource>(
            this IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, Int32, CancellationToken, UniTask<Boolean>> predicate
        ) {
            Error.ThrowArgumentNullException(source, nameof(source));
            Error.ThrowArgumentNullException(predicate, nameof(predicate));

            return new TakeWhileIntAwaitWithCancellation<TSource>(source, predicate);
        }
    }

    internal sealed class TakeWhile<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, bool> m_Predicate;

        public TakeWhile(IUniTaskAsyncEnumerable<TSource> source, Func<TSource, bool> predicate) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerTakeWhile(m_Source, m_Predicate, cancellationToken);
        }

        private class InnerTakeWhile : AsyncEnumeratorBase<TSource, TSource> {
            private Func<TSource, bool> m_Predicate;

            public InnerTakeWhile(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, bool> predicate,
                CancellationToken cancellationToken
            ) : base(source, cancellationToken) {
                m_Predicate = predicate;
            }

            protected override bool TryMoveNextCore(bool sourceHasCurrent, out bool result) {
                if (sourceHasCurrent) {
                    if (m_Predicate(SourceCurrent)) {
                        Current = SourceCurrent;
                        result = true;
                        return true;
                    }
                }

                result = false;
                return true;
            }
        }
    }

    internal sealed class TakeWhileInt<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, int, bool> m_Predicate;

        public TakeWhileInt(IUniTaskAsyncEnumerable<TSource> source, Func<TSource, int, bool> predicate) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerTakeWhileInt(m_Source, m_Predicate, cancellationToken);
        }

        private class InnerTakeWhileInt : AsyncEnumeratorBase<TSource, TSource> {
            private readonly Func<TSource, int, bool> m_Predicate;
            private int m_Index;

            public InnerTakeWhileInt(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, int, bool> predicate,
                CancellationToken cancellationToken
            ) : base(source, cancellationToken) {
                m_Predicate = predicate;
            }

            protected override bool TryMoveNextCore(bool sourceHasCurrent, out bool result) {
                if (sourceHasCurrent) {
                    if (m_Predicate(SourceCurrent, checked(m_Index++))) {
                        Current = SourceCurrent;
                        result = true;
                        return true;
                    }
                }

                result = false;
                return true;
            }
        }
    }

    internal sealed class TakeWhileAwait<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, UniTask<bool>> m_Predicate;

        public TakeWhileAwait(IUniTaskAsyncEnumerable<TSource> source, Func<TSource, UniTask<bool>> predicate) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerTakeWhileAwait(m_Source, m_Predicate, cancellationToken);
        }

        private class InnerTakeWhileAwait : AsyncEnumeratorAwaitSelectorBase<TSource, TSource, bool> {
            private Func<TSource, UniTask<bool>> m_Predicate;

            public InnerTakeWhileAwait(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, UniTask<bool>> predicate,
                CancellationToken cancellationToken
            ) : base(source, cancellationToken) {
                m_Predicate = predicate;
            }

            protected override UniTask<bool> TransformAsync(TSource sourceCurrent) {
                return m_Predicate(sourceCurrent);
            }

            protected override bool TrySetCurrentCore(bool awaitResult, out bool terminateIteration) {
                if (awaitResult) {
                    Current = SourceCurrent;
                    terminateIteration = false;
                    return true;
                } else {
                    terminateIteration = true;
                    return false;
                }
            }
        }
    }

    internal sealed class TakeWhileIntAwait<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, int, UniTask<bool>> m_Predicate;

        public TakeWhileIntAwait(IUniTaskAsyncEnumerable<TSource> source, Func<TSource, int, UniTask<bool>> predicate) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerTakeWhileIntAwait(m_Source, m_Predicate, cancellationToken);
        }

        private class InnerTakeWhileIntAwait : AsyncEnumeratorAwaitSelectorBase<TSource, TSource, bool> {
            private readonly Func<TSource, int, UniTask<bool>> m_Predicate;
            private int m_Index;

            public InnerTakeWhileIntAwait(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, int, UniTask<bool>> predicate,
                CancellationToken cancellationToken
            ) : base(source, cancellationToken) {
                m_Predicate = predicate;
            }

            protected override UniTask<bool> TransformAsync(TSource sourceCurrent) {
                return m_Predicate(sourceCurrent, checked(m_Index++));
            }

            protected override bool TrySetCurrentCore(bool awaitResult, out bool terminateIteration) {
                if (awaitResult) {
                    Current = SourceCurrent;
                    terminateIteration = false;
                    return true;
                } else {
                    terminateIteration = true;
                    return false;
                }
            }
        }
    }

    internal sealed class TakeWhileAwaitWithCancellation<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, CancellationToken, UniTask<bool>> m_Predicate;

        public TakeWhileAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, CancellationToken, UniTask<bool>> predicate
        ) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerTakeWhileAwaitWithCancellation(m_Source, m_Predicate, cancellationToken);
        }

        private class InnerTakeWhileAwaitWithCancellation : AsyncEnumeratorAwaitSelectorBase<TSource, TSource, bool> {
            private Func<TSource, CancellationToken, UniTask<bool>> m_Predicate;

            public InnerTakeWhileAwaitWithCancellation(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, CancellationToken, UniTask<bool>> predicate,
                CancellationToken cancellationToken
            ) : base(source, cancellationToken) {
                m_Predicate = predicate;
            }

            protected override UniTask<bool> TransformAsync(TSource sourceCurrent) {
                return m_Predicate(sourceCurrent, cancellationToken);
            }

            protected override bool TrySetCurrentCore(bool awaitResult, out bool terminateIteration) {
                if (awaitResult) {
                    Current = SourceCurrent;
                    terminateIteration = false;
                    return true;
                } else {
                    terminateIteration = true;
                    return false;
                }
            }
        }
    }

    internal sealed class TakeWhileIntAwaitWithCancellation<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
        private readonly Func<TSource, int, CancellationToken, UniTask<bool>> m_Predicate;

        public TakeWhileIntAwaitWithCancellation(
            IUniTaskAsyncEnumerable<TSource> source,
            Func<TSource, int, CancellationToken, UniTask<bool>> predicate
        ) {
            m_Source = source;
            m_Predicate = predicate;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerTakeWhileIntAwaitWithCancellation(m_Source, m_Predicate, cancellationToken);
        }

        private class InnerTakeWhileIntAwaitWithCancellation : AsyncEnumeratorAwaitSelectorBase<TSource, TSource, bool> {
            private readonly Func<TSource, int, CancellationToken, UniTask<bool>> m_Predicate;
            private int m_Index;

            public InnerTakeWhileIntAwaitWithCancellation(
                IUniTaskAsyncEnumerable<TSource> source,
                Func<TSource, int, CancellationToken, UniTask<bool>> predicate,
                CancellationToken cancellationToken
            ) : base(source, cancellationToken) {
                m_Predicate = predicate;
            }

            protected override UniTask<bool> TransformAsync(TSource sourceCurrent) {
                return m_Predicate(sourceCurrent, checked(m_Index++), cancellationToken);
            }

            protected override bool TrySetCurrentCore(bool awaitResult, out bool terminateIteration) {
                if (awaitResult) {
                    Current = SourceCurrent;
                    terminateIteration = false;
                    return true;
                } else {
                    terminateIteration = true;
                    return false;
                }
            }
        }
    }
}