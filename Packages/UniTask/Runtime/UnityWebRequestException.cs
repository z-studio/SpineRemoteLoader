#if ENABLE_UNITYWEBREQUEST && UNITASK_WEBREQUEST_SUPPORT

using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Cysharp.Threading.Tasks {
    public class UnityWebRequestException : Exception {
        public UnityWebRequest UnityWebRequest { get; }
        public UnityWebRequest.Result Result { get; }
        public string Error { get; }
        public string Text { get; }
        public long ResponseCode { get; }
        public Dictionary<string, string> ResponseHeaders { get; }

        private string m_Msg;

        public UnityWebRequestException(UnityWebRequest unityWebRequest) {
            UnityWebRequest = unityWebRequest;
            Result = unityWebRequest.result;
            Error = unityWebRequest.error;
            ResponseCode = unityWebRequest.responseCode;

            if (UnityWebRequest.downloadHandler != null) {
                if (unityWebRequest.downloadHandler is DownloadHandlerBuffer dhb) {
                    Text = dhb.text;
                }
            }

            ResponseHeaders = unityWebRequest.GetResponseHeaders();
        }

        public override string Message {
            get {
                if (m_Msg == null) {
                    if (!string.IsNullOrWhiteSpace(Text)) {
                        m_Msg = Error + Environment.NewLine + Text;
                    } else {
                        m_Msg = Error;
                    }
                }

                return m_Msg;
            }
        }
    }
}

#endif