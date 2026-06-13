namespace Spur.Models;

public static class SpurConstants
{
    public static readonly SearchResult[] WindowsSettings =
    [
        new() { Id="ws:network",   Type=ResultType.App, Name="Network & Internet",        Subtitle="ms-settings:network",              IconGlyph = "\ue701",  IconPath = "/Assets/Icons/url.png",        ExePath="ms-settings:network" },
        new() { Id="ws:wifi",      Type=ResultType.App, Name="Wi-Fi Settings",             Subtitle="ms-settings:network-wifi",         IconGlyph = "\ue701",  IconPath = "/Assets/Icons/url.png",        ExePath="ms-settings:network-wifi" },
        new() { Id="ws:cellular",  Type=ResultType.App, Name="Cellular & Mobile Data",     Subtitle="ms-settings:network-cellular",     IconGlyph = "\ue871",  IconPath = "/Assets/Icons/url.png",        ExePath="ms-settings:network-cellular" },
        new() { Id="ws:airplane",  Type=ResultType.App, Name="Airplane Mode",              Subtitle="ms-settings:network-airplanemode", IconGlyph = "\ue709",  IconPath = "/Assets/Icons/url.png",        ExePath="ms-settings:network-airplanemode" },
        new() { Id="ws:hotspot",   Type=ResultType.App, Name="Mobile Hotspot",             Subtitle="ms-settings:network-mobilehotspot",IconGlyph = "\ue72d",  IconPath = "/Assets/Icons/url.png",        ExePath="ms-settings:network-mobilehotspot" },
        new() { Id="ws:bt",        Type=ResultType.App, Name="Bluetooth & Devices",        Subtitle="ms-settings:bluetooth",            IconGlyph = "\ue702",  IconPath = "/Assets/Icons/settings.png",   ExePath="ms-settings:bluetooth" },
        new() { Id="ws:display",   Type=ResultType.App, Name="Display & Brightness",       Subtitle="ms-settings:display",              IconGlyph = "\ue7f4",  IconPath = "/Assets/Icons/image.png",      ExePath="ms-settings:display" },
        new() { Id="ws:sound",     Type=ResultType.App, Name="Sound & Audio",              Subtitle="ms-settings:sound",                IconGlyph = "\ue767",  IconPath = "/Assets/Icons/settings.png",   ExePath="ms-settings:sound" },
        new() { Id="ws:power",     Type=ResultType.App, Name="Power, Sleep & Lid",         Subtitle="ms-settings:powersleep",           IconGlyph = "\ue83f",  IconPath = "/Assets/Icons/settings.png",   ExePath="ms-settings:powersleep" },
        new() { Id="ws:battery",   Type=ResultType.App, Name="Battery Saver",              Subtitle="ms-settings:batterysaver",         IconGlyph = "\ue83f",  IconPath = "/Assets/Icons/settings.png",   ExePath="ms-settings:batterysaver" },
        new() { Id="ws:update",    Type=ResultType.App, Name="Windows Update",             Subtitle="ms-settings:windowsupdate",        IconGlyph = "\ue72c",  IconPath = "/Assets/Icons/restart.png",    ExePath="ms-settings:windowsupdate" },
        new() { Id="ws:privacy",   Type=ResultType.App, Name="Privacy & Security",         Subtitle="ms-settings:privacy",              IconGlyph = "\ueea1",  IconPath = "/Assets/Icons/lock.png",       ExePath="ms-settings:privacy" },
        new() { Id="ws:apps",      Type=ResultType.App, Name="Apps & Features",            Subtitle="ms-settings:appsfeatures",         IconGlyph = "\ue718",  IconPath = "/Assets/Icons/program.png",    ExePath="ms-settings:appsfeatures" },
        new() { Id="ws:default",   Type=ResultType.App, Name="Default Apps",               Subtitle="ms-settings:defaultapps",          IconGlyph = "\ue71c",  IconPath = "/Assets/Icons/program.png",    ExePath="ms-settings:defaultapps" },
        new() { Id="ws:notif",     Type=ResultType.App, Name="Notifications & Alerts",     Subtitle="ms-settings:notifications",        IconGlyph = "\uea8f",  IconPath = "/Assets/Icons/info.png",       ExePath="ms-settings:notifications" },
        new() { Id="ws:themes",    Type=ResultType.App, Name="Personalization & Themes",   Subtitle="ms-settings:personalization",      IconGlyph = "\ue790",  IconPath = "/Assets/Icons/color.png",      ExePath="ms-settings:personalization" },
        new() { Id="ws:accounts",  Type=ResultType.App, Name="Accounts & Users",           Subtitle="ms-settings:accounts",             IconGlyph = "\ue77b",  IconPath = "/Assets/Icons/settings.png",   ExePath="ms-settings:accounts" },
        new() { Id="ws:datetime",  Type=ResultType.App, Name="Date, Time & Clock",         Subtitle="ms-settings:dateandtime",          IconGlyph = "\ue121",  IconPath = "/Assets/Icons/history.png",    ExePath="ms-settings:dateandtime" },
        new() { Id="ws:region",    Type=ResultType.App, Name="Language & Region",          Subtitle="ms-settings:regionlanguage",       IconGlyph = "\ue12b",  IconPath = "/Assets/Icons/keyboard.png",   ExePath="ms-settings:regionlanguage" },
        new() { Id="ws:access",    Type=ResultType.App, Name="Ease of Access",             Subtitle="ms-settings:easeofaccess-display", IconGlyph = "\ue890",  IconPath = "/Assets/Icons/settings.png",   ExePath="ms-settings:easeofaccess-display" },
        new() { Id="ws:storage",   Type=ResultType.App, Name="Storage & Disk Space",       Subtitle="ms-settings:storagesense",         IconGlyph = "\ue105",  IconPath = "/Assets/Icons/folder.png",     ExePath="ms-settings:storagesense" },
        new() { Id="ws:about",     Type=ResultType.App, Name="About This PC",              Subtitle="ms-settings:about",                IconGlyph = "\ue946",  IconPath = "/Assets/Icons/info.png",       ExePath="ms-settings:about" },
        new() { Id="ws:taskmgr",   Type=ResultType.App, Name="Task Manager",               Subtitle="taskmgr.exe",                      IconGlyph = "\ue9d9",  IconPath = "/Assets/Icons/program.png",    ExePath="taskmgr.exe" },
        new() { Id="ws:devmgmt",   Type=ResultType.App, Name="Device Manager",             Subtitle="devmgmt.msc",                      IconGlyph = "\ue950",  IconPath = "/Assets/Icons/settings.png",   ExePath="devmgmt.msc" },
    ];

