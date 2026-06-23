using UnityEngine;

namespace ZStudio.SpineRemoteLoader {
    public enum ESpineLogLevel {
        Verbose = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        None = 4
    }

    /// <summary>
    /// 库内统一日志出口，业务层可通过 <see cref="sLevel"/> 控制输出等级。
    /// </summary>
    public static class SpineRemoteLog {
        private const string k_Tag = "[SpineRemoteLoader]";

        /// <summary>输出等级，默认 <see cref="ESpineLogLevel.Warning"/>。</summary>
        public static ESpineLogLevel sLevel = ESpineLogLevel.Warning;

        public static void Verbose(string message) {
            if (sLevel <= ESpineLogLevel.Verbose) {
                Debug.Log($"{k_Tag} {message}");
            }
        }

        public static void Info(string message) {
            if (sLevel <= ESpineLogLevel.Info) {
                Debug.Log($"{k_Tag} {message}");
            }
        }

        public static void Warning(string message) {
            if (sLevel <= ESpineLogLevel.Warning) {
                Debug.LogWarning($"{k_Tag} {message}");
            }
        }

        public static void Error(string message) {
            if (sLevel <= ESpineLogLevel.Error) {
                Debug.LogError($"{k_Tag} {message}");
            }
        }
    }
}
