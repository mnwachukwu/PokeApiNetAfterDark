using Xunit;

namespace PokeApiNetAfterDark.Tests;

/// <summary>Finds the api-data copy the integration tests read from.
///
/// <para>The corpus is hundreds of megabytes, so it is not committed here. Point
/// <c>POKEAPI_DATA</c> at a checkout of PokeAPI/api-data's <c>data/api/v2</c> folder, or leave a
/// <c>PokeAPI-Data</c> folder beside the test binaries.</para></summary>
internal static class TestData
{
    public const string EnvironmentVariable = "POKEAPI_DATA";

    /// <summary>The data folder, or null when there is none to read.</summary>
    public static string? Directory { get; } = Resolve();

    public static bool Available => Directory is not null;

    /// <summary>A client reading the corpus. Only call this from a test that is skipped when
    /// <see cref="Available"/> is false.</summary>
    public static PokeApiNetAfterDarkClient CreateClient() => new(Directory!);

    private static string? Resolve()
    {
        var configured = Environment.GetEnvironmentVariable(EnvironmentVariable);

        if (!string.IsNullOrWhiteSpace(configured) && System.IO.Directory.Exists(configured))
        {
            return configured;
        }

        var beside = Path.Combine(AppContext.BaseDirectory, PokeApiNetAfterDarkClient.DataDirectoryName);

        return System.IO.Directory.Exists(beside) ? beside : null;
    }
}

/// <summary>A <see cref="FactAttribute"/> that skips itself when there is no data to read, so a
/// checkout without the corpus reports skipped rather than failing or passing on nothing.</summary>
public sealed class DataFactAttribute : FactAttribute
{
    public DataFactAttribute()
    {
        if (!TestData.Available)
        {
            Skip = $"No PokéAPI data found. Set {TestData.EnvironmentVariable} to the api-data v2 folder to run this.";
        }
    }
}

/// <summary>The <see cref="TheoryAttribute"/> counterpart to <see cref="DataFactAttribute"/>.</summary>
public sealed class DataTheoryAttribute : TheoryAttribute
{
    public DataTheoryAttribute()
    {
        if (!TestData.Available)
        {
            Skip = $"No PokéAPI data found. Set {TestData.EnvironmentVariable} to the api-data v2 folder to run this.";
        }
    }
}
