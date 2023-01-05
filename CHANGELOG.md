# Changelog

## Unreleased

### Fixed
### Changed

### Removed

### Added 
- Stryker mutation testing
- Github workflow
- Classes that produce the kafka configs, consumers and consumer builders. Was to tight and needed splitting for testing.


## [0.2.1]

### Removed
- `docker-compose.test.yaml` as it was out of date
### Changed
- README.md
- TestConsole: added logging and cancellation of tasks + general maintenance 
- ExampleProject: Various bits and pieces
- Moved as much as of the initialisation as possible of `PartitionProcessor` into a new `IPartitionProcessorFactory`
- Created abstract classes from a few of the core classes; created `sealed` versions of those classes 

### Added 
- CHANGELOG.md
- ByteHandler example
- `MultiplyingBackOffCalculator` setting to `AddKafkaConsumerRetry()`

### Fixed
- `IDelayCalculator` returns a `DateTimeOffset` rather than a `TimeSpan`; timing could be wrong when value was eventually used.



## [0.1.4] - 2022-12-15

### Changed
- README.md

## [0.0.0] -> [0.1.3]

### Added
- Everything
  