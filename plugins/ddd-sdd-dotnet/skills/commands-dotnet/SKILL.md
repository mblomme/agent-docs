---
name: commands-dotnet
description: Restore, build, test, format, or run the Company.BoundedContext .NET solution using its repository commands.
---

# .NET commands

Run these commands from the repository root:

- Restore tools: `dotnet tool restore --tool-manifest dotnet-tools.json`
- Restore solution: `dotnet restore Company.BoundedContext.slnx`
- Build: `dotnet build Company.BoundedContext.slnx --no-restore -m:1`
- Test: `dotnet test Company.BoundedContext.slnx --no-build -m:1`
- Check formatting: `dotnet tool run csharpier check .`
- Run: `dotnet run --project src/Company.BoundedContext.Main`

If `dotnet test` fails with a socket Permission Denied error, do not abort. Retry with .NET diagnostic sockets disabled:

```bash
DOTNET_EnableDiagnostics=0 DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER=1 dotnet test --no-build
```
