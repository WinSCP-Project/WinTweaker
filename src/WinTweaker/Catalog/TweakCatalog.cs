using System.Collections.Generic;
using WinTweaker.Models;

namespace WinTweaker.Catalog
{
    public sealed class CategorySpec
    {
        public string Id;
        public string Title;
        public string Glyph;
        public string Description;
        public int Column;
    }

    public static class TweakCatalog
    {
        const string Adv = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
        const string Cdm = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
        const string Pers = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        const string Edge = @"SOFTWARE\Policies\Microsoft\Edge";
        const string Ai = @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI";
        const string AiCu = @"Software\Policies\Microsoft\Windows\WindowsAI";

        static readonly OsTarget All = OsTarget.Win7 | OsTarget.Win10 | OsTarget.Win11;
        static readonly OsTarget Ten = OsTarget.Win10 | OsTarget.Win11;
        static readonly OsTarget Eleven = OsTarget.Win11;

        public static IList<CategorySpec> Categories()
        {
            return new[]
            {
                Cat("privacy", "Privacy & Suggested Content", "\uE72E", "Telemetry, ads, tips, location, and Edge suggestions.", 0),
                Cat("ai", "AI", "\uE99A", "Copilot, Recall, Click to Do, and AI inside Edge, Paint, and Notepad.", 0),
                Cat("appearance", "Appearance", "\uE70F", "Theme, transparency, and animation settings for this user.", 0),
                Cat("system", "System", "\uE977", "Shell, mouse, startup, and device encryption policy.", 1),
                Cat("update", "Windows Update", "\uE895", "How soon updates install, reboot behavior, and peer sharing.", 1),
                Cat("explorer", "File Explorer", "\uE8B7", "Launch folder, extensions, hidden files, and navigation pane.", 1),
                Cat("start", "Start Menu & Search", "\uE782", "Pins, recommendations, Bing, and search history.", 2),
                Cat("taskbar", "Taskbar", "\uE7F4", "Alignment, search box, widgets, and button grouping.", 2)
            };
        }

