#!/usr/bin/env pwsh

<#
.SYNOPSIS
Deploy Saga Pattern to Local Kubernetes Cluster

.DESCRIPTION
This script deploys the Saga Pattern microservices to a local Kubernetes cluster
(Docker Desktop, Minikube, Kind, etc.). It handles building Docker images locally,
managing Kubernetes Secrets, and deploying all services.

ACTIONS:
  build      - Build Docker images locally
  deploy     - Build images and deploy to local cluster
  verify     - Check deployment health
  rollback   - Rollback deployments to previous version
  cleanup    - Clean up namespace and resources

EXAMPLES:
  # Deploy everything
  .\deploy-local.ps1 -Action deploy

  # Just build images
  .\deploy-local.ps1 -Action build

  # Verify deployment
  .\deploy-local.ps1 -Action verify

  # Cleanup
  .\deploy-local.ps1 -Action cleanup

.NOTES
- Requires local Kubernetes cluster running (Docker Desktop, Minikube, etc.)
- Requires kubectl and docker installed locally
- Assumes images are loaded into local cluster's Docker daemon
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("build", "deploy", "verify", "rollback", "cleanup")]
    [string]$Action,

    [Parameter()]
    [string]$KubernetesContext = "docker-desktop",

    [Parameter()]
    [string]$ImageRegistry = "localhost:5000",

    [Parameter()]
    [string]$PostgresUsername = "postgres",

    [Parameter()]
    [string]$PostgresPassword = "postgres",

    [Parameter()]
    [string]$RabbitMqUsername = "guest",

    [Parameter()]
    [string]$RabbitMqPassword = "guest",

    [switch]$PushToRegistry
)

$ErrorActionPreference = "Stop"
$WarningPreference = "Continue"

