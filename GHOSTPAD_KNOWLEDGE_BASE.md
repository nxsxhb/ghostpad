# GhostPad — Engineering Knowledge Base & Architecture Blueprint

This document compiles the architectural patterns, deep Windows OS plumbing, and hard-won lessons learned while building **GhostPad** (and its sister widget **GhostClock**). 

---

## 1. High-Level Architecture: The Hybrid Desktop Pattern

GhostPad uses a **.NET 10 Windows Forms Host + Chromium WebView2 Client** architecture.

```
┌────────────────────────────────────────────────────────┐
│                   GhostPad Architecture                │
├───────────────────────────┬────────────────────────────┤
│   C# Host (app.cs)        │   HTML/CSS/JS (ui.html)    │
│   • Win32 P/Invoke        │   • Apple Notes typography │
│   • Window message loop   │   • Responsive layout      │
│   • File I/O persistence  │   • Smart editor engine    │
│   • DWM & Z-order anchor  │   • SVGs & Theme palettes  │
└─────────────▲─────────────┴─────────────▲──────────────┘
              │                           │
              └─────── WebView2 Bridge ───┘
               postMessage() ↔ WebMessageReceived
```

### Why not Electron?
- **Binary Footprint**: Electron apps package Node.js and an entire Chromium binary (~150 MB). GhostPad’s core executable is **~160 KB** because it reuses the system's evergreen `WebView2` runtime preinstalled on Windows 10/11.
- **Resource Usage**: Instant startup with near-zero idle CPU and minimal RAM footprint.
- **Low-Level Control**: Direct access to Win32 APIs (`user32.dll`, `dwmapi.dll`) without needing native C++ node-gyp bindings.

---

## 2. The Desktop Layer Problem (Surviving Win + D)

### The Challenge
We required GhostPad to adhere to a hard constraint:
> *"Foreground of the wallpaper, background of any other app that comes there. No matter what the user clicks, this must not change."*

Standard top-level windows cannot achieve this out of the box because pressing **Win + D** ("Show Desktop") causes Windows Explorer to bring the desktop shell (`WorkerW` / `Progman`) to the top and minimize all application windows.

### What Didn't Work (And Why)

1. **`SetWindowPos(HWND_BOTTOM)` on Deactivation**:
   - *Failure*: `HWND_BOTTOM` places the window at the very bottom of the OS window stack. In Windows 10/11, `WorkerW` (the desktop wallpaper surface) sits *above* `HWND_BOTTOM`. When "Show Desktop" was pressed, the wallpaper was drawn **directly over GhostPad**, making it vanish until another app opened.
2. **`TopMost = true` with Dynamic Foreground Hooks**:
   - *Failure*: Listening to `EVENT_SYSTEM_FOREGROUND` and toggling `HWND_TOPMOST` whenever the desktop took focus caused race conditions. Clicking the Windows Taskbar to open an app briefly activated `Shell_TrayWnd`, triggering `TOPMOST` and causing GhostPad to flash, pop up, and linger over newly focused apps.
3. **Pure `WM_SYSCOMMAND / SC_MINIMIZE` Interception**:
   - *Failure*: Explorer's "Show Desktop" command uses internal shell dispatch calls that bypass `WM_SYSCOMMAND` and send `WM_WINDOWPOSCHANGING` / `WM_SHOWWINDOW` directly.

### The Winning Solution: Desktop Window Ownership (`GWLP_HWNDPARENT`)

In Win32 window management, changing a top-level window's owner via `GWLP_HWNDPARENT` (-8) sets up a native parent-child z-order hierarchy **without making it a child window** (meaning mouse focus and keyboard message loops are not broken).

```csharp
[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

public const int GWLP_HWNDPARENT = -8;

// Find SHELLDLL_DefView (the window that renders desktop icons)
IntPtr desktopOwner = GetDesktopDefView();
if (desktopOwner != IntPtr.Zero)
{
    SetWindowLongPtr(this.Handle, GWLP_HWNDPARENT, desktopOwner);
}
```

#### Locating `SHELLDLL_DefView` across Windows 10 & 11:
```csharp
private static IntPtr GetDesktopDefView()
{
    IntPtr progman = FindWindow("Progman", null);
    IntPtr defView = FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);
    if (defView != IntPtr.Zero) return defView;

    IntPtr found = IntPtr.Zero;
    EnumWindows((hWnd, lParam) =>
    {
        IntPtr child = FindWindowEx(hWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
        if (child != IntPtr.Zero)
        {
            found = child;
            return false; // Found, stop search
        }
        return true;
    }, IntPtr.Zero);

    return found != IntPtr.Zero ? found : progman;
}
```

