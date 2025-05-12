# PokeApiNet: After Dark
![image](https://github.com/user-attachments/assets/03a9a26a-673d-423c-99f8-79f45c738ae7)

So if the image above isn't explanation enough, what I've done is refitted PokeApiNet to work with the data stored at PokeAPI's api-data repository. It was actually rather painless, because of the way the data on PokeAPI's repository is structured. It is essentially a static copy of their API. Armed with that information, it only took a single `File.ReadAllText()` call to start reading data from the file system instead of the API.

I thought maybe others would want to build apps with static Pokémon data, instead of relying on an internet connection, so I am sharing my changes.

This is PokeApiNet: After Dark (*After Dark meaning, in case the API is inaccessible for any reason*)

## Disclaimers
- This will add 500mb of static data to your projects. Not suited for web apps, but perfect for desktop and mobile apps.
- The features that return collections haven't been worked out yet. This will be something I can work on if there is demand for it, but currently, I do not have a use for that. This could change in the future.

## Set up
Read the other project READMEs that I have included in this README. Then clone (or download) the [api-data](https://github.com/PokeAPI/api-data) repository. What you want is located here: `https://github.com/PokeAPI/api-data/tree/master/data/api/v2`.
![image](https://github.com/user-attachments/assets/b2e3af6c-a34f-4468-ad9c-eacbc6af42cb)

Then, set up PokeApiNet, and put the contents of `https://github.com/PokeAPI/api-data/tree/master/data/api/v2` in the same directory as the assembled library, inside of a folder named `PokeAPI-Data` (you can change this, you have the source code)

Then, integrate PokeApiNet as you normally would and boom. Data is now read from the files instead of the API.

Enjoy!

### Technical Impetus

If you're deeply interested in what I've done, check this out.

This is the code as written on PokeApiNet
```cs
private async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken)
{
    using var request = new HttpRequestMessage(HttpMethod.Get, url);
    using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);

    response.EnsureSuccessStatusCode();
    return DeserializeStream<T>(await response.Content.ReadAsStreamAsync());
}
```

And this is what I did
```cs
private static async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken)
{
    return await Task.Run(() =>
    {
        const int maxAttempts = 10;
        const int delayMs = 50;

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var data = File.ReadAllText($"PokeAPI-Data\\{url}\\index.json");
                return JsonSerializer.Deserialize<T>(data,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
            }
            catch (IOException)
            {
                Thread.Sleep(delayMs); // back off and try again
            }
        }

        //Console.WriteLine($"Failed to load PokeAPI data after {maxAttempts} attempts: {filePath}");
        return default;
    }, cancellationToken);
}
```

That's pretty much it. Simple right? :)

---

# The Other READMEs

## PokeApiNet
A .Net wrapper for the Pokemon API at [https://pokeapi.co](https://pokeapi.co).

Targets .Net Standard 2.0+.

[![NuGet](https://img.shields.io/nuget/v/PokeApiNet.svg?logo=nuget)](https://www.nuget.org/packages/PokeApiNet)
[![Build Status](https://mtrdp642.visualstudio.com/PokeApiNet/_apis/build/status/mtrdp642.PokeApiNet?branchName=master)](https://mtrdp642.visualstudio.com/PokeApiNet/_build/latest?definitionId=1&branchName=master)

## Use
```cs
using PokeApiNet;

...

// instantiate client
PokeApiClient pokeClient = new PokeApiClient();

// get a resource by name
Pokemon hoOh = await pokeClient.GetResourceAsync<Pokemon>("ho-oh");

// ... or by id
Item clawFossil = await pokeClient.GetResourceAsync<Item>(100);
```

To see all the resources that are available, see the [PokeAPI docs site](https://pokeapi.co/docs/v2).

Internally, `PokeApiClient` uses an instance of the `HttpClient` class. As such, instances of `PokeApiClient` are [meant to be instantiated once and re-used throughout the life of an application.](https://docs.microsoft.com/en-us/dotnet/api/system.net.http.httpclient?view=netcore-3.1#remarks)

### Navigation URLs
PokeAPI uses navigation urls for many of the resource's properties to keep requests lightweight, but require subsequent requests in order to resolve this data. Example:
```cs
Pokemon pikachu = await pokeClient.GetResourceAsync<Pokemon>("pikachu");
```

`pikachu.Species` only has a `Name` and `Url` property. In order to load this data, an additonal request is needed; this is more of a problem when the property is a list of navigation URLs, such as the `pikachu.Moves.Move` collection.

`GetResourceAsync` includes overloads to assist with resolving these navigation properties. Example:
```cs
// to resolve a single navigation url property
PokemonSpecies species = await pokeClient.GetResourceAsync(pikachu.Species);

// to resolve a list of them
List<Move> allMoves = await pokeClient.GetResourceAsync(pikachu.Moves.Select(move => move.Move));
```

### Paging
PokeAPI supports the paging of resources, allowing users to get a list of available resources for that API. Depending on the shape of the resource data, two methods for paging are included, along with overloads to allow for the specification of the page count limit and the page offset. Example:
```cs
// get a page of data (defaults to a limit of 20)
NamedApiResourceList<Berry> firstBerryPage = await client.GetNamedResourcePageAsync<Berry>();

// to specify a certain page, use the provided overloads
NamedApiResourceList<Berry> lotsMoreBerriesPage = await client.GetNamedResourcePageAsync<Berry>(60, 2);
```

Because the `Berry` resource has a `Name` property, the `GetNamedResourcePageAsync()` method is used. For resources that do not have a `Name` property, use the `GetApiResourcePageAsync()` method. Example:
```cs
ApiResourceList<ContestEffect> contestEffectPage = await client.GetApiResourcePageAsync<ContestEffect>();
```

Regardless of which method is used, the returning object includes a `Results` collection that can be used to pull the full resource data. Example:
```cs
Berry cheri = await client.GetResourceAsync<Berry>(firstBerryPage.Results[0]);
```

Refer to the PokeAPI documention to see which resources include a `Name` property.

#### `IAsyncEnumerable` Support
Two methods expose support for `IAsyncEnumerable` to make paging through all pages super simple: `GetAllNamedResourcesAsync<T>()` and `GetAllApiResourcesAsync<T>()`. Example:

```cs
await foreach (var berryRef in pokeClient.GetAllNamedResourcesAsync<Berry>())
{
    // do something with each berry reference
}
```

### Caching
Every resource and page response is automatically cached in memory, making all subsequent requests for the same resource or page pull cached data. Example:
```cs
// this will fetch the data from the API
Pokemon mew = await pokeClient.GetResourceAsync<Pokemon>(151);

// another call to the same resource will fetch from the cache
Pokemon mewCached = await pokeClient.GetResourceAsync<Pokemon>("mew");
```

To clear the cache of data:
```cs
// clear all caches for both resources and pages
pokeClient.ClearCache();
```

Additional overloads are provided to allow for clearing the individual caches for resources and pages, as well as by type of cache.

## Build
```
dotnet build
```

## Test
The test bank includes unit tests and integration tests. Integration tests will actually ping the PokeApi server for data while the unit tests run off of mocked data.
```
dotnet test --filter "Category = Unit"
dotnet test --filter "Category = Integration"
```

---

## PokéAPI data <a href="https://pokeapi.co/api/v2/pokemon/uxie"><img src='https://veekun.com/dex/media/pokemon/global-link/480.png' height=50px/></a>

[![CircleCI](https://circleci.com/gh/PokeAPI/api-data.svg?style=shield)](https://circleci.com/gh/PokeAPI/api-data)

This repository contains:

- [data/api](data/api): a static copy of the JSON data produced by [PokeAPI](https://github.com/PokeAPI/pokeapi)
- [data/schema](data/schema): a static copy of the PokeAPI schema generated from the above data
- [updater](updater): a [Ditto][1] based bot that runs in docker and updates the data stored in this repo

### Usage

If you'd like to use the JSON for your own purposes, you can apply your own base URL using [Ditto][1]:

```sh
ditto transform --base-url='https://pokeapi.co'
```

### Updater bot

You can manually update the data if necessary. See [the updater bot](updater).
You can run the bot in docker, or read and adapt its update script yourself.

[1]: https://github.com/pokeapi/ditto
