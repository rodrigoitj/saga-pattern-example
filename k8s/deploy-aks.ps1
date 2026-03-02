#!/usr/bin/env pwsh

<#
.SYNOPSIS
Complete AKS Deployment with Workload Identity and Image Building

.DESCRIPTION
This script provides comprehensive deployment capabilities for the Saga Pattern 
microservices to Azure Kubernetes Service. It combines production-grade security
(Workload Identity + CSI SecretProvider) with developer-friendly features like
image building, upgrades, and rollbacks.

ACTIONS:
  setup-identity    - Setup Workload Identity, managed identity, and OIDC federation
  deploy            - Deploy application to AKS
  upgrade           - Build, push images, and upgrade deployment
  rollback          - Rollback deployments to previous version
  verify            - Verify deployment health and configuration
  cleanup           - Cleanup managed identity and Kubernetes resources

EXAMPLES:
  # Initial setup with Workload Identity
  .\deploy-aks.ps1 -Action setup-identity `
    -ResourceGroup saga-pattern-rg `
    -ClusterName saga-pattern-aks `
    -TenantId <YOUR_TENANT_ID>

  # Deploy application
  .\deploy-aks.ps1 -Action deploy `
    -ResourceGroup saga-pattern-rg `
    -ClusterName saga-pattern-aks `
    -Registry sagapatternacr

  # Upgrade with new images
  .\deploy-aks.ps1 -Action upgrade `
    -Registry sagapatternacr `
    -Build

  # Verify deployment
  .\deploy-aks.ps1 -Action verify

  # Cleanup
  .\deploy-aks.ps1 -Action cleanup -ResourceGroup saga-pattern-rg
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("setup-identity", "deploy", "upgrade", "rollback", "verify", "cleanup")]
    [string]$Action,

    [Parameter()]
    [string]$KubernetesContext = "saga-pattern-aks",

    [Parameter()]
    [string]$ResourceGroup = "saga-pattern-rg",

    [Parameter()]
    [string]$ClusterName = "saga-pattern-aks",

    [Parameter()]
    [string]$Registry = "sagapatternacr",

    [Parameter()]
    [string]$KeyVaultName = "saga-pattern-kv",

    [Parameter()]
    [string]$TenantId,

    [Parameter()]
    [string]$Location = "brazilsouth",

    [Parameter()]
    [string]$ServiceAccount = "saga-pattern-sa",

    [Parameter()]
    [string]$IdentityName = "saga-workload-identity",

    [ValidateSet("dev", "prod")]
    [string]$Environment = "dev",

    [switch]$Build,
    [switch]$SetupWorkloadIdentity,
    [switch]$EnableCSISecretProvider,
    [switch]$UseKeyVault
)

$ErrorActionPreference = "Stop"
$WarningPreference = "Continue"