#### Why This Works Permanently:
- **Wallpaper Foreground**: Because its native owner is `SHELLDLL_DefView`, GhostPad always renders in front of the wallpaper.
- **Application Background**: Because `SHELLDLL_DefView` is at the bottom of the OS window stack, any normal application (Chrome, VSCode, File Explorer) sits in front of GhostPad.
- **Immune to Win + D**: When "Show Desktop" is triggered, Windows reveals `SHELLDLL_DefView`. Because GhostPad is owned by it, Windows displays GhostPad right along with the desktop.
- **Zero Race Conditions**: No background timers or event hooks needed.

---

## 3. Borderless Window Dragging & Resizing in WebView2

### The Challenge
Calling standard Win32 `ReleaseCapture()` + `SendMessage(WM_NCLBUTTONDOWN, HTCAPTION)` from C# fails when triggered from a web page in WebView2.

### Why It Failed
Chromium (`msedgewebview2.exe`) runs as a separate out-of-process renderer. When the user clicks on the HTML header, Chromium captures the mouse. Calling `ReleaseCapture()` in `GhostPad.exe` only releases capture for the C# thread, not the Chromium process. Thus, Windows ignores `HTCAPTION`.

### The Winning Solution: Screen Coordinate Delta Streaming
Instead of fighting Win32 mouse capture across processes, track physical screen coordinates (`e.screenX`, `e.screenY`) in JavaScript and stream the deltas to C#:

```javascript
// ui.html (JavaScript)
let isDragging = false;
let dragStartX = 0, dragStartY = 0;

function onDragStart(e) {
    if (isLocked || e.button !== 0) return;
    if (e.target.closest('button') || e.target.closest('input')) return;

    isDragging = true;
    dragStartX = e.screenX;
    dragStartY = e.screenY;
    window.addEventListener('mousemove', onDragMove);
    window.addEventListener('mouseup', onDragEnd);
}

function onDragMove(e) {
    if (!isDragging) return;
    const dx = e.screenX - dragStartX;
    const dy = e.screenY - dragStartY;
    if (dx !== 0 || dy !== 0) {
        dragStartX = e.screenX;
        dragStartY = e.screenY;
        postToHost({ action: 'moveDelta', dx: dx, dy: dy });
    }
}
```

```csharp
// app.cs (C#)
case "moveDelta":
    if (!isLocked)
    {
        int dx = root.GetProperty("dx").GetInt32();
        int dy = root.GetProperty("dy").GetInt32();
        this.BeginInvoke((Action)(() =>
        {
            this.Location = new Point(this.Left + dx, this.Top + dy);
        }));
    }
    break;
```
*Note: The exact same delta pattern powers the corner resize grip (`resizeDelta`), allowing smooth scaling without OS border flicker.*

---

## 4. Crisp High-DPI Rendering (Fixing Blurry UI)

On monitors running fractional display scaling (125%, 150%, 175%), Windows treats WinForms apps as "DPI Unaware" by default, rendering them at 96 DPI and stretching the bitmap, which creates a hazy, blurred look.

### The Fix
1. **In `Clock.csproj` / `GhostPad.csproj`**:
   ```xml
   <ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>
   ```
2. **In `app.cs`**:
   ```csharp
   Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
   ```
3. **In `app.manifest`**: Ensure Windows 10/11 compatibility GUID is enabled.
4. **In `ui.html`**:
   ```css
   -webkit-font-smoothing: antialiased;
   -moz-osx-font-smoothing: grayscale;
   text-rendering: optimizeLegibility;
   ```
5. **Use Vector SVGs**: Replace emoji characters and raster images with inline SVG paths. SVGs scale mathematically with subpixel antialiasing at any resolution.

---

## 5. Alt+Tab Exclusion

To make the widget completely invisible to the Windows Alt+Tab task switcher:
```csharp
protected override CreateParams CreateParams
{
    get
    {
        CreateParams cp = base.CreateParams;
        cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
        return cp;
    }
}
```
`WS_EX_TOOLWINDOW` instructs the DWM task switcher to omit the window from the Alt+Tab cycle while still allowing mouse focus and keyboard input.

---

## 6. Smart Editor & Multi-Note Storage Model

### Local Persistence
- **Speed**: Notes are saved in `localStorage` inside the Chromium profile for instant, lag-free restoration.
- **Backup**: Every change is debounced (250ms) and streamed to C#, which persists to:
  ```text
  %LOCALAPPDATA%\GhostPad\notes.json
  ```
  This ensures notes are stored as plain, human-readable JSON files that outlive browser cache clearing.

