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

#### Log Source Compiler
A powerful editor tool for managing logging channels and compiling log sources into DLLs.

**Access**: `Tools > QBS > Logs > Log Source Compiler`

**Features**:
- **Channel Manifests**: `LogChannel` is built from every `LogChannels.json` in the project. Manifests from packages installed read-only are shown greyed out; the project's own, and any embedded or local package's, are edited in the window
- **Source Compilation**: Reads raw sources from `RawSource~`, generates the enum + `ToStringFast` utility, then compiles into runtime/editor DLLs
- **Default Channels** (from core's own manifest, bits 0–9): Network, AI, Physics, UI, Input, SaveLoad, Loading, Gameplay, Audio, Rendering, with the combinations Core, Presentation and Simulation

#### Log Channel Manifests

A package that needs log channels declares them in `<package>/Editor/LogChannels.json`. The project's own channels live in `Assets/Editor/LogChannels.json`.

```json
{
    "StartBit": 16,
    "Direction": "Up",
    "Capacity": 8,
    "Channels": [ "Inventory", "", "Loot" ],
    "Combinations": [
        { "Name": "Items", "Flags": [ "Inventory", "Loot" ] }
    ]
}
```

- **Channels are pinned to bits.** Channel `i` sits at `StartBit + i` (`Up`) or `StartBit - i` (`Down`), so every project that installs a package gives its channels the same bits, whatever order packages were installed in.
- **Lists only grow.** Add channels at the end. To remove one, empty its entry (`""`, the **Retire** button in the window): the bit stays taken, and every channel after it keeps its own.
- **The layout is chosen once, when the window creates the file.** A manifest inside a package (a folder with a `package.json` above it) counts up, starting past every other upward manifest's reservation, and reserves `Capacity` bits (8 by default; core reserves 0–15) so it can grow without landing on packages built on top of it. The project's manifest counts down from bit 62, since the game is the last thing built. The choice is then saved in the file; if it guessed wrong, edit `StartBit`/`Direction` before anything is compiled against it.
- **Clashes stop generation.** Two manifests may declare the same channel on the same bit and share it. Any other two channels on one bit, or one channel on two bits, is an error naming both manifests; nothing is moved automatically.
- **A published package's bits are its public API.** Code compiled against a channel carries its value, so changing `StartBit`, `Direction`, or the order of `Channels` after release is a breaking change.

#### Packages Must Not Ship the Log DLLs

`Log.dll` and `Log-Editor.dll` are generated per project into `Assets/Plugins/Log/`, from core's `RawSource~` and every manifest in that project. A package must never include them:

- Unity refuses two precompiled assemblies with the same name, so a shipped copy collides with the project's.
- A shipped copy freezes `LogChannel` at whatever its author's project had, without the channels of any other package.

Ship only `Editor/LogChannels.json`, reference `Log.dll` as a precompiled reference from the package's asmdef, and keep the generated DLLs outside the package folder. When a project has no Log DLLs, the package generates them on load; when they exist but lack a manifest's channels, a warning in the console says to regenerate from the window.

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
    "com.qbs.core": "https://github.com/Quest-Begin-Studios/QBS-Core.git?path=/CoreUnity/Assets/com.qbs.core#v1.1.1"
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

### Log Source Compilation

```csharp
// Access via Tools > QBS > Logs > Log Source Compiler

// 1. Add or retire channels in the editable LogChannels.json manifests
// 2. Click 'Generate Enum' — saves the manifests, lays the channels out on
//    their bits, reads sources from RawSource~ and generates the enum +
//    ToStringFast utility code
// 3. Click 'Generate DLL' — compiles all sources into runtime and editor
//    DLLs under Assets/Plugins/Log/
```

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
│   ├── Log/                       # Log system and compiler tools
│   └── QBS.Core.Editor.asmdef
│
├── Plugins/
│   ├── Roslyn/Editor/             # Roslyn compiler DLLs (editor-only)
│   └── SourceGen/                 # Roslyn source generator DLLs
│
├── RawSource~/                    # Raw log source files (excluded from builds)
└── Documentation~/                # Additional documentation
```

## API Reference

### Namespaces

- `QBS.Core` - Runtime utilities
- `QBS.Core.Editor` - Editor tools and generators
- `QBS.SourceGenerators.GeneratorDiscoveryHelpers` - Source generator attributes and options

### Key Classes

- `EnumGeneratorComponent` - Enum generation UI and logic (shared by Enum Generator window and Log Source Compiler)
- `LogSourceCompiler` - Log channel management and DLL compilation
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
