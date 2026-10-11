param(
    [switch]$Test,
    [switch]$Render,
    [string]$OutputDirectory = 'dist'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$appName = 'ROTK-CPU-Manager'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The .NET Framework compiler is missing. Windows 10/11 x64 is required.'
}

$buildFolder = [IO.Path]::GetFullPath((Join-Path $projectRoot $OutputDirectory))
New-Item -ItemType Directory -Path $buildFolder -Force | Out-Null
$sources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'src') -Filter '*.cs' | Sort-Object Name | Select-Object -ExpandProperty FullName)
$references = @('/r:System.dll', '/r:System.Core.dll', '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll', '/r:System.Xml.dll')
$iconPath = Join-Path $buildFolder "$appName.ico"

function Write-AppIcon {
    Add-Type -AssemblyName System.Drawing
    if (-not ('RotkIconHandle' -as [type])) {
        Add-Type -TypeDefinition 'public static class RotkIconHandle { [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool DestroyIcon(System.IntPtr handle); }'
    }
    $bitmap = New-Object Drawing.Bitmap(64, 64)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $red = New-Object Drawing.SolidBrush([Drawing.Color]::FromArgb(209, 50, 32))
    $font = New-Object Drawing.Font('Impact', 43, [Drawing.FontStyle]::Regular, [Drawing.GraphicsUnit]::Pixel)
    try {
        $graphics.Clear([Drawing.Color]::FromArgb(17, 18, 19))
        $graphics.FillRectangle($red, 4, 4, 56, 56)
        $graphics.DrawString('R', $font, [Drawing.Brushes]::White, 15, 4)
        $graphics.FillRectangle([Drawing.Brushes]::Black, 12, 28, 40, 2)
        $handle = $bitmap.GetHicon()
        try {
            $icon = [Drawing.Icon]::FromHandle($handle)
            try {
                $stream = [IO.File]::Create($iconPath)
                try { $icon.Save($stream) }
                finally { $stream.Dispose() }
            }
            finally { $icon.Dispose() }
        }
        finally { [RotkIconHandle]::DestroyIcon($handle) | Out-Null }
    }
    finally {
        $font.Dispose()
        $red.Dispose()
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function Build-Executable {
    param([string]$Path, [string[]]$Inputs, [string]$Target = 'winexe', [string]$Language = '')
    $options = @('/nologo', '/optimize+', '/warn:4', '/warnaserror+', '/platform:x64', "/target:$Target", "/out:$Path", "/win32icon:$iconPath", "/win32manifest:$projectRoot\src\app.manifest")
    if ($Language) { $options += "/define:FORCE_$Language" }
    & $compiler @options @references @Inputs
    if ($LASTEXITCODE -ne 0) { throw "Compilation failed: $Path" }
}

function Write-PortablePackage {
    param([string]$Name, [string[]]$Instructions)
    $executable = Join-Path $buildFolder "$Name.exe"
    $checksum = Join-Path $buildFolder "$Name.sha256"
    $hash = (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText($checksum, "$hash  $Name.exe`r`n", [Text.Encoding]::ASCII)
    $files = @($executable, $checksum) + @($Instructions + @('LICENSE', 'NOTICE.txt') | ForEach-Object { Join-Path $buildFolder $_ })
    Compress-Archive -LiteralPath $files -DestinationPath (Join-Path $buildFolder "$Name-portable.zip") -Force
}

Write-AppIcon
$exePath = Join-Path $buildFolder "$appName.exe"
Build-Executable -Path $exePath -Inputs $sources

if ($Test -or $Render) {
    $testSources = @($sources | Where-Object { [IO.Path]::GetFileName($_) -ne 'Program.cs' }) + (Join-Path $projectRoot 'tests\Tests.cs')
    $testExe = Join-Path $buildFolder "$appName.Tests.exe"
    Build-Executable -Path $testExe -Inputs $testSources -Target 'exe'
    if ($Test) {
        & $testExe
        if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
    }
    if ($Render) {
        & $testExe --render (Join-Path $buildFolder 'preview.png')
        if ($LASTEXITCODE -ne 0) { throw 'Preview rendering failed.' }
    }
}

foreach ($file in @('LIRE-MOI.txt', 'READ-ME.txt', 'LICENSE', 'NOTICE.txt')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $file) -Destination $buildFolder -Force
}
Write-PortablePackage -Name $appName -Instructions @('LIRE-MOI.txt', 'READ-ME.txt')

foreach ($language in @('FR', 'EN')) {
    $name = "$appName-$language"
    Build-Executable -Path (Join-Path $buildFolder "$name.exe") -Inputs $sources -Language $language
    if ($Test) {
        $variantTest = Join-Path $buildFolder "$appName.Tests.$language.exe"
        Build-Executable -Path $variantTest -Inputs $testSources -Target 'exe' -Language $language
        & $variantTest --check-language $language.ToLowerInvariant()
        if ($LASTEXITCODE -ne 0) { throw "Language check failed: $language" }
    }
    $instructions = if ($language -eq 'FR') { 'LIRE-MOI.txt' } else { 'READ-ME.txt' }
    Write-PortablePackage -Name $name -Instructions @($instructions)
}

Write-Output "Built: $exePath"
