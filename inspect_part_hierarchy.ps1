$managedPath = Convert-Path ".\Managed"
$targetDll = Join-Path $managedPath "Assembly-CSharp.dll"

$onAssemblyResolve = {
    param($sender, $args)
    $name = new-object System.Reflection.AssemblyName($args.Name)
    $path = Join-Path $managedPath "$($name.Name).dll"
    if (Test-Path $path) { return [System.Reflection.Assembly]::LoadFrom($path) }
    return $null
}
Register-ObjectEvent -InputObject [System.AppDomain]::CurrentDomain -EventName AssemblyResolve -Action $onAssemblyResolve | Out-Null

try {
    $asm = [System.Reflection.Assembly]::LoadFrom($targetDll)
    $types = $asm.GetTypes()

    # 1. Find the base "Part" class
    $partClass = $types | Where-Object { $_.Name -eq "Part" }
    
    if ($partClass) {
        Write-Output "FOUND ROOT CLASS: Part"
        Write-Output "Base: $($partClass.BaseType.Name)"
        $partClass.GetFields([System.Reflection.BindingFlags]"Public,Instance,DeclaredOnly") | ForEach-Object { Write-Output "  Field: $($_.Name) ($($_.FieldType.Name))" }
        Write-Output "====================================="
        
        # 2. Find all inheritors
        $inheritors = $types | Where-Object { $_.IsSubclassOf($partClass) }
        Write-Output "INHERITORS of Part:"
        foreach ($t in $inheritors) {
            Write-Output "Class: $($t.Name)"
            $t.GetFields([System.Reflection.BindingFlags]"Public,Instance,DeclaredOnly") | ForEach-Object { Write-Output "  + Field: $($_.Name) ($($_.FieldType.Name))" }
        }
    }
    else {
        Write-Warning "Could not find exact class 'Part'. Searching for candidates..."
        $candidates = $types | Where-Object { $_.Name -match "Part" -and $_.IsSubclassOf([UnityEngine.MonoBehaviour]) }
        foreach ($c in $candidates) {
            Write-Output "Candidate: $($c.Name)"
        }
    }

    Write-Output "====================================="
    # 3. Inspect PartUIData
    $uiData = $types | Where-Object { $_.Name -eq "PartUIData" }
    if ($uiData) {
        Write-Output "CLASS: PartUIData"
        $uiData.GetFields([System.Reflection.BindingFlags]"Public,Instance,DeclaredOnly") | ForEach-Object { Write-Output "  Field: $($_.Name) ($($_.FieldType.Name))" }
    }

}
catch { Write-Error $_ }
