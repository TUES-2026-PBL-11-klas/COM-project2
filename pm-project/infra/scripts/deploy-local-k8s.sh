#!/usr/bin/env bash
set -euo pipefail

WORKDIR="$(cd "$(dirname "$0")/.." && pwd)"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
K8S_DIR="$WORKDIR/k8s"
SERVER_DIR="$WORKDIR/../server"

print(){ echo "[deploy] $*"; }

command_exists(){ command -v "$1" >/dev/null 2>&1; }

port_forward(){
  print "Starting port-forward(s)..."
  PM_API_LOCAL_PORT=${PM_API_LOCAL_PORT:-5130}
  POSTGRES_LOCAL_PORT=${POSTGRES_LOCAL_PORT:-5432}
  ALLOY_OTLP_LOCAL_PORT=${ALLOY_OTLP_LOCAL_PORT:-4317}
  POSTGRES_EXPORTER_LOCAL_PORT=${POSTGRES_EXPORTER_LOCAL_PORT:-9187}

  # detect service targetPort (fallback to 5130)
  SERVICE_PORT=$(kubectl get svc pm-api -n pm-project -o jsonpath='{.spec.ports[0].port}' 2>/dev/null || echo "5130")
  TARGET_PORT=$(kubectl get svc pm-api -n pm-project -o jsonpath='{.spec.ports[0].targetPort}' 2>/dev/null || echo "$SERVICE_PORT")
  if [ -z "$TARGET_PORT" ]; then TARGET_PORT=5130; fi

  # warn if service has no endpoints (no ready pods)
  ENDPTS=$(kubectl get endpoints pm-api -n pm-project -o jsonpath='{.subsets}' 2>/dev/null || echo "")
  if [ -z "$ENDPTS" ] || [ "$ENDPTS" = "[]" ]; then
    print "Warning: service 'pm-api' has no endpoints or pods are not ready — port-forward may fail until pod is ready"
  fi

  if [ "${PM_API_FOREGROUND:-0}" -eq 1 ]; then
    print "Port-forwarding pm-api -> local ${PM_API_LOCAL_PORT}:${TARGET_PORT} (foreground - will block)"
    kubectl -n pm-project port-forward svc/pm-api ${PM_API_LOCAL_PORT}:${TARGET_PORT}
  else
    (kubectl -n pm-project port-forward svc/pm-api ${PM_API_LOCAL_PORT}:${TARGET_PORT} >/dev/null 2>&1 &) || true
    PF_PMAPI=$!
  fi

  (kubectl -n pm-project port-forward svc/postgres-db ${POSTGRES_LOCAL_PORT}:5432 >/dev/null 2>&1 &) || true
  PF_PG=$!
  (kubectl -n pm-project port-forward svc/grafana-alloy ${ALLOY_OTLP_LOCAL_PORT}:4317 >/dev/null 2>&1 &) || true
  PF_ALLOY=$!
  (kubectl -n pm-project port-forward svc/postgres-exporter ${POSTGRES_EXPORTER_LOCAL_PORT}:9187 >/dev/null 2>&1 &) || true
  PF_PGEXP=$!

  sleep 1
  print "Port-forwards started (PIDs: ${PF_PMAPI} ${PF_PG} ${PF_ALLOY} ${PF_PGEXP})"
  print "pm-api -> http://127.0.0.1:${PM_API_LOCAL_PORT}"
  print "postgres -> 127.0.0.1:${POSTGRES_LOCAL_PORT}"
  print "alloy OTLP -> 127.0.0.1:${ALLOY_OTLP_LOCAL_PORT}"
  print "postgres exporter -> 127.0.0.1:${POSTGRES_EXPORTER_LOCAL_PORT}"
  print "To stop port-forwards: kill ${PF_PMAPI} ${PF_PG} ${PF_ALLOY} ${PF_PGEXP}"
}

ensure_kind(){
  if command_exists kind; then
    print "kind present"
    return 0
  fi
  if command_exists brew; then
    print "Installing kind via brew..."
    brew install kind
    return 0
  fi
  print "kind not found and Homebrew not available. Will try to proceed with current kube-context (Docker Desktop)"
  return 1
}

USE_KIND=0
if command_exists kind; then
  USE_KIND=1
else
  ensure_kind && USE_KIND=1 || USE_KIND=0
fi

