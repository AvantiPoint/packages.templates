param (
    [string]$TemplatePackRoot = '.',
    [string]$Version = '1.0.0'
    )

try
{
    $currentScriptPath = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

    function Get-ScriptDirectory {
        Split-Path -parent $PSCommandPath
    }

    Write-Host "Execution Path"
    $ScriptRoot = Get-ScriptDirectory
    Write-Host $ScriptRoot

    Write-Host "Cleaning Build Resources"
    Get-ChildItem $ScriptRoot -Include bin,obj -Recurse | ForEach-Object ($_) { Remove-Item $_.Fullname -Force -Recurse }
    Get-ChildItem $ScriptRoot -Include .mfractor -Attributes Hidden -Recurse | ForEach-Object ($_) { Remove-Item $_.Fullname -Force -Recurse }
    Get-ChildItem $ScriptRoot -Include .vs -Attributes Hidden -Recurse | ForEach-Object ($_) { Remove-Item $_.Fullname -Force -Recurse }
    Get-ChildItem $ScriptRoot -Include *.csproj.user -Recurse | ForEach-Object ($_) { Remove-Item $_.Fullname -Force -Recurse }
    Write-Host "Finished Cleaning Build Resources"

    Write-Host "Updating Ignorable Files"
    Get-ChildItem $ScriptRoot -Include .editorconfig -Recurse -File -Force | ForEach-Object ($_) { Rename-Item -Path $_.Fullname -NewName "editor.config" }
    Get-ChildItem $ScriptRoot -Include .gitignore -Recurse -File -Force | ForEach-Object ($_) { Rename-Item -Path $_.Fullname -NewName "git.ignore" }
    Rename-Item -Path .\LICENSE -NewName "LICENSE.txt"

    Write-Host "Begin Project Templates Nuget pack ..."

    $tempPath = Join-Path $TemplatePackRoot -ChildPath "Artifacts"

    if(!(Test-Path -Path $tempPath)) {
        New-Item -ItemType Directory -Force -Path $tempPath
    }

    $nugetOutputDirectory = Resolve-Path -Path $tempPath
    $nugetFileName = Join-Path $ScriptRoot -ChildPath "nuget.exe"

    Write-Host "NuGet Output Path set to: $nugetOutputDirectory"

    if (!(Test-Path $nugetFileName)) {
        Write-Host "Downloading Nuget.exe ..."
        Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile $nugetFileName
        Write-Host "Downloading Nuget.exe complete"
    }

    $nuspecPath = Join-Path $currentScriptPath -ChildPath "template.nuspec"

    $nugetHash = git rev-parse HEAD

    Invoke-Expression "$nugetFileName pack $nuspecPath -OutputDirectory $nugetOutputDirectory -Version $Version -properties commitId=$nugetHash"

    Write-Host "Completed Project Templates Nuget pack ..."
}
catch
{
    Write-Host $ErrorMessage = $_.Exception.Message
    exit 1
}
finally
{
    Write-Host "Resetting Ignorable Files"
    Get-ChildItem $ScriptRoot -Include editor.config -Recurse -File -Force | ForEach-Object ($_) { Rename-Item -Path $_.Fullname -NewName ".editorconfig" }
    Get-ChildItem $ScriptRoot -Include git.ignore -Recurse -File -Force | ForEach-Object ($_) { Rename-Item -Path $_.Fullname -NewName ".gitignore" }

    if((Test-Path -Path .\LICENSE.txt)) {
        Rename-Item -Path .\LICENSE.txt -NewName LICENSE
    }
}