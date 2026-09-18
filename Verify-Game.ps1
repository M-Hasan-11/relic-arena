param([int]$TimeoutSeconds = 180, [string]$BuildFolder = 'Builds/Windows')
$ErrorActionPreference = 'Stop'
$gameExe = Join-Path (Join-Path $PSScriptRoot $BuildFolder) 'RelicArena.exe'
if (!(Test-Path -LiteralPath $gameExe)) { throw 'Build Windows in Unity before running verification.' }
$verificationDir = Join-Path (Join-Path $PSScriptRoot $BuildFolder) 'Verification'
New-Item -ItemType Directory -Force -Path $verificationDir | Out-Null
$logPath = Join-Path $verificationDir 'player.log'
$reportPath = Join-Path $verificationDir 'smoke-results.txt'
if (Test-Path -LiteralPath $reportPath) { Remove-Item -LiteralPath $reportPath }
$arguments = '-relicSmoke -screen-fullscreen 0 -screen-width 1440 -screen-height 900 -logFile "' + $logPath + '"'
# A visible game window is required for Unity to render screenshot frames on Windows.
$gameProcess = Start-Process -FilePath $gameExe -ArgumentList $arguments -WindowStyle Normal -PassThru
if (!$gameProcess.WaitForExit($TimeoutSeconds * 1000)) {
    Stop-Process -Id $gameProcess.Id
    throw "Game verification timed out. See $logPath"
}
if (!(Test-Path -LiteralPath $reportPath)) { throw "No test report was produced. See $logPath" }
Get-Content -LiteralPath $reportPath
if ((Get-Content -LiteralPath $reportPath | Select-String '^FAIL') -or $gameProcess.ExitCode -ne 0) {
    throw 'Game verification failed.'
}
Write-Output "All smoke checks passed. Screenshots are in $BuildFolder\Verification."