# ============================================================================
# Output Formatting Functions
# ============================================================================

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Warning-Msg {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error-Msg {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-Section {
    param([string]$Message)
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════╗" -ForegroundColor Yellow
    Write-Host "║ $($Message.PadRight(44)) ║" -ForegroundColor Yellow
    Write-Host "╚════════════════════════════════════════════════╝" -ForegroundColor Yellow
    Write-Host ""
}

function Write-Header {
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
    Write-Host "║ Saga Pattern - Production AKS Deployment Script               ║" -ForegroundColor Magenta
    Write-Host "║ Combined: Workload Identity + Image Building + Full Lifecycle ║" -ForegroundColor Magenta
    Write-Host "╚════════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
    Write-Host ""
}

# ============================================================================
# Kubernetes Context Management
# ============================================================================

function Set-KubernetesContext {
    param([string]$ContextName)
    
    $currentContext = kubectl config current-context 2>$null
    
    if ($currentContext -eq $ContextName) {
        Write-Success "Already using context: $ContextName"
        return
    }
    
    # Check if context exists
    $contexts = kubectl config get-contexts -o name 2>$null
    if ($contexts -notcontains $ContextName) {
        Write-Error-Msg "Context '$ContextName' not found. Available contexts:"
        kubectl config get-contexts
        exit 1
    }
    
    Write-Info "Switching to context: $ContextName"
    kubectl config use-context $ContextName 2>$null
    Write-Success "Now using context: $ContextName"
}

# ============================================================================
# Prerequisites & Validation
# ============================================================================

function Test-Prerequisites {
    Write-Info "Testing prerequisites..."
    
    $tools = @('az', 'kubectl', 'docker')
    $missing = @()
    
    foreach ($tool in $tools) {
        $exists = $null -ne (Get-Command $tool -ErrorAction SilentlyContinue)
        if ($exists) {
            Write-Success "  ✓ $tool found"
        }
        else {
            Write-Error-Msg "  ✗ $tool not found"
            $missing += $tool
        }
    }
    
    if ($missing.Count -gt 0) {
        Write-Error-Msg "Missing tools: $($missing -join ', ')"
        exit 1
    }
    
    Write-Success "All prerequisites met"
}

# ============================================================================
# Azure Resource Management
# ============================================================================

function Ensure-ResourceGroup {
    Write-Info "Checking resource group: $ResourceGroup"
    
    $rg = az group exists -n $ResourceGroup | ConvertFrom-Json
    
    if (-not $rg) {
        Write-Info "Creating resource group: $ResourceGroup in $Location"
        az group create -n $ResourceGroup -l $Location | Out-Null
        Write-Success "Resource group created"
    }
    else {
        Write-Success "Resource group exists"
    }
}

function Ensure-Registry {
    Write-Info "Checking container registry: $Registry"
    
    if ([string]::IsNullOrWhiteSpace($Registry)) {
        Write-Error-Msg "Registry name is required"
        exit 1
    }
    
    $exists = az acr show -n $Registry -g $ResourceGroup 2>$null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Info "Creating container registry: $Registry"
        az acr create -n $Registry -g $ResourceGroup --sku Standard | Out-Null
        Write-Success "Container registry created"
    }
    else {
        Write-Success "Container registry exists"
    }
    
    Write-Info "Logging into registry..."
    az acr login -n $Registry | Out-Null
    Write-Success "Logged into registry"
}

function Ensure-Cluster {
    Write-Info "Checking AKS cluster: $ClusterName"
    
    $exists = az aks show -n $ClusterName -g $ResourceGroup 2>$null
    
    if ($LASTEXITCODE -ne 0) {
        Write-Info "Creating AKS cluster: $ClusterName"
        az aks create `
            -n $ClusterName `
            -g $ResourceGroup `
            --node-count 3 `
            --vm-set-type VirtualMachineScaleSets `
            --load-balancer-sku standard `
            --enable-managed-identity `
            --network-plugin azure `
            --network-policy azure `
            --enable-workload-identity-oidc `
            --attach-acr $Registry `
            --enable-cluster-autoscaling `
            --min-count 3 `
            --max-count 10 | Out-Null
        
        Write-Success "AKS cluster created with Workload Identity OIDC enabled"
    }
    else {
        Write-Success "AKS cluster exists"
    }
    
    Write-Info "Getting cluster credentials..."
    az aks get-credentials -n $ClusterName -g $ResourceGroup --overwrite-existing | Out-Null
    Write-Success "Cluster credentials updated"
}

# ============================================================================
# Workload Identity Setup
# ============================================================================

function Setup-WorkloadIdentity {
    Write-Section "WORKLOAD IDENTITY SETUP"

    if (-not $TenantId) {
        $TenantId = az account show --query tenantId -o tsv
        Write-Info "Using current subscription tenant: $TenantId"
    }

    # Check if script exists
    $setupScript = ".\k8s\setup-workload-identity.ps1"
    if (-not (Test-Path $setupScript)) {
        Write-Error-Msg "Setup script not found: $setupScript"
        exit 1
    }

    try {
        Write-Info "Running Workload Identity setup script..."
        & $setupScript `
            -ResourceGroup $ResourceGroup `
            -ClusterName $ClusterName `
            -KeyVaultName $KeyVaultName `
            -TenantId $TenantId `
            -Namespace saga-pattern `
            -ServiceAccount $ServiceAccount `
            -IdentityName $IdentityName `
            -Location $Location

        Write-Success "Workload Identity setup completed!"
    }
    catch {
        Write-Error-Msg "Workload Identity setup failed: $_"
        exit 1
    }
}

# ============================================================================
# Container Image Building
# ============================================================================

function Build-ContainerImages {
    Write-Section "BUILDING CONTAINER IMAGES"

    $services = @(
        @{Name = 'booking-api'; Path = 'Booking.API' },
        @{Name = 'flight-api'; Path = 'Flight.API' },
        @{Name = 'hotel-api'; Path = 'Hotel.API' },
        @{Name = 'car-api'; Path = 'Car.API' }
    )

    foreach ($service in $services) {
        $imageName = "$($service.Name):latest"
        Write-Info "Building $($service.Name)..."
        
        try {
            az acr build `
                -r $Registry `
                -t $imageName `
                -f "src/Services/$($service.Path)/Dockerfile" . | Out-Null
            
            Write-Success "Built $($service.Name)"
        }
        catch {
            Write-Error-Msg "Failed to build $($service.Name): $_"
            exit 1
        }
    }

    Write-Success "All images built successfully"
}

