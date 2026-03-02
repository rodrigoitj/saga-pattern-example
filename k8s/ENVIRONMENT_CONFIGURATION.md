# Environment Configuration Guide

## Overview

This guide explains how to maintain different configurations for **Local Development** and **Production AKS** environments with a single codebase.

```
┌─────────────────────────────────────────────────────────────────┐
│                    APPLICATION CODEBASE                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──────────────────────┐      ┌──────────────────────────┐   │
│  │  Local Development   │      │   Production (AKS)       │   │
│  ├──────────────────────┤      ├──────────────────────────┤   │
│  │ • docker-compose     │      │ • Kubernetes manifests   │   │
│  │ • Hardcoded secrets  │      │ • Workload Identity      │   │
│  │ • Local postgres/rmq │      │ • Azure Key Vault        │   │
│  │ • Fast iteration     │      │ • CSI SecretProvider     │   │
│  │ • No Azure deps      │      │ • Zero-credential auth   │   │
│  └──────────────────────┘      └──────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

## Local Development Environment

### What You Get

- ✅ **No Azure dependencies** - Completely offline capable
- ✅ **Fast iteration** - Changes reflected immediately
- ✅ **Simple debugging** - Standard .NET debugging tools
- ✅ **No credential management** - Hardcoded defaults
- ✅ **Single command startup** - `docker-compose up`

### Setup

```bash
# 1. No Azure CLI or Key Vault needed
# 2. Start containers
docker-compose -f docker-compose.dev.yml up

# 3. Services available at:
# - Booking API: http://localhost:5001/swagger
# - RabbitMQ: http://localhost:15672 (guest/guest)
# - PostgreSQL: localhost:5432 (postgres/postgres)
```

### Configuration Files

| File | Purpose | Environment |
|------|---------|-------------|
| `appsettings.json` | Base settings (shared) | All |
| `appsettings.Development.json` | Dev overrides | Local only |
| `appsettings.Production.json` | Prod overrides | AKS only |
| `docker-compose.dev.yml` | Local services | Local only |

### Default Secrets (Development Only)

```env
# PostgreSQL
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres

# RabbitMQ
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest
```

### Running Locally

```bash
# Terminal 1: Start infrastructure
docker-compose -f docker-compose.dev.yml up

# Terminal 2: Run the application (or use IDE)
cd src/Services/Booking.API
dotnet run --configuration Development

# Terminal 3: Test API
curl http://localhost:5000/swagger
```

**No changes needed to code!** The application automatically uses:
- `appsettings.json` (base config)
- `appsettings.Development.json` (overrides)
- Environment variables from docker-compose

---

## Production AKS Environment

### What You Get

- ✅ **Zero credentials in manifests** - All secrets from Key Vault
- ✅ **Workload Identity** - OIDC token exchange for authentication
- ✅ **Automatic secret sync** - CSI driver keeps secrets in sync
- ✅ **Audit trail** - All access logged in Key Vault
- ✅ **Production-grade security** - RBAC, encryption, rotation

### Prerequisites

```powershell
# 1. Azure CLI authenticated
az login
az account set --subscription <SUBSCRIPTION_ID>

# 2. AKS cluster exists
az aks list --resource-group saga-pattern-rg

# 3. Key Vault exists with secrets
az keyvault secret list --vault-name saga-pattern-kv

# 4. kubectl configured
kubectl cluster-info
kubectl get nodes  # Should show AKS nodes
```

### Complete Setup (Step by Step)

#### Step 1: Create Secrets in Key Vault

```powershell
# PostgreSQL credentials
az keyvault secret set --vault-name saga-pattern-kv \
  --name saga-postgres-username \
  --value postgres

az keyvault secret set --vault-name saga-pattern-kv \
  --name saga-postgres-password \
  --value "YourSecurePassword123!"

# RabbitMQ credentials
az keyvault secret set --vault-name saga-pattern-kv \
  --name saga-rabbitmq-username \
  --value saga-user

