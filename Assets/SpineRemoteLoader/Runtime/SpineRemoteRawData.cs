using System.Collections.Generic;

namespace ZStudio.SpineRemoteLoader {
    /// <summary>
    /// 下载或从磁盘读取得到的原始 Spine 资源字节，渲染无关。
    /// 可被序列化进磁盘缓存，也可在内存缓存中复用。
    /// </summary>
    public sealed class SpineRemoteRawData {
        public string skeletonExtension; // "skel" / "json"
        public byte[] skeletonBytes;
        public string atlasText;
        public List<SpineRemotePage> pages = new();

        public sealed class SpineRemotePage {
            public string name; // 不含扩展名
            public byte[] pngBytes;

            public SpineRemotePage() { }

            public SpineRemotePage(string name, byte[] pngBytes) {
                this.name = name;
                this.pngBytes = pngBytes;
            }
        }

        public bool IsValid() {
            if (skeletonBytes == null || skeletonBytes.Length == 0) {
                return false;
            }

            if (string.IsNullOrEmpty(atlasText)) {
                return false;
            }

            if (pages == null || pages.Count == 0) {
                return false;
            }

            foreach (var page in pages) {
                if (page == null || string.IsNullOrEmpty(page.name) || page.pngBytes == null || page.pngBytes.Length == 0) {
                    return false;
                }
            }

            return true;
        }
    }
}