if [ "$USE_KIND" -eq 1 ]; then
  print "Using kind cluster. Creating cluster 'pm' if needed..."
  if ! kind get clusters | grep -q "^pm$"; then
    cat > /tmp/kind-config.yaml <<'EOF'
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
containerdConfigPatches:
- |-
  [plugins."io.containerd.grpc.v1.cri".registry.mirrors."localhost:5000"]
    endpoint = ["http://kind-registry:5000"]
nodes:
- role: control-plane
  extraPortMappings:
  - containerPort: 80
    hostPort: 8081
    protocol: TCP
EOF
    print "Creating kind cluster 'pm'..."
    kind create cluster --name pm --config /tmp/kind-config.yaml
  else
    print "kind cluster 'pm' already exists"
  fi
fi

print "Building pm-api image..."
cd "$SERVER_DIR"
docker build -t alexnay/pm-api:latest -f PM.API/Dockerfile .

if [ "$USE_KIND" -eq 1 ]; then
  print "Loading image into kind..."
  kind load docker-image alexnay/pm-api:latest --name pm || true
else
  print "Not using kind; assuming cluster can access local Docker images (Docker Desktop)."
fi

if [ -f "$SCRIPT_DIR/.env" ]; then
  print "Loading environment from $SCRIPT_DIR/.env"
  set -a
  source "$SCRIPT_DIR/.env"
  set +a
elif [ -f "$K8S_DIR/.env" ]; then
  print "Loading environment from $K8S_DIR/.env"
  set -a
  source "$K8S_DIR/.env"
  set +a
fi

cd "$K8S_DIR"
print "Rendering secret manifest..."
export JWT_KEY JWT_ISSUER JWT_AUDIENCE GRAFANA_API_KEY GRAFANA_OTLP_URL GRAFANA_BASIC_AUTH OTEL_SERVICE_NAME LOKI_URL LOKI_USERNAME LOKI_PASSWORD LOKI_BASIC_AUTH
envsubst < pm-api-secret.yaml.template > pm-api-secret.yaml

if [ "${PORT_FORWARD_ONLY:-0}" -eq 1 ]; then
  print "PORT_FORWARD_ONLY=1 set — skipping apply/helm/logs and starting port-forward only"
  port_forward
  exit 0
fi

print "Applying manifests..."
kubectl apply -f "$K8S_DIR/namespace.yaml"
kubectl apply -f "$K8S_DIR/pm-api-secret.yaml"
kubectl apply -f "$K8S_DIR/postgres.yaml"
kubectl apply -f "$K8S_DIR/cassandra.yaml"
kubectl apply -f "$K8S_DIR/postgres-exporter.yaml"
kubectl apply -f "$K8S_DIR/alloy-configmap.yaml"
kubectl apply -f "$K8S_DIR/alloy-deployment.yaml"
kubectl apply -f "$K8S_DIR/alloy-service.yaml"
kubectl apply -f "$K8S_DIR/pm-api-deployment.yaml"
kubectl apply -f "$K8S_DIR/pm-api-service.yaml" || true
kubectl apply -f "$K8S_DIR/pm-api-ingress.yaml" || true

if command_exists helm; then
  print "Helm detected — installing monitoring stack (kube-prometheus-stack) if not present..."
  helm repo add prometheus-community https://prometheus-community.github.io/helm-charts || true
  helm repo update || true
  if ! helm status monitoring -n pm-project >/dev/null 2>&1; then
    helm install monitoring prometheus-community/kube-prometheus-stack --namespace pm-project --create-namespace || true
  else
    print "monitoring release already installed"
  fi
else
  print "helm not found — skipping monitoring install (Prometheus/Grafana). Install helm to enable this."
fi

print "Waiting for key deployments to become ready (may take a moment)..."
kubectl -n pm-project rollout status deploy/postgres-db --timeout=120s || true
kubectl -n pm-project rollout status deploy/cassandra-db --timeout=120s || true
kubectl -n pm-project rollout status deploy/postgres-exporter --timeout=120s || true
kubectl -n pm-project rollout status deploy/grafana-alloy --timeout=120s || true
kubectl -n pm-project rollout status deploy/pm-api --timeout=120s || true

print "Pods in namespace pm-project:"
kubectl get pods -n pm-project -o wide || true

kubectl -n pm-project port-forward svc/pm-api 5130:5130

print "Deployment complete"
