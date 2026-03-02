# Kubernetes Manifests Summary

This document provides a summary of all Kubernetes resources created for the Saga Pattern application deployment to AKS.

## Created Manifest Files

### Core Infrastructure (5 files)

1. **namespace.yaml**
   - Defines the `saga-pattern` namespace
   - All resources are deployed within this namespace

2. **secrets.yaml**
   - PostgreSQL credentials (username: postgres, password: postgres)
   - RabbitMQ credentials (username: guest, password: guest)
   - ⚠️ Note: In production, use Azure Key Vault

3. **postgres-configmap.yaml**
   - Database initialization script
   - Creates BookingDb, FlightDb, HotelDb, CarDb databases

### Stateful Services (2 files)

4. **postgres.yaml**
   - StatefulSet: Single PostgreSQL 16-alpine instance
   - Service: Headless service for internal access
   - PVC: 10Gi managed-csi storage
   - Health checks: Liveness and readiness probes
   - Replicas: 1
   - Resource limits: 256-512Mi memory, 250-500m CPU

5. **rabbitmq.yaml**
   - StatefulSet: Single RabbitMQ 3.13-management instance
   - Service: Headless service (AMQP + Management ports)
   - PVC: 5Gi managed-csi storage
   - Health checks: Liveness and readiness probes
   - Replicas: 1
   - Resource limits: 256-512Mi memory, 250-500m CPU

### Observability Stack (4 files)

6. **otel-collector.yaml**
   - Deployment: OpenTelemetry Collector
   - Service: ClusterIP for metrics collection
   - ConfigMap: OTLP receiver and exporter configuration
   - Replicas: 1
   - Resource limits: 128-256Mi memory, 100-200m CPU
   - Exports to: Prometheus, Tempo

7. **tempo.yaml**
   - Deployment: Grafana Tempo for distributed tracing
   - Service: ClusterIP for OTLP gRPC access
   - PVC: 5Gi managed-csi storage
   - ConfigMap: Tempo configuration
   - Replicas: 1
   - Resource limits: 128-256Mi memory, 100-200m CPU

8. **prometheus.yaml**
   - Deployment: Prometheus v2.54.1
   - Service: ClusterIP on port 9090
   - PVC: 5Gi managed-csi storage
   - ConfigMap: Scrape configurations for all services
   - Replicas: 1
   - Resource limits: 128-256Mi memory, 100-200m CPU
   - Scrapes: All API services, OTEL collector, itself

9. **grafana.yaml**
   - Deployment: Grafana 11.2.0
   - Service: LoadBalancer on port 80
   - PVC: 2Gi managed-csi storage
   - Replicas: 1
   - Resource limits: 128-256Mi memory, 100-200m CPU
   - Admin credentials: admin / admin (⚠️ Change in production)

### Microservices (4 files)

10. **booking-api.yaml**
    - Deployment: Booking service (HTTP API)
    - Service: LoadBalancer on port 80
    - Replicas: 2 (with HPA up to 10)
    - Resource limits: 256-512Mi memory, 250-500m CPU
    - Environment: Production
    - Dependencies: PostgreSQL, RabbitMQ, OTEL Collector
    - Health checks: Liveness and readiness probes
    - Termination grace period: 30s

11. **flight-api.yaml**
    - Deployment: Flight booking service (RabbitMQ consumer)
    - Service: ClusterIP on port 8080
    - Replicas: 1 (with HPA up to 5)
    - Resource limits: 256-512Mi memory, 250-500m CPU
    - Environment: Production
    - Dependencies: PostgreSQL, RabbitMQ, OTEL Collector
    - Health checks: Liveness and readiness probes

12. **hotel-api.yaml**
    - Deployment: Hotel booking service (RabbitMQ consumer)
    - Service: ClusterIP on port 8080
    - Replicas: 1 (with HPA up to 5)
    - Resource limits: 256-512Mi memory, 250-500m CPU
    - Environment: Production
    - Dependencies: PostgreSQL, RabbitMQ, OTEL Collector
    - Health checks: Liveness and readiness probes

