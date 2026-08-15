# CI/CD Pipeline Documentation

## Overview

This CI/CD pipeline automatically builds and deploys Docker images for microservices that have changed in a commit. It implements a GitOps workflow using GitHub Actions and ArgoCD.

## How It Works

### Architecture

```
┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
│   Git Push      │──────▶│  Detect Job    │──────▶│  Build Job      │
│  (develop/main) │      │  (find changes) │      │  (docker build) │
└─────────────────┘      └─────────────────┘      └─────────────────┘
                                                           │
                                                           ▼
                                                  ┌─────────────────┐
                                                  │  Docker Hub     │
                                                  │  (push images)  │
                                                  └─────────────────┘
                                                           │
                                                           ▼
┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐
│   ArgoCD       │◀─────│  Update Helm    │◀─────│  Update Job     │
│  (sync env)    │      │  (update values) │      │  (update yaml)  │
└─────────────────┘      └─────────────────┘      └─────────────────┘
```

### Workflow Breakdown

#### 1. **Detect Job** ([detect-and-build.yml:14](detect-and-build.yml#L14-L63))

**Purpose**: Identify which services have changed

**Process**:
- Compares current commit with previous commit: `git diff HEAD~1 HEAD`
- Checks for file changes in service directories:
  - `src/ApiGateway/` → `api-gateway`
  - `src/Services/PaymentProcessing/` → `payment-processing`
  - `src/Services/PaymentRouter/` → `payment-router`
  - `src/Services/RiskAssessment/` → `risk-assessment`
  - `src/Services/BankAdapter/` → `bank-adapter`
- Outputs a JSON array of changed services

**Environment Detection**:
- `develop` branch → `dev` environment
- `main` branch → `prod` environment

**Tag Generation**:
- Creates short Git SHA: `git rev-parse --short HEAD`
- Example: `a1b2c3d`

#### 2. **Build Job** ([detect-and-build.yml:65](detect-and-build.yml#L65-L147))

**Purpose**: Build and push Docker images

**Matrix Strategy**:
- Runs in parallel for each changed service
- `fail-fast: false` ensures one failure doesn't stop others

**Process**:
1. **Docker Login**: Authenticates with Docker Hub
2. **Set Dockerfile Path**: Maps service name to Dockerfile location
3. **Validate**: Checks if Dockerfile exists
4. **Build & Push**: Builds image with proper context and pushes to Docker Hub

**Image Tags**:
- Specific tag: `docker.io/username/service:a1b2c3d`
- Latest tag: `docker.io/username/service:latest`

#### 3. **Update Helm Job** ([update-helm.yml:24](update-helm.yml#L24-L99))

**Purpose**: Update Helm values to trigger ArgoCD deployment

**Process**:
1. Install `yq` tool for YAML manipulation
2. Update image.repository and image.tag in values files
3. Commit changes back to repository
4. Push to trigger ArgoCD sync

**Values Files**:
- Dev: `deployments/argo-apps/values/app-dev/{service}.yaml`
- Prod: `deployments/argo-apps/values/app-prod/{service}.yaml`

## Prerequisites

### Required GitHub Secrets

You must configure these in your GitHub repository settings (`Settings` → `Secrets and variables` → `Actions`):

| Secret | Description | Example |
|--------|-------------|---------|
| `DOCKERHUB_USERNAME` | Your Docker Hub username | `myorg` |
| `DOCKERHUB_TOKEN` | Docker Hub access token | `dckr_pat_...` |

#### Creating Docker Hub Token:

1. Go to [Docker Hub Settings](https://hub.docker.com/settings/security)
2. Click "New Access Token"
3. Give it a descriptive name (e.g., "github-actions")
4. Select permissions: Read, Write, Delete
5. Copy the token immediately (it won't be shown again)
6. Add it to GitHub secrets

### Required Git Branches

Ensure these branches exist:

```bash
# Create and push branches if they don't exist
git checkout -b develop
git push -u origin develop

git checkout -b main
git push -u origin main
```

### ArgoCD Setup (Required for deployment)

Your cluster should have:

1. **ArgoCD installed** in the cluster
2. **Application manifests** pointing to your values files
3. **Auto-sync enabled** or configured to sync on Git changes

Example ArgoCD Application:

```yaml
apiVersion: argoproj.io/v1alpha1
kind: Application
metadata:
  name: payment-system-dev
  namespace: argocd
spec:
  project: default
  source:
    repoURL: https://github.com/your-org/payment-system.git
    targetRevision: develop
    path: deployments/argo-apps
  destination:
    server: https://kubernetes.default.svc
    namespace: payment-system-dev
  syncPolicy:
    automated:
      prune: true
      selfHeal: true
    syncOptions:
      - CreateNamespace=true
```

### Directory Structure Verification

Ensure your repository has this structure:

```
payment-system/
├── .github/
│   └── workflows/
│       ├── detect-and-build.yml
│       └── update-helm.yml
├── src/
│   ├── ApiGateway/
│   │   └── Dockerfile
│   └── Services/
│       ├── PaymentProcessing/
│       │   └── Dockerfile
│       ├── PaymentRouter/
│       │   └── Dockerfile
│       ├── RiskAssessment/
│       │   └── Dockerfile
│       └── BankAdapter/
│           └── Dockerfile
└── deployments/
    └── argo-apps/
        └── values/
            ├── app-dev/
            │   ├── api-gateway.yaml
            │   ├── payment-processing.yaml
            │   ├── payment-router.yaml
            │   ├── risk-assessment.yaml
            │   └── bank-adapter.yaml
            └── app-prod/
                ├── api-gateway.yaml
                ├── payment-processing.yaml
                ├── payment-router.yaml
                ├── risk-assessment.yaml
                └── bank-adapter.yaml
```

### Helm Values File Format

Each service's values file should have this structure:

```yaml
# deployments/argo-apps/values/app-dev/api-gateway.yaml
image:
  repository: docker.io/your-org/api-gateway
  tag: a1b2c3d

# Other service configuration...
replicas: 1
resources:
  limits:
    cpu: 500m
    memory: 512Mi
```

## Running the Pipeline

### Automatic Execution

The pipeline runs automatically when you push to:

- `develop` branch → deploys to dev environment
- `main` branch → deploys to prod environment

### Manual Execution

1. Go to `Actions` tab in GitHub
2. Select "Build Changed Services" workflow
3. Click "Run workflow"
4. Select branch (develop or main)
5. Click "Run workflow"

### Example Workflow

```bash
# Make changes to a service
echo "// New feature" >> src/Services/PaymentProcessing/app.js

# Commit and push
git add src/Services/PaymentProcessing/app.js
git commit -m "feat: add new feature"
git push origin develop

# Pipeline automatically:
# 1. Detects PaymentProcessing changed
# 2. Builds docker.io/your-org/payment-processing:a1b2c3d
# 3. Updates deployments/argo-apps/values/app-dev/payment-processing.yaml
# 4. ArgoCD syncs the change to your cluster
```

## Troubleshooting

### Pipeline fails at Docker Login

**Issue**: Invalid Docker Hub credentials

**Solution**:
1. Verify `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` secrets are set
2. Generate a new token from Docker Hub
3. Update the secret in GitHub

### Pipeline fails at "Validate Dockerfile Exists"

**Issue**: Dockerfile not found at expected path

**Solution**:
- Ensure Dockerfiles exist at:
  - `src/ApiGateway/Dockerfile`
  - `src/Services/{Service}/Dockerfile`

### Helm values file not found

**Issue**: Values file missing

**Solution**:
- Ensure values files exist at:
  - `deployments/argo-apps/values/app-dev/{service}.yaml`
  - `deployments/argo-apps/values/app-prod/{service}.yaml`

### Changes not deploying to cluster

**Issue**: ArgoCD not syncing

**Solution**:
1. Check ArgoCD application status: `argocd app get payment-system-dev`
2. Verify ArgoCD can access your GitHub repository
3. Check ArgoCD logs: `kubectl logs -n argocd deployment/argocd-server`
4. Manually sync: `argocd app sync payment-system-dev`

## Security Considerations

- Docker Hub tokens should be scoped to only the necessary repositories
- Use separate Docker Hub organizations for dev/prod if possible
- Enable branch protection rules on `main` branch
- Review ArgoCD permissions and RBAC configuration

## Monitoring

- Check GitHub Actions logs for build failures
- Monitor ArgoCD dashboard for sync status
- Set up alerts for deployment failures
- Review image tags in values files for consistency

## Next Steps

1. [ ] Configure GitHub Secrets (DOCKERHUB_USERNAME, DOCKERHUB_TOKEN)
2. [ ] Ensure branches exist (develop, main)
3. [ ] Verify directory structure matches requirements
4. [ ] Set up ArgoCD in your cluster
5. [ ] Create ArgoCD Application manifests
6. [ ] Test pipeline with a sample commit
7. [ ] Configure monitoring and alerts