    public static readonly SearchResult[] StaticActions =
    [
        new() { Id = "act:timer",    Type = ResultType.Action, Name = "Start Timer",   Subtitle = "Type 'timer 5m' to start a countdown",          IconGlyph = "\ue121",  IconPath = "/Assets/Icons/history.png",     ActionId = "timer"    },
        new() { Id = "act:calc",     Type = ResultType.Action, Name = "Calculator",    Subtitle = "Type a math expression like '100 / 4'",          IconGlyph = "\ue1d0",  IconPath = "/Assets/Icons/calculator.png",  ActionId = "calc"     },
        new() { Id = "act:color",    Type = ResultType.Action, Name = "Color Picker",  Subtitle = "Type a hex code like '#ff0055'",                 IconGlyph = "\ue790",  IconPath = "/Assets/Icons/color.png",       ActionId = "color"    },
        new() { Id = "act:ip",       Type = ResultType.Action, Name = "IP Address",    Subtitle = "Type 'ip' to show your public and local IP",     IconGlyph = "\ue12b",  IconPath = "/Assets/Icons/url.png",         ActionId = "ip"       },
        new() { Id = "act:ai",       Type = ResultType.Action, Name = "Ask AI",        Subtitle = "Chat with AI assistant",                         IconGlyph = "\ue113",  IconPath = "/Assets/Icons/find.png",        ActionId = "ai"       },
        new() { Id = "act:settings", Type = ResultType.Action, Name = "Settings",      Subtitle = "Open application settings",                     IconGlyph = "\ue713",  IconPath = "/Assets/Icons/settings.png",    ActionId = "settings" },
        new() { Id = "act:currency", Type = ResultType.Action, Name = "Currency",      Subtitle = "Type '100 usd to eur' to convert",               IconGlyph = "\ue825",  IconPath = "/Assets/Icons/calculator.png",  ActionId = "currency" },
        new() { Id = "act:pw",       Type = ResultType.Action, Name = "Password Gen",  Subtitle = "Type 'pw 16' to generate a password",            IconGlyph = "\ue8d7",  IconPath = "/Assets/Icons/lock.png",        ActionId = "pw"       },
        new() { Id = "act:note",    Type = ResultType.Action, Name = "Quick Note",    Subtitle = "Type 'note your text' to save a note",           IconGlyph = "\ue8a5",  IconPath = "/Assets/Icons/copy.png",        ActionId = "note"     },
        new() { Id = "act:kill",    Type = ResultType.Action, Name = "Kill Process",  Subtitle = "Type 'kill notepad' to force-close",             IconGlyph = "\ue711",  IconPath = "/Assets/Icons/shell.png",       ActionId = "kill"     },
        new() { Id = "act:screenshot",Type = ResultType.Action,Name = "Screenshot",   Subtitle = "Type 'screenshot' to capture the screen",         IconGlyph = "\ue722",  IconPath = "/Assets/Icons/image.png",       ActionId = "screenshot"},
    ];
}


