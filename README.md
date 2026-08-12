kubectl port-forward \
  -n monitoring \
  svc/otel-collector-opentelemetry-collector \
  4317:4317 \
  4318:4318