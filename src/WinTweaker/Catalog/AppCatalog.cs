using System.Collections.Generic;
using WinTweaker.Models;

namespace WinTweaker.Catalog
{
    public static class AppCatalog
    {
        public static IList<AppDef> Apps()
        {
            return new[]
            {
                App("Microsoft.BingNews", "News", "Bing News.", true),
                App("Microsoft.BingWeather", "Weather", "Bing Weather.", true),
                App("Microsoft.BingSearch", "Bing Search", "Search highlights app package.", true),
                App("Microsoft.GetHelp", "Get Help", "Get Help.", true),
                App("Microsoft.Getstarted", "Tips", "Windows tips.", true),
                App("Microsoft.MicrosoftOfficeHub", "Office", "Office hub stub.", true),
                App("Microsoft.MicrosoftSolitaireCollection", "Solitaire Collection", "Solitaire.", true),
                App("Microsoft.People", "People", "People.", true),
                App("Microsoft.WindowsFeedbackHub", "Feedback Hub", "Feedback Hub.", true),
                App("Microsoft.XboxApp", "Xbox (legacy)", "Older Xbox app.", true),
                App("Microsoft.GamingApp", "Xbox", "Xbox app.", true),
                App("Microsoft.XboxGameOverlay", "Xbox Game Overlay", "Game overlay.", true),
                App("Microsoft.XboxGamingOverlay", "Xbox Game Bar", "Game Bar.", true),
                App("Microsoft.XboxIdentityProvider", "Xbox Identity Provider", "Xbox sign-in helper.", true),
                App("Microsoft.XboxSpeechToTextOverlay", "Xbox Speech to Text", "Xbox speech overlay.", true),
                App("Microsoft.Xbox.TCUI", "Xbox TCUI", "Xbox UI helper.", true),
                App("Microsoft.YourPhone", "Phone Link", "Phone Link.", true),
                App("Microsoft.ZuneMusic", "Media Player", "Groove / Media Player.", true),
                App("Microsoft.ZuneVideo", "Movies & TV", "Movies & TV.", true),
                App("Clipchamp.Clipchamp", "Clipchamp", "Clipchamp.", true),
                App("Microsoft.Todos", "Microsoft To Do", "To Do.", true),
                App("Microsoft.PowerAutomateDesktop", "Power Automate", "Power Automate.", true),
                App("MicrosoftCorporationII.QuickAssist", "Quick Assist", "Quick Assist.", true),
                App("Microsoft.549981C3F5F10", "Cortana", "Cortana app.", true),
                App("Microsoft.WindowsMaps", "Maps", "Windows Maps.", true),
                App("MicrosoftTeams", "Teams (personal)", "Consumer Teams.", true),
                App("Microsoft.Copilot", "Copilot", "Copilot app.", true),
                App("Microsoft.Windows.DevHome", "Dev Home", "Dev Home.", true),
                App("Microsoft.MixedReality.Portal", "Mixed Reality Portal", "Mixed Reality Portal.", true),
                App("Microsoft.SkypeApp", "Skype", "Skype.", true),
                App("microsoft.windowscommunicationsapps", "Mail and Calendar", "Mail and Calendar.", true),
                App("Microsoft.StartExperiencesApp", "Start Experiences", "Start menu feed.", true),
                App("MicrosoftWindows.Client.WebExperience", "Widgets", "Widgets host.", true),
                App("Microsoft.WidgetsPlatformRuntime", "Widgets runtime", "Widgets runtime.", true),
                App("MSTeams", "Teams", "Microsoft Teams.", false),
                App("Microsoft.OutlookForWindows", "Outlook", "New Outlook.", false),
                App("Microsoft.MicrosoftStickyNotes", "Sticky Notes", "Sticky Notes.", false),
                App("Microsoft.WindowsAlarms", "Clock", "Clock.", false),
                App("Microsoft.WindowsSoundRecorder", "Voice Recorder", "Voice Recorder.", false),
                App("Microsoft.WindowsCalculator", "Calculator", "Calculator.", false),
                App("Microsoft.Windows.Photos", "Photos", "Photos.", false),
                App("Microsoft.ScreenSketch", "Snipping Tool", "Snipping Tool.", false),
                App("Microsoft.Paint", "Paint", "Paint.", false),
                App("Microsoft.WindowsNotepad", "Notepad", "Store Notepad.", false),
                App("Microsoft.WindowsTerminal", "Terminal", "Windows Terminal.", false)
            };
        }

        static AppDef App(string package, string title, string description, bool common)
        {
            return new AppDef(package, title, description, common);
        }
    }
}
