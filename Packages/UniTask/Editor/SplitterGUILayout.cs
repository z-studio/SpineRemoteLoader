#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Cysharp.Threading.Tasks.Editor {
    // reflection call of UnityEditor.SplitterGUILayout
    internal static class SplitterGUILayout {
        private static BindingFlags s_Flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        private static Lazy<Type> s_SplitterStateType = new(() => {
                var type = typeof(EditorWindow).Assembly.GetTypes()
                                               .First(x => x.FullName == "UnityEditor.SplitterState");

                return type;
            }
        );

        private static Lazy<ConstructorInfo> s_SplitterStateCtor = new(() => {
                var type = s_SplitterStateType.Value;

                return type.GetConstructor(
                    s_Flags,
                    null,
                    new Type[] { typeof(float[]), typeof(int[]), typeof(int[]) },
                    null
                );
            }
        );

        private static Lazy<Type> s_SplitterGUILayoutType = new(() => {
                var type = typeof(EditorWindow).Assembly.GetTypes()
                                               .First(x => x.FullName == "UnityEditor.SplitterGUILayout");

                return type;
            }
        );

        private static Lazy<MethodInfo> s_BeginVerticalSplit = new(() => {
                var type = s_SplitterGUILayoutType.Value;

                return type.GetMethod(
                    "BeginVerticalSplit",
                    s_Flags,
                    null,
                    new Type[] { s_SplitterStateType.Value, typeof(GUILayoutOption[]) },
                    null
                );
            }
        );

        private static Lazy<MethodInfo> s_EndVerticalSplit = new(() => {
                var type = s_SplitterGUILayoutType.Value;
                return type.GetMethod("EndVerticalSplit", s_Flags, null, Type.EmptyTypes, null);
            }
        );

        public static object CreateSplitterState(float[] relativeSizes, int[] minSizes, int[] maxSizes) {
            return s_SplitterStateCtor.Value.Invoke(new object[] { relativeSizes, minSizes, maxSizes });
        }

        public static void BeginVerticalSplit(object splitterState, params GUILayoutOption[] options) {
            s_BeginVerticalSplit.Value.Invoke(null, new object[] { splitterState, options });
        }

        public static void EndVerticalSplit() {
            s_EndVerticalSplit.Value.Invoke(null, Type.EmptyTypes);
        }
    }
}