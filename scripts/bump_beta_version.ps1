param(
    [string]$VersionFile = "VERSION"
)

# Get existing tags matching beta pattern
$tags = git tag -l "v*beta*" | Sort-Object -Descending
if ($tags.Count -eq 0) {
    $next = "v0.1.0-beta.1"
} else {
    $last = $tags[0]
    if ($last -match "v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)-beta\.(?<num>\d+)") {
        $next = "v$($Matches['major']).$($Matches['minor']).$($Matches['patch'])-beta.$([int]$Matches['num'] + 1)"
    } else {
        $next = "v0.1.0-beta.1"
    }
}

Set-Content -Path $VersionFile -Value $next
Write-Host "Updated $VersionFile to $next"
