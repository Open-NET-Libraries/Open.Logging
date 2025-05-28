# Development History

This folder contains detailed release documentation and development history for Open.Logging.

## Structure

- **`v{version}.md`** - Complete release documentation including:
  - Release notes and overview
  - Breaking changes and migration guides
  - Technical details and architecture changes
  - Changelog details
  - Package information and links

- **`RELEASE_NOTES_v{version}.md`** - Original standalone release notes (archived)

## Current Releases

- **[v2.0.0](v2.0.0.md)** - Enhanced Reliability & Performance (May 28, 2025)
  - Fixed critical race condition in multi-logger scenarios
  - Breaking changes in FileLogger disposal behavior
  - Comprehensive test reliability improvements

## Quick Reference

For a summary of all changes, see the main [CHANGELOG.md](../CHANGELOG.md).

For installation and usage instructions, see the main [README.md](../README.md).

## Release Process

1. Update version in project files
2. Create detailed release documentation in this folder
3. Update main CHANGELOG.md with summary
4. Create and test NuGet packages
5. Tag release in git
6. Publish packages to NuGet.org
7. Create GitHub release with link to detailed documentation
