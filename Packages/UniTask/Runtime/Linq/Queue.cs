using System;
using System.Threading;

namespace Cysharp.Threading.Tasks.Linq {
    public static partial class UniTaskAsyncEnumerable {
        public static IUniTaskAsyncEnumerable<TSource> Queue<TSource>(this IUniTaskAsyncEnumerable<TSource> source) {
            return new QueueOperator<TSource>(source);
        }
    }

    internal sealed class QueueOperator<TSource> : IUniTaskAsyncEnumerable<TSource> {
        private readonly IUniTaskAsyncEnumerable<TSource> m_Source;

        public QueueOperator(IUniTaskAsyncEnumerable<TSource> source) {
            m_Source = source;
        }

        public IUniTaskAsyncEnumerator<TSource> GetAsyncEnumerator(CancellationToken cancellationToken = default) {
            return new Queue(m_Source, cancellationToken);
        }

        private sealed class Queue : IUniTaskAsyncEnumerator<TSource> {
            private readonly IUniTaskAsyncEnumerable<TSource> m_Source;
            private CancellationToken m_CancellationToken;

            private Channel<TSource> m_Channel;
            private IUniTaskAsyncEnumerator<TSource> m_ChannelEnumerator;
            private IUniTaskAsyncEnumerator<TSource> m_SourceEnumerator;
            private bool m_ChannelClosed;

            public Queue(IUniTaskAsyncEnumerable<TSource> source, CancellationToken cancellationToken) {
                m_Source = source;
                m_CancellationToken = cancellationToken;
            }

            public TSource Current => m_ChannelEnumerator.Current;

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();

                if (m_SourceEnumerator == null) {
                    m_SourceEnumerator = m_Source.GetAsyncEnumerator(m_CancellationToken);
                    m_Channel = Channel.CreateSingleConsumerUnbounded<TSource>();

                    m_ChannelEnumerator = m_Channel.Reader.ReadAllAsync().GetAsyncEnumerator(m_CancellationToken);

                    ConsumeAll(this, m_SourceEnumerator, m_Channel).Forget();
                }

                return m_ChannelEnumerator.MoveNextAsync();
            }

            private static async UniTaskVoid ConsumeAll(
                Queue self,
                IUniTaskAsyncEnumerator<TSource> enumerator,
                ChannelWriter<TSource> writer
            ) {
                try {
                    while (await enumerator.MoveNextAsync()) {
                        writer.TryWrite(enumerator.Current);
                    }

                    writer.TryComplete();
                } catch (Exception ex) {
                    writer.TryComplete(ex);
                } finally {
                    self.m_ChannelClosed = true;
                    await enumerator.DisposeAsync();
                }
            }

            public async UniTask DisposeAsync() {
                if (m_SourceEnumerator != null) {
                    await m_SourceEnumerator.DisposeAsync();
                }

                if (m_ChannelEnumerator != null) {
                    await m_ChannelEnumerator.DisposeAsync();
                }

                if (!m_ChannelClosed) {
                    m_ChannelClosed = true;
                    m_Channel.Writer.TryComplete(new OperationCanceledException());
                }
            }
        }
    }
}