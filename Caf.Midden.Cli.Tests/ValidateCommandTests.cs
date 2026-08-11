using Caf.Midden.Cli.Actions;

namespace Caf.Midden.Cli.Tests;

public class ValidateCommandTests : IDisposable
{
    private readonly DirectoryInfo directory = Directory.CreateTempSubdirectory();
    private readonly StringWriter output = new();
    private readonly StringWriter error = new();

    public void Dispose()
    {
        directory.Delete(recursive: true);
        GC.SuppressFinalize(this);
    }

    private int Run(params string[] args) =>
        ValidateCommand.HandleValidate(
            args,
            appConfigPath: null,
            warningsAsErrors: false,
            quiet: false,
            output,
            error);

    private string WriteFile(string relativePath, string contents)
    {
        var path = Path.Combine(directory.FullName, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return path;
    }

    private string WriteValidMetadata(string relativePath = "Good.midden") =>
        WriteFile(relativePath, ValidMetadataJson("CookEastMet"));

    /// <summary>
    /// A dataset that passes every rule, used as the baseline the failure cases deviate from.
    /// </summary>
    /// <remarks>
    /// A raw string literal is used so the escaped quotes inside the embedded GeoJSON geometry
    /// stay readable rather than becoming a wall of backslashes.
    /// </remarks>
    private static string ValidMetadataJson(string name) => $$"""
        {
          "file": { "schema-version": "v0.2", "creation-date": "2020-07-29" },
          "dataset": {
            "zone": "Raw",
            "project": "CafMeteorologyECTower",
            "name": "{{name}}",
            "description": "Meteorology datatable from an eddy covariance flux tower on Cook East field.",
            "tags": [ "meteorology" ],
            "contacts": [
              { "name": "Bryan Carlson", "email": "bryan.carlson@usda.gov", "role": "Manager" }
            ],
            "temporalExtent": "2011-01-01/2019-10-30",
            "geometry": "{\"type\":\"Point\",\"coordinates\":[-117.08205,46.78152]}",
            "spatialRepeats": 1,
            "structure": "Multiple",
            "variables": [
              { "name": "TIMESTAMP", "description": "Timestamp of the record.", "units": "Unitless" }
            ]
          }
        }
        """;

    /// <summary>
    /// Structurally sound but low quality: missing tags is a warning, never an error.
    /// </summary>
    private static string MetadataJsonWithoutTags() =>
        ValidMetadataJson("CookEastMet").Replace("""
            "tags": [ "meteorology" ],
        """.Trim(), """
            "tags": [],
        """.Trim());

    [Fact]
    public void Validate_CleanFile_SucceedsAndReportsOk()
    {
        var file = WriteValidMetadata();

        var exitCode = Run(file);

        Assert.Equal(0, exitCode);
        Assert.Contains("OK", output.ToString());
    }

    [Fact]
    public void Validate_FileWithBlockingIssue_FailsAndNamesTheField()
    {
        // A missing dataset name is the case that silently drops a dataset from the catalog.
        var file = WriteFile("Bad.midden", ValidMetadataJson(""));

        var exitCode = Run(file);

        Assert.Equal(1, exitCode);

        var report = output.ToString();
        Assert.Contains("Bad.midden", report);
        Assert.Contains("error:", report);
        Assert.Contains("dataset.name", report);
    }

    [Fact]
    public void Validate_UnparseableFile_IsReportedRatherThanSkipped()
    {
        // collate quietly continues past these, which is the gap this command closes.
        var file = WriteFile("Broken.midden", "{ this is not valid json");

        var exitCode = Run(file);

        Assert.Equal(1, exitCode);
        Assert.Contains("Broken.midden", error.ToString());
    }

    [Fact]
    public void Validate_OneBadFile_StillChecksTheRest()
    {
        WriteFile("A.midden", "{ not json");
        WriteValidMetadata("B.midden");

        Run(directory.FullName);

        // The summary proves the run did not abort at the first failure.
        Assert.Contains("checked 2 file(s)", output.ToString());
    }

    [Fact]
    public void Validate_Directory_FindsMiddenAndDescriptionFilesRecursively()
    {
        WriteValidMetadata(Path.Combine("Raw", "Nested.midden"));
        WriteFile(Path.Combine("Raw", "DESCRIPTION.md"), """
            ---
            project: "RawProject"
            status: "Active"
            ---
            # Raw Title
            """);
        WriteFile(Path.Combine("Raw", "notes.txt"), "ignored");

        Run(directory.FullName);

        var report = output.ToString();
        Assert.Contains("checked 2 file(s)", report);
        Assert.DoesNotContain("notes.txt", report);
    }

    [Fact]
    public void Validate_OverlappingPaths_DoesNotCheckTheSameFileTwice()
    {
        var file = WriteValidMetadata();

        Run(directory.FullName, file);

        Assert.Contains("checked 1 file(s)", output.ToString());
    }

    [Fact]
    public void Validate_MissingPath_ReportsUsageErrorDistinctFromValidationFailure()
    {
        var exitCode = Run(Path.Combine(directory.FullName, "nope"));

        // 2 rather than 1, so CI can tell "the check never ran" from "the metadata is wrong".
        Assert.Equal(2, exitCode);
        Assert.Contains("No file or directory exists", error.ToString());
    }

    [Fact]
    public void Validate_DirectoryWithNoMiddenFiles_ReportsUsageError()
    {
        WriteFile("readme.txt", "nothing to validate here");

        var exitCode = Run(directory.FullName);

        Assert.Equal(2, exitCode);
        Assert.Contains("No '.midden' or 'DESCRIPTION.md' files", error.ToString());
    }

    [Fact]
    public void Validate_ExplicitlyNamedFile_IsCheckedRegardlessOfExtension()
    {
        // The extension filter exists to make directory recursion useful, not to override a
        // deliberate argument.
        var file = WriteFile("renamed.json", ValidMetadataJson("CookEastMet"));

        var exitCode = Run(file);

        Assert.Equal(0, exitCode);
        Assert.Contains("renamed.json", output.ToString());
    }

    [Fact]
    public void Validate_WarningsAlone_SucceedByDefault()
    {
        var file = WriteFile("Warn.midden", MetadataJsonWithoutTags());

        var exitCode = ValidateCommand.HandleValidate(
            [file],
            appConfigPath: null,
            warningsAsErrors: false,
            quiet: false,
            output,
            error);

        // A researcher can knowingly publish a low-quality dataset; only errors block.
        Assert.Equal(0, exitCode);
        Assert.Contains("warning:", output.ToString());
    }

    [Fact]
    public void Validate_WarningsAsErrors_FailsOnWarningsAlone()
    {
        var file = WriteFile("Warn.midden", MetadataJsonWithoutTags());

        var exitCode = ValidateCommand.HandleValidate(
            [file],
            appConfigPath: null,
            warningsAsErrors: true,
            quiet: false,
            output,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains("--warnings-as-errors", output.ToString());
    }

    [Fact]
    public void Validate_QuietMode_StillReportsFilesWithIssues()
    {
        WriteValidMetadata("Clean.midden");
        WriteFile("Warn.midden", MetadataJsonWithoutTags());

        ValidateCommand.HandleValidate(
            [directory.FullName],
            appConfigPath: null,
            warningsAsErrors: false,
            quiet: true,
            output,
            error);

        var report = output.ToString();
        Assert.DoesNotContain("Clean.midden", report);
        Assert.Contains("Warn.midden", report);
    }

    [Fact]
    public void Validate_QuietMode_OmitsCleanFiles()
    {
        var file = WriteValidMetadata();

        ValidateCommand.HandleValidate(
            [file],
            appConfigPath: null,
            warningsAsErrors: false,
            quiet: true,
            output,
            error);

        Assert.DoesNotContain("OK", output.ToString());
    }

    [Fact]
    public void Validate_MissingAppConfig_ReportsUsageError()
    {
        var file = WriteValidMetadata();

        var exitCode = ValidateCommand.HandleValidate(
            [file],
            appConfigPath: Path.Combine(directory.FullName, "missing-config.json"),
            warningsAsErrors: false,
            quiet: false,
            output,
            error);

        Assert.Equal(2, exitCode);
        Assert.Contains("app configuration", error.ToString());
    }
}
