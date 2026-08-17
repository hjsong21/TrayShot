# TrayShot

Language: [English](README.md) | [한국어](README.ko.md)

[![en](https://img.shields.io/badge/lang-en-red.svg)](README.md)
[![ko](https://img.shields.io/badge/lang-ko-blue.svg)](README.ko.md)

TrayShot is a lightweight, high-performance **Smart Windows Screenshot Gallery & Image Manager**. It watches your screenshot directories in real-time, displays them in a gorgeous date-grouped layout, and provides advanced features like OCR, semantic search, clipboard integration, and automatic lossless image conversion.

---

## Key Features

- 🖼️ **Fast Screenshot Gallery**: Beautiful date-grouped thumbnail grid view with instant loading.
- ⌨️ **Global Hotkey Toggle**: Quickly show or hide the gallery panel anywhere with a customizable global hotkey (Default: `Ctrl + Alt + S`).
- 🔍 **AI-powered Search**:
  - **OCR Search**: Automatically extracts text from screenshots in the background for instant keyword searching.
  - **Semantic Search**: Powered by MobileCLIP (local AI) to search screenshots based on visual context and descriptive prompts.
- ⏳ **Image Format Conversion**:
  - Convert screenshots on-demand to PNG, JPG, WebP, BMP, GIF, TIFF, and HEIC.
  - Automatically convert new PNG screenshots to lossless WebP with a 1:1 pixel integrity check and custom disposal policies (Move to Recycle Bin, Delete, or Keep).
- 📋 **Seamless Clipboard & Shell Integration**:
  - Copy and paste screenshots directly using `Ctrl + C` / `Ctrl + V` across File Explorer, web browsers, and chat applications (e.g., KakaoTalk).
  - Support for `Ctrl + Z` to undo paste and file deletion.
- 🌐 **Real-time UI Localization**: Instantly switch languages (English and Korean) on the fly without restarting the application.

---

## Requirements

- **Operating System**: Windows 10 (Version 2004 / Build 19041) or higher.
- **Runtime**: [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) (WPF support).
- **Build Tool**: [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).

---

## Build & Run

### Clone the repository
```bash
git clone https://github.com/hjsong21/TrayShot.git
cd TrayShot
```

### Build the application
```powershell
dotnet build
```

### Run the application
```powershell
dotnet run --project src/TrayShot/TrayShot.csproj
```

### Run unit tests
```powershell
dotnet test
```

---

## Design Notes

- **Architecture**: Follows the **MVVM (Model-View-ViewModel)** design pattern using the `CommunityToolkit.Mvvm` package.
- **UI Framework**: Native WPF customized with sleek dark mode brushes, glassmorphic styling, and micro-animations.
- **Database & Indexing**: Uses SQLite (`Microsoft.Data.Sqlite`) to store OCR text indices, image metadata, and semantic vectors for rapid retrieval.
- **File System Monitoring**: Utilizes `FileSystemWatcher` to asynchronously update the gallery the instant a screenshot is taken or modified.
- **WPF Localization (i18n)**: Implements dynamic XAML `ResourceDictionary` switching. Subscribing to language changes triggers `SetResourceReference` dynamically to swap string resources in real-time.
- **Safe Conversion Pipeline**: The WebP conversion features an automated verification check that compares original and converted pixel rows (1:1 validation) and verifies file sizes before executing the configured source file disposal policy.

---

## Acknowledgements

This project is a Windows port inspired by **[Sukurini](https://github.com/ssut/Sukurini)**, originally created by Suhun Han (ssut) for macOS. 
Huge thanks to the original author for the great idea and open-source contribution.

---

## License

Apache License 2.0 — see the [LICENSE](LICENSE) file for details. 
Forks are always welcome, but please note that you must observe the following conditions:
- Keep the original copyright notices.
- State what changes you made to the files.
- Reproduce the `NOTICE` file wherever third-party notices are shown.

**Note:** The name "TrayShot" is not covered by this license.

---

## Author

- **Ho-Jeong Song** ([hjsong21](https://github.com/hjsong21))
