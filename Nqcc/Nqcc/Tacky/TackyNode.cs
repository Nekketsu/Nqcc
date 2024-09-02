using Nqcc.Tacky.BinaryOperators;
using Nqcc.Tacky.Instructions;
using Nqcc.Tacky.Operands;
using Nqcc.Tacky.UnaryOperators;

namespace Nqcc.Tacky;

public abstract class TackyNode
{
    public void WriteTo(TextWriter writer)
    {
        PrettyPrint(writer);
    }

    private void PrettyPrint(TextWriter writer, int indent = 0)
    {
        switch (this)
        {
            case Program program:
                PrettyPrintProgram(writer, program, indent);
                break;
            case Function function:
                PrettyPrintFunction(writer, function, indent);
                break;
            case Return @return:
                PrettyPrintReturn(writer, @return, indent);
                break;
            case Unary unary:
                PrettyPrintUnary(writer, unary, indent);
                break;
            case Binary binary:
                PrettyPrintBinary(writer, indent, binary);
                break;
            case Constant constant:
                PrettyPrintConstant(writer, constant, indent);
                break;
            case Variable variable:
                PrettyPrintVariable(writer, variable, indent);
                break;
            case UnaryOperator unaryOperator:
                PrettyPrintUnaryOperator(writer, unaryOperator, indent);
                break;
            case BinaryOperator binaryOperator:
                PrettyPrintBinaryOperator(writer, binaryOperator, indent);
                break;
            case Jump jump:
                PrettyPrintJump(writer, jump, indent);
                break;
            case JumpIfZero jumpIfZero:
                PrettyPrintJumpIfZero(writer, jumpIfZero, indent);
                break;
            case JumpIfNotZero jumpIfNotZero:
                PrettyPrintJumpIfNotZero(writer, jumpIfNotZero, indent);
                break;
            case Copy copy:
                PrettyPrintCopy(writer, indent, copy);
                break;
            case Label label:
                PrettyPrintLabel(writer, label, indent);
                break;
        }
    }

    private static void PrettyPrintProgram(TextWriter writer, Program program, int indent)
    {
        program.FunctionDefinition.PrettyPrint(writer, indent);
    }

    private static void PrettyPrintFunction(TextWriter writer, Function function, int indent)
    {
        WriteLine(writer, $"{function.Name}(", indent);
        foreach (var instruction in function.Body)
        {
            instruction.PrettyPrint(writer, indent + 1);
        }
        WriteLine(writer, ")", indent);
    }

    private static void PrettyPrintReturn(TextWriter writer, Return @return, int indent)
    {
        Write(writer, $"Return(", indent);
        @return.Value.PrettyPrint(writer, 0);
        WriteLine(writer, ")", 0);
    }

    private static void PrettyPrintUnary(TextWriter writer, Unary unary, int indent)
    {
        unary.Destination.PrettyPrint(writer, indent);
        writer.Write(" = ");
        unary.Operator.PrettyPrint(writer, 0);
        unary.Source.PrettyPrint(writer, 0);
        writer.WriteLine();
    }

    private static void PrettyPrintBinary(TextWriter writer, int indent, Binary binary)
    {
        binary.Destination.PrettyPrint(writer, indent);
        writer.Write(" = ");
        binary.Left.PrettyPrint(writer, 0);
        writer.Write(" ");
        binary.Operator.PrettyPrint(writer, 0);
        writer.Write(" ");
        binary.Right.PrettyPrint(writer, 0);
        writer.WriteLine();
    }

    private static void PrettyPrintConstant(TextWriter writer, Constant constant, int indent)
    {
        Write(writer, constant.Value.ToString(), indent);
    }

    private static void PrettyPrintVariable(TextWriter writer, Variable variable, int indent)
    {
        Write(writer, variable.Identifier, indent);
    }

    private static void PrettyPrintUnaryOperator(TextWriter writer, UnaryOperator @operator, int indent)
    {
        var operatorText = @operator switch
        {
            Complement => "~",
            Negate => "-",
            Not => "!",
            _ => throw new NotImplementedException()
        };

        Write(writer, operatorText, indent);
    }

    private static void PrettyPrintBinaryOperator(TextWriter writer, BinaryOperator @operator, int indent)
    {
        var operatorText = @operator switch
        {
            Add => "+",
            Subtract => "-",
            Multiply => "*",
            Divide => "/",
            Modulo => "%",
            BitwiseAnd => "&",
            BitwiseOr => "|",
            BitwiseXor => "^",
            LeftShift => "<<",
            RightShift => ">>",
            BinaryOperators.RelationalOperators.Equals => "==",
            BinaryOperators.RelationalOperators.NotEquals => "!=",
            BinaryOperators.RelationalOperators.LessThan => "<",
            BinaryOperators.RelationalOperators.LessOrEquals => "<=",
            BinaryOperators.RelationalOperators.GreaterThan => ">",
            BinaryOperators.RelationalOperators.GreaterOrEquals => ">=",
            _ => throw new NotImplementedException()
        };

        Write(writer, operatorText, indent);
    }

    private static void PrettyPrintJump(TextWriter writer, Jump jump, int indent)
    {
        WriteLine(writer, $"Jump({jump.Target})", indent);
    }

    private static void PrettyPrintJumpIfZero(TextWriter writer, JumpIfZero jumpIfZero, int indent)
    {
        Write(writer, "JumpIfZero(", indent);
        jumpIfZero.Condition.PrettyPrint(writer, 0);
        WriteLine(writer, $", {jumpIfZero.Target})", 0);
    }

    private static void PrettyPrintJumpIfNotZero(TextWriter writer, JumpIfNotZero jumpIfNotZero, int indent)
    {
        Write(writer, "JumpIfNotZero(", indent);
        jumpIfNotZero.Condition.PrettyPrint(writer, 0);
        WriteLine(writer, $", {jumpIfNotZero.Target})", 0);
    }

    private static void PrettyPrintCopy(TextWriter writer, int indent, Copy copy)
    {
        copy.Destination.PrettyPrint(writer, indent);
        Write(writer, " = ", 0);
        copy.Source.PrettyPrint(writer, 0);
        WriteLine(writer, "", 0);
    }

    private static void PrettyPrintLabel(TextWriter writer, Label label, int indent)
    {
        WriteLine(writer, $"{label.Identifier}:", indent);
    }

    private static void WriteLine(TextWriter writer, string text, int indent)
    {
        Write(writer, text, indent);
        writer.WriteLine();
    }

    private static void Write(TextWriter writer, string text, int indent)
    {
        var indentLength = 4;

        writer.Write(new string(' ', indentLength * indent));
        writer.Write(text);
    }

    public override string ToString()
    {
        using var writer = new StringWriter();
        WriteTo(writer);

        return writer.ToString();
    }
}
