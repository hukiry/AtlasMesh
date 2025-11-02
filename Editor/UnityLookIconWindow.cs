using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Hukiry
{
    public class ResourcesIcon:System.IEquatable<ResourcesIcon>, System.IComparable<ResourcesIcon> {
		public GUIContent icon;
		public GUIStyle style;
		public string iconName;

		public int CompareTo(ResourcesIcon other)
        {
			return this.iconName.CompareTo(other.iconName);
		}

        public bool Equals(ResourcesIcon other)
        {
			return this.iconName == other.iconName;

		}
    }

	class UnityLookIconWindow : EditorWindow, IHasCustomMenu
	{
		const int CELL = 6;
		[MenuItem("Hukiry/UnityLookIconWindow")]
		public static void ShowWindow()
		{

			var win = EditorWindow.GetWindow<UnityLookIconWindow>("内置图标");
			//win.titleContent = new GUIContent(" 内置图标", Hukiry.HukiryUtilEditor.GetTexture2D("d_UnityLogo"));
		}

		public Vector2 scrollPosition;
		List<ResourcesIcon> m_icons = new List<ResourcesIcon>();
        private string mSearchText,lastText;

        private void OnEnable()
		{
			m_icons = GetUnityIcon();

		}

		void OnGUI()
		{
			if (m_icons.Count == 0)
			{
				m_icons = GetUnityIcon();
			}


			SearchBarPreview(ref mSearchText);

			if (lastText != mSearchText)
			{
				lastText = mSearchText;
				m_icons = GetUnityIcon();
				m_icons = m_icons.Where(p => p.iconName.IndexOf(mSearchText.Trim()) >= 0).ToList();
			}


			GUI.skin.label.fontSize = 9;
			scrollPosition = GUILayout.BeginScrollView(scrollPosition);
			//鼠标放在按钮上的样式
			var items = Enum.GetValues(typeof(MouseCursor)).OfType<MouseCursor>().ToArray();
			for (int i = 0; i < items.Length; i += CELL)
			{
				GUILayout.BeginHorizontal();
				for (int j = 0; j < CELL; j++)
				{
					int index = i + j;
					if (index < items.Length)
					{
						GUILayout.Button(Enum.GetName(typeof(MouseCursor), items[index]));
						EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), items[index]);
					}
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(10);
			}

			//内置图标
			for (int i = 0; i < m_icons.Count; i += CELL)
			{
				GUILayout.BeginHorizontal();
				for (int j = 0; j < CELL; j++)
				{
					int index = i + j;
					if (index < m_icons.Count)
					{
						GUILayout.BeginVertical();
						if (GUILayout.Button(m_icons[index].icon, GUILayout.Width(50), GUILayout.Height(30)))
						{
							EditorGUIUtility.systemCopyBuffer = m_icons[index].iconName;
							this.ShowNotification(m_icons[index].icon);
						}
						GUILayout.Label(m_icons[index].iconName, GUILayout.Width(120));
						GUILayout.EndVertical();
					}
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndScrollView();
		}

		public static void SearchBarPreview(ref string mSearchText)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(84f);

				mSearchText = EditorGUILayout.TextField("", mSearchText, "SearchTextField");
				if (GUILayout.Button("", "SearchCancelButton", GUILayout.Width(18f)))
				{
					mSearchText = string.Empty;
					GUIUtility.keyboardControl = 0;
				}
				GUILayout.Space(84f);
			}
			GUILayout.EndHorizontal();

		}

		/// <summary>
		/// 获取Unity内置所有图标 
		/// </summary>
		/// <returns></returns>
		private List<ResourcesIcon> GetUnityIcon()
		{
			List<ResourcesIcon> m_Icons = new List<ResourcesIcon>();
			Texture2D[] t = Resources.FindObjectsOfTypeAll<Texture2D>();
			foreach (Texture2D x in t)
			{
				//if (x.hideFlags != HideFlags.HideAndDontSave && x.hideFlags != (HideFlags.HideInInspector | HideFlags.HideAndDontSave))
				//	continue;
				if (!EditorUtility.IsPersistent(x))
					continue;
				Debug.unityLogger.logEnabled = false;
				GUIContent gc = EditorGUIUtility.IconContent(x.name);
				Debug.unityLogger.logEnabled = true;

				if (gc != null && gc.image != null)
				{
					m_Icons.Add(new ResourcesIcon { icon = gc, iconName = x.name });
				}
			}

			m_Icons.Sort();
			Resources.UnloadUnusedAssets();
			System.GC.Collect();
			Repaint();
			return m_Icons;
		}

		public void AddItemsToMenu(GenericMenu menu)
		{
			menu.AddItem(new GUIContent("定位脚本"), false, () => {
				//Hukiry.HukiryUtilEditor.LocationObject<MonoScript>("UnityLookIconWindow");
			});
		}
	}
}