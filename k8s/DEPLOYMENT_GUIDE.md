# Kubernetes Deployment Guide for AKS

This guide provides comprehensive instructions for deploying the Saga Pattern microservices application to Azure Kubernetes Service (AKS).

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Prerequisites](#prerequisites)
3. [Azure Setup](#azure-setup)
4. [Building and Pushing Container Images](#building-and-pushing-container-images)
5. [Deploying to AKS](#deploying-to-aks)
6. [Verification and Testing](#verification-and-testing)
7. [Accessing Services](#accessing-services)
8. [Scaling and Performance](#scaling-and-performance)
9. [Monitoring and Observability](#monitoring-and-observability)
10. [Troubleshooting](#troubleshooting)
11. [Production Recommendations](#production-recommendations)
12. [Cleanup](#cleanup)

---

## Architecture Overview

The deployment consists of:

- **Microservices**: Booking.API, Flight.API, Hotel.API, Car.API
- **Infrastructure**: PostgreSQL, RabbitMQ
- **Observability Stack**: OpenTelemetry Collector, Prometheus, Tempo, Grafana
- **Kubernetes Components**: Namespaces, Deployments, StatefulSets, Services, ConfigMaps, Secrets, PersistentVolumes

```
┌─────────────────────────────────────────────────────┐
│         Azure Kubernetes Service (AKS)              │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ┌──────────────────────────────────────────────┐  │
│  │   saga-pattern Namespace                     │  │
│  ├──────────────────────────────────────────────┤  │
│  │                                              │  │
│  │  Bookings API ──┐                            │  │
│  │                 ├──→ RabbitMQ ──┬──→ Flight  │  │
│  │                 │                │           │  │
│  │                 │                ├──→ Hotel  │  │
│  │                 │                │           │  │
│  │                 └──→ PostgreSQL   └──→ Car    │  │
│  │                                              │  │
│  │  OTEL Collector ──→ Prometheus ──→ Grafana  │  │
│  │         ↓                                    │  │
│  │      Tempo (Tracing)                        │  │
│  │                                              │  │
│  └──────────────────────────────────────────────┘  │
│                                                     │
└─────────────────────────────────────────────────────┘
```

---

## Prerequisites

### Local Tools

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/)
- [kubectl](https://kubernetes.io/docs/tasks/tools/)
- [Docker](https://www.docker.com/get-started)
- [Kustomize](https://kustomize.io/) (optional, for custom deployments)
- PowerShell 5.1 or later

### Azure Subscription

- An active Azure subscription
- Sufficient permissions to create AKS clusters and container registries

### Container Registry

- Azure Container Registry (ACR) for storing Docker images

---

## Azure Setup

### Step 1: Create Resource Group

```powershell
$resourceGroupName = "saga-pattern-rg"
$location = "eastus"

az group create `
  --name $resourceGroupName `
  --location $location
```

### Step 2: Create Azure Container Registry

```powershell
$registryName = "sagapatternacr"

az acr create `
  --resource-group $resourceGroupName `
  --name $registryName `
  --sku Standard
```

### Step 3: Create AKS Cluster

```powershell
$clusterName = "saga-pattern-aks"
$nodeCount = 3
$vmSize = "Standard_D2s_v3"

az aks create `
  --resource-group $resourceGroupName `
  --name $clusterName `
  --node-count $nodeCount `
  --vm-set-type VirtualMachineScaleSets `
  --load-balancer-sku standard `
  --enable-managed-identity `
  --network-plugin azure `
  --network-policy azure `
  --node-vm-size $vmSize `
  --attach-acr $registryName `
  --enable-cluster-autoscaling `
  --min-count 3 `
  --max-count 10
```

### Step 4: Get Cluster Credentials

```powershell
az aks get-credentials `
  --resource-group $resourceGroupName `
  --name $clusterName `
  --overwrite-existing
```

### Step 5: Verify Cluster Access

```powershell
kubectl cluster-info
kubectl get nodes
```

### Step 6: Create and Populate Azure Key Vault (Recommended)

```powershell
$keyVaultName = "saga-pattern-kv"

az keyvault create `
  --name $keyVaultName `
  --resource-group $resourceGroupName `
  --location $location

az keyvault secret set --vault-name $keyVaultName --name saga-postgres-username --value postgres
az keyvault secret set --vault-name $keyVaultName --name saga-postgres-password --value <strong-password>
az keyvault secret set --vault-name $keyVaultName --name saga-rabbitmq-username --value guest
az keyvault secret set --vault-name $keyVaultName --name saga-rabbitmq-password --value <strong-password>
```

---

## Building and Pushing Container Images

### Step 1: Configure Docker Login

```powershell
$registryLoginServer = "$registryName.azurecr.io"

az acr login --name $registryName
```

### Step 2: Build and Push Images

#### Using Docker Compose

```powershell
# From the project root directory
docker-compose -f docker-compose.yml build

# Tag images
docker tag booking-api:latest "$registryLoginServer/booking-api:latest"
docker tag flight-api:latest "$registryLoginServer/flight-api:latest"
docker tag hotel-api:latest "$registryLoginServer/hotel-api:latest"
docker tag car-api:latest "$registryLoginServer/car-api:latest"

# Push to ACR
docker push "$registryLoginServer/booking-api:latest"
docker push "$registryLoginServer/flight-api:latest"
docker push "$registryLoginServer/hotel-api:latest"
docker push "$registryLoginServer/car-api:latest"
```

#### Using Azure CLI

```powershell
az acr build `
  --registry $registryName `
  --image booking-api:latest `
  --file src/Services/Booking.API/Dockerfile .

az acr build `
  --registry $registryName `
  --image flight-api:latest `
  --file src/Services/Flight.API/Dockerfile .

az acr build `
  --registry $registryName `
  --image hotel-api:latest `
  --file src/Services/Hotel.API/Dockerfile .

az acr build `
  --registry $registryName `
  --image car-api:latest `
  --file src/Services/Car.API/Dockerfile .
```

### Step 3: Verify Images in Registry

```powershell
az acr repository list --name $registryName
```

---

## Deploying to AKS

### Step 1: Update Image References

Before deploying, update the image references in the manifests:

```powershell
# Update booking-api.yaml
(Get-Content k8s/booking-api.yaml) -replace '<your-registry>', $registryLoginServer | Set-Content k8s/booking-api.yaml

# Update flight-api.yaml
(Get-Content k8s/flight-api.yaml) -replace '<your-registry>', $registryLoginServer | Set-Content k8s/flight-api.yaml

# Update hotel-api.yaml
(Get-Content k8s/hotel-api.yaml) -replace '<your-registry>', $registryLoginServer | Set-Content k8s/hotel-api.yaml

# Update car-api.yaml
(Get-Content k8s/car-api.yaml) -replace '<your-registry>', $registryLoginServer | Set-Content k8s/car-api.yaml
```

### Step 2: Deploy Using Kustomize

```powershell
# Deploy all manifests
kubectl apply -k k8s/

# Or download and use kustomize explicitly
kustomize build k8s/ | kubectl apply -f -
```

### Step 2B: Deploy with Azure Key Vault-backed secrets

Use the deployment script so secrets are pulled from Key Vault and synced to Kubernetes:

```powershell
.\k8s\deploy-to-aks.ps1 `
  -Action deploy `
  -Registry $registryName `
  -Cluster $clusterName `
  -ResourceGroup $resourceGroupName `
  -Location $location `
  -UseKeyVault `
  -KeyVaultName $keyVaultName
```

In this mode:
- `postgres-credentials` and `rabbitmq-credentials` are created from Key Vault values.
- Static `k8s/secrets.yaml` is skipped.

### Step 3: Monitor Deployment

```powershell
# Watch deployment status
kubectl rollout status deployment/booking-api -n saga-pattern
kubectl rollout status deployment/flight-api -n saga-pattern
kubectl rollout status deployment/hotel-api -n saga-pattern
kubectl rollout status deployment/car-api -n saga-pattern
kubectl rollout status statefulset/postgres -n saga-pattern
kubectl rollout status statefulset/rabbitmq -n saga-pattern

# View all resources
kubectl get all -n saga-pattern

# View pod status
kubectl get pods -n saga-pattern -o wide

# View logs for a pod
kubectl logs -n saga-pattern -l app=booking-api --tail=100
```

---

## Verification and Testing

### Step 1: Verify All Pods Are Running

```powershell
kubectl get pods -n saga-pattern

# Expected output should show all pods in Running state
```

### Step 2: Check Service Status

```powershell
kubectl get svc -n saga-pattern

# Check specific service
kubectl describe svc booking-api -n saga-pattern
```

### Step 3: Test Database Connectivity

```powershell
# Port-forward to postgres
kubectl port-forward -n saga-pattern svc/postgres 5432:5432 &

# In another terminal, connect with psql or a database client
# Connection string: postgres://postgres:postgres@localhost:5432/saga_db
```

### Step 4: Test RabbitMQ

```powershell
# Port-forward to RabbitMQ management console
kubectl port-forward -n saga-pattern svc/rabbitmq 15672:15672 &

# Access RabbitMQ Management UI
# URL: http://localhost:15672
# Credentials: guest / guest
```

### Step 5: Test Booking API

```powershell
# Get the LoadBalancer IP
kubectl get svc booking-api -n saga-pattern

# Wait for EXTERNAL-IP to be assigned (may take a few minutes)
# Then test the API
$bookingApiUrl = "http://<EXTERNAL-IP>"

# Create a booking
$bookingPayload = @{
    userId = "550e8400-e29b-41d4-a716-446655440000"
    checkInDate = "2026-03-15T00:00:00Z"
    checkOutDate = "2026-03-20T00:00:00Z"
    includeFlights = $true
    includeHotel = $true
    includeCar = $true
} | ConvertTo-Json

Invoke-RestMethod `
  -Uri "$bookingApiUrl/api/bookings" `
  -Method Post `
  -Headers @{"Content-Type"="application/json"} `
  -Body $bookingPayload

# Get booking details
Invoke-RestMethod -Uri "$bookingApiUrl/api/bookings/{bookingId}"
```

---

## Accessing Services

**See [ACCESSING_SERVICES.md](ACCESSING_SERVICES.md) for complete guide on all access methods.**

### Quick Access (No Custom Domain Required)

Use the provided helper script to see all service URLs:

```powershell
.\k8s\get-service-urls.ps1
```

Or manually get LoadBalancer IPs:

```powershell
# Booking API (main HTTP API)
kubectl get svc booking-api -n saga-pattern -o jsonpath='{.status.loadBalancer.ingress[0].ip}'

# Grafana (dashboards)
kubectl get svc grafana -n saga-pattern -o jsonpath='{.status.loadBalancer.ingress[0].ip}'
```

### Port Forwarding for Internal Services

```powershell
# Grafana (alternative to LoadBalancer)
kubectl port-forward -n saga-pattern svc/grafana 3000:80
# Access: http://localhost:3000 (admin/admin)

# Prometheus
kubectl port-forward -n saga-pattern svc/prometheus 9090:9090
# Access: http://localhost:9090

# Tempo (Distributed Tracing)
kubectl port-forward -n saga-pattern svc/tempo 3200:3200
# Use in Grafana data sources

# RabbitMQ Management
kubectl port-forward -n saga-pattern svc/rabbitmq 15672:15672
# Access: http://localhost:15672 (guest/guest)
```

### Custom Domain Setup

For production with registered domains, see [ACCESSING_SERVICES.md](ACCESSING_SERVICES.md#setting-up-custom-domains-future).

---

## Scaling and Performance

### Horizontal Pod Autoscaling

Create an HPA for the booking API:

```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: booking-api-hpa
  namespace: saga-pattern
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: booking-api
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

Apply HPA:

```powershell
kubectl apply -f k8s/hpa.yaml
```

### Updating Replica Counts

```powershell
# Scale booking-api to 5 replicas
kubectl scale deployment booking-api -n saga-pattern --replicas=5

# Scale flight-api to 3 replicas
kubectl scale deployment flight-api -n saga-pattern --replicas=3
```

---

## Monitoring and Observability

### View Metrics

1. **Port-forward to Grafana**:
   ```powershell
   kubectl port-forward -n saga-pattern svc/grafana 3000:80
   ```

2. **Access Grafana**: http://localhost:3000
   - Default credentials: admin / admin

3. **Add Prometheus Datasource**:
   - URL: http://prometheus:9090
   - Access: Server

4. **Import Dashboards**:
   - Use the dashboard files from `observability/dashboards/`

### View Distributed Traces

1. **Open Grafana** and navigate to Explore
2. **Select Tempo** as the datasource
3. **Search for traces** by service name or trace ID

### View Logs

```powershell
# View logs for a service
kubectl logs -n saga-pattern -l app=booking-api --all-containers=true

# Follow logs in real-time
kubectl logs -n saga-pattern -l app=booking-api -f

# View logs from previous container
kubectl logs -n saga-pattern -l app=booking-api --previous
```

---

## Troubleshooting

### Pod Not Starting

```powershell
# Describe the pod to see events
kubectl describe pod <pod-name> -n saga-pattern

# Check logs
kubectl logs <pod-name> -n saga-pattern

# Check image availability
az acr repository show --name $registryName --image booking-api:latest
```

### Database Connection Issues

```powershell
# Check if PostgreSQL is running
kubectl get pod -n saga-pattern | grep postgres

# Check PostgreSQL logs
kubectl logs -n saga-pattern postgres-0

# Test database connectivity
kubectl exec -it postgres-0 -n saga-pattern -- psql -U postgres -c "\l"
```

### RabbitMQ Connection Issues

```powershell
# Check RabbitMQ status
kubectl exec -it rabbitmq-0 -n saga-pattern -- rabbitmq-diagnostics status

# View RabbitMQ logs
kubectl logs -n saga-pattern rabbitmq-0
```

### Service Discovery Issues

```powershell
# Test DNS resolution from a pod
kubectl run -it --rm debug --image=busybox --restart=Never -n saga-pattern -- nslookup postgres

# Check service endpoints
kubectl get endpoints -n saga-pattern
```

### Persistent Volume Issues

```powershell
# List all PVs and PVCs
kubectl get pv,pvc -n saga-pattern

# Describe a PVC
kubectl describe pvc <pvc-name> -n saga-pattern

# Check disk usage
kubectl exec -it postgres-0 -n saga-pattern -- df -h
```

---

## Production Recommendations

### Security

1. **Use Azure Key Vault** for secrets:
   ```powershell
   az keyvault create --name saga-pattern-kv --resource-group $resourceGroupName
   az keyvault secret set --vault-name saga-pattern-kv --name db-password --value <strong-password>
   ```

  For this repository's AKS script, set:
  - `saga-postgres-username`
  - `saga-postgres-password`
  - `saga-rabbitmq-username`
  - `saga-rabbitmq-password`

2. **Enable Network Policies**:
   - Restrict traffic between services using NetworkPolicy resources

3. **Use RBAC**:
   - Create service accounts with minimal permissions
   - Use Kubernetes RBAC for access control

4. **Enable Pod Security Policies**:
   - Restrict privileged containers
   - Enforce security standards

### Database

1. **Use Azure Database for PostgreSQL** instead of running PostgreSQL in Kubernetes:
   ```powershell
   az postgres server create \
     --resource-group $resourceGroupName \
     --name saga-db-server \
     --location $location \
     --admin-user postgres \
     --admin-password <strong-password> \
     --sku-name B_Gen5_2 \
     --storage-size 51200
   ```

2. **Enable database backups and geo-replication**

3. **Use Azure Database Firewall** rules

### Message Broker

1. **Use Azure Service Bus** instead of RabbitMQ:
   ```powershell
   az servicebus namespace create \
     --resource-group $resourceGroupName \
     --name saga-pattern-ns \
     --location $location \
     --sku Standard
   ```

2. **Enable geo-replication and disaster recovery**

### Monitoring and Logging

1. **Use Azure Monitor** for comprehensive monitoring:
   ```powershell
   az monitor diagnostic-settings create \
     --name saga-logs \
     --resource /subscriptions/{subscription-id}/resourceGroups/$resourceGroupName \
     --logs '[{"category":"kube-audit","enabled":true}]' \
     --metrics '[{"category":"AllMetrics","enabled":true}]' \
     --workspace /subscriptions/{subscription-id}/resourceGroups/$resourceGroupName/providers/microsoft.operationalinsights/workspaces/saga-logs
   ```

2. **Enable Azure Application Insights** for application-level tracing

3. **Configure alerting rules** in Prometheus

### High Availability

1. **Deploy multiple replicas** of each service
2. **Use Pod Disruption Budgets** to maintain availability during updates
3. **Configure cluster autoscaling** for dynamic capacity
4. **Use multiple AZs** for zone redundancy
5. **Set up proper resource requests and limits**

### Infrastructure Upgrades

1. **Use node pools** to manage different workload types
2. **Enable AKS auto-upgrades**
3. **Plan maintenance windows** for updates
4. **Use Blue-Green deployments** for zero-downtime updates

---

## Cleanup

### Delete All Kubernetes Resources

```powershell
# Delete the namespace (cascades to all resources)
kubectl delete namespace saga-pattern
```

### Delete AKS Cluster

```powershell
az aks delete `
  --resource-group $resourceGroupName `
  --name $clusterName `
  --yes
```

### Delete Container Registry

```powershell
az acr delete `
  --name $registryName `
  --resource-group $resourceGroupName
```

### Delete Resource Group

```powershell
az group delete `
  --name $resourceGroupName `
  --yes
```

---

## Additional Resources

- [AKS Documentation](https://docs.microsoft.com/en-us/azure/aks/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [Azure Container Registry](https://docs.microsoft.com/en-us/azure/container-registry/)
- [Kustomize](https://kustomize.io/)
- [Helm Charts](https://helm.sh/)

---

## Support and Troubleshooting

For issues or questions:

1. Check the [AKS Troubleshooting Guide](https://docs.microsoft.com/en-us/azure/aks/troubleshooting)
2. Review Kubernetes logs: `kubectl logs -n saga-pattern <pod-name>`
3. Check Azure resources: `az resource list --resource-group $resourceGroupName`
4. Enable diagnostic logging for the AKS cluster
