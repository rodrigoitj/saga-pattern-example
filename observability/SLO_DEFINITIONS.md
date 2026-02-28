# SLO Definitions — Saga Pattern Booking Platform

This document defines Service Level Objectives (SLOs) for the booking platform.
All SLIs reference metrics instrumented in the codebase and queryable via Prometheus.

---

## 1. API Availability (per service)

| Property        | Value |
|-----------------|-------|
| **SLI**         | Proportion of non-5xx HTTP responses |
| **SLO Target**  | 99.9% over a 30-day rolling window |
| **Error Budget** | 0.1% ≈ 43.2 minutes/month |
| **PromQL (SLI)** | `1 - job:http_server_error_rate5m:ratio{job="<service>"}` |
| **Alert**       | `HighErrorRate` fires at > 5% (burn-rate proxy) |

**Applies to:** `booking-api`, `flight-api`, `hotel-api`, `car-api`

### Multi-Window Burn Rate (recommended)

For SLO-based alerting, use a multi-window, multi-burn-rate approach:

```promql
# Fast burn — 1h window, 14.4x budget consumption
(
  sum(rate(http_server_request_duration_seconds_count{job="booking-api", http_response_status_code=~"5.."}[1h]))
  /
  sum(rate(http_server_request_duration_seconds_count{job="booking-api"}[1h]))
) > (14.4 * 0.001)
AND
(
  sum(rate(http_server_request_duration_seconds_count{job="booking-api", http_response_status_code=~"5.."}[5m]))
  /
  sum(rate(http_server_request_duration_seconds_count{job="booking-api"}[5m]))
) > (14.4 * 0.001)
```

---

## 2. API Latency (per service)

| Property        | Value |
|-----------------|-------|
| **SLI**         | p95 HTTP response duration |
| **SLO Target**  | p95 < 500ms for 99% of 5-minute windows |
| **PromQL (SLI)** | `job:http_server_request_duration_seconds:p95{job="<service>"}` |
| **Alert**       | `HighLatencyP99` (warning at p99 > 2s), `HighLatencyP95Critical` (critical at p95 > 5s) |

**Applies to:** `booking-api`, `flight-api`, `hotel-api`, `car-api`

---

## 3. Booking Saga Success Rate

| Property        | Value |
|-----------------|-------|
| **SLI**         | Ratio of confirmed to created bookings |
| **SLO Target**  | ≥ 95% over a 30-day rolling window |
| **Error Budget** | 5% — allows for expected business failures (no availability, etc.) |
| **PromQL (SLI)** | `saga:booking_success_rate5m:ratio` |
| **Alert**       | `HighBookingFailureRate` fires at > 10% failure rate |

### Notes
- Distinguish between **technical failures** (infrastructure, timeouts) and **business failures** (no seats available). The 95% target accounts for expected business rejections.
- For stricter monitoring of technical-only failures, add a `reason` label to `booking_failed_total` and filter to `reason!="business"`.

---

## 4. Booking Saga Duration

| Property        | Value |
|-----------------|-------|
| **SLI**         | p95 end-to-end saga completion time |
| **SLO Target**  | p95 < 10 seconds |
| **PromQL (SLI)** | `saga:booking_creation_duration_seconds:p95` |
| **Alert**       | `SagaDurationHigh` fires at p95 > 30s |

### Notes
- Saga involves 3 parallel steps (flight + hotel + car). The p95 target is set at the overall saga, not individual steps.
- Individual step p95 targets: < 5s each (`FlightProcessingDurationHigh`, etc. fire at > 10s).

---

## 5. Sub-Service Reservation Success Rate

| Property        | Value |
|-----------------|-------|
| **SLI**         | Confirmed / Created ratio per sub-service |
| **SLO Target**  | ≥ 90% per sub-service |
| **PromQL (SLI)** | `saga:flight_success_rate5m:ratio`, `saga:hotel_success_rate5m:ratio`, `saga:car_success_rate5m:ratio` |
| **Alert**       | `FlightReservationFailureRate`, `HotelReservationFailureRate`, `CarRentalFailureRate` — fire at > 15% failure |

**Applies to:** Flight, Hotel, Car services

---

## 6. Messaging Reliability (Outbox)

