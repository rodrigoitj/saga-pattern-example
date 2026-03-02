# Kubernetes Manifests for Saga Pattern Application

This directory contains all the Kubernetes manifests needed to deploy the Saga Pattern microservices application to Azure Kubernetes Service (AKS).

## Directory Structure

```
k8s/
├── ACCESSING_SERVICES.md        # Guide for accessing services without custom domains
├── DEPLOYMENT_GUIDE.md          # Comprehensive deployment guide
├── README.md                     # This file
├── deploy-to-aks.ps1            # Automated deployment script
├── get-service-urls.ps1         # Helper to display all service access URLs
├── kustomization.yaml           # Kustomize configuration
├── namespace.yaml               # Namespace definition
├── secrets.yaml                 # Secret definitions
├── postgres-configmap.yaml      # PostgreSQL configuration
├── postgres.yaml                # PostgreSQL StatefulSet
├── rabbitmq.yaml                # RabbitMQ StatefulSet
├── otel-collector.yaml          # OpenTelemetry Collector
├── tempo.yaml                   # Tempo (Distributed Tracing)
├── prometheus.yaml              # Prometheus (Metrics)
├── grafana.yaml                 # Grafana (Visualization)
├── booking-api.yaml             # Booking API Deployment
├── flight-api.yaml              # Flight API Deployment
├── hotel-api.yaml               # Hotel API Deployment
├── car-api.yaml                 # Car API Deployment
├── hpa.yaml                     # Horizontal Pod Autoscalers
├── network-policies.yaml        # Network Security Policies
└── ingress.yaml                 # Ingress Configuration
```

## Quick Start

### Prerequisites

- Azure CLI installed and configured
- kubectl installed
- Docker installed
- Access to an Azure subscription

### Deployment Methods

#### Method 1: Automated Script (Recommended)

```powershell
# Make the script executable (if needed)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force

# Run the deployment script
.\deploy-to-aks.ps1 `
  -Action deploy `
  -Registry sagapatternacr `
  -Cluster saga-pattern-aks `
  -ResourceGroup saga-pattern-rg `
  -Location brazilsouth
```

#### Method 1B: Automated Script with Azure Key Vault (Recommended for AKS)

```powershell
.\deploy-to-aks.ps1 `
  -Action deploy `
  -Registry sagapatternacr `
  -Cluster saga-pattern-aks `
  -ResourceGroup saga-pattern-rg `
  -Location brazilsouth `
  -UseKeyVault `
  -KeyVaultName saga-pattern-kv
```

Required Key Vault secret names (defaults used by the script):
- `saga-postgres-username`
- `saga-postgres-password`
- `saga-rabbitmq-username`
- `saga-rabbitmq-password`

You can override secret names via:
- `-PostgresUsernameSecretName`
- `-PostgresPasswordSecretName`
- `-RabbitMqUsernameSecretName`
- `-RabbitMqPasswordSecretName`

#### Method 2: Manual Deployment with kubectl

```powershell
# Create Azure resources
.\deploy-to-aks.ps1 -Action deploy -Registry sagapatternacr

# Or manually:
# 1. Create resource group
az group create --name saga-pattern-rg --location brazilsouth

# 2. Create container registry
az acr create --name sagapatternacr --resource-group saga-pattern-rg --sku Standard

# 3. Create AKS cluster
az aks create --name saga-pattern-aks --resource-group saga-pattern-rg --node-count 3 --attach-acr sagapatternacr --location brazilsouth

# 4. Get credentials
az aks get-credentials --name saga-pattern-aks --resource-group saga-pattern-rg

# 5. Deploy with Kustomize
kubectl apply -k .
```

### Post-Deployment: Get Service URLs

After deployment completes, use the helper script to see all access URLs:

```powershell
.\get-service-urls.ps1
```

This shows:
- Public LoadBalancer IPs for Booking API and Grafana
- Port-forward commands for internal services
- Ingress URLs (if configured)
- Useful kubectl commands

## Manifest Files Overview

### Core Infrastructure

| File | Purpose | Type |
|------|---------|------|
| `namespace.yaml` | Create saga-pattern namespace | Namespace |
| `secrets.yaml` | Database and RabbitMQ credentials | Secret |
| `postgres-configmap.yaml` | PostgreSQL initialization | ConfigMap |

