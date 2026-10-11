# Contributing

For bug reports, include the Windows version, CPU model, selected profile,
expected behavior and relevant log lines. Remove personal paths from logs.
Do not attach game files or signing credentials.

Keep changes focused and compatible with C# 5 and .NET Framework 4.8. Match
`.editorconfig`, translate user-facing messages in `Locale.cs`, and add a focused
regression test when changing CPU selection, process targeting or restoration.

Before submitting a pull request, run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test -Render
```

Inspect the FR and EN previews after interface changes. Keep generated
executables, archives and local preferences out of source control.
