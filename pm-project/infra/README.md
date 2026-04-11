# Observability infra

This folder contains docker-compose and provisioning for a local observability stack used by the PM.API service.

Services (started by `docker compose up -d`):
- `prometheus` (http://localhost:9090) — scrapes `/metrics` exposed by `pm-api`.
- `elasticsearch` (http://localhost:9200) — stores logs from Serilog.
- `kibana` (http://localhost:5601`) — UI for Elasticsearch.
- `grafana` (http://localhost:3000) — dashboard (provisioned datasources + dashboards).
- `pm-api` (http://localhost:5130) — the API under observation.

How to run

1. From this folder run:

```bash
docker compose up -d
```

2. Confirm services are healthy:

```bash
docker compose ps
curl http://localhost:9090
curl http://localhost:9200
curl http://localhost:3000 (use credentials from .env)
```

3. Generate traffic to the API to populate metrics and logs:

```bash
curl -X POST http://localhost:5130/api/auth/register -H "Content-Type: application/json" -d '{"username":"alice","email":"a@e.com","password":"P@ssw0rd"}'
```

Grafana
- Dashboards are provisioned from `grafana/provisioning/dashboards`.
- Datasources are provisioned from `grafana/provisioning/datasources`.

Notes
- Prometheus scrape job `pm-api` targets `pm-api:5130` (service name inside the compose network). If running Prometheus outside docker, update `prometheus.yml` to target `host.docker.internal:5130` or the correct host.
- The Grafana dashboard queries were adapted to the metric names emitted by the application (prometheus-net / ASP.NET metrics). If you add custom instrumentation (OpenTelemetry), update the dashboard accordingly.
