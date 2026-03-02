# ============================================================================
# Setup Workload Identity for AKS Production
# ============================================================================
# This script configures Azure Workload Identity federation for secure
# Key Vault access from AKS pods without storing credentials.
#
# Prerequisites:
# - Azure CLI configured with appropriate permissions
# - AKS cluster already created
# - Azure Key Vault already created
#
# Usage:
#   .\setup-workload-identity.ps1 -ResourceGroup saga-pattern-rg `
#     -ClusterName saga-pattern-aks `
#     -KeyVaultName saga-pattern-kv `
#     -TenantId <YOUR_TENANT_ID>
# ============================================================================

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory=$true)]
    [string]$ClusterName,

    [Parameter(Mandatory=$true)]
    [string]$KeyVaultName,

    [Parameter(Mandatory=$true)]
    [string]$TenantId,

    [string]$Namespace = "saga-pattern",
    [string]$ServiceAccount = "saga-pattern-sa",
    [string]$IdentityName = "saga-workload-identity",
    [string]$Location = "eastus"
)

$ErrorActionPreference = "Stop"

function Write-Info {
    param([string]$Message)
    Write-Host "ℹ️  $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

try {
    Write-Info "Starting Workload Identity setup..."
    Write-Info "Resource Group: $ResourceGroup"
    Write-Info "Cluster: $ClusterName"
    Write-Info "Key Vault: $KeyVaultName"
    Write-Info "Tenant ID: $TenantId"
    Write-Info ""

    # Step 1: Create Managed Identity
    Write-Info "Step 1: Creating managed identity '$IdentityName'..."
    $identityCheck = az identity list --resource-group $ResourceGroup --query "[?name=='$IdentityName'].id" -o tsv 2>$null
    
    if ($identityCheck) {
        Write-Success "Managed identity '$IdentityName' already exists"
    } else {
        az identity create --name $IdentityName `
            --resource-group $ResourceGroup `
            --location $Location `
            -o none 2>$null
        Write-Success "Created managed identity '$IdentityName'"
    }

    # Get identity details
    $identityId = az identity show --name $IdentityName `
        --resource-group $ResourceGroup --query id -o tsv
    $clientId = az identity show --name $IdentityName `
        --resource-group $ResourceGroup --query clientId -o tsv
    $principalId = az identity show --name $IdentityName `
        --resource-group $ResourceGroup --query principalId -o tsv

    Write-Info "Identity Client ID: $clientId"
    Write-Info "Identity Principal ID: $principalId"
    Write-Info ""

    # Step 2: Grant Key Vault access
    Write-Info "Step 2: Granting Key Vault access to managed identity..."
    az keyvault set-policy --name $KeyVaultName `
        --object-id $principalId `
        --secret-permissions get list `
        -o none 2>$null
    Write-Success "Granted 'get' and 'list' permissions on Key Vault"
    Write-Info ""

    # Step 3: Enable OIDC issuer on AKS cluster
    Write-Info "Step 3: Enabling OIDC issuer on AKS cluster..."
    $oidcCheck = az aks show --name $ClusterName `
        --resource-group $ResourceGroup --query "oidcIssuerProfile.issuerUrl" -o tsv 2>$null
    
    if ($oidcCheck) {
        Write-Success "OIDC issuer already enabled: $oidcCheck"
    } else {
        Write-Info "This may take 2-3 minutes..."
        az aks update --name $ClusterName `
            --resource-group $ResourceGroup `
            --enable-oidc-issuer `
            -o none

        # Wait for OIDC to be configured
        Start-Sleep -Seconds 10
        $oidcCheck = az aks show --name $ClusterName `
            --resource-group $ResourceGroup --query "oidcIssuerProfile.issuerUrl" -o tsv
        Write-Success "OIDC issuer enabled: $oidcCheck"
    }
    Write-Info ""

    # Step 4: Create Kubernetes ServiceAccount and Federated Credential
    Write-Info "Step 4: Creating Kubernetes ServiceAccount and federated credential..."
    
    # First, ensure namespace exists
    Write-Info "Creating namespace '$Namespace' if needed..."
    kubectl create namespace $Namespace --dry-run=client -o yaml | kubectl apply -f - 2>$null
    Write-Success "Namespace '$Namespace' ready"

    # Create ServiceAccount
    Write-Info "Creating ServiceAccount '$ServiceAccount'..."
    kubectl create serviceaccount $ServiceAccount -n $Namespace --dry-run=client -o yaml | kubectl apply -f - 2>$null
    Write-Success "ServiceAccount '$ServiceAccount' created"

    # Annotate ServiceAccount with client ID
    kubectl annotate serviceaccount $ServiceAccount `
        -n $Namespace `
        azure.workload.identity/client-id=$clientId `
        --overwrite=true 2>$null
    Write-Success "Annotated ServiceAccount with client ID"
    Write-Info ""

    # Step 5: Create Federated Credential
    Write-Info "Step 5: Creating federated credential..."
    
    $issuerUrl = az aks show --name $ClusterName `
        --resource-group $ResourceGroup --query "oidcIssuerProfile.issuerUrl" -o tsv
    
    $fedCredCheck = az identity federated-credential list `
        --name $IdentityName `
        --resource-group $ResourceGroup `
        --query "[?name=='saga-fed-cred'].name" -o tsv 2>$null

    if ($fedCredCheck) {
        Write-Success "Federated credential 'saga-fed-cred' already exists"
    } else {
        az identity federated-credential create `
            --name saga-fed-cred `
            --identity-name $IdentityName `
            --resource-group $ResourceGroup `
            --issuer $issuerUrl `
            --subject "system:serviceaccount:${Namespace}:${ServiceAccount}" `
            -o none 2>$null
        Write-Success "Created federated credential 'saga-fed-cred'"
    }
    Write-Info ""

    # Step 6: Verify setup
    Write-Info "Step 6: Verifying setup..."
    
    $saAnnotation = kubectl get serviceaccount $ServiceAccount `
        -n $Namespace -o jsonpath='{.metadata.annotations.azure\.workload\.identity/client-id}' 2>$null
    
    if ($saAnnotation -eq $clientId) {
        Write-Success "ServiceAccount correctly annotated with client ID"
    } else {
        Write-Error-Custom "ServiceAccount annotation mismatch!"
        exit 1
    }

    Write-Success "All verification checks passed!"
    Write-Info ""

    # Summary
    Write-Info "=====================================================
    Write-Host "Setup Complete! Here's what was configured:" -ForegroundColor Yellow
    Write-Info ""
    Write-Host "Managed Identity:" -ForegroundColor Yellow
    Write-Host "  Name: $IdentityName"
    Write-Host "  Client ID: $clientId"
    Write-Host "  Resource ID: $identityId"
    Write-Host ""
    Write-Host "Azure OIDC Issuer:" -ForegroundColor Yellow
    Write-Host "  Issuer URL: $issuerUrl"
    Write-Host ""
    Write-Host "Kubernetes:" -ForegroundColor Yellow
    Write-Host "  Namespace: $Namespace"
    Write-Host "  ServiceAccount: $ServiceAccount"
    Write-Host "  Federated Credential: saga-fed-cred"
    Write-Host ""
    Write-Host "Key Vault: " -ForegroundColor Yellow
    Write-Host "  Name: $KeyVaultName"
    Write-Host "  Permissions: get, list"
    Write-Host ""
    Write-Host "Next Steps:" -ForegroundColor Yellow
    Write-Host "  1. Deploy your workloads with:"
    Write-Host "     serviceAccountName: $ServiceAccount"
    Write-Host "     and label: azure.workload.identity/use: 'true'"
    Write-Host "  2. Update your application to use DefaultAzureCredential"
    Write-Host "  3. Verify pods can access Key Vault using:"
    Write-Host "     kubectl exec <pod-name> -- az keyvault secret show --vault-name $KeyVaultName --name <secret-name>"
    Write-Host ""
    Write-Host "=====================================================

    Write-Success "Workload Identity setup completed successfully!"

} catch {
    Write-Error-Custom "Setup failed: $_"
    exit 1
}
