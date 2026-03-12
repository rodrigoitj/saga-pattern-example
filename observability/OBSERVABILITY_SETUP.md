# Observability Setup Guide

This guide walks you through the complete observability stack available in the local development environment. The stack is organized into three complementary layers:

1. **Metrics & Distributed Tracing** — Prometheus + Tempo + Grafana
2. **Log Aggregation & Analysis** — Elasticsearch + Kibana
3. **Alternative Performance Monitoring** — Dynatrace OneAgent

## Quick Start

### Start all services

```bash
# Copy the example .env file if you haven't already
cp .env.example .env

# (Optional) Add real Dynatrace credentials to .env for OneAgent monitoring
# DT_TENANT=your-tenant-id
# DT_API_URL=https://your-tenant.live.dynatrace.com
# DT_API_TOKEN=your-api-token

# Start the entire stack
docker compose up
```

### Verify all services are running

```bash
# Check service health
docker compose ps

# Expected output:
# booking-api-dev          running
# flight-api-dev           running
# hotel-api-dev            running
# car-api-dev              running
# saga-postgres            running
# saga-rabbitmq            running
# saga-otel-collector      running
# saga-prometheus          running
# saga-tempo               running
# saga-grafana             running
# saga-elasticsearch       running
# saga-kibana              running
# saga-dynatrace-oneagent  running
```

---

## Layer 1: Metrics & Distributed Tracing (Prometheus + Tempo + Grafana)

### Architecture

```
┌─────────────────┐
│  .NET Services  │  (Send OTLP traces, metrics, logs)
└────────┬────────┘
         │ OTLP/HTTP
         ↓
┌──────────────────────┐
│  OTLP Collector      │
└─┬──────────────────┬─┘
  │                  │
  │ gRPC proto       │ HTTP proto
  ↓                  ↓
Tempo             Prometheus
  │                  │
  └──────────┬───────┘
             ↓
          Grafana
```

### Access Grafana Dashboard

- **URL**: http://localhost:3000
- **Default username**: `admin`
- **Default password**: `admin`

### Pre-built Dashboards

The following dashboards are automatically provisioned:

1. **System Overview** — Cluster, pod, and node metrics
2. **Service Deep Dive** — Per-service request rates, errors, latency, database queries
3. **Service Topology** — Request flow between services
4. **Distributed Tracing** — View traces from Tempo with full span details
5. **Business Metrics** — Saga success rates, booking metrics, flight metrics, hotel metrics, car metrics

### Examining Traces

1. Navigate to **Explore** in Grafana
2. Select **Tempo** datasource
3. Use **Service** filter to find traces from specific services
4. Click on any trace to view:
   - Full request span hierarchy
   - Database queries (via EntityFrameworkCore instrumentation)
   - HTTP calls to other services
   - Execution duration of each operation

### Metrics Collection

The following metrics are automatically collected and available in Prometheus:

**HTTP Server Metrics**
- `http.server.request.duration` — Request latency percentiles (p50, p99)
- `http.server.request.duration.count` — Total request count
- Custom business metrics:
  - `booking.events.total` — Total booking events
  - `booking.success_rate` — Booking success/failure rates
  - `flight.queries.duration` — Flight query duration

**Database Metrics**
- `db.client.connections.active` — Active database connections
- `db.client.commands.duration` — Query duration by operation type (SELECT, INSERT, etc.)

**Runtime Metrics**
- `process.runtime.go.goroutines` → `dotnet_runtime_gc_collections_total` — GC collections
- `process.runtime.dotnet.monitor_lock_contention` — Lock contention

---

## Layer 2: Log Aggregation & Analysis (Elasticsearch + Kibana)

### Architecture

```
┌─────────────────┐
│  .NET Services  │  (Send logs via Serilog + OTLP)
└────────┬────────┘
         │ OTLP/HTTP
         ↓
┌──────────────────────┐
│  OTLP Collector      │
└──────────┬───────────┘
           │ Elasticsearch exporter
           ↓
┌──────────────────────┐
│  Elasticsearch       │  (Stores indexed logs)
└──────────┬───────────┘
           │ HTTP query
           ↓
        Kibana
```

### Access Kibana

- **URL**: http://localhost:5601
- **Default username**: None (no auth in dev mode)

### Create Index Pattern in Kibana

Kibana automatically discovers and displays logs as they arrive. The Elasticsearch exporter in the OTLP Collector stores logs with index name pattern `logs-YYYY.MM.DD`.

**To manually create an index pattern:**