13. **car-api.yaml**
    - Deployment: Car rental service (RabbitMQ consumer)
    - Service: ClusterIP on port 8080
    - Replicas: 1 (with HPA up to 5)
    - Resource limits: 256-512Mi memory, 250-500m CPU
    - Environment: Production
    - Dependencies: PostgreSQL, RabbitMQ, OTEL Collector
    - Health checks: Liveness and readiness probes

### Advanced Features (3 files)

14. **hpa.yaml**
    - HPA for booking-api: Min 2, Max 10 replicas (CPU 70%, Memory 80%)
    - HPA for flight-api: Min 1, Max 5 replicas (CPU 70%)
    - HPA for hotel-api: Min 1, Max 5 replicas (CPU 70%)
    - HPA for car-api: Min 1, Max 5 replicas (CPU 70%)

15. **network-policies.yaml**
    - Default deny all policy (zero-trust security)
    - Allow policies for inter-service communication
    - Allow DNS (port 53) for service discovery
    - Policies for: Booking, Flight, Hotel, Car, RabbitMQ, PostgreSQL, OTEL, Prometheus, Grafana

16. **ingress.yaml**
    - Ingress controller: Azure Application Gateway
    - Routes for: saga-booking.example.com (Booking API)
    - Routes for: grafana.example.com (Grafana)
    - ℹ️ Update hostnames for your domain

### Configuration & Orchestration

17. **kustomization.yaml**
    - Base Kustomize configuration
    - References all manifests
    - Applies common labels and annotations
    - Namespace: saga-pattern

## Kubernetes Resources Summary

### Namespaces
- `saga-pattern`

### Deployments (6)
- `otel-collector` (1 replica)
- `tempo` (1 replica)
- `prometheus` (1 replica)
- `grafana` (1 replica)
- `booking-api` (2 replicas, HPA: 2-10)
- `flight-api` (1 replica, HPA: 1-5)
- `hotel-api` (1 replica, HPA: 1-5)
- `car-api` (1 replica, HPA: 1-5)

### StatefulSets (2)
- `postgres` (1 replica)
- `rabbitmq` (1 replica)

### Services (10)
- `postgres` (Headless, port 5432)
- `rabbitmq` (Headless, ports 5672, 15672)
- `otel-collector` (ClusterIP, ports 4317, 4318, 8888)
- `tempo` (ClusterIP, port 3200)
- `prometheus` (ClusterIP, port 9090)
- `grafana` (LoadBalancer, port 80)
- `booking-api` (LoadBalancer, port 80)
- `flight-api` (ClusterIP, port 8080)
- `hotel-api` (ClusterIP, port 8080)
- `car-api` (ClusterIP, port 8080)

### Persistent Volume Claims (5)
- `postgres-pvc` (10Gi)
- `rabbitmq-pvc` (5Gi)
- `tempo-pvc` (5Gi)
- `prometheus-pvc` (5Gi)
- `grafana-pvc` (2Gi)

### ConfigMaps (3)
- `postgres-init-config`
- `otel-collector-config`
- `tempo-config`
- `prometheus-config`

### Secrets (1)
- `postgres-credentials`
- `rabbitmq-credentials`

### NetworkPolicies (11)
- `default-deny-all`
- `allow-booking-api-ingress`
- `allow-booking-to-rabbitmq`
- `allow-booking-to-postgres`
- `allow-booking-to-otel`
- `allow-flight-communication`
- `allow-hotel-communication`
- `allow-car-communication`
- `allow-rabbitmq-ingress`
- `allow-postgres-ingress`
- `allow-otel-ingress`
- `allow-prometheus-scrape`
- `allow-grafana-ingress`
- `allow-dns-egress`

### HorizontalPodAutoscalers (4)
- `booking-api-hpa` (target: 70% CPU, 80% memory)
- `flight-api-hpa` (target: 70% CPU)
- `hotel-api-hpa` (target: 70% CPU)
- `car-api-hpa` (target: 70% CPU)

