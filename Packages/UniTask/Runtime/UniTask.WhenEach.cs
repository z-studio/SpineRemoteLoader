using Cysharp.Threading.Tasks.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace Cysharp.Threading.Tasks {
    public partial struct UniTask {
        public static IUniTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(IEnumerable<UniTask<T>> tasks) {
            return new WhenEachEnumerable<T>(tasks);
        }

        public static IUniTaskAsyncEnumerable<WhenEachResult<T>> WhenEach<T>(params UniTask<T>[] tasks) {
            return new WhenEachEnumerable<T>(tasks);
        }
    }

    public readonly struct WhenEachResult<T> {
        public T Result { get; }
        public Exception Exception { get; }

        //[MemberNotNullWhen(false, nameof(Exception))]
        public bool IsCompletedSuccessfully => Exception == null;

        //[MemberNotNullWhen(true, nameof(Exception))]
        public bool IsFaulted => Exception != null;

        public WhenEachResult(T result) {
            Result = result;
            Exception = null;
        }

        public WhenEachResult(Exception exception) {
            if (exception == null) {
                throw new ArgumentNullException(nameof(exception));
            }

            Result = default;
            Exception = exception;
        }

        public void TryThrow() {
            if (IsFaulted) {
                ExceptionDispatchInfo.Capture(Exception).Throw();
            }
        }

        public T GetResult() {
            if (IsFaulted) {
                ExceptionDispatchInfo.Capture(Exception).Throw();
            }

            return Result;
        }

        public override string ToString() {
            if (IsCompletedSuccessfully) {
                return Result?.ToString() ?? "";
            } else {
                return $"Exception{{{Exception.Message}}}";
            }
        }
    }

    internal enum WhenEachState : byte {
        NotRunning,
        Running,
        Completed
    }

    internal sealed class WhenEachEnumerable<T> : IUniTaskAsyncEnumerable<WhenEachResult<T>> {
        private IEnumerable<UniTask<T>> m_Source;

        public WhenEachEnumerable(IEnumerable<UniTask<T>> source) {
            m_Source = source;
        }

        public IUniTaskAsyncEnumerator<WhenEachResult<T>> GetAsyncEnumerator(
            CancellationToken cancellationToken = default
        ) {
            return new Enumerator(m_Source, cancellationToken);
        }

        private sealed class Enumerator : IUniTaskAsyncEnumerator<WhenEachResult<T>> {
            private readonly IEnumerable<UniTask<T>> m_InnerSource;
            private CancellationToken m_CancellationToken;

            private Channel<WhenEachResult<T>> m_Channel;
            private IUniTaskAsyncEnumerator<WhenEachResult<T>> m_ChannelEnumerator;
            private int m_CompleteCount;
            private WhenEachState m_State;

            public Enumerator(IEnumerable<UniTask<T>> source, CancellationToken cancellationToken) {
                m_InnerSource = source;
                m_CancellationToken = cancellationToken;
            }

            public WhenEachResult<T> Current => m_ChannelEnumerator.Current;

            public UniTask<bool> MoveNextAsync() {
                m_CancellationToken.ThrowIfCancellationRequested();

                if (m_State == WhenEachState.NotRunning) {
                    m_State = WhenEachState.Running;
                    m_Channel = Channel.CreateSingleConsumerUnbounded<WhenEachResult<T>>();
                    m_ChannelEnumerator = m_Channel.Reader.ReadAllAsync().GetAsyncEnumerator(m_CancellationToken);

                    if (m_InnerSource is UniTask<T>[] array) {
                        ConsumeAll(this, array, array.Length);
                    } else {
                        using (var rentArray = ArrayPoolUtil.Materialize(m_InnerSource)) {
                            ConsumeAll(this, rentArray.Array, rentArray.Length);
                        }
                    }
                }

                return m_ChannelEnumerator.MoveNextAsync();
            }

            private static void ConsumeAll(Enumerator self, UniTask<T>[] array, int length) {
                for (var i = 0; i < length; i++) {
                    RunWhenEachTask(self, array[i], length).Forget();
                }
            }

            private static async UniTaskVoid RunWhenEachTask(Enumerator self, UniTask<T> task, int length) {
                try {
                    var result = await task;
                    self.m_Channel.Writer.TryWrite(new WhenEachResult<T>(result));
                } catch (Exception ex) {
                    self.m_Channel.Writer.TryWrite(new WhenEachResult<T>(ex));
                }

                if (Interlocked.Increment(ref self.m_CompleteCount) == length) {
                    self.m_State = WhenEachState.Completed;
                    self.m_Channel.Writer.TryComplete();
                }
            }

            public async UniTask DisposeAsync() {
                if (m_ChannelEnumerator != null) {
                    await m_ChannelEnumerator.DisposeAsync();
                }

                if (m_State != WhenEachState.Completed) {
                    m_State = WhenEachState.Completed;
                    m_Channel.Writer.TryComplete(new OperationCanceledException());
                }
            }
        }
    }
}