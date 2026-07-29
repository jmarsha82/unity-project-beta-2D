# unity-project-beta-2D

Unity 6000.4.7f1 2D project.

## Tests

Unity EditMode tests live in `Assets/Tests/EditMode`.

Run them locally with:

```powershell
unity test C:\Users\jmars\CodeRepos\unity-project-beta-2D --mode EditMode --output TestResults\editmode.xml --timeout 300 -- -nographics
```

The current tests validate `Assets/Scenes/FirstLevel.unity`: level length, grass/dirt surfaces, 2D collider, start/end flags, white camera background, and shading accent objects.

For GitHub-hosted CI without a Unity license secret, `.github/workflows/ci.yml` runs:

```powershell
./tools/Validate-FirstLevel.ps1
```

That script checks the same committed scene/material contract without launching Unity.

## GitHub Actions

Unit Tests:

`CI / Unit Tests / Asset Contract` runs on pushes to `main` and pull requests. It validates the first-level scene and JSON package files.

Code Scanning: Quality:

`CI / Code Scanning / Quality` runs `actionlint` against GitHub Actions workflow files.

Code Scanning: Security:

`CodeQL / Code Scanning / Security` runs CodeQL for C# with `build-mode: none`, which avoids a Unity build on GitHub-hosted runners. Code scanning availability depends on repository visibility and GitHub code security settings.

`Dependency Review / Code Scanning / Security / Dependency Review` runs on pull requests and fails on critical dependency vulnerabilities.

Dependency Automation:

Dependabot is configured for GitHub Actions updates in `.github/dependabot.yml`. Unity Package Manager manifests are not covered because Dependabot does not support Unity package manifests as a package ecosystem.

## Coverage

Unity code coverage is not enabled yet. This repository currently has scene and asset-contract tests rather than runtime gameplay code with meaningful line coverage. Add Unity Test Framework coverage once gameplay scripts are introduced and coverage can measure product code rather than only test assertions.

