kubectl port-forward \
  -n monitoring \
  svc/otel-collector-opentelemetry-collector \
  4317:4317 \
  4318:4318

// for nfs server
mkdir -p /data/prometheus/prometheus-db
chown -R 1000:2000 /data/prometheus/prometheus-db | nobody:nogroup
chmod -R 775 /data/prometheus/prometheus-db

helm install argo-apps . -n argo-cd -f values-dev.yaml

helm upgrade argo-apps . \
  -n argo-cd \
  -f values-dev.yaml

helm template argo-apps . \
  -n argo-cd \
  -f values-dev.yaml

helm uninstall argo-apps -n argo-cd