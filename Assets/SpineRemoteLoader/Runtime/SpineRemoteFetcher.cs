using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ZStudio.SpineRemoteLoader {
    /// <summary>
    /// 负责按约定的 URL 规则下载 Spine 原始字节（骨骼 / 图集 / 各图集页）并解析图集页名。
    /// 产物 <see cref="SpineRemoteRawData"/> 渲染无关。仅在主线程调用。
    /// </summary>
    internal sealed class SpineRemoteFetcher {
        private readonly ISpineDownloader m_Downloader;

        public SpineRemoteFetcher(ISpineDownloader downloader) {
            m_Downloader = downloader;
        }

        public async UniTask<SpineRemoteRawData> FetchAsync(SpineRemoteLoadOptions options) {
            options.progress?.Report(0f);

            var token = options.cancellationToken;
            var skeletonExtension = options.format == ESpineSkeletonFormat.Binary ? "skel" : "json";
            var baseDir = GetBaseDirectory(options.url);

            var skeletonBytes = await Download($"{options.url}.{skeletonExtension}", options, token);

            if (skeletonBytes == null) {
                return null;
            }

            options.progress?.Report(0.15f);

            var atlasBytes = await Download($"{options.url}.atlas", options, token);

            if (atlasBytes == null) {
                return null;
            }

            options.progress?.Report(0.3f);

            var atlasText = Encoding.UTF8.GetString(atlasBytes);
            var pageNames = SpineAtlasPageParser.GetPageNames(atlasText);

            if (pageNames.Count == 0) {
                SpineRemoteLog.Error($"atlas 未解析到任何图集页: {options.url}.atlas");
                return null;
            }

            var raw = new SpineRemoteRawData {
                skeletonExtension = skeletonExtension,
                skeletonBytes = skeletonBytes,
                atlasText = atlasText
            };

            for (var i = 0; i < pageNames.Count; i++) {
                var pageName = pageNames[i];
                var pageUrl = ResolvePageUrl(options, baseDir, pageName, i);
                var pageBytes = await Download(pageUrl, options, token);

                if (pageBytes == null) {
                    return null;
                }

                raw.pages.Add(new SpineRemoteRawData.SpineRemotePage(pageName, pageBytes));
                options.progress?.Report(0.3f + 0.7f * (i + 1) / pageNames.Count);
            }

            options.progress?.Report(1f);
            return raw;
        }

        private UniTask<byte[]> Download(string url, SpineRemoteLoadOptions options, CancellationToken token) {
            return SpineRemoteDownloader.DownloadBytesAsync(
                m_Downloader,
                url,
                options.timeoutSeconds,
                options.retryCount,
                options.retryIntervalSeconds,
                token
            );
        }

        internal static string GetBaseDirectory(string url) {
            var trimmed = url.TrimEnd('/');
            var lastSlash = trimmed.LastIndexOf('/');
            return lastSlash >= 0 ? trimmed.Substring(0, lastSlash + 1) : string.Empty;
        }

        // 传入 pageImageUrls 时优先使用，否则回退到内部拼接 {baseDir}{页名}.png。
        internal static string ResolvePageUrl(SpineRemoteLoadOptions options, string baseDir, string pageName, int index) {
            var custom = options.pageImageUrls;

            if (custom != null && index < custom.Length && !string.IsNullOrEmpty(custom[index])) {
                return custom[index];
            }

            return $"{baseDir}{pageName}.png";
        }
    }
}
