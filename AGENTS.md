# GodMode Umbraco 17 Repository

## Current Project State

This repository contains the Umbraco 17 / .NET 10 version of the Diplo GodMode package.

- `Diplo.GodMode/` is the active Umbraco 17 package project. It should be the default location for package code changes.
- `Diplo.GodMode.Testsite/` is the local Umbraco 17 demo/test site. It references the package project and is used for local verification.
- `Diplo.GodMode.slnx` is the active solution and includes both projects.
- Older Umbraco 13 / AngularJS code lives on the `v13` branch. Do not reintroduce AngularJS-era files into this branch.

Umbraco must be able to discover the package manifest at `/App_Plugins/DiploGodMode/umbraco-package.json`.

## Repo Layout

- `Diplo.GodMode/Diplo.GodMode.csproj` is the NuGet package project.
- `Diplo.GodMode/Client/` contains the Lit + TypeScript backoffice source.
- `Diplo.GodMode/Client/public/umbraco-package.json` is the source package manifest used by the client build.
- `Diplo.GodMode/wwwroot/App_Plugins/DiploGodMode/` contains the built package assets served by Umbraco and packed as static web assets.
- `Diplo.GodMode.Testsite/Diplo.GodMode.Testsite.csproj` is the local host site used to run and verify the package.
- `Directory.Packages.props` centrally manages .NET package versions.

## Working Rules For Agents

- Prefer changes in `Diplo.GodMode/` unless the request is specifically about the demo site, docs, packaging, or repo configuration.
- Use `Diplo.GodMode.Testsite/` only as the local host and verification site unless the requested work is explicitly host-site configuration.
- Build backoffice changes with Lit, TypeScript, Umbraco UI components, and Umbraco v17 extension manifests.
- Do not port or restore AngularJS controllers, `package.manifest`, or legacy `App_Plugins/DiploGodMode/backoffice/` views/scripts from v13.
- Keep the package identity as `Diplo.GodMode` even though this branch targets Umbraco 17.
- Preserve static web asset behavior: the package manifest and built JS must be available under `/App_Plugins/DiploGodMode/`, not only under `/_content/`.
- Be careful with generated folders. Do not commit `bin/`, `obj/`, `node_modules/`, local database files, or package output artifacts unless explicitly requested.

## Build, Run, And Package

Useful validation commands from the repository root:

```powershell
dotnet build Diplo.GodMode.slnx
dotnet build Diplo.GodMode.Testsite/Diplo.GodMode.Testsite.csproj
dotnet pack Diplo.GodMode/Diplo.GodMode.csproj -c Release
```

The `Diplo.GodMode` project has MSBuild targets that run the client build. When `Diplo.GodMode/Client/node_modules` is missing, the build restores packages with `npm ci`; package builds also run `npm ci` and `npm run build`.

For local browser testing, run the host site:

```powershell
dotnet run --project Diplo.GodMode.Testsite/Diplo.GodMode.Testsite.csproj
```

Then open the Umbraco backoffice and verify the GodMode package loads from `/App_Plugins/DiploGodMode/`.

## Package Manifest And Assets

- Source manifest: `Diplo.GodMode/Client/public/umbraco-package.json`.
- Built manifest: `Diplo.GodMode/wwwroot/App_Plugins/DiploGodMode/umbraco-package.json`.
- Vite output directory: `Diplo.GodMode/wwwroot/App_Plugins/DiploGodMode/`.
- NuGet package should contain `staticwebassets/App_Plugins/DiploGodMode/umbraco-package.json`.

## External References

### Umbraco v17 Docs

- Main docs: https://docs.umbraco.com/umbraco-cms
- Packages: https://docs.umbraco.com/umbraco-cms/extending/packages
- Creating a package: https://docs.umbraco.com/umbraco-cms/extending/packages/creating-a-package
- Good practices: https://docs.umbraco.com/umbraco-cms/extending/packages/good-practice-and-defaults
- Vite package setup: https://docs.umbraco.com/umbraco-cms/customizing/development-flow/vite-package-setup

### Umbraco Source

- Umbraco v17 source: https://github.com/umbraco/Umbraco-CMS/tree/v17/dev

### Example Packages

- Opinionated package starter: https://github.com/LottePitcher/opinionated-package-starter
