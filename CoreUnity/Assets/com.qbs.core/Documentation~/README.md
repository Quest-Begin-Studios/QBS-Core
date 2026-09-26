# QBS Core

**Core package for Quest Begins Studios** - A comprehensive Unity development toolkit providing code generation, logging utilities, and assembly management tools.

## Features

### 🔧 Code Generation Tools

#### Enum Generator
- **Simple Enums**: Generate standard C# enumerations
- **Flag Enums**: Create bitwise flag enumerations with automatic power-of-2 values
- **Custom Values**: Define enums with specific backing values
- **Backing Type Support**: Choose from byte, sbyte, short, ushort, int, uint, long, ulong
- **Attributes**: Optional `[Flags]` and `[Serializable]` attributes
- **Flag Combinations**: Define named combinations of flags

#### String Enum Generator
- Fast string representation for enum values — powered by the Enum Utility Source Generators via `GenerateToStringFast`
- Generated at compile time; no reflection overhead

### ⚡ Enum Utility Source Generators

Compile-time code generation via Roslyn. Annotate any enum with `[EnumUtilities]` and utility methods are generated automatically on the next compile — no editor window, no runtime reflection.

**Namespace**: `QBS.SourceGenerators.GeneratorDiscoveryHelpers`

**Generation Options** (`EnumUtilsGenOptions` flags, combinable with `|`):

| Option | Generated Member | Description |
|--------|-----------------|-------------|
| `GenerateToStringFast` | `ToStringFast()` extension | Switch-based string lookup; no reflection |
| `GenerateHasFlagFast` | `HasFlagFast(T flag)` extension | Bitwise flag check; no boxing |
| `GenerateValuesArray` | `<EnumName>Utils.Values` | `ImmutableArray<T>` of all enum values |
| `All` | All of the above | All options enabled |

### 📝 Log System

#### Channels
A channel is a `LogCategory` with a dot-separated name. Core ships `Log.dll` and `Log-Editor.dll` prebuilt in `Plugins/Log`, so there is nothing to generate per project.

- **Built-in channels** (`LogChannel`): Network, AI, Physics, UI, Input, SaveLoad, Loading, Gameplay, Audio, Rendering, and the groups Core (Input, Gameplay), Presentation (UI, Rendering, Audio) and Simulation (AI, Physics, Core)
- **Package and game channels**: `static readonly` fields of a `[LogCategories]` class, named after the owner's namespace (`Acme.Inventory.Save`) so names never collide
- **Groups**: `LogCategory.Group(name, members…)`. You log to a group like a channel; it is on while any member is
- **Rules**: `LogRules` switches a name and everything under it; the longest matching rule wins. Level filtering stays global (`Log.MinimumLevel`)
- **Configuration**: `Tools > Logs > Configure Logging`, which also reports undeclared categories and names that fall under a built-in channel's

### 🔨 DLL Generation

Compile C# source files into DLLs at runtime using Roslyn compiler.

**Features**:
- Runtime and Editor assembly compilation
- Custom assembly references
- Scripting symbol support
- Comprehensive error reporting
- Automatic dependency resolution

### 🛠️ Utilities

#### Assembly Compatibility
`AssemblyCompat` wraps the assembly lookup APIs that changed in Unity 6000.4, so the same call works across every 6000.x editor.

## Installation

### Via Package Manager (Git URL)

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click `+` → `Add package from git URL`
3. Enter: `https://github.com/Quest-Begin-Studios/QBS-Core.git?path=/CoreUnity/Assets/com.qbs.core`

### Via manifest.json

Add to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.qbs.core": "https://github.com/Quest-Begin-Studios/QBS-Core.git?path=/CoreUnity/Assets/com.qbs.core#v2.0.0"
  }
}
```

## Requirements

- **Unity Version**: 6000.0 or higher
- **Dependencies**: None

Unity 6000.4 replaced `AppDomain.CurrentDomain.GetAssemblies()` and `Assembly.Location` with `UnityEngine.Assemblies.CurrentAssemblies` and `Assembly.GetLoadedAssemblyPath()`, which are required under CoreCLR. The package picks the correct API for the running editor through `AssemblyCompat`, so no consumer-side version guards are needed.

## Usage

### Enum Generation

```csharp
using QBS.Core.Editor;

// Access via Tools > QBS > Enum Generator
// Configure enum settings (name, namespace, type, backing type)
// Add enum keys, then click "Generate Enum"
// Generated source is displayed and can be copied to clipboard
```

### Log Channels

```csharp
using QBS.Core;

// Declare a package's channels once, in its own assembly.
[LogCategories]
public static class InventoryLog
{
    public static readonly LogCategory Save = LogCategory.Get("Acme.Inventory.Save");
    public static readonly LogCategory Crafting = LogCategory.Get("Acme.Inventory.Crafting");

    // Groups after their members: field initializers run in order.
    public static readonly LogCategory Gear = LogCategory.Group("Acme.Gear", Save, LogChannel.Gameplay);
}

Log.Info("Connected", channel: LogChannel.Network);   // [Info] [Network] Connected
Log.Warning("Slow save", channel: InventoryLog.Save);
Log.Info("Equipped", channel: InventoryLog.Gear);     // on while Save or Gameplay is

