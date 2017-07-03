using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_5_3_OR_NEWER
#endif

namespace Plugins.Isolationist.Editor {
	[InitializeOnLoad]
	public static class EditorIsolateCommand {
		private const string ISOLATE_HIDE_LIGHTS_PREF = "IsolationistHideLights";
		private const string ISOLATE_HIDE_CAMERAS_PREF = "IsolationistHideCameras";

		private static bool _showLights;
		private static bool _showCameras;
		private static GameObject _lastSelection;
		private static int _lastSelectionCount;
		private static List<GameObject> _lastSelectionList;
		private static readonly List<Type> _lightTypeList = new List<Type>();
		private static readonly List<Type> _cameraTypeList = new List<Type>();

		private static bool CtrlOrShiftPressed { get { return HotKey.Ctrl || HotKey.Shift; } }

		private static EditorHotKey HotKey { get; set; }

		static EditorIsolateCommand() {
			HotKey = new EditorHotKey("Isolate", KeyCode.I);
			_showLights = !EditorPrefs.GetBool(ISOLATE_HIDE_LIGHTS_PREF, true);
			_showCameras = !EditorPrefs.GetBool(ISOLATE_HIDE_CAMERAS_PREF, true);
			EditorApplication.update += Update;
			EditorApplication.playmodeStateChanged += PlaymodeStateChanged;
			EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
			SceneView.onSceneGUIDelegate += OnSceneGUI;

			AddCameraType(typeof(Camera));
			AddLightType(typeof(Light));
			AddLightType(typeof(ReflectionProbe));
		}

		public static void AddLightType(Type type) { _lightTypeList.Add(type); }

		public static void AddCameraType(Type type) { _cameraTypeList.Add(type); }

		private static void Update() {
			if (!IsolateInfo.IsIsolated || _lastSelection == Selection.activeGameObject && _lastSelectionCount == Selection.gameObjects.Length) { return; }
			List<GameObject> selectionList = Selection.gameObjects.ToList();
			List<GameObject> newItems = _lastSelectionList == null ? selectionList : selectionList.Except(_lastSelectionList).ToList();
			_lastSelection = Selection.activeGameObject;
			_lastSelectionCount = Selection.gameObjects.Length;
			_lastSelectionList = selectionList;
			SelectionChanged(newItems);
		}

		private static void OnSceneGUI(SceneView sceneView) { OnGUI(); }

		private static void HierarchyWindowItemOnGUI(int instanceId, Rect selectionRect) {
			if (!GUI.GetNameOfFocusedControl().IsNullOrEmpty()) { return; }
			OnGUI();
		}

		private static void OnGUI() {
			if (!HotKey.Pressed) { return; }
			ToggleIsolate();
			Event.current.Use();
		}

		private static void PlaymodeStateChanged() {
			if (EditorApplication.isPlayingOrWillChangePlaymode) { IsolateInfo.Show(); }
			else { IsolateInfo.Hide(); }
		}

		private static void SelectionChanged(List<GameObject> newItems) {
			if (WasHidden(Selection.activeTransform) && !CtrlOrShiftPressed) {
				EndIsolation();
				return;
			}

			if (!CtrlOrShiftPressed) { return; }

			UpdateIsolation(newItems);
		}

		private static List<GameObject> GetAllGameObjectsToHide() { return IsolateInfo.Instance.FocusObjects.SelectMany(GetGameObjectsToHide).Distinct().ToList(); }

		[MenuItem("Tools/Toggle Isolate", true)]
		public static bool CanToggleIsolate() {
			return Selection.activeGameObject || IsolateInfo.IsIsolated;
		}

		[MenuItem("Tools/Toggle Isolate")]
		public static void ToggleIsolate() {
			if (IsolateInfo.IsIsolated) { EndIsolation(); }
			else { StartIsolation(); }
		}

