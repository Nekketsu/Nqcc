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
            case Constant constant:
                PrettyPrintConstant(writer, constant, indent);
                break;
            case Variable variable:
                PrettyPrintVariable(writer, variable, indent);
                break;
            case UnaryOperator unaryOperator:
                PrettyPrintUnaryOperator(writer, unaryOperator, indent);
                break;
        }
    }

    private void PrettyPrintProgram(TextWriter writer, Program program, int indent)
    {
        program.FunctionDefinition.PrettyPrint(writer, indent);
    }

    private void PrettyPrintFunction(TextWriter writer, Function function, int indent)
    {
        WriteLine(writer, $"{function.Name}(", indent);
        foreach (var instruction in function.Body)
        {
            instruction.PrettyPrint(writer, indent + 1);
        }
        WriteLine(writer, ")", indent);
    }

    private void PrettyPrintReturn(TextWriter writer, Return @return, int indent)
    {
        Write(writer, $"Return(", indent);
        @return.Value.PrettyPrint(writer, 0);
        WriteLine(writer, ")", 0);
    }

    private void PrettyPrintUnary(TextWriter writer, Unary unary, int indent)
    {
        unary.Destination.PrettyPrint(writer, indent);
        writer.Write(" = ");
        unary.Operator.PrettyPrint(writer, 0);
        unary.Source.PrettyPrint(writer, 0);
        writer.WriteLine();
    }

    private void PrettyPrintConstant(TextWriter writer, Constant constant, int indent)
    {
        Write(writer, constant.Value.ToString(), indent);
    }

    private void PrettyPrintVariable(TextWriter writer, Variable variable, int indent)
    {
        Write(writer, variable.Identifier, indent);
    }

    private void PrettyPrintUnaryOperator(TextWriter writer, UnaryOperator @operator, int indent)
    {
        var operatorText = @operator switch
        {
            Complement => "~",
            Negate => "-",
            _ => throw new NotImplementedException()
        };

        Write(writer, operatorText, indent);
    }

    private void WriteLine(TextWriter writer, string text, int indent)
    {
        Write(writer, text, indent);
        writer.WriteLine();
    }

    private void Write(TextWriter writer, string text, int indent)
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
