kubectl port-forward \
  -n monitoring \
  svc/otel-collector-opentelemetry-collector \
  4317:4317 \
  4318:4318

// for nfs server
sudo mkdir -p /data/prometheus
sudo chown -R 10001:10001 /data/prometheus
sudo chmod -R 775 /data/prometheus