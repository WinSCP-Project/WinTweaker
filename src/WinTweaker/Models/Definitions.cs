using System;
using System.Collections.Generic;

namespace WinTweaker.Models
{
    [Flags]
    public enum OsTarget
    {
        Win7 = 1,
        Win10 = 2,
        Win11 = 4
    }

    public enum OsFamily
    {
        Windows7,
        Windows10,
        Windows11,
        Other
    }

    public enum Hive
    {
        Cu,
        Lm,
        Users
    }

    public enum RegKind
    {
        Dword,
        String,
        Binary,
        DeleteValue,
        DeleteKey
    }

    public sealed class RegOp
    {
        public Hive Hive;
        public string Key;
        public string Name;
        public RegKind Kind;
        public int Dword;
        public string Text;
        public byte[] Bytes;

        public static RegOp D(Hive hive, string key, string name, int value)
        {
            return new RegOp { Hive = hive, Key = key, Name = name, Kind = RegKind.Dword, Dword = value };
        }

        public static RegOp S(Hive hive, string key, string name, string value)
        {
            return new RegOp { Hive = hive, Key = key, Name = name, Kind = RegKind.String, Text = value };
        }

        public static RegOp Bin(Hive hive, string key, string name, byte[] value)
        {
            return new RegOp { Hive = hive, Key = key, Name = name, Kind = RegKind.Binary, Bytes = value };
        }

        public static RegOp Del(Hive hive, string key, string name)
        {
            return new RegOp { Hive = hive, Key = key, Name = name, Kind = RegKind.DeleteValue };
        }

        public static RegOp DelKey(Hive hive, string key)
        {
            return new RegOp { Hive = hive, Key = key, Kind = RegKind.DeleteKey };
        }
    }

    public sealed class ChoiceDef
    {
        public string Id;
        public string Label;
        public RegOp[] Ops;
        public bool ClearPins;

        public ChoiceDef(string id, string label, RegOp[] ops, bool clearPins)
        {
            Id = id;
            Label = label;
            Ops = ops ?? new RegOp[0];
            ClearPins = clearPins;
        }
    }

    public sealed class TweakDef
    {
        public string Id;
        public string CategoryId;
        public string Title;
        public string Description;
        public OsTarget Os;
        public bool IsDefault;
        public bool IsCombo;
        public RegOp[] Ops;
        public string[] DisableServices;
        public string[] ManualServices;
        public string[] Packages;
        public bool MouseOff;
        public bool DenyStore;
        public bool KillWidgets;
        public ChoiceDef[] Choices;
    }

    public sealed class AppDef
    {
        public string Package;
        public string Title;
        public string Description;
        public bool Common;

        public AppDef(string package, string title, string description, bool common)
        {
            Package = package;
            Title = title;
            Description = description;
            Common = common;
        }
    }

    public sealed class PlanItem
    {
        public string Title;
        public List<RegOp> Ops = new List<RegOp>();
        public List<string> DisableServices = new List<string>();
        public List<string> ManualServices = new List<string>();
        public List<string> Packages = new List<string>();
        public bool MouseOff;
        public bool DenyStore;
        public bool KillWidgets;
        public bool ClearPins;
    }

    public sealed class Plan
    {
        public bool RestorePoint;
        public bool RestartExplorer;
        public List<PlanItem> Items = new List<PlanItem>();
        public List<string> Packages = new List<string>();
    }
}
