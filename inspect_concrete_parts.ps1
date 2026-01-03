$managedPath = Convert-Path ".\Managed"
$targetDll = Join-Path $managedPath "Assembly-CSharp.dll"
try { [System.Reflection.Assembly]::LoadFrom((Join-Path $managedPath "UnityEngine.CoreModule.dll")) | Out-Null } catch {}
try { [System.Reflection.Assembly]::LoadFrom((Join-Path $managedPath "UnityEngine.dll")) | Out-Null } catch {}

try {
    $asm = [System.Reflection.Assembly]::LoadFrom($targetDll)
    $types = $asm.GetTypes()

    # 1. Check PartUIData inheritance specifically
    $uiData = $types | Where-Object { $_.Name -eq "PartUIData" }
    Write-Output "CLASS: PartUIData"
    Write-Output "Base: $($uiData.BaseType.FullName)"
    
    # 2. Inspect concrete PlaneParts for physics fields
    $partsToInspect = @("Engine", "Wing", "Fuselage", "Wheel", "FuelTank", "ControlSurface", "Breaks")
    
    foreach ($pName in $partsToInspect) {
        $t = $types | Where-Object { $_.Name -eq $pName }
        if ($t) {
            Write-Output "CLASS: $($t.Name)"
            Write-Output "  Base: $($t.BaseType.Name)"
            $t.GetFields([System.Reflection.BindingFlags]"Public,Instance,DeclaredOnly") | ForEach-Object { 
                Write-Output "    $($_.Name) : $($_.FieldType.Name)" 
            }
            Write-Output "--------------------------------"
        }
    }

    # 3. Check PartPrefabs static fields or methods that might populate the list
    $prefabs = $types | Where-Object { $_.Name -eq "PartPrefabs" }
    if ($prefabs) {
        Write-Output "CLASS: PartPrefabs"
        $prefabs.GetFields([System.Reflection.BindingFlags]"Public,Instance,DeclaredOnly") | ForEach-Object { 
            Write-Output "    Field: $($_.Name) : $($_.FieldType.Name)" 
        }
        $prefabs.GetFields([System.Reflection.BindingFlags]"Public,Static,DeclaredOnly") | ForEach-Object { 
            Write-Output "    Static: $($_.Name) : $($_.FieldType.Name)" 
        }
    }

}
catch { Write-Error $_ }
