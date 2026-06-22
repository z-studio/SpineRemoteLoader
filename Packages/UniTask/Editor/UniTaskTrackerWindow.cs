#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

namespace Cysharp.Threading.Tasks.Editor {
    public class UniTaskTrackerWindow : EditorWindow {
        private static int s_Interval;

        private static UniTaskTrackerWindow s_Window;

        [MenuItem("Window/UniTask Tracker")]
        public static void OpenWindow() {
            if (s_Window != null) {
                s_Window.Close();
            }

            // will called OnEnable(singleton instance will be set).
            GetWindow<UniTaskTrackerWindow>("UniTask Tracker").Show();
        }

        private static readonly GUILayoutOption[] s_EmptyLayoutOption = new GUILayoutOption[0];

        private UniTaskTrackerTreeView m_TreeView;
        private object m_SplitterState;

        private void OnEnable() {
            s_Window = this; // set singleton.
            m_SplitterState = SplitterGUILayout.CreateSplitterState(new float[] { 75f, 25f }, new int[] { 32, 32 }, null);
            m_TreeView = new UniTaskTrackerTreeView();

            TaskTracker.EditorEnableState.EnableAutoReload =
                EditorPrefs.GetBool(TaskTracker.EnableAutoReloadKey, false);

            TaskTracker.EditorEnableState.EnableTracking = EditorPrefs.GetBool(TaskTracker.EnableTrackingKey, false);

            TaskTracker.EditorEnableState.EnableStackTrace =
                EditorPrefs.GetBool(TaskTracker.EnableStackTraceKey, false);
        }

        private void OnGUI() {
            // Head
            RenderHeadPanel();

            // Splittable
            SplitterGUILayout.BeginVerticalSplit(this.m_SplitterState, s_EmptyLayoutOption);

            {
                // Column Tabble
                RenderTable();

                // StackTrace details
                RenderDetailsPanel();
            }

            SplitterGUILayout.EndVerticalSplit();
        }


        #region HeadPanel

        public static bool EnableAutoReload => TaskTracker.EditorEnableState.EnableAutoReload;
        public static bool EnableTracking => TaskTracker.EditorEnableState.EnableTracking;
        public static bool EnableStackTrace => TaskTracker.EditorEnableState.EnableStackTrace;

        private static readonly GUIContent s_EnableAutoReloadHeadContent = EditorGUIUtility.TrTextContent(
            "Enable AutoReload",
            "Reload automatically.",
            (Texture)null
        );

        private static readonly GUIContent s_ReloadHeadContent =
            EditorGUIUtility.TrTextContent("Reload", "Reload View.", (Texture)null);

        private static readonly GUIContent s_GCHeadContent = EditorGUIUtility.TrTextContent(
            "GC.Collect",
            "Invoke GC.Collect.",
            (Texture)null
        );

        private static readonly GUIContent s_EnableTrackingHeadContent = EditorGUIUtility.TrTextContent(
            "Enable Tracking",
            "Start to track async/await UniTask. Performance impact: low",
            (Texture)null
        );

        private static readonly GUIContent s_EnableStackTraceHeadContent = EditorGUIUtility.TrTextContent(
            "Enable StackTrace",
            "Capture StackTrace when task is started. Performance impact: high",
            (Texture)null
        );

        // [Enable Tracking] | [Enable StackTrace]
        private void RenderHeadPanel() {
            EditorGUILayout.BeginVertical(s_EmptyLayoutOption);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, s_EmptyLayoutOption);

            if (GUILayout.Toggle(
                    EnableAutoReload,
                    s_EnableAutoReloadHeadContent,
                    EditorStyles.toolbarButton,
                    s_EmptyLayoutOption
                )
                != EnableAutoReload) {
                TaskTracker.EditorEnableState.EnableAutoReload = !EnableAutoReload;
            }

            if (GUILayout.Toggle(
                    EnableTracking,
                    s_EnableTrackingHeadContent,
                    EditorStyles.toolbarButton,
                    s_EmptyLayoutOption
                )
                != EnableTracking) {
                TaskTracker.EditorEnableState.EnableTracking = !EnableTracking;
            }

            if (GUILayout.Toggle(
                    EnableStackTrace,
                    s_EnableStackTraceHeadContent,
                    EditorStyles.toolbarButton,
                    s_EmptyLayoutOption
                )
                != EnableStackTrace) {
                TaskTracker.EditorEnableState.EnableStackTrace = !EnableStackTrace;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(s_ReloadHeadContent, EditorStyles.toolbarButton, s_EmptyLayoutOption)) {
                TaskTracker.CheckAndResetDirty();
                m_TreeView.ReloadAndSort();
                Repaint();
            }

            if (GUILayout.Button(s_GCHeadContent, EditorStyles.toolbarButton, s_EmptyLayoutOption)) {
                GC.Collect(0);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        #endregion


        #region TableColumn

        private Vector2 m_TableScroll;
        private GUIStyle m_TableListStyle;

        private void RenderTable() {
            if (m_TableListStyle == null) {
                m_TableListStyle = new GUIStyle("CN Box");
                m_TableListStyle.margin.top = 0;
                m_TableListStyle.padding.left = 3;
            }

            EditorGUILayout.BeginVertical(m_TableListStyle, s_EmptyLayoutOption);

            m_TableScroll = EditorGUILayout.BeginScrollView(
                m_TableScroll,
                new GUILayoutOption[] {
                    GUILayout.ExpandWidth(true),
                    GUILayout.MaxWidth(2000f)
                }
            );

            var controlRect = EditorGUILayout.GetControlRect(
                new GUILayoutOption[] {
                    GUILayout.ExpandHeight(true),
                    GUILayout.ExpandWidth(true)
                }
            );

            m_TreeView?.OnGUI(controlRect);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void Update() {
            if (EnableAutoReload) {
                if (s_Interval++ % 120 == 0) {
                    if (TaskTracker.CheckAndResetDirty()) {
                        m_TreeView.ReloadAndSort();
                        Repaint();
                    }
                }
            }
        }

        #endregion


        #region Details

        private static GUIStyle s_DetailsStyle;
        private Vector2 m_DetailsScroll;

        private void RenderDetailsPanel() {
            if (s_DetailsStyle == null) {
                s_DetailsStyle = new GUIStyle("CN Message");
                s_DetailsStyle.wordWrap = false;
                s_DetailsStyle.stretchHeight = true;
                s_DetailsStyle.margin.right = 15;
            }

            string message = "";
            var selected = m_TreeView.state.selectedIDs;

            if (selected.Count > 0) {
                var first = selected[0];
                var item = m_TreeView.CurrentBindingItems.FirstOrDefault(x => x.id == first) as UniTaskTrackerViewItem;

                if (item != null) {
                    message = item.Position;
                }
            }

            m_DetailsScroll = EditorGUILayout.BeginScrollView(this.m_DetailsScroll, s_EmptyLayoutOption);
            var vector = s_DetailsStyle.CalcSize(new GUIContent(message));

            EditorGUILayout.SelectableLabel(
                message,
                s_DetailsStyle,
                new GUILayoutOption[] {
                    GUILayout.ExpandHeight(true),
                    GUILayout.ExpandWidth(true),
                    GUILayout.MinWidth(vector.x),
                    GUILayout.MinHeight(vector.y)
                }
            );

            EditorGUILayout.EndScrollView();
        }

        #endregion
    }
}