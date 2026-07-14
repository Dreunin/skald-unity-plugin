using System;
using System.Linq;
using Skald.Import;
using UnityEngine;
using UnityEngine.UIElements;

namespace Skald.Code
{
    public class Presentation : MonoBehaviour, IDialoguePresenter
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string projectPath;
        [SerializeField] private string conversationTitle;
        private DialogueEngine _dialogueEngine;
        private DialogueEngine.Conversation conversation;


        private VisualElement root;
        private Label speakerNameLabel;
        private Label dialogueLabel;
        private ScrollView choicesScroll;
        private VisualElement choicesContainer;
        private Button continueButton;

        public void Start()
        {
            if (projectPath == null) throw new Exception("Project path is not set.");
            BuildUI();
            _dialogueEngine = new DialogueEngine(this);
            _dialogueEngine.LoadProject(projectPath);
            conversation = _dialogueEngine.StartConversation(conversationTitle.Equals("") ? _dialogueEngine.Project.Conversations.First().Title : conversationTitle);
        }


        public void ShowDialogue(SkaldCharacter character, string dialogue)
        {
            choicesContainer.Clear();
            choicesScroll.style.display = DisplayStyle.None;
            continueButton.style.display = DisplayStyle.Flex;

            if (character == null)
            {
                speakerNameLabel.style.display = DisplayStyle.None;
            }
            else
            {
                speakerNameLabel.style.display = DisplayStyle.Flex;
                speakerNameLabel.text = character.Name;
            }

            dialogueLabel.text = dialogue;
        }

        public void ShowOptions(Option[] options)
        {
            choicesContainer.Clear();
            choicesScroll.style.display = DisplayStyle.Flex;
            continueButton.style.display = DisplayStyle.None;

            for (var i = 0; i < options.Length; i++)
            {
                var index = i;
                var button = new Button(() => conversation.SelectOption(index))
                {
                    text = options[i].Text,
                    name = $"choice-{i + 1}",
                    enabledSelf = options[i].IsAvailable
                };
                button.AddToClassList("choice");
                choicesContainer.Add(button);
            }
        }

        public void EndConversation()
        {
            conversation = null;
            root.Clear();
        }


        private void BuildUI()
        {
            root = uiDocument.rootVisualElement;
            speakerNameLabel = root.Q<Label>("speaker-name");
            dialogueLabel = root.Q<Label>("dialogue-text");
            choicesScroll = root.Q<ScrollView>("choices-scroll");
            choicesContainer = root.Q<VisualElement>("choices-container");
            continueButton = root.Q<Button>("continue-button");

            continueButton.clicked -= OnContinueClicked;
            continueButton.clicked += OnContinueClicked;
        }

        private void OnContinueClicked()
        {
            conversation.Continue();
        }

    }
}
