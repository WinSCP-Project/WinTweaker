using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.Win32;
using WinTweaker.Models;

namespace WinTweaker.Engine
{
    public static class ApplyEngine
    {
        public static string Root
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinTweaker"); }
        }

        public static string Apply(Plan plan, IProgress<string> log)
        {
            if (plan.Items.Count == 0 && plan.Packages.Count == 0)
            {
                log.Report("nothing selected");
                return null;
            }

            Directory.CreateDirectory(Root);
            string dir = Path.Combine(Root, "backups", DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
            Directory.CreateDirectory(dir);
            string undoPath = Path.Combine(dir, "undo.tsv");
            var undo = new StreamWriter(undoPath, false, new UTF8Encoding(false));
            undo.AutoFlush = true;
            undo.WriteLine("# WinTweaker undo 1");
            int changes = 0;

            try
            {
                if (plan.RestorePoint)
                {
                    try
                    {
                        SystemOps.CreateRestorePoint(log);
                    }
                    catch (Exception ex)
                    {
                        log.Report("restore point: " + ex.Message);
                    }
                }

                foreach (var item in plan.Items)
                {
                    log.Report("apply: " + item.Title);
                    try
                    {
                        if (item.MouseOff)
                            undo.WriteLine("MOUSE");

                        foreach (var op in item.Ops)
                        {
                            if (ApplyOp(op, dir, undo, log))
                                changes++;
                        }

                        foreach (var service in item.DisableServices)
                        {
                            if (ApplyService(service, 4, undo, log))
                                changes++;
                        }

                        foreach (var service in item.ManualServices)
                        {
                            if (ApplyService(service, 3, undo, log))
                                changes++;
                        }

                        if (item.MouseOff)
                        {
                            SystemOps.ApplyMouse(0, 0, 0);
                            log.Report("mouse: acceleration off");
                            changes++;
                        }

                        if (item.KillWidgets)
                            PackageOps.KillWidgetProcesses();

                        if (item.DenyStore)
                        {
                            if (PackageOps.DenyStoreSuggestions(dir, line => undo.WriteLine(line), log))
                                changes++;
                        }

                        if (item.ClearPins)
                        {
                            if (PackageOps.ClearStartPins(dir, line => undo.WriteLine(line), log))
                                changes++;
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Report("failed: " + item.Title + " (" + ex.Message + ")");
                    }
                }

                if (plan.Packages.Count > 0)
                {
                    PackageOps.RemoveApps(plan.Packages, log);
                    changes++;
                }

                SystemOps.BroadcastSettings();

                if (plan.RestartExplorer && changes > 0)
                {
                    log.Report("explorer: restarting");
                    SystemOps.RestartExplorer();
                }
            }
            finally
            {
                undo.Dispose();
            }

            if (changes == 0)
            {
                log.Report("nothing was written");
                try { Directory.Delete(dir, true); } catch { }
                return null;
            }

            Push(dir);
            log.Report("undo snapshot: " + dir);
            return dir;
        }

        public static bool UndoLast(IProgress<string> log)
        {
            string dir = Pop();
            if (dir == null)
            {
                log.Report("no undo snapshot");
                return false;
            }

            string undoPath = Path.Combine(dir, "undo.tsv");
            if (!File.Exists(undoPath))
            {
                log.Report("undo file missing: " + undoPath);
                return false;
            }

            string[] lines = File.ReadAllLines(undoPath);
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
                    continue;
                try
                {
                    UndoLine(line, dir, log);
                }
                catch (Exception ex)
                {
                    log.Report("undo failed: " + ex.Message);
                }
            }

            SystemOps.BroadcastSettings();
            log.Report("undo finished");
            return true;
        }

        static bool ApplyOp(RegOp op, string dir, StreamWriter undo, IProgress<string> log)
        {
            if (op.Kind == RegKind.DeleteKey)
            {
                string file = Path.Combine(dir, "key-" + Guid.NewGuid().ToString("N") + ".reg");
                string error;
                if (!SystemOps.ExportKey(op.Hive, op.Key, file, out error))
                {
                    log.Report("key already absent or not exported: " + SystemOps.HiveName(op.Hive) + "\\" + op.Key);
                    return false;
                }
                undo.WriteLine("DELKEY\t" + Path.GetFileName(file));
                SystemOps.Write(op);
                return true;
            }

            var previous = SystemOps.ReadValue(op.Hive, op.Key, op.Name ?? "");
            if (!previous.Exists)
            {
                undo.WriteLine(string.Join("\t", new[] { "ABSENT", HiveToken(op.Hive), op.Key, op.Name ?? "" }));
            }
            else
            {
                undo.WriteLine(string.Join("\t", new[]
                {
                    "SET",
                    HiveToken(op.Hive),
                    op.Key,
                    op.Name ?? "",
                    KindToken(previous.Kind),
                    DataToken(previous.Kind, previous.Data)
                }));
            }

            SystemOps.Write(op);
            return true;
        }

        static bool ApplyService(string name, int start, StreamWriter undo, IProgress<string> log)
        {
            bool exists;
            int old = SystemOps.QueryServiceStart(name, out exists);
            if (!exists)
            {
                log.Report("service: " + name + " is not installed");
                return false;
            }

            undo.WriteLine("SVC\t" + name + "\t" + old.ToString(CultureInfo.InvariantCulture));
            string error;
            bool ok = SystemOps.TrySetServiceStart(name, start, out error);
            if (!ok)
            {
                log.Report("service: " + name + " " + error);
                return false;
            }
            if (!string.IsNullOrEmpty(error))
                log.Report("service: " + error);
            else
                log.Report("service: " + name + " -> " + SystemOps.StartMode(start));
            return true;
        }

        static void UndoLine(string line, string dir, IProgress<string> log)
        {
            string[] parts = line.Split('\t');
            switch (parts[0])
            {
                case "SET":
                    RestoreSet(parts);
                    break;
                case "ABSENT":
                    SystemOps.DeleteValue(ParseHive(parts[1]), parts[2], parts[3]);
                    break;
                case "DELKEY":
                    string error;
                    if (!SystemOps.ImportKey(Path.Combine(dir, parts[1]), out error))
                        log.Report("undo key: " + error);
                    break;
                case "SVC":
                    int start = int.Parse(parts[2], CultureInfo.InvariantCulture);
                    string serviceError;
                    SystemOps.TrySetServiceStart(parts[1], start, out serviceError);
                    if (!string.IsNullOrEmpty(serviceError))
                        log.Report("undo service: " + serviceError);
                    break;
                case "STORE":
                    PackageOps.UndoStore(parts[1], parts[2], log);
                    break;
                case "START":
                    PackageOps.UndoStart(parts[1], parts[2], parts[3], log);
                    break;
                case "MOUSE":
                    SystemOps.ApplyMouseFromRegistry();
                    log.Report("undo mouse: applied the restored curve");
                    break;
            }
        }

        static void RestoreSet(string[] parts)
        {
            Hive hive = ParseHive(parts[1]);
            string path = parts[2];
            string name = parts[3];
            string kind = parts[4];
            string data = parts.Length > 5 ? parts[5] : "";
            switch (kind)
            {
                case "DWORD":
                    SystemOps.RestoreValue(hive, path, name, RegistryValueKind.DWord, int.Parse(data, CultureInfo.InvariantCulture));
                    break;
                case "SZ":
                    SystemOps.RestoreValue(hive, path, name, RegistryValueKind.String, data);
                    break;
                case "EXPAND":
                    SystemOps.RestoreValue(hive, path, name, RegistryValueKind.ExpandString, data);
                    break;
                case "HEX":
                    SystemOps.RestoreValue(hive, path, name, RegistryValueKind.Binary, ParseHex(data));
                    break;
            }
        }

        static string KindToken(RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.DWord: return "DWORD";
                case RegistryValueKind.ExpandString: return "EXPAND";
                case RegistryValueKind.Binary: return "HEX";
                default: return "SZ";
            }
        }

