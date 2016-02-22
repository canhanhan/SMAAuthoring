# SMAAuthoring
The Powershell ISE add-on provides a vertical menu to testing and publish SMA runbooks easily.
![addon](https://cloud.githubusercontent.com/assets/46500/13207868/29339696-d8d9-11e5-8959-9a391f7666bb.png)

## Installation
### Automatical
  Install.ps1 will automatically create the folder structure, copy the files and add commands for importing the module to the profile file.
  
### Manual
  1. Copy SMAAuthoring folder to Powershell Modules folder (either of following): 
    * C:\Windows\System32\WindowsPowerShell\v1.0\Modules
    * C:\Users\<username>\Documents\WindowsPowershell\Modules
  
  2. Edit the ISE profile (Microsoft.PowerShellISE_profile.ps1) to import the module during each launch:
    - Import-Module SMAAuthoring
  
## First Run
 * Fill-in your SMA web service URL to the textbox (i.e.: https://<hostname>:9090/00000000-0000-0000-0000-000000000000) ![webserviceuri](https://cloud.githubusercontent.com/assets/46500/13207974/0540ffb6-d8da-11e5-902a-643ba95e06b2.png)

## Test
 * Clicking to Test button will update draft of existing runbook and trigger a Test run.
 * If no runbooks were selected, it will create a new one.
 * Results of test will be shown in the output box.    ![test](https://cloud.githubusercontent.com/assets/46500/13207973/053a333e-d8da-11e5-8bd9-744bfb7cce69.png)
 
## Publish
 * Clicking to Publish button will publish the last draft of the runbook.
