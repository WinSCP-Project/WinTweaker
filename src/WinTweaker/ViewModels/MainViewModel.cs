using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WinTweaker.Catalog;
using WinTweaker.Engine;
using WinTweaker.Models;

namespace WinTweaker.ViewModels
{
    public sealed class ChoiceVm
    {
        public string Id { get; private set; }
        public string Label { get; private set; }
        public ChoiceDef Source { get; private set; }

        public ChoiceVm(ChoiceDef source)
        {
            Source = source;
            Id = source.Id;
            Label = source.Label;
        }
    }

    public sealed class TweakVm : NotifyBase
    {
        readonly TweakDef _source;
        bool _checked;
        bool _match = true;
        ChoiceVm _choice;

        public TweakVm(TweakDef source, bool available, string reason)
        {
            _source = source;
            Id = source.Id;
            Title = source.Title;
            Description = source.Description;
            IsCombo = source.IsCombo;
            IsDefault = source.IsDefault;
            IsAvailable = available;
            UnavailableReason = reason;
            Choices = new ObservableCollection<ChoiceVm>();
            foreach (var choice in source.Choices)
                Choices.Add(new ChoiceVm(choice));
            if (Choices.Count > 0)
                _choice = Choices[0];
        }

        public TweakDef Source { get { return _source; } }
        public string Id { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public bool IsCombo { get; private set; }
        public bool IsDefault { get; private set; }
        public bool IsAvailable { get; private set; }
        public string UnavailableReason { get; private set; }
        public ObservableCollection<ChoiceVm> Choices { get; private set; }

        public string ToolTipText
        {
            get { return IsAvailable ? Description : Description + "\n\n" + UnavailableReason; }
        }

        public bool IsChecked
        {
            get { return _checked; }
            set { Set(ref _checked, value); }
        }

        public ChoiceVm SelectedChoice
        {
            get { return _choice; }
            set { Set(ref _choice, value); }
        }

        public bool IsMatch
        {
            get { return _match; }
            set { Set(ref _match, value); }
        }
    }

    public sealed class CategoryVm : NotifyBase
    {
        public string Title { get; set; }
        public string Glyph { get; set; }
        public string Description { get; set; }
        public ObservableCollection<TweakVm> Items { get; private set; }

        public CategoryVm()
        {
            Items = new ObservableCollection<TweakVm>();
        }

        public bool HasVisibleItems
        {
            get
            {
                foreach (var item in Items)
                {
                    if (item.IsMatch)
                        return true;
                }
                return false;
            }
        }

        public void Refresh()
        {
            Raise(nameof(HasVisibleItems));
        }
    }

    public sealed class AppVm : NotifyBase
    {
        bool _checked;
        bool _match = true;

        public string Package { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool Common { get; set; }

        public bool IsChecked
        {
            get { return _checked; }
            set { Set(ref _checked, value); }
        }

        public bool IsMatch
        {
            get { return _match; }
            set { Set(ref _match, value); }
        }
    }

    public sealed class PageDot : NotifyBase
    {
        bool _active;

        public int Index { get; set; }

        public bool Active
        {
            get { return _active; }
            set { Set(ref _active, value); }
        }
    }

    public sealed class MainViewModel : NotifyBase
    {
        readonly List<TweakVm> _tweaks = new List<TweakVm>();
        readonly List<CategoryVm> _categories = new List<CategoryVm>();
        bool _suspend;
        bool _dirty;
        bool _applying;
        bool _applied;
        int _page = 2;
        string _search = "";
        string _appSearch = "";
        string _log = "";
        bool _restore = true;
        bool _restartExplorer = true;

        public MainViewModel()
        {
            Os = OsInfo.Detect();
            IsAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            AppsSupported = Os.Family == OsFamily.Windows10 || Os.Family == OsFamily.Windows11;
            AvailabilityNote = Os.Caption + ". Settings this version does not have are greyed out.";

            BuildTweaks();
            BuildApps();

            Dots = new ObservableCollection<PageDot>();
            for (int i = 0; i < 4; i++)
                Dots.Add(new PageDot { Index = i, Active = i == _page });

            LeftColumn = Column(0);
            MiddleColumn = Column(1);
            RightColumn = Column(2);
            ReviewItems = new ObservableCollection<string>();

            _suspend = true;
            SelectDefaults();
            _suspend = false;

            NextCommand = new RelayCommand(_ => Next(), _ => !_applying);
            BackCommand = new RelayCommand(_ => Back(), _ => _page > 0 && !_applying);
            GoPageCommand = new RelayCommand(GoPage, _ => !_applying);
            SelectDefaultCommand = new RelayCommand(_ => { SelectDefaults(); _dirty = true; });
            SelectLastCommand = new RelayCommand(_ => LoadLast());
            ClearCommand = new RelayCommand(_ => ClearTweaks());
            SelectCommonCommand = new RelayCommand(_ => SelectCommon());
            ClearAppsCommand = new RelayCommand(_ => ClearApps());
            ElevateCommand = new RelayCommand(_ => Elevate());
            UndoCommand = new RelayCommand(_ => Undo(), _ => !_applying);
            RestartExplorerCommand = new RelayCommand(_ => SystemOps.RestartExplorer(), _ => !_applying);
            AboutCommand = new RelayCommand(_ => About());
            CloseCommand = new RelayCommand(_ => Application.Current.Shutdown());
        }

