# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[Unreleased]: https://github.com/TheAngryByrd/IcedTasks/compare/v0.3.0-beta003...HEAD
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
