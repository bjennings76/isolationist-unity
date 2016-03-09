using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Plugins.Isolationist.Editor
{
	[InitializeOnLoad]
	public static class EditorIsolateCommand
	{
		private const string ISOLATE_KEY_PREF = "IsolationistKey";
		private const string ISOLATE_ALT_PREF = "IsolationistAlt";
		private const string ISOLATE_CTRL_PREF = "IsolationistCtrl";
		private const string ISOLATE_SHIFT_PREF = "IsolationistShift";
		private static bool _alt;
		private static bool _ctrl;
		private static bool _shift;
		private static bool _ctrlPressedInEditor;
		private static KeyCode _hotkey;
		private static GameObject _lastSelection;
		private static int _lastSelectionCount;
		private static List<GameObject> _lastSelectionList;

		static EditorIsolateCommand()
		{
			_alt = EditorPrefs.GetBool(ISOLATE_ALT_PREF, false);
			_ctrl = EditorPrefs.GetBool(ISOLATE_CTRL_PREF, false);
			_shift = EditorPrefs.GetBool(ISOLATE_SHIFT_PREF, false);
			_hotkey = (KeyCode) EditorPrefs.GetInt(ISOLATE_KEY_PREF, (int) KeyCode.I);
			EditorApplication.update -= Update;
			EditorApplication.update += Update;
			EditorApplication.playmodeStateChanged -= PlaymodeStateChanged;
			EditorApplication.playmodeStateChanged += PlaymodeStateChanged;
			EditorApplication.hierarchyWindowItemOnGUI -= HierarchyWindowItemOnGUI;
			EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
			SceneView.onSceneGUIDelegate -= OnSceneGUI;
			SceneView.onSceneGUIDelegate += OnSceneGUI;
		}

		private static bool IsolateKeyPressed
		{
			get
			{
				if (Event.current == null) return false;
				if (Event.current.type != EventType.keyUp) return false;
				return Event.current.keyCode == _hotkey && Event.current.alt == _alt && Event.current.control == _ctrl &&
				       Event.current.shift == _shift;
			}
		}

		private static void Update()
		{
			if (!IsolateInfo.IsIsolated ||
			    _lastSelection == Selection.activeGameObject && _lastSelectionCount == Selection.gameObjects.Length) return;
			var selectionList = Selection.gameObjects.ToList();
			var newItems = _lastSelectionList == null ? selectionList : selectionList.Except(_lastSelectionList).ToList();
			_lastSelection = Selection.activeGameObject;
			_lastSelectionCount = Selection.gameObjects.Length;
			_lastSelectionList = selectionList;
			SelectionChanged(newItems);
		}

		private static void OnSceneGUI(SceneView sceneView)
		{
			OnGUI();
		}

		private static void HierarchyWindowItemOnGUI(int instanceId, Rect selectionRect)
		{
			if (!string.IsNullOrEmpty(GUI.GetNameOfFocusedControl())) return;
			OnGUI();
		}

		private static void OnGUI()
		{
			_ctrlPressedInEditor = Event.current.control;
			if (!IsolateKeyPressed) return;
			ToggleIsolate();
			Event.current.Use();
		}

		private static void PlaymodeStateChanged()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode) IsolateInfo.Show();
			else IsolateInfo.Hide();
		}

		private static void SelectionChanged(List<GameObject> newItems)
		{
			if (WasHidden(Selection.activeTransform) && !_ctrlPressedInEditor)
			{
				EndIsolation();
				return;
			}

			if (!_ctrlPressedInEditor) return;

			UpdateIsolation(newItems);
		}

		private static List<GameObject> GetAllGameObjectsToHide()
		{
			return IsolateInfo.Instance.FocusObjects.SelectMany<GameObject, GameObject>(GetGameObjectsToHide).Distinct().ToList();
		}

		[MenuItem("Tools/Toggle Isolate", true), UsedImplicitly]
		public static bool CanToggleIsolate()
		{
			return Selection.activeGameObject || IsolateInfo.IsIsolated;
		}

		[MenuItem("Tools/Toggle Isolate"), UsedImplicitly]
		public static void ToggleIsolate()
		{
			if (IsolateInfo.IsIsolated) EndIsolation();
			else StartIsolation();
		}

		private static void StartIsolation()
		{
			if (IsolateInfo.Instance)
			{
				Debug.LogWarning(
					"Isolationist: Found previous isolation info. This shouldn't happen. Ending the previous isolation anyway.");
				EndIsolation();
			}

			if (EditorApplication.isPlayingOrWillChangePlaymode)
				Debug.LogWarning("Isolationist: Can't isolate while playing. It'll break stuff!");

			// Create new IsolateInfo object.
			var container = new GameObject("IsolationInfo") {hideFlags = HideFlags.HideInHierarchy};
			Undo.RegisterCreatedObjectUndo(container, "Isolate");
			IsolateInfo.Instance = container.AddComponent<IsolateInfo>();
			IsolateInfo.Instance.FocusObjects = Selection.gameObjects.ToList();
			IsolateInfo.Instance.HiddenObjects = GetAllGameObjectsToHide();

			if (!IsolateInfo.Instance.HiddenObjects.Any())
			{
				Object.DestroyImmediate(container);
				Debug.LogWarning("Isolationist: Nothing to isolate.");
				return;
			}

			Undo.RecordObjects(IsolateInfo.Instance.HiddenObjects.Cast<Object>().ToArray(), "Isolate");
			IsolateInfo.Hide();
		}

		private static void UpdateIsolation(List<GameObject> newItems)
		{
			if (!newItems.Any()) return;
			Undo.RecordObject(IsolateInfo.Instance, "Isolate");
			Undo.RecordObjects(IsolateInfo.Instance.HiddenObjects.Cast<Object>().ToArray(), "Isolate");
			IsolateInfo.Show();
			IsolateInfo.Instance.FocusObjects = IsolateInfo.Instance.FocusObjects.Concat(newItems).Distinct().ToList();
			var newHiddenObjects = GetAllGameObjectsToHide();
			Undo.RecordObjects(newHiddenObjects.Except(IsolateInfo.Instance.HiddenObjects).Cast<Object>().ToArray(), "Isolate");
			IsolateInfo.Instance.HiddenObjects = newHiddenObjects;
			IsolateInfo.Hide();
		}

		private static bool WasHidden(Transform t)
		{
			return t && !t.GetComponent<IsolateInfo>() && !IsolateInfo.Instance.FocusObjects.Any(t.gameObject.IsRelative);
		}

		private static bool CanUnhide(Transform t)
		{
			return WasHidden(t) || t.GetChildren().Any(CanUnhide);
		}

		private static bool CanHide(Transform t)
		{
			return t && t.gameObject.activeSelf && !t.GetComponent<IsolateInfo>() &&
			       !IsolateInfo.Instance.FocusObjects.Any(t.gameObject.IsRelative);
		}

		private static IEnumerable<GameObject> GetGameObjectsToHide(GameObject keeperGo)
		{
			var keeper = keeperGo.transform;
			var transformsToHide = new List<Transform>();

			while (keeper.parent)
			{
				transformsToHide.AddRange(keeper.parent.GetChildren().Where(CanHide));
				keeper = keeper.parent;
			}

			transformsToHide.AddRange(GetRootTransforms().Where(CanHide));
			return transformsToHide.Select(t => t.gameObject);
		}

		[PreferenceItem("Isolationist"), UsedImplicitly]
		public static void PreferencesGUI()
		{
			_ctrl = EditorGUILayout.Toggle("Ctrl", _ctrl);
			_alt = EditorGUILayout.Toggle("Alt", _alt);
			_shift = EditorGUILayout.Toggle("Shift", _shift);
			_hotkey = (KeyCode) EditorGUILayout.EnumPopup("Shortcut Key", _hotkey);

			if (!GUI.changed) return;

			EditorPrefs.SetBool(ISOLATE_CTRL_PREF, _ctrl);
			EditorPrefs.SetBool(ISOLATE_ALT_PREF, _alt);
			EditorPrefs.SetBool(ISOLATE_SHIFT_PREF, _shift);
			EditorPrefs.SetInt(ISOLATE_KEY_PREF, (int) _hotkey);
		}

		private static void EndIsolation()
		{
			if (!IsolateInfo.Instance) return;

			if (IsolateInfo.Instance.HiddenObjects != null)
			{
				Undo.RecordObjects(IsolateInfo.Instance.HiddenObjects.Cast<Object>().ToArray(), "DeIsolate");
				IsolateInfo.Show();
			}

			Undo.DestroyObjectImmediate(IsolateInfo.Instance.gameObject);
		}

		#region Utils

		private static bool IsParent(this Transform parent, Transform transform)
		{
			while (parent)
			{
				if (parent == transform) return true;
				parent = parent.parent;
			}

			return false;
		}

		private static bool IsParent(this GameObject parent, GameObject go)
		{
			return parent && go && IsParent(parent.transform, go.transform);
		}

		private static bool IsRelative(this GameObject go1, GameObject go2)
		{
			return go2.IsParent(go1) || go1.IsParent(go2);
		}

		private static IEnumerable<Transform> GetChildren(this Transform t)
		{
			var children = new List<Transform>();
			for (var i = 0; i < t.childCount; i++) children.Add(t.GetChild(i));
			return children;
		}

		private static IEnumerable<GameObject> GetRootSceneObjects()
		{
			var prop = new HierarchyProperty(HierarchyType.GameObjects);
			var expanded = new int[0];
			while (prop.Next(expanded)) yield return prop.pptrValue as GameObject;
		}

		private static IEnumerable<Transform> GetRootTransforms()
		{
			return GetRootSceneObjects().Select(go => go.transform);
		}

		#endregion
	}
}