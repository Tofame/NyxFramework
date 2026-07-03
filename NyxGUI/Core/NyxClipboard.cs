namespace NyxGui;

public static class NyxClipboard
{
	public static void SetText(string text)
	{
		if (System.OperatingSystem.IsWindows())
		{
			SetWindowsClipboardText(text);
		}
		else if (System.OperatingSystem.IsMacOS())
		{
			SetMacClipboardText(text);
		}
		else if (System.OperatingSystem.IsLinux())
		{
			SetLinuxClipboardText(text);
		}
	}

	public static string GetText()
	{
		if (System.OperatingSystem.IsWindows())
		{
			return GetWindowsClipboardText();
		}
		if (System.OperatingSystem.IsMacOS())
		{
			return GetMacClipboardText();
		}
		if (System.OperatingSystem.IsLinux())
		{
			return GetLinuxClipboardText();
		}
		return string.Empty;
	}

	// ── Windows Native Clipboard (Win32 API) ───────────────────────────────────

	[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
	private static extern bool OpenClipboard(System.IntPtr hWndNewOwner);

	[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
	private static extern bool CloseClipboard();

	[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
	private static extern bool EmptyClipboard();

	[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
	private static extern System.IntPtr SetClipboardData(uint uFormat, System.IntPtr hMem);

	[System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
	private static extern System.IntPtr GetClipboardData(uint uFormat);

	[System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
	private static extern System.IntPtr GlobalAlloc(uint uFlags, System.UIntPtr dwBytes);

	[System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
	private static extern System.IntPtr GlobalLock(System.IntPtr hMem);

	[System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
	[return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
	private static extern bool GlobalUnlock(System.IntPtr hMem);

	private const uint CF_UNICODETEXT = 13;

	private static void SetWindowsClipboardText(string text)
	{
		if (!OpenClipboard(System.IntPtr.Zero)) return;
		try
		{
			EmptyClipboard();
			var bytes = (text.Length + 1) * 2;
			var hGlobal = GlobalAlloc(0x0002 /* GMEM_MOVEABLE */, (System.UIntPtr)bytes);
			if (hGlobal == System.IntPtr.Zero) return;
			var target = GlobalLock(hGlobal);
			if (target == System.IntPtr.Zero) return;
			try
			{
				System.Runtime.InteropServices.Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
				System.Runtime.InteropServices.Marshal.WriteInt16(target, text.Length * 2, 0);
			}
			finally
			{
				GlobalUnlock(hGlobal);
			}
			SetClipboardData(CF_UNICODETEXT, hGlobal);
		}
		finally
		{
			CloseClipboard();
		}
	}

	private static string GetWindowsClipboardText()
	{
		if (!OpenClipboard(System.IntPtr.Zero)) return string.Empty;
		try
		{
			var hGlobal = GetClipboardData(CF_UNICODETEXT);
			if (hGlobal == System.IntPtr.Zero) return string.Empty;
			var target = GlobalLock(hGlobal);
			if (target == System.IntPtr.Zero) return string.Empty;
			try
			{
				return System.Runtime.InteropServices.Marshal.PtrToStringUni(target) ?? string.Empty;
			}
			finally
			{
				GlobalUnlock(hGlobal);
			}
		}
		finally
		{
			CloseClipboard();
		}
	}

	// ── macOS Clipboard (pbcopy / pbpaste) ──────────────────────────────────────

	private static void SetMacClipboardText(string text)
	{
		try
		{
			using var process = new System.Diagnostics.Process();
			process.StartInfo = new System.Diagnostics.ProcessStartInfo
			{
				FileName = "pbcopy",
				UseShellExecute = false,
				RedirectStandardInput = true,
				CreateNoWindow = true
			};
			process.Start();
			using (var writer = process.StandardInput)
			{
				writer.Write(text);
			}
			process.WaitForExit();
		}
		catch { }
	}

	private static string GetMacClipboardText()
	{
		try
		{
			using var process = new System.Diagnostics.Process();
			process.StartInfo = new System.Diagnostics.ProcessStartInfo
			{
				FileName = "pbpaste",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			};
			process.Start();
			var text = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			return text;
		}
		catch
		{
			return string.Empty;
		}
	}

	// ── Linux Clipboard (xclip / xsel) ──────────────────────────────────────────

	private static void SetLinuxClipboardText(string text)
	{
		try
		{
			using var process = new System.Diagnostics.Process();
			process.StartInfo = new System.Diagnostics.ProcessStartInfo
			{
				FileName = "xclip",
				Arguments = "-selection clipboard",
				UseShellExecute = false,
				RedirectStandardInput = true,
				CreateNoWindow = true
			};
			process.Start();
			using (var writer = process.StandardInput)
			{
				writer.Write(text);
			}
			process.WaitForExit();
		}
		catch
		{
			try
			{
				using var process = new System.Diagnostics.Process();
				process.StartInfo = new System.Diagnostics.ProcessStartInfo
				{
					FileName = "xsel",
					Arguments = "--clipboard --input",
					UseShellExecute = false,
					RedirectStandardInput = true,
					CreateNoWindow = true
				};
				process.Start();
				using (var writer = process.StandardInput)
				{
					writer.Write(text);
				}
				process.WaitForExit();
			}
			catch { }
		}
	}

	private static string GetLinuxClipboardText()
	{
		try
		{
			using var process = new System.Diagnostics.Process();
			process.StartInfo = new System.Diagnostics.ProcessStartInfo
			{
				FileName = "xclip",
				Arguments = "-selection clipboard -o",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				CreateNoWindow = true
			};
			process.Start();
			var text = process.StandardOutput.ReadToEnd();
			process.WaitForExit();
			return text;
		}
		catch
		{
			try
			{
				using var process = new System.Diagnostics.Process();
				process.StartInfo = new System.Diagnostics.ProcessStartInfo
				{
					FileName = "xsel",
					Arguments = "--clipboard --output",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					CreateNoWindow = true
				};
				process.Start();
				var text = process.StandardOutput.ReadToEnd();
				process.WaitForExit();
				return text;
			}
			catch
			{
				return string.Empty;
			}
		}
	}
}
