param()

$ErrorActionPreference = 'Stop'

$version = '3.4.14'
$expectedSha256 = '69A4E55645651AF85E6CCFE40981B5A0BC2C594D0004FE7844DB680E23CFBDAF'
$archiveName = "SDL3-$version-win32-x64.zip"
$downloadUrl = "https://github.com/libsdl-org/SDL/releases/download/release-$version/$archiveName"
$nativeDirectory = Join-Path $PSScriptRoot 'native'
$archivePath = Join-Path $nativeDirectory $archiveName
$extractDirectory = Join-Path $nativeDirectory "_extract_$version"
$destination = Join-Path $nativeDirectory 'SDL3.dll'

New-Item -ItemType Directory -Path $nativeDirectory -Force | Out-Null
Invoke-WebRequest -Uri $downloadUrl -OutFile $archivePath

$actualSha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $archivePath).Hash
if ($actualSha256 -ne $expectedSha256) {
    throw "SDL archive SHA-256 mismatch: $actualSha256"
}

if (Test-Path -LiteralPath $extractDirectory) {
    Remove-Item -LiteralPath $extractDirectory -Recurse -Force
}
Expand-Archive -LiteralPath $archivePath -DestinationPath $extractDirectory

$source = Join-Path $extractDirectory 'SDL3.dll'
if (-not (Test-Path -LiteralPath $source)) {
    throw "SDL3.dll was not found in $archiveName"
}
Copy-Item -LiteralPath $source -Destination $destination -Force
Remove-Item -LiteralPath $extractDirectory -Recurse -Force

Write-Output "Installed $destination"
Write-Output "Archive SHA-256: $actualSha256"
