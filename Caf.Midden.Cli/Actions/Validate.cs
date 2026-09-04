using Caf.Midden.Core.Services;
using Caf.Midden.Core.Services.Metadata;
using Caf.Midden.Core.Services.Validation;
using System.CommandLine;
using System.Text.Json;

// Aliased with distinct names for the same reason as in MetadataValidator: the sibling namespaces
// Caf.Midden.Core.Services.Metadata and Caf.Midden.Core.Services.Configuration win simple-name
// resolution against the model types.
using AppConfiguration = Caf.Midden.Core.Models.v0_2.Configuration;
using DatasetMetadata = Caf.Midden.Core.Models.v0_2.Metadata;

namespace Caf.Midden.Cli.Actions;

/// <summary>
/// Checks <c>.midden</c> and <c>DESCRIPTION.md</c> files against the same validators the editor
/// uses, without building a catalog.
/// </summary>
/// <remarks>
/// <para>
/// This exists because <c>collate</c> silently skips metadata it cannot parse, so a typo in a
/// single file shows up only as a dataset quietly missing from the published catalog. Running the
/// rules ahead of the crawl turns that into an explicit, attributable failure.
/// </para>
/// <para>
/// The validators come from <c>Caf.Midden.Core</c>, so a file this command accepts is exactly a
/// file the editor would let a researcher download. If the two ever disagree, that is a bug in one
/// shared validator rather than a difference of opinion between two implementations.
/// </para>
/// </remarks>
public static class ValidateCommand
{
    /// <summary>Every checked file was clean.</summary>
    private const int ExitClean = 0;

    /// <summary>At least one file had a blocking issue.</summary>
    private const int ExitValidationFailed = 1;

    /// <summary>
    /// The command could not do its job: a path did not exist, or a file could not be read.
    /// Kept distinct from <see cref="ExitValidationFailed"/> so CI can tell "your metadata is
    /// wrong" apart from "the check never ran".
    /// </summary>
    private const int ExitUsageError = 2;

    public static Command Create()
    {
        var pathsArgument = new Argument<string[]>("paths")
        {
            Description = "Files or directories to validate. Directories are searched recursively for '.midden' and 'DESCRIPTION.md' files.",
            Arity = ArgumentArity.OneOrMore,
        };

        var appConfigOption = new Option<string?>("--app-config", ["-a"])
        {
            Description = "Path to the Midden app configuration (the file the Configuration editor produces). Supplies the controlled vocabularies used for zone, project, and tag checks. Without it, those checks are skipped.",
        };

        var warningsAsErrorsOption = new Option<bool>("--warnings-as-errors", ["-w"])
        {
            Description = "Treat quality warnings as failures. Useful for a repository that has agreed to a documentation standard.",
        };

        var quietOption = new Option<bool>("--quiet", ["-q"])
        {
            Description = "Only print files that have issues.",
        };

        var command = new Command("validate", "Check '.midden' and 'DESCRIPTION.md' files against the same rules the editor enforces.");
        command.Add(pathsArgument);
        command.Add(appConfigOption);
        command.Add(warningsAsErrorsOption);
        command.Add(quietOption);
        command.SetAction(parseResult => HandleValidate(
            parseResult.GetValue(pathsArgument) ?? [],
            parseResult.GetValue(appConfigOption),
            parseResult.GetValue(warningsAsErrorsOption),
            parseResult.GetValue(quietOption)));

        return command;
    }

    /// <summary>
    /// Runs the validators over every resolved file.
    /// </summary>
    /// <remarks>
    /// Exposed to the test project (rather than being private) so path expansion, exit codes, and
    /// the <c>--warnings-as-errors</c> promotion can be exercised without spawning a process.
    /// </remarks>
    internal static int HandleValidate(
        IReadOnlyList<string> paths,
        string? appConfigPath,
        bool warningsAsErrors,
        bool quiet,
        TextWriter? output = null,
        TextWriter? error = null)
    {
        output ??= Console.Out;
        error ??= Console.Error;

        AppConfiguration? appConfiguration = null;

        if (!string.IsNullOrWhiteSpace(appConfigPath))
        {
            if (!TryReadAppConfiguration(appConfigPath, error, out appConfiguration))
            {
                return ExitUsageError;
            }
        }

        if (!TryResolveFiles(paths, error, out var files))
        {
            return ExitUsageError;
        }

        if (files.Count == 0)
        {
            error.WriteLine("No '.midden' or 'DESCRIPTION.md' files were found at the given path(s).");
            return ExitUsageError;
        }

        var metadataValidator = new MetadataValidator();
        var projectValidator = new ProjectValidator();
        var metadataParser = new MetadataParser(new MetadataConverter());
        var projectReader = new ProjectReader(new ProjectParser());

        var filesWithErrors = 0;
        var filesWithWarnings = 0;
        var unreadable = 0;

        foreach (var file in files)
        {
            ValidationResult result;

            try
            {
                result = IsProjectFile(file)
                    ? ValidateProject(file, projectReader, projectValidator, appConfiguration)
                    : ValidateMetadata(file, metadataParser, metadataValidator, appConfiguration);
            }
            catch (Exception exception)
            {
                // A file that cannot even be parsed is exactly the case collate hides, so it is
                // reported per-file and the run continues to the rest.
                error.WriteLine($"{file}: could not be read as a Midden file. {exception.Message}");
                unreadable++;
                continue;
            }

            var errors = result.Errors.ToList();
            var warnings = result.Warnings.ToList();

            if (errors.Count > 0)
            {
                filesWithErrors++;
            }

            if (warnings.Count > 0)
            {
                filesWithWarnings++;
            }

            WriteFileReport(output, file, errors, warnings, quiet);
        }

        var failed = filesWithErrors > 0
            || unreadable > 0
            || (warningsAsErrors && filesWithWarnings > 0);

        output.WriteLine(
            $"Summary: checked {files.Count} file(s). "
            + $"{filesWithErrors} with errors, {filesWithWarnings} with warnings, {unreadable} unreadable.");

        if (warningsAsErrors && filesWithWarnings > 0 && filesWithErrors == 0 && unreadable == 0)
        {
            output.WriteLine("Failing because --warnings-as-errors was requested.");
        }

        return failed ? ExitValidationFailed : ExitClean;
    }

