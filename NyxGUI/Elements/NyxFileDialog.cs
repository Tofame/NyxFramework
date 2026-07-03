using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NyxGui;

/// <summary>
/// Dialog mode for <see cref="NyxFileDialog"/>.
/// </summary>
public enum NyxFileDialogMode
{
	/// <summary>Prompt the user to select an existing file to open.</summary>
	Open,

	/// <summary>Prompt the user to specify a file path for saving.</summary>
	Save,
}

/// <summary>
/// Options passed to <see cref="NyxFileDialog.ShowAsync"/>.
/// </summary>
public sealed class NyxFileDialogOptions
{
	/// <summary>Dialog title bar text.</summary>
	public string Title { get; set; } = "Select File";

	/// <summary>
	/// Initial directory shown when the dialog opens.
	/// <c>null</c> uses the OS default (usually the last visited folder).
	/// </summary>
	public string? InitialDirectory { get; set; }

	/// <summary>
	/// Allowed file extensions without leading dot, e.g. <c>new[]{"png","jpg"}</c>.
	/// When <c>null</c> or empty, all files are accepted.
	/// </summary>
	public string[]? Extensions { get; set; }

	/// <summary>
	/// Human-readable label for the extension group shown in the dialog,
	/// e.g. <c>"Image files"</c>. Defaults to <c>"Files"</c> when not set.
	/// </summary>
	public string? FilterLabel { get; set; }

	/// <summary>Default file extension appended when the user omits one (Save mode only).</summary>
	public string? DefaultExtension { get; set; }
}

/// <summary>
/// Cross-platform file dialog helper. Launches the OS-native dialog asynchronously
/// on a background thread and returns the selected path, or <c>null</c> when the
/// user cancels.
/// <para>
/// No external NuGet dependencies are required. Each platform is served by a
/// standard command-line tool that ships with the OS:
/// <list type="bullet">
///   <item>Windows — PowerShell + WinForms <c>OpenFileDialog</c> / <c>SaveFileDialog</c></item>
///   <item>macOS — <c>osascript</c> (AppleScript, always available)</item>
///   <item>Linux — <c>zenity</c> (GNOME/GTK) with <c>kdialog</c> (KDE) as a fallback</item>
/// </list>
/// </para>
/// </summary>
public static class NyxFileDialog
{
	/// <summary>
	/// Opens an Open or Save file dialog asynchronously.
	/// Returns the chosen path, or <c>null</c> if the user cancelled or an error occurred.
	/// </summary>
	public static Task<string?> ShowAsync(
		NyxFileDialogMode mode = NyxFileDialogMode.Open,
		NyxFileDialogOptions? options = null)
	{
		options ??= new NyxFileDialogOptions();

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			return ShowWindowsAsync(mode, options);
		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			return ShowMacOsAsync(mode, options);
		return ShowLinuxAsync(mode, options);
	}

	/// <summary>Convenience: opens an <see cref="NyxFileDialogMode.Open"/> dialog.</summary>
	public static Task<string?> OpenFileAsync(NyxFileDialogOptions? options = null) =>
		ShowAsync(NyxFileDialogMode.Open, options);

	/// <summary>Convenience: opens a <see cref="NyxFileDialogMode.Save"/> dialog.</summary>
	public static Task<string?> SaveFileAsync(NyxFileDialogOptions? options = null) =>
		ShowAsync(NyxFileDialogMode.Save, options);

	// ── Helpers ───────────────────────────────────────────────────────

	/// <summary>
	/// Builds the Windows COMMDLG pipe filter string from <see cref="NyxFileDialogOptions.Extensions"/>.
	/// Result: <c>"Image files (*.png;*.jpg)|*.png;*.jpg|All Files (*.*)|*.*"</c>
	/// </summary>
	private static string BuildWindowsFilter(NyxFileDialogOptions options)
	{
		if (options.Extensions is not { Length: > 0 } exts)
			return "All Files (*.*)|*.*";

		var normalized = Array.ConvertAll(exts, e => e.TrimStart('.').ToLowerInvariant());
		var patterns = string.Join(";", Array.ConvertAll(normalized, e => $"*.{e}"));
		var label = string.IsNullOrEmpty(options.FilterLabel)
			? "Files"
			: options.FilterLabel!;

		return $"{label} ({patterns})|{patterns}|All Files (*.*)|*.*";
	}

