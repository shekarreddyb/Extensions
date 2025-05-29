# 🧪 Testing Strategy

## Unit Tests

```bash
dotnet test tests/Project.UnitTests
```

## Integration

```bash
docker-compose up -d
dotnet test tests/Project.IntegrationTests
```

## Code Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```