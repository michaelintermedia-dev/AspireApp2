# React Native Build Fix Script
# This script cleans and rebuilds the React Native project

Write-Host "Cleaning React Native build..." -ForegroundColor Cyan

# Navigate to the mobile app directory
Set-Location -Path $PSScriptRoot

# Step 1: Clean node_modules and caches
Write-Host ""
Write-Host "Step 1: Cleaning node_modules and caches..." -ForegroundColor Yellow
if (Test-Path "node_modules") {
    Remove-Item -Recurse -Force "node_modules"
}
if (Test-Path ".expo") {
    Remove-Item -Recurse -Force ".expo"
}
if (Test-Path "android/build") {
    Remove-Item -Recurse -Force "android/build"
}
if (Test-Path "android/app/build") {
    Remove-Item -Recurse -Force "android/app/build"
}
if (Test-Path "android/.gradle") {
    Remove-Item -Recurse -Force "android/.gradle"
}
if (Test-Path "android/.cxx") {
    Remove-Item -Recurse -Force "android/.cxx"
}
if (Test-Path "package-lock.json") {
    Remove-Item -Force "package-lock.json"
}
if (Test-Path "yarn.lock") {
    Remove-Item -Force "yarn.lock"
}

Write-Host "Cleanup complete!" -ForegroundColor Green

# Step 2: Reinstall dependencies
Write-Host ""
Write-Host "Step 2: Installing dependencies..." -ForegroundColor Yellow
npm install
if ($LASTEXITCODE -ne 0) {
    Write-Host "npm install failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Dependencies installed!" -ForegroundColor Green

# Step 3: Clean Gradle
Write-Host ""
Write-Host "Step 3: Cleaning Gradle..." -ForegroundColor Yellow
Set-Location "android"
.\gradlew.bat clean
if ($LASTEXITCODE -ne 0) {
    Write-Host "Gradle clean failed!" -ForegroundColor Red
    Set-Location ..
    exit 1
}
Set-Location ..
Write-Host "Gradle cleaned!" -ForegroundColor Green

# Step 4: Prebuild
Write-Host ""
Write-Host "Step 4: Running expo prebuild..." -ForegroundColor Yellow
npx expo prebuild --clean
if ($LASTEXITCODE -ne 0) {
    Write-Host "Prebuild failed!" -ForegroundColor Red
    exit 1
}
Write-Host "Prebuild complete!" -ForegroundColor Green

Write-Host ""
Write-Host "Build fix complete! You can now run:" -ForegroundColor Green
Write-Host "   npx expo run:android" -ForegroundColor Cyan