### Ingress (1)
- `saga-ingress` (Application Gateway)

## Deployment Quick Reference

### Deploy All Resources
```powershell
kubectl apply -k k8s/
```

### Deploy Specific Resources
```powershell
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/secrets.yaml
kubectl apply -f k8s/postgres.yaml
```

### Monitor Deployment
```powershell
kubectl get pods -n saga-pattern
kubectl get svc -n saga-pattern
```

### Scale a Service
```powershell
kubectl scale deployment booking-api -n saga-pattern --replicas=5
```

### Access Services
```powershell
# Booking API
kubectl get svc booking-api -n saga-pattern  # Get LoadBalancer IP

# Grafana
kubectl port-forward -n saga-pattern svc/grafana 3000:80

# RabbitMQ
kubectl port-forward -n saga-pattern svc/rabbitmq 15672:15672

# Prometheus
kubectl port-forward -n saga-pattern svc/prometheus 9090:9090
```

## Resource Consumption

### Storage
- Total PVC Storage: 32Gi
  - PostgreSQL: 10Gi
  - RabbitMQ: 5Gi
  - Tempo: 5Gi
  - Prometheus: 5Gi
  - Grafana: 2Gi

### Memory
- Total Requested: ~2.5Gi
- Total Limited: ~4.6Gi
- Per Service Limit: 256-512Mi

### CPU
- Total Requested: ~2250m (~2.25 CPU)
- Total Limited: ~4300m (~4.3 CPU)
- Per Service Limit: 250-500m

## Configuration Details

### Database Connection
- Host: `postgres`
- Port: `5432`
- Databases: BookingDb, FlightDb, HotelDb, CarDb
- Username: postgres
- Password: postgres (from secret)

### RabbitMQ Connection
- Host: `rabbitmq`
- Port: `5672`
- Management: `rabbitmq:15672`
- Username: guest
- Password: guest (from secret)

### Observability
- OTEL Endpoint: `http://otel-collector:4318`
- Prometheus: `http://prometheus:9090`
- Tempo: `http://tempo:3200`
- Grafana: `http://<LoadBalancer-IP>`

## Important Notes

⚠️ **Production Considerations**:
1. Replace hardcoded passwords with Azure Key Vault secrets
2. Use Azure Database for PostgreSQL (managed service)
3. Use Azure Service Bus instead of RabbitMQ
4. Update Ingress hostnames to match your domain
5. Configure TLS/SSL certificates
6. Enable Azure AD integration for authentication
7. Use Application Gateway with WAF
8. Enable cluster monitoring with Azure Monitor
9. Configure backup and disaster recovery strategies

📝 **Image Registry**:
- Update `<your-registry>` placeholder in service manifests
- Example: `sagapatternacr.azurecr.io`

🔐 **Security**:
- Enable network policies (included)
- Implement RBAC
- Use Pod Security Policies
- Configure service accounts
- Enable container image scanning

## Deployment Workflow

1. **Preparation**
   - Create Azure resource group
   - Create container registry
   - Build and push images

2. **Cluster Setup**
   - Create AKS cluster
   - Get cluster credentials
   - Configure kubectl context

3. **Deployment**
   - Update image references
   - Apply Kustomize manifests
   - Wait for rollout completion

4. **Verification**
   - Verify all pods are running
   - Check service endpoints
   - Test API connectivity

5. **Monitoring**
   - Access Grafana dashboards
   - Monitor metrics in Prometheus
   - View traces in Tempo

## Troubleshooting

See [k8s/DEPLOYMENT_GUIDE.md](DEPLOYMENT_GUIDE.md#troubleshooting) for comprehensive troubleshooting steps.

Common issues:
- Image pull errors: Ensure ACR credentials are configured
- Pod pending: Check resource limits and node availability
- Database connectivity: Verify postgres StatefulSet is running
- Service access: Check service discovery and endpoints
