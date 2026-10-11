using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using WinTweaker.Models;

namespace WinTweaker.Engine
{
    public static class SystemOps
    {
        public static RegistryKey OpenHive(Hive hive)
        {
            var view = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;
            switch (hive)
            {
                case Hive.Cu:
                    return RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
                case Hive.Lm:
                    return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                default:
                    return RegistryKey.OpenBaseKey(RegistryHive.Users, view);
            }
        }

        public static string HiveName(Hive hive)
        {
            switch (hive)
            {
                case Hive.Cu: return "HKCU";
                case Hive.Lm: return "HKLM";
                default: return "HKU";
            }
        }

        public static bool TryReadDword(Hive hive, string path, string name, out int value, out bool existed)
        {
            value = 0;
            existed = false;
            try
            {
                using (var root = OpenHive(hive))
                using (var key = root.OpenSubKey(path, false))
                {
                    if (key == null)
                        return true;
                    var raw = key.GetValue(name);
                    if (raw == null)
                        return true;
                    existed = true;
                    value = Convert.ToInt32(raw, CultureInfo.InvariantCulture);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public sealed class PreviousValue
        {
            public bool Exists;
            public RegistryValueKind Kind;
            public object Data;
        }

        public static PreviousValue ReadValue(Hive hive, string path, string name)
        {
            var previous = new PreviousValue();
            using (var root = OpenHive(hive))
            using (var key = root.OpenSubKey(path, false))
            {
                if (key == null)
                    return previous;
                var data = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                if (data == null)
                {
                    try
                    {
                        key.GetValueKind(name);
                        previous.Exists = true;
                        previous.Kind = RegistryValueKind.String;
                        previous.Data = "";
                    }
                    catch (IOException)
                    {
                        previous.Exists = false;
                    }
                    return previous;
                }
                previous.Exists = true;
                previous.Kind = key.GetValueKind(name);
                previous.Data = data;
                return previous;
            }
        }

        public static void Write(RegOp op)
        {
            if (op.Kind == RegKind.DeleteKey)
            {
                using (var root = OpenHive(op.Hive))
                    root.DeleteSubKeyTree(op.Key, false);
                return;
            }

            using (var root = OpenHive(op.Hive))
            using (var key = root.CreateSubKey(op.Key))
            {
                if (key == null)
                    throw new InvalidOperationException("Could not open " + HiveName(op.Hive) + "\\" + op.Key);

                switch (op.Kind)
                {
                    case RegKind.Dword:
                        key.SetValue(op.Name ?? "", op.Dword, RegistryValueKind.DWord);
                        break;
                    case RegKind.String:
                        key.SetValue(op.Name ?? "", op.Text ?? "", RegistryValueKind.String);
                        break;
                    case RegKind.Binary:
                        key.SetValue(op.Name ?? "", op.Bytes ?? new byte[0], RegistryValueKind.Binary);
                        break;
                    case RegKind.DeleteValue:
                        key.DeleteValue(op.Name ?? "", false);
                        break;
                }
            }

            if (op.Kind == RegKind.DeleteValue)
            {
                using (var root = OpenHive(op.Hive))
                    Prune(root, op.Key);
            }
        }

        public static void RestoreValue(Hive hive, string path, string name, RegistryValueKind kind, object data)
        {
            using (var root = OpenHive(hive))
            using (var key = root.CreateSubKey(path))
            {
                if (key == null)
                    throw new InvalidOperationException("Could not open " + HiveName(hive) + "\\" + path);
                key.SetValue(name ?? "", data, kind);
            }
        }

        public static void DeleteValue(Hive hive, string path, string name)
        {
            using (var root = OpenHive(hive))
            using (var key = root.OpenSubKey(path, true))
            {
                if (key == null)
                    return;
                key.DeleteValue(name ?? "", false);
                Prune(root, path);
            }
        }

        static void Prune(RegistryKey root, string path)
        {
            using (var key = root.OpenSubKey(path, true))
            {
                if (key == null)
                    return;
                if (key.ValueCount != 0 || key.SubKeyCount != 0)
                    return;
            }

            int slash = path.LastIndexOf('\\');
            if (slash <= 0)
                return;
            string parent = path.Substring(0, slash);
            string leaf = path.Substring(slash + 1);
            using (var parentKey = root.OpenSubKey(parent, true))
            {
                if (parentKey == null)
                    return;
                try
                {
                    parentKey.DeleteSubKey(leaf, false);
                }
                catch
                {
                }
            }
        }

        public static bool ExportKey(Hive hive, string path, string file, out string error)
        {
            error = null;
            string full = HiveName(hive) + "\\" + path;
            string args = "export \"" + full + "\" \"" + file + "\" /y";
            int code = Run(Path.Combine(Environment.SystemDirectory, "reg.exe"), args, null, out string stdout, out string stderr);
            if (code == 0)
                return true;
            error = FirstLine(stderr, stdout, "reg export failed (" + code + ")");
            return false;
        }

        public static bool ImportKey(string file, out string error)
        {
            error = null;
            int code = Run(Path.Combine(Environment.SystemDirectory, "reg.exe"), "import \"" + file + "\"", null, out string stdout, out string stderr);
            if (code == 0)
                return true;
            error = FirstLine(stderr, stdout, "reg import failed (" + code + ")");
            return false;
        }

        public static bool TrySetServiceStart(string name, int start, out string error)
        {
            error = null;
            bool exists;
            QueryServiceStart(name, out exists);
            if (!exists)
            {
                error = name + " is not installed";
                return false;
            }

            string mode = StartMode(start);
            string args = "config " + name + " start= " + mode;
            int code = Run(Path.Combine(Environment.SystemDirectory, "sc.exe"), args, null, out string stdout, out string stderr);
            if (code != 0)
            {
                try
                {
                    using (var root = OpenHive(Hive.Lm))
                    using (var key = root.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + name, true))
                    {
                        if (key == null)
                        {
                            error = FirstLine(stderr, stdout, "sc config failed (" + code + ")");
                            return false;
                        }
                        key.SetValue("Start", start, RegistryValueKind.DWord);
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    return false;
                }
            }

            if (start == 4)
            {
                int stop = Run(Path.Combine(Environment.SystemDirectory, "sc.exe"), "stop " + name, null, out _, out _);
                if (stop != 0 && stop != 1062)
                    error = name + " start type is disabled, but the service did not stop (" + stop + ")";
            }

            return true;
        }

        public static int QueryServiceStart(string name, out bool exists)
        {
            exists = false;
            try
            {
                using (var root = OpenHive(Hive.Lm))
                using (var key = root.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + name, false))
                {
                    if (key == null)
                        return 0;
                    var raw = key.GetValue("Start");
                    if (raw == null)
                        return 0;
                    exists = true;
                    return Convert.ToInt32(raw, CultureInfo.InvariantCulture);
                }
            }
            catch
            {
                return 0;
            }
        }

        public static string StartMode(int start)
        {
            switch (start)
            {
                case 0: return "boot";
                case 1: return "system";
                case 2: return "auto";
                case 3: return "demand";
                default: return "disabled";
            }
        }

        public static void ApplyMouse(int threshold1, int threshold2, int speed)
        {
            int[] values = { threshold1, threshold2, speed };
            SystemParametersInfo(0x0004, 0, values, 0x0001 | 0x0002);
        }

        public static void ApplyMouseFromRegistry()
        {
            int t1 = ReadMouse("MouseThreshold1", 6);
            int t2 = ReadMouse("MouseThreshold2", 10);
            int speed = ReadMouse("MouseSpeed", 1);
            ApplyMouse(t1, t2, speed);
        }

        static int ReadMouse(string name, int fallback)
        {
            try
            {
                using (var root = OpenHive(Hive.Cu))
                using (var key = root.OpenSubKey(@"Control Panel\Mouse", false))
                {
                    if (key == null)
                        return fallback;
                    var raw = key.GetValue(name) as string;
                    int parsed;
                    if (raw != null && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                        return parsed;
                    return fallback;
                }
            }
            catch
            {
                return fallback;
            }
        }

        public static void BroadcastSettings()
        {
            IntPtr result;
            SendMessageTimeout(new IntPtr(0xffff), 0x001A, IntPtr.Zero, "Policy", 0x0002, 1500, out result);
        }

        public static void RestartExplorer()
        {
            foreach (var process in Process.GetProcessesByName("explorer"))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(4000);
                }
                catch
                {
                }
            }

            System.Threading.Thread.Sleep(600);
            if (Process.GetProcessesByName("explorer").Length == 0)
            {
                string explorer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
                Process.Start(explorer);
            }
        }

        public static void CreateRestorePoint(IProgress<string> log)
        {
            log.Report("restore point: creating (this can take a minute)");
            int? previous = null;
            bool hadFrequency = false;
            const string path = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore";
            try
            {
                int current;
                bool existed;
                if (TryReadDword(Hive.Lm, path, "PointCreationFrequency", out current, out existed) && existed)
                {
                    hadFrequency = true;
                    previous = current;
                }
                Write(RegOp.D(Hive.Lm, path, "PointCreationFrequency", 0));
            }
            catch (Exception ex)
            {
                log.Report("restore point: could not clear the 24h limit (" + ex.Message + ")");
            }

            string script = "$ErrorActionPreference = 'Stop'\r\n" +
                            "try {\r\n" +
                            "  Checkpoint-Computer -Description 'WinTweaker' -RestorePointType 'MODIFY_SETTINGS'\r\n" +
                            "  Write-Output 'OK'\r\n" +
                            "} catch {\r\n" +
                            "  Write-Output ('FAIL ' + $_.Exception.Message)\r\n" +
                            "}\r\n";
            int code = RunPowerShell(script, log);
            if (code != 0)
                log.Report("restore point: powershell exited " + code);

            try
            {
                if (hadFrequency && previous.HasValue)
                    Write(RegOp.D(Hive.Lm, path, "PointCreationFrequency", previous.Value));
                else
                    DeleteValue(Hive.Lm, path, "PointCreationFrequency");
            }
            catch
            {
            }
        }

        public static int RunPowerShell(string script, IProgress<string> log)
        {
            string file = Path.Combine(Path.GetTempPath(), "wintweaker-" + Guid.NewGuid().ToString("N") + ".ps1");
            File.WriteAllText(file, script, new UTF8Encoding(true));
            try
            {
                string exe = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
                return Run(exe, "-NoProfile -NonInteractive -ExecutionPolicy Bypass -File \"" + file + "\"", log, out _, out _);
            }
            finally
            {
                try { File.Delete(file); } catch { }
            }
        }

        public static int Run(string exe, string args, IProgress<string> log, out string stdout, out string stderr)
        {
            var outBuilder = new StringBuilder();
            var errBuilder = new StringBuilder();
            var start = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = new Process())
            {
                process.StartInfo = start;
                process.OutputDataReceived += (s, e) =>
                {
                    if (e.Data == null)
                        return;
                    outBuilder.AppendLine(e.Data);
                    if (log != null)
                        log.Report(e.Data);
                };
                process.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data == null)
                        return;
                    errBuilder.AppendLine(e.Data);
                    if (log != null)
                        log.Report(e.Data);
                };
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();
                stdout = outBuilder.ToString();
                stderr = errBuilder.ToString();
                return process.ExitCode;
            }
        }

        static string FirstLine(string stderr, string stdout, string fallback)
        {
            string text = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            if (string.IsNullOrWhiteSpace(text))
                return fallback;
            int cut = text.IndexOfAny(new[] { '\r', '\n' });
            return cut < 0 ? text.Trim() : text.Substring(0, cut).Trim();
        }

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo", SetLastError = true)]
        static extern bool SystemParametersInfo(uint action, uint param, int[] values, uint winIni);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern IntPtr SendMessageTimeout(IntPtr hwnd, int msg, IntPtr wParam, string lParam, int flags, int timeout, out IntPtr result);
    }
}
