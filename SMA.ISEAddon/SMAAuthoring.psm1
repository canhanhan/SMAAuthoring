function Register-SMAAuthoringAddon() {
	Add-Type -Path (Join-Path $PSScriptRoot 'SMA.ISEAddon.dll')

    $addOnName = 'SMA Authoring'
    $exists = $psISE.CurrentPowerShellTab.VerticalAddOnTools | where { $_.Name -eq $addOnName }
    if ($exists) {
        $psISE.CurrentPowerShellTab.VerticalAddOnTools.Remove($exists)
    }
	$psISE.CurrentPowerShellTab.VerticalAddOnTools.Add($addOnName, [SMA.ISEAddon.Views.SMAAuthoringUserControl], $true) | Out-Null
}

if ($host.Name -ne 'Windows PowerShell ISE Host') {
	throw "SMAAuthoring module only runs inside PowerShell ISE"
}

if ($PSVersionTable.PSVersion.Major -lt 4) {
	throw "SMAAuthoring module requires Powershell 4.0 or above"
}

Register-SMAAuthoringAddon