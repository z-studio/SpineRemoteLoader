using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace ZStudio.SpineRemoteLoader {
    /// <summary>
    /// 基于 <see cref="UnityWebRequest"/> 的默认下载实现。
    /// </summary>
    public sealed class UnityWebRequestSpineDownloader : ISpineDownloader {
        public async UniTask<byte[]> GetBytesAsync(string url, int timeoutSeconds, CancellationToken cancellationToken) {
            using var request = UnityWebRequest.Get(url);
            request.timeout = timeoutSeconds;

            try {
                await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
            } catch (OperationCanceledException) {
                throw;
            } catch (Exception e) {
                SpineRemoteLog.Warning($"下载失败: {url}, {e.Message}");
                return null;
            }

            if (request.result != UnityWebRequest.Result.Success) {
                SpineRemoteLog.Warning($"下载失败: {url}, {request.error}");
                return null;
            }

            return request.downloadHandler.data;
        }
    }
}
