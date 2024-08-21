using Nqcc;
using System.CommandLine;


var returnCode = 0;
var defaultPlatform = OperatingSystem.IsLinux()
    ? Platform.Linux
    : OperatingSystem.IsMacOS()
        ? Platform.OSX
        : Platform.Windows;


var sourceArgument = new Argument<string>("source", "Source File");

var lexOption = new Option<bool>("--lex", "Run the lexer");
var parseOption = new Option<bool>("--parse", "Run the lexer and parser");
var codegenOption = new Option<bool>("--codegen", "Run through code generation but stop befor emitting assembly");
var assemblyOption = new Option<bool>(["-s", "-S"], "Stop before assembling (keep assembly file)");

var debugOption = new Option<bool>("-d", "Write out pre- and post-register-allocation assembly and DOT files of interference graphs.");

var targetOption = new Option<Platform>("--target", () => defaultPlatform, "Choose target platform");

var rootCommand = new RootCommand("A not-quite-C compiler")
{
    sourceArgument,
    lexOption,
    parseOption,
    codegenOption,
    assemblyOption,
    targetOption,
    debugOption
};

rootCommand.AddValidator(result =>
{
    if (result.Children.Count(c => c.Symbol == lexOption ||
                                   c.Symbol == parseOption ||
                                   c.Symbol == codegenOption ||
                                   c.Symbol == assemblyOption) > 1)
    {
        result.ErrorMessage = "You must use either --lex or --parse or --codegen";
    }
});

rootCommand.SetHandler(async (source, lex, parse, codegen, assembly, target, debug) =>
{
    var stage = lex
        ? Stage.Lex
        : parse
            ? Stage.Parse
            : codegen
                ? Stage.Codegen
                : assembly
                    ? Stage.Assembly
                    : Stage.Executable;

    var settings = new Settings
    {
        Stage = stage,
        Debug = debug
    };

    IDriver driver = target switch
    {
        Platform.Linux => new Nqcc.Platforms.Linux.Driver(settings),
        Platform.OSX => new Nqcc.Platforms.OSX.Driver(settings),
        Platform.Windows => new Nqcc.Platforms.Windows.Driver(settings),
        _ => throw new NotImplementedException()
    };

    try
    {
        await driver.Drive(source);
    }
    catch (Exception e)
    {
        returnCode = 1;
        Console.Error.WriteLine(e.Message);
        throw;
    }
}, sourceArgument, lexOption, parseOption, codegenOption, assemblyOption, targetOption, debugOption);

await rootCommand.InvokeAsync(args);

return returnCode;