		private static void StartIsolation() {
			if (IsolateInfo.Instance) {
				Debug.LogWarning("Isolationist: Found previous isolation info. This shouldn't happen. Ending the previous isolation anyway.");
				EndIsolation();
			}

			if (EditorApplication.isPlayingOrWillChangePlaymode) {
				Debug.LogWarning("Isolationist: Can't isolate while playing. It'll break stuff!");
				return;
			}

			// Create new IsolateInfo object.
			GameObject container = new GameObject("IsolationInfo") {hideFlags = HideFlags.HideInHierarchy};
			Undo.RegisterCreatedObjectUndo(container, "Isolate");
			IsolateInfo.Instance = container.AddComponent<IsolateInfo>();
			List<GameObject> focusList = IsolateInfo.Instance.FocusObjects = Selection.gameObjects.ToList();

			if (_showLights) { _lightTypeList.ForEach(t => focusList.AddRange(Object.FindObjectsOfType(t).Select(ObjectToGO))); }

			if (_showCameras) { _cameraTypeList.ForEach(t => focusList.AddRange(Object.FindObjectsOfType(t).Select(ObjectToGO))); }

			IsolateInfo.Instance.HiddenObjects = GetAllGameObjectsToHide();

			if (!IsolateInfo.Instance.HiddenObjects.Any()) {
				Object.DestroyImmediate(container);
				Debug.LogWarning("Isolationist: Nothing to isolate.");
				return;
			}

			Undo.RecordObjects(IsolateInfo.Instance.HiddenObjects.Cast<Object>().ToArray(), "Isolate");
			IsolateInfo.Hide();
		}

		private static GameObject ObjectToGO(Object obj) {
			Component component = obj as Component;
			return component ? component.gameObject : null;
		}

		private static void UpdateIsolation(List<GameObject> newItems) {
			if (!newItems.Any()) { return; }
			Undo.RecordObject(IsolateInfo.Instance, "Isolate");
			Undo.RecordObjects(IsolateInfo.Instance.HiddenObjects.Cast<Object>().ToArray(), "Isolate");
			IsolateInfo.Show();
			IsolateInfo.Instance.FocusObjects = IsolateInfo.Instance.FocusObjects.Concat(newItems).Distinct().ToList();
			List<GameObject> newHiddenObjects = GetAllGameObjectsToHide();
			Undo.RecordObjects(newHiddenObjects.Except(IsolateInfo.Instance.HiddenObjects).Cast<Object>().ToArray(), "Isolate");
			IsolateInfo.Instance.HiddenObjects = newHiddenObjects;
			IsolateInfo.Hide();
		}

		private static bool WasHidden(Transform t) { return t && !t.gameObject.activeInHierarchy && !t.GetComponent<IsolateInfo>() && !IsolateInfo.Instance.FocusObjects.Any(t.gameObject.IsRelative); }

		private static bool CanHide(Transform t) { return t && t.gameObject.activeSelf && !t.GetComponent<IsolateInfo>() && !IsolateInfo.Instance.FocusObjects.Any(t.gameObject.IsRelative); }

		private static IEnumerable<GameObject> GetGameObjectsToHide(GameObject keeperGo) {
			if (!keeperGo) { return new List<GameObject>(); }

			Transform keeper = keeperGo.transform;
			List<Transform> transformsToHide = new List<Transform>();

			while (keeper.parent) {
				transformsToHide.AddRange(keeper.parent.GetChildren().Where(CanHide));
				keeper = keeper.parent;
			}

			transformsToHide.AddRange(Utils.GetRootTransforms().Where(CanHide));
			return transformsToHide.Select(t => t.gameObject);
		}

		[PreferenceItem("Isolationist")]
		private static void PreferencesGUI() {
			EditorHideCommand.PreferencesGUI();

			GUILayout.Label("");

			HotKey.OnGUI();

			GUILayout.Label("");

			_showLights = EditorGUILayout.Toggle("Skip Lights on Isolate", _showLights);
			_showCameras = EditorGUILayout.Toggle("Skip Cameras on Isolate", _showCameras);

			if (GUI.changed) {
				EditorPrefs.SetBool(ISOLATE_HIDE_LIGHTS_PREF, !_showLights);
				EditorPrefs.SetBool(ISOLATE_HIDE_CAMERAS_PREF, !_showCameras);
			}
		}

		private static void EndIsolation() {
			if (!IsolateInfo.Instance) { return; }

			if (IsolateInfo.Instance.HiddenObjects != null) {
				Undo.RecordObjects(IsolateInfo.Instance.HiddenObjects.Cast<Object>().ToArray(), "DeIsolate");
				IsolateInfo.Show();
			}

			Undo.DestroyObjectImmediate(IsolateInfo.Instance.gameObject);
		}
	}
}