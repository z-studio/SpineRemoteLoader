using UnityEngine.Networking;

namespace Cysharp.Threading.Tasks.Internal {
#if ENABLE_UNITYWEBREQUEST && UNITASK_WEBREQUEST_SUPPORT

    internal static class UnityWebRequestResultExtensions {
        public static bool IsError(this UnityWebRequest unityWebRequest) {
            var result = unityWebRequest.result;

            return result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.DataProcessingError or UnityWebRequest.Result.ProtocolError;
        }
    }

#endif
}