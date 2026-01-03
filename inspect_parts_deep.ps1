$managedPath = Convert-Path ".\Managed"
$targetDll = Join-Path $managedPath "Assembly-CSharp.dll"

# Pre-load Unity Core if possible to help resolution
try { [System.Reflection.Assembly]::LoadFrom((Join-Path $managedPath "UnityEngine.CoreModule.dll")) | Out-Null } catch {}
try { [System.Reflection.Assembly]::LoadFrom((Join-Path $managedPath "UnityEngine.dll")) | Out-Null } catch {}

try {
    $asm = [System.Reflection.Assembly]::LoadFrom($targetDll)
    $types = $asm.GetTypes()

    function Inspect-Type ($typeName) {
        $t = $types | Where-Object { $_.Name -eq $typeName }
        if ($t) {
            Write-Output "CLASS: $($t.Name)"
            Write-Output "Base: $($t.BaseType.Name)"
            
            Write-Output "  [Fields]"
            $t.GetFields([System.Reflection.BindingFlags]"Public,Instance,DeclaredOnly") | ForEach-Object { 
                Write-Output "    $($_.Name) : $($_.FieldType.Name)" 
            }
            
            Write-Output "  [INHERITORS]"
            $inheritors = $types | Where-Object { $_.IsSubclassOf($t) }
            foreach ($sub in $inheritors) {
                Write-Output "    -> $($sub.Name)"
            }
            Write-Output "--------------------------------------------------"
        }
    }

    Inspect-Type "BuildingPart"
    Inspect-Type "PlanePart"
    Inspect-Type "PartUIData"
    
    # Also check for specific categories if they don't inherit directly
    Write-Output "Searching for specific part types (Wing, Engine, etc)..."
    $types | Where-Object { $_.Name -match "Wing|Fuselage|Cockpit|Engine|Wheel" -and $_.IsSubclassOf([UnityEngine.MonoBehaviour]) } | ForEach-Object {
        Write-Output "  Found: $($_.Name) (Base: $($_.BaseType.Name))"
    }

}
catch { Write-Error $_ }
