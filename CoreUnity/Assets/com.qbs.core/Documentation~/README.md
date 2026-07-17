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
- **Enum Generation**: Configure LogChannel enum keys (name/type/namespace are pre-configured)
- **Source Compilation**: Reads raw sources from `RawSource~`, generates the enum + `ToStringFast` utility, then compiles into runtime/editor DLLs
- **Default Channels**: Network, AI, Physics, Audio, UI, Gameplay, Animation, Input, Save/Load, Dialogue, Inventory, Quest, Combat

### 🔨 DLL Generation

Compile C# source files into DLLs at runtime using Roslyn compiler.

**Features**:
- Runtime and Editor assembly compilation
- Custom assembly references
- Scripting symbol support
- Comprehensive error reporting
- Automatic dependency resolution

### 🛠️ Utilities

#### Assembly Utilities
Helper methods for working with Unity assemblies and reflection.

## Installation

### Via Package Manager (Git URL)

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click `+` → `Add package from git URL`
3. Enter: `https://github.com/QuestBeginStudios/QBS-Core.git?path=/CoreUnity/Assets/com.qbs.core`

### Via manifest.json

Add to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.qbs.core": "https://github.com/QuestBeginStudios/QBS-Core.git?path=/CoreUnity/Assets/com.qbs.core#v1.1.0"
  }
}
```

## Requirements

- **Unity Version**: 6000.0 or higher
- **Dependencies**: None

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

// 1. Add or remove LogChannel enum keys in the Enum Generation section
// 2. Click 'Generate Enum' — reads sources from RawSource~ and generates
//    the enum + ToStringFast utility code
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
│   ├── AssemblyUtilities.cs       # Assembly helper utilities
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
- `AssemblyUtilities` - Assembly reflection helpers
- `EnumUtilitiesAttribute` - Marks enums for compile-time utility generation via Source Generators
- `EnumUtilsGenOptions` - Flags controlling which utilities are generated (`ToStringFast`, `HasFlagFast`, `ValuesArray`)

## Support

- **Email**: questbeginstudios@gmail.com
- **Issues**: [GitHub Issues](https://github.com/QuestBeginStudios/QBS-Core/issues)

## License

See [LICENSE.md](LICENSE.md) for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.

---

**Quest Begins Studios** © 2026
