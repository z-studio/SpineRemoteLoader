using System.Collections.Generic;

namespace ZStudio.SpineRemoteLoader {
    /// <summary>
    /// 解析 .atlas 文件，提取其中引用的图集页文件名。
    /// 与 spine-unity 内部 <c>SpineAtlasAsset.CreateRuntimeInstance</c> 的解析规则保持一致：
    /// 页名为以 .png 结尾、且不含冒号的行（去掉扩展名）。
    /// </summary>
    public static class SpineAtlasPageParser {
        /// <summary>返回不含扩展名的页名列表，按出现顺序。</summary>
        public static List<string> GetPageNames(string atlasText) {
            var result = new List<string>();

            if (string.IsNullOrEmpty(atlasText)) {
                return result;
            }

            var normalized = atlasText.Replace("\r", "");
            var lines = normalized.Split('\n');

            foreach (var raw in lines) {
                var line = raw.Trim();

                if (line.Length == 0) {
                    continue;
                }

                // 属性行形如 "size: 1024,1024"，含冒号；页头行是纯文件名。
                if (line.Contains(":")) {
                    continue;
                }

                if (line.EndsWith(".png")) {
                    result.Add(line.Substring(0, line.Length - ".png".Length));
                }
            }

            return result;
        }

        /// <summary>探测 atlas 是否声明了预乘 Alpha（pma: true）。</summary>
        public static bool DetectPremultiplyAlpha(string atlasText) {
            if (string.IsNullOrEmpty(atlasText)) {
                return false;
            }

            var normalized = atlasText.Replace("\r", "");
            var lines = normalized.Split('\n');

            foreach (var raw in lines) {
                var line = raw.Trim();

                if (line.StartsWith("pma:")) {
                    var value = line.Substring("pma:".Length).Trim();

                    if (value.Equals("true", System.StringComparison.OrdinalIgnoreCase)) {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}