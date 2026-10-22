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
  - Also apply the same naming convention to the test projects
- Run the application by running the Main project.
- Use CSharpier with `.csharpierrc` and `printWidth: 80`.
- Add `dotnet-tools.json` at the repository root.
- Use xUnit for unit tests and ArchUnitNET for architecture tests.

## Check

- Verify that all dotnet projects are named correctly according to the naming convention.
- Verify that the `dotnet-tools.json` file is present at the repository root.
- Verify that the `.csharpierrc` file is present at the repository root and has `printWidth: 80`.
- Verify that the `Main` project is a Web project and the application entrypoint.
- Verify that the `Main` project can run the application.
- Verify that xUnit is used for unit tests and ArchUnitNET is used for architecture tests.

