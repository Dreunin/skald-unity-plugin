using Skald;
using Skald.Import;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;

public class Presentation : MonoBehaviour, IDialoguePresenter
{
    [ContextMenu("Load Project")]
    public void TestLoad()
    {
        skald = new Skald.Skald(this);
        skald.LoadProject("Assets/Resources/Skald/jfb8zhi1ib95iabsaxb6q0aj.json");
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

    private Skald.Skald skald;
    private Skald.Skald.Conversation conversation;

    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private Label speakerNameLabel;
    private Label dialogueLabel;
    private ScrollView choicesScroll;
    private VisualElement choicesContainer;
    private Button continueButton;

    public void Start()
    {
        BuildUI();
        TestLoad();
        conversation = skald.StartConversation(skald.Project.Conversations.First().Title);
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
