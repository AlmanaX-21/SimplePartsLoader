$managedPath = Convert-Path ".\Managed"
$targetDll = Join-Path $managedPath "Assembly-CSharp.dll"
try { [System.Reflection.Assembly]::LoadFrom((Join-Path $managedPath "UnityEngine.CoreModule.dll")) | Out-Null } catch {}
try { [System.Reflection.Assembly]::LoadFrom((Join-Path $managedPath "UnityEngine.dll")) | Out-Null } catch {}
try { [System.Reflection.Assembly]::LoadFrom((Join-Path $managedPath "UnityEngine.UI.dll")) | Out-Null } catch {}

try {
    $asm = [System.Reflection.Assembly]::LoadFrom($targetDll)
    $types = $asm.GetTypes()
    
    $pb = $types | Where-Object { $_.Name -eq "PartButton" }
    if ($pb) {
        Write-Output "CLASS: PartButton"
        Write-Output "Base: $($pb.BaseType.Name)"
        $pb.GetFields([System.Reflection.BindingFlags]"Public,Instance,DeclaredOnly") | ForEach-Object { 
            Write-Output "    Field: $($_.Name) : $($_.FieldType.Name)" 
        }
    }
}
catch { Write-Error $_ }
