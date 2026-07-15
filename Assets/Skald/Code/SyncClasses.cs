using System;
using System.Collections.Generic;
using System.Linq;
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

        [JsonProperty("lore")]
        public SkaldLore[] Lore { get; set; }

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

        [JsonProperty("type")]
        public TypeName Type { get; set; }

        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; }
    }

    public record SkaldConversation
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

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

        [JsonProperty("description")]
        public string Description { get; set; }
    }

    public record SkaldLore
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
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

    public interface ISkaldContinuable
    {
        string NextNode { get; }
    }

    [JsonConverter(typeof(SkaldExportedNodeConverter))]
    public abstract record SkaldExportedNode
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }

    public record SkaldExportedDialogueNode : SkaldExportedNode, ISkaldContinuable
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

    public record SkaldExportedStartNode : SkaldExportedNode, ISkaldContinuable
    {
        [JsonProperty("nextNode")]
        public string NextNode { get; set; }

        [JsonProperty("isDefault")]
        public bool IsDefault { get; set; }
    }

    public record SkaldExportedEndNode : SkaldExportedNode
    {
    }

    public record SkaldExportedAssignmentNode : SkaldExportedNode, ISkaldContinuable
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

    public record SkaldExportedPlayerChoice : ISkaldContinuable
    {
        [JsonProperty("precondition")]
        public TypedExpression Precondition { get; set; }

        [JsonProperty("text")]
        public RichTextProgram Text { get; set; }

        [JsonProperty("nextNode")]
        public string NextNode { get; set; }
    }

    public record SkaldExportedConditionalNode : SkaldExportedNode
    {
        [JsonProperty("conditions")]
        public SkaldExportedCondition[] Conditions { get; set; }
        
        [JsonProperty("defaultNextNode")]
        public string DefaultNextNode { get; set; }
    }

    public record SkaldExportedCondition : ISkaldContinuable
    {
        [JsonProperty("expression")]
        public TypedExpression Expression { get; set; }
        
        [JsonProperty("nextNode")]
        public string NextNode { get; set; }
    }

    public class MentionContext
    {
        private readonly Dictionary<string, SkaldCharacter> _charactersById;
        private readonly Dictionary<string, SkaldLore> _loreById;
        private readonly HashSet<string> _loreTypes;

        public MentionContext(SkaldCharacter[] characters, SkaldLore[] lore)
        {
            _charactersById = (characters ?? Array.Empty<SkaldCharacter>())
                .ToDictionary(character => character.Id);
            _loreById = (lore ?? Array.Empty<SkaldLore>())
                .ToDictionary(loreItem => loreItem.Id);
            _loreTypes = new HashSet<string>(
                (lore ?? Array.Empty<SkaldLore>()).Select(loreItem => loreItem.Type));
        }

        public string Resolve(Mention mention)
        {
            if (mention == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(mention.Id))
            {
                if (IsCharacterMention(mention) &&
                    _charactersById.TryGetValue(mention.Id, out var character))
                {
                    return character.Name;
                }

                if (IsLoreMention(mention) &&
                    _loreById.TryGetValue(mention.Id, out var loreItem))
                {
                    return loreItem.Name;
                }

                if (_charactersById.TryGetValue(mention.Id, out var fallbackCharacter))
                {
                    return fallbackCharacter.Name;
                }

                if (_loreById.TryGetValue(mention.Id, out var fallbackLore))
                {
                    return fallbackLore.Name;
                }
            }

            return mention.Name ?? string.Empty;
        }

        private bool IsCharacterMention(Mention mention) =>
            string.IsNullOrEmpty(mention.MentionableType) ||
            mention.MentionableType == "character";

        private bool IsLoreMention(Mention mention) =>
            mention.MentionableType == "lore" ||
            (!string.IsNullOrEmpty(mention.MentionableType) &&
             _loreTypes.Contains(mention.MentionableType));
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
                "conditional" => new SkaldExportedConditionalNode(),
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
