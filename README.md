kubectl port-forward \
  -n monitoring \
  svc/otel-collector-opentelemetry-collector \
  4317:4317 \
  4318:4318

// for nfs server
mkdir -p /data/prometheus/prometheus-db
chown -R 1000:2000 /data/prometheus/prometheus-db
chmod -R 775 /data/prometheus/prometheus-db