$path = 'c:\dev\Arc\ViewModels\MainViewModel.cs'
$lines = Get-Content $path -Raw
$lines = $lines -split "`r`n"
if ($lines.Length -eq 1) { $lines = $lines -split "`n" }

$startIdx = -1
$endIdx = -1

for ($i = 0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match "private async Task StartAiAsync") { $startIdx = $i }
    if ($lines[$i] -match "private void CancelTimer\(\)") { $endIdx = $i + 6 }
}

if ($startIdx -ge 0 -and $endIdx -gt $startIdx) {
    $lines = $lines[0..($startIdx-1)] + $lines[($endIdx+1)..($lines.Length-1)]
}

for ($i = 0; $i -lt $lines.Length; $i++) {
    $lines[$i] = $lines[$i] -replace "StartTimerCommand\.Execute\(null\);", "Timer.StartCommand.Execute(null);"
    $lines[$i] = $lines[$i] -replace "StartAiAsync\(Query\)", "Ai.StartAiAsync(Query)"
    $lines[$i] = $lines[$i] -replace "CancelTimerCommand\.Execute\(null\);", "Timer.CancelCommand.Execute(null);"
}

[System.IO.File]::WriteAllLines($path, $lines)
