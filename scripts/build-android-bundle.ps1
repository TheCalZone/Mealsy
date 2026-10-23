[CmdletBinding()]
param(
    [string]$DotNet = 'dotnet',
    [Parameter(Mandatory)][string]$AndroidSdkDirectory,
    [Parameter(Mandatory)][string]$JavaSdkDirectory,
    [Parameter(Mandatory)][string]$SigningKeyStore,
    [Parameter(Mandatory)][string]$SigningPasswordFile,
    [string]$SigningKeyAlias = 'mealsy-upload',
    [ValidateSet('android-arm64','android-x64')][string]$RuntimeIdentifier = 'android-arm64'
)
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '../RecipeApp/RecipeApp.csproj'
$keyPath = (Resolve-Path -LiteralPath $SigningKeyStore).Path
$passwordPath = (Resolve-Path -LiteralPath $SigningPasswordFile).Path
# Pass a file reference, never read the password into arguments or build logs.
# Explicit Android RID avoids host-RID inference by dotnet publish.
& $DotNet publish $project -f net10.0-android -c Release -r $RuntimeIdentifier `
    "-p:AndroidSdkDirectory=$AndroidSdkDirectory" "-p:JavaSdkDirectory=$JavaSdkDirectory" `
    -p:AndroidPackageFormats=aab -p:AndroidKeyStore=true `
    "-p:AndroidSigningKeyStore=$keyPath" "-p:AndroidSigningKeyAlias=$SigningKeyAlias" `
    "-p:AndroidSigningStorePass=file:$passwordPath" "-p:AndroidSigningKeyPass=file:$passwordPath"
if ($LASTEXITCODE -ne 0) { throw 'Android bundle publish failed.' }
