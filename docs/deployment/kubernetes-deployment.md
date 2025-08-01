# Kubernetes Deployment Guide

This guide covers deploying the DevOps MCP Server to a Kubernetes cluster.

## Prerequisites

- Kubernetes cluster (1.24+)
- kubectl configured
- Helm 3 (optional)
- cert-manager (for TLS)
- NGINX Ingress Controller

## Quick Start

### 1. Create Namespace and Secrets

```bash
# Create namespace
kubectl create namespace devops-mcp

# Create secrets
kubectl create secret generic devops-mcp-secrets \
  --namespace devops-mcp \
  --from-literal=azure-devops-org-url=https://dev.azure.com/YOUR_ORG \
  --from-literal=azure-devops-pat=YOUR_PAT \
  --from-literal=redis-connection-string=redis://redis:6379
```

### 2. Deploy Using Kustomize

```bash
# Deploy base configuration
kubectl apply -k k8s/base

# Or deploy with environment-specific overlay
kubectl apply -k k8s/overlays/prod
```

### 3. Verify Deployment

```bash
# Check pods
kubectl -n devops-mcp get pods

# Check services
kubectl -n devops-mcp get svc

# Check ingress
kubectl -n devops-mcp get ingress
```

## Configuration

### Resource Limits

Edit `k8s/base/deployment.yaml` to adjust resource limits:

```yaml
resources:
  requests:
    cpu: 100m
    memory: 128Mi
  limits:
    cpu: 500m
    memory: 512Mi
```

### Scaling

#### Manual Scaling
```bash
kubectl -n devops-mcp scale deployment devops-mcp --replicas=5
```

#### Auto Scaling
```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: devops-mcp
  namespace: devops-mcp
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: devops-mcp
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

### TLS Configuration

1. Install cert-manager:
```bash
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml
```

2. Create ClusterIssuer:
```yaml
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: your-email@example.com
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
```

3. Update ingress with your domain:
```yaml
spec:
  tls:
  - hosts:
    - devops-mcp.yourdomain.com
    secretName: devops-mcp-tls
```

## Monitoring

### Prometheus Integration

1. Add ServiceMonitor:
```yaml
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: devops-mcp
  namespace: devops-mcp
spec:
  selector:
    matchLabels:
      app.kubernetes.io/name: devops-mcp
  endpoints:
  - port: http
    path: /metrics
    interval: 30s
```

2. Configure Grafana dashboards (see `monitoring/grafana/dashboards/`)

### Health Checks

The deployment includes liveness and readiness probes:

- **Liveness**: `/health` - Ensures the container is running
- **Readiness**: `/health/ready` - Ensures the service is ready to accept traffic

## Troubleshooting

### Check Logs
```bash
# View logs for all pods
kubectl -n devops-mcp logs -l app.kubernetes.io/name=devops-mcp

# View logs for specific pod
kubectl -n devops-mcp logs devops-mcp-7d8b9c6f5-abc123

# Follow logs
kubectl -n devops-mcp logs -f deployment/devops-mcp
```

### Debug Pod
```bash
# Get pod details
kubectl -n devops-mcp describe pod devops-mcp-7d8b9c6f5-abc123

# Execute into pod
kubectl -n devops-mcp exec -it devops-mcp-7d8b9c6f5-abc123 -- /bin/sh
```

### Common Issues

1. **ImagePullBackOff**
   - Check image name and tag
   - Verify registry credentials if using private registry

2. **CrashLoopBackOff**
   - Check logs for startup errors
   - Verify environment variables and secrets
   - Check resource limits

3. **Service Unavailable**
   - Verify ingress configuration
   - Check service endpoints
   - Ensure pods are in Ready state

## Security Best Practices

1. **Use Network Policies**
```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: devops-mcp
  namespace: devops-mcp
spec:
  podSelector:
    matchLabels:
      app.kubernetes.io/name: devops-mcp
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ingress-nginx
    ports:
    - protocol: TCP
      port: 8080
```

2. **Pod Security Standards**
   - Run as non-root user
   - Read-only root filesystem
   - Drop all capabilities

3. **Secret Management**
   - Use Sealed Secrets or External Secrets Operator
   - Rotate credentials regularly
   - Never commit secrets to Git

## Backup and Recovery

### Backup Redis Data
```bash
# Create backup
kubectl -n devops-mcp exec redis-0 -- redis-cli BGSAVE

# Copy backup file
kubectl -n devops-mcp cp redis-0:/data/dump.rdb ./redis-backup.rdb
```

### Disaster Recovery
1. Maintain infrastructure as code
2. Regular backups of persistent data
3. Document recovery procedures
4. Test recovery process regularly