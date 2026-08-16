# CHANGELOG

Language: [English](CHANGELOG.md) | [한국어](CHANGELOG.ko.md)

[![en](https://img.shields.io/badge/lang-en-red.svg)](CHANGELOG.md)
[![ko](https://img.shields.io/badge/lang-ko-blue.svg)](CHANGELOG.ko.md)

---

## [v0.10.0] - 2026-08-07

### Added
- **DynamicResource Runtime Localization**: Real-time language switching (Korean/English) based on WPF `ResourceDictionary` and `DynamicResource`
- **Universal OLE Drag-and-Drop**: Built Win32 `CF_DIB` (DIBv5) memory stream pipeline for seamless MS Office and HWP compatibility
- **Web Editor In-Memory PNG**: In-memory PNG conversion support for web browsers and editors like Antigravity 2.0

### Fixed
- **Duplicate Drag-and-Drop Insertion**: Resolved single file path in FileDrop payload to prevent duplicate image insertion in KakaoTalk and Office
- **Dynamic Hotkey Status Refresh**: Real-time translation update of global hotkey status text when language changes
- **Initial Tray Icon Rendering**: Fixed transparent icon issue on initial startup by initializing TaskbarIcon before TrayIconAnimator

## [v0.9.0] - 2026-08-02

### Added
- **Date-Grouped Gallery UI**: Grouped screenshots in gallery panel under date section headers (Today, Yesterday, etc.)
- **2D Grid Keyboard Navigation**: 2D grid row/column keyboard navigation engine calculated across grouped items
- **Home/End/PgUp/PgDn Fast Navigation**: Fast jump to first/last item and page up/down navigation with synchronized scrolling

### Improved
- **Column-Preserving Vertical Navigation**: Maintained preferred column position across date group boundaries via `GridRowInfo`
- **Preview Selection & Scroll Sync**: Synchronized main gallery selection cursor and scroll position during preview navigation
- **Refined Date Grouping**: Grouped screenshots using combined actual file timestamps (`CreationTime`) and filename parsing

### Fixed
- **Grouped View Command Bindings**: Resolved broken context menu command bindings (Copy, Convert, Delete, etc.) in grouped view
- **Selection Visual Update on Deletion**: Ensured immediate visual selection border update on the next item following deletion

## [v0.8.0] - 2026-07-31

### Added
- **About Window Popup**: Implemented `AboutWindow` displaying app version, copyright, and feature highlights
- **Header Info & Tray Menu Binding**: Wired gallery header info button and system tray context menu to About window

### Improved
- **Dynamic Assembly Metadata Extraction**: Dynamically extracted Version and Copyright from `TrayShot.csproj` for UI display
- **Preferences Window Center-Top Alignment**: Automatically positioned `PreferencesWindow` to top-center of gallery panel
- **ESC Key to Close Preferences**: Added KeyDown handler to close `PreferencesWindow` on ESC key press
- **Auto-Selection on Deletion**: Automatically selected next image card after item deletion without losing selection
- **Relative Arrow Key Navigation**: Key navigation moves relative to currently selected item with auto-scrolling

### Fixed
- **AppSettings Enum Deserialization**: Added `JsonStringEnumConverter` to JSON options to resolve disposal policy reset bug
- **Preferences Tab Mouseover Flickering**: Fixed background brush flickering issue on tab hover in preferences window

## [v0.7.0] - 2026-07-30

### Added
- **Right-Click Context Menu**: Context menu for gallery items (Open, Copy, Paste, Format Conversion, Delete, Undo)
- **6-Format Manual Image Conversion**: Submenu for immediate manual conversion to PNG, JPG, WebP, BMP, GIF, and TIFF
- **HEIC Image Codec Support**: HEIC decoding and cross-format conversion via Windows WIC codec pipeline
- **Toast Notification Banner**: Toast UI updates for conversion progress ('⏳ Converting...') and success ('✅ Converted!')

### Improved
- **Original Timestamp Preservation**: Preserved `CreationTime` and `LastWriteTime` timestamps and metadata on converted files
- **Format Badge Indicator**: Visual format badge (PNG, WEBP, JPG, etc.) on top-right corner of gallery item cards

### Performance
- **Asynchronous Conversion Execution**: Asynchronous `Task.Run` execution for image conversion to keep UI responsive

## [v0.6.0] - 2026-07-30

### Added
- **Win32 Recycle Bin Deletion**: Safe file deletion to Windows Recycle Bin via `SHFileOperation` API on Delete key press
- **Bidirectional Clipboard Integration**: Dual file path and bitmap copying on `Ctrl+C`, pasting external images on `Ctrl+V`
- **Ctrl+Z Undo Stack**: In-memory undo stack for removing pasted files and recovering deleted items from Recycle Bin

## [v0.5.0] - 2026-07-30

### Added
- **Preview Window Keyboard Navigation**: Arrow keys and Home/End navigation support inside `PreviewWindow` popup
- **Search Bar Clear Button**: Clear query button (X) and command activated when text is present in search bar
- **Multi-Stage ESC Key Handling**: Multi-stage ESC key logic (Stage 1: clear search query -> Stage 2: hide gallery window)

### Improved
- **Preview Focus Restoration**: Retained gallery window visibility on preview open and restored focus on preview close
- **Header Icon Vector Resources**: Updated gallery header settings and exit action buttons with crisp SVG vector icons

## [v0.4.0] - 2026-07-30

### Added
- **Win32 Global Hotkeys**: Desktop global hotkey (`Ctrl+Alt+S`) to toggle gallery panel via `RegisterHotKey` API
- **Interactive Hotkey Input Control**: Custom input control in preferences with real-time Win32 shortcut conflict validation

### Fixed
- **Delayed HWND Initialization**: Resolved global hotkey registration failure on hidden windows using `EnsureHandle()`
- **Shortcut Preemption Guard**: Added guard to prevent single modifier keys (Ctrl/Shift/Alt) from blocking system shortcuts
- **Infinite Conversion Loop Guard**: Fixed infinite WebP reconversion loop when source PNG retention (Keep) policy is active

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
- **AppSettings & 로거**: JSON 직렬화 설정 싱글톤 인프라 및 비동기 파일 로거 구축
- **스크린샷 폴더 감시자**: Reactive Debounce 적용 `FolderWatcher` 및 `RecursiveFolderWatcher` 구현
- **중앙 상태 관리자**: 신규 파일 캡처 안착 검증(Settlement ladder) 및 `ScreenshotStore` 저장소 구축
