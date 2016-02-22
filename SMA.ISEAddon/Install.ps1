$target = "$([Environment]::getfolderpath("mydocuments"))\WindowsPowershell\Modules"
if (!(Test-Path -Path "$target" -PathType Container))
{
	New-Item -Path "$target" -ItemType Container
}	

if (Test-Path -Path "$target\SMAAuthoring" -PathType Container)
{
	Remove-Item -Path "$target\SMAAuthoring"  -Recurse -Force
}

New-Item -Path "$target\SMAAuthoring" -ItemType Container

$currentDir = Split-Path -parent $MyInvocation.MyCommand.Path
Get-ChildItem "$currentDir\*.dll" -File | ForEach-Object {
	Copy-Item -Path $_.FullName -Destination "$target\SMAAuthoring"
}

Copy-Item -Path "SMAAuthoring.psm1" -Destination "$target\SMAAuthoring"
Copy-Item -Path "SMAAuthoring.psd1" -Destination "$target\SMAAuthoring"
Copy-Item -Path "SMA.ISEAddon.dll.config" -Destination "$target\SMAAuthoring"

$profileFile = "$target\..\Microsoft.PowerShellISE_profile.ps1"
if (!(Test-Path $profileFile)) {
	New-Item -Path $profileFile -ItemType file | Out-Null
	$contents = ""
} else {
	$contents = Get-Content -Path $profileFile | Out-String
}

$importModule = "Import-Module SMAAuthoring"

if ($contents -inotmatch $importModule) {
	Add-Content -Path $profileFile -Value $importModule | Out-Null
}