        public OsInfo Os { get; private set; }
        public bool IsAdmin { get; private set; }
        public bool ShowAdminWarning { get { return !IsAdmin; } }
        public bool AppsSupported { get; private set; }
        public string AvailabilityNote { get; private set; }
        public ObservableCollection<CategoryVm> LeftColumn { get; private set; }
        public ObservableCollection<CategoryVm> MiddleColumn { get; private set; }
        public ObservableCollection<CategoryVm> RightColumn { get; private set; }
        public ObservableCollection<AppVm> Apps { get; private set; }
        public ObservableCollection<PageDot> Dots { get; private set; }
        public ObservableCollection<string> ReviewItems { get; private set; }

        public ICommand NextCommand { get; private set; }
        public ICommand BackCommand { get; private set; }
        public ICommand GoPageCommand { get; private set; }
        public ICommand SelectDefaultCommand { get; private set; }
        public ICommand SelectLastCommand { get; private set; }
        public ICommand ClearCommand { get; private set; }
        public ICommand SelectCommonCommand { get; private set; }
        public ICommand ClearAppsCommand { get; private set; }
        public ICommand ElevateCommand { get; private set; }
        public ICommand UndoCommand { get; private set; }
        public ICommand RestartExplorerCommand { get; private set; }
        public ICommand AboutCommand { get; private set; }
        public ICommand CloseCommand { get; private set; }