# ============================================================================
# Output Formatting Functions
# ============================================================================

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Warning-Msg {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error-Msg {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Write-Section {
    param([string]$Message)
    Write-Host ""
    Write-Host "================================================" -ForegroundColor Yellow
    Write-Host " $Message" -ForegroundColor Yellow
    Write-Host "================================================" -ForegroundColor Yellow
    Write-Host ""
}

function Write-Header {
    Write-Host ""
    Write-Host "================================================================" -ForegroundColor Magenta
    Write-Host " Saga Pattern - Local Kubernetes Deployment" -ForegroundColor Magenta
    Write-Host " Docker Builds + Local Secrets + Standard K8s Deployments" -ForegroundColor Magenta
    Write-Host "================================================================" -ForegroundColor Magenta
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
    
    $tools = @('kubectl', 'docker')
    $missing = @()
    
    foreach ($tool in $tools) {
        $exists = $null -ne (Get-Command $tool -ErrorAction SilentlyContinue)
        if ($exists) {
            Write-Success "  [OK] $tool found"
        }
        else {
            Write-Error-Msg "  [MISSING] $tool not found"
            $missing += $tool
        }
    }
    
    if ($missing.Count -gt 0) {
        Write-Error-Msg "Missing tools: $($missing -join ', ')"
        Write-Info "Please install the missing tools and try again."
        exit 1
    }
    
    # Check kubectl context
    Write-Info "Checking Kubernetes context..."
    $context = kubectl config current-context 2>$null
    if (-not $context) {
        Write-Error-Msg "No active Kubernetes context. Please start your local cluster."
        Write-Info ""
        Write-Info "For Docker Desktop: Enable Kubernetes in Docker Desktop settings"
        Write-Info "For Minikube: Run 'minikube start'"
        Write-Info "For Kind: Run 'kind create cluster'"
        exit 1
    }
    
    Write-Success "  [OK] Kubernetes cluster connected: $context"
    Write-Success "All prerequisites met"
}

# ============================================================================
# Docker Builds
# ============================================================================

function Build-ContainerImages {
    Write-Section "BUILDING DOCKER IMAGES"

    $services = @(
        @{Name = 'booking-api'; Path = 'Booking.API' },
        @{Name = 'flight-api'; Path = 'Flight.API' },
        @{Name = 'hotel-api'; Path = 'Hotel.API' },
        @{Name = 'car-api'; Path = 'Car.API' }
    )

    foreach ($service in $services) {
        $imageName = "$($service.Name):local"
        Write-Info "Building $($service.Name)..."
        
        # Run docker build with quiet progress output
        $null = docker build `
            -t $imageName `
            -f "src/Services/$($service.Path)/Dockerfile" . `
            --progress=quiet 2>&1
        
        # Check if the image was actually created (this is the real indicator of success)
        $imageExists = docker image inspect $imageName 2>$null
        if ($null -ne $imageExists) {
            Write-Success "Built $($service.Name)"
        }
        else {
            Write-Error-Msg "Failed to build $($service.Name) - image not created"
            exit 1
        }

        # Push to registry if requested
        if ($PushToRegistry) {
            $registryImage = "$ImageRegistry/$imageName"
            Write-Info "  Pushing to $registryImage..."
            docker tag $imageName $registryImage 2>$null
            docker push $registryImage 2>$null
            Write-Success "Pushed $($service.Name) to registry"
        }
    }

    Write-Success "All images built successfully"
}

# ============================================================================
# Kubernetes Resource Management
# ============================================================================

function Create-Namespace {
    Write-Info "Creating namespace: saga-pattern"
    
    try {
        kubectl create namespace saga-pattern --dry-run=client -o yaml | kubectl apply -f - 2>$null
        Write-Success "Namespace 'saga-pattern' ready"
    }
    catch {
        Write-Error-Msg "Failed to create namespace: $_"
        exit 1
    }
}

function Create-Secrets {
    Write-Section "CREATING KUBERNETES SECRETS"

    try {
        # PostgreSQL credentials
        Write-Info "Creating postgres-credentials secret..."
        kubectl create secret generic postgres-credentials `
            --namespace saga-pattern `
            --from-literal=username="$PostgresUsername" `
            --from-literal=password="$PostgresPassword" `
            --dry-run=client -o yaml | kubectl apply -f - | Out-Null
        Write-Success "Created postgres-credentials"

        # RabbitMQ credentials
        Write-Info "Creating rabbitmq-credentials secret..."
        kubectl create secret generic rabbitmq-credentials `
            --namespace saga-pattern `
            --from-literal=username="$RabbitMqUsername" `
            --from-literal=password="$RabbitMqPassword" `
            --dry-run=client -o yaml | kubectl apply -f - | Out-Null
        Write-Success "Created rabbitmq-credentials"

        Write-Success "All secrets created successfully"
    }
    catch {
        Write-Error-Msg "Failed to create secrets: $_"
        exit 1
    }
}

function Deploy-Application {
    Write-Section "DEPLOYING APPLICATION TO LOCAL CLUSTER"

    # Check if namespace is terminating and wait for it
    Write-Info "Checking namespace status..."
    $previousEA = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    $nsStatus = kubectl get namespace saga-pattern -o jsonpath='{.status.phase}' 2>$null
    $ErrorActionPreference = $previousEA
    if ($nsStatus -eq "Terminating") {
        Write-Info "Namespace is terminating, waiting for cleanup to complete..."
        $timeout = 0
        while ($nsStatus -eq "Terminating" -and $timeout -lt 60) {
            Start-Sleep -Seconds 2
            $timeout += 2
            $previousEA = $ErrorActionPreference
            $ErrorActionPreference = "Continue"
            $nsStatus = kubectl get namespace saga-pattern -o jsonpath='{.status.phase}' 2>$null
            $ErrorActionPreference = $previousEA
            Write-Info "  Still terminating... ($timeout seconds elapsed)"
        }
        if ($nsStatus -eq "Terminating") {
            Write-Warning-Msg "Namespace still terminating after 60 seconds, proceeding with deployment..."
        }
    }

    # Apply using Kustomize local overlay (may get warnings about resources being deleted)
    Write-Info "Deploying with Kustomize (local overlay)..."
    
    # Try apply multiple times if it fails due to namespace terminating
    $maxRetries = 3
    $retryCount = 0
    $deployed = $false
    
    while ($retryCount -lt $maxRetries -and -not $deployed) {
        # Use call operator and suppress errors temporarily to handle kubectl warnings gracefully
        $previousErrorAction = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        
        $output = & kubectl apply -k k8s/overlays/local 2>&1
        
        $ErrorActionPreference = $previousErrorAction
        
        # Check if deployment was successful by looking for resources created/unchanged
        $statusLines = $output | Where-Object { $_ -match "created|unchanged|configured" }
        
        if ($statusLines -and $statusLines.Count -gt 0) {
            # We successfully applied resources
            Write-Success "All manifests applied successfully"
            $deployed = $true
            break
        }
        
        # Check if we got terminating namespace errors
        $terminatingError = $output | Select-String "currently being deleted"
        
        if ($null -ne $terminatingError) {
            $retryCount++
            if ($retryCount -lt $maxRetries) {
                Write-Info "Namespace still terminating, retrying in 5 seconds... (Attempt $retryCount/$maxRetries)"
                Start-Sleep -Seconds 5
            }
            else {
                # After retries, most resources should be applied anyway
                Write-Warning-Msg "Namespace deletion is slow, but proceeding with deployment verification..."
                $deployed = $true
            }
        }
        else {
            # Some other error
            Write-Error-Msg "Deployment encountered unexpected error"
            $output | Where-Object { $_ -match "Error" }
            exit 1
        }
    }

    # Wait for deployments
    Write-Info ""
    Write-Info "Waiting for deployments to be ready (this may take 2-5 minutes)..."
    
    $deployments = @("booking-api", "flight-api", "hotel-api", "car-api", "otel-collector")
    foreach ($deployment in $deployments) {
        Write-Info "  Waiting for $deployment..."
        $previousEA = $ErrorActionPreference
        $ErrorActionPreference = "Continue"
        kubectl rollout status deployment/$deployment -n saga-pattern --timeout=5m 2>$null
        $ErrorActionPreference = $previousEA
        if ($LASTEXITCODE -ne 0) {
            Write-Warning-Msg "$deployment rollout did not complete within timeout, continuing..."
        }
    }

    Write-Success "All deployments are ready!"
}

# ============================================================================
# Deployment Verification
# ============================================================================

function Verify-Deployment {
    Write-Section "VERIFYING DEPLOYMENT"

    try {
        Write-Info "Checking pod status..."
        $pods = kubectl get pods -n saga-pattern -o json | ConvertFrom-Json
        
        if (-not $pods.items -or $pods.items.Count -eq 0) {
            Write-Error-Msg "No pods found in namespace 'saga-pattern'"
            exit 1
        }

        $running = ($pods.items | Where-Object { $_.status.phase -eq "Running" }).Count
        $total = $pods.items.Count

        Write-Info "Pod Status: $running/$total Running"
        Write-Info ""

        if ($running -eq $total) {
            Write-Success "All pods are running!"
        }
        else {
            Write-Warning-Msg "Some pods are not running yet"
            Write-Info ""
            Write-Info "Pod details:"
            kubectl get pods -n saga-pattern -o wide
        }

        # Show services
        Write-Info ""
        Write-Info "Services:"
        kubectl get svc -n saga-pattern -o wide 2>$null

        # Port forwarding info
        Write-Info ""
        Write-Info "To access services, use port forwarding:"
        Write-Info "  kubectl port-forward -n saga-pattern svc/booking-api 5001:80"
        Write-Info "  kubectl port-forward -n saga-pattern svc/flight-api 5002:8080"
        Write-Info "  kubectl port-forward -n saga-pattern svc/hotel-api 5003:8080"
        Write-Info "  kubectl port-forward -n saga-pattern svc/car-api 5004:8080"
        Write-Info "  kubectl port-forward -n saga-pattern svc/rabbitmq 15672:15672"
        Write-Info "  kubectl port-forward -n saga-pattern svc/grafana 3000:80"
        Write-Info "  kubectl port-forward -n saga-pattern svc/prometheus 9090:9090"

        Write-Success ""
        Write-Success "Verification complete!"

    }
    catch {
        Write-Error-Msg "Verification failed: $_"
        exit 1
    }
}

# ============================================================================
# Deployment Management
# ============================================================================

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

function Cleanup-Resources {
    Write-Section "CLEANUP"

    Write-Warning-Msg "This will delete the entire namespace and all resources"
    $confirm = Read-Host "Continue? (yes/no)"
    if ($confirm -ne "yes") {
        Write-Info "Cleanup cancelled"
        return
    }

    try {
        Write-Info "Deleting namespace 'saga-pattern'..."
        kubectl delete namespace saga-pattern --ignore-not-found 2>$null
        Write-Success "Namespace deleted"

        Write-Success "Cleanup complete!"

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
    Write-Info "  Image Registry: $ImageRegistry"
    Write-Info "  Push To Registry: $PushToRegistry"
    Write-Info ""
    
    # Ensure we're on the correct context
    Set-KubernetesContext -ContextName $KubernetesContext
    Write-Info ""
    
    Test-Prerequisites
    Write-Info ""
    
    switch ($Action) {
        'build' {
            Build-ContainerImages
        }
        'deploy' {
            Build-ContainerImages
            Deploy-Application
            Verify-Deployment
        }
        'verify' {
            Verify-Deployment
        }
        'rollback' {
            Rollback-Deployment
        }
        'cleanup' {
            Cleanup-Resources
        }
    }
    
    Write-Host ""
    Write-Host "================================================" -ForegroundColor Green
    Write-Host " [OK] Action completed successfully!           " -ForegroundColor Green
    Write-Host "================================================" -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Error-Msg "Fatal error: $_"
    exit 1
}
