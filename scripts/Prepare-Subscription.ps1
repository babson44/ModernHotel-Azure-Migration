<#
.SYNOPSIS
    Pre-flight check and fix for deploying ModernHotel to Azure.
    Run this ONCE per subscription before clicking Deploy to Azure.

.DESCRIPTION
    Some Azure subscription types (Microsoft Employee managed environments,
    certain CSP subscriptions) require a feature flag to be registered before
    Standard SKU Public IP addresses can be created. This script checks for
    that requirement and registers it automatically if needed.

    Safe to run on any subscription — it is idempotent (running it twice does nothing).

.EXAMPLE
    # Fix current subscription only
    .\Prepare-Subscription.ps1

    # Fix a specific subscription
    .\Prepare-Subscription.ps1 -SubscriptionId "b4a7420a-8802-47ce-8a8b-20f127b44b1b"

    # Fix ALL your subscriptions at once
    .\Prepare-Subscription.ps1 -AllSubscriptions
#>

param(
    [string]$SubscriptionId,
    [switch]$AllSubscriptions
)

$featureName    = 'AllowBringYourOwnPublicIpAddress'
$featureNS      = 'Microsoft.Network'

function Register-FeatureOnSubscription {
    param([string]$SubId, [string]$SubName)

    Write-Host "`n[$SubName]" -ForegroundColor Cyan

    $current = az feature show `
        --name $featureName `
        --namespace $featureNS `
        --subscription $SubId `
        --query "properties.state" -o tsv 2>$null

    if ($current -eq 'Registered') {
        Write-Host "  ✅ Feature already registered — no action needed." -ForegroundColor Green
        return
    }

    Write-Host "  ⚠️  Feature not registered (state: $current). Registering now..." -ForegroundColor Yellow

    az feature register `
        --name $featureName `
        --namespace $featureNS `
        --subscription $SubId | Out-Null

    az provider register -n $featureNS --subscription $SubId | Out-Null

    # Wait for registration (up to 3 minutes)
    $tries = 0
    do {
        Start-Sleep -Seconds 10
        $tries++
        $state = az feature show `
            --name $featureName `
            --namespace $featureNS `
            --subscription $SubId `
            --query "properties.state" -o tsv 2>$null
        Write-Host "  ... state: $state ($($tries * 10)s)" -ForegroundColor Gray
    } while ($state -ne 'Registered' -and $tries -lt 18)

    if ($state -eq 'Registered') {
        Write-Host "  ✅ Registered successfully." -ForegroundColor Green
    } else {
        Write-Host "  ❌ Timed out — check manually: az feature show --name $featureName --namespace $featureNS --subscription $SubId" -ForegroundColor Red
    }
}

# ── Main ──────────────────────────────────────────────────────────────────────

Write-Host "ModernHotel — Subscription Pre-flight Check" -ForegroundColor White
Write-Host "============================================" -ForegroundColor White

if ($AllSubscriptions) {
    Write-Host "Mode: ALL subscriptions`n"
    $subs = az account list --query "[].{id:id, name:name}" -o json | ConvertFrom-Json
    foreach ($sub in $subs) {
        Register-FeatureOnSubscription -SubId $sub.id -SubName $sub.name
    }
} elseif ($SubscriptionId) {
    $name = az account show --subscription $SubscriptionId --query "name" -o tsv
    Register-FeatureOnSubscription -SubId $SubscriptionId -SubName $name
} else {
    $sub = az account show --query "{id:id, name:name}" -o json | ConvertFrom-Json
    Register-FeatureOnSubscription -SubId $sub.id -SubName $sub.name
}

Write-Host "`nDone. You can now click the Deploy to Azure button." -ForegroundColor Green