        public bool IsApplying
        {
            get { return _applying; }
            private set
            {
                if (!Set(ref _applying, value))
                    return;
                Raise(nameof(NextLabel));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public int Page
        {
            get { return _page; }
            set
            {
                if (value < 0 || value > 3 || _applying || value == _page)
                    return;
                if (_page == 3 && value != 3)
                    _applied = false;
                Set(ref _page, value);
                foreach (var dot in Dots)
                    dot.Active = dot.Index == value;
                Raise(nameof(NextLabel));
                if (value == 3)
                    RebuildReview();
            }
        }

        public string NextLabel
        {
            get
            {
                if (_page < 3)
                    return "Next  →";
                return _applied ? "Finish" : "Apply";
            }
        }

        public string SearchText
        {
            get { return _search; }
            set
            {
                string text = value ?? "";
                if (!Set(ref _search, text))
                    return;
                foreach (var tweak in _tweaks)
                {
                    tweak.IsMatch = text.Length == 0
                        || tweak.Title.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
                }
                foreach (var category in _categories)
                    category.Refresh();
            }
        }

        public string AppSearchText
        {
            get { return _appSearch; }
            set
            {
                string text = value ?? "";
                if (!Set(ref _appSearch, text))
                    return;
                foreach (var app in Apps)
                {
                    app.IsMatch = text.Length == 0
                        || app.Title.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
        }

        public bool CreateRestorePoint
        {
            get { return _restore; }
            set { if (Set(ref _restore, value)) MarkDirty(); }
        }

        public bool RestartExplorer
        {
            get { return _restartExplorer; }
            set { if (Set(ref _restartExplorer, value)) MarkDirty(); }
        }

        public string LogText
        {
            get { return _log; }
            private set { Set(ref _log, value); }
        }

        public string ReviewSubtitle { get; private set; }

        public void SaveIfDirty()
        {
            if (_dirty)
                Save();
        }

        void BuildTweaks()
        {
            string reason = "Not available on " + Os.ProductName + ".";
            foreach (var spec in TweakCatalog.Categories())
            {
                var category = new CategoryVm
                {
                    Title = spec.Title,
                    Glyph = spec.Glyph,
                    Description = spec.Description
                };
                _categories.Add(category);
            }

            var byId = new Dictionary<string, CategoryVm>();
            int index = 0;
            foreach (var spec in TweakCatalog.Categories())
            {
                byId[spec.Id] = _categories[index];
                index++;
            }

            foreach (var def in TweakCatalog.Tweaks())
            {
                bool available = Os.Supports(def.Os);
                var tweak = new TweakVm(def, available, available ? null : reason);
                tweak.PropertyChanged += TweakChanged;
                _tweaks.Add(tweak);
                CategoryVm category;
                if (byId.TryGetValue(def.CategoryId, out category))
                    category.Items.Add(tweak);
            }
        }

        ObservableCollection<CategoryVm> Column(int column)
        {
            var list = new ObservableCollection<CategoryVm>();
            int index = 0;
            foreach (var spec in TweakCatalog.Categories())
            {
                if (spec.Column == column)
                    list.Add(_categories[index]);
                index++;
            }
            return list;
        }

        void BuildApps()
        {
            Apps = new ObservableCollection<AppVm>();
            foreach (var def in AppCatalog.Apps())
            {
                var app = new AppVm
                {
                    Package = def.Package,
                    Title = def.Title,
                    Description = def.Description,
                    Common = def.Common
                };
                app.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "IsChecked")
                        MarkDirty();
                };
                Apps.Add(app);
            }
        }

        void TweakChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked" || e.PropertyName == "SelectedChoice")
                MarkDirty();
        }

        void MarkDirty()
        {
            if (!_suspend)
                _dirty = true;
        }

        void SelectDefaults()
        {
            _suspend = true;
            foreach (var tweak in _tweaks)
            {
                if (tweak.IsCombo)
                    tweak.SelectedChoice = tweak.Choices.Count > 0 ? tweak.Choices[0] : null;
                else
                    tweak.IsChecked = tweak.IsDefault && tweak.IsAvailable;
            }
            _suspend = false;
        }

        void ClearTweaks()
        {
            _suspend = true;
            foreach (var tweak in _tweaks)
            {
                if (tweak.IsCombo)
                    tweak.SelectedChoice = tweak.Choices.Count > 0 ? tweak.Choices[0] : null;
                else
                    tweak.IsChecked = false;
            }
            _suspend = false;
            _dirty = true;
        }

        void SelectCommon()
        {
            foreach (var app in Apps)
                app.IsChecked = app.Common;
        }

        void ClearApps()
        {
            foreach (var app in Apps)
                app.IsChecked = false;
        }

        void LoadLast()
        {
            string path = Path.Combine(ApplyEngine.Root, "last-used.txt");
            if (!File.Exists(path))
            {
                MessageBox.Show("No saved selection yet. It is written when you apply, or when you close the window after changing something.", "WinTweaker");
                return;
            }

            var tweakById = new Dictionary<string, TweakVm>(StringComparer.OrdinalIgnoreCase);
            foreach (var tweak in _tweaks)
                tweakById[tweak.Id] = tweak;
            var appById = new Dictionary<string, AppVm>(StringComparer.OrdinalIgnoreCase);
            foreach (var app in Apps)
                appById[app.Package] = app;

            _suspend = true;
            try
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length < 3)
                        continue;
                    if (parts[0] == "tweak" && parts.Length >= 4)
                    {
                        TweakVm tweak;
                        if (!tweakById.TryGetValue(parts[1], out tweak))
                            continue;
                        if (parts[2] == "check")
                            tweak.IsChecked = parts[3] == "1";
                        else if (parts[2] == "combo")
                        {
                            foreach (var choice in tweak.Choices)
                            {
                                if (string.Equals(choice.Id, parts[3], StringComparison.OrdinalIgnoreCase))
                                {
                                    tweak.SelectedChoice = choice;
                                    break;
                                }
                            }
                        }
                    }
                    else if (parts[0] == "app")
                    {
                        AppVm app;
                        if (appById.TryGetValue(parts[1], out app))
                            app.IsChecked = parts[2] == "1";
                    }
                    else if (parts[0] == "opt" && parts[1] == "restore")
                        _restore = parts[2] == "1";
                    else if (parts[0] == "opt" && parts[1] == "explorer")
                        _restartExplorer = parts[2] == "1";
                }
            }
            finally
            {
                _suspend = false;
            }

            Raise(nameof(CreateRestorePoint));
            Raise(nameof(RestartExplorer));
            _dirty = false;
        }

        void Save()
        {
            Directory.CreateDirectory(ApplyEngine.Root);
            var lines = new List<string>();
            foreach (var tweak in _tweaks)
            {
                if (tweak.IsCombo)
                {
                    string id = tweak.SelectedChoice == null ? "none" : tweak.SelectedChoice.Id;
                    lines.Add("tweak\t" + tweak.Id + "\tcombo\t" + id);
                }
                else
                {
                    lines.Add("tweak\t" + tweak.Id + "\tcheck\t" + (tweak.IsChecked ? "1" : "0"));
                }
            }
            foreach (var app in Apps)
                lines.Add("app\t" + app.Package + "\t" + (app.IsChecked ? "1" : "0"));
            lines.Add("opt\trestore\t" + (CreateRestorePoint ? "1" : "0"));
            lines.Add("opt\texplorer\t" + (RestartExplorer ? "1" : "0"));
            File.WriteAllLines(Path.Combine(ApplyEngine.Root, "last-used.txt"), lines, new UTF8Encoding(false));
            _dirty = false;
        }

