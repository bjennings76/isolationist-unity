using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Plugins.Isolationist {
	public class IsolateInfo : MonoBehaviour {
		private static IsolateInfo _instance;
		private static bool _searched;

		public List<GameObject> FocusObjects;
		public List<GameObject> HiddenObjects;

		public static IsolateInfo Instance { get { return _instance ? _instance : (_instance = FindObjectOfType<IsolateInfo>()); } set { _instance = value; } }

		public static bool IsIsolated {
			get {
				if (!_searched) {
					_instance = FindObjectOfType<IsolateInfo>();
					_searched = true;
				}
				return _instance;
			}
		}

		public static void Hide() {
			if (Instance && Instance.HiddenObjects != null) { Instance.HiddenObjects.Where(go => go).ToList().ForEach(HideObject); }
		}

		private static void HideObject(GameObject go) {
			go.SetActive(false);
			go.hideFlags = go.hideFlags | HideFlags.HideInHierarchy;
		}

		public static void Show() {
			if (Instance && Instance.HiddenObjects != null) { Instance.HiddenObjects.Where(go => go).ToList().ForEach(ShowObject); }
		}

		private static void ShowObject(GameObject go) {
			go.SetActive(true);
			go.hideFlags = go.hideFlags & ~HideFlags.HideInHierarchy;
		}

		private void Awake() { Show(); }
	}
}