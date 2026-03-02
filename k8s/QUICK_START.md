# Saga Pattern - Environment Setup Guide

## Quick Start

### 🚀 Local Development (5 minutes)

**No Azure account needed!**

```bash
# 1. Copy environment file
cp .env.example .env

# 2. Start services
docker-compose -f docker-compose.dev.yml up

# 3. Access application
# Booking API: http://localhost:5000/swagger
# RabbitMQ: http://localhost:15672 (guest/guest)
# PostgreSQL: localhost:5432 (postgres/postgres)
```

### ☁️ Production AKS (30 minutes)

**Requires Azure account and existing AKS cluster**

```powershell
# 1. Get your tenant ID
$tenantId = (az account show --query tenantId -o tsv)

# 2. Setup Workload Identity and OIDC federation
.\k8s\deploy-production-aks.ps1 `
  -Action setup-identity `
  -ResourceGroup saga-pattern-rg `
  -ClusterName saga-pattern-aks `
  -KeyVaultName saga-pattern-kv `
  -TenantId $tenantId

# 3. Deploy to AKS
.\k8s\deploy-production-aks.ps1 `
  -Action deploy `
  -ResourceGroup saga-pattern-rg `
  -ClusterName saga-pattern-aks `
  -EnableCSISecretProvider

# 4. Verify deployment
.\k8s\deploy-production-aks.ps1 `
  -Action verify `
  -ResourceGroup saga-pattern-rg `
  -ClusterName saga-pattern-aks
```

---

## Architecture

### Local Development Stack

```
┌──────────────────────────────────────────┐
│       Docker Compose Services            │
├──────────────────────────────────────────┤
│                                          │
│  ┌────────────┐  ┌──────────────────┐  │
│  │ PostgreSQL │  │  RabbitMQ + UI   │  │
│  │  (5432)    │  │   (5672/15672)   │  │
│  └────────────┘  └──────────────────┘  │
│        ▲                    ▲            │
│        │                    │            │
│  ┌─────────────────────────────────┐   │
│  │  .NET 8 APIs (Docker)           │   │
│  │  • Booking API (5000)           │   │
│  │  • Flight API (consumer)        │   │
│  │  • Hotel API (consumer)         │   │
│  │  • Car API (consumer)           │   │
│  └─────────────────────────────────┘   │
│                                          │
│  ┌────────────────────────────────┐    │
│  │  Observability Stack            │    │
│  │  • Prometheus (9090)            │    │
│  │  • Grafana (3000)               │    │
│  │  • OpenTelemetry Collector      │    │
│  │  • Tempo                        │    │
│  └────────────────────────────────┘    │
└──────────────────────────────────────────┘
```

### Production AKS Stack

```
┌──────────────────────────────────────────┐
│      Azure Kubernetes Service (AKS)      │
├──────────────────────────────────────────┤
│                                          │
│  ┌──────────────────────────────────┐  │
│  │  Kubernetes Resources             │  │
│  │  • Deployments (4 APIs)           │  │
│  │  • StatefulSets (Postgres, RmQ)   │  │
│  │  • Services (LoadBalancer, CIP)   │  │
│  │  • Ingress (Path-based routing)   │  │
│  │  • HPA (Auto-scaling)             │  │
│  │  • NetworkPolicies (Zero-trust)   │  │
│  └──────────────────────────────────┘  │
│          │                              │
│          ▼                              │
│  ┌──────────────────────────────────┐  │
│  │  Secret Management                │  │
│  │  • Workload Identity              │  │
│  │  • OIDC Federation                │  │
│  │  • CSI SecretProvider              │  │
│  │  • K8s Secrets (synced from KV)   │  │
│  └──────────────────────────────────┘  │
│          │                              │
│          ▼                              │
│  ┌──────────────────────────────────┐  │
│  │  Azure Key Vault                  │  │
│  │  • saga-postgres-password         │  │
│  │  • saga-rabbitmq-password         │  │
│  │  • Audit logs                     │  │
│  │  • Access policies                │  │
│  └──────────────────────────────────┘  │
│                                          │
└──────────────────────────────────────────┘
```

---

## Configuration Comparison

| Feature | Local Dev | Production |
|---------|-----------|-----------|
| **Orchestration** | Docker Compose | Kubernetes |
| **Infrastructure** | Local containers | Azure managed services |
| **Secret Storage** | Hardcoded / .env | Azure Key Vault |
| **Authentication** | None | Workload Identity + OIDC |
| **Database** | PostgreSQL container | Azure Database for PostgreSQL |
| **Message Broker** | RabbitMQ container | Managed RabbitMQ or Event Grid |
| **Monitoring** | Prometheus/Grafana containers | Azure Monitor + Grafana on AKS |
| **Startup Time** | ~30 seconds | ~5 minutes |
| **Cost** | Free (laptop) | Azure consumption-based |
| **Configuration File** | appsettings.Development.json | appsettings.Production.json |

---

## File Structure