        void Back()
        {
            Page = _page - 1;
        }

        void Next()
        {
            if (_applying)
                return;
            if (_page < 3)
            {
                Page = _page + 1;
                return;
            }
            if (_applied)
            {
                Application.Current.Shutdown();
                return;
            }
            StartApply();
        }

        void GoPage(object parameter)
        {
            int page;
            if (parameter is int)
                page = (int)parameter;
            else if (!int.TryParse(parameter == null ? "" : parameter.ToString(), out page))
                return;
            Page = page;
        }

        void RebuildReview()
        {
            ReviewItems.Clear();
            int tweaks = 0;
            int apps = 0;
            foreach (var tweak in _tweaks)
            {
                if (!tweak.IsAvailable)
                    continue;
                if (tweak.IsCombo)
                {
                    if (tweak.SelectedChoice != null && tweak.SelectedChoice.Id != "none")
                    {
                        ReviewItems.Add(tweak.Title + ": " + tweak.SelectedChoice.Label);
                        tweaks++;
                    }
                }
                else if (tweak.IsChecked)
                {
                    ReviewItems.Add(tweak.Title);
                    tweaks++;
                }
            }
            foreach (var app in Apps)
            {
                if (!app.IsChecked)
                    continue;
                ReviewItems.Add("Remove " + app.Title);
                apps++;
            }
            if (ReviewItems.Count == 0)
                ReviewItems.Add("Nothing selected.");
            string point = CreateRestorePoint ? "A restore point is created first. " : "";
            string shell = RestartExplorer ? "Explorer restarts at the end." : "Explorer stays running.";
            ReviewSubtitle = tweaks + " tweaks, " + apps + " apps. " + point + shell;
            Raise(nameof(ReviewSubtitle));
        }

        void StartApply()
        {
            Save();
            var plan = BuildPlan();
            LogText = "";
            IsApplying = true;
            var progress = new Progress<string>(line => { LogText = LogText + line + "\n"; });
            Task.Run(() => ApplyEngine.Apply(plan, progress))
                .ContinueWith(task =>
                {
                    if (task.Exception != null)
                        LogText = LogText + task.Exception.GetBaseException().Message + "\n";
                    _applied = true;
                    IsApplying = false;
                    Raise(nameof(NextLabel));
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        Plan BuildPlan()
        {
            var plan = new Plan
            {
                RestorePoint = CreateRestorePoint,
                RestartExplorer = RestartExplorer
            };
            var packages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var tweak in _tweaks)
            {
                if (!tweak.IsAvailable)
                    continue;

                if (tweak.IsCombo)
                {
                    var choice = tweak.SelectedChoice;
                    if (choice == null || choice.Id == "none")
                        continue;
                    var item = new PlanItem { Title = tweak.Title + ": " + choice.Label, ClearPins = choice.Source.ClearPins };
                    item.Ops.AddRange(choice.Source.Ops);
                    plan.Items.Add(item);
                    continue;
                }

                if (!tweak.IsChecked)
                    continue;

                var check = new PlanItem
                {
                    Title = tweak.Title,
                    MouseOff = tweak.Source.MouseOff,
                    DenyStore = tweak.Source.DenyStore,
                    KillWidgets = tweak.Source.KillWidgets
                };
                check.Ops.AddRange(tweak.Source.Ops);
                check.DisableServices.AddRange(tweak.Source.DisableServices);
                check.ManualServices.AddRange(tweak.Source.ManualServices);
                foreach (string package in tweak.Source.Packages)
                    packages.Add(package);
                plan.Items.Add(check);
            }

            if (AppsSupported)
            {
                foreach (var app in Apps)
                {
                    if (app.IsChecked)
                        packages.Add(app.Package);
                }
            }

            plan.Packages.AddRange(packages);
            return plan;
        }

        void Undo()
        {
            if (_applying)
                return;
            Page = 3;
            LogText = "";
            IsApplying = true;
            var progress = new Progress<string>(line => { LogText = LogText + line + "\n"; });
            Task.Run(() => ApplyEngine.UndoLast(progress))
                .ContinueWith(task =>
                {
                    if (task.Exception != null)
                        LogText = LogText + task.Exception.GetBaseException().Message + "\n";
                    _applied = true;
                    IsApplying = false;
                    Raise(nameof(NextLabel));
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        void Elevate()
        {
            try
            {
                string exe = Process.GetCurrentProcess().MainModule.FileName;
                Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, Verb = "runas" });
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "WinTweaker");
            }
        }

        void About()
        {
            MessageBox.Show(
                "WinTweaker 0.1\n" + Os.Caption + "\n\nUndo snapshots are in %LocalAppData%\\WinTweaker\\backups.",
                "WinTweaker",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
