using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Skald.Language;

namespace Skald.Import
{
    public class EngineImport
    {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; }

        [JsonProperty("exportedAt")]
        public string ExportedAt { get; set; }

        [JsonProperty("project")]
        public Project Project { get; set; }

        [JsonProperty("characters")]
        public Character[] Characters { get; set; }

        [JsonProperty("tags")]
        public Tag[] Tags { get; set; }

        [JsonProperty("variables")]
        public Variable[] Variables { get; set; }

        [JsonProperty("conversations")]
        public Conversation[] Conversations { get; set; }
    }

    public class Tag
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public class Variable
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("variableType")]
        public VariableType VariableType { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum VariableType
    {
        [EnumMember(Value = "string")]
        String,

        [EnumMember(Value = "integer")]
        Integer,

        [EnumMember(Value = "float")]
        Float,

        [EnumMember(Value = "boolean")]
        Boolean,
    }

    public class Conversation
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("data")]
        public ConversationData Data { get; set; }
    }

    public class ConversationData
    {
        [JsonProperty("nodes")]
        public ExportedNode[] Nodes { get; set; }
    }

    public class Character
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }
    }

    public class Project
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    [JsonConverter(typeof(ExportedNodeConverter))]
    public abstract class ExportedNode
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public class ExportedDialogueNode : ExportedNode
    {
        [JsonProperty("characterId")]
        public string CharacterId { get; set; }

        [JsonProperty("text")]
        public RichTextProgram Text { get; set; }

        [JsonProperty("tags")]
        public string[] Tags { get; set; }

        [JsonProperty("nextNode")]
        public string NextNode { get; set; }
    }

    public class ExportedStartNode : ExportedNode
    {
        [JsonProperty("nextNode")]
        public string NextNode { get; set; }
    }

    public class ExportedEndNode : ExportedNode
    {
    }

    public class ExportedAssignmentNode : ExportedNode
    {
        [JsonProperty("expression")]
        public TypedAssignment Expression { get; set; }

        [JsonProperty("nextNode")]
        public string NextNode { get; set; }
    }

    public class ExportedPlayerChoiceNode : ExportedNode
    {
        [JsonProperty("choices")]
        public ExportedPlayerChoice[] Choices { get; set; }

        [JsonProperty("tags")]
        public string[] Tags { get; set; }
    }

    public class ExportedPlayerChoice
    {
        [JsonProperty("precondition")]
        public TypedExpression Precondition { get; set; }

        [JsonProperty("text")]
        public RichTextProgram Text { get; set; }

        [JsonProperty("nextNode")]
        public string NextNode { get; set; }
    }

    public class ExportedNodeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ExportedNode).IsAssignableFrom(objectType);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var type = jo["type"]?.Value<string>();
            ExportedNode target = type switch
            {
                "dialogue" => new ExportedDialogueNode(),
                "start" => new ExportedStartNode(),
                "end" => new ExportedEndNode(),
                "assignment" => new ExportedAssignmentNode(),
                "playerChoice" => new ExportedPlayerChoiceNode(),
                _ => throw new JsonSerializationException(
                                        $"Unknown exported node type: {type}"),
            };
            serializer.Populate(jo.CreateReader(), target);
            return target;
        }

        public override void WriteJson(
            JsonWriter writer,
            object value,
            JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
