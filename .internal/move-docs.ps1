# Move Internal Docs to .internal/ folder
# Run from repository root

Write-Host "📂 Moving internal documentation to .internal/ folder..." -ForegroundColor Cyan

# Create directory structure
$internalDir = ".internal"
$guidesDir = "$internalDir/guides"
$releasesDir = "$internalDir/releases/v1.0.1"

New-Item -ItemType Directory -Force -Path $guidesDir | Out-Null
New-Item -ItemType Directory -Force -Path $releasesDir | Out-Null

# Files to move
$filesToMove = @(
    @{ Source = "RELEASE_CHECKLIST.md"; Dest = "$releasesDir/RELEASE_CHECKLIST.md" },
    @{ Source = "docs/NuGet-Publishing-Guide.md"; Dest = "$guidesDir/NuGet-Publishing-Guide.md" },
    @{ Source = "docs/GitHub-Actions-Trusted-Publishing.md"; Dest = "$guidesDir/GitHub-Actions-Trusted-Publishing.md" }
)

foreach ($file in $filesToMove) {
    if (Test-Path $file.Source) {
        Write-Host "  Moving: $($file.Source) → $($file.Dest)" -ForegroundColor Yellow
        Move-Item -Path $file.Source -Destination $file.Dest -Force
    } else {
        Write-Host "  Skip (not found): $($file.Source)" -ForegroundColor Gray
    }
}

# Optional: Remove from Git if they were tracked
Write-Host ""
Write-Host "✅ Files moved to .internal/ folder" -ForegroundColor Green
Write-Host ""
Write-Host "📌 Next steps:" -ForegroundColor Cyan
Write-Host "  1. Review moved files in .internal/"
Write-Host "  2. Run: git rm --cached <file> (if files were tracked)"
Write-Host "  3. Commit changes"
Write-Host ""
Write-Host "💡 Tip: .internal/ is gitignored, so files stay local!" -ForegroundColor Magenta
