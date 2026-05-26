using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Skald.Language
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TypeName
    {
        [EnumMember(Value = "integer")]
        Integer,

        [EnumMember(Value = "float")]
        Float,

        [EnumMember(Value = "string")]
        String,

        [EnumMember(Value = "boolean")]
        Boolean
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum BinaryOperator
    {
        [EnumMember(Value = "or")]
        Or,

        [EnumMember(Value = "and")]
        And,

        [EnumMember(Value = "==")]
        Eq,

        [EnumMember(Value = "!=")]
        Neq,

        [EnumMember(Value = ">")]
        Gt,

        [EnumMember(Value = "<")]
        Lt,

        [EnumMember(Value = ">=")]
        Geq,

        [EnumMember(Value = "<=")]
        Leq,

        [EnumMember(Value = "+")]
        Add,

        [EnumMember(Value = "-")]
        Sub,

        [EnumMember(Value = "*")]
        Mul,

        [EnumMember(Value = "/")]
        Div,

        [EnumMember(Value = "%")]
        Mod
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum UnaryOperator
    {
        [EnumMember(Value = "!")]
        Not,

        [EnumMember(Value = "-")]
        Negate
    }

    public abstract record RawNodeBase
    {
        [JsonProperty("from")]
        public int From { get; set; }

        [JsonProperty("to")]
        public int To { get; set; }
    }

    public record RichTextProgram : RawNodeBase
    {
        [JsonProperty("content")]
        public RichTextSegment[] Content { get; set; }
    }

    [JsonConverter(typeof(RichTextSegmentConverter))]
    public abstract record RichTextSegment : RawNodeBase
    {
    }

    public record RichTextContent : RichTextSegment
    {
        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public record Tag : RichTextSegment
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public ITagValue Value { get; set; }

        [JsonProperty("attributes")]
        public TagAttribute[] Attributes { get; set; }

        [JsonProperty("content")]
        public RichTextSegment[] Content { get; set; }

        [JsonProperty("openTagTo")]
        public int OpenTagTo { get; set; }

        [JsonProperty("closeTagFrom")]
        public int CloseTagFrom { get; set; }
    }

    public record Template : RichTextSegment, ITagValue
    {
        [JsonProperty("content")]
        public TypedExpression Content { get; set; }
    }

    public record TagAttribute : RawNodeBase
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public ITagValue Value { get; set; }
    }

    [JsonConverter(typeof(TagValueConverter))]
    public interface ITagValue
    {
    }

    public record TagIdentifier : RawNodeBase, ITagValue
    {
        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public record TagColorHex : RawNodeBase, ITagValue
    {
        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public record TagString : RawNodeBase, ITagValue
    {
        [JsonProperty("content")]
        public string Content { get; set; }
    }

    [JsonConverter(typeof(TypedExpressionConverter))]
    public abstract record TypedExpression : RawNodeBase
    {
        [JsonProperty("resultType")]
        public TypeName ResultType { get; set; }
    }

    public record TypedBinaryExpression : TypedExpression
    {
        [JsonProperty("left")]
        public TypedExpression Left { get; set; }

        [JsonProperty("op")]
        public BinaryOperator Op { get; set; }

        [JsonProperty("right")]
        public TypedExpression Right { get; set; }
    }

    public record TypedUnaryExpression : TypedExpression
    {
        [JsonProperty("op")]
        public UnaryOperator Op { get; set; }

        [JsonProperty("expr")]
        public TypedExpression Expr { get; set; }
    }

    public record TypedAssignment : RawNodeBase
    {
        [JsonProperty("variable")]
        public TypedIdentifier Variable { get; set; }

        [JsonProperty("value")]
        public TypedExpression Value { get; set; }

        [JsonProperty("resultType")]
        public TypeName ResultType { get; set; }
    }

    public record TypedInteger : TypedExpression
    {
        [JsonProperty("content")]
        public int Content { get; set; }
    }

    public record TypedFloat : TypedExpression
    {
        [JsonProperty("content")]
        public double Content { get; set; }
    }

    public record TypedStringLiteral : TypedExpression
    {
        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public record TypedIdentifier : TypedExpression
    {
        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public record TypedBooleanLiteral : TypedExpression
    {
        [JsonProperty("content")]
        public bool Content { get; set; }
    }

    public record TypedParenthesizedExpression : TypedExpression
    {
        [JsonProperty("content")]
        public TypedExpression Content { get; set; }
    }

    public class RichTextSegmentConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(RichTextSegment).IsAssignableFrom(objectType);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var type = jo["type"]?.Value<string>();
            RichTextSegment target = type switch
            {
                "RichTextContent" => new RichTextContent(),
                "Tag" => new Tag(),
                "Template" => new Template(),
                _ => throw new JsonSerializationException(
                                        $"Unknown rich text segment type: {type}"),
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

    public class TypedExpressionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(TypedExpression).IsAssignableFrom(objectType);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var type = jo["type"]?.Value<string>();
            TypedExpression target = type switch
            {
                "BinaryExpression" => new TypedBinaryExpression(),
                "UnaryExpression" => new TypedUnaryExpression(),
                "Integer" => new TypedInteger(),
                "Float" => new TypedFloat(),
                "String" => new TypedStringLiteral(),
                "Identifier" => new TypedIdentifier(),
                "BooleanLiteral" => new TypedBooleanLiteral(),
                "ParenthesizedExpression" => new TypedParenthesizedExpression(),
                _ => throw new JsonSerializationException(
                                        $"Unknown typed expression type: {type}"),
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

    public class TagValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ITagValue).IsAssignableFrom(objectType);
        }

        public override object ReadJson(
            JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var type = jo["type"]?.Value<string>();
            object target = type switch
            {
                "TagIdentifier" => new TagIdentifier(),
                "TagColorHex" => new TagColorHex(),
                "TagString" => new TagString(),
                "Template" => new Template(),
                _ => throw new JsonSerializationException(
                                        $"Unknown tag value type: {type}"),
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
