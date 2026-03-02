# Production AKS Deployment - Workload Identity Configuration

## Overview

This guide explains how to deploy the Saga Pattern application to Azure Kubernetes Service (AKS) with production-grade secret management using **Workload Identity + Azure Key Vault**.

### Architecture

```
┌─────────────────────────────────────────┐
│         Kubernetes Pod (API)            │
├─────────────────────────────────────────┤
│  • ServiceAccount: saga-pattern-sa      │
│  • Label: azure.workload.identity/use   │
│  • Container: booking-api, flight, etc. │
└────────┬────────────────────────────────┘
         │
         │ (OIDC Token Exchange)
         │
         ▼
┌─────────────────────────────────────────┐
│    Azure AD Federation                  │
│  (OIDC Issuer on K8s cluster)          │
└────────┬────────────────────────────────┘
         │
         │ (Service Principal Token)
         │
         ▼
┌─────────────────────────────────────────┐
│    Managed Identity                     │
│  (saga-workload-identity)               │
└────────┬────────────────────────────────┘
         │
         │ (Secret Retrieval)
         │
         ▼
┌─────────────────────────────────────────┐
│    Azure Key Vault                      │
│  • saga-postgres-password               │
│  • saga-rabbitmq-password               │
│  • saga-postgres-username               │
│  • saga-rabbitmq-username               │
└─────────────────────────────────────────┘
```

## Setup Steps

### Prerequisites

- **Azure CLI** installed and authenticated
- **kubectl** configured for your AKS cluster
- **AKS Cluster** running with support for OIDC issuer
- **Azure Key Vault** created with required secrets

### 1. Enable AKS Features (One-time Setup)

```powershell
# Enable OIDC issuer for the cluster
az aks update --name saga-pattern-aks \
  --resource-group saga-pattern-rg \
  --enable-oidc-issuer

# Enable AKS secret encryption at rest
az aks update --name saga-pattern-aks \
  --resource-group saga-pattern-rg \
  --enable-secret-rotation
```

### 2. Run the Workload Identity Setup Script

```powershell
.\k8s\setup-workload-identity.ps1 `
  -ResourceGroup saga-pattern-rg `
  -ClusterName saga-pattern-aks `
  -KeyVaultName saga-pattern-kv `
  -TenantId <YOUR_TENANT_ID> `
  -Namespace saga-pattern `
  -ServiceAccount saga-pattern-sa
```

This script will:
- ✅ Create a managed identity (`saga-workload-identity`)
- ✅ Grant Key Vault access to the identity
- ✅ Create OIDC federation between K8s and Azure AD
- ✅ Create Kubernetes ServiceAccount with proper annotations
- ✅ Verify all components are configured correctly

### 3. Deploy SecretProviderClass

The SecretProviderClass enables CSI driver integration with Key Vault:

```powershell
# First, replace placeholders in the manifest
$tenantId = (az account show --query tenantId -o tsv)
$keyVaultName = "saga-pattern-kv"
$clientId = (az identity show --name saga-workload-identity `
  --resource-group saga-pattern-rg --query clientId -o tsv)

# Replace environment variables in the manifest
$content = Get-Content k8s/secretproviderclass.yaml
$content = $content -replace '\$\{TENANT_ID\}', $tenantId
$content = $content -replace '\$\{KEY_VAULT_NAME\}', $keyVaultName
$content = $content -replace '\$\{WORKLOAD_IDENTITY_CLIENT_ID\}', $clientId
$content | kubectl apply -f -

# Verify
kubectl get secretproviderclass -n saga-pattern
```

### 4. Deploy Manifests with Workload Identity

```powershell
./k8s/deploy-to-aks.ps1 -Action deploy `
  -Registry sagapatternacr `
  -Cluster saga-pattern-aks `
  -ResourceGroup saga-pattern-rg `
  -Location brazilsouth `
  -UseKeyVault `
  -KeyVaultName saga-pattern-kv `
  -EnableWorkloadIdentity
```

### 5. Verify Workload Identity is Working

```powershell
# Check pod has the correct ServiceAccount
kubectl get pod -n saga-pattern -o jsonpath='{.items[0].spec.serviceAccountName}'

# Check pod label for Workload Identity
kubectl get pods -n saga-pattern -L azure.workload.identity/use

# Verify CSI volumes are mounted (if using CSI)
kubectl describe pod <pod-name> -n saga-pattern | grep -A 5 "Mounts:"

# Test secret access from pod
kubectl exec <pod-name> -n saga-pattern -- env | grep POSTGRES_PASSWORD
```

