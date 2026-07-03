# NyxFileDialog — File Selection & Saving

`NyxFileDialog` is a cross-platform, zero-dependency file dialog system built into NyxGUI.
It covers two use cases:

| Use Case | C# Type | .nyxui Type |
|---|---|---|
| Open OS dialog from code | `NyxFileDialog` (static helper) | — |
| Button + path label widget | `NyxFileDialogButton` | `FileDialogButton` |

**Platform support:**

| OS | Backend | Requirement |
|---|---|---|
| Windows | PowerShell + WinForms dialog (STA thread) | Included in Windows |
| macOS | `osascript` (AppleScript) | Included in macOS |
| Linux | `zenity` → `kdialog` fallback | Install `zenity` or `kdialog` |

No NuGet packages are added. The dialog runs on a background thread so the render loop is never blocked.

---

## 1. Static Helper — `NyxFileDialog`

`NyxFileDialog.ShowAsync` is the lowest-level entry point.
It accepts a mode and an options bag, launches the OS dialog on a thread-pool thread,
and returns the chosen path (or `null` if the user cancelled).

```csharp
// Open dialog — all files
string? path = await NyxFileDialog.OpenFileAsync();

// Open dialog — filtered
string? path = await NyxFileDialog.OpenFileAsync(new NyxFileDialogOptions
{
    Title            = "Select an image",
    Extensions       = new[] { "png", "jpg", "jpeg", "bmp" },
    FilterLabel      = "Image files",
    InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
});

// Save dialog
string? savePath = await NyxFileDialog.SaveFileAsync(new NyxFileDialogOptions
{
    Title            = "Save as…",
    Extensions       = new[] { "txt" },
    FilterLabel      = "Text files",
    DefaultExtension = "txt",
});

if (path is not null)
    Console.WriteLine($"Selected: {path}");
```

### `NyxFileDialogOptions` Properties

| Property | Type | Description |
|---|---|---|
| `Title` | `string` | Dialog window title. Default: `"Select File"` |
| `Extensions` | `string[]?` | Allowed extensions without dot, e.g. `new[]{"png","jpg"}`. `null` = all files. |
| `FilterLabel` | `string?` | Human-readable group name shown in the filter dropdown, e.g. `"Image files"`. |
| `InitialDirectory` | `string?` | Starting directory. `null` = OS default (last visited folder). |
| `DefaultExtension` | `string?` | Extension appended when the user omits one (Save mode). |

### `NyxFileDialogMode`

- **`NyxFileDialogMode.Open`** (default):
  - Used to prompt the user to select an **existing file**. The user cannot enter or select a path that does not exist on disk.
  - Ideal for opening or loading assets, configuration settings, documents, or data.
- **`NyxFileDialogMode.Save`**:
  - Used to prompt the user to select or enter a path to **write/save** a file. The target file path does not need to exist yet.
  - If the user selects an existing file, the OS dialog automatically shows a confirmation dialog asking if they want to overwrite it.
  - Supports the `DefaultExtension` setting (e.g. `"txt"`). If the user types a file name without an extension (like `backup`), the dialog automatically appends the extension to yield `backup.txt`.
  - **Important:** The file dialog does *not* actually write or create files on disk itself. It only acts as an OS selection prompt and returns the chosen absolute path. Your C# code is responsible for receiving the returned path and writing the actual data using standard I/O APIs (such as `System.IO.File.WriteAllText` or `System.IO.File.WriteAllBytes`).

---

## 2. Compound Widget — `NyxFileDialogButton`

`NyxFileDialogButton` is a `NyxWidget` that combines a clickable button face with a
filename label. Clicking the button face opens the OS dialog; the selected filename
appears in the label zone to the right.

```
┌────────────────┐  ┌──────────────────────────────────────┐
│  Select file…  │  │  filename.png   (or "No file selected")│
└────────────────┘  └──────────────────────────────────────┘
  ← ButtonWidth →  Gap  ←──── remaining Bounds width ──────→
```

### C# Usage

