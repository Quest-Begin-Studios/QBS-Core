# QBS-Core

**Core Utility Package from Quest Begins Studios** - A comprehensive Unity development toolkit providing code generation, logging utilities, and assembly management tools.

## Overview

QBS-Core provides essential development tools for building Unity projects. It includes powerful code generation systems, DLL compilation utilities, and logging infrastructure.

## Features

### 🔧 Code Generation Tools
- **Enum Generator**: Create simple, flags, or custom-value enumerations with configurable backing types
- **String Enum Generator**: Fast `ToStringFast()` string representation — now powered by the Roslyn Source Generators at compile time
- **Flag Combinations**: Define named combinations of flag values
- **Enum Utility Source Generators**: Auto-generate `HasFlagFast()`, `ToStringFast()`, and a `Values` array for any enum at compile time via `[EnumUtilities]`

### 📝 Logging System
- **Log Source Compiler**: Editor tool for managing logging channels and compiling log sources
- **Default Log Channels**: Network, AI, Physics, Audio, UI, Gameplay, Animation, Input, Save/Load, Dialogue, Inventory, Quest, Combat
- **DLL Compilation**: Compile log sources into runtime or editor assemblies

### 🔨 DLL Generation
- **Roslyn-based Compilation**: Compile C# source files into DLLs at runtime
- **Assembly References**: Support for custom assembly references and scripting symbols
- **Runtime & Editor Support**: Generate both runtime and editor assemblies

### 🛠️ Utilities
- **Assembly Compatibility**: `AssemblyCompat` resolves loaded assemblies and their paths through the right API for the running editor, across the Unity 6000.4 assembly API change

## Installation

### Method 1: Via Unity Package Manager (Git URL)

1. Open your Unity project
2. Go to `Window > Package Manager`
3. Click the `+` button in the top-left corner
4. Select `Add package from git URL`
5. Enter the following URL:
   ```
   https://github.com/Quest-Begin-Studios/QBS-Core.git?path=/CoreUnity/Assets/com.qbs.core
   ```
6. Click `Add`

### Method 2: Via manifest.json

1. Navigate to your Unity project's `Packages` folder
2. Open `manifest.json` in a text editor
3. Add the following line to the `dependencies` section:
   ```json
   {
     "dependencies": {
       "com.qbs.core": "https://github.com/Quest-Begin-Studios/QBS-Core.git?path=/CoreUnity/Assets/com.qbs.core#v1.1.1"
     }
   }
   ```
4. Save the file and return to Unity (it will automatically import the package)

### Method 3: Install Specific Version

To install a specific version, append the version tag to the Git URL:
```json
"com.qbs.core": "https://github.com/Quest-Begin-Studios/QBS-Core.git?path=/CoreUnity/Assets/com.qbs.core#v1.1.1"
```

## Requirements

- **Unity Version**: 6000.0 or higher — the package compiles on both sides of the Unity 6000.4 assembly API change
- **Dependencies**: None (dependency-free package)

## Quick Start

### Access Editor Tools

- **Log Source Compiler**: `Tools > QBS > Logs > Log Source Compiler`
- **Enum Generator**: `Tools > QBS > Enum Generator`

### Generate Enums

1. Open Enum Generator (`Tools > QBS > Enum Generator`)
2. Configure enum settings (name, namespace, type, backing type)
3. Add enum keys (and flag combinations if using a Flags enum)
4. Click "Generate Enum" — source is displayed and can be copied to clipboard

### Compile Log Sources

1. Open Log Source Compiler (`Tools > QBS > Logs > Log Source Compiler`)
2. Add or remove LogChannel enum keys in the Enum Generation section
3. Click "Generate Enum" — reads sources from `RawSource~` and generates the enum + string utilities
4. Click "Generate DLL" — compiles into runtime and editor DLLs under `Assets/Plugins/Log/`

### Use Enum Utility Source Generators

1. Add `using QBS.SourceGenerators.GeneratorDiscoveryHelpers;` to your file
2. Annotate any enum with `[EnumUtilities(EnumUtilsGenOptions.All)]`
3. Utilities are generated on next compile — no editor window required
4. Access via `MyEnumUtils.Values`, `.ToStringFast()`, and `.HasFlagFast()`

## Package Structure

```
QBS-Core/
└── CoreUnity/
    └── Assets/
        └── com.qbs.core/              # Unity Package
            ├── Runtime/               # Runtime scripts
            │   ├── AssemblyCompat.cs
            │   └── QBS.Core.asmdef
            ├── Editor/                # Editor tools
            │   ├── DLLGeneration/
            │   ├── EnumGeneration/
            │   └── Log/
            ├── Plugins/               # Precompiled DLLs
            │   ├── Roslyn/Editor/     # Roslyn compiler DLLs (editor-only)
            │   └── SourceGen/         # Roslyn source generator DLLs
            ├── RawSource~/            # Raw source files (excluded from builds)
            ├── Documentation~/        # Documentation (excluded from builds)
            ├── package.json           # Package manifest
            ├── README.md              # Package documentation
            ├── CHANGELOG.md           # Version history
            └── LICENSE.md             # MIT License
```

## Documentation

For detailed API documentation and usage examples, see the [package README](CoreUnity/Assets/com.qbs.core/Documentation~/README.md).

## Development

### Project Structure

This repository contains:
- **CoreUnity**: Unity project for package development and testing
- **com.qbs.core**: The actual Unity package (located in `CoreUnity/Assets/com.qbs.core`)

### Building from Source

1. Clone this repository
2. Open `CoreUnity` folder in Unity 6000.0 or higher
3. The package is located at `Assets/com.qbs.core`

## Versioning

This project follows [Semantic Versioning](https://semver.org/):
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward-compatible)
- **PATCH**: Bug fixes (backward-compatible)

## Support

- **Email**: questbeginstudios@gmail.com
- **Issues**: [GitHub Issues](https://github.com/Quest-Begin-Studios/QBS-Core/issues)

## License

This project is licensed under the MIT License - see the [LICENSE.md](CoreUnity/Assets/com.qbs.core/LICENSE.md) file for details.

## Changelog

See [CHANGELOG.md](CoreUnity/Assets/com.qbs.core/CHANGELOG.md) for version history and release notes.

---

**Quest Begins Studios** © 2026
