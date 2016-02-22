$curDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Import-Module "$curDir\SMAAuthoring.psd1" -Force