namespace Nqcc.Platforms.Linux.Compiling;

public class CodeEmitter(TextWriter writer) : Nqcc.Compiling.CodeEmitter(writer)
{
    protected override string GetName(string name)
    {
        return name;
    }

    protected override string GetLabelName(string name) => $".L{name}";

    protected override void EmitStackNote()
    {
        writer.WriteLine("\t.section\t.note.GNU-stack,\"\",@progbits");
    }
}
