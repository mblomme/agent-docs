# .NET

- Each layer is a separate .NET class library project (`classlib`).
- Main is a Web project (`web`) and the application entrypoint.
- Projects and there folders should be named {CompanyName}.{BoundedContext}.{Layer}, e.g. `Acme.Inventory.Domain`.
  - e.g. src/
    - Acme.Inventory.Domain/
    - Acme.Inventory.Application/
    - Acme.Inventory.Infrastructure/
    - Acme.Inventory.Main/
  - CompanyName is optional
- Run the application by running the Main project.
- Use CSharpier with `.csharpierrc` and `printWidth: 80`.
- Add `dotnet-tools.json` at the repository root.
- Use xUnit for unit tests and ArchUnitNET for architecture tests.
