using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Skald.Import;
using System.Linq;

namespace Skald.Code.Editor
{
    public class SyncWithScaldWindow : EditorWindow
    {
        private Texture2D logoTexture;
        SyncWithScald syncWithScald;
        private Project[] projects = Array.Empty<Project>();
        private int selectedProjectIndex = 0;
        private Project selectedProject;

        [MenuItem("Tools/Skald/Sync With Scald")]
        public static void Open()
        {
            GetWindow<SyncWithScaldWindow>("Scald Project Window");
        }

        private void OnEnable()
        {
            logoTexture = SyncWithScaldState.LoadLogoTexture();
            syncWithScald = new SyncWithScald();
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
            if (GUILayout.Button("Disconnect from Scald"))
            {
                HandleLogout();
            }

            if (GUILayout.Button("Sync"))
            {
                HandleSync();
            }

            EditorGUILayout.LabelField("Project", EditorStyles.boldLabel);

            if (projects != null && projects.Length > 0)
            {
                selectedProjectIndex = Mathf.Clamp(
                    selectedProjectIndex,
                    0,
                    projects.Length - 1
                );

                var projectNames = projects.Select(p => p.Title).ToArray();

                selectedProjectIndex = EditorGUILayout.Popup(
                    "Project",
                    selectedProjectIndex,
                    projectNames
                );

                var selectedProjectName = projectNames[selectedProjectIndex];

                EditorGUILayout.LabelField("Selected:", selectedProjectName);
                selectedProject = projects[selectedProjectIndex];
            }
            else
            {
                EditorGUILayout.HelpBox("No projects found.", MessageType.Info);
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
            Project[] fetchedProjects = await syncWithScald.Sync();
            SetProjects(fetchedProjects);
            Repaint();
        }

        private async Task HandleLogin()
        {
            await syncWithScald.Login();
            Repaint();
        }

        private void SetProjects(Project[] fetchedProjects)
        {
            projects = fetchedProjects ?? Array.Empty<Project>();
            selectedProjectIndex = 0;

            selectedProject = projects.Length > 0 ? projects[0] : null;

            Repaint();
        }
    }
}
