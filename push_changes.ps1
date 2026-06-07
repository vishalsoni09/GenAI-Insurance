# Push workspace changes helper script
# Usage: .\push_changes.ps1 -Branch "feature/save-work" -CommitMessage "Save workspace changes"
param(
  [string]$Branch = "feature/save-work",
  [string]$CommitMessage = "Save workspace changes"
)

Write-Host "Running git operations in: $(Get-Location)"

# Check for git
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
  Write-Error "git CLI not found. Install Git for Windows: https://git-scm.com/download/win"
  exit 1
}

# Show status
git status --porcelain

# Create branch if it doesn't exist locally
$exists = git rev-parse --verify $Branch 2>$null
if ($LASTEXITCODE -ne 0) {
  Write-Host "Creating and switching to branch '$Branch'..."
  git checkout -b $Branch
} else {
  Write-Host "Switching to branch '$Branch'..."
  git checkout $Branch
}

# Stage all changes
Write-Host "Staging changes..."
git add -A

# Commit
if (-not $CommitMessage) { $CommitMessage = "Save workspace changes" }
Write-Host "Committing: $CommitMessage"
git commit -m "$CommitMessage" || Write-Host "No changes to commit or commit failed."

# Push
Write-Host "Pushing to origin/$Branch..."
$push = git push -u origin $Branch
if ($LASTEXITCODE -ne 0) {
  Write-Host "Push failed. Common fixes: authenticate with Git credential manager or use a PAT."
  Write-Host "Try: git config --global credential.helper manager-core" 
  Write-Host "Or login via GitHub CLI: gh auth login" 
  Write-Host "After authenticating, re-run this script."
  exit 1
}

Write-Host "Push completed."
