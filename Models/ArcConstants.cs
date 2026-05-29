namespace Arc.Models;

public static class ArcConstants
{
    public static readonly SearchResult[] WindowsSettings =
    [
        new() { Id="ws:network",   Type=ResultType.App, Name="Network & Internet",        Subtitle="ms-settings:network",              LucideIcon="wifi",        ExePath="ms-settings:network" },
        new() { Id="ws:wifi",      Type=ResultType.App, Name="Wi-Fi Settings",             Subtitle="ms-settings:network-wifi",         LucideIcon="wifi",        ExePath="ms-settings:network-wifi" },
        new() { Id="ws:cellular",  Type=ResultType.App, Name="Cellular & Mobile Data",     Subtitle="ms-settings:network-cellular",     LucideIcon="signal",      ExePath="ms-settings:network-cellular" },
        new() { Id="ws:airplane",  Type=ResultType.App, Name="Airplane Mode",              Subtitle="ms-settings:network-airplanemode",  LucideIcon="plane",       ExePath="ms-settings:network-airplanemode" },
        new() { Id="ws:hotspot",   Type=ResultType.App, Name="Mobile Hotspot",             Subtitle="ms-settings:network-mobilehotspot", LucideIcon="share-2",     ExePath="ms-settings:network-mobilehotspot" },
        new() { Id="ws:bt",        Type=ResultType.App, Name="Bluetooth & Devices",        Subtitle="ms-settings:bluetooth",            LucideIcon="bluetooth",   ExePath="ms-settings:bluetooth" },
        new() { Id="ws:display",   Type=ResultType.App, Name="Display & Brightness",       Subtitle="ms-settings:display",              LucideIcon="monitor",     ExePath="ms-settings:display" },
        new() { Id="ws:sound",     Type=ResultType.App, Name="Sound & Audio",              Subtitle="ms-settings:sound",                LucideIcon="volume-2",    ExePath="ms-settings:sound" },
        new() { Id="ws:power",     Type=ResultType.App, Name="Power, Sleep & Lid",         Subtitle="ms-settings:powersleep",           LucideIcon="battery",     ExePath="ms-settings:powersleep" },
        new() { Id="ws:battery",   Type=ResultType.App, Name="Battery Saver",              Subtitle="ms-settings:batterysaver",         LucideIcon="battery",     ExePath="ms-settings:batterysaver" },
        new() { Id="ws:update",    Type=ResultType.App, Name="Windows Update",             Subtitle="ms-settings:windowsupdate",        LucideIcon="refresh-cw",  ExePath="ms-settings:windowsupdate" },
        new() { Id="ws:privacy",   Type=ResultType.App, Name="Privacy & Security",         Subtitle="ms-settings:privacy",              LucideIcon="shield",      ExePath="ms-settings:privacy" },
        new() { Id="ws:apps",      Type=ResultType.App, Name="Apps & Features",            Subtitle="ms-settings:appsfeatures",         LucideIcon="package",     ExePath="ms-settings:appsfeatures" },
        new() { Id="ws:default",   Type=ResultType.App, Name="Default Apps",               Subtitle="ms-settings:defaultapps",          LucideIcon="layout-grid", ExePath="ms-settings:defaultapps" },
        new() { Id="ws:notif",     Type=ResultType.App, Name="Notifications & Alerts",     Subtitle="ms-settings:notifications",        LucideIcon="bell",        ExePath="ms-settings:notifications" },
        new() { Id="ws:themes",    Type=ResultType.App, Name="Personalization & Themes",   Subtitle="ms-settings:personalization",      LucideIcon="palette",     ExePath="ms-settings:personalization" },
        new() { Id="ws:accounts",  Type=ResultType.App, Name="Accounts & Users",           Subtitle="ms-settings:accounts",             LucideIcon="user",        ExePath="ms-settings:accounts" },
        new() { Id="ws:datetime",  Type=ResultType.App, Name="Date, Time & Clock",         Subtitle="ms-settings:dateandtime",          LucideIcon="clock",       ExePath="ms-settings:dateandtime" },
        new() { Id="ws:region",    Type=ResultType.App, Name="Language & Region",          Subtitle="ms-settings:regionlanguage",       LucideIcon="globe",       ExePath="ms-settings:regionlanguage" },
        new() { Id="ws:access",    Type=ResultType.App, Name="Ease of Access",             Subtitle="ms-settings:easeofaccess-display", LucideIcon="eye",         ExePath="ms-settings:easeofaccess-display" },
        new() { Id="ws:storage",   Type=ResultType.App, Name="Storage & Disk Space",       Subtitle="ms-settings:storagesense",         LucideIcon="hard-drive",  ExePath="ms-settings:storagesense" },
        new() { Id="ws:about",     Type=ResultType.App, Name="About This PC",              Subtitle="ms-settings:about",                LucideIcon="info",        ExePath="ms-settings:about" },
        new() { Id="ws:taskmgr",   Type=ResultType.App, Name="Task Manager",               Subtitle="taskmgr.exe",                      LucideIcon="activity",    ExePath="taskmgr.exe" },
        new() { Id="ws:devmgmt",   Type=ResultType.App, Name="Device Manager",             Subtitle="devmgmt.msc",                      LucideIcon="cpu",         ExePath="devmgmt.msc" },
    ];

    public static readonly SearchResult[] StaticActions =
    [
        new() { Id = "act:timer",    Type = ResultType.Action, Name = "Start Timer",   Subtitle = "Type 'timer 5m' to start a countdown",          LucideIcon = "timer",      ActionId = "timer"    },
        new() { Id = "act:calc",     Type = ResultType.Action, Name = "Calculator",    Subtitle = "Type a math expression like '100 / 4'",          LucideIcon = "calculator", ActionId = "calc"     },
        new() { Id = "act:color",    Type = ResultType.Action, Name = "Color Picker",  Subtitle = "Type a hex code like '#ff0055'",                 LucideIcon = "palette",    ActionId = "color"    },
        new() { Id = "act:ip",       Type = ResultType.Action, Name = "IP Address",    Subtitle = "Type 'ip' to show your public and local IP",     LucideIcon = "globe",      ActionId = "ip"       },
        new() { Id = "act:ai",       Type = ResultType.Action, Name = "Ask AI",        Subtitle = "Type 'ai what is the capital of France?'",       LucideIcon = "sparkles",   ActionId = "ai"       },
        new() { Id = "act:settings", Type = ResultType.Action, Name = "Settings",      Subtitle = "Open application settings",                     LucideIcon = "settings",   ActionId = "settings" },
    ];
}