	// ── Windows ───────────────────────────────────────────────────────

	private static Task<string?> ShowWindowsAsync(NyxFileDialogMode mode, NyxFileDialogOptions options)
	{
		// Run entirely on a thread-pool thread so the dialog's STA pump does not
		// block the game/render loop. PowerShell spawns its own STA internally.
		return Task.Run(() =>
		{
			var dialogType = mode == NyxFileDialogMode.Save
				? "System.Windows.Forms.SaveFileDialog"
				: "System.Windows.Forms.OpenFileDialog";

			var script = BuildWindowsPsScript(dialogType, options);

			var psi = new ProcessStartInfo
			{
				FileName = "powershell.exe",
				Arguments = $"-NoProfile -NonInteractive -Command {QuotePs(script)}",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};

			using var proc = Process.Start(psi);
			if (proc is null) return null;

			var output = proc.StandardOutput.ReadToEnd().Trim();
			proc.WaitForExit();

			return string.IsNullOrEmpty(output) || proc.ExitCode != 0 ? null : output;
		});
	}

	private static string BuildWindowsPsScript(string dialogType, NyxFileDialogOptions options)
	{
		var sb = new System.Text.StringBuilder();
		sb.Append("[System.Reflection.Assembly]::LoadWithPartialName('System.Windows.Forms')|Out-Null;");
		sb.Append($"$d=New-Object {dialogType};");
		sb.Append($"$d.Title='{Ps(options.Title)}';");

		if (!string.IsNullOrEmpty(options.InitialDirectory))
			sb.Append($"$d.InitialDirectory='{Ps(options.InitialDirectory!)}';");

		sb.Append($"$d.Filter='{Ps(BuildWindowsFilter(options))}';");

		if (!string.IsNullOrEmpty(options.DefaultExtension))
			sb.Append($"$d.DefaultExt='{Ps(options.DefaultExtension!.TrimStart('.'))}';");

		sb.Append("if($d.ShowDialog()-eq'OK'){$d.FileName}");
		return sb.ToString();
	}

	/// <summary>Escapes single-quotes for use inside PowerShell single-quoted strings.</summary>
	private static string Ps(string s) => s.Replace("'", "''");

