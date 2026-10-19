src/
|-- Domain/
|   |-- AggregateName(plural)/
|   |   |-- Events/
|   |   |-- ValueObjects/
|   |   | AggregateName(file)
|   |   | AggregateNameRepository(file)
|   |   | AggregateNameAsserts(file)
|   |   | SomeEntity1(file)
|   |   |-- SomeEntity2(file)
|   |-- DomainServices1(file)

`-- Main/
    |-- Modules/
    |   |-- Application/
    |   |-- BackgroundProcesses/
    |   |-- IAM/
    |   |-- Messaging/
    |   |   |-- DomainEvents/
    |   |   `-- IntegrationEvents/
    |   |-- Persistence/
    |   |   |-- Read/
    |   |   |   `-- Dapper/
    |   |   `-- Write/
    |   |       `-- EntityFramework/
    |   `-- WebApi/
    `-- Properties/