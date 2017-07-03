using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugins.Isolationist.Editor {
	internal static class Utils {
		internal static void ForEach<T>(this IEnumerable<T> items, Action<T> action) {
			if (items == null) { return; }
			foreach (T obj in items) { action(obj); }
		}

		[ContractAnnotation("source:null => true")]
		internal static bool IsNullOrEmpty(this string source) {
			return string.IsNullOrEmpty(source);
		}

		private static bool IsParent(this Transform parent, Transform transform) {
			while (parent) {
				if (parent == transform) { return true; }
				parent = parent.parent;
			}

			return false;
		}

		private static bool IsParent(this GameObject parent, GameObject go) { return parent && go && IsParent(parent.transform, go.transform); }

		internal static bool IsRelative(this GameObject go1, GameObject go2) { return go2.IsParent(go1) || go1.IsParent(go2); }

		internal static IEnumerable<Transform> GetChildren(this Transform t) {
			List<Transform> children = new List<Transform>();
			for (int i = 0; i < t.childCount; i++) { children.Add(t.GetChild(i)); }
			return children;
		}

		private static IEnumerable<GameObject> GetRootSceneObjects() {
#if UNITY_5_3_OR_NEWER
			List<GameObject> rootObjects = new List<GameObject>();
			for (int i = 0; i < SceneManager.sceneCount; i++) {
				Scene scene = SceneManager.GetSceneAt(i);
				rootObjects.AddRange(scene.GetRootGameObjects());
			}
			return rootObjects;
#else
			var prop = new HierarchyProperty(HierarchyType.GameObjects);
			var expanded = new int[0];
			while (prop.Next(expanded)) yield return prop.pptrValue as GameObject;
#endif
		}

		internal static IEnumerable<Transform> GetRootTransforms() { return GetRootSceneObjects().Where(go => go).Select(go => go.transform); }
	}
}