function Update-ManifestImages {
    Write-Info "Updating deployment manifests with registry: $Registry"
    
    $registryUrl = "$Registry.azurecr.io"
    
    $apiFiles = @(
        'k8s/booking-api.yaml',
        'k8s/flight-api.yaml',
        'k8s/hotel-api.yaml',
        'k8s/car-api.yaml'
    )

    foreach ($file in $apiFiles) {
        if (Test-Path $file) {
            $content = Get-Content $file
            $updated = $content -replace '<your-registry>', $registryUrl
            $updated | Set-Content $file
            Write-Success "Updated $file"
        }
    }
}

# ============================================================================
# Secret Management
# ============================================================================

function Deploy-SecretProviderClass {
    if (-not $EnableCSISecretProvider) {
        return
    }

    Write-Section "DEPLOYING CSI SECRET PROVIDER CLASS"

    try {
        if (-not $TenantId) {
            $TenantId = az account show --query tenantId -o tsv
        }

        $clientId = az identity show --name $IdentityName `
            --resource-group $ResourceGroup --query clientId -o tsv 2>$null
        
        if (-not $clientId) {
            Write-Error-Msg "Managed identity not found. Run 'setup-identity' action first."
            exit 1
        }

        Write-Info "Preparing SecretProviderClass manifest..."
        Write-Info "  Tenant ID: $TenantId"
        Write-Info "  Client ID: $clientId"
        Write-Info "  Key Vault: $KeyVaultName"

        # Read and substitute placeholders
        $secretProviderContent = Get-Content "k8s/secretproviderclass.yaml" -Raw
        $secretProviderContent = $secretProviderContent -replace '\$\{TENANT_ID\}', $TenantId
        $secretProviderContent = $secretProviderContent -replace '\$\{KEY_VAULT_NAME\}', $KeyVaultName
        $secretProviderContent = $secretProviderContent -replace '\$\{WORKLOAD_IDENTITY_CLIENT_ID\}', $clientId

        # Apply the manifest
        $secretProviderContent | kubectl apply -f - | Out-Null
        Write-Success "SecretProviderClass deployed"

        # Verify deployment
        $classes = kubectl get secretproviderclass -n saga-pattern -o name 2>$null
        if ($classes) {
            Write-Info "Deployed SecretProviderClasses:"
            foreach ($class in $classes) {
                Write-Info "  - $class"
            }
        }
    }
    catch {
        Write-Error-Msg "SecretProviderClass deployment failed: $_"
        exit 1
    }
}

function Get-KeyVaultSecretValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$VaultName,

        [Parameter(Mandatory = $true)]
        [string]$SecretName
    )

    $value = az keyvault secret show --vault-name $VaultName --name $SecretName --query value -o tsv 2>$null

    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($value)) {
        throw "Unable to read Key Vault secret '$SecretName' from vault '$VaultName'."
    }

    return $value
}

function Sync-KeyVaultSecrets {
    if (-not $UseKeyVault) {
        return
    }

    if ([string]::IsNullOrWhiteSpace($KeyVaultName)) {
        throw "-KeyVaultName is required when -UseKeyVault is set."
    }

    Write-Info "Syncing Kubernetes secrets from Azure Key Vault '$KeyVaultName'..."

    try {
        $postgresUsername = Get-KeyVaultSecretValue -VaultName $KeyVaultName -SecretName "saga-postgres-username"
        $postgresPassword = Get-KeyVaultSecretValue -VaultName $KeyVaultName -SecretName "saga-postgres-password"
        $rabbitmqUsername = Get-KeyVaultSecretValue -VaultName $KeyVaultName -SecretName "saga-rabbitmq-username"
        $rabbitmqPassword = Get-KeyVaultSecretValue -VaultName $KeyVaultName -SecretName "saga-rabbitmq-password"

        kubectl create secret generic postgres-credentials `
            --namespace saga-pattern `
            --from-literal=username="$postgresUsername" `
            --from-literal=password="$postgresPassword" `
            --dry-run=client -o yaml | kubectl apply -f - | Out-Null

        kubectl create secret generic rabbitmq-credentials `
            --namespace saga-pattern `
            --from-literal=username="$rabbitmqUsername" `
            --from-literal=password="$rabbitmqPassword" `
            --dry-run=client -o yaml | kubectl apply -f - | Out-Null

        Write-Success "Kubernetes secrets synced from Key Vault"
    }
    catch {
        Write-Error-Msg "Secret sync failed: $_"
        exit 1
    }
}

# ============================================================================
# Kubernetes Deployment
# ============================================================================

function Deploy-Application {
    Write-Section "DEPLOYING APPLICATION TO AKS"

    try {
        # Ensure namespace exists
        Write-Info "Creating namespace if needed..."
        kubectl create namespace saga-pattern --dry-run=client -o yaml | kubectl apply -f - 2>$null
        Write-Success "Namespace 'saga-pattern' ready"

        # Deploy SecretProviderClass if enabled
        Deploy-SecretProviderClass

        # Sync secrets from Key Vault if enabled
        Sync-KeyVaultSecrets

        # Apply manifests using Kustomize production overlay
        Write-Info "Deploying with Kustomize (production overlay)..."
        kubectl apply -k k8s/overlays/production 2>$null

        Write-Success "All manifests applied successfully"

        # Wait for deployments
        Write-Info ""
        Write-Info "Waiting for deployments to be ready (this may take 2-5 minutes)..."
        
        $deployments = @("booking-api", "flight-api", "hotel-api", "car-api", "otel-collector")
        foreach ($deployment in $deployments) {
            Write-Info "  Waiting for $deployment..."
            kubectl rollout status deployment/$deployment -n saga-pattern --timeout=5m 2>$null
        }

        Write-Success "All deployments are ready!"

    }
    catch {
        Write-Error-Msg "Deployment failed: $_"
        exit 1
    }
}

# ============================================================================
# Deployment Verification
# ============================================================================

function Verify-Deployment {
    Write-Section "VERIFYING DEPLOYMENT"

    try {
        Write-Info "Checking pod status..."
        $pods = kubectl get pods -n saga-pattern -o json | ConvertFrom-Json
        
        $running = ($pods.items | Where-Object { $_.status.phase -eq "Running" }).Count
        $total = $pods.items.Count

        Write-Info "Pod Status: $running/$total Running"
        Write-Info ""

        # Check ServiceAccount
        Write-Info "Verifying Workload Identity configuration..."
        $sa = kubectl get serviceaccount $ServiceAccount -n saga-pattern -o json 2>$null | ConvertFrom-Json
        
        if ($sa -and $sa.metadata.annotations.'azure.workload.identity/client-id') {
            Write-Success "ServiceAccount has correct Workload Identity annotation"
        }
        else {
            Write-Warning-Msg "ServiceAccount missing Workload Identity annotation"
        }

        # Check pod labels
        Write-Info ""
        Write-Info "Pod Workload Identity labels:"
        kubectl get pods -n saga-pattern -L azure.workload.identity/use 2>$null | Select-Object -Skip 1 | ForEach-Object {
            if ($_ -match "true") {
                Write-Success "  $($_)"
            }
            else {
                Write-Info "  $_"
            }
        }

        # Check services
        Write-Info ""
        Write-Info "Services and LoadBalancers:"
        kubectl get svc -n saga-pattern -o wide 2>$null | Where-Object { $_ -match "LoadBalancer|NAME" }

        Write-Success ""
        Write-Success "Verification complete!"

    }
    catch {
        Write-Error-Msg "Verification failed: $_"
        exit 1
    }
}

# ============================================================================
# Deployment Upgrades & Rollbacks
# ============================================================================

function Upgrade-Deployment {
    Write-Section "UPGRADING DEPLOYMENT"

    try {
        if ($Build) {
            Build-ContainerImages
            Update-ManifestImages
        }

        # Sync secrets if applicable
        Sync-KeyVaultSecrets

        Write-Info "Applying updated manifests..."
        $manifests = @(
            'k8s/booking-api.yaml',
            'k8s/flight-api.yaml',
            'k8s/hotel-api.yaml',
            'k8s/car-api.yaml'
        )
        
        foreach ($manifest in $manifests) {
            if (Test-Path $manifest) {
                kubectl apply -f $manifest 2>$null
            }
        }

        # Restart deployments
        Write-Info "Restarting deployments..."
        kubectl rollout restart deployment -n saga-pattern | Out-Null

        Write-Info "Waiting for rollout..."
        kubectl rollout status deployment/booking-api -n saga-pattern --timeout=5m | Out-Null

        Write-Success "Deployment upgraded successfully!"
        Verify-Deployment

    }
    catch {
        Write-Error-Msg "Upgrade failed: $_"
        exit 1
    }
}

function Rollback-Deployment {
    Write-Section "ROLLING BACK DEPLOYMENT"

    try {
        $services = @('booking-api', 'flight-api', 'hotel-api', 'car-api')
        
        foreach ($service in $services) {
            Write-Info "Rolling back $service..."
            kubectl rollout undo deployment/$service -n saga-pattern | Out-Null
            kubectl rollout status deployment/$service -n saga-pattern --timeout=5m 2>$null
        }
        
        Write-Success "Deployment rolled back successfully!"
        Verify-Deployment

    }
    catch {
        Write-Error-Msg "Rollback failed: $_"
        exit 1
    }
}

# ============================================================================
# Cleanup
# ============================================================================

function Cleanup-Resources {
    Write-Section "CLEANUP"

    Write-Warning-Msg "This will delete managed identity, ServiceAccount, and Kubernetes resources"
    $confirm = Read-Host "Continue? (yes/no)"
    if ($confirm -ne "yes") {
        Write-Info "Cleanup cancelled"
        return
    }

    try {
        Write-Info "Deleting managed identity '$IdentityName'..."
        az identity delete --name $IdentityName `
            --resource-group $ResourceGroup `
            --yes 2>$null
        Write-Success "Managed identity deleted"

        Write-Info "Removing Kubernetes ServiceAccount..."
        kubectl delete serviceaccount $ServiceAccount -n saga-pattern 2>$null
        Write-Success "ServiceAccount deleted"

        Write-Success "Cleanup complete! Key Vault secrets remain intact."

    }
    catch {
        Write-Error-Msg "Cleanup failed: $_"
        exit 1
    }
}

# ============================================================================
# Main Execution
# ============================================================================

try {
    Write-Header
    
    Write-Info "Configuration:"
    Write-Info "  Action: $Action"
    Write-Info "  Kubernetes Context: $KubernetesContext"
    Write-Info "  Environment: $Environment"
    Write-Info "  Resource Group: $ResourceGroup"
    Write-Info "  Cluster: $ClusterName"
    Write-Info "  Registry: $Registry"
    Write-Info "  Service Account: $ServiceAccount"
    if ($EnableCSISecretProvider) {
        Write-Info "  CSI SecretProvider: Enabled"
    }
    if ($UseKeyVault) {
        Write-Info "  Key Vault: $KeyVaultName"
    }
    Write-Info ""
    
    # Ensure we're on the correct context
    Set-KubernetesContext -ContextName $KubernetesContext
    Write-Info ""
    
    Test-Prerequisites
    Write-Info ""
    
    switch ($Action) {
        'setup-identity' {
            Ensure-ResourceGroup
            Ensure-Cluster
            Setup-WorkloadIdentity
        }
        'deploy' {
            Ensure-ResourceGroup
            Ensure-Registry
            Ensure-Cluster
            Deploy-Application
            Verify-Deployment
        }
        'upgrade' {
            Upgrade-Deployment
        }
        'rollback' {
            Rollback-Deployment
        }
        'verify' {
            Verify-Deployment
        }
        'cleanup' {
            Cleanup-Resources
        }
    }
    
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║ ✅ Action completed successfully!              ║" -ForegroundColor Green
    Write-Host "╚════════════════════════════════════════════════╝" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Error-Msg "Fatal error: $_"
    exit 1
}
