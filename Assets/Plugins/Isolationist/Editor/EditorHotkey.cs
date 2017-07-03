using UnityEditor;
using UnityEngine;

namespace Plugins.Isolationist.Editor {
	public class EditorHotKey {
		private const string KEY_PREF = "Key";
		private const string ALT_PREF = "Alt";
		private const string CTRL_PREF = "Ctrl";
		private const string SHIFT_PREF = "Shift";

		private static GUIStyle _buttonOff;
		private static GUIStyle _buttonOn;
		private static GUIStyle _popup;
		private string _shortcutDisplay;

		private KeyCode KeyCode { get; set; }
		private bool Alt { get; set; }
		public bool Ctrl { get; private set; }
		public bool Shift { get; private set; }
		private string Name { get; set; }

		public bool Pressed {
			get {
				if (Event.current == null) { return false; }
				if (Event.current.type != EventType.keyUp) { return false; }
				return Event.current.keyCode == KeyCode && Event.current.alt == Alt && Event.current.control == Ctrl && Event.current.shift == Shift;
			}
		}

		private string ShortcutDisplay { get { return _shortcutDisplay.IsNullOrEmpty() ? _shortcutDisplay = GetShortcutDisplay() : _shortcutDisplay; } }

		public EditorHotKey(string name, KeyCode defaultKey, bool defaultCtrl = false, bool defaultAlt = false, bool defaultShift = false) {
			Name = name;
			Ctrl = EditorPrefs.GetBool(Name + CTRL_PREF, defaultCtrl);
			Alt = EditorPrefs.GetBool(Name + ALT_PREF, defaultAlt);
			Shift = EditorPrefs.GetBool(Name + SHIFT_PREF, defaultShift);
			KeyCode = (KeyCode) EditorPrefs.GetInt(Name + KEY_PREF, (int) defaultKey);
		}

		private string GetShortcutDisplay() {
			string display = "";
			if (Ctrl) { display += "Ctrl+"; }
			if (Alt) { display += "Alt+"; }
			if (Shift) { display += "Shift+"; }
			display += KeyCode;
			return display;
		}

		public void OnGUI() {
			if (_buttonOff == null) {
				_buttonOff = new GUIStyle(GUI.skin.GetStyle("Button")) {fixedWidth = 70};
				_buttonOn = new GUIStyle(_buttonOff) {normal = {textColor = Color.white}};
				_buttonOn.normal.background = _buttonOn.active.background;
				_buttonOff.normal.textColor = new Color(0.45f, 0.45f, 0.45f);
				_popup = new GUIStyle(GUI.skin.GetStyle("Popup")) {fixedHeight = 18, normal = {textColor = Color.white}};
			}

			GUILayout.Label(string.Format("{0} Shortcut: {1}", Name, ShortcutDisplay));
			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Ctrl", Ctrl ? _buttonOn : _buttonOff)) { Ctrl = !Ctrl; }
			if (GUILayout.Button("Alt", Alt ? _buttonOn : _buttonOff)) { Alt = !Alt; }
			if (GUILayout.Button("Shift", Shift ? _buttonOn : _buttonOff)) { Shift = !Shift; }

			KeyCode = (KeyCode) EditorGUILayout.EnumPopup(KeyCode, _popup);
			GUILayout.EndHorizontal();
			if (GUI.changed) { UpdateGUI(); }
		}

		private void UpdateGUI() {
			EditorPrefs.SetBool(Name + CTRL_PREF, Ctrl);
			EditorPrefs.SetBool(Name + ALT_PREF, Alt);
			EditorPrefs.SetBool(Name + SHIFT_PREF, Shift);
			EditorPrefs.SetInt(Name + KEY_PREF, (int) KeyCode);
			_shortcutDisplay = GetShortcutDisplay();
		}
	}
}