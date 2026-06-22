using System.Threading;
using Cysharp.Threading.Tasks;

namespace ZStudio.SpineRemoteLoader {
    /// <summary>
    /// 可插拔的下载后端抽象。库默认使用 <see cref="UnityWebRequestSpineDownloader"/>，
    /// 业务层可注入自定义实现（如基于 Best.HTTP 的下载器）。
    /// </summary>
    public interface ISpineDownloader {
        /// <summary>
        /// 下载指定 URL 的字节内容。
        /// 失败时应返回 null（不要抛异常）；取消时应抛出 <see cref="System.OperationCanceledException"/>。
        /// 重试由上层负责，本方法只需完成一次请求。
        /// </summary>
        UniTask<byte[]> GetBytesAsync(string url, int timeoutSeconds, CancellationToken cancellationToken);
    }
}
