namespace Nqcc.Platforms.OSX.Compiling;

public class CodeEmitter(TextWriter writer) : Nqcc.Compiling.CodeEmitter(writer)
{
    protected override string GetLabelName(string name) => $"_{name}";

    protected override string GetLocalLabelName(string name) => $"L{name}";

    protected override string GetFunctionName(string name) => $"_{name}";

    protected override void EmitStackNote()
    {
    }
}
