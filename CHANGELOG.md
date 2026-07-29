# CHANGELOG

Language: [English](CHANGELOG.md) | [한국어](CHANGELOG.ko.md)

[![en](https://img.shields.io/badge/lang-en-red.svg)](CHANGELOG.md)
[![ko](https://img.shields.io/badge/lang-ko-blue.svg)](CHANGELOG.ko.md)

---

## [v0.3.0] - 2026-07-29

### Added
- **Custom Resize Handles**: Native WM_NCHITTEST handles for borderless window border drag resizing
- **Window Size Persistence**: Persistent storage of resized gallery window width/height in AppSettings with reset support
- **Dynamic Theme Switching**: Real-time Dark/Light/System theme switching via `DynamicResource` bindings
- **Lossless WebP Conversion Pipeline**: Automatic WebP conversion pipeline with 1:1 RGB/Alpha pixel verification
- **Original PNG Disposal Policies**: Configurable disposal policy for source PNGs upon successful WebP conversion (Trash, Delete, or Keep)

### Fixed
- **App.xaml Resource Exception**: Resolved missing `BooleanToVisibilityConverter` resource exception during XAML data binding
- **Conversion Pipeline Linkage**: Interconnected `ScreenshotStore` file detection events with `ConversionCoordinator`

## [v0.2.0] - 2026-07-29

### Added
- **Asynchronous Thumbnail Decoding**: High-speed image decoding and memory cache loader using ImageSharp and ConcurrentDictionary
- **Tray Animation Renderer**: 30 FPS dynamic pulse ring animation on notification tray icon upon screenshot detection
- **Spotlight Gallery UI**: macOS Spotlight-style borderless gallery panel UI with Acrylic theme layout
- **WinRT OCR Text Extraction**: Automatic Korean/English text extraction from screenshots via Windows.Media.Ocr engine
- **FTS5 Trigram Search**: SQLite FTS5 Trigram full-text search indexer supporting partial word and prefix search
- **MobileCLIP AI Semantic Search**: Natural language image semantic search based on ONNX MobileCLIP deep learning model and BPE tokenizer
- **Fluent Design Preferences UI**: Modern WPF-UI based settings window (`PreferencesWindow`) layout and tab structure
- **Startup Manager & Sentry Integration**: Registry startup launch control (`StartupManager`) and Sentry telemetry integration

## [v0.1.0] - 2026-07-29

### Added
- **Core Data Models**: Defined `Screenshot`, `ScreenshotChange`, and `FileType` domain models
- **SQLite Local DB Wrapper**: Implemented high-performance thread-safe SQLite wrapper with WAL (Write-Ahead Logging) mode
- **AppSettings & Logger**: Built JSON serialization configuration singleton infrastructure and asynchronous file logger
- **Screenshot Folder Watcher**: Implemented reactive debounced `FolderWatcher` and `RecursiveFolderWatcher`
- **Central State Manager**: Established settlement ladder verification for new captures and `ScreenshotStore` repository
