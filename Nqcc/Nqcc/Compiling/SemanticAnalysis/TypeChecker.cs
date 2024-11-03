using Nqcc.Ast;
using Nqcc.Ast.Declarations;
using Nqcc.Ast.ForInits;
using Nqcc.Ast.StorageClasses;
using Nqcc.Symbols;
using Nqcc.Symbols.IdentifierAttributes;
using Nqcc.Symbols.InitialValues;
using Nqcc.Symbols.Types;

namespace Nqcc.Compiling.SemanticAnalysis;

public class TypeChecker(SymbolTable symbols, Program program)
{
    public void Check() => CheckProgram(program);

    private void CheckProgram(Program program)
    {
        foreach (var declaration in program.Declarations)
        {
            CheckGlobalDeclaration(declaration);
        }
    }

    private void CheckGlobalDeclaration(Declaration declaration)
    {
        switch (declaration)
        {
            case FunctionDeclaration functionDeclaration:
                CheckFunctionDeclaration(functionDeclaration);
                break;
            case VariableDeclaration variableDeclaration:
                CheckFileScopeVariableDeclaration(variableDeclaration);
                break;
        }
    }

    private void CheckFunctionDeclaration(FunctionDeclaration functionDeclaration)
    {
        var functionType = new FunctionType(functionDeclaration.Parameters.Length);
        var hasBody = functionDeclaration.Body is not null;
        var alreadyDefined = false;
        var global = functionDeclaration.StorageClass is not Static;

        if (symbols.TryGetValue(functionDeclaration.Name, out var symbol))
        {
            if (symbol is not Function function || function.FunctionType.ParameterCount != functionDeclaration.Parameters.Length)
            {
                throw new Exception($"Redeclared function {functionDeclaration.Name} with a different type");
            }

            alreadyDefined = function.Attributes.Defined;
            if (alreadyDefined && hasBody)
            {
                throw new Exception($"Defined body of function {functionDeclaration.Name} twice");
            }

            if (function.Attributes.Global && functionDeclaration.StorageClass is Static)
            {
                throw new Exception("Static function declaration follows non-static");
            }

            global = function.Attributes.Global;
        }

        var attributes = new FunctionAttributes(alreadyDefined || hasBody, global);
        symbols.AddOrReplace(new Function(functionDeclaration.Name, functionType, attributes));

        if (functionDeclaration.Body is not null)
        {
            foreach (var paramenter in functionDeclaration.Parameters)
            {
                symbols.Add(new Variable(paramenter, new Int(), new LocalAttributes()));
            }
            CheckBlock(functionDeclaration.Body);
        }
    }

    private void CheckFileScopeVariableDeclaration(VariableDeclaration variableDeclaration)
    {
        InitialValue initialValue = variableDeclaration.Initializer switch
        {
            Ast.Expressions.Constant constant => new Initial(constant.Value),
            null => variableDeclaration.StorageClass is Extern
                ? new NoInitializer()
                : new Tentative(),
            _ => throw new Exception("Non-constant initializer!"),
        };

        var global = variableDeclaration.StorageClass is not Static;

        if (symbols.TryGetValue(variableDeclaration.Name, out var symbol))
        {
            if (symbol is not Variable variable)
            {
                throw new Exception("Function redeclared as variable");
            }
            if (variable.Attributes is not StaticAttributes staticAttributes)
            {
                throw new Exception("Internal error, file-scope variable previously declared as local variable or function");
            }

            if (variableDeclaration.StorageClass is Extern)
            {
                global = staticAttributes.Global;
            }
            else if (staticAttributes.Global != global)
            {
                throw new Exception("Conflicting variable linkage");
            }

            if (staticAttributes.InitialValue is Initial)
            {
                if (initialValue is Initial)
                {
                    throw new Exception("Conflicting file scope variable definitions");
                }
                else
                {
                    initialValue = staticAttributes.InitialValue;
                }
            }
            else if (initialValue is not Initial && staticAttributes.InitialValue is Tentative)
            {
                initialValue = new Tentative();
            }
        }

        var attributes = new StaticAttributes(initialValue, global);
        symbols.AddOrReplace(new Variable(variableDeclaration.Name, new Int(), attributes));
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
                CheckLocalVariableDeclaration(variableDeclaration);
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
                var functionSymbol = symbols.GetFunction(functionCall.Name);
                if (functionSymbol.FunctionType.ParameterCount != functionCall.Arguments.Length)
                {
                    throw new Exception("Function called with wrong number of argumnets");
                }
                foreach (var argument in functionCall.Arguments)
                {
                    CheckExpression(argument);
                }
                break;
            case Ast.Expressions.Variable variable:
                _ = symbols.GetVariable(variable.Name);
                break;
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
            case InitDeclaration { Declaration.StorageClass: not null }:
                throw new Exception("Storage class not permitted on declaration in for loop header");
            case InitDeclaration initDeclaration:
                CheckLocalVariableDeclaration(initDeclaration.Declaration);
                break;
            case InitExpression initExpression:
                if (initExpression.Expression is not null)
                {
                    CheckExpression(initExpression.Expression);
                }
                break;
        }
    }

    private void CheckLocalVariableDeclaration(VariableDeclaration variableDeclaration)
    {
        if (variableDeclaration.StorageClass is Extern)
        {
            if (variableDeclaration.Initializer is not null)
            {
                throw new Exception("Initializer or local extern variable declaration");
            }
            if (symbols.TryGetValue(variableDeclaration.Name, out var symbol))
            {
                if (symbol is not Variable)
                {
                    throw new Exception("Function declared as variable");
                }
            }
            else
            {
                symbols.Add(new Variable(variableDeclaration.Name, new Int(), new StaticAttributes(new NoInitializer(), true)));
            }
        }
        else if (variableDeclaration.StorageClass is Static)
        {
            InitialValue initialValue = variableDeclaration.Initializer switch
            {
                Ast.Expressions.Constant constant => new Initial(constant.Value),
                null => new Initial(0),
                _ => throw new Exception("Non-constant initializer on local static variable"),
            };
            symbols.AddOrReplace(new Variable(variableDeclaration.Name, new Int(), new StaticAttributes(initialValue, false)));

        }
        else
        {
            symbols.Add(new Variable(variableDeclaration.Name, new Int(), new LocalAttributes()));
            if (variableDeclaration.Initializer is not null)
            {
                CheckExpression(variableDeclaration.Initializer);
            }
        }
    }
}
