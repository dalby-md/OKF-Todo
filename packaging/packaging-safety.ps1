function Test-SqliteFileSignature {
    param([Parameter(Mandatory)][string]$Path)

    $stream = [System.IO.File]::OpenRead($Path)
    try {
        if ($stream.Length -lt 4) {
            return $false
        }

        $header = New-Object byte[] 16
        $bytesRead = $stream.Read($header, 0, $header.Length)
        if ($bytesRead -ge 16 -and
            [Text.Encoding]::ASCII.GetString($header, 0, 16) -eq "SQLite format 3`0") {
            return $true
        }

        $isWriteAheadLog =
            $header[0] -eq 0x37 -and
            $header[1] -eq 0x7f -and
            $header[2] -eq 0x06 -and
            ($header[3] -eq 0x82 -or $header[3] -eq 0x83)
        if ($isWriteAheadLog) {
            return $true
        }

        return $bytesRead -ge 8 -and
            $header[0] -eq 0xd9 -and
            $header[1] -eq 0xd5 -and
            $header[2] -eq 0x05 -and
            $header[3] -eq 0xf9 -and
            $header[4] -eq 0x20 -and
            $header[5] -eq 0xa1 -and
            $header[6] -eq 0x63 -and
            $header[7] -eq 0xd7
    }
    finally {
        $stream.Dispose()
    }
}

function Assert-NoPackagedDatabaseFiles {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "Packaging safety path does not exist: $Path"
    }

    $databaseFiles = @(Get-ChildItem -LiteralPath $Path -Recurse -File |
        Where-Object {
            $_.Name -match '(?i)\.(db|sqlite|sqlite3)($|-(wal|shm|journal)$|\.(bak|backup|copy|old)$)' -or
            (Test-SqliteFileSignature -Path $_.FullName)
        })
    if ($databaseFiles.Count -eq 0) {
        return
    }

    $relativePaths = @($databaseFiles | ForEach-Object {
        $_.FullName.Substring([System.IO.Path]::GetFullPath($Path).TrimEnd('\').Length + 1)
    })
    throw "Packaging safety violation: database files must never be included in application packages: $($relativePaths -join ', ')"
}

function Assert-InstallerDoesNotManageUserDatabase {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    $installerSource = Get-Content -LiteralPath $Path -Raw
    $forbiddenReferences = @(
        'okf-todo.db',
        '{localappdata}\Okf-Todo\',
        '{userappdata}\Okf-Todo\'
    )
    foreach ($forbiddenReference in $forbiddenReferences) {
        if ($installerSource.IndexOf(
            $forbiddenReference,
            [StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "Packaging safety violation: installer source must not manage the user database: $forbiddenReference"
        }
    }
}
