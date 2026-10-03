# Script de développement - Lance le backend et le frontend
# Usage: .\start-dev.ps1

$rootPath = $PSScriptRoot

Write-Host "Demarrage de l'environnement de developpement..." -ForegroundColor Cyan

# Lancer le backend (.NET) dans une nouvelle fenêtre
Write-Host "Lancement du backend (https://localhost:7228)..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$rootPath\backend'; dotnet run --project DartsTournament.Api --launch-profile https"

# Lancer le frontend (Angular) dans une nouvelle fenêtre
Write-Host "Lancement du frontend (http://localhost:4200)..." -ForegroundColor Green
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$rootPath\frontend\darts-tournament'; npm start"

Write-Host ""
Write-Host "Les deux services demarrent dans des fenetres separees." -ForegroundColor Yellow
Write-Host "  - Backend:  https://localhost:7228" -ForegroundColor White
Write-Host "  - Frontend: http://localhost:4200" -ForegroundColor White
Write-Host ""
Write-Host "Fermez les fenetres PowerShell pour arreter les services." -ForegroundColor Yellow
