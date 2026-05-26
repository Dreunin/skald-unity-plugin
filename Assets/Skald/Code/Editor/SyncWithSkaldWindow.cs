using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Skald.Import;
using System.Linq;

namespace Skald.Code.Editor
{
    public class SyncWithSkaldWindow : EditorWindow
    {
        private Texture2D logoTexture;
        SyncWithSkald syncWithSkald;
        private Project[] projects = Array.Empty<Project>();
        private int selectedProjectIndex = 0;
        private Project selectedProject;

        [MenuItem("Tools/Skald/Sync With Skald")]
        public static void Open()
        {
            GetWindow<SyncWithSkaldWindow>("Skald Project Window");
        }

        private void OnEnable()
        {
            logoTexture = SyncWithSkaldState.LoadLogoTexture();
            syncWithSkald = new SyncWithSkald();
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
            EditorGUILayout.LabelField("Connected:", SyncWithSkaldState.IsLoggedIn ? "Yes" : "No");

            GUILayout.Space(6);

            EditorGUI.BeginDisabledGroup(SyncWithSkaldState.IsLoggedIn);
            if (GUILayout.Button("Connect to Skald"))
            {
                _ = HandleLogin();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!SyncWithSkaldState.IsLoggedIn);
            if (GUILayout.Button("Disconnect from Skald"))
            {
                _ = HandleLogout();
            }

            if (GUILayout.Button("Get Projects"))
            {
                _ = HandleGetProjects();
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


                if (GUILayout.Button("Import Selected Project"))
                {
                    _ = HandleImportSelectedProject();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No projects found.", MessageType.Info);
            }

            EditorGUI.EndDisabledGroup();
        }

        private async Task HandleImportSelectedProject()
        {
            try
            {
                var success = await syncWithSkald.LoadProject(selectedProject.Id);
                UnityEditor.AssetDatabase.Refresh();
                if (success)
                {
                    EditorUtility.DisplayDialog("Success", $"Project {selectedProject.Title} imported successfully.", "OK");
                }
                Repaint();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to import project: {e.Message}", "OK");
            }
        }

        private async Task HandleLogout()
        {
            try
            {
                await syncWithSkald.Logout();
                Repaint();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to logout: {e.Message}", "OK");
            }
        }

        private async Task HandleGetProjects()
        {
            try
            {
                Project[] fetchedProjects = await syncWithSkald.GetProjects();
                SetProjects(fetchedProjects);
                Repaint();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to get projects: {e.Message}", "OK");
            }
        }

        private async Task HandleLogin()
        {
            try
            {
                await syncWithSkald.Login();
                Repaint();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to login: {e.Message}", "OK");
            }
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
