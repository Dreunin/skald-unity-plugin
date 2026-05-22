using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Presentation : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private string defaultSpeaker = "Character Name";
    private string[] defaultDialogue = new string[] {"This is where you dialogue would be...", "if you had any!", "Assign dialogue to have it show up here."};

    private VisualElement root;
    private Label speakerNameLabel;
    private Label dialogueLabel;
    private ScrollView choicesScroll;
    private VisualElement choicesContainer;
    private Button continueButton;

    public event Action ContinuePressed;
    public event Action<int> ChoiceSelected;

    public void Start()
    {
        BuildUI();
        SetConversation(defaultSpeaker, defaultDialogue[0], Array.Empty<string>());
    }

    private void BuildUI()
    {
        var conversationUxml = uiDocument.visualTreeAsset;
        if( conversationUxml == null)
        {
            Debug.LogError("No UXML found in UIDocument.");
            return;
        }
        
        root = uiDocument.rootVisualElement;

        if (conversationUxml != null)
        {
            root.Clear();
            conversationUxml.CloneTree(root);
        }

        speakerNameLabel = root.Q<Label>("speaker-name");
        dialogueLabel = root.Q<Label>("dialogue-text");
        choicesScroll = root.Q<ScrollView>("choices-scroll");
        choicesContainer = root.Q<VisualElement>("choices-container");
        continueButton = root.Q<Button>("continue-button");

        if (continueButton != null)
        {
            continueButton.clicked -= OnContinueClicked;
            continueButton.clicked += OnContinueClicked;
        }
    }

    public void SetConversation(string speaker, string dialogue, IReadOnlyList<string> choices)
    {
        speakerNameLabel.text = string.IsNullOrWhiteSpace(speaker) ? "" : speaker;

        dialogueLabel.text = dialogue ?? string.Empty;

        var hasChoices = choices != null && choices.Count > 0;

        choicesScroll.style.display = hasChoices ? DisplayStyle.Flex : DisplayStyle.None;

        continueButton.style.display = hasChoices ? DisplayStyle.None : DisplayStyle.Flex;

        if (choicesContainer == null)
        {
            return;
        }

        choicesContainer.Clear();

        if (!hasChoices)
        {
            return;
        }

        for (var i = 0; i < choices.Count; i++)
        {
            var index = i;
            var button = new Button(() => ChoiceSelected?.Invoke(index))
            {
                text = choices[i] ?? string.Empty,
                name = $"choice-{i + 1}"
            };
            button.AddToClassList("choice");
            button.style.width = Length.Percent(100);
            button.style.height = 30;

            choicesContainer.Add(button);
        }
    }

    private void OnContinueClicked()
    {
        ContinuePressed?.Invoke();
    }
}
