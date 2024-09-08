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
var validateOption = new Option<bool>("--validate", "Run the lexer, parser, and semantic analysis");
var tackyOption = new Option<bool>("--tacky", "Run the lexer, parser, and tacky generator");
var codegenOption = new Option<bool>("--codegen", "Run through code generation but stop befor emitting assembly");
var assemblyOption = new Option<bool>(["-s", "-S"], "Stop before assembling (keep assembly file)");

var debugOption = new Option<bool>("-d", "Write out pre- and post-register-allocation assembly and DOT files of interference graphs.");

var targetOption = new Option<Platform>("--target", () => defaultPlatform, "Choose target platform");

var rootCommand = new RootCommand("A not-quite-C compiler")
{
    sourceArgument,
    lexOption,
    parseOption,
    validateOption,
    tackyOption,
    codegenOption,
    assemblyOption,
    targetOption,
    debugOption
};

rootCommand.AddValidator(result =>
{
    if (result.Children.Count(c => c.Symbol == lexOption ||
                                   c.Symbol == parseOption ||
                                   c.Symbol == validateOption ||
                                   c.Symbol == tackyOption ||
                                   c.Symbol == codegenOption ||
                                   c.Symbol == assemblyOption) > 1)
    {
        result.ErrorMessage = "You must use either --lex or --parse or --tacky or --codegen";
    }
});

rootCommand.SetHandler(async context =>
{
    var source = context.ParseResult.GetValueForArgument(sourceArgument);
    var lex = context.ParseResult.GetValueForOption(lexOption);
    var parse = context.ParseResult.GetValueForOption(parseOption);
    var validate = context.ParseResult.GetValueForOption(validateOption);
    var tacky = context.ParseResult.GetValueForOption(tackyOption);
    var codegen = context.ParseResult.GetValueForOption(codegenOption);
    var assembly = context.ParseResult.GetValueForOption(assemblyOption);
    var target = context.ParseResult.GetValueForOption(targetOption);
    var debug = context.ParseResult.GetValueForOption(debugOption);

    var stage = lex
        ? Stage.Lex
        : parse
            ? Stage.Parse
            : validate
                ? Stage.Validate
                : tacky
                    ? Stage.Tacky
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
});

await rootCommand.InvokeAsync(args);

return returnCode;