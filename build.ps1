$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$taskOutput = Join-Path $taskRoot 'dist'
New-Item -ItemType Directory -Force -Path $taskOutput | Out-Null
$taskCompiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $taskCompiler /nologo /target:winexe /platform:anycpu /optimize+ /warnaserror+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /out:"$taskOutput\HotlineMiami-IgnoreHits-v1.0.1.exe" "$taskRoot\src\IgnoreHits.cs" "$taskRoot\src\Definitions.cs" "$taskRoot\src\ScoreBoost.cs"
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Write-Output "Built $taskOutput\HotlineMiami-IgnoreHits-v1.0.1.exe"
