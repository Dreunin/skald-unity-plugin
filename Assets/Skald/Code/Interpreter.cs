using Skald.Language;
using System;
using System.Collections.Generic;

namespace Skald
{
    public static class Interpreter
    {

    public static string InterpretRichText(RichTextProgram text, Dictionary<string, Skald.Variable> variables)
    {
        return null;
    }

    public static ExpressionResult InterpretExpression(TypedExpression expression, Dictionary<string, Skald.Variable> variables)
    {
        switch (expression) {
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

    public static ExpressionResult InterpretBinaryExpression(TypedBinaryExpression binaryExpression, Dictionary<string, Skald.Variable> variables)
    {
        var left = InterpretExpression(binaryExpression.Left, variables);
        var right = InterpretExpression(binaryExpression.Right, variables);
        switch (binaryExpression.Op) {
            case BinaryOperator.Add:
                return left.Add(right);
            case BinaryOperator.Sub:
                return left.Subtract(right);
            case BinaryOperator.Mul:
                return left.Multiply(right);
            case BinaryOperator.Div:
                return left.Divide(right);
            case BinaryOperator.Mod:
                return left.Modulo(right);
            case BinaryOperator.And:
                return left.And(right);
            case BinaryOperator.Or:
                return left.Or(right);
            case BinaryOperator.Eq:
                return left.Eq(right);
            case BinaryOperator.Neq:
                return left.NotEquals(right);
            case BinaryOperator.Gt:
                return left.GreaterThan(right);
            case BinaryOperator.Lt:
                return left.LessThan(right);
            case BinaryOperator.Geq:
                return left.GreaterThanOrEqual(right);
            case BinaryOperator.Leq:
                return left.LessThanOrEqual(right);
            default:
                throw new NotImplementedException();
        }
    }

    private static ExpressionResult InterpretUnaryExpression(TypedUnaryExpression unaryExpression, Dictionary<string, Skald.Variable> variables)
    {
        var value = InterpretExpression(unaryExpression.Expr, variables);
        switch (unaryExpression.Op) {
            case UnaryOperator.Not:
                return value.Not();
            case UnaryOperator.Negate:
                return value.Negate();
            default:
                throw new NotImplementedException();
        }
    }

    public static void InterpretAssignment(TypedAssignment assignment, Dictionary<string, Skald.Variable> variables)
    {
        var variable = variables[assignment.Variable.Content];
        var value = InterpretExpression(assignment.Value, variables);
        switch (value.Type) {
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

    public record ExpressionResult
    {
        public TypeName Type { get; }

        private readonly string stringValue;
        private readonly bool booleanValue;
        private readonly int integerValue;
        private readonly float floatValue;

        public string StringValue
        {
            get
            {
                if (Type != TypeName.String)
                    throw new Exception("Value is not a string.");
                return stringValue;
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
        }

        public int IntegerValue
        {
            get
            {
                if (Type != TypeName.Integer)
                    throw new Exception("Value is not an integer.");
                return integerValue;
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
        }

        public ExpressionResult(string stringValue)
        {
            Type = TypeName.String;
            this.stringValue = stringValue;
        }

        public ExpressionResult(bool booleanValue)
        {
            Type = TypeName.Boolean;
            this.booleanValue = booleanValue;
        }

        public ExpressionResult(int integerValue)
        {
            Type = TypeName.Integer;
            this.integerValue = integerValue;
        }

        public ExpressionResult(float floatValue)
        {
            Type = TypeName.Float;
            this.floatValue = floatValue;
        }

        public static ExpressionResult FromVariable(Skald.Variable variable)
        {
            switch (variable.Type)
            {
                case TypeName.Boolean:
                    return new ExpressionResult(variable.BooleanValue);
                case TypeName.Integer:
                    return new ExpressionResult(variable.IntegerValue);
                case TypeName.Float:
                    return new ExpressionResult(variable.FloatValue);
                case TypeName.String:
                    return new ExpressionResult(variable.StringValue);
                default:
                    throw new NotImplementedException();
            }
        }

        public ExpressionResult Add(ExpressionResult other)
        {
            if (Type == TypeName.String || other.Type == TypeName.String)
                return new ExpressionResult(ToDisplayString()
                    + other.ToDisplayString());

            if (Type == TypeName.Integer && other.Type == TypeName.Integer)
                return new ExpressionResult(IntegerValue + other.IntegerValue);

            return new ExpressionResult(ToNumber() + other.ToNumber());
        }

        public ExpressionResult Subtract(ExpressionResult other)
        {
            if (Type == TypeName.Integer && other.Type == TypeName.Integer)
                return new ExpressionResult(IntegerValue - other.IntegerValue);

            return new ExpressionResult(ToNumber() - other.ToNumber());
        }

        public ExpressionResult Multiply(ExpressionResult other)
        {
            if (Type == TypeName.Integer && other.Type == TypeName.Integer)
                return new ExpressionResult(IntegerValue * other.IntegerValue);

            return new ExpressionResult(ToNumber() * other.ToNumber());
        }

        public ExpressionResult Divide(ExpressionResult other)
        {
            if (Type == TypeName.Integer && other.Type == TypeName.Integer)
                return new ExpressionResult(IntegerValue / other.IntegerValue);

            return new ExpressionResult(ToNumber() / other.ToNumber());
        }

        public ExpressionResult Modulo(ExpressionResult other)
        {
            if (Type == TypeName.Integer && other.Type == TypeName.Integer)
                return new ExpressionResult(IntegerValue % other.IntegerValue);

            return new ExpressionResult(ToNumber() % other.ToNumber());
        }

        public ExpressionResult GreaterThan(ExpressionResult other)
        {
            return new ExpressionResult(ToNumber() + 1e-15f > other.ToNumber());
        }

        public ExpressionResult LessThan(ExpressionResult other)
        {
            return new ExpressionResult(ToNumber() < other.ToNumber() + 1e-15f);
        }

        public ExpressionResult GreaterThanOrEqual(ExpressionResult other)
        {
            return new ExpressionResult(ToNumber() + 1e-15f >= other.ToNumber());
        }

        public ExpressionResult LessThanOrEqual(ExpressionResult other)
        {
            return new ExpressionResult(ToNumber() <= other.ToNumber() + 1e-15f);
        }

        public ExpressionResult Eq(ExpressionResult other)
        {
            if (IsNumeric() && other.IsNumeric())
                return new ExpressionResult(Approximately(ToNumber(), other.ToNumber()));
            if (Type == TypeName.String && other.Type == TypeName.String)
                return new ExpressionResult(StringValue == other.StringValue);
            if (Type == TypeName.Boolean && other.Type == TypeName.Boolean)
                return new ExpressionResult(BooleanValue == other.BooleanValue);
            throw new Exception($"Cannot compare {Type} with {other.Type}");
        }

        public ExpressionResult NotEquals(ExpressionResult other)
        {
            if (IsNumeric() && other.IsNumeric())
                return new ExpressionResult(!Approximately(ToNumber(), other.ToNumber()));
            if (Type == TypeName.String && other.Type == TypeName.String)
                return new ExpressionResult(StringValue != other.StringValue);
            if (Type == TypeName.Boolean && other.Type == TypeName.Boolean)
                return new ExpressionResult(BooleanValue != other.BooleanValue);
            throw new Exception($"Cannot compare {Type} with {other.Type}");
        }

        public static bool Approximately(float a, float b, float tolerance = 1e-15f)
        {
            return MathF.Abs(a - b) < tolerance;
        }

        public ExpressionResult Or(ExpressionResult other)
        {
            if (Type == TypeName.Boolean && other.Type == TypeName.Boolean)
                return new ExpressionResult(BooleanValue || other.BooleanValue);
            throw new Exception($"Cannot compare {Type} with {other.Type}");
        }

        public ExpressionResult And(ExpressionResult other)
        {
            if (Type == TypeName.Boolean && other.Type == TypeName.Boolean)
                return new ExpressionResult(BooleanValue && other.BooleanValue);
            throw new Exception($"Cannot compare {Type} with {other.Type}");
        }

        public ExpressionResult Negate()
        {
            return Type switch
            {
                TypeName.Integer => new ExpressionResult(-IntegerValue),
                TypeName.Float => new ExpressionResult(-FloatValue),
                _ => throw new InvalidOperationException(
                    $"Cannot negate value of type {Type}.")
            };
        }

        public ExpressionResult Not()
        {
            if (Type != TypeName.Boolean)
                throw new InvalidOperationException(
                    $"Cannot apply logical not to type {Type}.");

            return new ExpressionResult(!BooleanValue);
        }

        private bool IsNumeric()
        {
            return Type == TypeName.Integer || Type == TypeName.Float;
        }

        private float ToNumber()
        {
            return Type switch
            {
                TypeName.Integer => IntegerValue,
                TypeName.Float => FloatValue,
                _ => throw new InvalidOperationException(
                    $"Value of type {Type} is not numeric.")
            };
        }

        private string ToDisplayString()
        {
            return Type switch
            {
                TypeName.String => StringValue,
                TypeName.Boolean => BooleanValue.ToString(),
                TypeName.Integer => IntegerValue.ToString(),
                TypeName.Float => FloatValue.ToString(),
                _ => throw new InvalidOperationException(
                    $"Cannot convert type {Type} to string.")
            };
        }
    }
}
}