az keyvault secret set --vault-name saga-pattern-kv \
  --name saga-rabbitmq-password \
  --value "YourSecurePassword456!"
```

#### Step 2: Setup Workload Identity

```powershell
$tenantId = (az account show --query tenantId -o tsv)

.\k8s\deploy-production-aks.ps1 `
  -Action setup-identity `
  -ResourceGroup saga-pattern-rg `
  -ClusterName saga-pattern-aks `
  -KeyVaultName saga-pattern-kv `
  -TenantId $tenantId
```

This creates:
- ✅ Managed identity
- ✅ OIDC federation
- ✅ Kubernetes ServiceAccount
- ✅ Key Vault access policies

#### Step 3: Deploy Application

```powershell
.\k8s\deploy-production-aks.ps1 `
  -Action deploy `
  -ResourceGroup saga-pattern-rg `
  -ClusterName saga-pattern-aks `
  -KeyVaultName saga-pattern-kv `
  -EnableCSISecretProvider
```

This deploys:
- ✅ SecretProviderClass manifests
- ✅ All Kubernetes resources
- ✅ Pods with Workload Identity
- ✅ CSI volume mounts

#### Step 4: Verify Deployment

```powershell
.\k8s\deploy-production-aks.ps1 `
  -Action verify `
  -ResourceGroup saga-pattern-rg `
  -ClusterName saga-pattern-aks
```

Checks:
- ✅ Pod Workload Identity annotations
- ✅ ServiceAccount configuration
- ✅ Secret synchronization
- ✅ Service availability

### Configuration Flow

```
AKS Pod (with Workload Identity)
        ↓
Azure OIDC Issuer (validates pod identity)
        ↓
Managed Identity Token Exchange
        ↓
Azure Key Vault Access
        ↓
CSI SecretProvider Mount
        ↓
Kubernetes Secret Created
        ↓
Environment Variables Injected
        ↓
Application Code (reads env vars)
```

### Application Configuration (Production)

The application reads:

1. **appsettings.json** - Base settings
2. **appsettings.Production.json** - Production overrides
3. **Environment variables** - Runtime injections from Kubernetes Secrets

```csharp
// Program.cs - Works in BOTH local and production!
var builder = WebApplication.CreateBuilder(args);

// Automatically uses appropriate config file based on environment
// Development → appsettings.Development.json + env vars from docker-compose
// Production → appsettings.Production.json + env vars from K8s secrets

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// Contains: "Host=postgres;Database=BookingDb;Username=postgres;Password=${POSTGRES_PASSWORD}"
// Variable ${POSTGRES_PASSWORD} is replaced by K8s at runtime
```

### Running in Production

No application code changes needed! Just deploy with:

```powershell
kubectl apply -f k8s/booking-api.yaml
```

The deployment specifies:
```yaml
spec:
  serviceAccountName: saga-pattern-sa        # ← Workload Identity
  labels:
    azure.workload.identity/use: "true"      # ← Enable Workload Identity
  env:
  - name: POSTGRES_PASSWORD
    valueFrom:
      secretKeyRef:
        name: postgres-credentials           # ← From CSI/Key Vault
        key: password
```

---

## Key Differences Summary

| Aspect | Local Development | Production AKS |
|--------|------------------|-----------------|
| **Container Orchestration** | Docker Compose | Kubernetes |
| **Secret Storage** | Hardcoded / .env | Azure Key Vault |
| **Authentication** | None | Workload Identity + OIDC |
| **Secret Injection** | Environment variables | CSI + K8s Secrets |
| **Infrastructure** | Local SQL + RabbitMQ | Managed Azure services |
| **Configuration File** | appsettings.Development.json | appsettings.Production.json |
| **Startup Time** | ~20 seconds | ~3-5 minutes |
| **Cost** | Laptop resources | Azure consumption |
| **Debugging** | Visual Studio debugger | `kubectl logs` / `stern` |

