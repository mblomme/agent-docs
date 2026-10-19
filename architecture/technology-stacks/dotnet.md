# .NET

- Each layer is a separate .NET class library project (`classlib`).
- Main is a Web project (`web`) and the application entrypoint.
- Projects should be names {CompanyName}.{BoundedContext}.{Layer}, e.g. `Acme.Inventory.Domain`.
  - CompanyName is optional
- Run the application by running the Main project.
- Use CSharpier with `.csharpierrc` and `printWidth: 80`.
- Add `dotnet-tools.json` at the repository root.
- Use xUnit for unit tests and ArchUnitNET for architecture tests.
