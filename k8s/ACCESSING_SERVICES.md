# Accessing Services in AKS Without Custom Domains

This guide explains how to access your Saga Pattern services deployed to AKS when you don't have registered domain names.

## Service Access Options

### Option 1: Direct LoadBalancer IPs (Recommended for Testing)

**Booking API and Grafana are already configured with LoadBalancer services**, so they get public IPs automatically.

#### Get the IPs:
```powershell
# Get Booking API IP
kubectl get svc booking-api -n saga-pattern

# Get Grafana IP
kubectl get svc grafana -n saga-pattern
```

#### Access the services:
```powershell
# Booking API
$bookingIp = kubectl get svc booking-api -n saga-pattern -o jsonpath='{.status.loadBalancer.ingress[0].ip}'
Write-Host "Booking API: http://$bookingIp"

# Grafana
$grafanaIp = kubectl get svc grafana -n saga-pattern -o jsonpath='{.status.loadBalancer.ingress[0].ip}'
Write-Host "Grafana: http://$grafanaIp"
```

**Pros:**
- Simple, no additional configuration
- Works immediately after deployment
- No DNS or domain required

**Cons:**
- IPs may change if services are recreated
- Not user-friendly for sharing
- No SSL/TLS by default

---

### Option 2: Azure Application Gateway with Path-Based Routing

Use the Ingress controller with path-based routing (no custom domain needed).

#### Current Configuration:
The `ingress.yaml` is set to use path-based routing by default:
- `http://<INGRESS-IP>/api` → Booking API
- `http://<INGRESS-IP>/grafana` → Grafana

#### Get Ingress IP:
```powershell
kubectl get ingress saga-ingress -n saga-pattern

# Wait for EXTERNAL-IP (may take 5-10 minutes)
# Once available:
$ingressIp = kubectl get ingress saga-ingress -n saga-pattern -o jsonpath='{.status.loadBalancer.ingress[0].ip}'
Write-Host "Booking API: http://$ingressIp/api"
Write-Host "Grafana: http://$ingressIp/grafana"
```

**Note:** Grafana may need configuration to work under a subpath. Add this to grafana deployment env:
```yaml
- name: GF_SERVER_ROOT_URL
  value: "%(protocol)s://%(domain)s/grafana/"
- name: GF_SERVER_SERVE_FROM_SUB_PATH
  value: "true"
```

**Pros:**
- Single IP for all services
- Organized by path
- Can add SSL/TLS certificate later

**Cons:**
- Requires Application Gateway (additional Azure cost)
- May need app configuration for subpath routing

---

### Option 3: nip.io Wildcard DNS

