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
- Generate string-based enumerations for type-safe string constants
- Useful for serialization and configuration

### 📝 Log System

#### Log Source Compiler
A powerful editor tool for managing logging channels and compiling log sources into DLLs.

**Access**: `Tools > QBS > Logs > Log Source Compiler`

**Features**:
- **Source Fetch**: Collect and manage log source files
- **Enum Generation**: Create log channel enumerations
- **Source Compilation**: Compile sources into runtime/editor DLLs
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
    "com.qbs.core": "https://github.com/QuestBeginStudios/QBS-Core.git?path=/CoreUnity/Assets/com.qbs.core#v1.0.0"
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

// Access via Tools > QBS > Logs > Log Source Compiler
// Navigate to the "Enum Generation" tab
// Configure your enum settings and generate code
```

### Log Source Compilation

```csharp
// Access via Tools > QBS > Logs > Log Source Compiler

// 1. Source Fetch Tab: Select folder containing log source files
// 2. Enum Generation Tab: Generate log channel enums
// 3. Source Compilation Tab: Compile into DLL with assembly references
```

### DLL Generation (Programmatic)

```csharp
using QBS.Core.Editor;

var parameters = new DLLGenerationParameters
{
    SourceFiles = new List<SourceFile> { /* your sources */ },
    OutputPath = "Assets/GeneratedDLLs/MyAssembly.dll",
    AssemblyName = "MyAssembly",
    IsRuntimeCompiler = true,
    RuntimeAssemblyReferences = new List<string>(),
    ScriptingSymbols = new List<string>()
};

var helper = new DLLGenerationHelper();
bool success = helper.GenerateDLL(parameters);
```

## Package Structure

```
com.qbs.core/
├── Runtime/
│   ├── AssemblyUtilities.cs       # Assembly helper utilities
│   └── QBS.Core.asmdef
│
├── Editor/
│   ├── DLLGeneration/             # Runtime DLL compilation
│   ├── EnumGeneration/            # Enum code generation
│   ├── Log/                       # Log system tools
│   ├── StringEnumGeneration/      # String enum generation
│   └── QBS.Core.Editor.asmdef
│
└── Documentation~/                # Additional documentation
```

## API Reference

### Namespaces

- `QBS.Core` - Runtime utilities
- `QBS.Core.Editor` - Editor tools and generators

### Key Classes

- `EnumGeneratorComponent` - Enum generation UI and logic
- `LogSourceCompiler` - Log channel management and compilation
- `DLLGenerator` - Roslyn-based DLL compilation
- `DLLGenerationHelper` - High-level DLL generation interface
- `AssemblyUtilities` - Assembly reflection helpers

## Support

- **Email**: questbeginstudios@gmail.com
- **Issues**: [GitHub Issues](https://github.com/QuestBeginStudios/QBS-Core/issues)

## License

See [LICENSE.md](LICENSE.md) for details.

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.

---

**Quest Begins Studios** © 2026
