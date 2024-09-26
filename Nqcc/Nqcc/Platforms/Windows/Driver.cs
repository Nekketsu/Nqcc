using Nqcc.Platforms.Windows.Compiling;

namespace Nqcc.Platforms.Windows;

public class Driver(Settings settings) : Nqcc.Driver(settings)
{
    protected override Nqcc.Compiling.ICompiler GetCompiler() => new Compiler();

    protected override async Task<string> PreprocessAsync(string inputFile)
    {
        var process = StartProcess(@"cl", $"/EP {inputFile}");
        await process.WaitForExitAsync();

        var preprocessedFile = Path.ChangeExtension(inputFile, ".i");
        using var fileStream = File.Create(preprocessedFile);
        await process.StandardOutput.BaseStream.CopyToAsync(fileStream);

        return preprocessedFile;
    }

    protected override async Task<string> AssembleAndLinkAsync(string assemblyFile)
    {
        var outputFile = Path.ChangeExtension(assemblyFile, ".exe");

        var process = StartProcess(@"ml64", $"{assemblyFile} /link /entry:main /OUT:{outputFile}");
        await process.WaitForExitAsync();

        return outputFile;
    }

    protected override async Task<string> Assemble(string assemblyFile)
    {
        var outputFile = Path.ChangeExtension(assemblyFile, ".obj");

        var process = StartProcess(@"ml64", $"/c {assemblyFile}");
        await process.WaitForExitAsync();

        return outputFile;
    }
}
