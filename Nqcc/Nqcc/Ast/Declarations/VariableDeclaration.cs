namespace Nqcc.Ast.Declarations;

public class VariableDeclaration(string name, Expression? initializer, StorageClass? storageClass) : Declaration
{
    public string Name { get; } = name;
    public Expression? Initializer { get; } = initializer;
    public StorageClass? StorageClass { get; } = storageClass;
}
