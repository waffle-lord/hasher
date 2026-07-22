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
using ValidationResult = hasher.Model.ValidationResult;

// options
var algorithmOption = new Option<SupportedAlgorithms>("--algorithm")
{
    Description = "The algorithm to use to create the hashes",
    Aliases = { "-a" },
    DefaultValueFactory = _ => SupportedAlgorithms.MD5,
    Recursive = true
};

var saveResultsOption = new Option<bool>("--save-results")
{
    Description = "Save the results of validation to a json file",
    Aliases = { "-sr", "-save" },
    Arity = ArgumentArity.ZeroOrOne,
    DefaultValueFactory = _ => false
};

var noStatsOption = new Option<bool>("--no-stats")
{
    Description = "Don't show stats at the end of validation",
    Aliases = { "-ns" },
    Arity = ArgumentArity.ZeroOrOne,
    DefaultValueFactory = _ => false
};

var progressOnlyOption = new Option<bool>("--progress-only")
{
    Description = "Show only the progress of hashing. Do not output the hash info the screen",
    Aliases = { "-po" },
    Arity = ArgumentArity.ZeroOrOne,
    DefaultValueFactory = _ => false,
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
    Arguments = { pathArgument },
    Aliases = { "c" },
};

var validateCommand = new Command("validate")
{
    Arguments = { pathArgument },
    Aliases = { "v" },
    Options = { saveResultsOption, noStatsOption }
};
        
// command actions
createCommand.SetAction(async parseResult =>
{
    var pathArg = parseResult.GetValue(pathArgument);
    var algorithm = parseResult.GetValue(algorithmOption);
    var progressOnly = parseResult.GetValue(progressOnlyOption);
    
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

            if (!progressOnly)
            {
                AnsiConsole.MarkupLine($"[yellow]{hash.EscapeMarkup()}[/] [gray]::[/] [yellow]{relativeFilePath.EscapeMarkup()}[/]");
            }
            
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
        
validateCommand.SetAction(async parseResult =>
{
    var pathArg = parseResult.GetValue(pathArgument);
    var algorithm = parseResult.GetValue(algorithmOption);
    var noStats = parseResult.GetValue(noStatsOption);
    var saveResults = parseResult.GetValue(saveResultsOption);
    var progressOnly = parseResult.GetValue(progressOnlyOption);
    
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

    var hashDataContents = File.ReadAllLines(hashFile.FullName);
    var hashDataList = hashDataContents.Select(line => new HashData(line)).ToList();

    // don't include the currently running program AND hash file in the event current directory is selected
    var programFile = new FileInfo(Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location);
    var fileList = FileHelper.GetFilesToCheck(dirInfo, [programFile, hashFile]);
    
    var cancelTokenSource = new CancellationTokenSource();
    
    var results = new FileValidationResults();
    
    Color GetValidationColor(ValidationResult result) => result switch
    {
        ValidationResult.Match => Color.Green,
        ValidationResult.Mismatch => Color.Red,
        ValidationResult.Missing => Color.Yellow,
        ValidationResult.Unexpected => Color.Blue,
        _ => Color.Aqua
    };

    void CollectAndOutputHashInfo(string expectedHash, string actualHash, string relativePath, ValidationResult validationResult)
    {
        var color = GetValidationColor(validationResult);

        if (saveResults)
        {
            results.AddAndCount(new FileCheckResult(expectedHash, actualHash, relativePath, validationResult));
        }
        else
        {
            results.CountOnly(validationResult);
        }

        if (progressOnly) 
            return;
                
        var resultKind = new string(' ', 11).Insert(0, validationResult.ToString());
        AnsiConsole.MarkupLine(
            $"[{color}]{resultKind.EscapeMarkup()}[/][gray]|[/] [{color}]{actualHash.EscapeMarkup()}[/] [gray]::[/] [{color}]{relativePath.EscapeMarkup()}[/]");
    }

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
            
            var foundHash = hashDataList.FirstOrDefault(x => x.RelativePath == relativeFilePath);
            
            if (foundHash == null)
            {
                // unexpected file
                CollectAndOutputHashInfo("n/a", hash, relativeFilePath, ValidationResult.Unexpected);
            }
            else if (foundHash.Hash == hash)
            {
                // match hash
                CollectAndOutputHashInfo(foundHash.Hash, hash, relativeFilePath, ValidationResult.Match);
                foundHash.Found = true;
            }
            else
            {
                // mismatch hash
                CollectAndOutputHashInfo(foundHash.Hash, hash, relativeFilePath, ValidationResult.Mismatch);
                foundHash.Found = true;
            }

            task.Increment(1);
            task.Description = $"Validating files ( {task.Value} / {task.MaxValue} )";
            ctx.Refresh();
        });
    });

    if (hashDataList.Count > 0)
    {
        foreach (var hashData in hashDataList.Where(x => !x.Found))
        {
            // missing files
            CollectAndOutputHashInfo(hashData.Hash, "n/a", hashData.RelativePath, ValidationResult.Missing);
        }
    }

    AnsiConsole.Write(new Rule {Style =  new Style().Foreground(Color.Gray)});
    
    AnsiConsole.MarkupLine(results.TotalMatch == fileList.Count
        ? "[green]All files are OK[/]"
        : "[red]Some files failed validation[/]");

    if (!noStats)
    {
        // output stats
        AnsiConsole.WriteLine();
        var chart = new BarChart()
            .AddItem("Match", results.TotalMatch, Color.Green)
            .AddItem("Mismatch", results.TotalMisMatch, Color.Red)
            .AddItem("Missing", results.TotalMissing, Color.Yellow)
            .AddItem("Unexpected", results.TotalUnexpected, Color.Blue);

        chart.Width = 100;
        
        AnsiConsole.Write(chart);
    }

    if (saveResults)
    {
        // save results to file
        var resultsFile = new FileInfo(Path.Join(pathArg, $"{dirInfo.Name.Replace(" ", "_")}-{algorithm}-results-{DateTimeOffset.Now.ToUnixTimeSeconds()}.json"));
        
        AnsiConsole.MarkupLine(await results.SaveBagToJsonAsync(resultsFile)
            ? $"[green]results saved to[/] [blue]{resultsFile.FullName.EscapeMarkup()}[/]" 
            : "[red]failed to save results file[/]");
    }
});

// root command
var rootCommand = new RootCommand("A simple, portable, directory hashing tool")
{
    Options = {  algorithmOption, progressOnlyOption },
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