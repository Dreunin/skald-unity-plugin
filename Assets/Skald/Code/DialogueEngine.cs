using System.IO;
using Skald.Import;
using Newtonsoft.Json;
using System.Linq;
using Skald.Language;
using System.Collections.Generic;
using System;


namespace Skald
{
    public record Option
    {
        public string Text { get; }
        public bool IsAvailable { get; }

        public Option(string text, bool isAvailable)
        {
            Text = text;
            IsAvailable = isAvailable;
        }
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
        public EngineImport Project { get; private set; }
        public Dictionary<string, Variable> Variables { get; private set; } = new();

        public DialogueEngine(IDialoguePresenter dialoguePresenter)
        {
            _dialoguePresenter = dialoguePresenter;
        }

        public void LoadProject(string path)
        {
            Project = JsonConvert.DeserializeObject<EngineImport>(File.ReadAllText(path), new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            ResetVariables();
        }

        public void ResetVariables()
        {
            Variables.Clear();
            foreach (var variable in Project.Variables)
            {
                switch (variable.VariableType)
                {
                    case TypeName.String:
                        Variables.Add(variable.Name, new Variable(variable.DefaultValue));
                        break;
                    case TypeName.Integer:
                        Variables.Add(variable.Name, new Variable(int.Parse(variable.DefaultValue)));
                        break;
                    case TypeName.Float:
                        Variables.Add(variable.Name, new Variable(float.Parse(variable.DefaultValue)));
                        break;
                    case TypeName.Boolean:
                        Variables.Add(variable.Name, new Variable(bool.Parse(variable.DefaultValue)));
                        break;
                }
            }
        }

        /// <summary>
        /// Starts a conversation with the given title, optionally starting from a specific node.
        /// The starting node can technically be any node, but if not provided, the first start node will be used.
        /// </summary>
        public Conversation StartConversation(string title, SkaldExportedNode startingNode = null)
        {
            var skaldConversation = Project.Conversations.First(c => c.Title == title);
            if (skaldConversation == null) throw new Exception($"Conversation with title '{title}' not found.");

            startingNode ??= skaldConversation.Data.Nodes.First(n => n is SkaldExportedStartNode);
            if (startingNode == null) throw new Exception($"No start node found for conversation '{title}'.");

            return new Conversation(skaldConversation, startingNode, ExecuteNode);
        }

        private void ExecuteNode(SkaldExportedNode node, Conversation conversation)
        {
            switch (node)
            {
                case SkaldExportedDialogueNode dialogueNode:
                    var character = dialogueNode.CharacterId != null ? Project.Characters.First(c => c.Id == dialogueNode.CharacterId) : null;
                    _dialoguePresenter.ShowDialogue(character, Interpreter.InterpretRichText(dialogueNode.Text, Variables));
                    break;
                case SkaldExportedStartNode:
                    conversation.Continue();
                    break;
                case SkaldExportedEndNode:
                    _dialoguePresenter.EndConversation();
                    break;
                case SkaldExportedPlayerChoiceNode playerChoiceNode:
                    var options = playerChoiceNode.Choices.Select(choice =>
                    {
                        var text = Interpreter.InterpretRichText(choice.Text, Variables);
                        var isAvailable = choice.Precondition == null || Interpreter.InterpretExpression(choice.Precondition, Variables).BooleanValue;
                        return new Option(text, isAvailable);
                    }).ToArray();
                    _dialoguePresenter.ShowOptions(options);
                    break;
                case SkaldExportedAssignmentNode assignmentNode:
                    Interpreter.InterpretAssignment(assignmentNode.Expression, Variables);
                    conversation.Continue();
                    break;
            }
        }

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
                if (_currentNode is not ISkaldContinuable continuable) throw new System.Exception("Current node is not a continuable node.");
                if (continuable.NextNode == null) throw new System.Exception("No next node found.");
                _currentNode = _skaldConversation.Data.Nodes.First(n => n.Id == continuable.NextNode);
                _executeNode(_currentNode, this);
            }

            public void SelectOption(int index)
            {
                if (_currentNode is not SkaldExportedPlayerChoiceNode playerChoiceNode) throw new System.Exception("Current node is not a player choice node.");
                _currentNode = _skaldConversation.Data.Nodes.First(n => n.Id == playerChoiceNode.Choices[index].NextNode);
                _executeNode(_currentNode, this);
            }
        }

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
