using Nqcc.Ast;
using Nqcc.Ast.Expressions;
using System.Collections.Immutable;

namespace Nqcc.Compiling.SemanticAnalysis;

public class Resolver(Program ast)
{
    private readonly Dictionary<string, string> variableMap = [];

    public Program Resolve() => ResolveProgram(ast);

    private Program ResolveProgram(Program program)
    {
        var functionDefinition = ResolveFunction(program.FunctionDefinition);

        return new Program(functionDefinition);
    }

    private Function ResolveFunction(Function function)
    {
        var builder = ImmutableArray.CreateBuilder<BlockItem>();

        foreach (var blockItem in function.Body)
        {
            builder.Add(ResolveBlockItem(blockItem));
        }

        var body = builder.ToImmutable();

        return new Function(function.Name, body);
    }

    private BlockItem ResolveBlockItem(BlockItem blockItem) => blockItem switch
    {
        Ast.BlockItems.Statement statement => new Ast.BlockItems.Statement(ResolveStatement(statement.InnerStatement)),
        Ast.BlockItems.Declaration declaration => new Ast.BlockItems.Declaration(ResolveDeclaration(declaration.InnerDeclaration)),
        _ => blockItem
    };

    private Statement ResolveStatement(Statement statement) => statement switch
    {
        Ast.Statements.Return @return => new Ast.Statements.Return(ResolveExpression(@return.Expression)),
        Ast.Statements.Expression expression => new Ast.Statements.Expression(ResolveExpression(expression.InnerExpression)),
        _ => statement
    };

    private Declaration ResolveDeclaration(Declaration declaration)
    {
        if (variableMap.ContainsKey(declaration.Name))
        {
            throw new Exception("Duplicate variable declaration!");
        }

        var uniqueName = UniqueId.MakeTemporary();
        variableMap.Add(declaration.Name, uniqueName);
        var initializer = declaration.Initializer is null
            ? null
            : ResolveExpression(declaration.Initializer);

        return new Declaration(uniqueName, initializer);
    }

    private Expression ResolveExpression(Expression expression) => expression switch
    {
        Assignment { Left: Variable } assignment => new Assignment(ResolveExpression(assignment.Left), ResolveExpression(assignment.Right)),
        Assignment => throw new Exception("Invalid lvalue!"),

        Variable variable when variableMap.TryGetValue(variable.Name, out var resolvedVariableName) => new Variable(resolvedVariableName),
        Variable => throw new Exception("Undeclared variable!"),

        Unary unary => new Unary(unary.Operator, ResolveExpression(unary.Expression)),
        Binary binary => new Binary(ResolveExpression(binary.Left), binary.Operator, ResolveExpression(binary.Right)),
        
        Compound { Left: Variable } compound => new Compound(ResolveExpression(compound.Left), compound.Operator, ResolveExpression(compound.Right)),
        Compound => throw new Exception("Invalid lvalue!"),

        Prefix { Expression: Variable} prefix => new Prefix(prefix.Operator, ResolveExpression(prefix.Expression)),
        Prefix => throw new Exception("Invalid lvalue!"),

        Postfix { Expression: Variable } postfix => new Postfix(ResolveExpression(postfix.Expression), postfix.Operator),
        Postfix => throw new Exception("Invalid lvalue!"),

        _ => expression
    };
}
