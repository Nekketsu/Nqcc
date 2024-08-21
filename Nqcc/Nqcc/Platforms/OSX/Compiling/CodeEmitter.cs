namespace Nqcc.Platforms.OSX.Compiling;

public class CodeEmitter(TextWriter writer) : Nqcc.Compiling.CodeEmitter(writer)
{
    protected override string GetName(string name)
    {
        return $"_{name}";
    }

    protected override void EmitStackNote()
    {
    }
}
