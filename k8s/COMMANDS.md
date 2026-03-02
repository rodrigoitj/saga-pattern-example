
# Commands

```bash
az provider register --namespace Microsoft.ContainerRegistry
az provider register --namespace Microsoft.ContainerService

az keyvault create `
  --name saga-pattern-kv `
  --resource-group saga-pattern-rg `
  --location brazilsouth

az role assignment create --role "Key Vault Secrets Officer" --assignee cf4fb317-ccd3-41f8-8211-5641d3f46097 --scope /subscriptions/ba8cd560-2282-4169-8a26-59563f126c55/resourceGroups/saga-pattern-rg/providers/Microsoft.KeyVault/vaults/saga-pattern-kv

az keyvault secret set --vault-name saga-pattern-kv --name saga-postgres-username --value postgres
az keyvault secret set --vault-name saga-pattern-kv --name saga-postgres-password --value postgres
az keyvault secret set --vault-name saga-pattern-kv --name saga-rabbitmq-username --value guest
az keyvault secret set --vault-name saga-pattern-kv --name saga-rabbitmq-password --value guest


# Show key value
az keyvault secret show --vault-name saga-pattern-kv --name saga-postgres-username


docker tag booking-api:latest "sagapatternacr.azurecr.io/booking-api:latest"
docker tag flight-api:latest "sagapatternacr.azurecr.io/flight-api:latest"
docker tag hotel-api:latest "sagapatternacr.azurecr.io/hotel-api:latest"
docker tag car-api:latest "sagapatternacr.azurecr.io/car-api:latest"

docker push "sagapatternacr.azurecr.io/booking-api:latest"
docker push "sagapatternacr.azurecr.io/flight-api:latest"
docker push "sagapatternacr.azurecr.io/hotel-api:latest"
docker push "sagapatternacr.azurecr.io/car-api:latest"



.\k8s\deploy-to-aks.ps1 `
  -Action deploy `
  -Registry sagapatternacr `
  -Cluster saga-pattern-aks `
  -ResourceGroup saga-pattern-rg `
  -Location brazilsouth `
  -UseKeyVault `
  -KeyVaultName saga-pattern-kv
```

# AKS (Azure Kubernetes Service)

```bash
# Initial setup with Workload Identity
.\deploy-aks.ps1 -Action setup-identity -ResourceGroup saga-pattern-rg -ClusterName saga-pattern-aks

# Deploy application (production overlay - ACR images)
.\deploy-aks.ps1 -Action deploy -Registry sagapatternacr -EnableCSISecretProvider

# Or manually with kubectl + kustomize
kubectl apply -k k8s/overlays/production

# Preview production configuration
kubectl kustomize k8s/overlays/production

# Upgrade with new images
.\deploy-aks.ps1 -Action upgrade -Registry sagapatternacr -Build

# Verify and rollback
.\deploy-aks.ps1 -Action verify
.\deploy-aks.ps1 -Action rollback

# Cleanup resources
.\deploy-aks.ps1 -Action cleanup
```

# Local Kubernetes (Docker Desktop, Minikube, Kind)

```bash
# Deploy to local cluster (uses local overlay - local images)
.\deploy-local.ps1 -Action deploy

# Or manually with kubectl + kustomize
kubectl apply -k k8s/overlays/local

# Preview local configuration
kubectl kustomize k8s/overlays/local

# Build images only
.\deploy-local.ps1 -Action build

# Verify deployment
.\deploy-local.ps1 -Action verify

# Rollback to previous version
.\deploy-local.ps1 -Action rollback

# Cleanup namespace
.\deploy-local.ps1 -Action cleanup

# With custom credentials
.\deploy-local.ps1 -Action deploy `
  -PostgresUsername myuser `
  -PostgresPassword mypassword `
  -RabbitMqUsername myguest `
  -RabbitMqPassword mypassword

# Access services via port-forward
kubectl port-forward -n saga-pattern svc/booking-api 5001:80

# Access RabbitMQ Management UI (port 15672)
# Default credentials: guest / guest
kubectl port-forward -n saga-pattern svc/rabbitmq 15672:15672

# Access Grafana (port 3000)
# Default credentials: admin / admin
kubectl port-forward -n saga-pattern svc/grafana 3000:80
```