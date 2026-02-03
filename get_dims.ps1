$files = Get-ChildItem Content\Weapons\*.png
foreach ($file in $files) {
    try {
        $fs = [System.IO.File]::OpenRead($file.FullName)
        $b = New-Object byte[] 32
        $null = $fs.Read($b, 0, 32)
        $fs.Close()
        
        # PNG dimensions are at offset 16 (width) and 20 (height), Big Endian
        $w = [System.BitConverter]::ToUInt32($b[19..16], 0)
        $h = [System.BitConverter]::ToUInt32($b[23..20], 0)
        
        Write-Output "$($file.Name): $($w)x$($h)"
    } catch {
        Write-Error "Failed to process $($file.Name): $($_.Exception.Message)"
    }
}
