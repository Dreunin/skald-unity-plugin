using System.IO;
using Skald.Import;
using Newtonsoft.Json;
using System.Linq;
using Skald.Language;
using System.Collections.Generic;
using System;

namespace Skald
{
    public record Option(string Text, bool IsAvailable)
    {
        public string Text { get; } = Text;
        public bool IsAvailable { get; } = IsAvailable;
    }

    public interface IDialoguePresenter
    {
        public void ShowDialogue(SkaldCharacter character, string dialogue);
        public void ShowOptions(Option[] options);
        public void EndConversation();
    }

    public class DialogueEngine
    {
        private readonly IDialoguePresenter _dialoguePresenter;
        private MentionContext _mentionContext;
        public EngineImport Project { get; private set; }
        public Dictionary<string, Variable> Variables { get; private set; } = new();

        public DialogueEngine(IDialoguePresenter dialoguePresenter)
        {
            _dialoguePresenter = dialoguePresenter;
        }

        /// <summary>
        /// Loads the project from the given path.
        /// </summary>
        public void LoadProject(string path)
        {
            Project = JsonConvert.DeserializeObject<EngineImport>(File.ReadAllText(path), new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            IMentionable[] mentionables = Project.Characters.ToArray<IMentionable>();
            mentionables = mentionables.Concat(Project.Lore).ToArray();
            _mentionContext = new MentionContext(mentionables);
            ResetVariables();
        }

        public void ResetVariables()
        {
            Variables.Clear();
            foreach (var variable in Project.Variables)
            {
                var newVariable = variable.Type switch
                {
                    TypeName.String => new Variable(variable.DefaultValue),
                    TypeName.Integer => new Variable(int.Parse(variable.DefaultValue)),
                    TypeName.Float => new Variable(float.Parse(variable.DefaultValue)),
                    TypeName.Boolean => new Variable(bool.Parse(variable.DefaultValue)),
                    _ => throw new ArgumentOutOfRangeException()
                };
                Variables.Add(variable.Name, newVariable);
            }
        }

        /// <summary>
        /// Starts a conversation with the given title.
        /// </summary>
        public Conversation StartConversation(string title)
        {
            var skaldConversation = Project.Conversations.First(c => c.Title == title);
            var startingNode = skaldConversation.Data.Nodes.First(n => n is SkaldExportedStartNode { IsDefault: true });

            return new Conversation(skaldConversation, startingNode, ExecuteNode);
        }

        /// <summary>
        /// Executes a node and updates the dialogue presenter accordingly.
        /// </summary>
        /// <list type="bullet">
        /// <item> Proceeds the conversation if the node has a next node. </item>
        /// <item> Waits for user input if the node requires a player choice. </item>
        /// <item> Ends the conversation if it is an end node. </item>
        /// </list>
        private void ExecuteNode(SkaldExportedNode node, Conversation conversation)
        {
            switch (node)
            {
                case SkaldExportedDialogueNode dialogueNode:
                    var character = dialogueNode.CharacterId != null ? Project.Characters.First(c => c.Id == dialogueNode.CharacterId) : null;
                    _dialoguePresenter.ShowDialogue(character, Interpreter.InterpretRichText(dialogueNode.Text, Variables, _mentionContext));
                    break;
                case SkaldExportedPlayerChoiceNode playerChoiceNode:
                    var options = playerChoiceNode.Choices.Select(choice =>
                    {
                        var text = Interpreter.InterpretRichText(choice.Text, Variables, _mentionContext);
                        var isAvailable = choice.Precondition == null || Interpreter.InterpretExpression(choice.Precondition, Variables).BooleanValue;
                        return new Option(text, isAvailable);
                    }).ToArray();
                    _dialoguePresenter.ShowOptions(options);
                    break;
                case SkaldExportedStartNode:
                    conversation.Continue();
                    break;
                case SkaldExportedEndNode:
                    _dialoguePresenter.EndConversation();
                    break;
                case SkaldExportedAssignmentNode assignmentNode:
                    Interpreter.InterpretAssignment(assignmentNode.Expression, Variables);
                    conversation.Continue();
                    break;
                case SkaldExportedConditionalNode conditionalNode:
                    var nextNodeId = conditionalNode.DefaultNextNode;
                    foreach (var condition in conditionalNode.Conditions)
                    {
                        if (!Interpreter.InterpretExpression(condition.Expression, Variables).BooleanValue) continue;
                        nextNodeId = condition.NextNode;
                        break;
                    }
                    conversation.SetCurrentNode(nextNodeId);
                    ExecuteNode(conversation.GetCurrentNode(), conversation);
                    break;
            }
        }

        /// <summary>
        /// Conversations hold the state of a conversation, such as the current node and the ability to continue or select options (player choices).
        /// </summary>
        public class Conversation
        {
            private readonly SkaldConversation _skaldConversation;
            private SkaldExportedNode _currentNode;
            public bool IsActive => _currentNode is SkaldExportedEndNode;

            public delegate void ExecuteNode(SkaldExportedNode node, Conversation conversation);
            private readonly ExecuteNode _executeNode;

            public Conversation(SkaldConversation skaldConversation, SkaldExportedNode startingNode, ExecuteNode executeNode)
            {
                _skaldConversation = skaldConversation;
                _currentNode = startingNode;
                _executeNode = executeNode;
                _executeNode(_currentNode, this); // Execute the startnode
            }

            public bool CanContinue()
            {
                return _currentNode is ISkaldContinuable;
            }

            public void Continue()
            {
                if (_currentNode is not ISkaldContinuable continuable) throw new Exception("Current node is not a continuable node.");
                if (continuable.NextNode == null) throw new Exception("No next node found.");
                SetCurrentNode(continuable.NextNode);
                _executeNode(_currentNode, this);
            }

            public void SelectOption(int index)
            {
                if (_currentNode is not SkaldExportedPlayerChoiceNode playerChoiceNode) throw new Exception("Current node is not a player choice node.");
                SetCurrentNode(playerChoiceNode.Choices[index].NextNode);
                _executeNode(_currentNode, this);
            }

            public void SetCurrentNode(string id)
            {
                _currentNode = _skaldConversation.Data.Nodes.First(n => n.Id == id);
            }
            
            public SkaldExportedNode GetCurrentNode()
            {
                return _currentNode;
            }
        }

        /// <summary>
        /// Skald variables can be of type string, boolean, integer or float.
        /// </summary>
        public class Variable
        {
            public TypeName Type { get; }

            private string stringValue;
            private bool booleanValue;
            private int integerValue;
            private float floatValue;

            public string StringValue
            {
                get
                {
                    if (Type != TypeName.String)
                        throw new Exception("Value is not a string.");
                    return stringValue;
                }
                set
                {
                    if (Type != TypeName.String)
                        throw new Exception("Value is not a string.");
                    stringValue = value;
                }
            }

            public bool BooleanValue
            {
                get
                {
                    if (Type != TypeName.Boolean)
                        throw new Exception("Value is not a boolean.");
                    return booleanValue;
                }
                set
                {
                    if (Type != TypeName.Boolean)
                        throw new Exception("Value is not a boolean.");
                    booleanValue = value;
                }
            }

            public int IntegerValue
            {
                get
                {
                    if (Type != TypeName.Integer)
                        throw new Exception("Value is not an integer.");
                    return integerValue;
                }
                set
                {
                    if (Type != TypeName.Integer)
                        throw new Exception("Value is not an integer.");
                    integerValue = value;
                }
            }

            public float FloatValue
            {
                get
                {
                    if (Type != TypeName.Float)
                        throw new Exception("Value is not a float.");
                    return floatValue;
                }
                set
                {
                    if (Type != TypeName.Float)
                        throw new Exception("Value is not a float.");
                    floatValue = value;
                }
            }

            public Variable(string stringValue)
            {
                Type = TypeName.String;
                this.stringValue = stringValue;
            }

            public Variable(bool booleanValue)
            {
                Type = TypeName.Boolean;
                this.booleanValue = booleanValue;
            }

            public Variable(int integerValue)
            {
                Type = TypeName.Integer;
                this.integerValue = integerValue;
            }

            public Variable(float floatValue)
            {
                Type = TypeName.Float;
                this.floatValue = floatValue;
            }

            public string ToDisplayString()
            {
                return Type switch
                {
                    TypeName.String => stringValue,
                    TypeName.Boolean => booleanValue.ToString(),
                    TypeName.Integer => integerValue.ToString(),
                    TypeName.Float => floatValue.ToString(),
                    _ => throw new Exception("Unknown type."),
                };
            }
        }
    }

}