### Typing Ergonomics
1. **Tab Handling**: Standard browser `<textarea>` loses focus on <kbd>Tab</kbd>. We intercept `keydown`:
   ```javascript
   if (e.key === 'Tab') {
       e.preventDefault();
       noteContent.setRangeText('  ', start, end, 'end');
   }
   ```
2. **Smart List Continuation**: Intercepts <kbd>Enter</kbd> using regex:
   - Bullet lists (`- `, `* `)
   - Numbered lists (`1. `, `2. `)
   - Interactive Checkboxes (`- [ ] `, `- [x] `)
   Pressing Enter on an empty bullet line clears the bullet and restores a standard newline.

---

## 7. Packaging & Web Distribution Blueprint

### Self-Contained Binary
To allow anyone to download and run the application without installing the .NET 10 Desktop Runtime:
```powershell
& "C:\Program Files\dotnet\dotnet.exe" publish -c Release -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o ".\publish-package"
```

### Inno Setup (`installer.iss`)
- Packages `GhostPad.exe`, `ui.html`, and `WebView2Loader.dll`.
- Per-user installation (`PrivilegesRequired=lowest`) avoids UAC administrator prompts.
- Registers shortcuts in Start Menu and Desktop.
- Optional task to add a shortcut to `{userstartup}` for automatic launch on boot.
- Registers standard uninstaller in Windows *Settings > Installed apps*.

---

## 8. Ultimate Minimalism & Zen Mode Pattern

To provide a distraction-free, borderless desktop scratchpad, GhostPad implements a customizable Zen Mode:

### 1. Auto-Hiding Slide Header
- **Normal state**: `#header` is positioned absolutely at the top and translated off-screen:
  ```css
  .zen-header header {
      position: absolute;
      top: 0; left: 0; right: 0;
      transform: translateY(-100%);
      opacity: 0;
      pointer-events: none;
      transition: transform 0.24s cubic-bezier(0.16, 1, 0.3, 1), opacity 0.2s ease;
  }
  ```
- **Hover Reveal**: An invisible `#top-hover-trigger` strip (14px) at the very top of `#app` and tracking pointer coordinates `clientY <= 42` triggers the header to smoothly slide down.
- **Context Pinning**: The header is automatically pinned visible whenever navigating the All Notes list, the Settings view, or when the hamburger menu is opened.

### 2. Titleless Scratchpad Mode
- Hiding `#note-title` transforms the pad into a raw writing surface where typing starts from the very top.
- Pressing `+ New Note` automatically drops keyboard focus directly into the editor body without prompting for or requiring a title.
- Header title decouples from note content and defaults cleanly to `GhostPad`.

### 3. Smart Content-Derived Note Titles
In the Notes Manager list, notes created without titles are given clean, context-rich display titles automatically parsed from their first non-empty line:
```javascript
function getNoteDisplayTitle(n) {
    if (n.title && n.title.trim()) return n.title.trim();
    if (!n.content || !n.content.trim()) return 'Untitled Note';
    const lines = n.content.trim().split('\n');
    for (let line of lines) {
        const clean = line.replace(/^(\s*[-*+]\s*(\[[ xX]\]\s*)?|\d+\.\s*)/, '').trim();
        if (clean) return clean.substring(0, 38);
    }
    return 'Untitled Note';
}
```
This ensures titleless notes are searchable, browsable, and beautifully indexed like modern note-taking tools (Apple Notes, Bear).

### 4. Native OS Window Corner Rounding Adjustability
- **Win32 Region Shaping (`SetWindowRgn`)**: Because a standard borderless WinForms window is a native rectangular surface, CSS `border-radius` alone leaves the native window's sharp corners underneath. We synchronize the slider directly to the native Win32 window region using `CreateRoundRectRgn(0, 0, Width + 1, Height + 1, radius * 2, radius * 2)` and `SetWindowRgn(this.Handle, hRgn, true)`.
- **True Transparency & Click-Through**: Outside the rounded region, the desktop wallpaper shows through directly and mouse clicks hit whatever desktop icons or windows sit behind the rounded corners.
- **Dynamic Slider**: Ranges from `0px` (completely sharp square corners, removing the region with `SetWindowRgn(hWnd, IntPtr.Zero, true)`) up to `32px` (floating pill shape).
- **Auto-Sync on Resize**: Every time the user drags the corner resize grip, `ApplyWindowRegion(cornerRadius)` recalculates the GDI region for the new dimensions in real time.
- **Dual Persistence**: Saved to `%LOCALAPPDATA%\GhostPad\config.json` in C# and `localStorage.ghostpad_corner_radius` in HTML.
