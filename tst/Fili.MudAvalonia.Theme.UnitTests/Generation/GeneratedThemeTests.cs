using Xunit;

namespace Fili.MudAvalonia.Theme.UnitTests.Generation;

/// <summary>
/// The files <see cref="ButtonThemeGenerator"/> owns must be exactly what it writes.
///
/// <para>
/// <b>When this fails</b>, either a generated file was edited by hand - make the change in the
/// generator instead - or the generator changed and the files were not rewritten. Rewrite them:
/// <code>$env:FILI_REGENERATE = "1"; dotnet test tst/Fili.MudAvalonia.Theme.UnitTests --filter-class "*GeneratedThemeTests"</code>
/// The rewrite lands in the source tree, so the result arrives as a reviewable diff.
/// </para>
/// </summary>
public class GeneratedThemeTests
{
    private const string RegenerateVariable = "FILI_REGENERATE";

    [Fact]
    public void GeneratedFilesMatchTheGenerator()
    {
        var themeDirectory = ThemeDirectory();
        var regenerate = Environment.GetEnvironmentVariable(RegenerateVariable) == "1";
        var stale = new List<string>();

        foreach (var (relativePath, content) in ButtonThemeGenerator.Generate(themeDirectory))
        {
            var path = Path.Combine(themeDirectory, relativePath);
            var current = ButtonThemeGenerator.Normalise(File.ReadAllText(path));

            if (current == content)
            {
                continue;
            }

            if (regenerate)
            {
                File.WriteAllText(path, content);
            }
            else
            {
                stale.Add(relativePath);
            }
        }

        Assert.True(
            stale.Count == 0,
            $"Out of date with ButtonThemeGenerator: {string.Join(", ", stale)}. Change the generator, "
            + $"not the file, then rewrite them with {RegenerateVariable}=1 (see this test's summary).");
    }

    /// <summary>
    /// The theme project's source directory, found by walking up from the test binary to the
    /// solution - the generated files have to be read and written in the SOURCE tree.
    /// </summary>
    public static string ThemeDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Fili.MudAvalonia.Theme.sln")))
            {
                return Path.Combine(directory.FullName, "src", "Fili.MudAvalonia.Theme");
            }
        }

        throw new InvalidOperationException("Could not find Fili.MudAvalonia.Theme.sln above the test binary.");
    }
}
