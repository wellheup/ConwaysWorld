---
name: Blazor partial class usings
description: .cs partial class files in Blazor don't inherit _Imports.razor using directives — each file needs explicit usings.
---

## Rule
Every `.cs` partial class file under `ConwaysWorld.Blazor/Pages/` must declare its own `using` directives. The `_Imports.razor` file only affects `.razor` files.

**Why:** Blazor's `@using` and `_Imports.razor` directives are processed by the Razor compiler and injected into the generated code-behind for `.razor` files. Plain `.cs` partial class files bypass this mechanism entirely — they are compiled as standard C# with no injected usings.

**How to apply:** When creating or editing a `.cs` partial in the Blazor project that references simulation types or ASP.NET Components, add:
```csharp
using ConwaysWorld.Simulation;                 // CellType, Model, SimulationSettings, etc.
using Microsoft.AspNetCore.Components;         // ChangeEventArgs, ComponentBase, etc.
using Microsoft.AspNetCore.Components.Web;     // MouseEventArgs, KeyboardEventArgs, etc.
using Microsoft.JSInterop;                     // IJSRuntime, DotNetObjectReference, InvokeVoidAsync, etc.
```
Only include what the file actually uses to avoid unused-using warnings.
