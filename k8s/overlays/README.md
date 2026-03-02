# Kustomize Overlays

This directory contains environment-specific Kubernetes configurations using Kustomize overlays.

## Structure

```
overlays/
├── local/
│   └── kustomization.yaml    # Local development configuration
└── production/
    └── kustomization.yaml    # Production AKS configuration
```

## Local Overlay

The local overlay (`overlays/local/`) is designed for local Kubernetes clusters (Docker Desktop, Minikube, Kind):

**Key Features:**
- Uses local Docker images (e.g., `booking-api:latest` instead of `sagapatternacr.azurecr.io/booking-api:latest`)
- Sets `imagePullPolicy: Never` to use locally built images
- Generates secrets directly in Kubernetes (postgres, rabbitmq credentials)
- Single replica for resource efficiency
- No Azure-specific resources

**Usage:**
```bash
# Deploy using the script
.\deploy-local.ps1 -Action deploy

# Or manually with kubectl
kubectl apply -k k8s/overlays/local
```

## Production Overlay

The production overlay (`overlays/production/`) is designed for Azure Kubernetes Service (AKS):

**Key Features:**
- Uses Azure Container Registry images (`sagapatternacr.azurecr.io/*`)
- Increased replicas (3 per service) for high availability
- Includes HPA (Horizontal Pod Autoscaler) and network policies
- Includes SecretProviderClass for Azure Key Vault integration
- Production-grade resource limits

**Usage:**
```bash
# Deploy using the script
.\deploy-aks.ps1 -Action deploy -Registry sagapatternacr

# Or manually with kubectl
kubectl apply -k k8s/overlays/production
```

## How It Works

Kustomize overlays work by:

1. **Base Configuration**: Base YAML files in `k8s/` contain common configuration
2. **Environment Overlays**: Overlays patch specific values for each environment
3. **Image Substitution**: The `images` section replaces container image references
4. **Patches**: JSON patches modify specific fields (replicas, imagePullPolicy, etc.)
5. **Secret Generation**: Overlays can generate secrets or reference external sources

## Benefits

- **Single Source of Truth**: Base manifests remain unchanged
- **Environment Isolation**: Local and production configs don't interfere
- **Easy Testing**: Test production configs locally with `kustomize build`
- **No Duplication**: Shared configuration stays in base files
- **GitOps Ready**: Each overlay can be deployed independently

## Testing Overlays

Preview what will be deployed without actually applying:

```bash
# Preview local deployment
kubectl kustomize k8s/overlays/local

# Preview production deployment
kubectl kustomize k8s/overlays/production
```

## Adding a New Environment

To add a new environment (e.g., staging):

1. Create `overlays/staging/kustomization.yaml`
2. Reference base resources
3. Add environment-specific patches
4. Update deployment scripts to use the new overlay
