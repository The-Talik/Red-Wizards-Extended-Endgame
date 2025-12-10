
using System;
using UnityEngine;

namespace RW
{
	public class SimplePopup : MonoBehaviour
	{
		private static SimplePopup _inst;

		private string _message = "";
		private string _url = null;
		private Rect _win = new Rect(0, 0, Math.Max(500, Screen.width / 3), 220);
		private bool _visible;
		private GUIStyle _label, _btn, _title;
		private bool _stylesBuilt;
		private string _title_text;

		public static void Show(string title, string message, string url = null)//1 = warn, 2=error
		{
			if (_inst == null)
			{
				var go = new GameObject("HEG_SimplePopup");
				UnityEngine.Object.DontDestroyOnLoad(go);
				_inst = go.AddComponent<SimplePopup>();
			}
			_inst._message = string.IsNullOrEmpty(message) ? "Message" : message;
			_inst._url = string.IsNullOrEmpty(url) ? null : url;
			_inst._visible = true;
			_inst.enabled = true;
			_inst._title_text = title;
			_inst.CenterWindow();
			if (GameManager.instance != null)
				GameManager.instance.PauseGame(true);

		}

		public static void Hide()
		{
			if (_inst != null)
			{
				_inst._visible = false;
				_inst.enabled = false;
			}
			if (GameManager.instance != null)
				GameManager.instance.ResumeGame();
		}

		private void Awake()
		{
			CenterWindow();
			enabled = false;
		}

		private void CenterWindow()
		{
			_win.x = (Screen.width - _win.width) * 0.5f;
			_win.y = (Screen.height - _win.height) * 0.5f;
		}

		private void OnGUI()
		{
			GUI.depth = 0;
			if (!_visible) return;

			if (!_stylesBuilt) BuildStyles();

			// dim bg
			var old = GUI.color;
			GUI.color = new Color(0, 0f, 0, .5f);

			GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
			GUI.depth = 1;

			// apply bigger window title
			GUI.skin.window = _title;
			GUI.color = new Color(1f, 1f, 1f, 1f);
			_win = GUILayout.Window(0x5EED123, _win, DoWindow, _inst._title_text);
			GUI.color = old;
		}

		private void DoWindow(int id)
		{
			GUILayout.Space(6);
			GUILayout.Label(_message, _label, GUILayout.ExpandHeight(true));

			GUILayout.FlexibleSpace();
			GUILayout.BeginHorizontal();
			if (!string.IsNullOrEmpty(_url))
			{
				if (GUILayout.Button("Open Page", _btn, GUILayout.Width(180), GUILayout.Height(36)))
				{
					Application.OpenURL(_url);
					Hide();
				}
				GUILayout.FlexibleSpace();
			}
			if (GUILayout.Button("Close", _btn, GUILayout.Width(140), GUILayout.Height(36)))
				Hide();
			GUILayout.EndHorizontal();

			GUI.DragWindow(new Rect(0, 0, 10000, 20));
		}

		private void BuildStyles()
		{
			_stylesBuilt = true;

			// Scale font with screen height (tweak base size as desired)
			float scale = Mathf.Clamp(Screen.height / 1080f, 0.85f, 1.5f);
			int baseSize = Mathf.RoundToInt(16f * scale);

			_label = new GUIStyle(GUI.skin.label)
			{
				wordWrap = true,
				richText = true,
				fontSize = baseSize     // <- bigger message text
			};

			_btn = new GUIStyle(GUI.skin.button)
			{
				fontSize = baseSize      // <- bigger button text
			};

			_title = new GUIStyle(GUI.skin.window)
			{
				fontSize = baseSize + 2  // <- bigger window title
			};
		}
	}
}
