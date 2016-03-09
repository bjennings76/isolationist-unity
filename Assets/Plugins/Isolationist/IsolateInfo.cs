using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Plugins.Isolationist
{
	public class IsolateInfo : MonoBehaviour
	{
		private static IsolateInfo _instance;
		public List<GameObject> FocusObjects;
		public List<GameObject> HiddenObjects;

		public static IsolateInfo Instance { get { return _instance ? _instance : (_instance = FindObjectOfType<IsolateInfo>()); } set { _instance = value; } }
		public static bool IsIsolated { get { return Instance; } }

		public static void Hide() { if (Instance && Instance.HiddenObjects != null) Instance.HiddenObjects.Where(go => go).ToList().ForEach(go => go.SetActive(false)); }
		public static void Show() { if (Instance && Instance.HiddenObjects != null) Instance.HiddenObjects.Where(go => go).ToList().ForEach(go => go.SetActive(true)); }

		private void Awake() { Show(); }
	}
}