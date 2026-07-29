# CHANGELOG

Language: [English](CHANGELOG.md) | [한국어](CHANGELOG.ko.md)

[![en](https://img.shields.io/badge/lang-en-red.svg)](CHANGELOG.md)
[![ko](https://img.shields.io/badge/lang-ko-blue.svg)](CHANGELOG.ko.md)

---

## [v0.1.0] - 2026-07-29

### Added
- **Core Data Models**: Defined `Screenshot`, `ScreenshotChange`, and `FileType` domain models
- **SQLite Local DB Wrapper**: Implemented high-performance thread-safe SQLite wrapper with WAL (Write-Ahead Logging) mode
- **AppSettings & Logger**: Built JSON serialization configuration singleton infrastructure and asynchronous file logger
- **Screenshot Folder Watcher**: Implemented reactive debounced `FolderWatcher` and `RecursiveFolderWatcher`
- **Central State Manager**: Established settlement ladder verification for new captures and `ScreenshotStore` repository