```
saga-pattern-example/
├── .env.example                          ← Copy for local secrets
├── docker-compose.dev.yml                ← Local development services
├── docker-compose.yml                    ← Production-like services
├── src/
│   └── Services/
│       ├── Booking.API/
│       │   ├── appsettings.json         ← Base config (shared)
│       │   ├── appsettings.Development.json  ← Local overrides
│       │   └── appsettings.Production.json   ← AKS overrides
│       ├── Flight.API/
│       ├── Hotel.API/
│       └── Car.API/
├── k8s/
│   ├── ENVIRONMENT_CONFIGURATION.md      ← THIS GUIDE
│   ├── PRODUCTION_SECURITY_GUIDE.md      ← Security details
│   ├── setup-workload-identity.ps1       ← Workload ID setup
│   ├── deploy-production-aks.ps1         ← AKS deployment
│   ├── secretproviderclass.yaml          ← CSI manifests
│   ├── booking-api.yaml                  ← Deployment manifests
│   ├── flight-api.yaml
│   ├── hotel-api.yaml
│   ├── car-api.yaml
│   ├── postgres.yaml                     ← Infrastructure manifests
│   ├── rabbitmq.yaml
│   ├── otel-collector.yaml
│   ├── tempo.yaml
│   ├── prometheus.yaml
│   ├── grafana.yaml
│   └── ... (other k8s resources)
└── README.md                              ← Main project README
```

---

## Local Development

### Prerequisites

- **Docker & Docker Compose** (latest version)
- **.NET 8 SDK** (optional, for running without Docker)
- **PostgreSQL client** (optional, for direct DB access)
- **RabbitMQ Management Plugin** (included in container)

### Getting Started

#### Option 1: Using Docker Compose (Recommended)

```bash
# Clone the repository
git clone https://github.com/your-repo/saga-pattern-example.git
cd saga-pattern-example

# Copy and customize environment
cp .env.example .env
# Edit .env if needed (usually not necessary for local dev)

# Start all services
docker-compose -f docker-compose.dev.yml up

# In another terminal, verify services
docker-compose -f docker-compose.dev.yml exec postgres pg_isready
docker-compose -f docker-compose.dev.yml exec rabbitmq rabbitmq-diagnostics ping
```

#### Option 2: Running APIs Locally (Recommended for Development)

```bash
# Start infrastructure only
docker-compose -f docker-compose.dev.yml up postgres rabbitmq

# In separate terminals, run each API
cd src/Services/Booking.API
dotnet run --configuration Development

# The application will automatically use:
# - appsettings.json (base)
# - appsettings.Development.json (overrides)
# - Docker container services: postgres, rabbitmq
```

### Accessing Services

| Service | URL | Credentials |
|---------|-----|-------------|
| **Booking API Swagger** | http://localhost:5000/swagger | N/A |
| **RabbitMQ Management** | http://localhost:15672 | guest/guest |
| **PostgreSQL** | localhost:5432 | postgres/postgres |
| **Prometheus** | http://localhost:9090 | N/A |
| **Grafana** | http://localhost:3000 | admin/admin |

### Debugging

```bash
# View logs for a specific service
docker-compose -f docker-compose.dev.yml logs -f booking-api

# Execute commands in a container
docker-compose -f docker-compose.dev.yml exec postgres psql -U postgres

# View network
docker-compose -f docker-compose.dev.yml networks ls

# Rebuild after code changes
docker-compose -f docker-compose.dev.yml build --no-cache
docker-compose -f docker-compose.dev.yml up
```

---

## Production Deployment

### Prerequisites

- **Azure Account** with active subscription
- **Azure CLI** (latest version, authenticated)
- **kubectl** configured to your AKS cluster
- **AKS Cluster** running (v1.30+)
- **Azure Key Vault** created with secrets
- **Azure Container Registry** (optional, for private images)

### Step 1: Prepare Azure Resources

```powershell
# Set variables
$ResourceGroup = "saga-pattern-rg"
$ClusterName = "saga-pattern-aks"
$KeyVaultName = "saga-pattern-kv"
$TenantId = (az account show --query tenantId -o tsv)

# Verify resources exist
az group show --name $ResourceGroup
az aks show --name $ClusterName --resource-group $ResourceGroup
az keyvault show --name $KeyVaultName --resource-group $ResourceGroup

# Create secrets in Key Vault (if not already created)
az keyvault secret set --vault-name $KeyVaultName `
  --name saga-postgres-username --value postgres
az keyvault secret set --vault-name $KeyVaultName `
  --name saga-postgres-password --value "YourSecurePassword!"
az keyvault secret set --vault-name $KeyVaultName `
  --name saga-rabbitmq-username --value saga
az keyvault secret set --vault-name $KeyVaultName `
  --name saga-rabbitmq-password --value "YourSecurePassword2!"
```

### Step 2: Setup Workload Identity

```powershell
# This creates:
# - Managed identity for the application
# - OIDC federation with your AKS cluster
# - Kubernetes ServiceAccount with annotations
# - Key Vault access policies

.\k8s\setup-workload-identity.ps1 `
  -ResourceGroup $ResourceGroup `
  -ClusterName $ClusterName `
  -KeyVaultName $KeyVaultName `
  -TenantId $TenantId

# Verify setup
az identity show --name saga-workload-identity --resource-group $ResourceGroup
```

