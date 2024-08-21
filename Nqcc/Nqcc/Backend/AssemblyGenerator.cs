using Nqcc.Assembly;
using Nqcc.Assembly.Instructions;
using Nqcc.Assembly.Operands;
using System.Collections.Immutable;

namespace Nqcc.Backend;

public class AssemblyGenerator(Ast.Program ast)
{
    public Program Convert() => ConvertProgram(ast);

    private static Program ConvertProgram(Ast.Program program)
    {
        var function = ConvertFunction(program.FunctionDefinition);
        return new Program(function);
    }

    private static Function ConvertFunction(Ast.Function function)
    {
        var body = ConvertStatement(function.Body);
        return new Function(function.Name, body);
    }

    private static ImmutableArray<Instruction> ConvertStatement(Ast.Statement statement) => statement switch
    {
        Ast.Statements.Return @return => ConvertReturnStatement(@return),
        _ => throw new NotImplementedException()
    };

    private static ImmutableArray<Instruction> ConvertReturnStatement(Ast.Statements.Return @return)
    {
        var builder = ImmutableArray.CreateBuilder<Instruction>();

        var expression = ConvertExpression(@return.Expression);

        builder.AddRange(
            new Mov(expression, new Register()),
            new Ret()
        );

        return builder.ToImmutable();
    }

    private static Imm ConvertExpression(Ast.Expression expression) => expression switch
    {
        Ast.Expressions.Constant constant => ConvertConstantExpression(constant),
        _ => throw new NotImplementedException()
    };

    private static Imm ConvertConstantExpression(Ast.Expressions.Constant constant)
    {
        var value = constant.Value;
        return new Imm(value);
    }
}