Use the free [nip.io](https://nip.io) service to get DNS names based on your LoadBalancer IPs.

#### How it works:
For IP `20.10.5.10`, these DNS names automatically resolve to that IP:
- `booking.20.10.5.10.nip.io` → `20.10.5.10`
- `grafana.20.10.5.10.nip.io` → `20.10.5.10`
- `anything.20.10.5.10.nip.io` → `20.10.5.10`

#### Update ingress.yaml:
```powershell
# Get your Ingress IP first
$ingressIp = kubectl get ingress saga-ingress -n saga-pattern -o jsonpath='{.status.loadBalancer.ingress[0].ip}'

# Edit ingress.yaml and use Option 3 template
# Replace <INGRESS-IP> with your actual IP
# Example: booking.20.10.5.10.nip.io
```

#### Apply and access:
```powershell
kubectl apply -f k8s/ingress.yaml

# Access via friendly URLs:
# http://booking.<your-ingress-ip>.nip.io
# http://grafana.<your-ingress-ip>.nip.io
```

**Pros:**
- Free DNS names without registration
- Works like real domains
- Good for demos and testing

**Cons:**
- Depends on external service (nip.io)
- Not suitable for production
- No built-in SSL

---

### Option 4: Azure-Provided FQDN

Some Azure regions provide `*.cloudapp.azure.com` FQDNs for public IPs.

#### Create with Azure DNS:
```powershell
# Get the public IP resource ID
$publicIpId = az network public-ip list `
  --resource-group MC_saga-pattern-rg_saga-pattern-aks_eastus `
  --query "[?contains(ipAddress, '<your-loadbalancer-ip>')].id" -o tsv

# Set DNS name
az network public-ip update `
  --ids $publicIpId `
  --dns-name saga-booking

# Access via: saga-booking.<region>.cloudapp.azure.com
```

**Pros:**
- Azure-managed DNS
- More permanent than nip.io
- Free within Azure

**Cons:**
- Limited customization
- Requires finding the public IP resource
- Different per region

---

### Option 5: Port Forwarding (Development Only)

For local development/testing, forward ports from your local machine to AKS services.

```powershell
# Booking API on localhost:5001
kubectl port-forward -n saga-pattern svc/booking-api 5001:80

# Grafana on localhost:3000
kubectl port-forward -n saga-pattern svc/grafana 3000:80

# Prometheus on localhost:9090
kubectl port-forward -n saga-pattern svc/prometheus 9090:9090

# RabbitMQ Management on localhost:15672
kubectl port-forward -n saga-pattern svc/rabbitmq 15672:15672
```

**Pros:**
- Easy for local testing
- No network configuration
- Works with private clusters

**Cons:**
- Only accessible from your machine
- Connection drops if terminal closes
- Not for sharing with others

---

## Recommendation by Use Case

| Use Case | Recommended Option | Why |
|----------|-------------------|-----|
| **Quick Testing** | Direct LoadBalancer IPs (Option 1) | Simplest, works immediately |
| **Demo/POC** | nip.io (Option 3) | Friendly URLs without domain cost |
| **Development** | Port Forwarding (Option 5) | Local access, secure |
| **Staging/Production** | Custom Domain + Ingress | Professional, SSL support |

---

## Setting Up Custom Domains (Future)

When you're ready to use real domains:

1. **Register a domain** (e.g., via Azure App Service Domains, GoDaddy, Namecheap)

2. **Get Ingress IP:**
   ```powershell
   kubectl get ingress saga-ingress -n saga-pattern
   ```

3. **Create DNS A records:**
   ```
   booking.yourdomain.com  → <INGRESS-IP>
   grafana.yourdomain.com  → <INGRESS-IP>
   ```

4. **Update ingress.yaml** to Option 2 (custom domains section)

5. **Add SSL/TLS:**
   ```yaml
   metadata:
     annotations:
       cert-manager.io/cluster-issuer: letsencrypt-prod
   spec:
     tls:
     - hosts:
       - booking.yourdomain.com
       secretName: booking-tls
   ```

---

## Current Default Configuration

The repository is configured for **Option 1 (Path-based routing without domains)**:

```
✓ Booking API LoadBalancer: Enabled
✓ Grafana LoadBalancer: Enabled
✓ Ingress path-based: /api and /grafana
✗ Ingress custom domains: Commented out
```

### Quick Start After Deployment:

```powershell
# Show all access URLs
kubectl get svc -n saga-pattern | Where-Object { $_.SPEC -match 'LoadBalancer' }

# Or use this helper:
Write-Host "Booking API:"
kubectl get svc booking-api -n saga-pattern -o jsonpath='{.status.loadBalancer.ingress[0].ip}'

Write-Host "`nGrafana:"
kubectl get svc grafana -n saga-pattern -o jsonpath='{.status.loadBalancer.ingress[0].ip}'
```

---

## Troubleshooting

### LoadBalancer IP shows `<pending>`
Wait 2-5 minutes for Azure to provision the public IP. Check with:
```powershell
kubectl get svc -n saga-pattern --watch
```

### Ingress not getting IP
Ensure Application Gateway Ingress Controller is installed:
```powershell
az aks addon list --resource-group saga-pattern-rg --name saga-pattern-aks
```

### Can't access service from browser
1. Check if pod is running: `kubectl get pods -n saga-pattern`
2. Check service endpoints: `kubectl get endpoints -n saga-pattern`
3. Check Azure Network Security Groups for port 80/443
4. Try with `curl` first: `curl http://<IP>`

### nip.io not resolving
- Try with different DNS server (8.8.8.8, 1.1.1.1)
- Check if nip.io is accessible: `nslookup 127.0.0.1.nip.io`
- Fall back to direct IP access

---

## Additional Resources

- [Azure Load Balancer](https://docs.microsoft.com/en-us/azure/load-balancer/)
- [AKS Application Gateway Ingress Controller](https://docs.microsoft.com/en-us/azure/application-gateway/ingress-controller-overview)
- [nip.io Documentation](https://nip.io)
- [cert-manager for SSL](https://cert-manager.io/docs/)
