# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-09-21

### Added

- **The Log Source Compiler populates itself from the `LogChannel` already compiled into the project**, falling back to `LogChannelDefaults` only when there is none. Both the window and `GenerateLogDLLsHeadless` read it, so neither replaces a project's channels with the defaults — which until now deleted every game-added channel from under its own call sites on the next regeneration. Single-bit members come back as the key list in bit order and multi-bit members as flag combinations, each expressed with the smaller combinations it contains where there are any, so values survive the round trip unchanged.
- `EnumGeneratorComponent.GetGeneratedKeyNames()`, the member names the next generation emits in the order it assigns their values. Both compilers build their `ToStringFast` helper from it instead of each flattening its own copy of the key lists, so the helper can no longer disagree with the enum generated beside it: a blank or repeated row in the window is dropped from both or from neither.
- `LogEditorUtility.ReadActiveChannels` / `WriteActiveChannels`, which carry the editor's channel mask through `EditorPrefs` as a string. `EditorPrefs` has no `long`, and the mask no longer fits an `int`.

### Changed

- **`LogChannel` is generated as a `long`-backed flags enum** rather than `int`, raising the ceiling from 31 channels to 63. Consumers must regenerate `Assets/Plugins/Log/` (`Tools > QBS > Logs > Log Source Compiler`, or `LogSourceCompiler.GenerateLogDLLsHeadless` in CI) after upgrading; a stale `Log.dll` keeps its 32-bit enum and will not link against code compiled for the new one.
- `RuntimeLogSettings.enabledChannels` is serialized as `long`. Unity serializes an enum field as 32 bits, which would silently drop every channel past the 32nd. Existing assets carry `-1` and keep deserializing to every channel enabled.
- The editor's saved channel mask moved to the `QBS_Log_ActiveChannelMask` key. `EditorPrefs` entries are typed, so the 32-bit value under the old key is ignored and the first read falls back to all channels enabled.

### Fixed

- `EnumGeneratorComponent.ConfigureEnumKeys` copies the flag-combination entries it is handed instead of storing the caller's own objects. The entries are mutable and the window edits them in place, so editing a combination wrote straight into the list that supplied it — `LogChannelDefaults` being the one that always does.

- **`Error` and `Fatal` are no longer `[Conditional("ENABLE_LOGS")]`**, on `Log` and on `Log.LogBuilder`. The attribute strips the call at the *call site*, so a player built without the symbol compiled away every error report and left a crash reporter with nothing to send. Those two levels are now gated at runtime by `MinimumLevel` alone; `Trace`, `Debug`, `Info` and `Warning` keep the attribute and still leave a release build entirely. `LogMessage`, `LogToUnity` and `LogToUnityWithReference` lose the attribute for the same reason, so the path an error takes out of the package does not depend on how `Log.dll` itself was built.
- Flag values are computed with a 64-bit shift. `1 << index` is an `int` expression: past bit 31 it wrapped to `int.MinValue` and then to zero, so a `long`- or `ulong`-backed flags enum could not have been generated correctly whatever backing type was selected in the window.

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