```csharp
var btn = new NyxFileDialogButton("myFileBtn")
{
    ButtonLabel  = "Browse…",
    ButtonWidth  = 90,
    Mode         = NyxFileDialogMode.Open,
    DialogOptions = new NyxFileDialogOptions
    {
        Title       = "Open Sprite File",
        Extensions  = new[] { "spr", "dat", "png" },
        FilterLabel = "Client assets",
    },
};
btn.FixedHeight = 22;

// Subscribe to file selection
btn.FileSelected += (_, e) =>
{
    Console.WriteLine($"User picked: {e.Path}");
    // e.Path is the full absolute path
};

// btn.SelectedPath  — most recently selected path, or null
// btn.Mode          — Open / Save
// btn.ShowSelectedPath = false  — hide the path label
// btn.PlaceholderText  = "…"   — text when nothing selected
```

### .nyxui Declarative Usage

```toml
[myFileBtn]
type         = "FileDialogButton"
bounds       = { x = 10, y = 60, w = 340, h = 22 }
button_label = "Browse…"
button_width = 90
mode         = "open"
dialog_title = "Open Image"
extensions   = ["png", "jpg", "bmp"]
filter_label = "Image files"
placeholder  = "No image selected"
on_file_selected = "OnImagePicked"
anchors.left   = "parent.left"
anchors.right  = "parent.right"
anchors.top    = "parent.top"
```

Subscribe to the action in C# using `BindActions`:

```csharp
document.BindActions(new Dictionary<string, Action>
{
    ["OnImagePicked"] = () =>
    {
        var fileBtn = document.TryGetFileDialogButton("myFileBtn");
        if (fileBtn?.SelectedPath is { } path)
            LoadImage(path);
    },
});
```

Or access the widget directly:

```csharp
var fileBtn = document.TryGetFileDialogButton("myFileBtn");
fileBtn!.FileSelected += (_, e) => LoadImage(e.Path);
```

### `NyxFileDialogButton` Properties Summary

| Property | Type | Default | Description |
|---|---|---|---|
| `ButtonLabel` | `string` | `"Select file…"` | Text on the button face |
| `ButtonWidth` | `int` | `120` | Width of the button face in pixels |
| `Gap` | `int` | `6` | Gap between button and path label |
| `ShowSelectedPath` | `bool` | `true` | Show filename label to the right |
| `PlaceholderText` | `string` | `"No file selected"` | Text when nothing is chosen |
| `Mode` | `NyxFileDialogMode` | `Open` | `Open` or `Save` |
| `DialogOptions` | `NyxFileDialogOptions` | defaults | Options forwarded to the OS dialog |
| `SelectedPath` | `string?` | `null` | Full path of the last confirmed selection |

### Event

```csharp
event EventHandler<NyxFileSelectedEventArgs>? FileSelected;
// e.Path — the full absolute path the user confirmed
```

> **Note:** `FileSelected` may be raised from a background thread-pool thread
> (the dialog runs async). `NyxLabel.Text` is safe to write from any thread,
> but if you update other UI state you should marshal back to the game thread
> (e.g. via a `volatile bool` flag checked in the next frame).

---

## 3. Save Dialog Example

```csharp
var saveBtn = new NyxFileDialogButton
{
    ButtonLabel      = "Save log…",
    Mode             = NyxFileDialogMode.Save,
    ShowSelectedPath = false,      // button only, no path label
    DialogOptions    = new NyxFileDialogOptions
    {
        Title            = "Save log file",
        Extensions       = new[] { "txt", "log" },
        FilterLabel      = "Text files",
        DefaultExtension = "txt",
    },
};

saveBtn.FileSelected += async (_, e) =>
{
    await File.WriteAllTextAsync(e.Path, BuildLogContent());
    Console.WriteLine($"Saved to {e.Path}");
};
```

---

## 4. Linux Notes

`zenity` is the preferred backend and ships with most GNOME desktops.
On KDE / systems without `zenity`, `kdialog` is used as a fallback.

To install:
```sh
# Debian/Ubuntu
sudo apt install zenity

# Fedora
sudo dnf install zenity

# Arch
sudo pacman -S zenity
```

If neither tool is available, `ShowAsync` returns `null`.
