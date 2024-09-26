using Nqcc.Ast;
using Nqcc.Ast.Declarations;
using Nqcc.Ast.ForInits;
using Nqcc.Symbols;

namespace Nqcc.Compiling.SemanticAnalysis;

public class TypeChecker(SymbolTable symbols, Program program)
{
    public void Check() => CheckProgram(program);

    private void CheckProgram(Program program)
    {
        foreach (var functionDeclaration in program.FunctionDeclarations)
        {
            CheckFunctionDeclaration(functionDeclaration);
        }
    }

    private void CheckFunctionDeclaration(FunctionDeclaration functionDeclaration)
    {
        var hasBody = functionDeclaration.Body is not null;
        var alreadyDefined = false;

        if (symbols.TryGetValue(functionDeclaration.Name, out var symbol))
        {
            if (symbol is not Function function || function.ParameterCount != functionDeclaration.Parameters.Length)
            {
                throw new Exception($"Redeclared function {functionDeclaration.Name} with a different type");
            }

            alreadyDefined = function.IsDefined;
            if (alreadyDefined && hasBody)
            {
                throw new Exception($"Defined body of function {functionDeclaration.Name} twice");
            }
        }

        symbols.AddOrReplace(new Function(functionDeclaration.Name, functionDeclaration.Parameters.Length, alreadyDefined || hasBody));

        if (functionDeclaration.Body is not null)
        {
            foreach (var paramenter in functionDeclaration.Parameters)
            {
                symbols.Add(new Variable(paramenter, new Symbols.Types.Int()));
            }
            CheckBlock(functionDeclaration.Body);
        }
    }

    private void CheckBlock(Block block)
    {
        foreach (var blockItem in block.BlockItems)
        {
            CheckBlockItem(blockItem);
        }
    }

    private void CheckBlockItem(BlockItem blockItem)
    {
        switch (blockItem)
        {
            case Ast.BlockItems.Statement statement:
                CheckStatement(statement.InnerStatement);
                break;
            case Ast.BlockItems.Declaration declaration:
                CheckDeclaration(declaration.InnerDeclaration);
                break;
        }
    }

    private void CheckStatement(Statement statement)
    {
        switch (statement)
        {
            case Ast.Statements.Return @return:
                CheckExpression(@return.Expression);
                break;
            case Ast.Statements.Expression expression:
                CheckExpression(expression.InnerExpression);
                break;
            case Ast.Statements.If @if:
                CheckExpression(@if.Condition);
                CheckStatement(@if.Then);
                if (@if.Else is not null)
                {
                    CheckStatement(@if.Else);
                }
                break;
            case Ast.Statements.Compound compound:
                CheckBlock(compound.Block);
                break;
            case Ast.Statements.While @while:
                CheckExpression(@while.Condition);
                CheckStatement(@while.Body);
                break;
            case Ast.Statements.DoWhile doWhile:
                CheckStatement(doWhile.Body);
                CheckExpression(doWhile.Condition);
                break;
            case Ast.Statements.For @for:
                CheckForInit(@for.Init);
                if (@for.Condition is not null)
                {
                    CheckExpression(@for.Condition);
                }
                if (@for.Post is not null)
                {
                    CheckExpression(@for.Post);
                }
                CheckStatement(@for.Body);
                break;
            case Ast.Statements.Switch @switch:
                CheckExpression(@switch.Condition);
                CheckStatement(@switch.Body);
                break;
            case Ast.Statements.Case @case:
                CheckExpression(@case.Condition);
                CheckStatement(@case.Statement);
                break;
            case Ast.Statements.Default @default:
                CheckStatement(@default.Statement);
                break;
        }
    }

    private void CheckDeclaration(Declaration declaration)
    {
        switch (declaration)
        {
            case VariableDeclaration variableDeclaration:
                CheckVariableDeclaration(variableDeclaration);
                break;
            case FunctionDeclaration functionDeclaration:
                CheckFunctionDeclaration(functionDeclaration);
                break;
        }
    }

    private void CheckExpression(Expression expression)
    {
        switch (expression)
        {
            case Ast.Expressions.FunctionCall functionCall:
                {
                    var symbol = symbols[functionCall.Name];
                    switch (symbol)
                    {
                        case Variable:
                            throw new Exception("Tried to use variable as a function name");
                        case Function function:
                            if (function.ParameterCount != functionCall.Arguments.Length)
                            {
                                throw new Exception("Function called with wrong number of argumnets");
                            }
                            foreach (var argument in functionCall.Arguments)
                            {
                                CheckExpression(argument);
                            }
                            break;
                    }
                    break;
                }
            case Ast.Expressions.Variable variable:
                {
                    var symbol = symbols[variable.Name];
                    if (symbol is Function)
                    {
                        throw new Exception("Tried to use function name as variable");
                    }
                    break;
                }
            case Ast.Expressions.Unary unary:
                CheckExpression(unary.Expression);
                break;
            case Ast.Expressions.Binary binary:
                CheckExpression(binary.Left);
                CheckExpression(binary.Right);
                break;
            case Ast.Expressions.Assignment assignment:
                CheckExpression(assignment.Left);
                CheckExpression(assignment.Right);
                break;
            case Ast.Expressions.Conditional conditional:
                CheckExpression(conditional.Condition);
                CheckExpression(conditional.Then);
                CheckExpression(conditional.Else);
                break;
            case Ast.Expressions.Compound compound:
                CheckExpression(compound.Left);
                CheckExpression(compound.Right);
                break;
            case Ast.Expressions.Prefix prefix:
                CheckExpression(prefix.Expression);
                break;
            case Ast.Expressions.Postfix postfix:
                CheckExpression(postfix.Expression);
                break;
        }
    }

    private void CheckForInit(ForInit init)
    {
        switch (init)
        {
            case InitDeclaration initDeclaration:
                CheckVariableDeclaration(initDeclaration.Declaration);
                break;
            case InitExpression initExpression:
                if (initExpression.Expression is not null)
                {
                    CheckExpression(initExpression.Expression);
                }
                break;
        }
    }

    private void CheckVariableDeclaration(VariableDeclaration declaration)
    {
        symbols.Add(new Variable(declaration.Name, new Symbols.Types.Int()));
        if (declaration.Initializer is not null)
        {
            CheckExpression(declaration.Initializer);
        }
    }
}
