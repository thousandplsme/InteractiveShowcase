param([string]$PythonExecutable, [string]$EnvironmentPath)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
try {
    if (-not $PythonExecutable) {
        $candidate = Join-Path $env:LOCALAPPDATA 'Programs\Python\Python311\python.exe'
        if (Test-Path -LiteralPath $candidate) { $PythonExecutable = $candidate }
        elseif (Get-Command py.exe -ErrorAction SilentlyContinue) {
            $PythonExecutable = & py.exe -3.11 -c 'import sys; print(sys.executable)'
            if ($LASTEXITCODE -ne 0) { throw 'Python 3.11 x64 is required. Specify -PythonExecutable with its full path.' }
        }
        else { throw 'Python 3.11 x64 is required. Specify -PythonExecutable with its full path.' }
    }
    if (-not $EnvironmentPath) { $EnvironmentPath = Join-Path $projectRoot 'vision\.venv' }
    & $PythonExecutable (Join-Path $projectRoot 'vision\setup_environment.py') --environment $EnvironmentPath
    if ($LASTEXITCODE -ne 0) { throw "Setup failed (exit $LASTEXITCODE). Existing environment has not been deleted." }
    Write-Host 'Setup completed. Run CheckEnvironment.cmd, then start Unity and StartVision.cmd.'
    exit 0
}
catch { Write-Host $_.Exception.Message; exit 1 }