	/// <summary>Wraps the script in a PowerShell -Command argument.</summary>
	private static string QuotePs(string script) => $"\"{script.Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ")}\"";

	// ── macOS ─────────────────────────────────────────────────────────

	private static Task<string?> ShowMacOsAsync(NyxFileDialogMode mode, NyxFileDialogOptions options)
	{
		return Task.Run(() =>
		{
			string script;
			if (mode == NyxFileDialogMode.Save)
			{
				var name = !string.IsNullOrEmpty(options.DefaultExtension)
					? $"untitled.{options.DefaultExtension!.TrimStart('.')}"
					: "untitled";
				var dir = !string.IsNullOrEmpty(options.InitialDirectory)
					? $" default location \"{AppleEsc(options.InitialDirectory!)}\""
					: string.Empty;
				script = $"POSIX path of (choose file name with prompt \"{AppleEsc(options.Title)}\" default name \"{name}\"{dir})";
			}
			else
			{
				var dir = !string.IsNullOrEmpty(options.InitialDirectory)
					? $" default location \"{AppleEsc(options.InitialDirectory!)}\""
					: string.Empty;
				// Build of_type list from extensions.
				var ofType = BuildMacOsTypeList(options.Extensions);
				script = string.IsNullOrEmpty(ofType)
					? $"POSIX path of (choose file with prompt \"{AppleEsc(options.Title)}\"{dir})"
					: $"POSIX path of (choose file with prompt \"{AppleEsc(options.Title)}\" of type {{{ofType}}}{dir})";
			}

			return RunProcess("osascript", $"-e \"{script.Replace("\"", "\\\"")}\"");
		});
	}

	private static string BuildMacOsTypeList(string[]? extensions)
	{
		if (extensions is not { Length: > 0 }) return string.Empty;
		// of type {"png","jpg"} — osascript expects quoted comma-separated list.
		var quoted = Array.ConvertAll(extensions, e => $"\\\"{e.TrimStart('.')}\\\"");
		return string.Join(",", quoted);
	}

	private static string AppleEsc(string s) => s.Replace("\"", "\\\"").Replace("'", "\\'");

	// ── Linux ─────────────────────────────────────────────────────────

	private static Task<string?> ShowLinuxAsync(NyxFileDialogMode mode, NyxFileDialogOptions options)
	{
		return Task.Run(() =>
		{
			// Try zenity (GTK/GNOME) first, then kdialog (KDE).
			return TryZenity(mode, options) ?? TryKdialog(mode, options);
		});
	}

	private static string? TryZenity(NyxFileDialogMode mode, NyxFileDialogOptions options)
	{
		var sb = new System.Text.StringBuilder("--file-selection");
		if (mode == NyxFileDialogMode.Save) sb.Append(" --save --confirm-overwrite");
		if (!string.IsNullOrEmpty(options.Title)) sb.Append($" --title={ShellQuote(options.Title)}");
		if (!string.IsNullOrEmpty(options.InitialDirectory)) sb.Append($" --filename={ShellQuote(options.InitialDirectory!)}");

		// zenity --file-filter="label | *.ext *.ext2"
		if (options.Extensions is { Length: > 0 } exts)
		{
			var label = string.IsNullOrEmpty(options.FilterLabel) ? "Files" : options.FilterLabel!;
			var patterns = string.Join(" ", Array.ConvertAll(exts, e => $"*.{e.TrimStart('.')}"));
			sb.Append($" --file-filter={ShellQuote($"{label} | {patterns}")}");
			sb.Append($" --file-filter={ShellQuote("All Files | *")}");
		}

		return RunProcess("zenity", sb.ToString());
	}

	private static string? TryKdialog(NyxFileDialogMode mode, NyxFileDialogOptions options)
	{
		var verb = mode == NyxFileDialogMode.Save ? "--getsavefilename" : "--getopenfilename";
		var startDir = string.IsNullOrEmpty(options.InitialDirectory) ? "." : options.InitialDirectory!;

		// kdialog filter: "Description (*.ext *.ext2)" space-separated.
		var filter = BuildKdialogFilter(options);

		var args = $"{verb} {ShellQuote(startDir)} {ShellQuote(filter)}";
		if (!string.IsNullOrEmpty(options.Title)) args += $" --title {ShellQuote(options.Title)}";

		return RunProcess("kdialog", args);
	}

	private static string BuildKdialogFilter(NyxFileDialogOptions options)
	{
		if (options.Extensions is not { Length: > 0 } exts) return "*";
		var label = string.IsNullOrEmpty(options.FilterLabel) ? "Files" : options.FilterLabel!;
		var patterns = string.Join(" ", Array.ConvertAll(exts, e => $"*.{e.TrimStart('.')}"));
		return $"{label} ({patterns})";
	}

	/// <summary>Wraps a string in single-quotes for a POSIX shell argument.</summary>
	private static string ShellQuote(string s) => $"'{s.Replace("'", "'\\''")}'";

	// ── Process runner ────────────────────────────────────────────────

	private static string? RunProcess(string exe, string args)
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = exe,
				Arguments = args,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};

			using var proc = Process.Start(psi);
			if (proc is null) return null;

			var output = proc.StandardOutput.ReadToEnd().Trim();
			proc.WaitForExit();
			return proc.ExitCode == 0 && !string.IsNullOrEmpty(output) ? output : null;
		}
		catch
		{
			// Tool not found or execution failed — return null so caller can try fallback.
			return null;
		}
	}
}
