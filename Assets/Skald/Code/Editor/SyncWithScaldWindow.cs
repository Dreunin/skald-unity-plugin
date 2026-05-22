using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Skald.Code.Editor
{
	public class SyncWithScaldWindow : EditorWindow
	{
		private Texture2D logoTexture;
		SyncWithScald syncWithScald = new SyncWithScald();

		[MenuItem("Tools/Skald/Sync With Scald")]
		public static void Open()
		{
			GetWindow<SyncWithScaldWindow>("Scald Project Window");
		}

		private void OnEnable()
		{
			logoTexture = SyncWithScaldState.LoadLogoTexture();
		}

		private void OnGUI()
		{
			GUILayout.Space(8);

			if (logoTexture != null)
			{
				Rect r = GUILayoutUtility.GetRect(position.width - 20f, 128f, GUILayout.ExpandWidth(true));
				GUI.DrawTexture(r, logoTexture, ScaleMode.ScaleToFit);
				GUILayout.Space(8);
			}

			EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Connected:", SyncWithScaldState.IsLoggedIn ? "Yes" : "No");

			GUILayout.Space(6);

			EditorGUI.BeginDisabledGroup(SyncWithScaldState.IsLoggedIn);
			if (GUILayout.Button("Connect to Scald"))
			{
				HandleLogin();
			}
			EditorGUI.EndDisabledGroup();
			
			EditorGUI.BeginDisabledGroup(!SyncWithScaldState.IsLoggedIn);
			if(GUILayout.Button("Disconnect from Scald"))
			{
				HandleLogout();
			}

			if (GUILayout.Button("Sync"))
			{
				HandleSync();
			}
			EditorGUI.EndDisabledGroup();
		}

		private async Task HandleLogout()
		{
			await syncWithScald.Logout();
			Repaint();
		}

		private async Task HandleSync()
		{
			await syncWithScald.Sync();
			Repaint();
		}

		private async Task HandleLogin()
		{
			await syncWithScald.Login();
			Repaint();
		}
	}
}


