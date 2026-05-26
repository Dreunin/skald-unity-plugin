using System;
using System.Linq;
using System.Threading.Tasks;
using Skald.Import;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Skald.Code.Editor
{
    public class SkaldUI : EditorWindow
    {
        private const string StyleSheetPath =
            "Assets/Skald/UI/SkaldUI.uss";

        private Texture2D logoTexture;
        private SyncWithSkald syncWithSkald;

        private SkaldProject[] projects = Array.Empty<SkaldProject>();
        private int selectedProjectIndex;
        private SkaldProject selectedProject;

        private Label connectedValueLabel;
        private Button connectButton;
        private Button disconnectButton;
        private Button getProjectsButton;
        private Button importButton;

        private VisualElement projectsContainer;
        private Label selectedProjectLabel;
        private PopupField<string> projectPopup;

        [MenuItem("Tools/Skald/Skald UI")]
        public static void Open()
        {
            GetWindow<SkaldUI>("Skald Project Window");
        }

        private void OnEnable()
        {
            logoTexture = SyncWithSkaldState.LoadLogoTexture();
            syncWithSkald = new SyncWithSkald();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);

            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            rootVisualElement.AddToClassList("skald-root");

            var card = new VisualElement();
            card.AddToClassList("skald-card");
            rootVisualElement.Add(card);

            if (logoTexture != null)
            {
                var logo = new Image
                {
                    image = logoTexture,
                    scaleMode = ScaleMode.ScaleToFit
                };

                logo.AddToClassList("skald-logo");
                card.Add(logo);
            }

            var title = new Label("Skald Sync");
            title.AddToClassList("skald-title");
            card.Add(title);

            var subtitle = new Label(
                "Connect, select a project, and import it into Unity."
            );
            subtitle.AddToClassList("skald-subtitle");
            card.Add(subtitle);

            card.Add(CreateDivider());

            var statusRow = new VisualElement();
            statusRow.AddToClassList("skald-row");

            var connectedLabel = new Label("Connected");
            connectedLabel.AddToClassList("skald-label");

            connectedValueLabel = new Label();

            statusRow.Add(connectedLabel);
            statusRow.Add(connectedValueLabel);
            card.Add(statusRow);

            connectButton = CreateButton("Connect to Skald");
            connectButton.clicked += () => _ = HandleLogin();
            card.Add(connectButton);

            disconnectButton = CreateButton("Disconnect from Skald");
            disconnectButton.clicked += () => _ = HandleLogout();
            card.Add(disconnectButton);

            getProjectsButton = CreateButton("Get Projects");
            getProjectsButton.clicked += () => _ = HandleGetProjects();
            card.Add(getProjectsButton);

            card.Add(CreateDivider());

            var projectTitle = new Label("Project");
            projectTitle.AddToClassList("skald-label");
            card.Add(projectTitle);

            projectsContainer = new VisualElement();
            projectsContainer.AddToClassList("skald-projects");
            card.Add(projectsContainer);

            RefreshUi();
        }

        private Button CreateButton(string text)
        {
            var button = new Button
            {
                text = text
            };

            button.AddToClassList("skald-button");
            return button;
        }

        private VisualElement CreateDivider()
        {
            var divider = new VisualElement();
            divider.AddToClassList("skald-divider");
            return divider;
        }

        private void RefreshUi()
        {
            var isLoggedIn = SyncWithSkaldState.IsLoggedIn;

            if (connectedValueLabel != null)
            {
                connectedValueLabel.text = isLoggedIn ? "Yes" : "No";

                connectedValueLabel.RemoveFromClassList("skald-status-ok");
                connectedValueLabel.RemoveFromClassList("skald-status-error");

                connectedValueLabel.AddToClassList(
                    isLoggedIn ? "skald-status-ok" : "skald-status-error"
                );
            }

            connectButton?.SetEnabled(!isLoggedIn);
            disconnectButton?.SetEnabled(isLoggedIn);
            getProjectsButton?.SetEnabled(isLoggedIn);

            RebuildProjectsUi(isLoggedIn);
        }

        private void RebuildProjectsUi(bool isLoggedIn)
        {
            if (projectsContainer == null)
            {
                return;
            }

            projectsContainer.Clear();

            if (projects == null || projects.Length == 0)
            {
                var message = isLoggedIn
                    ? "No projects found. Click Get Projects to fetch your Skald projects."
                    : "Connect to Skald to view your projects.";

                var helpBox = new HelpBox(message, HelpBoxMessageType.Info);
                projectsContainer.Add(helpBox);
                return;
            }

            selectedProjectIndex = Mathf.Clamp(selectedProjectIndex, 0, projects.Length - 1);

            var projectNames = projects.Select(project => project.Title).ToList();

            projectPopup = new PopupField<string>(
                "Project",
                projectNames,
                selectedProjectIndex
            );

            projectPopup.AddToClassList("skald-popup");
            projectPopup.SetEnabled(isLoggedIn);

            projectPopup.RegisterValueChangedCallback(evt =>
            {
                selectedProjectIndex = projectNames.IndexOf(evt.newValue);
                selectedProjectIndex = Mathf.Clamp(
                    selectedProjectIndex,
                    0,
                    projects.Length - 1
                );

                selectedProject = projects[selectedProjectIndex];
            });

            selectedProject = projects[selectedProjectIndex];

            selectedProjectLabel = new Label($"Selected: {selectedProject.Title}");
            selectedProjectLabel.AddToClassList("skald-selected-project");

            importButton = CreateButton("Import Selected Project");
            importButton.clicked += () => _ = HandleImportSelectedProject();
            importButton.SetEnabled(isLoggedIn && selectedProject != null);

            projectsContainer.Add(projectPopup);
            projectsContainer.Add(selectedProjectLabel);
            projectsContainer.Add(importButton);
        }

        private async Task HandleImportSelectedProject()
        {
            if (selectedProject == null)
            {
                EditorUtility.DisplayDialog("Error", "No project selected.", "OK");
                return;
            }

            SetBusy(true);

            try
            {
                var success = await syncWithSkald.LoadProject(selectedProject.Id);

                AssetDatabase.Refresh();

                if (success)
                {
                    EditorUtility.DisplayDialog(
                        "Success",
                        $"Project {selectedProject.Title} imported successfully.",
                        "OK"
                    );
                }
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Failed to import project: {e.Message}",
                    "OK"
                );
            }
            finally
            {
                SetBusy(false);
                RefreshUi();
            }
        }

        private async Task HandleLogout()
        {
            SetBusy(true);

            try
            {
                await syncWithSkald.Logout();

                projects = Array.Empty<SkaldProject>();
                selectedProject = null;
                selectedProjectIndex = 0;
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Failed to logout: {e.Message}",
                    "OK"
                );
            }
            finally
            {
                SetBusy(false);
                RefreshUi();
            }
        }

        private async Task HandleGetProjects()
        {
            SetBusy(true);

            try
            {
                var fetchedProjects = await syncWithSkald.GetProjects();
                SetProjects(fetchedProjects);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Failed to get projects: {e.Message}",
                    "OK"
                );
            }
            finally
            {
                SetBusy(false);
                RefreshUi();
            }
        }

        private async Task HandleLogin()
        {
            SetBusy(true);

            try
            {
                await syncWithSkald.Login();
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Failed to login: {e.Message}",
                    "OK"
                );
            }
            finally
            {
                SetBusy(false);
                RefreshUi();
            }
        }

        private void SetProjects(SkaldProject[] fetchedProjects)
        {
            projects = fetchedProjects ?? Array.Empty<SkaldProject>();
            selectedProjectIndex = 0;
            selectedProject = projects.Length > 0 ? projects[0] : null;
        }

        private void SetBusy(bool busy)
        {
            var isLoggedIn = SyncWithSkaldState.IsLoggedIn;

            connectButton?.SetEnabled(!busy && !isLoggedIn);
            disconnectButton?.SetEnabled(!busy && isLoggedIn);
            getProjectsButton?.SetEnabled(!busy && isLoggedIn);
            projectPopup?.SetEnabled(!busy && isLoggedIn);
            importButton?.SetEnabled(!busy && isLoggedIn && selectedProject != null);
        }
    }
}
