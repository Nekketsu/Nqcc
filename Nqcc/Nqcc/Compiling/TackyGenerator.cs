using Nqcc.Tacky;
using Nqcc.Tacky.Instructions;
using Nqcc.Tacky.Operands;
using Nqcc.Tacky.UnaryOperators;
using System.Collections.Immutable;

namespace Nqcc.Compiling;

public class TackyGenerator(Ast.Program ast)
{
    private int temporaryCounter = 0;

    public Program Generate() => EmitProgram(ast);

    private Program EmitProgram(Ast.Program program)
    {
        var functionDefinition = EmitFunction(program.FunctionDefinition);

        return new Program(functionDefinition);
    }

    private Function EmitFunction(Ast.Function function)
    {
        var body = EmitStatement(function.Body);

        return new Function(function.Name, body);
    }

    private ImmutableArray<Instruction> EmitStatement(Ast.Statement statement) => statement switch
    {
        Ast.Statements.Return @return => EmitReturn(@return),
        _ => throw new NotImplementedException()
    };

    private ImmutableArray<Instruction> EmitReturn(Ast.Statements.Return @return)
    {
        var builder = ImmutableArray.CreateBuilder<Instruction>();
        var value = EmitExpression(builder, @return.Expression);

        builder.Add(new Return(value));

        return builder.ToImmutable();
    }

    private Operand EmitExpression(ImmutableArray<Instruction>.Builder builder, Ast.Expression expression) => expression switch
    {
        Ast.Expressions.Constant constant => EmitConstant(constant),
        Ast.Expressions.Unary unary => EmitUnary(builder, unary),
        _ => throw new NotImplementedException()
    };

    private static Constant EmitConstant(Ast.Expressions.Constant constant)
    {
        return new Constant(constant.Value);
    }

    private Variable EmitUnary(ImmutableArray<Instruction>.Builder builder, Ast.Expressions.Unary unary)
    {
        var source = EmitExpression(builder, unary.Expression);
        var destinationName = MakeTemporary();
        var destination = new Variable(destinationName);
        var tackyOperator = ConvertUnaryOperator(unary.Operator);
        builder.Add(new Unary(tackyOperator, source, destination));

        return destination;
    }

    private static UnaryOperator ConvertUnaryOperator(Ast.UnaryOperator @operator) => @operator switch
    {
        Ast.UnaryOperators.Complement => new Complement(),
        Ast.UnaryOperators.Negate => new Negate(),
        _ => throw new NotImplementedException(),
    };

    private string MakeTemporary() => $"tmp.{temporaryCounter++}";
}