| Property        | Value |
|-----------------|-------|
| **SLI**         | Proportion of outbox messages published without failure |
| **SLO Target**  | 99.99% publish success rate |
| **Error Budget** | 0.01% |
| **PromQL (SLI)** | `1 - (rate(messaging_outbox_publish_failed_total[5m]) / clamp_min(rate(messaging_outbox_published_total[5m]), 0.001))` |
| **Alert**       | `OutboxPublishFailures` fires on any failure rate > 0 |

### Notes
- The outbox pattern guarantees at-least-once delivery. Failures at this layer are infrastructure issues (RabbitMQ down, network partition) and should be treated as critical.
- `OutboxBacklog` alert detects growing gap between enqueued and published messages.

---

## Error Budget Policy

| Remaining Budget | Action |
|-----------------|--------|
| > 50%           | Normal development velocity. Feature work proceeds. |
| 25% – 50%      | Increased caution. All changes require extra review. Prioritize reliability improvements. |
| 10% – 25%      | Feature freeze. Focus exclusively on reliability. Postmortem required for any further budget consumption. |
| < 10%           | Emergency. Roll back recent changes. All hands on reliability. No deployments except fixes. |

---

## Dashboard Cross-Reference

| SLO | Dashboard | Panel |
|-----|-----------|-------|
| API Availability | 1 - System Overview | Global Error Rate %, Error Rate % per Service |
| API Latency | 2 - Service Deep Dive | Latency p50/p95/p99 |
| Booking Success | 1 - System Overview | Booking Success Rate |
| Saga Duration | 3 - Business Metrics | Saga Duration p50/p95/p99 |
| Sub-Service Success | 3 - Business Metrics | Sub-Service Success Rate % |
| Messaging Reliability | 1 - System Overview | Outbox Pipeline, Inbox Pipeline |

---

## Metric Inventory (validated against codebase)

### Auto-Instrumented (OpenTelemetry)
- `http_server_request_duration_seconds{method, http_route, http_response_status_code}` — histogram
- `http_server_active_requests` — gauge
- `http_client_request_duration_seconds` — histogram
- `process_runtime_dotnet_gc_collections_count_total{generation}` — counter
- `process_runtime_dotnet_gc_heap_size_bytes{generation}` — gauge
- `process_runtime_dotnet_threadpool_queue_length` — gauge

### Custom — Booking (meter: SagaPattern.Booking)
- `booking_created_total` — counter
- `booking_confirmed_total` — counter
- `booking_failed_total{reason}` — counter
- `booking_step_completed_total{step_type}` — counter
- `booking_cancelled_total` — counter
- `booking_creation_duration_seconds` — histogram
- `booking_revenue_total` — counter (double)
- `booking_inflight` — up-down counter

### Custom — Flight (meter: SagaPattern.Flight)
- `flight_reservations_created_total` — counter
- `flight_reservations_confirmed_total` — counter
- `flight_reservations_cancelled_total` — counter
- `flight_reservations_failed_total{reason}` — counter
- `flight_reservation_processing_duration_seconds` — histogram
- `flight_revenue_total` — counter (double)
- `flight_reservations_active` — up-down counter

### Custom — Hotel (meter: SagaPattern.Hotel)
- `hotel_reservations_created_total` — counter
- `hotel_reservations_confirmed_total` — counter
- `hotel_reservations_cancelled_total` — counter
- `hotel_reservations_failed_total{reason}` — counter
- `hotel_reservation_processing_duration_seconds` — histogram
- `hotel_revenue_total` — counter (double)
- `hotel_reservations_active` — up-down counter

### Custom — Car (meter: SagaPattern.Car)
- `car_rentals_created_total` — counter
- `car_rentals_confirmed_total` — counter
- `car_rentals_cancelled_total` — counter
- `car_rentals_failed_total{reason}` — counter
- `car_rental_processing_duration_seconds` — histogram
- `car_revenue_total` — counter (double)
- `car_rentals_active` — up-down counter

### Custom — Messaging (meter: SagaPattern.Messaging)
- `messaging_outbox_enqueued_total` — counter
- `messaging_outbox_published_total` — counter
- `messaging_outbox_publish_failed_total` — counter
- `messaging_outbox_publish_duration_ms` — histogram
- `messaging_inbox_consumed_total` — counter
- `messaging_inbox_duplicate_skipped_total` — counter
- `messaging_inbox_consume_duration_ms` — histogram
