# QBS-Core

**Core Utility Package from Quest Begins Studios** - A comprehensive Unity development toolkit providing code generation, logging utilities, and assembly management tools.

## Overview

QBS-Core provides essential development tools for building Unity projects. It includes powerful code generation systems, DLL compilation utilities, and logging infrastructure.

## Features

### 🔧 Code Generation Tools
- **Enum Generator**: Create simple, flags, or custom-value enumerations with configurable backing types
- **String Enum Generator**: Generate type-safe string constant enumerations
- **Flag Combinations**: Define named combinations of flag values

### 📝 Logging System
- **Log Source Compiler**: Editor tool for managing logging channels and compiling log sources
- **Default Log Channels**: Network, AI, Physics, Audio, UI, Gameplay, Animation, Input, Save/Load, Dialogue, Inventory, Quest, Combat
- **DLL Compilation**: Compile log sources into runtime or editor assemblies

### 🔨 DLL Generation
- **Roslyn-based Compilation**: Compile C# source files into DLLs at runtime
- **Assembly References**: Support for custom assembly references and scripting symbols
- **Runtime & Editor Support**: Generate both runtime and editor assemblies

### 🛠️ Utilities
- **Assembly Utilities**: Helper methods for Unity assembly management and reflection

## Installation

### Method 1: Via Unity Package Manager (Git URL)

1. Open your Unity project
2. Go to `Window > Package Manager`
3. Click the `+` button in the top-left corner
4. Select `Add package from git URL`
5. Enter the following URL:
   ```
   https://github.com/QuestBeginStudios/com.qbs.core.git
   ```
6. Click `Add`

### Method 2: Via manifest.json

1. Navigate to your Unity project's `Packages` folder
2. Open `manifest.json` in a text editor
3. Add the following line to the `dependencies` section:
   ```json
   {
     "dependencies": {
       "com.qbs.core": "https://github.com/QuestBeginStudios/com.qbs.core.git#v1.0.0"
     }
   }
   ```
4. Save the file and return to Unity (it will automatically import the package)

### Method 3: Install Specific Version

To install a specific version, append the version tag to the Git URL:
```json
"com.qbs.core": "https://github.com/QuestBeginStudios/com.qbs.core.git#v1.0.0"
```

## Requirements

- **Unity Version**: 6000.0 or higher
- **Dependencies**: None (dependency-free package)

## Quick Start

### Access Editor Tools

- **Log Source Compiler**: `Tools > QBS > Logs > Log Source Compiler`

### Generate Enums

1. Open Log Source Compiler window
2. Navigate to the "Enum Generation" tab
3. Configure enum settings (name, namespace, type, backing type)
4. Add enum keys
5. Click "Generate Code" to copy to clipboard

### Compile Log Sources

1. Open Log Source Compiler window
2. **Source Fetch Tab**: Select folder containing source files
3. **Enum Generation Tab**: Generate log channel enums
4. **Source Compilation Tab**: Configure assembly references and compile to DLL

## Package Structure

```
QBS-Core/
└── CoreUnity/
    └── Assets/
        └── com.qbs.core/              # Unity Package
            ├── Runtime/               # Runtime scripts
            │   ├── AssemblyUtilities.cs
            │   └── QBS.Core.asmdef
            ├── Editor/                # Editor tools
            │   ├── DLLGeneration/
            │   ├── EnumGeneration/
            │   ├── Log/
            │   └── StringEnumGeneration/
            ├── RawSource~/            # Raw source files (excluded from builds)
            ├── Documentation~/        # Documentation (excluded from builds)
            ├── package.json           # Package manifest
            ├── README.md              # Package documentation
            ├── CHANGELOG.md           # Version history
            └── LICENSE.md             # MIT License
```

## Documentation

For detailed API documentation and usage examples, see the [package README](CoreUnity/Assets/com.qbs.core/README.md).

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
- **Issues**: [GitHub Issues](https://github.com/QuestBeginStudios/com.qbs.core/issues)

## License

This project is licensed under the MIT License - see the [LICENSE.md](CoreUnity/Assets/com.qbs.core/LICENSE.md) file for details.

## Changelog

See [CHANGELOG.md](CoreUnity/Assets/com.qbs.core/CHANGELOG.md) for version history and release notes.

---

**Quest Begins Studios** © 2026
