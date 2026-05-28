using System;
using Skald.Language;

namespace Skald
{

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
            return variable.Type switch
            {
                TypeName.Boolean => new ExpressionResult(variable.BooleanValue),
                TypeName.Integer => new ExpressionResult(variable.IntegerValue),
                TypeName.Float => new ExpressionResult(variable.FloatValue),
                TypeName.String => new ExpressionResult(variable.StringValue),
                _ => throw new NotImplementedException(),
            };
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
