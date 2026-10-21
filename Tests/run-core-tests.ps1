$ErrorActionPreference = 'Stop'
$project = Split-Path $PSScriptRoot -Parent
$out = Join-Path $project 'TestResults'
New-Item -ItemType Directory -Force -Path $out | Out-Null
$sources = @(Get-ChildItem -LiteralPath (Join-Path $project 'Assets/Mobility/Core') -Filter '*.cs' | ForEach-Object FullName)
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:exe "/out:$out/CoreTests.exe" $sources (Join-Path $PSScriptRoot 'CoreTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Core compilation failed' }
& (Join-Path $out 'CoreTests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed' }
