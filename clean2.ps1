$path = 'c:\dev\Arc\Views\PreviewPanel.xaml.cs'
$content = Get-Content $path -Raw

$content = $content -replace "_vm\.TimerDisplay", "_vm.Timer?.TimerDisplay"
$content = $content -replace "_vm\.TimerProgress", "(_vm.Timer?.TimerProgress ?? 0)"
$content = $content -replace "_vm\.TimerRunning", "(_vm.Timer?.TimerRunning ?? false)"
$content = $content -replace "_vm\.AiText", "_vm.Ai?.AiText"
$content = $content -replace "_vm\.AiLoading", "(_vm.Ai?.AiLoading ?? false)"
$content = $content -replace "_vm\.AiError", "_vm.Ai?.AiError"
$content = $content -replace "_vm\.AiConversation", "_vm.Ai?.AiConversation"
$content = $content -replace "_vm\.AiFollowUpCommand", "_vm.Ai?.AiFollowUpCommand"
$content = $content -replace "_vm\.ConversationChanged", "_vm.Ai.ConversationChanged"
$content = $content -replace "_vm\?.CancelTimerCommand", "_vm?.Timer?.CancelCommand"

# In OnVmChanged, the property names were nameof(MainViewModel.TimerDisplay) etc.
$content = $content -replace "nameof\(MainViewModel\.TimerDisplay\)", "nameof(Arc.ViewModels.TimerViewModel.TimerDisplay)"
$content = $content -replace "nameof\(MainViewModel\.TimerProgress\)", "nameof(Arc.ViewModels.TimerViewModel.TimerProgress)"
$content = $content -replace "nameof\(MainViewModel\.TimerRunning\)", "nameof(Arc.ViewModels.TimerViewModel.TimerRunning)"
$content = $content -replace "nameof\(MainViewModel\.AiText\)", "nameof(Arc.ViewModels.AiChatViewModel.AiText)"
$content = $content -replace "nameof\(MainViewModel\.AiLoading\)", "nameof(Arc.ViewModels.AiChatViewModel.AiLoading)"
$content = $content -replace "nameof\(MainViewModel\.AiError\)", "nameof(Arc.ViewModels.AiChatViewModel.AiError)"

[System.IO.File]::WriteAllText($path, $content)
