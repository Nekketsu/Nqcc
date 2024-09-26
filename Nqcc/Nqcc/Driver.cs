using Nqcc.Compiling;
using System.Diagnostics;

namespace Nqcc;

public abstract class Driver(Settings settings) : IDriver
{
    public async Task Drive(string source)
    {
        ValidateExtension(source);

        var preprocessedFile = await PreprocessAsync(source);

        var compiler = GetCompiler();
        var assemblyFile = await compiler.CompileAsync(preprocessedFile, settings.Stage);

        File.Delete(preprocessedFile);

        if (settings.Stage == Stage.Object)
        {
            await Assemble(assemblyFile);
        }
        else if (settings.Stage == Stage.Executable)
        {
            await AssembleAndLinkAsync(assemblyFile);
        }

        if (!settings.Debug)
        {
            File.Delete(assemblyFile);
        }
    }

    protected static Process StartProcess(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo(fileName, arguments)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var process = new Process
        {
            StartInfo = startInfo
        };
        process.Start();

        return process;
    }

    protected abstract ICompiler GetCompiler();

    protected virtual async Task<string> PreprocessAsync(string inputFile)
    {
        var preprocessedFile = Path.ChangeExtension(inputFile, ".i");

        var process = StartProcess("gcc", $"-E -P {inputFile} -o {preprocessedFile}");
        await process.WaitForExitAsync();

        return preprocessedFile;
    }

    protected virtual async Task<string> AssembleAndLinkAsync(string assemblyFile)
    {
        var path = Path.GetDirectoryName(assemblyFile);
        var file = Path.GetFileNameWithoutExtension(assemblyFile);
        string outputFile = path is null
            ? file
            : Path.Combine(path, file);

        var process = StartProcess("gcc", $"{assemblyFile} -o {outputFile}");
        await process.WaitForExitAsync();

        return outputFile;
    }

    protected virtual async Task<string> Assemble(string assemblyFile)
    {
        var path = Path.GetDirectoryName(assemblyFile);
        var file = Path.ChangeExtension(assemblyFile, ".o");
        string outputFile = path is null
            ? file
            : Path.Combine(path, file);

        var process = StartProcess("gcc", $"-c {assemblyFile} -o {outputFile}");
        await process.WaitForExitAsync();

        return outputFile;
    }

    private static void ValidateExtension(string inputFile)
    {
        var extension = Path.GetExtension(inputFile);
        if (!(extension == ".c" || extension == ".h"))
        {
            throw new Exception("Expected C source file with .c or .h extension");
        }
    }
}
