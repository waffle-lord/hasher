/* hasher.exe
        validate
        create
            --Algorithm
                valid options: MD5, SHA1, SHA256, SHA384, SHA512
            --Path
                <file_path>
*/

// options

using System.CommandLine;
using System.Reflection;
using hasher.Model;
using Spectre.Console;

var algorithmOption = new Option<SupportedAlgorithms>("--algorithm")
{
    Description = "The algorithm to use to create the hashes",
    DefaultValueFactory = _ => SupportedAlgorithms.MD5,
    Recursive = true
};

// arguments
var pathArgument = new Argument<string>("path")
{
    Description = "The path to the directory to create or validate hashes for",
    DefaultValueFactory = _ => Directory.GetCurrentDirectory()
};

// commands
var createCommand = new Command("create")
{
    Arguments = { pathArgument }
};

var validateCommand = new Command("validate")
{
    Arguments = { pathArgument }
};
        
// command actions
createCommand.SetAction(async parseResult =>
{
    var pathArg = parseResult.GetValue(pathArgument);
    var algorithm = parseResult.GetValue(algorithmOption);
    
    AnsiConsole.MarkupLine($"[Gray]Algorithm :[/] [blue]{algorithm}[/]");
    AnsiConsole.MarkupLine($"[Gray]Path      :[/] [blue]{pathArg}[/]");
    AnsiConsole.WriteLine();

    // don't include the currently running program in the event current directory is selected
    var programFile = new FileInfo(Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location);
    var fileList = FileHelper.GetFilesToCheck(new DirectoryInfo(pathArg ?? Directory.GetCurrentDirectory()), [programFile]);
    
    var cancelTokenSource = new CancellationTokenSource();

    await AnsiConsole.Progress().Columns(
        new SpinnerColumn(),
        new TaskDescriptionColumn(),
        new ProgressBarColumn(),
        new PercentageColumn(),
        new RemainingTimeColumn()
        ).StartAsync(async ctx =>
    {
        var task = ctx.AddTask("Hashing files", maxValue: fileList.Count);
        
        await Parallel.ForEachAsync(fileList, cancelTokenSource.Token, async (file, token) =>
        {
            var hash = await FileHelper.GetFileHashAsync(algorithm, file);
            AnsiConsole.MarkupLine($"[yellow]{hash}[/] [gray]::[/] [yellow]{file.Name}[/]");
            task.Increment(1);
            task.Description = $"Hashing files ({task.Value} / {task.MaxValue})";
            ctx.Refresh();
        });
    });
});
        
validateCommand.SetAction(async parseResult=>
{
    var pathArg = parseResult.GetValue(pathArgument);
    var algorithm = parseResult.GetValue(algorithmOption);
});

// root command
var rootCommand = new RootCommand("Just a simple hashing tool")
{
    Options = {  algorithmOption },
    Subcommands = {  createCommand, validateCommand }
};

var figlet = new FigletText("Hasher").Color(Color.Blue);
var rule = new Rule
{
    Title = "[purple]Just a simple hashing tool[/]",
    Justification = Justify.Left,
    Style = new Style().Foreground(Color.Blue),
};
AnsiConsole.Write(figlet);
AnsiConsole.Write(rule);

await rootCommand.Parse(args).InvokeAsync();