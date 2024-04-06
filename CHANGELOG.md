# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.11.4] - 2024-04-06

### Changed
- [Cleanup WhileAsync and other implementations](https://github.com/TheAngryByrd/IcedTasks/pull/44) - Credits @TheAngryByrd

## [0.11.3] - 2024-02-07

### Removed
- [Accidental addition of AwaiterOfResult](https://github.com/TheAngryByrd/IcedTasks/commit/c060b79620398b21c742b0ea475c84f91d52c0ec) - Credits @TheAngryByrd

## [0.11.2] - 2024-02-07

### Fixed
- [Fix parallelism in merge sources (`and!`)](https://github.com/TheAngryByrd/IcedTasks/pull/41) - Credits @TheAngryByrd

## [0.11.0] - 2024-01-30

### Added
- [Use Microsoft.Bcl.AsyncInterfaces in netstandard2.0](https://github.com/TheAngryByrd/IcedTasks/pull/39) This allows for IAsyncDisposable, IAsyncEnumerable, and IAsyncEnumerator, and ValueTask to be used in netstandard2.0.  Credits @TheAngryByrd.

## [0.10.2] - 2024-01-28

### Changed
- [Various documentation updates](https://github.com/TheAngryByrd/IcedTasks/commit/9b3447f6df2e5bef3ed3f572aacb444d99850145)  Credits @TheAngryByrd.
- [Test against net8](https://github.com/TheAngryByrd/IcedTasks/pull/38)  Credits @TheAngryByrd.

## [0.10.0] - 2023-11-21

### Added
- [IAsyncEnumerable support](https://github.com/TheAngryByrd/IcedTasks/pull/37) Credits @TheAngryByrd.

## [0.9.2] - 2023-11-09

### Fixed
- [Type Inference for CancellableTask<unit>](https://github.com/TheAngryByrd/IcedTasks/pull/36) Credits @TheAngryByrd.

## [0.9.1] - 2023-11-07

### Fixed
- [Polyfill fixes](https://github.com/TheAngryByrd/IcedTasks/pull/35) Credits @TheAngryByrd.

## [0.9.0] - 2023-11-05

### Changed
- [Reusable MethodBuilder/TaskBase](https://github.com/TheAngryByrd/IcedTasks/pull/34) Credits @TheAngryByrd.  Adds many new CEs to IcedTasks. Check out the link for details.

## [0.8.5] - 2023-10-29

### Changed
- [Reduce fsharp core version](https://github.com/TheAngryByrd/IcedTasks/pull/32)

## [0.8.5-beta001] - 2023-10-28

### Changed
- [Reduce fsharp core version](https://github.com/TheAngryByrd/IcedTasks/pull/32)

## [0.8.4] - 2023-10-28

### Fixed

- [TryFinallyAsync implementation ignores potential exceptions in TryFinally](https://github.com/TheAngryByrd/IcedTasks/pull/31) Credits @TheAngryByrd

## [0.8.3] - 2023-10-27

### Fixed
- [Dispose not occurring after cancellation](https://github.com/TheAngryByrd/IcedTasks/pull/30) Credits @TheAngryByrd

## [0.8.2] - 2023-10-22

### Fixed
- [AssemblyInfo Generation](https://github.com/TheAngryByrd/IcedTasks/commit/b0ce1550713820cede327036439145733ec4bdde)

## [0.8.2-beta003] - 2023-10-22

### Fixed
- [AssemblyInfo Generation](https://github.com/TheAngryByrd/IcedTasks/commit/b0ce1550713820cede327036439145733ec4bdde)

## [0.8.2-beta002] - 2023-10-22

### Fixed
- [AssemblyInfo Generation](https://github.com/TheAngryByrd/IcedTasks/commit/b0ce1550713820cede327036439145733ec4bdde)

## [0.8.2-beta001] - 2023-10-22

### Fixed
- [AssemblyInfo Generation](https://github.com/TheAngryByrd/IcedTasks/commit/b0ce1550713820cede327036439145733ec4bdde)

## [0.8.0] - 2023-07-17

### Added
- [PoolingValueTask](https://github.com/TheAngryByrd/IcedTasks/pull/27) Credits @TheAngryByrd

### Changed
- [Puts NS2.0 coverage into own test project](https://github.com/TheAngryByrd/IcedTasks/pull/26) Credits @TheAngryByrd

## [0.7.1] - 2023-07-08

### Changed
- [Allocation Optimizations](https://github.com/TheAngryByrd/IcedTasks/pull/25) Credits @TheAngryByrd

## [0.7.0] - 2023-07-04

### Added
- [AsyncEx](https://github.com/TheAngryByrd/IcedTasks/pull/24) Credits @TheAngryByrd

## [0.6.0] - 2023-06-30

### Added
- [CancellableTask.whenAll/throttled/sequential](https://github.com/TheAngryByrd/IcedTasks/pull/23)  Credits @TheAngryByrd

## [0.6.0-beta001] - 2023-06-30

### Added
- [CancellableTask.whenAll/throttled/sequential](https://github.com/TheAngryByrd/IcedTasks/pull/23)  Credits @TheAngryByrd

## [0.5.4] - 2023-04-03

### Changed
- New Release Documentation process Credits @TheAngryByrd

## [0.5.4-beta004] - 2023-04-03

### Changed
- New Release Documentation process Credits @TheAngryByrd

## [0.5.4-beta003] - 2023-03-05

### Changed
- New Release Documentation process Credits @TheAngryByrd

## [0.5.4-beta002] - 2023-03-05

### Changed
- New Release Documentation process Credits @TheAngryByrd

## [0.5.4-beta001] - 2023-03-05

### Changed
- New Release Documentation process Credits @TheAngryByrd

## [0.5.3] - 2023-02-22

### Fixed
- [package netstandard2.1 using PackageOutputPath](https://github.com/TheAngryByrd/IcedTasks/pull/21) Credits @thinkbeforecoding

## [0.5.2] - 2023-02-21 [YANKED]

### Changed
- [Using new SRTP helpers](https://github.com/TheAngryByrd/IcedTasks/pull/19) Credits @TheAngryByrd

## [0.5.1] - 2022-12-17

### Changed
- [Fixed Doc comments and changed StartAsTask to StartImmediateAsTask](https://github.com/TheAngryByrd/IcedTasks/pull/17) Credits @TheAngryByrd

## [0.5.0] - 2022-12-08

### Added
- [Adds BindReturn/MergeSources to allow for parallel computations using and!](https://github.com/TheAngryByrd/IcedTasks/pull/16) Credits @TheAngryByrd

## [0.5.0-beta001] - 2022-12-08

### Added
- [Adds BindReturn/MergeSources to allow for parallel computations using and!](https://github.com/TheAngryByrd/IcedTasks/pull/16) Credits @TheAngryByrd

## [0.4.0] - 2022-12-01

### Added
- [Adds ValueTask and CancellableValueTask](https://github.com/TheAngryByrd/IcedTasks/pull/15) Credits @TheAngryByrd

## [0.3.2] - 2022-11-30

### Changed
- [Refactor Binds to Source members](https://github.com/TheAngryByrd/IcedTasks/pull/12) Credits @TheAngryByrd
- [Expand TFMs to netstandard2.0 and 2.1](https://github.com/TheAngryByrd/IcedTasks/pull/13) Credits @TheAngryByrd
- [InlineIfLambda was just adding compile time without benefit](https://github.com/TheAngryByrd/IcedTasks/pull/14) Credits @TheAngryByrd

## [0.3.2-beta002] - 2022-11-30

### Changed

- [Refactor Binds to Source members](https://github.com/TheAngryByrd/IcedTasks/pull/12) Credits @TheAngryByrd
- [Expand TFMs to netstandard2.0 and 2.1](https://github.com/TheAngryByrd/IcedTasks/pull/13) Credits @TheAngryByrd
- [InlineIfLambda was just adding compile time without benefit](https://github.com/TheAngryByrd/IcedTasks/pull/14) Credits @TheAngryByrd

## [0.3.2-beta001] - 2022-11-30

### Changed
- [Refactor Binds to Source members](https://github.com/TheAngryByrd/IcedTasks/pull/12) Credits @TheAngryByrd

## [0.3.1] - 2022-11-27

### Changed
- [Test refactoring and docs](https://github.com/TheAngryByrd/IcedTasks/pull/11). Credits @TheAngryByrd

## [0.3.1-beta002] - 2022-11-27

### Changed
- [Test refactoring and docs](https://github.com/TheAngryByrd/IcedTasks/pull/11). Credits @TheAngryByrd

## [0.3.1-beta001] - 2022-11-27

### Changed
- [Test refactoring and docs](https://github.com/TheAngryByrd/IcedTasks/pull/11). Credits @TheAngryByrd

## [0.3.0] - 2022-11-08

### Fixed
- [Verifies upstream F# fix for task resumption code](https://github.com/TheAngryByrd/IcedTasks/issues/8)

### Changed
- [Updates to F# 7](https://github.com/TheAngryByrd/IcedTasks/issues/8)

## [0.3.0-beta007] - 2022-11-08

### Fixed
- [Verifies upstream F# fix for task resumption code](https://github.com/TheAngryByrd/IcedTasks/issues/8)

### Changed
- [Updates to F# 7](https://github.com/TheAngryByrd/IcedTasks/issues/8)

## [0.3.0-beta006] - 2022-11-08

### Fixed
- [Verifies upstream F# fix for task resumption code](https://github.com/TheAngryByrd/IcedTasks/issues/8)

### Changed
- [Updates to F# 7](https://github.com/TheAngryByrd/IcedTasks/issues/8)

## [0.3.0-beta005] - 2022-11-07

### Fixed
- [Verifies upstream F# fix for task resumption code](https://github.com/TheAngryByrd/IcedTasks/issues/8)

### Changed
- [Updates to F# 7](https://github.com/TheAngryByrd/IcedTasks/issues/8)

## [0.3.0-beta004] - 2022-11-07

### Fixed
- [Verifies upstream F# fix for task resumption code](https://github.com/TheAngryByrd/IcedTasks/issues/8)

### Changed
- [Updates to F# 7](https://github.com/TheAngryByrd/IcedTasks/issues/8)

## [0.3.0-beta003] - 2022-11-07

### Fixed
- [Verifies upstream F# fix for task resumption code](https://github.com/TheAngryByrd/IcedTasks/issues/8)

### Changed
- [Updates to F# 7](https://github.com/TheAngryByrd/IcedTasks/issues/8)

## [0.3.0-beta002] - 2022-11-07

### Fixed
- [Verifies upstream F# fix for task resumption code](https://github.com/TheAngryByrd/IcedTasks/issues/8)

### Changed
- [Updates to F# 7](https://github.com/TheAngryByrd/IcedTasks/issues/8)

## [0.3.0-beta001] - 2022-11-06

### Fixed

- [Verifies upstream F# fix for task resumption code](https://github.com/TheAngryByrd/IcedTasks/issues/8)

### Changed

- [Updates to F# 7](https://github.com/TheAngryByrd/IcedTasks/issues/8)

## [0.2.0] - 2022-03-22

### Added

- [Adds parallelAsync computation expression](https://github.com/TheAngryByrd/IcedTasks/pull/7)

## [0.1.1] - 2022-03-07

### Changed

- Small memory performance improvement by changing how state machine was being copied
- Remove excess InlineIfLambda. This was causing excessive compile times without any performance benefits.

## [0.1.0] - 2022-03-06

### Added

- Adds ColdTask and CancellableTask

### Changed

- Removes excess namespaces, adds baseline to benchmarks
- Increased soeed and lowered memory usage of ColdTask and CancellableTask
- Build for netstandard2.0 and netstandard2.1

[Unreleased]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.11.4...HEAD
[0.11.4]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.11.3...v0.11.4
[0.11.3]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.11.2...v0.11.3
[0.11.2]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.11.0...v0.11.2
[0.11.1]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.11.0...v0.11.1
[0.11.0]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.10.2...v0.11.0
[0.10.2]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.10.0...v0.10.2
[0.10.1]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.10.0...v0.10.1
[0.10.0]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.9.2...v0.10.0
[0.9.2]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.9.1...v0.9.2
[0.9.1]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.9.0...v0.9.1
[0.9.0]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.8.5...v0.9.0
[0.8.5]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.8.4...v0.8.5
[0.8.5-beta001]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.8.4...v0.8.5-beta001
[0.8.5-beta001]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.8.4...v0.8.5-beta001
[0.8.4]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.8.3...v0.8.4
[0.8.3]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.8.2...v0.8.3
[0.8.2]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.8.0...v0.8.2
[0.8.2-beta003]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.8.0...v0.8.2-beta003
[0.8.2-beta002]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.8.0...v0.8.2-beta002
[0.8.2-beta001]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.8.0...v0.8.2-beta001
[0.8.1]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.8.0...v0.8.1
[0.8.0]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.7.1...v0.8.0
[0.7.1]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.7.0...v0.7.1
[0.7.0]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.6.0...v0.7.0
[0.6.0]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.5.4...v0.6.0
[0.6.0-beta001]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.5.4...v0.6.0-beta001
[0.5.4]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.5.3...v0.5.4
[0.5.4-beta004]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.5.3...v0.5.4-beta004
[0.5.4-beta003]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.5.3...v0.5.4-beta003
[0.5.4-beta002]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.5.3...v0.5.4-beta002
[0.5.4-beta001]: https://github.com/TheAngryByrd/IcedTasks//compare/v0.5.3...v0.5.4-beta001
[0.5.3]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.5.2...v0.5.3
[0.5.2]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.5.1...v0.5.2
[0.5.1]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.5.0...v0.5.1
[0.5.0]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.4.0...v0.5.0
[0.5.0-beta001]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.4.0...v0.5.0-beta001
[0.4.0]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.3.2...v0.4.0
[0.3.2]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.3.1...v0.3.2
[0.3.2-beta002]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.3.1...v0.3.2-beta002
[0.3.2-beta001]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.3.1...v0.3.2-beta001
[0.3.1]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.3.0...v0.3.1
[0.3.1-beta002]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.3.0...v0.3.1-beta002
[0.3.1-beta001]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.3.0...v0.3.1-beta001
[0.3.0]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.2.0...v0.3.0
[0.3.0-beta007]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.2.0...v0.3.0-beta007
[0.3.0-beta006]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.2.0...v0.3.0-beta006
[0.3.0-beta005]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.2.0...v0.3.0-beta005
[0.3.0-beta004]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.2.0...v0.3.0-beta004
[0.3.0-beta003]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.2.0...v0.3.0-beta003
[0.3.0-beta002]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.2.0...v0.3.0-beta002
[0.3.0-beta001]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.2.0...v0.3.0-beta001
[0.2.0]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.1.1...v0.2.0
[0.1.1]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.1.0...v0.1.1
[0.1.0]: https://github.com/TheAngryByrd/IcedTasks/releases/tag/v0.1.0
[0.1.0-beta004]: https://github.com/TheAngryByrd/IcedTasks/releases/tag/v0.1.0-beta004
[0.1.0-beta003]: https://github.com/TheAngryByrd/IcedTasks/releases/tag/v0.1.0-beta003
[0.1.0-beta002]: https://github.com/TheAngryByrd/IcedTasks/releases/tag/v0.1.0-beta002
[0.1.0-beta001]: https://github.com/TheAngryByrd/IcedTasks/releases/tag/v0.1.0-beta001
