# 🚀 Deployment Guide

## Development

```bash
dotnet run --project src/Project.Api
```

Or:

```bash
docker-compose up --build
```

## Production

- Setup environment variables
- TLS termination via reverse proxy

### Manual:

```bash
dotnet publish -c Release -o out
scp -r ./out user@server:/var/www/app
```

### CI/CD

Handled by internal pipelines.