        public static IList<TweakDef> Tweaks()
        {
            var list = new List<TweakDef>();

            var telemetry = Check("privacy.telemetry", "privacy",
                "Disable telemetry, tracking & targeted ads",
                "Sets the diagnostic policy to its lowest level, turns off the advertising id and activity history, and stops DiagTrack. Home and Pro can still keep a minimum diagnostic level; the service stays off.",
                All, true, TelemetryOps());
            telemetry.DisableServices = new[] { "DiagTrack", "dmwappushservice" };
            list.Add(telemetry);

            list.Add(Check("privacy.tips", "privacy",
                "Disable tips, tricks & suggested content throughout Windows",
                "Turns off suggested apps, Settings suggestions, welcome tips, and account nags in Content Delivery.",
                Ten, true, SuggestionOps()));

            list.Add(Check("privacy.location", "privacy",
                "Disable Windows location services & app location access",
                "Sets the location-sensor policy so Windows and apps are not given a location fix.",
                Ten, false, new[] { RegOp.D(Hive.Lm, @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", 1) }));

            list.Add(Check("privacy.findmy", "privacy",
                "Disable Find My Device location tracking",
                "Blocks the Find My Device policy. A device that is already registered stays on the account until you remove it there.",
                Ten, false, new[] { RegOp.D(Hive.Lm, @"SOFTWARE\Policies\Microsoft\FindMyDevice", "AllowFindMyDevice", 0) }));

            list.Add(Check("privacy.locktips", "privacy",
                "Disable tips & tricks on the lock screen",
                "Turns off lock-screen fun facts and the rotating lock-screen overlay.",
                Ten, true, new[]
                {
                    RegOp.D(Hive.Cu, Cdm, "SubscribedContent-338387Enabled", 0),
                    RegOp.D(Hive.Cu, Cdm, "RotatingLockScreenOverlayEnabled", 0)
                }));

            list.Add(Check("privacy.spotlight", "privacy",
                "Disable Windows Spotlight for desktop",
                "Stops Spotlight from replacing the desktop background and clears the Spotlight desktop icon entry.",
                Ten, false, new[]
                {
                    RegOp.D(Hive.Cu, @"Software\Policies\Microsoft\Windows\CloudContent", "DisableSpotlightCollectionOnDesktop", 1),
                    RegOp.Del(Hive.Cu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel", "{2cc5ca98-6485-489a-920e-b3e88a6ccce3}")
                }));

            list.Add(Check("privacy.edgeads", "privacy",
                "Disable ads, suggestions and newsfeed in Edge",
                "Turns off Edge new-tab feed, shopping, recommendations, and sponsored content. Does not block you from setting Edge as the default browser.",
                Ten, true, EdgeAdOps()));

            list.Add(Check("privacy.m365", "privacy",
                "Hide Microsoft 365 Copilot ads in Settings Home",
                "Hides the consumer-account promo card on Settings Home.",
                Ten, false, new[] { RegOp.D(Hive.Lm, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableConsumerAccountStateContent", 1) }));

            list.Add(Check("ai.copilot", "ai",
                "Disable Microsoft Copilot",
                "Removes the Copilot taskbar button and turns Copilot off for this user and for the machine.",
                Eleven, true, new[]
                {
                    RegOp.D(Hive.Cu, Adv, "ShowCopilotButton", 0),
                    RegOp.D(Hive.Cu, @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1),
                    RegOp.D(Hive.Lm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1)
                }));

            list.Add(Check("ai.recall", "ai",
                "Disable Windows Recall",
                "Disables Recall snapshots and blocks turning Recall back on from Settings.",
                Eleven, true, new[]
                {
                    RegOp.D(Hive.Cu, AiCu, "DisableAIDataAnalysis", 1),
                    RegOp.D(Hive.Lm, Ai, "DisableAIDataAnalysis", 1),
                    RegOp.D(Hive.Lm, Ai, "AllowRecallEnablement", 0),
                    RegOp.D(Hive.Lm, Ai, "TurnOffSavingSnapshots", 1)
                }));

            list.Add(Check("ai.clicktodo", "ai",
                "Disable Click To Do, AI text & image analysis",
                "Turns off Click to Do for this user and for the machine.",
                Eleven, true, new[]
                {
                    RegOp.D(Hive.Cu, AiCu, "DisableClickToDo", 1),
                    RegOp.D(Hive.Lm, Ai, "DisableClickToDo", 1)
                }));

            var aiService = Check("ai.service", "ai",
                "Prevent AI service from starting automatically",
                "Sets WSAIFabricSvc to manual start. The service is left running if it is already up; it just will not start on the next boot by itself.",
                Eleven, true, new RegOp[0]);
            aiService.ManualServices = new[] { "WSAIFabricSvc" };
            list.Add(aiService);

            list.Add(Check("ai.edge", "ai",
                "Disable AI features in Microsoft Edge",
                "Turns off Copilot page context, the sidebar, inline compose, and the new-tab Bing chat entry in Edge.",
                Ten, false, new[]
                {
                    RegOp.D(Hive.Lm, Edge, "CopilotCDPPageContext", 0),
                    RegOp.D(Hive.Lm, Edge, "CopilotPageContext", 0),
                    RegOp.D(Hive.Lm, Edge, "HubsSidebarEnabled", 0),
                    RegOp.D(Hive.Lm, Edge, "EdgeEntraCopilotPageContext", 0),
                    RegOp.D(Hive.Lm, Edge, "EdgeHistoryAISearchEnabled", 0),
                    RegOp.D(Hive.Lm, Edge, "ComposeInlineEnabled", 0),
                    RegOp.D(Hive.Lm, Edge, "GenAILocalFoundationalModelSettings", 1),
                    RegOp.D(Hive.Lm, Edge, "NewTabPageBingChatEnabled", 0)
                }));

            list.Add(Check("ai.paint", "ai",
                "Disable AI features in Paint",
                "Turns off Cocreator, generative fill, image creator, generative erase, and background removal in Paint.",
                Eleven, false, new[]
                {
                    RegOp.D(Hive.Lm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint", "DisableCocreator", 1),
                    RegOp.D(Hive.Lm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint", "DisableGenerativeFill", 1),
                    RegOp.D(Hive.Lm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint", "DisableImageCreator", 1),
                    RegOp.D(Hive.Lm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint", "DisableGenerativeErase", 1),
                    RegOp.D(Hive.Lm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint", "DisableRemoveBackground", 1)
                }));

            list.Add(Check("ai.notepad", "ai",
                "Disable AI features in Notepad",
                "Turns off Notepad rewrite and the other Notepad AI features via policy.",
                Eleven, false, new[] { RegOp.D(Hive.Lm, @"SOFTWARE\Policies\WindowsNotepad", "DisableAIFeatures", 1) }));

            list.Add(Check("appearance.dark", "appearance",
                "Enable dark theme for system and apps",
                "Sets both app mode and system mode to dark for this user.",
                Ten, false, new[]
                {
                    RegOp.D(Hive.Cu, Pers, "AppsUseLightTheme", 0),
                    RegOp.D(Hive.Cu, Pers, "SystemUsesLightTheme", 0)
                }));

            list.Add(Check("appearance.transparency", "appearance",
                "Disable transparency effects",
                "Turns off acrylic and transparency in Settings > Personalization > Colors.",
                Ten, false, new[] { RegOp.D(Hive.Cu, Pers, "EnableTransparency", 0) }));

            list.Add(Check("appearance.animations", "appearance",
                "Disable animations and visual effects",
                "Writes the animation-off UserPreferencesMask. Undo puts the previous mask back, so other bits in that mask are restored with it.",
                All, false, new[]
                {
                    RegOp.Bin(Hive.Cu, @"Control Panel\Desktop", "UserPreferencesMask", new byte[] { 0x90, 0x12, 0x07, 0x80, 0x10, 0x00, 0x00, 0x00 })
                }));

            list.Add(Check("system.dragtray", "system",
                "Disable 'Drag Tray' for sharing & moving files",
                "Turns off the drag tray that appears when you drag a file toward the top of the screen.",
                Eleven, true, new[] { RegOp.D(Hive.Cu, @"Software\Microsoft\Windows\CurrentVersion\CDP", "DragTrayEnabled", 0) }));

            list.Add(Check("system.classicmenu", "system",
                "Use classic Windows 10 context menu style",
                "Restores the full right-click menu. Explorer has to restart before it shows up.",
                Eleven, false, new[]
                {
                    RegOp.S(Hive.Cu, @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", "", "")
                }));

            var mouse = Check("system.mouse", "system",
                "Disable Enhance Pointer Precision (mouse acceleration)",
                "Sets the mouse curve to a flat 1:1 and applies it immediately.",
                All, false, new[]
                {
                    RegOp.S(Hive.Cu, @"Control Panel\Mouse", "MouseSpeed", "0"),
                    RegOp.S(Hive.Cu, @"Control Panel\Mouse", "MouseThreshold1", "0"),
                    RegOp.S(Hive.Cu, @"Control Panel\Mouse", "MouseThreshold2", "0")
                });
            mouse.MouseOff = true;
            list.Add(mouse);

            list.Add(Check("system.sticky", "system",
                "Disable Sticky Keys keyboard shortcut (5x shift)",
                "Clears the hotkey bit on Sticky Keys so five Shift presses no longer open the prompt.",
                All, false, new[] { RegOp.S(Hive.Cu, @"Control Panel\Accessibility\StickyKeys", "Flags", "506") }));

            list.Add(Check("system.storage", "system",
                "Disable Storage Sense automatic disk cleanup",
                "Turns Storage Sense off so it does not delete temp files and recycle bin items on a schedule.",
                Ten, false, new[] { RegOp.D(Hive.Cu, @"SOFTWARE\Microsoft\Windows\CurrentVersion\StorageSense\Parameters\StoragePolicy", "01", 0) }));

            list.Add(Check("system.faststartup", "system",
                "Disable fast start-up",
                "Turns off Hiberboot. A full shutdown takes longer. Hibernate itself stays installed.",
                Ten, true, new[] { RegOp.D(Hive.Lm, @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", "HiberbootEnabled", 0) }));

            list.Add(Check("system.bitlocker", "system",
                "Disable BitLocker automatic device encryption",
                "Stops Windows from encrypting the drive on its own. A volume that is already encrypted is not decrypted.",
                Ten, false, new[] { RegOp.D(Hive.Lm, @"SYSTEM\CurrentControlSet\Control\BitLocker", "PreventDeviceEncryption", 1) }));

            list.Add(Check("update.asap", "update",
                "Prevent getting updates as soon as they're available",
                "Turns off the 'get the latest updates as soon as they're available' switch. Updates still install on the normal schedule.",
                Eleven, false, new[] { RegOp.D(Hive.Lm, @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings", "IsContinuousInnovationOptedIn", 0) }));

            list.Add(Check("update.noreboot", "update",
                "Prevent automatic restarts after updates while signed in",
                "Stops Windows Update from rebooting while someone is logged on. It can still reboot at the scheduled time if nobody is signed in.",
                All, false, new[] { RegOp.D(Hive.Lm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU", "NoAutoRebootWithLoggedOnUsers", 1) }));

            list.Add(Check("update.delivery", "update",
                "Disable sharing downloaded updates with other PCs",
                "Sets Delivery Optimization for the Network Service account to download-only, so this PC does not upload updates to other machines.",
                Ten, false, new[] { RegOp.D(Hive.Users, @"S-1-5-20\Software\Microsoft\Windows\CurrentVersion\DeliveryOptimization\Settings", "DownloadMode", 0) }));

            list.Add(Combo("explorer.launch", "explorer", "Open File Explorer to",
                "Where a new Explorer window opens.",
                Ten, new[]
                {
                    None(),
                    new ChoiceDef("home", "Home", new[] { RegOp.D(Hive.Cu, Adv, "LaunchTo", 2) }, false),
                    new ChoiceDef("thispc", "This PC", new[] { RegOp.D(Hive.Cu, Adv, "LaunchTo", 1) }, false),
                    new ChoiceDef("downloads", "Downloads", new[] { RegOp.D(Hive.Cu, Adv, "LaunchTo", 3) }, false),
                    new ChoiceDef("onedrive", "OneDrive", new[] { RegOp.D(Hive.Cu, Adv, "LaunchTo", 4) }, false)
                }));

            list.Add(Check("explorer.extensions", "explorer",
                "Show file extensions for known file types",
                "Turns HideFileExt off so .txt, .exe, and the rest stay visible.",
                All, true, new[] { RegOp.D(Hive.Cu, Adv, "HideFileExt", 0) }));

            list.Add(Check("explorer.hidden", "explorer",
                "Show hidden files, folders and drives",
                "Shows items with the hidden attribute. Protected system files stay hidden.",
                All, false, new[] { RegOp.D(Hive.Cu, Adv, "Hidden", 1) }));

            list.Add(Check("explorer.home", "explorer",
                "Hide 'Home' from navigation pane",
                "Unpins Home from the Explorer navigation pane and adds the matching Folder Options checkbox.",
                Eleven, false, HomeOps()));

            list.Add(Check("explorer.gallery", "explorer",
                "Hide 'Gallery' from navigation pane",
                "Unpins Gallery from the Explorer navigation pane.",
                Eleven, false, GalleryOps()));

            list.Add(Check("explorer.dupes", "explorer",
                "Hide duplicate removable drive entries",
                "Removes the delegate folder that lists the same USB drive twice in the navigation pane.",
                Ten, false, new[]
                {
                    RegOp.DelKey(Hive.Lm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Desktop\NameSpace\DelegateFolders\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}")
                }));

            list.Add(Combo("start.pins", "start", "Remove pinned apps from the start menu",
                "Replaces this user's start2.bin with an empty layout. The previous file is kept in the undo snapshot.",
                Eleven, new[]
                {
                    None(),
                    new ChoiceDef("clear", "Remove all pinned apps", new RegOp[0], true)
                }));

            list.Add(Check("start.recommended", "start",
                "Hide recommended section in the start menu",
                "Collapses the Recommended section. Recent files stop showing there.",
                Eleven, false, new[] { RegOp.D(Hive.Cu, @"SOFTWARE\Policies\Microsoft\Windows\Explorer", "HideRecommendedSection", 1) }));

            list.Add(Check("start.phone", "start",
                "Disable Phone Link integration in the start menu",
                "Turns off the mobile-device companion tile in Start.",
                Eleven, false, new[]
                {
                    RegOp.D(Hive.Cu, @"Software\Microsoft\Windows\CurrentVersion\Start\Companions\Microsoft.YourPhone_8wekyb3d8bbwe", "IsEnabled", 0)
                }));

            var bing = Check("start.bing", "start",
                "Disable Bing web search & Copilot integration in search",
                "Turns off web results and Cortana in the search box, and removes the Bing Search app package if it is installed.",
                Ten, true, new[]
                {
                    RegOp.D(Hive.Cu, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1),
                    RegOp.D(Hive.Lm, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0),
                    RegOp.D(Hive.Lm, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "CortanaConsent", 0),
                    RegOp.D(Hive.Cu, @"Software\Microsoft\Windows\CurrentVersion\Search", "BingSearchEnabled", 0)
                });
            bing.Packages = new[] { "Microsoft.BingSearch" };
            list.Add(bing);

            var store = Check("start.store", "start",
                "Disable Microsoft Store app suggestions in search",
                "Denies Everyone write access to this user's Store database so search cannot refill it with app suggestions.",
                Ten, true, new RegOp[0]);
            store.DenyStore = true;
            list.Add(store);

            list.Add(Check("start.highlights", "start",
                "Disable Search Highlights in the taskbar search box",
                "Turns off the illustrated search highlights and the dynamic content in Windows Search.",
                Ten, true, new[]
                {
                    RegOp.D(Hive.Cu, @"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDynamicSearchBoxEnabled", 0),
                    RegOp.D(Hive.Lm, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "EnableDynamicContentInWSB", 0)
                }));

            list.Add(Check("start.history", "start",
                "Disable local Windows Search history",
                "Stops this device from keeping a local search history for the search box.",
                Ten, false, new[] { RegOp.D(Hive.Cu, @"Software\Microsoft\Windows\CurrentVersion\SearchSettings", "IsDeviceSearchHistoryEnabled", 0) }));

            list.Add(Check("taskbar.left", "taskbar",
                "Left taskbar alignment",
                "Moves the taskbar icons to the left.",
                Eleven, false, new[] { RegOp.D(Hive.Cu, Adv, "TaskbarAl", 0) }));

            list.Add(Combo("taskbar.search", "taskbar", "Taskbar search style",
                "How search shows up on the taskbar.",
                Ten, new[]
                {
                    None(),
                    new ChoiceDef("hidden", "Hidden", new[] { RegOp.D(Hive.Cu, @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 0) }, false),
                    new ChoiceDef("icon", "Search icon only", new[] { RegOp.D(Hive.Cu, @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 1) }, false),
                    new ChoiceDef("label", "Search icon and label", new[] { RegOp.D(Hive.Cu, @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 3) }, false),
                    new ChoiceDef("box", "Search box", new[] { RegOp.D(Hive.Cu, @"Software\Microsoft\Windows\CurrentVersion\Search", "SearchboxTaskbarMode", 2) }, false)
                }));

            list.Add(Check("taskbar.taskview", "taskbar",
                "Hide 'Task view' button on the taskbar",
                "Removes the Task view button. Win+Tab still opens Task view.",
                Ten, false, new[] { RegOp.D(Hive.Cu, Adv, "ShowTaskViewButton", 0) }));

            var widgets = Check("taskbar.widgets", "taskbar",
                "Disable widgets on the taskbar & lock screen",
                "Hides the widgets button and News and Interests, and removes the widgets app packages when they are installed.",
                Ten, true, new[]
                {
                    RegOp.D(Hive.Cu, Adv, "TaskbarDa", 0),
                    RegOp.D(Hive.Cu, @"Software\Microsoft\Windows\CurrentVersion\Feeds", "ShellFeedsTaskbarViewMode", 2),
                    RegOp.D(Hive.Lm, @"SOFTWARE\Policies\Microsoft\Windows\Windows Feeds", "EnableFeeds", 0),
                    RegOp.D(Hive.Cu, Cdm, "RotatingLockScreenOverlayEnabled", 0),
                    RegOp.D(Hive.Cu, Cdm, "SubscribedContent-338387Enabled", 0)
                });
            widgets.Packages = new[] { "Microsoft.StartExperiencesApp", "MicrosoftWindows.Client.WebExperience", "Microsoft.WidgetsPlatformRuntime" };
            widgets.KillWidgets = true;
            list.Add(widgets);

            list.Add(Check("taskbar.endtask", "taskbar",
                "Show 'End Task' option in taskbar context menu",
                "Adds End Task to the taskbar right-click menu for a running app.",
                Eleven, true, new[] { RegOp.D(Hive.Cu, Adv + @"\TaskbarDeveloperSettings", "TaskbarEndTask", 1) }));

            list.Add(Check("taskbar.lastactive", "taskbar",
                "Enable 'Last Active Click' behavior for taskbar apps",
                "Clicking a grouped taskbar button focuses the last active window. Hover still shows the preview.",
                All, false, new[] { RegOp.D(Hive.Cu, Adv, "LastActiveClick", 1) }));

            list.Add(Combo("taskbar.where", "taskbar", "Show taskbar apps on",
                "Which monitors show taskbar buttons when you have more than one.",
                Ten, new[]
                {
                    None(),
                    new ChoiceDef("all", "All taskbars", new[] { RegOp.D(Hive.Cu, Adv, "MMTaskbarMode", 0) }, false),
                    new ChoiceDef("main", "Main taskbar and taskbar where window is open", new[] { RegOp.D(Hive.Cu, Adv, "MMTaskbarMode", 1) }, false),
                    new ChoiceDef("open", "Taskbar where window is open", new[] { RegOp.D(Hive.Cu, Adv, "MMTaskbarMode", 2) }, false)
                }));

            list.Add(Combo("taskbar.combine", "taskbar", "Combine taskbar buttons on the main display",
                "Whether buttons for the same app collapse into one.",
                All, new[]
                {
                    None(),
                    new ChoiceDef("always", "Always", new[] { RegOp.D(Hive.Cu, Adv, "TaskbarGlomLevel", 0) }, false),
                    new ChoiceDef("full", "When taskbar is full", new[] { RegOp.D(Hive.Cu, Adv, "TaskbarGlomLevel", 1) }, false),
                    new ChoiceDef("never", "Never", new[] { RegOp.D(Hive.Cu, Adv, "TaskbarGlomLevel", 2) }, false)
                }));

            return list;
        }

        static RegOp[] TelemetryOps()
        {
            return new[]
            {
                RegOp.D(Hive.Cu, @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo", "Enabled", 0),
                RegOp.D(Hive.Cu, @"Software\Microsoft\Windows\CurrentVersion\Privacy", "TailoredExperiencesWithDiagnosticDataEnabled", 0),
                RegOp.D(Hive.Cu, @"Software\Microsoft\Speech_OneCore\Settings\OnlineSpeechPrivacy", "HasAccepted", 0),
                RegOp.D(Hive.Cu, @"Software\Microsoft\Input\TIPC", "Enabled", 0),
                RegOp.D(Hive.Cu, @"Software\Microsoft\InputPersonalization", "RestrictImplicitInkCollection", 1),
                RegOp.D(Hive.Cu, @"Software\Microsoft\InputPersonalization", "RestrictImplicitTextCollection", 1),
                RegOp.D(Hive.Cu, @"Software\Microsoft\InputPersonalization\TrainedDataStore", "HarvestContacts", 0),
                RegOp.D(Hive.Cu, @"Software\Microsoft\Personalization\Settings", "AcceptedPrivacyPolicy", 0),
                RegOp.D(Hive.Lm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection", "AllowTelemetry", 0),
                RegOp.D(Hive.Lm, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry", 0),
                RegOp.D(Hive.Cu, Adv, "Start_TrackProgs", 0),
                RegOp.D(Hive.Lm, @"SOFTWARE\Policies\Microsoft\Windows\System", "PublishUserActivities", 0),
                RegOp.D(Hive.Cu, @"SOFTWARE\Microsoft\Siuf\Rules", "NumberOfSIUFInPeriod", 0),
                RegOp.Del(Hive.Cu, @"SOFTWARE\Microsoft\Siuf\Rules", "PeriodInNanoSeconds"),
                RegOp.D(Hive.Lm, Edge, "PersonalizationReportingEnabled", 0),
                RegOp.D(Hive.Lm, Edge, "DiagnosticData", 0),
                RegOp.D(Hive.Lm, @"SOFTWARE\Policies\Microsoft\SQMClient\Windows", "CEIPEnable", 0)
            };
        }

        static RegOp[] SuggestionOps()
        {
            return new[]
            {
                RegOp.D(Hive.Cu, Cdm, "SubscribedContent-310093Enabled", 0),
                RegOp.D(Hive.Cu, Cdm, "SubscribedContent-338388Enabled", 0),
                RegOp.D(Hive.Cu, Cdm, "SystemPaneSuggestionsEnabled", 0),
                RegOp.D(Hive.Cu, Adv, "Start_IrisRecommendations", 0),
                RegOp.D(Hive.Cu, Cdm, "SubscribedContent-338389Enabled", 0),
                RegOp.D(Hive.Cu, Cdm, "SoftLandingEnabled", 0),
                RegOp.D(Hive.Cu, Cdm, "SubscribedContent-338393Enabled", 0),
                RegOp.D(Hive.Cu, Cdm, "SubscribedContent-353694Enabled", 0),
                RegOp.D(Hive.Cu, Cdm, "SubscribedContent-353696Enabled", 0),
                RegOp.D(Hive.Cu, Cdm, "SubscribedContent-353698Enabled", 0),
                RegOp.D(Hive.Cu, @"Software\Microsoft\Windows\CurrentVersion\SystemSettings\AccountNotifications", "EnableAccountNotifications", 0),
                RegOp.D(Hive.Cu, @"SOFTWARE\Microsoft\Windows\CurrentVersion\UserProfileEngagement", "ScoobeSystemSettingEnabled", 0),
                RegOp.D(Hive.Cu, Adv, "ShowSyncProviderNotifications", 0),
                RegOp.D(Hive.Cu, Cdm, "SilentInstalledAppsEnabled", 0),
                RegOp.D(Hive.Cu, @"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.Suggested", "Enabled", 0),
                RegOp.D(Hive.Cu, @"Software\Microsoft\Windows\CurrentVersion\Mobility", "OptedIn", 0),
                RegOp.D(Hive.Cu, Adv, "Start_AccountNotifications", 0),
                RegOp.D(Hive.Cu, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.BackupReminder", "Enabled", 0)
            };
        }

        static RegOp[] EdgeAdOps()
        {
            return new[]
            {
                RegOp.D(Hive.Lm, Edge, "NewTabPageContentEnabled", 0),
                RegOp.D(Hive.Lm, Edge, "NewTabPageHideDefaultTopSites", 1),
                RegOp.D(Hive.Lm, Edge, "EdgeShoppingAssistantEnabled", 0),
                RegOp.D(Hive.Lm, Edge, "TabServicesEnabled", 0),
                RegOp.D(Hive.Lm, Edge, "AlternateErrorPagesEnabled", 0),
                RegOp.D(Hive.Lm, Edge, "UserFeedbackAllowed", 0),
                RegOp.D(Hive.Lm, Edge, "ShowRecommendationsEnabled", 0),
                RegOp.D(Hive.Lm, Edge, "WalletDonationEnabled", 0),
                RegOp.D(Hive.Lm, Edge, "HideFirstRunExperience", 1),
                RegOp.D(Hive.Lm, Edge, "DefaultBrowserSettingsCampaignEnabled", 0),
                RegOp.D(Hive.Lm, Edge, "SpotlightExperiencesAndRecommendationsEnabled", 0),
                RegOp.D(Hive.Lm, Edge, "ShowAcrobatSubscriptionButton", 0)
            };
        }

        static RegOp[] HomeOps()
        {
            string clsid = @"Software\Classes\CLSID\{f874310e-b6b7-47dc-bc84-b9e6b38f5903}";
            string nav = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\NavPane\ShowHome";
            return new[]
            {
                RegOp.S(Hive.Cu, clsid, "", "CLSID_MSGraphHomeFolder"),
                RegOp.D(Hive.Cu, clsid, "System.IsPinnedToNameSpaceTree", 0),
                RegOp.D(Hive.Lm, nav, "CheckedValue", 1),
                RegOp.D(Hive.Lm, nav, "DefaultValue", 0),
                RegOp.D(Hive.Lm, nav, "HKeyRoot", unchecked((int)0x80000001)),
                RegOp.D(Hive.Lm, nav, "Id", 13),
                RegOp.S(Hive.Lm, nav, "RegPath", @"Software\Classes\CLSID\{f874310e-b6b7-47dc-bc84-b9e6b38f5903}"),
                RegOp.S(Hive.Lm, nav, "Text", "Show Home"),
                RegOp.S(Hive.Lm, nav, "Type", "checkbox"),
                RegOp.D(Hive.Lm, nav, "UncheckedValue", 0),
                RegOp.S(Hive.Lm, nav, "ValueName", "System.IsPinnedToNameSpaceTree")
            };
        }

        static RegOp[] GalleryOps()
        {
            string clsid = @"Software\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}";
            string nav = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced\NavPane\ShowGallery";
            return new[]
            {
                RegOp.D(Hive.Cu, clsid, "System.IsPinnedToNameSpaceTree", 0),
                RegOp.D(Hive.Lm, nav, "CheckedValue", 1),
                RegOp.D(Hive.Lm, nav, "DefaultValue", 0),
                RegOp.D(Hive.Lm, nav, "HKeyRoot", unchecked((int)0x80000001)),
                RegOp.D(Hive.Lm, nav, "Id", 13),
                RegOp.S(Hive.Lm, nav, "RegPath", @"Software\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}"),
                RegOp.S(Hive.Lm, nav, "Text", "Show Gallery"),
                RegOp.S(Hive.Lm, nav, "Type", "checkbox"),
                RegOp.D(Hive.Lm, nav, "UncheckedValue", 0),
                RegOp.S(Hive.Lm, nav, "ValueName", "System.IsPinnedToNameSpaceTree")
            };
        }

        static CategorySpec Cat(string id, string title, string glyph, string description, int column)
        {
            return new CategorySpec { Id = id, Title = title, Glyph = glyph, Description = description, Column = column };
        }

        static ChoiceDef None()
        {
            return new ChoiceDef("none", "No Change", new RegOp[0], false);
        }

        static TweakDef Check(string id, string category, string title, string description, OsTarget os, bool isDefault, RegOp[] ops)
        {
            return new TweakDef
            {
                Id = id,
                CategoryId = category,
                Title = title,
                Description = description,
                Os = os,
                IsDefault = isDefault,
                IsCombo = false,
                Ops = ops ?? new RegOp[0],
                DisableServices = new string[0],
                ManualServices = new string[0],
                Packages = new string[0],
                Choices = new ChoiceDef[0]
            };
        }

        static TweakDef Combo(string id, string category, string title, string description, OsTarget os, ChoiceDef[] choices)
        {
            return new TweakDef
            {
                Id = id,
                CategoryId = category,
                Title = title,
                Description = description,
                Os = os,
                IsDefault = false,
                IsCombo = true,
                Ops = new RegOp[0],
                DisableServices = new string[0],
                ManualServices = new string[0],
                Packages = new string[0],
                Choices = choices
            };
        }
    }
}
