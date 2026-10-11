using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Windows;

namespace WinTweaker.Engine
{
    public static class PackageOps
    {
        public static void RemoveApps(IList<string> packages, IProgress<string> log)
        {
            if (packages == null || packages.Count == 0)
                return;

            var script = new StringBuilder();
            script.AppendLine("$ErrorActionPreference = 'Continue'");
            script.AppendLine("$names = @(");
            for (int i = 0; i < packages.Count; i++)
            {
                string name = packages[i];
                if (name.IndexOf('\'') >= 0 || name.IndexOf('"') >= 0)
                {
                    log.Report("apps: skipped " + name + " (bad package name)");
                    continue;
                }
                script.AppendLine("  '" + name + "'");
            }
            script.AppendLine(")");
            script.AppendLine("foreach ($n in $names) {");
            script.AppendLine("  $pkgs = @(Get-AppxPackage -AllUsers -Name $n -ErrorAction SilentlyContinue)");
            script.AppendLine("  if ($pkgs.Count -eq 0) { Write-Output \"SKIP $n not installed\"; continue }");
            script.AppendLine("  foreach ($p in $pkgs) {");
            script.AppendLine("    try {");
            script.AppendLine("      Remove-AppxPackage -Package $p.PackageFullName -AllUsers -ErrorAction Stop");
            script.AppendLine("      Write-Output \"OK $n\"");
            script.AppendLine("    } catch {");
            script.AppendLine("      try {");
            script.AppendLine("        Remove-AppxPackage -Package $p.PackageFullName -ErrorAction Stop");
            script.AppendLine("        Write-Output \"OK $n\"");
            script.AppendLine("      } catch {");
            script.AppendLine("        Write-Output (\"FAIL $n \" + $_.Exception.Message)");
            script.AppendLine("      }");
            script.AppendLine("    }");
            script.AppendLine("  }");
            script.AppendLine("  $prov = @(Get-AppxProvisionedPackage -Online | Where-Object { $_.DisplayName -eq $n })");
            script.AppendLine("  foreach ($p in $prov) {");
            script.AppendLine("    try {");
            script.AppendLine("      Remove-AppxProvisionedPackage -Online -PackageName $p.PackageName -ErrorAction Stop | Out-Null");
            script.AppendLine("      Write-Output \"OKPROV $n\"");
            script.AppendLine("    } catch {");
            script.AppendLine("      Write-Output (\"FAILPROV $n \" + $_.Exception.Message)");
            script.AppendLine("    }");
            script.AppendLine("  }");
            script.AppendLine("}");

            log.Report("apps: removing " + packages.Count + " package(s)");
            SystemOps.RunPowerShell(script.ToString(), log);
            log.Report("apps: package removal is not reverted by undo");
        }

        public static void KillWidgetProcesses()
        {
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    if (process.ProcessName.StartsWith("Widget", StringComparison.OrdinalIgnoreCase))
                    {
                        process.Kill();
                        process.WaitForExit(2000);
                    }
                }
                catch
                {
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        public static bool DenyStoreSuggestions(string backupDir, Action<string> undoLine, IProgress<string> log)
        {
            string packageDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Packages\Microsoft.WindowsStore_8wekyb3d8bbwe");
            if (!Directory.Exists(packageDir))
            {
                log.Report("store suggestions: Store package folder is not on this profile");
                return false;
            }

            string dir = Path.Combine(packageDir, "LocalState");
            string file = Path.Combine(dir, "store.db");
            Directory.CreateDirectory(dir);
            bool created = !File.Exists(file);
            if (created)
            {
                using (File.Create(file))
                {
                }
            }

            var info = new FileInfo(file);
            var acl = info.GetAccessControl();
            var world = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            acl.AddAccessRule(new FileSystemAccessRule(world, FileSystemRights.FullControl, AccessControlType.Deny));
            info.SetAccessControl(acl);
            undoLine("STORE\t" + (created ? "1" : "0") + "\t" + file);
            log.Report("store suggestions: denied write access to store.db");
            return true;
        }

        public static void UndoStore(string createdFlag, string file, IProgress<string> log)
        {
            if (!File.Exists(file))
            {
                log.Report("undo store: file already gone");
                return;
            }

            try
            {
                TakeOwn(file);
                var info = new FileInfo(file);
                var acl = info.GetAccessControl();
                var world = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                var rules = acl.GetAccessRules(true, true, typeof(SecurityIdentifier));
                foreach (FileSystemAccessRule rule in rules)
                {
                    if (rule.AccessControlType != AccessControlType.Deny)
                        continue;
                    if (!world.Equals(rule.IdentityReference))
                        continue;
                    acl.RemoveAccessRuleSpecific(rule);
                }
                info.SetAccessControl(acl);
            }
            catch (Exception ex)
            {
                log.Report("undo store: " + ex.Message);
            }

            if (createdFlag == "1")
            {
                try
                {
                    File.Delete(file);
                    log.Report("undo store: removed the file this app created");
                }
                catch (Exception ex)
                {
                    log.Report("undo store: could not delete " + file + " (" + ex.Message + ")");
                }
            }
            else
            {
                log.Report("undo store: deny rule removed");
            }
        }

        static void TakeOwn(string file)
        {
            string takeown = Path.Combine(Environment.SystemDirectory, "takeown.exe");
            string icacls = Path.Combine(Environment.SystemDirectory, "icacls.exe");
            SystemOps.Run(takeown, "/F \"" + file + "\" /A", null, out _, out _);
            SystemOps.Run(icacls, "\"" + file + "\" /grant *S-1-5-32-544:F /C", null, out _, out _);
        }

        public static bool ClearStartPins(string backupDir, Action<string> undoLine, IProgress<string> log)
        {
            var uri = new Uri("pack://application:,,,/Assets/start2.bin", UriKind.Absolute);
            var resource = Application.GetResourceStream(uri);
            if (resource == null)
            {
                log.Report("start pins: blank layout is missing from the build");
                return false;
            }

            string packageDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Packages\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy");
            if (!Directory.Exists(packageDir))
            {
                log.Report("start pins: Start menu package is not on this profile");
                resource.Stream.Dispose();
                return false;
            }

            string dir = Path.Combine(packageDir, "LocalState");
            Directory.CreateDirectory(dir);
            string target = Path.Combine(dir, "start2.bin");

            foreach (var process in Process.GetProcessesByName("StartMenuExperienceHost"))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(3000);
                }
                catch
                {
                }
            }

            bool existed = File.Exists(target);
            string backup = Path.Combine(backupDir, "start2.bin.bak");
            if (existed)
                File.Copy(target, backup, true);

            using (resource.Stream)
            using (var output = File.Create(target))
                resource.Stream.CopyTo(output);

            undoLine("START\t" + (existed ? "1" : "0") + "\t" + backup + "\t" + target);
            log.Report("start pins: replaced start2.bin with an empty layout");
            return true;
        }

        public static void UndoStart(string existed, string backup, string target, IProgress<string> log)
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("StartMenuExperienceHost"))
                {
                    try { process.Kill(); } catch { }
                }

                if (existed == "1" && File.Exists(backup))
                {
                    File.Copy(backup, target, true);
                    log.Report("undo start pins: restored " + target);
                }
                else if (File.Exists(target))
                {
                    File.Delete(target);
                    log.Report("undo start pins: removed the layout this app created");
                }
            }
            catch (Exception ex)
            {
                log.Report("undo start pins: " + ex.Message);
            }
        }
    }
}
