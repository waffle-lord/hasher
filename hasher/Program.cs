/* hasher.exe
        validate
        create
            <path>
            --Algorithm
                valid options: [MD5], SHA1, SHA256, SHA384, SHA512
*/

using System.Collections.Concurrent;
using System.CommandLine;
using System.Reflection;
using hasher.Model;
using Spectre.Console;

// options
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

    pathArg = Path.TrimEndingDirectorySeparator(pathArg ?? Directory.GetCurrentDirectory());
    var dirInfo = new DirectoryInfo(pathArg);
    var outputFilePath = Path.Join(pathArg, $"{dirInfo.Name}.{algorithm}");
    var outputFile = new FileInfo(outputFilePath);

    if (outputFile.Exists)
    {
        if (await AnsiConsole.ConfirmAsync($"Output file '{outputFile.Name}' already exists. Overwrite?"))
        {
            outputFile.Delete();
        }
        else
        {
            AnsiConsole.MarkupLine("[red]aborting :([/]");
            return;
        }
    }

    // don't include the currently running program in the event current directory is selected
    var programFile = new FileInfo(Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location);
    var fileList = FileHelper.GetFilesToCheck(dirInfo, [programFile]);
    
    var cancelTokenSource = new CancellationTokenSource();
    var outputBag = new ConcurrentBag<string>();

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
            var relativeFilePath = file.FullName.Replace(dirInfo.FullName, string.Empty);
            var hash = await FileHelper.GetFileHashAsync(algorithm, file);
            outputBag.Add($"{hash} :: {relativeFilePath}");
            AnsiConsole.MarkupLine($"[yellow]{hash.EscapeMarkup()}[/] [gray]::[/] [yellow]{relativeFilePath.EscapeMarkup()}[/]");
            task.Increment(1);
            task.Description = $"Hashing files ( {task.Value} / {task.MaxValue} )";
            ctx.Refresh();
        });
    });
    
    await AnsiConsole.Status().StartAsync("saving hash data ...", async ctx =>
    { 
        await File.WriteAllLinesAsync(outputFilePath, outputBag);
    });
        
    AnsiConsole.MarkupLine($"[green]hash data saved: {outputFile.FullName.EscapeMarkup()}[/]");
});
        
validateCommand.SetAction(async parseResult=>
{
    var pathArg = parseResult.GetValue(pathArgument);
    var algorithm = parseResult.GetValue(algorithmOption);
    
    AnsiConsole.MarkupLine($"[Gray]Algorithm :[/] [blue]{algorithm}[/]");
    AnsiConsole.MarkupLine($"[Gray]Path      :[/] [blue]{pathArg}[/]");
    AnsiConsole.WriteLine();

    pathArg = Path.TrimEndingDirectorySeparator(pathArg ?? Directory.GetCurrentDirectory());
    var dirInfo = new DirectoryInfo(pathArg);
    var hashFilePath = Path.Join(pathArg, $"{dirInfo.Name}.{algorithm}");
    var hashFile = new FileInfo(hashFilePath);

    if (!hashFile.Exists)
    {
        AnsiConsole.MarkupLine($"[red]Hash file doesn't exist[/]");
        return;
    }

    var hashData = File.ReadAllLines(hashFile.FullName);
    
    // don't include the currently running program AND hash file in the event current directory is selected
    var programFile = new FileInfo(Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location);
    var fileList = FileHelper.GetFilesToCheck(dirInfo, [programFile, hashFile]);
    
    var cancelTokenSource = new CancellationTokenSource();
    var okCount = 0;

    await AnsiConsole.Progress().Columns(
        new SpinnerColumn(),
        new TaskDescriptionColumn(),
        new ProgressBarColumn(),
        new PercentageColumn(),
        new RemainingTimeColumn()
    ).StartAsync(async ctx =>
    {
        var task = ctx.AddTask("Validating files", maxValue: fileList.Count);
        
        await Parallel.ForEachAsync(fileList, cancelTokenSource.Token, async (file, token) =>
        {
            var relativeFilePath = file.FullName.Replace(dirInfo.FullName, string.Empty);
            var hash = await FileHelper.GetFileHashAsync(algorithm, file);
            if (hashData.Contains($"{hash} :: {relativeFilePath}"))
            {
                okCount++;
                AnsiConsole.MarkupLine($"[green]{hash.EscapeMarkup()}[/] [gray]::[/] [green]{relativeFilePath.EscapeMarkup()}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]{hash.EscapeMarkup()}[/] [gray]::[/] [red]{relativeFilePath.EscapeMarkup()}[/]");
            }
            
            task.Increment(1);
            task.Description = $"Validating files ( {task.Value} / {task.MaxValue} )";
            ctx.Refresh();
        });
    });

    AnsiConsole.Write(new Rule {Style =  new Style().Foreground(Color.Gray)});

    AnsiConsole.MarkupLine(okCount == fileList.Count
        ? "[green]All files are OK[/]"
        : "[red]Some files failed validation[/]");
});

// root command
var rootCommand = new RootCommand("A simple, portable, directory hashing tool")
{
    Options = {  algorithmOption },
    Subcommands = {  createCommand, validateCommand }
};

// startup banner for fancy
var version = Assembly.GetExecutingAssembly().GetName().Version;
var figlet = new FigletText("Hasher").Color(Color.Blue);
var rule = new Rule
{
    Title = $"[purple]A simple, portable, directory hashing tool[/] [gray]( v[/][yellow]{version?.ToString().EscapeMarkup() ?? "n/a"}[/] [gray])[/]",
    Justification = Justify.Left,
    Style = new Style().Foreground(Color.Blue),
};
AnsiConsole.Write(figlet);
AnsiConsole.Write(rule);

await rootCommand.Parse(args).InvokeAsync();