1. Open Kibana → **Discover**
2. If prompted, create an index pattern:
   - **Index pattern name**: `logs-*`
   - **Timestamp field**: `@timestamp` (auto-selected)
   - Click **Create index pattern**
3. Logs from all services should now be visible

### View & Search Logs

1. Go to **Discover** tab
2. All application logs are indexed with the following fields:
   - `service.name` — Service that generated the log (booking-api, flight-api, etc.)
   - `message` — Log message
   - `level` — Log level (Information, Warning, Error, etc.)
   - `trace_id` — OpenTelemetry trace ID (for correlation with Tempo traces)
   - `span_id` — OpenTelemetry span ID
   - Custom fields — Any structured properties (e.g., `sagaId`, `bookingId`, `flightId`)

### Example Searches

**Find all errors from Booking.API:**
```json
service.name:"booking-api" AND level:"Error"
```

**Find all logs with a specific trace ID:**
```json
trace_id:"abc123def456..."
```

**Find all booking-related operations:**
```json
message:"booking" OR message:"Booking"
```

**Find slow database queries (from EF Core instrumentation):**
```json
message:"database" AND duration_ms > 1000
```

### Correlate Logs with Traces

1. Find a log entry in Kibana Discover
2. Note the `trace_id` field value
3. Open Grafana → **Explore** → **Tempo**
4. Paste the trace ID into the search field
5. View the complete distributed trace spanning all services

### Log Retention

Logs are stored in Elasticsearch indefinitely by default in local development. To clean up old logs:

```bash
# Delete logs older than 7 days (adjust date as needed)
curl -X DELETE "http://localhost:9200/logs-2026.02.26"

# Or use Kibana Dev Tools (Stack Management → Dev Tools)
DELETE /logs-2026.02.26
```

---

## Layer 3: Alternative Performance Monitoring (Dynatrace OneAgent)

Dynatrace OneAgent provides an alternative observability platform with automatic instrumentation, advanced root cause analysis, and AI-powered insights.

### Setup Dynatrace Monitoring

#### Option A: With Real Dynatrace Environment (Production Use)