### Step 3: Deploy Application

```powershell
# This deploys:
# - All Kubernetes manifests
# - CSI SecretProvider configuration
# - API deployments with Workload Identity
# - Infrastructure (Postgres, RabbitMQ, Observability)

.\k8s\deploy-production-aks.ps1 `
  -Action deploy `
  -ResourceGroup $ResourceGroup `
  -ClusterName $ClusterName `
  -KeyVaultName $KeyVaultName `
  -EnableCSISecretProvider
```

### Step 4: Verify Deployment

```powershell
.\k8s\deploy-production-aks.ps1 `
  -Action verify `
  -ResourceGroup $ResourceGroup `
  -ClusterName $ClusterName

# Get access URLs
kubectl get svc -n saga-pattern --watch

# Watch pod startup
kubectl logs -f deployment/booking-api -n saga-pattern

# Verify Workload Identity is working
kubectl get pods -n saga-pattern -L azure.workload.identity/use
```

### Step 5: Test Application

```powershell
# Get Booking API LoadBalancer IP
$bookingIP = kubectl get svc booking-api -n saga-pattern `
  -o jsonpath='{.status.loadBalancer.ingress[0].ip}'

# Test API
Invoke-WebRequest -Uri "http://$bookingIP/api/bookings" -Method POST `
  -Body '{"customerId":"123","hotelId":"H1","carId":"C1"}' `
  -ContentType "application/json"
```

---

## Secret Management Strategy

### Local Development ("Let it fail fast")

```env
# .env file (DO NOT COMMIT)
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres  ← Simple for easy development
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest
```

### Production AKS ("Zero Trust")

```
Azure Key Vault
  ↓ (secret name: saga-postgres-password)
  ↓ (Workload Identity + OIDC)
CSI SecretProvider
  ↓ (mounts /mnt/secrets/postgres-password)
Kubernetes Secret (synced from CSI)
  ↓ (postgres-credentials at runtime)
Environment Variable Injection
  ↓
Application Code
```

---

## Updating Secrets

### In Development

Simply edit `.env` and restart services:

```bash
# Update .env with new values
nano .env

# Restart containers
docker-compose -f docker-compose.dev.yml restart
```

### In Production

```powershell
# Update secret in Key Vault
az keyvault secret set --vault-name saga-pattern-kv `
  --name saga-postgres-password `
  --value "NewSecurePassword!"

# Kubernetes secret automatically syncs (check CSI driver)
# Pods may need to restart to pick up changes

# Force pod restart if needed
kubectl rollout restart deployment/booking-api -n saga-pattern
```

---

## Troubleshooting

### "Docker daemon is not running"

```bash
# Start Docker
docker-compose -f docker-compose.dev.yml up
# OR start Docker Desktop application
```

### "Cannot reach localhost:5000"

```bash
# Check service is running
docker-compose -f docker-compose.dev.yml ps

# Check service logs
docker-compose -f docker-compose.dev.yml logs booking-api

# Verify port binding
docker-compose -f docker-compose.dev.yml port booking-api
```

### "Pod stuck in CrashLoopBackOff"

```powershell
# Check pod logs
kubectl logs <pod-name> -n saga-pattern --previous

# Check events
kubectl describe pod <pod-name> -n saga-pattern

# Verify secrets are present
kubectl get secrets -n saga-pattern
kubectl describe secret postgres-credentials -n saga-pattern
```

### "Workload Identity not working"

```powershell
# Verify ServiceAccount annotation
kubectl get sa saga-pattern-sa -n saga-pattern -o yaml

# Verify pod label
kubectl get pods -n saga-pattern --show-labels | grep workload

# Check OIDC issuer
az aks show --name saga-pattern-aks --resource-group saga-pattern-rg `
  --query "oidcIssuerProfile.issuerUrl"
```

---

## Next Steps

1. **Read** [ENVIRONMENT_CONFIGURATION.md](./k8s/ENVIRONMENT_CONFIGURATION.md) - Detailed environment setup
2. **Read** [PRODUCTION_SECURITY_GUIDE.md](./k8s/PRODUCTION_SECURITY_GUIDE.md) - Security best practices
3. **Review** [DEPLOYMENT_GUIDE.md](./k8s/DEPLOYMENT_GUIDE.md) - Complete deployment walkthrough
4. **Check** [../PROJECT_STRUCTURE.md](../PROJECT_STRUCTURE.md) - Code organization

---

## Getting Help

| Problem | Solution |
|---------|----------|
| Containers won't start | Check Docker is running: `docker ps` |
| Services can't communicate | Verify network: `docker network ls` |
| Secrets not syncing | Check CSI driver: `kubectl get pods -n kube-system \| grep csi` |
| API crashes | View logs: `kubectl logs <pod> -n saga-pattern` |
| Cannot access Key Vault | Verify Workload Identity setup |

---

**Happy developing!** 🚀

For detailed security information, see [PRODUCTION_SECURITY_GUIDE.md](./k8s/PRODUCTION_SECURITY_GUIDE.md).
