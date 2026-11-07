<p align="center">
  <img src="icon.png" width="120" height="120" alt="GhostPad Mascot" />
</p>

<h1 align="center">GhostPad</h1>

<p align="center">
  <em>A featherweight, distraction-free desktop notepad for Windows that lives directly on your wallpaper.</em>
</p>

<p align="center">
  <a href="#-download"><img src="https://img.shields.io/badge/Download-v1.0.0-6366f1?style=for-the-badge&logo=windows" alt="Download"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-Non--Commercial-red?style=for-the-badge" alt="License"></a>
</p>

---

## 📥 Download

Choose the format that fits your workflow best:

| Edition | Description | Download |
| :--- | :--- | :--- |
| **Installer** *(Recommended)* | Self-contained Windows installer wizard. Creates Desktop & Start Menu shortcuts, optional Windows Startup launch, clean Windows uninstaller. | [**GhostPad-Setup.exe**](https://github.com/nxsxhb/ghostpad/releases/latest/download/GhostPad-Setup.exe) |
| **Portable** | Zero-installation standalone package. Completely self-contained into `./data/` folder, perfect for USB drives. | [**GhostPad-Portable.zip**](https://github.com/nxsxhb/ghostpad/releases/latest/download/GhostPad-Portable.zip) |

---

## ✨ Features

- **Wallpaper Anchored**: Native Win32 desktop ownership (`SHELLDLL_DefView`). GhostPad sits permanently in the foreground of your wallpaper and behind every application. It never pops over your browser or IDE, and **never disappears when you press <kbd>Win</kbd> + <kbd>D</kbd>**.
- **Ultimate Minimalism / Zen Mode**:
  - **Auto-Hiding Header**: Smoothly slides up off-screen while you type; gently reveals itself when your cursor touches the top edge.
  - **Titleless Scratchpad**: Jump straight into typing without being forced to name your note.
  - **Smart Title Fallback**: In the notes list, titleless notes automatically derive clean titles from your first line (just like Apple Notes & Bear).
  - **Auto-Hide Status Bar**: Bottom word count and save indicator fade out unless hovered.
- **Physical Window Corner Rounding**:
  - Continuous slider from **`0px`** (sharp 90° corners for tiling setups) up to **`28px`** (smooth modern curved card).
  - Uses native Win32 `SetWindowRgn` and `CreateRoundRectRgn` to physically shape the OS window and clip child rendering surfaces.
- **Typing Ergonomics**:
  - Full <kbd>Tab</kbd> and <kbd>Shift</kbd> + <kbd>Tab</kbd> indentation.
  - Smart bullet lists (`- ` or `* `) and numbered lists (`1. `).
  - Interactive markdown checkboxes (`- [ ] ` & `- [x] `).
  - Toggle checklist with <kbd>Ctrl</kbd> + <kbd>/</kbd>.
- **Customizable Aesthetics**:
  - 8 curated color palettes (Obsidian, Espresso, Ocean, Plum, Ember, Pine, Canary, Paper) plus a full hex color picker with dynamic contrast inversion.
  - Live opacity slider (20% to 100%).
  - Font size slider (12px to 22px).
- **Offline & Private**:
  - Notes and window configurations are saved locally in human-readable JSON files:
    `%LOCALAPPDATA%\GhostPad\notes.json`
  - Zero cloud dependency, zero telemetry, zero analytics.
- **Butter-Smooth Motion**:
  - Screen-coordinate delta tracking (`e.screenX/Y`) streams movement directly to the C# Win32 host, bypassing Chromium mouse capture constraints.
  - Single-instance mutex prevents duplicate background instances.

---

## 🚀 Quick Start

### Prerequisites
- Windows 10 (1809+) or Windows 11
- [.NET 10.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) *(or build the self-contained package below)*
- [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) *(preinstalled on modern Windows 10/11)*

### Running from Pre-built Binaries
Double-click `GhostPad.exe`. GhostPad will launch anchored to your wallpaper.
- Click the **Lock** icon in the header to toggle between **Desktop Mode (Locked)** and **Movable Mode**.
- Click the **Gear** icon to adjust opacity, corner rounding, font size, or color palette.

---

## 🛠️ Building from Source

Clone the repository and compile using the .NET CLI:

### 1. Framework-Dependent Build (Lightweight ~160 KB executable)
```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o "./publish"
```

### 2. Standalone Self-Contained Build (Zero .NET runtime installation required)
```powershell
dotnet publish -c Release -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o "./publish-package"
```

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Action |
| :--- | :--- |
| <kbd>Tab</kbd> | Indent text by 2 spaces |
| <kbd>Shift</kbd> + <kbd>Tab</kbd> | Unindent text |
| <kbd>Enter</kbd> | Continue bullet/numbered list or checklist |
| <kbd>Enter</kbd> on empty bullet | Clear bullet and start clean line |
| <kbd>Ctrl</kbd> + <kbd>/</kbd> | Toggle `- [ ] ` checkbox on current line |

---

## 🏗️ Architecture

```text
┌────────────────────────────────────────────────────────┐
│                   GhostPad Architecture                │
├───────────────────────────┬────────────────────────────┤
│   C# Host (app.cs)        │   HTML/CSS/JS (ui.html)    │
│   • Win32 P/Invoke        │   • Apple Notes typography │
│   • SetWindowRgn clipping │   • Auto-hide Zen header   │
│   • GWLP_HWNDPARENT anchor│   • LocalStorage cache     │
│   • JSON disk persistence │   • Markdown list engine   │
└─────────────▲─────────────┴─────────────▲──────────────┘
              │                           │
              └─────── WebView2 Bridge ───┘
               postMessage() ↔ WebMessageReceived
```

For an in-depth engineering deep dive into Windows DWM surfaces, `SHELLDLL_DefView`, and Chromium mouse capture challenges, see [`GHOSTPAD_KNOWLEDGE_BASE.md`](GHOSTPAD_KNOWLEDGE_BASE.md).

---

## 📄 License

This project is licensed under the [MIT-NC Non-Commercial License](LICENSE).
Free to use, modify, and distribute for personal and non-commercial purposes. Commercial use, resale, or sublicensing for profit is strictly prohibited.