    private static void WriteFileReport(
        TextWriter output,
        string file,
        IReadOnlyList<ValidationIssue> errors,
        IReadOnlyList<ValidationIssue> warnings,
        bool quiet)
    {
        if (errors.Count == 0 && warnings.Count == 0)
        {
            if (!quiet)
            {
                output.WriteLine($"{file}: OK");
            }

            return;
        }

        output.WriteLine(file);

        foreach (var issue in errors)
        {
            WriteIssue(output, "error", issue);
        }

        foreach (var issue in warnings)
        {
            WriteIssue(output, "warning", issue);
        }
    }

    // The path is included because it is the same string the editor uses to locate the offending
    // control, so an operator can tell a researcher exactly which field to open.
    private static void WriteIssue(TextWriter output, string label, ValidationIssue issue)
    {
        output.WriteLine($"  {label}: {issue.Path}: {issue.Message}");

        if (!string.IsNullOrWhiteSpace(issue.Hint))
        {
            output.WriteLine($"    {issue.Hint}");
        }
    }

    private static ValidationResult ValidateMetadata(
        string file,
        MetadataParser parser,
        MetadataValidator validator,
        AppConfiguration? appConfiguration)
    {
        DatasetMetadata metadata = parser.Parse(File.ReadAllText(file));

        return validator.Validate(metadata, appConfiguration);
    }

    private static ValidationResult ValidateProject(
        string file,
        ProjectReader reader,
        ProjectValidator validator,
        AppConfiguration? appConfiguration)
    {
        var project = reader.Read(File.ReadAllText(file))
            ?? throw new InvalidDataException("The file does not contain a valid Midden project front matter block.");

        return validator.Validate(project, appConfiguration);
    }

    private static bool TryReadAppConfiguration(
        string appConfigPath,
        TextWriter error,
        out AppConfiguration? appConfiguration)
    {
        appConfiguration = null;

        try
        {
            appConfiguration = Caf.Midden.Core.Services.Configuration.AppConfigurationParser.Parse(
                File.ReadAllText(appConfigPath));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            error.WriteLine($"Unable to read the app configuration at '{appConfigPath}': {exception.Message}");
            return false;
        }

        if (appConfiguration is null)
        {
            error.WriteLine($"The app configuration at '{appConfigPath}' is empty.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Expands the given paths into a de-duplicated, ordered list of files to check.
    /// </summary>
    private static bool TryResolveFiles(
        IReadOnlyList<string> paths,
        TextWriter error,
        out List<string> files)
    {
        // Overlapping arguments (a directory and a file inside it) must not validate the same
        // file twice and inflate the counts in the summary.
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        files = [];

        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                foreach (var found in EnumerateMiddenFiles(path))
                {
                    if (seen.Add(Path.GetFullPath(found)))
                    {
                        files.Add(found);
                    }
                }

                continue;
            }

            if (File.Exists(path))
            {
                // An explicitly named file is validated whatever it is called; the extension
                // filter exists to make directory recursion useful, not to second-guess a
                // deliberate argument.
                if (seen.Add(Path.GetFullPath(path)))
                {
                    files.Add(path);
                }

                continue;
            }

            error.WriteLine($"No file or directory exists at '{path}'.");
            return false;
        }

        return true;
    }

    private static IEnumerable<string> EnumerateMiddenFiles(string directory) =>
        Directory
            .EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Where(IsMiddenFile)
            .OrderBy(file => file, StringComparer.OrdinalIgnoreCase);

    private static bool IsMiddenFile(string file) =>
        IsProjectFile(file)
        || Path.GetExtension(file).Equals(
            MiddenFileConventions.MiddenFileExtension,
            StringComparison.OrdinalIgnoreCase);

    private static bool IsProjectFile(string file) =>
        Path.GetFileName(file).Equals(
            MiddenFileConventions.MippenFileSearchTerm,
            StringComparison.OrdinalIgnoreCase);
}
