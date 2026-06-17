---
name: Blazor WASM setup quirks
description: Environment-specific issues when scaffolding and running Blazor WASM in this Replit project
---

## .NET version
SDK is 7.0.410. Target framework must be `net7.0`, not `net8.0`.

## Scaffold problem
`dotnet new blazorwasm -o ConwaysWorld.Blazor` from workspace root (which already has a `ConwaysWorld.csproj`) does NOT create a `.csproj` inside the output directory — it only drops Pages/ and wwwroot/. Write `ConwaysWorld.Blazor.csproj` manually.

## Run command
`dotnet run --project ConwaysWorld.Blazor/ConwaysWorld.Blazor.csproj --urls http://0.0.0.0:5000`

**Why:** Replit's preview proxy requires port 5000; default Blazor dev server picks a random port.

## Package versions (net7.0)
```
Microsoft.AspNetCore.Components.WebAssembly          7.0.20
Microsoft.AspNetCore.Components.WebAssembly.DevServer 7.0.20 (PrivateAssets=all)
```

## Shared namespace in _Imports.razor
Must add `@using ConwaysWorld.Blazor.Shared` explicitly — the `Shared/` subdirectory namespace isn't auto-imported.
