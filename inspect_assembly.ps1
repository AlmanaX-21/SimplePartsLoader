$managedPath = Convert-Path ".\Managed"
$targetDll = Join-Path $managedPath "Assembly-CSharp.dll"

Write-Output "Target DLL: $targetDll"
Write-Output "Dependency Path: $managedPath"

# Hook AssemblyResolve to find dependencies in the Managed folder
$onAssemblyResolve = {
    param($sender, $args)
    $name = new-object System.Reflection.AssemblyName($args.Name)
    $path = Join-Path $managedPath "$($name.Name).dll"
    if (Test-Path $path) {
        return [System.Reflection.Assembly]::LoadFrom($path)
    }
    return $null
}

# Add the event handler
$curDomain = [System.AppDomain]::CurrentDomain
Register-ObjectEvent -InputObject $curDomain -EventName AssemblyResolve -Action $onAssemblyResolve | Out-Null

try {
    $asm = [System.Reflection.Assembly]::LoadFrom($targetDll)
    Write-Output "Assembly loaded: $($asm.FullName)"

    # Get types safely
    try {
        $types = $asm.GetTypes()
    }
    catch [System.Reflection.ReflectionTypeLoadException] {
        Write-Warning "Some types failed to load."
        $types = $_.Exception.Types | Where-Object { $_ -ne $null }
    }

    Write-Output "Total Types Found: $($types.Count)"
    Write-Output "Scanning for Plane Parts, Managers, and UI..."
    Write-Output "=================================================="

    foreach ($t in $types) {
        # broader search terms
        if ($t.Name -match "Part|Block|Module|Wing|Fuselage|Cockpit|Engine|Aero|Lift|Manager|Database|Registry|Build|UI") {
            
            # Skip some common noise if needed, but keeping it open for now
            if ($t.Name -match "^TMP_|DisplayClass|Compiler") { continue }

            Write-Output "Type: $($t.FullName)"
            
            # Is abstract? Is Interface?
            $status = @()
            if ($t.IsAbstract) { $status += "Abstract" }
            if ($t.IsInterface) { $status += "Interface" }
            if ($t.IsEnum) { $status += "Enum" }
            if ($t.IsValueType -and -not $t.IsEnum) { $status += "Struct" }
            if ($status.Count -eq 0) { $status += "Class" }
            Write-Output "  Kind: $($status -join ', ')"

            # Inheritance
            if ($t.BaseType) {
                Write-Output "  Base: $($t.BaseType.FullName)"
            }

            # Interfaces
            $interfaces = $t.GetInterfaces()
            if ($interfaces) {
                Write-Output "  Implements: $($interfaces.Name -join ', ')"
            }

            # Interesting Fields (Public or SerializeField-ish)
            # We want to see Unity fields -> Public, Instance
            try {
                $fields = $t.GetFields([System.Reflection.BindingFlags]"Public,Instance,DeclaredOnly")
                foreach ($f in $fields) {
                    Write-Output "    Field: $($f.Name) : $($f.FieldType.Name)"
                }
            }
            catch {}

            # Static Fields often hold Singleton instances or Registries
            try {
                $staticFields = $t.GetFields([System.Reflection.BindingFlags]"Public,Static,DeclaredOnly")
                foreach ($f in $staticFields) {
                    Write-Output "    Static: $($f.Name) : $($f.FieldType.Name)"
                }
            }
            catch {}

            Write-Output "--------------------------------"
        }
    }

}
catch {
    Write-Error "Script Failed: $_"
}
