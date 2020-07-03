cd $PSScriptRoot

dotnet pack -o ./output

if ((Get-Command dotnet-package-helper).Length -gt 0) {
  dotnet tool uninstall -g dotnet-package-helper
}

dotnet tool install -g --add-source ./output dotnet-package-helper