### Databases and Message Broker

| File | Purpose | Replicas | Storage |
|------|---------|----------|---------|
| `postgres.yaml` | PostgreSQL database | 1 | 10Gi |
| `rabbitmq.yaml` | RabbitMQ message broker | 1 | 5Gi |

### Observability Stack

| File | Purpose | Replicas | Storage |
|------|---------|----------|---------|
| `otel-collector.yaml` | OpenTelemetry Collector | 1 | None |
| `tempo.yaml` | Trace storage (Grafana Tempo) | 1 | 5Gi |
| `prometheus.yaml` | Metrics collection | 1 | 5Gi |
| `grafana.yaml` | Dashboard visualization | 1 | 2Gi |

### Microservices

| File | Purpose | Replicas | Initial |
|------|---------|----------|---------|
| `booking-api.yaml` | Booking service | 2 | 2 |
| `flight-api.yaml` | Flight service | 1 | 1 |
| `hotel-api.yaml` | Hotel service | 1 | 1 |
| `car-api.yaml` | Car rental service | 1 | 1 |

### Advanced Configuration

| File | Purpose | Type |
|------|---------|------|
| `hpa.yaml` | Horizontal Pod Autoscaling | HorizontalPodAutoscaler |
| `network-policies.yaml` | Network security rules | NetworkPolicy |
| `ingress.yaml` | HTTP routing | Ingress |

## Accessing Services

**No custom domain required!** Services use LoadBalancer IPs by default.

### Quick Access (after deployment)

```powershell
# Get Booking API IP
kubectl get svc booking-api -n saga-pattern -o jsonpath='{.status.loadBalancer.ingress[0].ip}'

# Get Grafana IP
kubectl get svc grafana -n saga-pattern -o jsonpath='{.status.loadBalancer.ingress[0].ip}'
```

### All Access Options

See **[ACCESSING_SERVICES.md](ACCESSING_SERVICES.md)** for detailed guide including:
- ✓ Direct LoadBalancer IPs (default, no domain needed)
- ✓ Ingress with path-based routing
- ✓ Free DNS via nip.io
- ✓ Azure-provided FQDN
- ✓ Port forwarding for local development
- ✓ Custom domain setup (when ready)

### Port Forwarding Examples

```powershell
# Grafana
kubectl port-forward -n saga-pattern svc/grafana 3000:80
# http://localhost:3000 (admin/admin)

# RabbitMQ Management
kubectl port-forward -n saga-pattern svc/rabbitmq 15672:15672
# http://localhost:15672 (guest/guest)

# Prometheus
kubectl port-forward -n saga-pattern svc/prometheus 9090:9090
# http://localhost:9090
```

## Deployment Operations

### Upgrade Services

```powershell
# Using script
.\deploy-to-aks.ps1 -Action upgrade

# Manual upgrade
kubectl set image deployment/booking-api `
  booking-api=<registry>/booking-api:v2 `
  -n saga-pattern

# Monitor rollout
kubectl rollout status deployment/booking-api -n saga-pattern
```

### Rollback to Previous Version

```powershell
# Using script
.\deploy-to-aks.ps1 -Action rollback

# Manual rollback
kubectl rollout undo deployment/booking-api -n saga-pattern
```

### Scale Deployments

```powershell
# Manual scaling
kubectl scale deployment booking-api -n saga-pattern --replicas=5

# Check HPA status
kubectl get hpa -n saga-pattern
kubectl describe hpa booking-api-hpa -n saga-pattern
```

## Monitoring and Troubleshooting

### View Pod Logs

```powershell
# View logs from a single pod
kubectl logs -n saga-pattern pod/booking-api-xyz

# Stream logs
kubectl logs -n saga-pattern -f deployment/booking-api

# View logs from all pods of a deployment
kubectl logs -n saga-pattern -l app=booking-api --all-containers=true
```

### Debug Pod Issues

