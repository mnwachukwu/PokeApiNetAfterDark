using System.Text.Json;
using PokeApiNetAfterDark.Models;
using Xunit;

namespace PokeApiNetAfterDark.Tests;

/// <summary>What the client does with a data directory: reads a resource out of it, caches what it
/// read, and says so plainly when a resource is not there.
///
/// <para>Each test builds its own directory holding only the resources it needs, so these run
/// anywhere and need none of the api-data corpus. Reading the real thing is
/// <see cref="IntegrationTests"/>.</para></summary>
public class PokeApiNetAfterDarkClientTests : IDisposable
{
    private readonly string _dataDirectory =
        Path.Combine(Path.GetTempPath(), "pokeapinet-tests", Guid.NewGuid().ToString("n"));

    public void Dispose()
    {
        if (Directory.Exists(_dataDirectory))
        {
            Directory.Delete(_dataDirectory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>Writes one resource where the client looks for it: &lt;endpoint&gt;/&lt;key&gt;/index.json.</summary>
    private string WriteResource(string endpoint, string key, string json)
    {
        var folder = Path.Combine(_dataDirectory, endpoint, key);
        Directory.CreateDirectory(folder);

        var path = Path.Combine(folder, "index.json");
        File.WriteAllText(path, json);

        return path;
    }

    private PokeApiNetAfterDarkClient CreateSut() => new(_dataDirectory);

    private const string CheriBerry = """{"id":1,"name":"cheri","growth_time":3,"max_harvest":5,"size":20}""";

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetResourceAsync_ById_ReadsFromTheDataDirectory()
    {
        WriteResource("berry", "1", CheriBerry);
        using var sut = CreateSut();

        var berry = await sut.GetResourceAsync<Berry>(1);

        Assert.Equal(1, berry.Id);
        Assert.Equal("cheri", berry.Name);
        Assert.Equal(20, berry.Size);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetResourceAsync_ByName_ReadsFromTheDataDirectory()
    {
        WriteResource("berry", "cheri", CheriBerry);
        using var sut = CreateSut();

        var berry = await sut.GetResourceAsync<Berry>("cheri");

        Assert.Equal("cheri", berry.Name);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetResourceAsync_ByName_IsCaseInsensitive()
    {
        WriteResource("berry", "cheri", CheriBerry);
        using var sut = CreateSut();

        var berry = await sut.GetResourceAsync<Berry>("CHERI");

        Assert.Equal("cheri", berry.Name);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetResourceAsync_SecondCall_IsServedFromCache()
    {
        var path = WriteResource("berry", "1", CheriBerry);
        using var sut = CreateSut();

        await sut.GetResourceAsync<Berry>(1);

        // With the file gone, a second read that still succeeds can only have come from the cache.
        File.Delete(path);
        var cached = await sut.GetResourceAsync<Berry>(1);

        Assert.Equal("cheri", cached.Name);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ClearCache_SendsTheNextCallBackToDisk()
    {
        var path = WriteResource("berry", "1", CheriBerry);
        using var sut = CreateSut();

        await sut.GetResourceAsync<Berry>(1);
        sut.ClearCache();
        File.Delete(path);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetResourceAsync<Berry>(1));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetResourceAsync_Missing_ThrowsNamingTheDirectory()
    {
        using var sut = CreateSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetResourceAsync<Berry>(999));

        Assert.Contains("berry/999", ex.Message);
        Assert.Contains(_dataDirectory, ex.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_RejectsAnEmptyDataDirectory()
    {
        Assert.Throws<ArgumentException>(() => new PokeApiNetAfterDarkClient(string.Empty));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DataDirectory_DefaultsToTheConventionalFolder()
    {
        using var sut = new PokeApiNetAfterDarkClient();

        Assert.Equal(PokeApiNetAfterDarkClient.DataDirectoryName, sut.DataDirectory);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetResourceAsync_ReadsShowdownSprites()
    {
        var pokemon = JsonSerializer.Serialize(new
        {
            id = 25,
            name = "pikachu",
            sprites = new
            {
                other = new
                {
                    showdown = new
                    {
                        front_default = "https://example.invalid/showdown/25.gif",
                        back_default = "https://example.invalid/showdown/back/25.gif"
                    }
                }
            }
        });

        WriteResource("pokemon", "25", pokemon);
        using var sut = CreateSut();

        var result = await sut.GetResourceAsync<Models.Pokemon>(25);

        Assert.Equal("https://example.invalid/showdown/25.gif", result.Sprites.Other.Showdown.FrontDefault);
        Assert.Equal("https://example.invalid/showdown/back/25.gif", result.Sprites.Other.Showdown.BackDefault);
    }
}
