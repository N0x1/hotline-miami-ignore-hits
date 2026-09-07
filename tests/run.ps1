$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $taskRoot 'build.ps1')
$taskExe = Join-Path $taskRoot 'dist\HotlineMiami-IgnoreHits-v1.0.1.exe'
$taskResult = Join-Path $taskRoot 'verification-memory.txt'
$taskRun = Start-Process -FilePath $taskExe -ArgumentList '--self-test', ('"' + $taskResult + '"') -WindowStyle Hidden -Wait -PassThru
if ($taskRun.ExitCode -ne 0) { throw 'Self-tests failed.' }
$taskTests = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force -Path $taskTests | Out-Null
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $taskCompiler /nologo /target:exe /platform:x86 /out:"$taskTests\ScoreFixture.exe" "$PSScriptRoot\ScoreFixture.cs"
if ($LASTEXITCODE -ne 0) { throw 'Fixture compilation failed.' }
& $taskCompiler /nologo /target:exe /platform:x64 /main:NativeScoreTest /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /out:"$taskTests\NativeScoreTest.exe" "$taskRoot\src\IgnoreHits.cs" "$taskRoot\src\Definitions.cs" "$taskRoot\src\ScoreBoost.cs" "$PSScriptRoot\NativeScoreTest.cs"
if ($LASTEXITCODE -ne 0) { throw 'Native test compilation failed.' }
& "$taskTests\NativeScoreTest.exe" "$taskTests\ScoreFixture.exe" "$taskRoot\verification-native-score.txt"
if ($LASTEXITCODE -ne 0) { throw 'Native score test failed.' }
Get-Content $taskResult
Get-Content (Join-Path $taskRoot 'verification-native-score.txt')
