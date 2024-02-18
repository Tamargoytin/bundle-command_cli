using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;

static void RunCommand(string command)
{
    Console.WriteLine(command);
    try
    {
        using (Process process = new Process())
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "fib.exe",
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.StartInfo = startInfo;
            process.Start();

            using (StreamWriter sw = process.StandardInput)
            {
                if (sw.BaseStream.CanWrite)
                {
                    sw.WriteLine(command);
                }
            }

            process.WaitForExit();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error running command: {ex.Message}");
    }
}
static void CreateResponseFile()
{
    // Add logic to create the response file based on user input
    string[] options = { "--language [csharp, css, vb,html, docx, pwsh, sql,all]", "--note", "--sort", "--remove-empty-lines", "--author" };
    Console.Write($"Enter the pathwhere you want the response file to be saved: ");
    string output = Console.ReadLine();

    using (StreamWriter sw = new StreamWriter(output))
    {
        //sw.WriteLine("fib bundle");
        foreach (var option in options)
        {
            Console.Write($"Enter value for {option}: ");
            string value = null;
            value = Console.ReadLine();
            if (!string.IsNullOrEmpty(value))
                sw.WriteLine($"{option.Split(" ")[0]} {value}");
        }
    }

    Console.WriteLine($"Response file created successfully at: {output}");
    string[] responseLines = File.ReadAllLines(output);
    string lines = "";
    foreach (string line in responseLines)
    {
        Console.WriteLine($"Processing line: {line}");
        lines = lines + " " + line;
    }
    Console.WriteLine(lines);
    RunCommand(lines);
}
static bool ShouldIncludeFile(string filePath)
{
    string[] excludedDirectories = { "bin", "debug" };

    // Check if the file path contains any excluded directory
    return !excludedDirectories.Any(dir => filePath.Contains(dir, StringComparison.OrdinalIgnoreCase));
}
static void BundleCode(string output, List<string> languages,  bool note, string sort, bool removeEmptyLines, string author)
{
    string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(),"*.*", SearchOption.AllDirectories);
    string[] lan = languages.ToString().Split(' ');

    List<string> filesShouldInclude = new List<string>();
    List<string> selectedFiles = new List<string>();

    foreach (var filePath in files)
        {
            if (ShouldIncludeFile(filePath))
            {
                 filesShouldInclude.Add(filePath);
            }
        }
    foreach (var item in filesShouldInclude)
    {
        Console.WriteLine(item);
    };
    if (languages.Contains("all"))
    {
        selectedFiles = filesShouldInclude;
    }
    else
    {
        foreach (var language in languages)
        {
            selectedFiles.AddRange(files.Where(file => file.EndsWith($".{language}", StringComparison.OrdinalIgnoreCase)));
        }
    }

    if (!selectedFiles.Any())
    {
        Console.WriteLine("No files found for the specified languages.");
        return;
    }

    if (removeEmptyLines)
    {
        selectedFiles = selectedFiles.Select(RemoveEmptyLinesFromFile).ToList();
    }

    if (sort.ToLower() == "type")
    {
        selectedFiles.Sort((a, b) => Path.GetExtension(a).CompareTo(Path.GetExtension(b)));
    }
    else
    {
        selectedFiles.Sort();
    }

    string bundleContent = string.Join(Environment.NewLine, selectedFiles.Select(File.ReadAllText));
    Console.WriteLine(bundleContent);
    if (note)
    {
        bundleContent = $"// Source Note: {Directory.GetCurrentDirectory()}\n\n{bundleContent}";
    }
    if (author != null)
    {
        bundleContent = $"// Source Note: {author} - {DateTime.Now}\n\n{bundleContent}";
    }
    try
    {
        File.WriteAllText(output, bundleContent);
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine("Error:File path is invalid");
    }
    Console.WriteLine("Bundle created successfully!");
}
static string RemoveEmptyLinesFromFile(string filePath)
{
    string[] lines = File.ReadAllLines(filePath);
    string content = string.Join(Environment.NewLine, lines.Where(line => !string.IsNullOrWhiteSpace(line)));
    File.WriteAllText(filePath, content);
    return filePath;
}

var bundleCommand = new Command("bundle", "bundle code files to a single file");
var rspCommand = new Command("create-rsp", "creating a rsp file");

var languagesOptions = new Option<List<string>>(
    "--language",
    "An option that must be one or more or juat all of the values of a static list.")
{ IsRequired = true }
    .FromAmong("csharp", "css", "vb", "pwsh", "sql","html","docx", "all");
languagesOptions.AddAlias("-l");
languagesOptions.AllowMultipleArgumentsPerToken = true;

var outputOption = new Option<string>("--output", "save location");
outputOption.AddAlias("-o");
outputOption.SetDefaultValue("bundleFile.txt");


var noteOption = new Option<bool>("--note", "Include source code note.");
noteOption.AddAlias("-n");
noteOption.SetDefaultValue(false);


var sortOption = new Option<string>("--sort", "Sort order (name or type).");
sortOption.AddAlias("-s");
sortOption.SetDefaultValue("name");

var removeOption = new Option<bool>("--remove-empty-lines", "Remove empty lines from source code.");
removeOption.AddAlias("-r");
removeOption.SetDefaultValue(false);


var authorOption = new Option<string>("--author", "Author's name.");
authorOption.AddAlias("-a");

bundleCommand.AddOption(languagesOptions);
bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(removeOption);
bundleCommand.AddOption(authorOption);


bundleCommand.SetHandler((languages, output, note, sort, removeEmptyLines, author) =>
{
    BundleCode(output, languages, note, sort, removeEmptyLines, author);
}, languagesOptions, outputOption,noteOption, sortOption, removeOption, authorOption);

rspCommand.SetHandler(() => { CreateResponseFile(); });

var rootCommand = new RootCommand("root command for file bundler cli");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(rspCommand);
rootCommand.InvokeAsync(args);



