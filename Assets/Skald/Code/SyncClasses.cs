using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Skald.Language;

namespace Skald.Import
{
    public record EngineImport
    {
        [JsonProperty("schemaVersion")]
        public int SchemaVersion { get; set; }

        [JsonProperty("exportedAt")]
        public string ExportedAt { get; set; }

        [JsonProperty("project")]
        public SkaldProject Project { get; set; }

        [JsonProperty("characters")]
        public SkaldCharacter[] Characters { get; set; }

        [JsonProperty("tags")]
        public SkaldTag[] Tags { get; set; }

        [JsonProperty("variables")]
        public SkaldVariable[] Variables { get; set; }

        [JsonProperty("conversations")]
        public SkaldConversation[] Conversations { get; set; }
    }

    public record SkaldTag
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    public record SkaldVariable
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("variableType")]
        public SkaldVariableType VariableType { get; set; }

        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum SkaldVariableType
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

    public record SkaldConversation
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("data")]
        public SkaldConversationData Data { get; set; }
    }

    public record SkaldConversationData
    {
        [JsonProperty("nodes")]
        public SkaldExportedNode[] Nodes { get; set; }
    }

    public record SkaldCharacter
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }
    }

    public record SkaldProject
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    [JsonConverter(typeof(SkaldExportedNodeConverter))]
    public abstract record SkaldExportedNode
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public record SkaldExportedDialogueNode : SkaldExportedNode
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

    public record SkaldExportedStartNode : SkaldExportedNode
    {
        [JsonProperty("nextNode")]
        public string NextNode { get; set; }
    }

    public record SkaldExportedEndNode : SkaldExportedNode
    {
    }

    public record SkaldExportedAssignmentNode : SkaldExportedNode
    {
        [JsonProperty("expression")]
        public TypedAssignment Expression { get; set; }

        [JsonProperty("nextNode")]
        public string NextNode { get; set; }
    }

    public record SkaldExportedPlayerChoiceNode : SkaldExportedNode
    {
        [JsonProperty("choices")]
        public SkaldExportedPlayerChoice[] Choices { get; set; }

        [JsonProperty("tags")]
        public string[] Tags { get; set; }
    }

    public record SkaldExportedPlayerChoice
    {
        [JsonProperty("precondition")]
        public TypedExpression Precondition { get; set; }

        [JsonProperty("text")]
        public RichTextProgram Text { get; set; }

        [JsonProperty("nextNode")]
        public string NextNode { get; set; }
    }

    public class SkaldExportedNodeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(SkaldExportedNode).IsAssignableFrom(objectType);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var type = jo["type"]?.Value<string>();
            SkaldExportedNode target = type switch
            {
                "dialogue" => new SkaldExportedDialogueNode(),
                "start" => new SkaldExportedStartNode(),
                "end" => new SkaldExportedEndNode(),
                "assignment" => new SkaldExportedAssignmentNode(),
                "playerChoice" => new SkaldExportedPlayerChoiceNode(),
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