## Environment-Specific Configuration

### Local Development (docker-compose)

**No changes needed!** The docker-compose.dev.yml continues to work as-is:

```bash
docker-compose -f docker-compose.dev.yml up
```

Uses:
- ❌ No Azure integration
- ✅ Local environment variables
- ✅ Local secrets in `.env` or hardcoded
- ✅ Fast iteration and debugging

### Production (AKS)

Uses:
- ✅ **Workload Identity** for pod authentication
- ✅ **Azure Key Vault** for secret storage
- ✅ **CSI SecretProvider** for secret injection
- ✅ **OIDC Federation** for zero-credential authentication
- ✅ **Automatic secret syncing** to Kubernetes Secrets
- ✅ **Audit trail** in Key Vault logs

## How Secrets Flow in Production

### For Kubernetes Secrets (postgres-credentials, rabbitmq-credentials)

1. **Pod starts** with `azure.workload.identity/use: "true"` label
2. **Workload Identity webhook intercepts** the pod startup
3. **OIDC token exchanged** between Kubernetes and Azure AD
4. **Managed Identity authenticated** via OIDC token
5. **CSI SecretProvider mounted** in the pod
6. **Secrets fetched from Key Vault** using authenticated identity
7. **Kubernetes Secret created/synced** from CSI mount (`secretObjects`)
8. **Environment variables populated** from Kubernetes Secret
9. **Application reads** connection strings with proper credentials

### For Direct Key Vault Access (Advanced)

If your application uses `Azure.Identity.DefaultAzureCredential`:

```csharp
// Program.cs
var credential = new DefaultAzureCredential();
var client = new SecretClient(
    new Uri("https://saga-pattern-kv.vault.azure.net/"),
    credential
);

var secret = await client.GetSecretAsync("saga-postgres-password");
// Workload Identity automatically provides the credential
```

## Troubleshooting

### Pod stuck in CrashLoopBackOff

```powershell
# Check pod logs
kubectl logs -f <pod-name> -n saga-pattern

# Check CSI driver status
kubectl get pods -n kube-system | grep csi

# Verify SecretProviderClass is mounted
kubectl describe pod <pod-name> -n saga-pattern | grep "azure-kv"
```

### Cannot access Key Vault

```powershell
# Verify ServiceAccount has Workload Identity annotation
kubectl describe sa saga-pattern-sa -n saga-pattern

# Verify federated credential exists
az identity federated-credential list \
  --name saga-workload-identity \
  --resource-group saga-pattern-rg

# Check Key Vault access policy
az keyvault list-access-policies \
  --name saga-pattern-kv \
  --resource-group saga-pattern-rg
```

### Secrets not syncing to Kubernetes Secret

```powershell
# Check SecretProviderClass status
kubectl get secretproviderclass -n saga-pattern -o yaml

# Check if Kubernetes Secret was created
kubectl get secrets -n saga-pattern

# Manually trigger sync (delete pod to restart)
kubectl delete pod <pod-name> -n saga-pattern
```

## Security Considerations

### What's Protected?

✅ **Zero credentials in manifests** - No hardcoded passwords  
✅ **Automatic token rotation** - Tokens refreshed hourly  
✅ **RBAC control** - Fine-grained Key Vault access policies  
✅ **Audit logs** - All access logged in Key Vault activity logs  
✅ **Encryption in transit** - TLS for all communication  
✅ **Encryption at rest** - Kubernetes secrets encrypted in etcd  

### Best Practices

1. **Rotate credentials regularly** using Azure's secret rotation
2. **Enable Key Vault logging** for audit trail
3. **Limit ServiceAccount permissions** - Only allow secret retrieval
4. **Use separate identities** per service if needed (advanced)
5. **Monitor CSI driver** for any sync failures
6. **Version your secrets** in Key Vault for easy rollback

## Cleanup (Optional)

```powershell
# Tear down Workload Identity (keeps Key Vault secrets intact)
az identity delete --name saga-workload-identity \
  --resource-group saga-pattern-rg

# Remove federated credentials
az identity federated-credential delete \
  --name saga-fed-cred \
  --identity-name saga-workload-identity \
  --resource-group saga-pattern-rg
```

## References

- [Workload Identity Overview](https://learn.microsoft.com/en-us/azure/aks/workload-identity-overview)
- [CSI Secrets Provider Overview](https://learn.microsoft.com/en-us/azure/aks/csi-secrets-store-driver)
- [Key Vault Best Practices](https://learn.microsoft.com/en-us/azure/key-vault/general/best-practices)
