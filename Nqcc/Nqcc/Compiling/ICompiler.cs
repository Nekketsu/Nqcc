namespace Nqcc.Compiling;

public interface ICompiler
{
    Task<string> CompileAsync(string preprocessedFile, Stage stage = Stage.Codegen);
}
