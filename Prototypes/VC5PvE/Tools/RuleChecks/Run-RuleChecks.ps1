$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$repoRoot = (Resolve-Path (Join-Path $projectRoot '..\..\..\..')).Path
$unityRoot = ('D:\unity\' + [char]0x5B89 + [char]0x88C5 + [char]0x4F4D + [char]0x7F6E + '\2021.3.33f1c1\Editor\Data\MonoBleedingEdge')
$csc = Join-Path $unityRoot 'lib\mono\4.5\csc.exe'
$mono = Join-Path $unityRoot 'bin\mono.exe'
$output = Join-Path $env:TEMP ('VC5PvE-RuleChecks-' + $PID + '.exe')
$sources = @(
    (Join-Path $projectRoot 'Assets\VC5PvE\Rules\*.cs'),
    (Join-Path $projectRoot 'Assets\VC5PvE\Tests\Editor\RulesAcceptanceTests.cs'),
    (Join-Path $PSScriptRoot 'NUnitLite.cs'),
    (Join-Path $PSScriptRoot 'Program.cs')
)
$expandedSources = @()
foreach ($source in $sources) { $expandedSources += Get-ChildItem $source -ErrorAction SilentlyContinue | ForEach-Object FullName }
if ($expandedSources.Count -eq 0) { throw 'No rule or test sources found.' }
& $mono $csc /nologo /langversion:latest /out:$output $expandedSources
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $mono $output
$result = $LASTEXITCODE
Remove-Item -LiteralPath $output -Force -ErrorAction SilentlyContinue
exit $result