---

## Security Best Practices

### Local Development (OK to be lenient)

✅ Use simple, hardcoded passwords  
✅ Store in `.env` or appsettings.json (never commit)  
✅ Disable HTTPS/SSL requirements  
✅ Allow any localhost traffic  

### Production AKS (Zero Trust)

❌ **Never** hardcode credentials  
✅ Use Azure Key Vault for secret storage  
✅ Use Workload Identity for authentication  
✅ Enable Kubernetes audit logging  
✅ Use NetworkPolicies for pod-to-pod communication  
✅ Enable RBAC on all API operations  
✅ Encrypt secrets at rest in etcd  
✅ Implement OIDC federation  
✅ Rotate credentials regularly  
✅ Monitor Key Vault access logs  

---

## Switching Environments

### From Development to Production

```bash
# 1. All code remains the same
# 2. No code changes needed
# 3. Only deployment process changes:

# Development
docker-compose -f docker-compose.dev.yml up

# Production
./k8s/deploy-production-aks.ps1 -Action setup-identity
./k8s/deploy-production-aks.ps1 -Action deploy
./k8s/deploy-production-aks.ps1 -Action verify
```

### Configuration Resolution Order

#### Development
1. `appsettings.json` (base)
2. `appsettings.Development.json` (overrides)
3. Environment variables from `docker-compose.dev.yml`
4. Hardcoded defaults in code

#### Production
1. `appsettings.json` (base)
2. `appsettings.Production.json` (overrides)
3. Environment variables from Kubernetes Secrets
4. Values from Key Vault (via CSI mounts)

---

## Troubleshooting

### Development Issues

#### "Connection refused" on PostgreSQL

```bash
# Check if containers are running
docker-compose -f docker-compose.dev.yml ps

# Check logs
docker-compose -f docker-compose.dev.yml logs postgres

# Restart containers
docker-compose -f docker-compose.dev.yml restart
```

#### "Cannot connect to RabbitMQ"

```bash
# Verify RabbitMQ is healthy
docker-compose -f docker-compose.dev.yml exec rabbitmq rabbitmq-diagnostics ping

# Check credentials in docker-compose.dev.yml
grep RABBITMQ docker-compose.dev.yml
```

### Production Issues

#### "Pods in CrashLoopBackOff"

```powershell
# Check pod logs
kubectl logs <pod-name> -n saga-pattern -f

# Check Workload Identity annotation
kubectl describe pod <pod-name> -n saga-pattern | grep identity

# Verify secrets are synced
kubectl get secrets -n saga-pattern
kubectl describe secret postgres-credentials -n saga-pattern
```

#### "Cannot access Key Vault"

```powershell
# Verify Workload Identity is set up
az identity show --name saga-workload-identity --resource-group saga-pattern-rg

# Check federated credential
az identity federated-credential list `
  --name saga-workload-identity `
  --resource-group saga-pattern-rg

# Verify Key Vault access policy
az keyvault list-access-policies --name saga-pattern-kv
```

---

## Next Steps

1. **Local Development**: Run `docker-compose -f docker-compose.dev.yml up`
2. **Test Application**: Navigate to http://localhost:5000/swagger
3. **Production Deployment**: Follow [PRODUCTION_SECURITY_GUIDE.md](./PRODUCTION_SECURITY_GUIDE.md)
4. **Security Hardening**: Review [../OBSERVABILITY.md](../OBSERVABILITY.md) for monitoring

---

## References

- [ASP.NET Core Configuration](https://learn.microsoft.com/aspnet/core/fundamentals/configuration)
- [Docker Compose](https://docs.docker.com/compose/)
- [Workload Identity on AKS](https://learn.microsoft.com/azure/aks/workload-identity-overview)
- [Azure Key Vault](https://learn.microsoft.com/azure/key-vault/)