1. **Create Dynatrace SaaS Account** (if you don't have one)
   - Go to https://www.dynatrace.com/trial/

2. **Get Your Tenant Credentials**
   - Dynatrace → **Settings** → **Integration** → **Dynatrace API**
   - Copy your **Tenant ID** (e.g., `abc12345`)
   - Note your **Environment URL** (e.g., `https://abc12345.live.dynatrace.com`)

3. **Generate API Token**
   - Dynatrace UI → **Settings** → **Integration** → **Dynatrace API** → **Create Token**
   - Enable scopes:
     - `app_monitoring:read:token`
     - `document_entities:read`
     - `entity:read:scope`
     - `installer:download`
   - Copy the token

4. **Configure .env**
   ```bash
   cp .env.example .env
   # Edit .env and add:
   DT_TENANT=abc12345
   DT_API_URL=https://abc12345.live.dynatrace.com
   DT_API_TOKEN=your-generated-token
   DT_HOST_ID=saga-local-dev
   ```

5. **Start containers**
   ```bash
   docker compose up
   ```

6. **Wait for data to appear**
   - OneAgent initialization takes 1-3 minutes
   - Check OneAgent logs: `docker logs saga-dynatrace-oneagent`
   - Open Dynatrace UI → **Hosts** — Should see `saga-local-dev` appearing

#### Option B: Mock/Development Mode (No Real Credentials)

If you don't have Dynatrace credentials:

1. Leave `DT_TENANT`, `DT_API_URL`, `DT_API_TOKEN` empty in `.env`
2. Start containers: `docker compose up`
3. OneAgent will fail to connect to Dynatrace but continue running
4. Check logs: `docker logs saga-dynatrace-oneagent`
5. Expected behavior: Agent logs show connection attempts without throwing errors

### Monitor Services in Dynatrace

Once connected to a real Dynatrace environment:

1. **View Services**
   - Dynatrace UI → **Services** → Filtering by `saga-*`
   - Each .NET service (Booking.API, Flight.API, Hotel.API, Car.API) appears as a monitored service

2. **View Request Traces**
   - Click any service → **Traces** tab
   - View complete request flow with timing breakdown

3. **Monitor Database Performance**
   - Click service → **Database** tab
   - See PostgreSQL query performance and connection pool stats

4. **View Service Dependencies**
   - **Applications** → **Service Flow** tab
   - Visual map of request flow between services

5. **Custom Metrics & Events**
   - OneAgent automatically sends events for deployments, problem detection, and anomalies
   - View in **Problems** dashboard

### Dynatrace OneAgent Container Details

| Component | Value |
|-----------|-------|
| **Container name** | saga-dynatrace-oneagent |
| **Image** | dynatrace/oneagent:x86_64 |
| **Privilege mode** | true (required for container monitoring) |
| **Docker socket** | /var/run/docker.sock (for container discovery) |
| **Environment vars** | DT_TENANT, DT_API_URL, DT_API_TOKEN, DT_HOST_ID |

### Troubleshooting Dynatrace

**Problem**: OneAgent container fails to start
- **Solution**: Check logs with `docker logs saga-dynatrace-oneagent`
- Verify environment variables are set correctly in `.env`

**Problem**: Agent shows "Checking Dynatrace connectivity..." but doesn't connect
- **Solution**: Verify DT_API_URL is correct (e.g., `https://abc12345.live.dynatrace.com` — note HTTPS)
- Check firewall/proxy settings if behind corporate firewall

**Problem**: Services appear in Dynatrace but show no data
- **Solution**: Wait 2-3 minutes for initial instrumentation
- Trigger some API requests to generate data: `curl http://localhost:5001/api/v1/bookings`

**Problem**: Want to disable OneAgent temporarily
- **Solution**: Remove from docker-compose.yml or stop individually: `docker compose stop saga-dynatrace-oneagent`

---

## Environment Variables Reference

### For OTLP and Observability Services

| Variable | Default | Purpose |
|----------|---------|---------|
| OTLP_ENDPOINT | http://otel-collector:4318 | Where services send traces/metrics/logs |
| GRAFANA_ADMIN_PASSWORD | admin | Grafana authentication |
| PROMETHEUS_RETENTION | 15d | How long Prometheus stores metrics |

### For Dynatrace

| Variable | Example | Required |
|----------|---------|----------|
| DT_TENANT | abc12345 | Only if using real Dynatrace |
| DT_API_URL | https://abc12345.live.dynatrace.com | Only if using real Dynatrace |
| DT_API_TOKEN | (API token) | Only if using real Dynatrace |
| DT_HOST_ID | saga-local-dev | Optional; defaults to hostname |

---

## Comparison: Three Layers

| Aspect | Prometheus + Tempo + Grafana | Elasticsearch + Kibana | Dynatrace OneAgent |
|--------|-----|-----|-----|
| **Best for** | Metrics, distributed tracing, correlation | Full-text log search, pattern analysis | Advanced root cause analysis, service dependencies |
| **Data types** | Metrics timeseries, trace spans | Structured & unstructured logs | Everything (traces, logs, metrics, service flow) |
| **Setup** | Fully local, no credentials needed | Fully local, no credentials needed | Requires Dynatrace environment |
| **Storage** | Prometheus (15d), Tempo (24h) | Elasticsearch (indefinite) | Cloud (Dynatrace SaaS) |
| **UI/UX** | Grafana dashboards, trace explorer | Kibana Discover, saved searches | Dynatrace Problems, Smart Alerting |
| **Learning curve** | Medium (Prometheus/Grafana concepts) | Low-Medium (Elasticsearch/Kibana) | Low (Dynatrace auto-discovers) |

### Recommended Workflow

1. **During development**: Use **Grafana** for system overview; use **Kibana** to search specific logs
2. **Debugging issues**: Use **Tempo traces** for exact request flow; use **Kibana** to find related logs
3. **Performance tuning**: Use **Prometheus metrics** to identify bottlenecks; use **Grafana dashboards** for visualization
4. **For production evaluation**: Use **Dynatrace** for advanced root cause analysis and AI-driven insights

---

## FAQ

**Q: What if I see 403 errors from Elasticsearch in the OTLP Collector logs?**  
A: Elasticsearch security is disabled for local dev (xpack.security.enabled=false). Ensure the OTLP Collector config doesn't specify authentication when connecting to Elasticsearch. The config should use `endpoints: [ "http://elasticsearch:9200" ]` without credentials.

**Q: How do I view raw OTLP data?**  
A: The OTLP Collector has a `debug` exporter enabled. Check collector logs:
```bash
docker logs saga-otel-collector | grep -E "(trace|metric|log)"
```

**Q: Can I use Kibana and Grafana at the same time?**  
A: Yes! They serve different purposes:
- **Grafana**: Metrics dashboards and trace exploration (via Tempo)
- **Kibana**: Full-text log search and analysis

**Q: What happens if I stop the Elasticsearch container?**  
A: 
- OTLP Collector will fail to export logs to Elasticsearch but will continue running
- Logs will only be sent to the debug exporter (console)
- Once Elasticsearch restarts, logs will resume flowing to it

**Q: How do I clear all observability data?**  
A: 
```bash
# Stop all containers
docker compose down

# Remove volumes (be careful—this deletes all data!)
docker volume rm saga_elasticsearch_data saga_grafana_data

# Restart
docker compose up
```

**Q: Does OneAgent work without a Dynatrace subscription?**  
A: OneAgent can run with mock credentials, but monitoring will only work with a valid Dynatrace SaaS or Managed environment. Use your 15-day free trial: https://www.dynatrace.com/trial/

---

## Additional Resources

- [OpenTelemetry Collector Documentation](https://opentelemetry.io/docs/collector/)
- [Elasticsearch Documentation](https://www.elastic.co/guide/en/elasticsearch/reference/current/index.html)
- [Kibana User Guide](https://www.elastic.co/guide/en/kibana/current/index.html)
- [Grafana Dashboard Guide](https://grafana.com/docs/grafana/latest/dashboards/)
- [Dynatrace Documentation](https://www.dynatrace.com/support/help/)

---

## Architecture Diagram

```
┌───────────────────────────────────────────────────────────────────┐
│                        Docker Compose Network                     │
│                           (saga-network)                          │
├───────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │              .NET Microservices                          │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │   │
│  │  │ Booking.API  │  │  Flight.API  │  │  Hotel.API   │  │   │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  │   │
│  │  ┌──────────────┐                                       │   │
│  │  │   Car.API    │  + PostgreSQL, RabbitMQ              │   │
│  │  └──────────────┘                                       │   │
│  └──────────────────────┬───────────────────────────────────┘   │
│                         │ OTLP/HTTP (port 4318)                  │
│  ┌──────────────────────▼───────────────────────────────────┐   │
│  │            OTLP Collector                                │   │
│  │  ┌─────────────────────────────────────────────────────┐ │   │
│  │  │ Exporters:                                          │ │   │
│  │  │ • Debug (console)                                   │ │   │
│  │  │ • OTLP/gRPC → Tempo (traces)                        │ │   │
│  │  │ • Elasticsearch HTTP → Elasticsearch (logs)         │ │   │
│  │  └─────────────────────────────────────────────────────┘ │   │
│  └──────────────┬─────────────┬────────────────┬──────────────┘   │
│                 │             │                │                  │
│  ┌──────────────▼──┐  ┌───────▼────────┐      │                  │
│  │ Prometheus      │  │      Tempo     │      │                  │
│  │ • Scrapes /met  │  │ • Trace storage│      │                  │
│  │ • Retention 15d │  │ • Tempo API    │      │                  │
│  │ (port 9090)     │  │ (port 3200)    │      │                  │
│  └────────────┬────┘  └────────┬───────┘      │                  │
│               │                │               │                  │
│  ┌────────────▼────────────────▼───┐          │                  │
│  │        Grafana                  │          │                  │
│  │ • Prometheus datasource         │          │                  │
│  │ • Tempo datasource              │          │                  │
│  │ • Dashboards (5 pre-built)      │          │                  │
│  │ (port 3000)                     │          │                  │
│  └─────────────────────────────────┘          │                  │
│                                               │                  │
│  ┌────────────────────────────────────────────▼─────┐            │
│  │           Elasticsearch                          │            │
│  │ • Indexed logs (logs-YYYY.MM.DD)                │            │
│  │ • Pattern: logs-*                               │            │
│  │ • Persistent volume: elasticsearch_data         │            │
│  │ (port 9200)                                     │            │
│  └────────────────┬─────────────────────────────────┘            │
│                   │ HTTP query API                                │
│  ┌────────────────▼─────────────────────────────────┐            │
│  │              Kibana                              │            │
│  │ • Log discovery & search                        │            │
│  │ • Visualizations & saved searches               │            │
│  │ (port 5601)                                     │            │
│  └─────────────────────────────────────────────────┘            │
│                                                                   │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │     Dynatrace OneAgent (Optional)                       │    │
│  │ • Docker socket access: /var/run/docker.sock           │    │
│  │ • Privileged mode: true                               │    │
│  │ • Sends metrics → Dynatrace Cloud                      │    │
│  │ (requires DT_TENANT, DT_API_URL, DT_API_TOKEN)         │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                   │
└───────────────────────────────────────────────────────────────────┘
```

---

**Last Updated**: February 2026  
**Maintainer**: DevOps / Platform Team
