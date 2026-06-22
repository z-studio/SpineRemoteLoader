#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks.Internal;

namespace Cysharp.Threading.Tasks {
    // public for add user custom.

    public static class TaskTracker {
#if UNITY_EDITOR

        private static int s_TrackingId = 0;

        public const string EnableAutoReloadKey = "UniTaskTrackerWindow_EnableAutoReloadKey";
        public const string EnableTrackingKey = "UniTaskTrackerWindow_EnableTrackingKey";
        public const string EnableStackTraceKey = "UniTaskTrackerWindow_EnableStackTraceKey";

        public static class EditorEnableState {
            private static bool s_EnableAutoReload;

            public static bool EnableAutoReload {
                get => s_EnableAutoReload;
                set {
                    s_EnableAutoReload = value;
                    UnityEditor.EditorPrefs.SetBool(EnableAutoReloadKey, value);
                }
            }

            private static bool s_EnableTracking;

            public static bool EnableTracking {
                get => s_EnableTracking;
                set {
                    s_EnableTracking = value;
                    UnityEditor.EditorPrefs.SetBool(EnableTrackingKey, value);
                }
            }

            private static bool s_EnableStackTrace;

            public static bool EnableStackTrace {
                get => s_EnableStackTrace;
                set {
                    s_EnableStackTrace = value;
                    UnityEditor.EditorPrefs.SetBool(EnableStackTraceKey, value);
                }
            }
        }

#endif

        private static
            List<KeyValuePair<IUniTaskSource, (string formattedType, int trackingId, DateTime addTime, string stackTrace
                )>> s_ListPool = new();

        private static readonly
            WeakDictionary<IUniTaskSource, (string formattedType, int trackingId, DateTime addTime, string stackTrace)>
            s_Tracking = new();

        [Conditional("UNITY_EDITOR")]
        public static void TrackActiveTask(IUniTaskSource task, int skipFrame) {
#if UNITY_EDITOR
            s_Dirty = true;

            if (!EditorEnableState.EnableTracking) {
                return;
            }

            var stackTrace = EditorEnableState.EnableStackTrace
                ? new StackTrace(skipFrame, true).CleanupAsyncStackTrace() : "";

            string typeName;

            if (EditorEnableState.EnableStackTrace) {
                var sb = new StringBuilder();
                TypeBeautify(task.GetType(), sb);
                typeName = sb.ToString();
            } else {
                typeName = task.GetType().Name;
            }

            s_Tracking.TryAdd(task, (typeName, Interlocked.Increment(ref s_TrackingId), DateTime.UtcNow, stackTrace));
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void RemoveTracking(IUniTaskSource task) {
#if UNITY_EDITOR
            s_Dirty = true;

            if (!EditorEnableState.EnableTracking) {
                return;
            }

            var success = s_Tracking.TryRemove(task);
#endif
        }

        private static bool s_Dirty;

        public static bool CheckAndResetDirty() {
            var current = s_Dirty;
            s_Dirty = false;
            return current;
        }

        /// <summary>(trackingId, awaiterType, awaiterStatus, createdTime, stackTrace)</summary>
        public static void ForEachActiveTask(Action<int, string, UniTaskStatus, DateTime, string> action) {
            lock (s_ListPool) {
                var count = s_Tracking.ToList(ref s_ListPool, clear: false);

                try {
                    for (int i = 0; i < count; i++) {
                        action(
                            s_ListPool[i].Value.trackingId,
                            s_ListPool[i].Value.formattedType,
                            s_ListPool[i].Key.UnsafeGetStatus(),
                            s_ListPool[i].Value.addTime,
                            s_ListPool[i].Value.stackTrace
                        );

                        s_ListPool[i] = default;
                    }
                } catch {
                    s_ListPool.Clear();
                    throw;
                }
            }
        }

        private static void TypeBeautify(Type type, StringBuilder sb) {
            if (type.IsNested) {
                // TypeBeautify(type.DeclaringType, sb);
                sb.Append(type.DeclaringType.Name.ToString());
                sb.Append(".");
            }

            if (type.IsGenericType) {
                var genericsStart = type.Name.IndexOf("`");

                if (genericsStart != -1) {
                    sb.Append(type.Name.Substring(0, genericsStart));
                } else {
                    sb.Append(type.Name);
                }

                sb.Append("<");
                var first = true;

                foreach (var item in type.GetGenericArguments()) {
                    if (!first) {
                        sb.Append(", ");
                    }

                    first = false;
                    TypeBeautify(item, sb);
                }

                sb.Append(">");
            } else {
                sb.Append(type.Name);
            }
        }

        //static string RemoveUniTaskNamespace(string str)
        //{
        //    return str.Replace("Cysharp.Threading.Tasks.CompilerServices", "")
        //        .Replace("Cysharp.Threading.Tasks.Linq", "")
        //        .Replace("Cysharp.Threading.Tasks", "");
        //}
    }
}