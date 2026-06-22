using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ZStudio.SpineRemoteLoader {
    /// <summary>
    /// 在 <see cref="ISpineDownloader"/> 之上提供统一的重试逻辑。
    /// </summary>
    internal static class SpineRemoteDownloader {
        public static async UniTask<byte[]> DownloadBytesAsync(
            ISpineDownloader downloader,
            string url,
            int timeoutSeconds,
            int retryCount,
            float retryIntervalSeconds,
            CancellationToken cancellationToken
        ) {
            var attempt = 0;

            while (true) {
                cancellationToken.ThrowIfCancellationRequested();

                byte[] data = null;

                try {
                    data = await downloader.GetBytesAsync(url, timeoutSeconds, cancellationToken);
                } catch (OperationCanceledException) {
                    throw;
                } catch (Exception e) {
                    SpineRemoteLog.Warning($"下载异常({attempt + 1}/{retryCount + 1}): {url}, {e.Message}");
                }

                if (data != null && data.Length > 0) {
                    return data;
                }

                if (attempt >= retryCount) {
                    SpineRemoteLog.Error($"下载最终失败: {url}");
                    return null;
                }

                attempt++;

                if (retryIntervalSeconds > 0f) {
                    await UniTask.Delay(TimeSpan.FromSeconds(retryIntervalSeconds), cancellationToken: cancellationToken);
                }
            }
        }
    }
}