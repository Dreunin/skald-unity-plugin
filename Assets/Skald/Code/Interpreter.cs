using Skald.Language;
using System;
using System.Collections.Generic;

namespace Skald
{
    public static class Interpreter
    {

        public static string InterpretRichText(RichTextProgram text, Dictionary<string, DialogueEngine.Variable> variables)
        {
            var output = "";
            foreach (var segment in text.Content)
            {
                output += InterpretRichTextSegment(segment, variables);
            }
            return output;
        }

        private static string InterpretRichTextSegment(RichTextSegment segment, Dictionary<string, DialogueEngine.Variable> variables)
        {
            return segment switch
            {
                RichTextContent content => content.Content,
                Tag tag => InterpretTag(tag, variables),
                Template template => InterpretTemplate(template, variables),
                _ => throw new NotImplementedException()
            };
        }

        private static string InterpretTag(Tag tag, Dictionary<string, DialogueEngine.Variable> variables)
        {
            var content = "";
            foreach (var segment in tag.Content)
            {
                content += InterpretRichTextSegment(segment, variables);
            }

            return tag.Name switch
            {
                "b" => $"<b>{content}</b>",
                "i" => $"<i>{content}</i>",
                "u" => $"<u>{content}</u>",
                "s" => $"<s>{content}</s>",
                "color" => $"<color={InterpretTagValue(tag, variables)}>{content}</color>",
                _ => throw new NotImplementedException(),
            };
        }

        private static string InterpretTagValue(Tag tag, Dictionary<string, DialogueEngine.Variable> variables)
        {
            return tag.Value switch
            {
                Template template => $"\"{InterpretTemplate(template, variables)}\"",
                TagIdentifier tagIdentifier => $"\"{variables[tagIdentifier.Content].ToDisplayString()}\"",
                TagColorHex tagColorHex => tagColorHex.Content,
                TagString tagString => $"\"{tagString.Content}\"",
                _ => throw new NotImplementedException(),
            };
        }

        private static string InterpretTemplate(Template template, Dictionary<string, DialogueEngine.Variable> variables)
        {
            var expressionResult = InterpretExpression(template.Content, variables);
            return expressionResult.ToDisplayString();
        }

        public static ExpressionResult InterpretExpression(TypedExpression expression, Dictionary<string, DialogueEngine.Variable> variables)
        {
            switch (expression)
            {
                case TypedBinaryExpression binaryExpression:
                    return InterpretBinaryExpression(binaryExpression, variables);
                case TypedUnaryExpression unaryExpression:
                    return InterpretUnaryExpression(unaryExpression, variables);
                case TypedInteger integerExpression:
                    return new ExpressionResult(integerExpression.Content);
                case TypedFloat floatExpression:
                    return new ExpressionResult(floatExpression.Content);
                case TypedStringLiteral stringExpression:
                    return new ExpressionResult(stringExpression.Content);
                case TypedBooleanLiteral booleanExpression:
                    return new ExpressionResult(booleanExpression.Content);
                case TypedParenthesizedExpression parenthesisExpression:
                    return InterpretExpression(parenthesisExpression.Content, variables);
                case TypedIdentifier identifierExpression:
                    var variable = variables[identifierExpression.Content];
                    return ExpressionResult.FromVariable(variable);
                default:
                    return null;
            }
        }

        public static ExpressionResult InterpretBinaryExpression(TypedBinaryExpression binaryExpression, Dictionary<string, DialogueEngine.Variable> variables)
        {
            var left = InterpretExpression(binaryExpression.Left, variables);
            var right = InterpretExpression(binaryExpression.Right, variables);
            return binaryExpression.Op switch
            {
                BinaryOperator.Add => left.Add(right),
                BinaryOperator.Sub => left.Subtract(right),
                BinaryOperator.Mul => left.Multiply(right),
                BinaryOperator.Div => left.Divide(right),
                BinaryOperator.Mod => left.Modulo(right),
                BinaryOperator.And => left.And(right),
                BinaryOperator.Or => left.Or(right),
                BinaryOperator.Eq => left.Eq(right),
                BinaryOperator.Neq => left.NotEquals(right),
                BinaryOperator.Gt => left.GreaterThan(right),
                BinaryOperator.Lt => left.LessThan(right),
                BinaryOperator.Geq => left.GreaterThanOrEqual(right),
                BinaryOperator.Leq => left.LessThanOrEqual(right),
                _ => throw new NotImplementedException(),
            };
        }

        private static ExpressionResult InterpretUnaryExpression(TypedUnaryExpression unaryExpression, Dictionary<string, DialogueEngine.Variable> variables)
        {
            var value = InterpretExpression(unaryExpression.Expr, variables);
            return unaryExpression.Op switch
            {
                UnaryOperator.Not => value.Not(),
                UnaryOperator.Negate => value.Negate(),
                _ => throw new NotImplementedException(),
            };

        }

        public static void InterpretAssignment(TypedAssignment assignment, Dictionary<string, DialogueEngine.Variable> variables)
        {
            var variable = variables[assignment.Variable.Content];
            var value = InterpretExpression(assignment.Value, variables);
            switch (value.Type)
            {
                case TypeName.String:
                    variable.StringValue = value.StringValue;
                    break;
                case TypeName.Integer:
                    variable.IntegerValue = value.IntegerValue;
                    break;
                case TypeName.Float:
                    variable.FloatValue = value.FloatValue;
                    break;
                case TypeName.Boolean:
                    variable.BooleanValue = value.BooleanValue;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

    }
}
