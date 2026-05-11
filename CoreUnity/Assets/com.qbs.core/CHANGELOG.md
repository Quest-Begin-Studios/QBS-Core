# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
