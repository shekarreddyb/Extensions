configurations.all {
    resolutionStrategy.eachDependency { DependencyResolveDetails details ->
        if (details.requested.group == 'com.fasterxml.jackson.core' && details.requested.name == 'jackson-core') {
            details.useVersion 'YOUR_DESIRED_VERSION'
        }
        if (details.requested.group == 'org.yaml' && details.requested.name == 'snakeyaml') {
            details.useVersion 'YOUR_DESIRED_VERSION'
        }
    }
}
