#!/usr/bin/env pwsh

<#
.SYNOPSIS
Display access URLs for all deployed Saga Pattern services in AKS

.DESCRIPTION
This script shows all public IPs and access URLs for services deployed in the saga-pattern namespace.
Useful for quickly finding how to access your services after deployment.

.PARAMETER Namespace
The Kubernetes namespace (default: saga-pattern)

.EXAMPLE
.\get-service-urls.ps1

.EXAMPLE
.\get-service-urls.ps1 -Namespace saga-pattern
#>

param(
    [string]$Namespace = 'saga-pattern'
)

$ErrorActionPreference = 'Continue'

function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " $Text" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════" -ForegroundColor Cyan
}

function Write-ServiceInfo {
    param(
        [string]$ServiceName,
        [string]$Description
    )
    
    Write-Host ""
    Write-Host "► $ServiceName" -ForegroundColor Yellow
    Write-Host "  $Description" -ForegroundColor Gray
    
    $ip = kubectl get svc $ServiceName -n $Namespace -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>$null
    
    if ($ip) {
        Write-Host "  URL: " -NoNewline -ForegroundColor Green
        Write-Host "http://$ip" -ForegroundColor White
    }
    else {
        $type = kubectl get svc $ServiceName -n $Namespace -o jsonpath='{.spec.type}' 2>$null
        if ($type -eq 'LoadBalancer') {
            Write-Host "  Status: " -NoNewline -ForegroundColor Yellow
            Write-Host "Waiting for LoadBalancer IP (this may take 2-5 minutes)..." -ForegroundColor Gray
        }
        elseif ($type -eq 'ClusterIP') {
            Write-Host "  Access: " -NoNewline -ForegroundColor Cyan
            Write-Host "Internal only - use port-forward" -ForegroundColor Gray
            Write-Host "  Command: kubectl port-forward -n $Namespace svc/$ServiceName <local-port>:<service-port>" -ForegroundColor DarkGray
        }
        else {
            Write-Host "  Status: " -NoNewline -ForegroundColor Red
            Write-Host "Service not found or not accessible" -ForegroundColor Gray
        }
    }
}

Write-Header "Saga Pattern - Service Access URLs"

Write-Host ""
Write-Host "Namespace: $Namespace" -ForegroundColor White
Write-Host "Cluster: " -NoNewline
kubectl config current-context

# Check if namespace exists
$namespaceExists = kubectl get namespace $Namespace 2>$null
if (-not $namespaceExists) {
    Write-Host ""
    Write-Host "ERROR: Namespace '$Namespace' not found." -ForegroundColor Red
    Write-Host "Have you deployed the application yet?" -ForegroundColor Yellow
    exit 1
}

# Public Services (LoadBalancer)
Write-Header "Public Services (External Access)"

Write-ServiceInfo "booking-api" "Main HTTP API for creating and managing bookings"
Write-ServiceInfo "grafana" "Observability dashboards and metrics visualization"

# Ingress
Write-Host ""
Write-Host "► Ingress Controller" -ForegroundColor Yellow
Write-Host "  Path-based routing for all services" -ForegroundColor Gray
$ingressIp = kubectl get ingress saga-ingress -n $Namespace -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>$null
if ($ingressIp) {
    Write-Host "  Booking API: " -NoNewline -ForegroundColor Green
    Write-Host "http://$ingressIp/api" -ForegroundColor White
    Write-Host "  Grafana: " -NoNewline -ForegroundColor Green
    Write-Host "http://$ingressIp/grafana" -ForegroundColor White
}
else {
    Write-Host "  Status: " -NoNewline -ForegroundColor Yellow
    Write-Host "Ingress not configured or waiting for IP" -ForegroundColor Gray
}

# Internal Services
Write-Header "Internal Services (Port-Forward Required)"

Write-Host ""
Write-Host "► RabbitMQ Management Console" -ForegroundColor Yellow
Write-Host "  Message broker administration interface" -ForegroundColor Gray
Write-Host "  Command: " -NoNewline -ForegroundColor Cyan
Write-Host "kubectl port-forward -n $Namespace svc/rabbitmq 15672:15672" -ForegroundColor White
Write-Host "  Access: http://localhost:15672 (guest/guest)" -ForegroundColor Gray

Write-Host ""
Write-Host "► Prometheus" -ForegroundColor Yellow
Write-Host "  Metrics and monitoring" -ForegroundColor Gray
Write-Host "  Command: " -NoNewline -ForegroundColor Cyan
Write-Host "kubectl port-forward -n $Namespace svc/prometheus 9090:9090" -ForegroundColor White
Write-Host "  Access: http://localhost:9090" -ForegroundColor Gray

Write-Host ""
Write-Host "► Tempo" -ForegroundColor Yellow
Write-Host "  Distributed tracing (query via Grafana)" -ForegroundColor Gray
Write-Host "  Command: " -NoNewline -ForegroundColor Cyan
Write-Host "kubectl port-forward -n $Namespace svc/tempo 3200:3200" -ForegroundColor White
Write-Host "  Access: http://localhost:3200" -ForegroundColor Gray

Write-Host ""
Write-Host "► PostgreSQL Database" -ForegroundColor Yellow
Write-Host "  Primary database (BookingDb, FlightDb, HotelDb, CarDb)" -ForegroundColor Gray
Write-Host "  Command: " -NoNewline -ForegroundColor Cyan
Write-Host "kubectl port-forward -n $Namespace svc/postgres 5432:5432" -ForegroundColor White
Write-Host "  Connection: postgres://postgres:postgres@localhost:5432" -ForegroundColor Gray

# Microservices (ClusterIP)
Write-Header "Microservices (Internal - ClusterIP)"

Write-ServiceInfo "flight-api" "Flight booking consumer service"
Write-ServiceInfo "hotel-api" "Hotel booking consumer service"
Write-ServiceInfo "car-api" "Car rental consumer service"

# Helper Commands
Write-Header "Useful Commands"

Write-Host ""
Write-Host "• Watch service status:" -ForegroundColor White
Write-Host "  kubectl get svc -n $Namespace --watch" -ForegroundColor Gray

Write-Host ""
Write-Host "• View all pods:" -ForegroundColor White
Write-Host "  kubectl get pods -n $Namespace" -ForegroundColor Gray

Write-Host ""
Write-Host "• View logs for booking-api:" -ForegroundColor White
Write-Host "  kubectl logs -n $Namespace -l app=booking-api -f" -ForegroundColor Gray

Write-Host ""
Write-Host "• Test Booking API:" -ForegroundColor White
Write-Host "  `$ip = kubectl get svc booking-api -n $Namespace -o jsonpath='{.status.loadBalancer.ingress[0].ip}'" -ForegroundColor Gray
Write-Host "  curl http://`$ip/health" -ForegroundColor Gray

Write-Host ""
Write-Host "• For more access options, see: k8s/ACCESSING_SERVICES.md" -ForegroundColor Cyan

Write-Host ""
