param(
    [string]$UnityPath = $env:UNITY_PATH
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ResultsDir = Join-Path $ProjectRoot "TestResults"

if (-not $UnityPath -or -not (Test-Path $UnityPath)) {
    $candidates = @(
        "${env:ProgramFiles}\Unity\Hub\Editor\*\Editor\Unity.exe",
        "${env:ProgramFiles(x86)}\Unity\Hub\Editor\*\Editor\Unity.exe"
    )
    foreach ($pattern in $candidates) {
        $found = Get-ChildItem -Path $pattern -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($found) {
            $UnityPath = $found.FullName
            break
        }
    }
}

if (-not $UnityPath -or -not (Test-Path $UnityPath)) {
    Write-Error "找不到 Unity.exe。请设置环境变量 UNITY_PATH 或安装 Unity Hub 编辑器。"
}

New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null

$xml = Join-Path $ResultsDir "unity_test_results.xml"
$log = Join-Path $ResultsDir "unity_test.log"

Write-Host "Project: $ProjectRoot"
Write-Host "Unity:   $UnityPath"
Write-Host "Running EditMode tests..."

& $UnityPath `
    -batchmode `
    -nographics `
    -projectPath $ProjectRoot `
    -runTests `
    -testPlatform editmode `
    -assemblyNames "Assembly-CSharp-Editor" `
    -testResults $xml `
    -logFile $log

$exit = $LASTEXITCODE
Write-Host "Unity exit code: $exit"
Write-Host "See also: TestResults/vc5_logic_latest.json"

if ($exit -ne 0) { exit $exit }
