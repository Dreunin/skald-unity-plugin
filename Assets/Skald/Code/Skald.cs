using System.IO;
using Skald.Import;
using Newtonsoft.Json;

namespace Skald
{
    public class Option
    {
        public string Text { get; set; }
        public bool IsAvailable { get; set; }
    }

    public interface IDialoguePresenter
    {
        public void ShowDialogue(SkaldCharacter character, string dialogue);
        public void ShowOptions(Option[] options);
    }

    public class Skald
    {
        private IDialoguePresenter _dialoguePresenter;
        public EngineImport Project { get; private set; }

        public Skald(IDialoguePresenter dialoguePresenter)
        {
            _dialoguePresenter = dialoguePresenter;
        }

        public void LoadProject(string path)
        {
            Project = JsonConvert.DeserializeObject<EngineImport>(File.ReadAllText(path), new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        public Conversation StartConversation(string name)
        {
            return null;
        }

        public class Conversation
        {
            public void Continue() { }
            public void SelectOption(int index) { }
        }
    }

}
