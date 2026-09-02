# ADR 0002: Template, not runtime framework

Status: Accepted

## Context

WPF already has mature control libraries, toolkit packages, dependency-injection options, and application frameworks. MiniMica's useful niche is a small modern application starting point that developers own and edit.

## Decision

MiniMica ships source and a `dotnet new` template. It does not require downstream applications to reference a MiniMica runtime NuGet package.

## Consequences

- downstream developers can refactor freely;
- there is no framework-version lockstep;
- breaking internal changes are less important than clarity;
- fixes are not automatically inherited by already-generated applications;
- documentation must make the source easy to understand and update manually.
