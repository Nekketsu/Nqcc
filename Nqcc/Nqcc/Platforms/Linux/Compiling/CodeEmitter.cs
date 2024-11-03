using Nqcc.Symbols;

namespace Nqcc.Platforms.Linux.Compiling;

public class CodeEmitter(SymbolTable symbols, TextWriter writer) : Nqcc.Compiling.CodeEmitter(writer)
{
    protected override string GetLabelName(string name) => name;

    protected override string GetLocalLabelName(string name) => $".L{name}";

    protected override string GetFunctionName(string name)
    {
        var function = symbols.GetFunction(name);

        return function.Attributes.Defined ? name : $"{name}@PLT";
    }

    protected override string GetAlignDirective(int alignment) => $".align {alignment}";

    protected override void EmitStackNote()
    {
        writer.WriteLine("\t.section\t.note.GNU-stack,\"\",@progbits");
    }
}
