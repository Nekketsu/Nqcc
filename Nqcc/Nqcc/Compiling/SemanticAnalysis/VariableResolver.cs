using Nqcc.Ast;
using Nqcc.Ast.Expressions;
using System.Collections.Immutable;

namespace Nqcc.Compiling.SemanticAnalysis;

public class VariableResolver(Program ast)
{
    private readonly VariableMap variableMap = new();

    public Program Resolve() => ResolveProgram(ast);

    private Program ResolveProgram(Program program)
    {
        var functionDefinition = ResolveFunction(program.FunctionDefinition);

        return new Program(functionDefinition);
    }

    private Function ResolveFunction(Function function)
    {
        var body = ResolveBlock(function.Body);

        return new Function(function.Name, body);
    }

    private Block ResolveBlock(Block block)
    {
        var builder = ImmutableArray.CreateBuilder<BlockItem>();

        foreach (var blockItem in block.BlockItems)
        {
            builder.Add(ResolveBlockItem(blockItem));
        }

        return new Block(builder.ToImmutable());
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
        Ast.Statements.Compound compound => ResolveCompound(compound),
        Ast.Statements.If @if => new Ast.Statements.If(ResolveExpression(@if.Condition), ResolveStatement(@if.Then), @if.Else is null ? null : ResolveStatement(@if.Else)),
        Ast.Statements.Label label => new Ast.Statements.Label(label.Name, ResolveStatement(label.Statement)),
        _ => statement
    };

    private Ast.Statements.Compound ResolveCompound(Ast.Statements.Compound compound)
    {
        variableMap.Push();
        var block = ResolveBlock(compound.Block);
        variableMap.Pop();

        return new Ast.Statements.Compound(block);
    }

    private Declaration ResolveDeclaration(Declaration declaration)
    {
        if (variableMap.Contains(declaration.Name))
        {
            throw new Exception("Duplicate variable declaration!");
        }

        var uniqueName = UniqueId.MakeTemporary();
        variableMap[declaration.Name] = uniqueName;
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

        Conditional conditional => new Conditional(ResolveExpression(conditional.Condition), ResolveExpression(conditional.Then), ResolveExpression(conditional.Else)),

        _ => expression
    };
}