        static string DataToken(RegistryValueKind kind, object data)
        {
            if (data == null)
                return "";
            switch (kind)
            {
                case RegistryValueKind.DWord:
                    return Convert.ToInt32(data, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                case RegistryValueKind.Binary:
                    var bytes = (byte[])data;
                    var hex = new StringBuilder();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        if (i > 0)
                            hex.Append(',');
                        hex.Append(bytes[i].ToString("X2", CultureInfo.InvariantCulture));
                    }
                    return hex.ToString();
                default:
                    return Convert.ToString(data, CultureInfo.InvariantCulture) ?? "";
            }
        }

        static byte[] ParseHex(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new byte[0];
            string[] parts = text.Split(',');
            var bytes = new byte[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                bytes[i] = byte.Parse(parts[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return bytes;
        }

        static string HiveToken(Hive hive)
        {
            switch (hive)
            {
                case Hive.Cu: return "CU";
                case Hive.Lm: return "LM";
                default: return "U";
            }
        }

        static Hive ParseHive(string token)
        {
            switch (token)
            {
                case "CU": return Hive.Cu;
                case "LM": return Hive.Lm;
                default: return Hive.Users;
            }
        }

        static void Push(string dir)
        {
            Directory.CreateDirectory(Root);
            File.AppendAllText(Path.Combine(Root, "stack.txt"), dir + Environment.NewLine, new UTF8Encoding(false));
        }

        static string Pop()
        {
            string stack = Path.Combine(Root, "stack.txt");
            if (!File.Exists(stack))
                return null;
            var lines = new List<string>(File.ReadAllLines(stack));
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
                lines.RemoveAt(lines.Count - 1);
            if (lines.Count == 0)
                return null;
            string last = lines[lines.Count - 1];
            lines.RemoveAt(lines.Count - 1);
            File.WriteAllLines(stack, lines.ToArray(), new UTF8Encoding(false));
            return last;
        }
    }
}
