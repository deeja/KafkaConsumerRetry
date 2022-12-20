# Changelog

## Unreleased

### Changed
- README.md
- TestConsole: added logging and cancellation of tasks
- ExampleProject: Various bits and pieces


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
  