LogRules.Set("Acme.Inventory", false);                // the whole branch off...
LogRules.Set("Acme.Inventory.Save", true);            // ...except Save
Log.SetChannelEnabled(LogChannel.Core, false);        // Input and Gameplay off
```

**Declaring channels**
- Declare every channel as a `static readonly` field of a `[LogCategories]` class. Never call `LogCategory.Get("…")` inline at a call site: a typo there silently makes a new channel, and the configuration window only lists declared channels before they first log.
- Name channels after your own namespace (`Acme.Inventory.Save`). Don't start a name with a built-in channel's name (`Network.Transport`), or switching that built-in off silences yours too.
- Declare a group after all of its members. Field initializers run in order, so a group above one of its members sees `null` and its class fails to initialize.
- A log call with no channel is filtered by `Log.MinimumLevel` alone.

**Configuring**
- `Tools > Logs > Configure Logging` switches channels and groups on or off, and sets the minimum level. Settings are saved per machine and to `Assets/Resources/RuntimeLogSettings.asset`, which players load at startup.
- The same window turns the `ENABLE_LOGS` define on or off. Without it, `Trace`, `Debug`, `Info` and `Warning` calls are compiled out; `Error`, `Fatal` and `Exception` always log.
- The window checks declared channels whenever it opens or scripts recompile, and reports channels declared outside a `[LogCategories]` class, channels declared twice, classes that fail to initialize, and names under a built-in channel's.

**Upgrading from 1.x**
- Delete the project's generated `Assets/Plugins/Log/`, or it becomes a second assembly called `Log`.
- Move the project's own channels and combinations into a `[LogCategories]` class; built-in channels and groups (`LogChannel.Network`, `LogChannel.Core`) need no changes.
- `Log.EnabledChannels`, `LogChannel.None`, `LogChannel.All` and the channel `ToStringFast()` are gone: use `Log.SetChannelEnabled`, `Log.SetAllChannelsEnabled` and `channel.Name`.

Core's own repository rebuilds the DLLs from `RawSource~` with `Tools > Logs > Build Log DLLs` after changing the log sources. Projects that install the package never do.

### DLL Generation (Programmatic)

```csharp
using QBS.Core.Editor;
using System.Collections.Generic;

var sources = new List<SourceFile>
{
    new SourceFile(mySourceCode, "MyFile.cs")
};

// Sources are automatically split into runtime and editor DLLs
// based on whether they reference editor-specific namespaces.
// Output: Assets/Plugins/<outputDLLFolderName>/Runtime/<dllName>.dll
//         Assets/Plugins/<outputDLLFolderName>/Editor/<dllName>-Editor.dll
bool success = DLLGenerationHelper.TryGeneratingDLL(
    sources,
    outputDLLFolderName: "MyAssembly",
    dllName: "MyAssembly",
    runtimeAssemblyReferences: null,
    editorAssemblyReferences: null,
    scriptingSymbols: null
);
```

### Enum Utility Source Generators

```csharp
using QBS.SourceGenerators.GeneratorDiscoveryHelpers;

[EnumUtilities(EnumUtilsGenOptions.GenerateHasFlagFast
             | EnumUtilsGenOptions.GenerateValuesArray
             | EnumUtilsGenOptions.GenerateToStringFast)]
public enum MyStatus
{
    Active,
    Inactive,
    Pending
}

// After compile, MyStatusUtils is generated automatically:
string s   = MyStatus.Active.ToStringFast();               // "Active"
bool hit   = MyStatus.Active.HasFlagFast(MyStatus.Active); // true
var values = MyStatusUtils.Values;                         // ImmutableArray<MyStatus>
```

Works on nested types too:

```csharp
public class MyClass
{
    [EnumUtilities(EnumUtilsGenOptions.All)]
    public enum NestedEnum { A, B, C }
}
// Generated utility class: MyClass.NestedEnumUtils
```

## Package Structure

```
com.qbs.core/
├── Runtime/
│   ├── AssemblyCompat.cs          # Version-safe assembly lookups
│   └── QBS.Core.asmdef
│
├── Editor/
│   ├── DLLGeneration/             # Roslyn-based DLL compilation
│   ├── EnumGeneration/            # Enum code generation editor tool
│   ├── Log/                       # Log DLL builder
│   └── QBS.Core.Editor.asmdef
│
├── Plugins/
│   ├── Log/                       # Prebuilt Log.dll and Log-Editor.dll
│   ├── Roslyn/Editor/             # Roslyn compiler DLLs (editor-only)
│   └── SourceGen/                 # Roslyn source generator DLLs
│
├── RawSource~/                    # Log sources the Log DLLs are built from (excluded from builds)
└── Documentation~/                # Additional documentation
```

## API Reference

### Namespaces

- `QBS.Core` - Runtime utilities
- `QBS.Core.Editor` - Editor tools and generators
- `QBS.SourceGenerators.GeneratorDiscoveryHelpers` - Source generator attributes and options

### Key Classes

- `EnumGeneratorComponent` - Enum generation UI and logic
- `LogCategory` / `LogRules` / `LogChannel` - Log channels, the rules that switch them, and the built-in set
- `LogDllBuilder` - Builds the prebuilt Log DLLs from `RawSource~` (core's repository only)
- `DLLGenerator` - Low-level Roslyn-based DLL compilation
- `DLLGenerationHelper` - High-level static API; auto-splits sources into runtime/editor and retries on missing assembly errors
- `AssemblyCompat` - Version-safe assembly lookups (`GetLoadedAssemblies`, `GetAssemblyPath`)
- `EnumUtilitiesAttribute` - Marks enums for compile-time utility generation via Source Generators
- `EnumUtilsGenOptions` - Flags controlling which utilities are generated (`ToStringFast`, `HasFlagFast`, `ValuesArray`)

## Support

- **Email**: questbeginstudios@gmail.com
- **Issues**: [GitHub Issues](https://github.com/Quest-Begin-Studios/QBS-Core/issues)

## License

See [LICENSE.md](LICENSE.md) for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.

---

**Quest Begins Studios** © 2026
