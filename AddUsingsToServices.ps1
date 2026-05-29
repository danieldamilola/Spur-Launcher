$filesToAddWindows = @(
    "c:\dev\Arc\Services\ThemeManager.cs",
    "c:\dev\Arc\Services\NotificationService.cs",
    "c:\dev\Arc\Services\IconService.cs",
    "c:\dev\Arc\Services\ClipboardService.cs"
)

foreach ($f in $filesToAddWindows) {
    $content = Get-Content $f -Raw
    $content = "using System.Windows;`n" + $content
    [System.IO.File]::WriteAllText($f, $content)
}

$hotkey = "c:\dev\Arc\Services\HotkeyService.cs"
$content = Get-Content $hotkey -Raw
$content = "using System.Windows.Input;`n" + $content
[System.IO.File]::WriteAllText($hotkey, $content)