```powershell
# Describe a pod to see events
kubectl describe pod <pod-name> -n saga-pattern

# Execute commands in a pod
kubectl exec -it <pod-name> -n saga-pattern -- /bin/sh

# Port-forward to debug
kubectl port-forward -n saga-pattern pod/booking-api-xyz 5000:8080
```

### Check Metrics

```powershell
# View resource usage
kubectl top pods -n saga-pattern
kubectl top nodes

# View events
kubectl get events -n saga-pattern --sort-by='.lastTimestamp'
```

## Network Policies

Network policies implement zero-trust security. They restrict all traffic by default and only allow necessary communication paths.

To enable network policies:

```powershell
kubectl apply -f network-policies.yaml
```

To view active policies:

```powershell
kubectl get networkpolicies -n saga-pattern
kubectl describe networkpolicy allow-booking-to-rabbitmq -n saga-pattern
```

## Updating Configuration

### Update Environment Variables

Edit the corresponding deployment file and update the `env` section:

```powershell
kubectl set env deployment/booking-api KEY=VALUE -n saga-pattern
```

### Update ConfigMaps

```powershell
# Edit ConfigMap
kubectl edit configmap otel-collector-config -n saga-pattern

# Or recreate from file
kubectl create configmap otel-collector-config \
  --from-file=config.yaml=./otel-collector-config.yaml \
  -n saga-pattern --dry-run=client -o yaml | kubectl apply -f -
```

### Update Secrets

```powershell
# Create or update secrets
kubectl create secret generic postgres-credentials \
  --from-literal=username=postgres \
  --from-literal=password=<new-password> \
  -n saga-pattern --dry-run=client -o yaml | kubectl apply -f -
```

## Production Recommendations

1. **Database**: Use Azure Database for PostgreSQL instead of in-cluster PostgreSQL
2. **Message Broker**: Use Azure Service Bus instead of RabbitMQ
3. **Secrets**: Use Azure Key Vault instead of Kubernetes secrets
4. **Monitoring**: Integrate with Azure Monitor and Application Insights
5. **Networking**: Use Application Gateway for ingress
6. **Security**: Enable Pod Security Policies and RBAC
7. **High Availability**: Deploy across multiple availability zones

See [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md) for comprehensive production recommendations.

## Azure Key Vault Notes

- `secrets.yaml` is a local/dev fallback.
- When `-UseKeyVault` is enabled, deployment script reads secrets from Azure Key Vault and creates/updates:
  - `postgres-credentials`
  - `rabbitmq-credentials`
- In Key Vault mode, static `secrets.yaml` is skipped during apply.

## Cleanup

### Delete All Resources

```powershell
# Using script
.\deploy-to-aks.ps1 -Action cleanup

# Manual deletion
kubectl delete namespace saga-pattern
az aks delete --name saga-pattern-aks --resource-group saga-pattern-rg --yes
az acr delete --name sagapatternacr --yes
az group delete --name saga-pattern-rg --yes
```

## Image Registry Configuration

Update the image registry in deployment manifests:

```powershell
# Before deployment, replace <your-registry> with your ACR name
# Example: sagapatternacr.azurecr.io

# Using sed/PowerShell
Get-ChildItem *.yaml | ForEach-Object {
    (Get-Content $_) -replace '<your-registry>', 'sagapatternacr.azurecr.io' | Set-Content $_
}
```

## Storage Classes

The manifests use the `managed-csi` storage class for Azure managed disks. If using a different storage class, update the `storageClassName` field in PVCs.

## Kustomize Customization

To create environment-specific overlays:

```
k8s/
├── kustomization.yaml          # Base configuration
└── overlays/
    ├── dev/
    │   ├── kustomization.yaml
    │   └── patches/
    └── prod/
        ├── kustomization.yaml
        └── patches/
```

Deploy with overlays:

```powershell
kubectl apply -k overlays/prod
```

## Additional Resources

- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [AKS Documentation](https://docs.microsoft.com/en-us/azure/aks/)
- [Kustomize Documentation](https://kustomize.io/)
- [OpenTelemetry](https://opentelemetry.io/)
- [Prometheus](https://prometheus.io/)
- [Grafana](https://grafana.com/)

## Support

For deployment issues, see the [DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md#troubleshooting) troubleshooting section or check the logs using kubectl commands.
