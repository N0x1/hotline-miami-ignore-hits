$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'build.ps1')
$taskExe = Join-Path $PSScriptRoot 'dist\HotlineMiami-IgnoreHits-v1.0.exe'
$taskResult = Join-Path $PSScriptRoot 'verification-memory.txt'
$taskRun = Start-Process -FilePath $taskExe -ArgumentList '--self-test', ('"' + $taskResult + '"') -WindowStyle Hidden -Wait -PassThru
if ($taskRun.ExitCode -ne 0) { throw 'Self-tests failed.' }
$taskTests = Join-Path $PSScriptRoot 'tests\bin'
New-Item -ItemType Directory -Force -Path $taskTests | Out-Null
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $taskCompiler /nologo /target:exe /platform:x86 /out:"$taskTests\ScoreFixture.exe" "$PSScriptRoot\tests\ScoreFixture.cs"
if ($LASTEXITCODE -ne 0) { throw 'Fixture compilation failed.' }
& $taskCompiler /nologo /target:exe /platform:x64 /main:NativeScoreTest /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /out:"$taskTests\NativeScoreTest.exe" "$PSScriptRoot\src\IgnoreHits.cs" "$PSScriptRoot\src\Definitions.cs" "$PSScriptRoot\src\ScoreBoost.cs" "$PSScriptRoot\tests\NativeScoreTest.cs"
if ($LASTEXITCODE -ne 0) { throw 'Native test compilation failed.' }
& "$taskTests\NativeScoreTest.exe" "$taskTests\ScoreFixture.exe" "$PSScriptRoot\verification-native-score.txt"
if ($LASTEXITCODE -ne 0) { throw 'Native score test failed.' }
Get-Content $taskResult
Get-Content (Join-Path $PSScriptRoot 'verification-native-score.txt')
