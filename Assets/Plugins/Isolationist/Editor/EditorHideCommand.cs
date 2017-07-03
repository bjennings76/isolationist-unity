// ---------------------------------------------------------------------
// Copyright (c) 2017 Magic Leap. All Rights Reserved.
// Magic Leap Confidential and Proprietary
// ---------------------------------------------------------------------

using UnityEditor;
using UnityEngine;

namespace Plugins.Isolationist.Editor {
	[InitializeOnLoad]
	public static class EditorHideCommand {
		private const string HIDE_SETTING_PREFIX = "HideToggle";

		private static bool _ranThisFrame;

		private static EditorHotKey HotKey { get; set; }

		static EditorHideCommand() {
			HotKey = new EditorHotKey(HIDE_SETTING_PREFIX, KeyCode.I, defaultShift: true);
			EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
			SceneView.onSceneGUIDelegate += OnSceneGUI;
		}

		private static void OnSceneGUI(SceneView sceneview) { OnGUI(); }

		private static void OnHierarchyWindowItemOnGUI(int instanceid, Rect selectionrect) { OnGUI(); }

		private static void OnGUI() {
			if (_ranThisFrame || !HotKey.Pressed || !Selection.activeGameObject) { return; }

			if (EditorGUIUtility.editingTextField && !string.IsNullOrEmpty(GUI.GetNameOfFocusedControl())) {
				Debug.Log(string.Format("Skipped. In text field. :P Control ID: {0} aka {1}", GUIUtility.GetControlID(FocusType.Keyboard), GUI.GetNameOfFocusedControl()));
				return;
			}

			ToggleHide();
		}

		public static void PreferencesGUI() { HotKey.OnGUI(); }

		[MenuItem("Tools/Toggle Hide", true)]
		public static bool CanToggleHide() {
			return Selection.activeGameObject || IsolateInfo.IsIsolated;
		}

		[MenuItem("Tools/Toggle Hide")]
		public static void ToggleHide() {
			bool activeState = Selection.activeGameObject.activeSelf;
			string undoName = string.Format("{0} {1}", activeState ? "Hide" : "Unhide", Selection.gameObjects.Length > 1 ? Selection.gameObjects.Length + " Objects" : Selection.activeGameObject.name);
			Undo.RecordObjects(Selection.objects, undoName);
			Selection.gameObjects.ForEach(go => go.SetActive(!activeState));

			Debug.Log(undoName);

			_ranThisFrame = true;
			EditorApplication.delayCall += () => _ranThisFrame = false;
		}
	}
}