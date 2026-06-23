using System.Collections.Generic;

namespace ZStudio.SpineRemoteLoader {
    /// <summary>
    /// 解析 .atlas 文件，提取其中引用的图集页文件名（用于决定下载哪些 png）。
    ///
    /// 重要：这里的页名规则必须与真正的消费者 <c>spine-unity</c> 的
    /// <c>SpineAtlasAsset.CreateRuntimeInstance(TextAsset, Texture2D[], ...)</c> 完全一致——
    /// 后者内部同样以「行以 .png 结尾」为页头、并按去扩展名后的名字（忽略大小写）匹配 <c>Texture2D.name</c>。
    /// 因此本解析器刻意采用相同的朴素规则；切勿改成"更聪明/更通用"的解析（如 spine 原生 Atlas 的空行分隔规则），
    /// 否则会与 CreateRuntimeInstance 的页名集合脱钩，触发 "Could not find matching atlas page" 异常。
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