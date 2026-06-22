using Cysharp.Threading.Tasks.Internal;
using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TResult> Cast<TResult>(this IUniTaskAsyncEnumerable<Object> source) {
            Error.ThrowArgumentNullException(source, nameof(source));

            return new Cast<TResult>(source);
        }
    }

    internal sealed class Cast<TResult> : IUniTaskAsyncEnumerable<TResult> {
        private readonly IUniTaskAsyncEnumerable<object> m_Source;

        public Cast(IUniTaskAsyncEnumerable<object> source) {
            m_Source = source;
        }

        public IUniTaskAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new InnerCast(m_Source, cancellationToken);
        }

        private class InnerCast : AsyncEnumeratorBase<object, TResult> {
            public InnerCast(IUniTaskAsyncEnumerable<object> source, CancellationToken cancellationToken)
                : base(source, cancellationToken) { }

            protected override bool TryMoveNextCore(bool sourceHasCurrent, out bool result) {
                if (sourceHasCurrent) {
                    Current = (TResult)SourceCurrent;
                    result = true;
                    return true;
                }

                result = false;
                return true;
            }
        }
    }
}