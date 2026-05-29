$usings = @"
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

"@

$files = Get-ChildItem -Path "c:\dev\Arc" -Recurse -Filter "*.cs" | Where-Object { $_.FullName -notmatch "\\obj\\" -and $_.FullName -notmatch "\\bin\\" }

foreach ($file in $files) {
    # Skip GlobalUsings.cs
    if ($file.Name -eq "GlobalUsings.cs") { continue }
    
    $content = Get-Content $file.FullName -Raw
    
    # Check if we already added it or it already has using System.Windows;
    if ($content -notmatch "using System\.Windows;") {
        # Only add to UI related folders or ViewModels
        if ($file.FullName -match "\\Views\\" -or $file.FullName -match "\\ViewModels\\" -or $file.FullName -match "\\Converters\\" -or $file.FullName -match "\\Extensions\\" -or $file.Name -eq "App.xaml.cs" -or $file.Name -eq "MainWindow.xaml.cs") {
            $content = $usings + $content
            [System.IO.File]::WriteAllText($file.FullName, $content)
        }
    }
}
