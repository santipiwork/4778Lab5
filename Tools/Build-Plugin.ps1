param([string]$UnityEditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.2.8f1\Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$unityData = Join-Path $UnityEditorPath 'Data'
$output = Join-Path $projectRoot 'Assets\Plugins\Avoider'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$compiler = Join-Path $unityData 'DotNetSdkRoslyn\csc.dll'
$runtime = Join-Path $unityData 'NetCoreRuntime\dotnet.exe'
$arguments = @($compiler, '-nologo', '-target:library', '-langversion:9.0', '-optimize+', '-deterministic+', '-nostdlib+', "-out:$output\Avoider.dll")
$arguments += "-reference:$unityData\NetStandard\ref\2.1.0\netstandard.dll"
foreach ($module in @('CoreModule', 'AIModule', 'PhysicsModule')) {
    $arguments += "-reference:$unityData\Managed\UnityEngine\UnityEngine.$module.dll"
}
$arguments += Get-ChildItem (Join-Path $projectRoot 'PluginSource\Avoider\*.cs') | ForEach-Object FullName
& $runtime $arguments
if ($LASTEXITCODE -ne 0) { throw 'Avoider DLL compilation failed.' }
Write-Host "Built $output\Avoider.dll"
