<p align="center">
  <img src="icon.png" width="128" height="128" alt="GhostPad - Minimalist Desktop Wallpaper Notepad for Windows" />
</p>

<h1 align="center">GhostPad</h1>

<p align="center">
  <strong>The featherweight, distraction-free desktop wallpaper notepad for Windows 10 & 11.</strong><br>
  <em>Glued to your desktop background. Never minimizes on <kbd>Win</kbd> + <kbd>D</kbd>. Zero cloud, 100% offline.</em>
</p>

<p align="center">
  <a href="#-download"><img src="https://img.shields.io/badge/Download-v1.0.0-6366f1?style=for-the-badge&logo=windows" alt="Download GhostPad"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/License-Non--Commercial-red?style=for-the-badge" alt="License: Non-Commercial"></a>
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011-0078D6?style=for-the-badge&logo=windows" alt="Platform: Windows 10 / 11">
  <img src="https://img.shields.io/badge/Status-Stable-emerald?style=for-the-badge" alt="Status: Stable">
</p>

---

## 📌 Table of Contents
- [Why GhostPad?](#-why-ghostpad)
- [Feature Comparison](#-feature-comparison)
- [Key Features](#-key-features)
- [Download & Installation](#-download)
- [Keyboard Shortcuts](#-keyboard-shortcuts)
- [Use Cases](#-use-cases)
- [Architecture & Tech Stack](#-architecture--tech-stack)
- [Frequently Asked Questions (FAQ)](#-frequently-asked-questions)
- [License](#-license)

---

## 💡 Why GhostPad?

Most Windows note-taking apps and sticky note widgets have major friction points:
- **They clutter your Taskbar and <kbd>Alt</kbd> + <kbd>Tab</kbd> switcher.**
- **Pressing <kbd>Win</kbd> + <kbd>D</kbd> minimizes all your notes**, hiding your to-do list right when you want to see your desktop.
- **They demand cloud accounts, internet connections, and slow Electron runtimes.**

**GhostPad solves this completely.** By natively anchoring to the Windows Shell desktop layer (`SHELLDLL_DefView`), GhostPad becomes an organic part of your wallpaper. It sits behind every active application when you work, yet stays immediately visible and interactive the moment you glance at your desktop.

---

## 📊 Feature Comparison

| Feature | GhostPad | Windows Sticky Notes | Notion / Obsidian | Notepad.exe |
| :--- | :---: | :---: | :---: | :---: |
| **Glued to Wallpaper** (Behind all apps) | ✅ **Yes** | ❌ Always-on-top | ❌ Windowed | ❌ Windowed |
| **Persists on <kbd>Win</kbd> + <kbd>D</kbd>** | ✅ **Never minimizes** | ❌ Minimizes | ❌ Minimizes | ❌ Minimizes |
| **Zero Taskbar / Alt+Tab Clutter** | ✅ **Clean** | ❌ Clutters | ❌ Clutters | ❌ Clutters |
| **Auto-Hide Zen Mode Header** | ✅ **Yes** | ❌ No | ❌ No | ❌ No |
| **Hardware Corner Rounding (0–28px)** | ✅ **Live Slider** | ❌ Fixed | ❌ Fixed | ❌ Fixed |
| **Interactive Checklists & Tabs** | ✅ **Yes** | ⚠️ Basic | ✅ Yes | ❌ No |
| **100% Offline & Private (JSON)** | ✅ **Zero Telemetry** | ❌ Microsoft Sync | ⚠️ Mixed | ✅ Yes |
| **Portable Mode (USB / Zero Install)** | ✅ **Yes (`./data/`)** | ❌ No | ❌ No | ❌ No |

---

## 📥 Download

GhostPad is distributed in two zero-dependency editions:

| Edition | Description | Direct Download |
| :--- | :--- | :--- |
| **📦 Setup Wizard** *(Recommended)* | 1-click self-contained installer. Unpacks GhostPad, lets you choose any custom install folder, adds Start Menu & Desktop shortcuts, and registers a clean Windows uninstaller. | [**Download GhostPad-Setup.exe**](https://github.com/nxsxhb/ghostpad/releases/latest/download/GhostPad-Setup.exe) |
| **⚡ Portable Edition** | Zero-installation standalone package. Fully isolated into its own `./data/` folder. Perfect for thumb drives, work laptops, and sandboxed setups. | [**Download GhostPad-Portable.zip**](https://github.com/nxsxhb/ghostpad/releases/latest/download/GhostPad-Portable.zip) |

*System Requirements: Windows 10 (1809+) or Windows 11 (64-bit).*

---

## ✨ Key Features

### 1. Wallpaper-Anchored Win32 Engine
GhostPad hooks directly into Windows `SHELLDLL_DefView` via Win32 `SetWindowLongPtr` with `GWLP_HWNDPARENT`. It sits in the exact Z-order plane above your desktop wallpaper but below every open browser, game, or IDE window.

### 2. Ultimate Minimalism & Zen Mode
- **Auto-Hiding Header**: The title bar and control buttons smoothly collapse into the top margin when typing and slide down gently when your mouse hovers over the upper border.
- **Titleless Scratchpad**: Start typing immediately. You are never interrupted by a mandatory "Title" prompt.
- **Smart Title Fallback**: Notes derive automatic clean titles from their first non-empty sentence (identical to Apple Notes and Bear).
- **Auto-Hiding Footer**: Word counts and storage sync indicators fade out during focus.

### 3. Real-Time Hardware Corner Radius
Whether you run a sharp tiling window manager (FancyZones, GlazeWM) or rounded Windows 11 glassmorphism, use the continuous live slider from **`0px`** (sharp 90° corners) up to **`28px`** (ultra-smooth card). GhostPad clips both window handles and child viewports with Win32 GDI `CreateRoundRectRgn`.

### 4. Rich Typing & Keyboard Ergonomics
- **True Indentation**: Hit <kbd>Tab</kbd> and <kbd>Shift</kbd> + <kbd>Tab</kbd> to indent and outdent multi-line text blocks.
- **Intelligent Lists**: Automatic markdown continuation for bullet points (`- `, `* `) and ordered numbering (`1. `).
- **Interactive Checklists**: Type `- [ ] ` or use <kbd>Ctrl</kbd> + <kbd>/</kbd> to create interactive checkboxes that cross off completed tasks with a single click.

### 5. Curated Themes & Opacity Control
- Choose from 8 hand-crafted color schemes (*Obsidian, Espresso, Ocean, Plum, Ember, Pine, Canary, Paper*) or pick any custom hex code with automatic contrast calculation.
- Live opacity slider (15% to 100%) blends your notes into your wallpaper aesthetic.

### 6. Local, Transparent & Private
All notes and preferences are saved in plain, human-readable JSON files:
- **Installed**: `%LOCALAPPDATA%\GhostPad\notes.json`
- **Portable**: `<GhostPadFolder>\data\notes.json`
- **Zero internet connection required. No accounts, no background tracking, no telemetry.**

---

## ⌨️ Keyboard Shortcuts

| Shortcut | Description |
| :--- | :--- |
| <kbd>Tab</kbd> | Indent text / selection by 2 spaces |
| <kbd>Shift</kbd> + <kbd>Tab</kbd> | Outdent text / selection |
| <kbd>Enter</kbd> | Automatically continue bullet list, numbered list, or checklist |
| <kbd>Enter</kbd> on empty bullet | End list formatting and return to normal paragraph |
| <kbd>Ctrl</kbd> + <kbd>/</kbd> | Toggle `- [ ] ` interactive checkbox on current line |
| <kbd>Ctrl</kbd> + <kbd>F</kbd> | Instant search across all saved notes |

---

## 🎯 Use Cases

- **Developer Scratchpad**: Stash temporary API keys, curl commands, JSON blobs, and terminal snippets right on your desktop without cluttering your code editor.
- **Daily Productivity & Focus**: Keep your top 3 daily priorities and interactive to-do checklists in constant peripheral view.
- **Minimalist Workstations & Aesthetic Desktops**: Perfect for users on r/unixporn, r/desktops, and r/minimalism who want an unobtrusive, beautiful scratchpad that matches their wallpaper theme.
- **Quick Phone / Meeting Notes**: Click your desktop and type immediately during phone calls or meetings.

---

## 🏗️ Architecture & Tech Stack

```text
┌────────────────────────────────────────────────────────┐
│                   GhostPad Architecture                │
├───────────────────────────┬────────────────────────────┤
│   Native C# Host (app.cs) │   Frontend Engine (ui.html)│
│   • Win32 SHELLDLL_DefView│   • Glassmorphism CSS UI   │
│   • SetWindowRgn clipping │   • Spring Hover Zen Mode  │
│   • Single-Instance Mutex │   • Interactive Markdown   │
│   • Local JSON Storage    │   • Multi-Palette Engine   │
└─────────────▲─────────────┴─────────────▲──────────────┘
              │                           │
              └─────── WebView2 Bridge ───┘
                postMessage() ↔ WebMessageReceived
```

- **Runtime**: .NET 10.0 (Windows x64)
- **Host**: Win32 P/Invoke & Windows Forms
- **Viewport**: Microsoft Edge WebView2 Evergreen Engine
- **Persistence**: High-speed JSON disk serializer

For a deep technical breakdown of Win32 desktop Z-order layering and DWM window clipping, read the [`GHOSTPAD_KNOWLEDGE_BASE.md`](GHOSTPAD_KNOWLEDGE_BASE.md).

---

## ❓ Frequently Asked Questions

### Does GhostPad collect any personal data?
**No.** GhostPad is 100% offline. It has zero network calls, zero analytics, and zero tracking. Your notes are stored purely on your local hard drive in standard JSON.

### Why doesn't GhostPad minimize when I press <kbd>Win</kbd> + <kbd>D</kbd>?
Standard applications are owned by the desktop taskbar. GhostPad sets its native Win32 window parent (`GWLP_HWNDPARENT`) directly to `SHELLDLL_DefView` (the Windows desktop shell). Because it is part of the desktop plane, "Show Desktop" does not hide it.

### How do I move or resize GhostPad?
Click the **Lock icon** in the top navigation bar to switch to **Movable Mode**. Drag the window by its header, or drag any of the 4 borders/corners to resize. Click the Lock icon again to lock it in place on your wallpaper.

### Can I run GhostPad from a USB flash drive?
**Yes.** Download the **Portable Edition** (`GhostPad-Portable.zip`). It includes an automatic `portable.dat` flag that forces GhostPad to store all notes, settings, and cache locally inside a `./data/` subfolder.

---

## 📄 License

This project is licensed under the **[MIT-NC Non-Commercial License](LICENSE)**.

- ✅ **Free for personal use, modification, and non-commercial distribution.**
- ❌ **Commercial sale, distribution for a fee, or resale of GhostPad is strictly prohibited.**

---

<p align="center">
  Crafted with care by <a href="https://github.com/nxsxhb">nxsxhb</a> &bull; If you enjoy GhostPad, please star ⭐ the repository!
</p>
