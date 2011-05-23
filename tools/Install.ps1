param($installPath, $toolsPath, $package, $project)

Write-Host "Replacing project version in MakeCoffee.t4 to" $package.Version
$coffee_template = ($project.ProjectItems.Item("Scripts").ProjectItems | Where-Object { $_.Name -eq "MakeCoffee.t4" }).FileNames(1)

((Get-Content $coffee_template) | ForEach-Object { $_ -replace "[$]packageversion[$]", $package.Version }) | Set-Content $coffee_template
