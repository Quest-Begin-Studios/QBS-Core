# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.1] - 2026-09-10

### Added
- `QBS.Core` runtime assembly holding `AssemblyCompat`, a wrapper over the assembly lookup APIs that changed in Unity 6000.4. `GetLoadedAssemblies()` and `GetAssemblyPath(assembly)` call `UnityEngine.Assemblies.CurrentAssemblies` and `Assembly.GetLoadedAssemblyPath()` on newer editors, and fall back to `AppDomain.CurrentDomain.GetAssemblies()` and `Assembly.Location` on older ones. The guard is `UNITY_6000_4_OR_NEWER`: both APIs are documented in the 6000.4 script reference and absent from 6000.3. Downstream packages can reference the assembly instead of repeating the version guard.

### Changed
- `LogSourceCompiler` and `DLLGenerationHelper` resolve assemblies through `AssemblyCompat`, so the package compiles again on Unity 6000.0 through 6000.3, where the `UnityEngine.Assemblies` namespace does not exist

### Fixed
- `DLLGenerationHelper` tested `GetLoadedAssemblyPath()` for emptiness and then added `Assembly.Location` to the extra reference list. Under CoreCLR, where assemblies can be loaded from memory, `Location` is empty, so a recompilation retry was handed an empty path instead of the assembly it had just resolved. Both the test and the value now come from the same lookup.

## [1.1.0] - 2026-05-12

### Added
- Enum Utility Source Generators: apply `[EnumUtilities]` to any enum to auto-generate `HasFlagFast()`, `ToStringFast()`, and a static `Values` array at compile time
- Recursive retry logic in `DLLGenerationHelper` to handle transient file-lock failures during compilation
- Additional configuration options in `DLLGenerationParameters`

### Changed
- Roslyn compiler DLLs reorganized under `Plugins/Roslyn/Editor/` as a cleaner package-import structure
- `AssemblyUtilities` updated for forward compatibility with Unity 6000.6 assembly changes

### Removed
- Runtime `EnumUtilsGenerator.cs` and `EnumUtilsAttribute.cs` — superseded by the new compile-time Source Generators

## [1.0.0] - 2026-03-28

### Added
- Initial release of QBS Core package
- Enum Generator with support for simple, flags, and custom value enums
- String Enum Generator for type-safe string constants
- Log Source Compiler with default logging channels
- DLL Generation system using Roslyn compiler
- Assembly Utilities for reflection and assembly management
