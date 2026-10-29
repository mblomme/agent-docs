# .NET

The architecture documents are mostly technology agnostic. This documents makes it concrete for the .NET technology stack. It describes how to implement the architecture in .NET.

## Guardrails
- Each layer is a separate .NET class library project (`classlib`).
- `Main` is a Web project (`web`) and the application entrypoint.
- Dotnet projects should be named {CompanyName}.{BoundedContext}.{Layer}, e.g. `Acme.Inventory.Domain`.
  - e.g. src/
    - Acme.Inventory.Domain/
    - Acme.Inventory.Application/
    - Acme.Inventory.Infrastructure/
    - Acme.Inventory.Main/
  - CompanyName is optional
- Dotnet projects in the tests folder follow the same the same naming convention as above.
- Run the application by running the Main project.
- Use xUnit for unit tests and ArchUnitNET for architecture tests.

## Check

- Verify that all dotnet projects in the `src` and `tests` folder are named correctly according to the naming convention.
- Verify that the `Main` project is a Web project and the application entrypoint.
- Verify that the `Main` project can run the application.
- Verify that xUnit is used for unit tests and ArchUnitNET is used for architecture tests.

