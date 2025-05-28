# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2025-05-28

### 🚀 Major Release: Enhanced Reliability & Performance

**⚠️ Breaking Changes**: FileLogger disposal behavior changes - see [detailed release notes](releases/v2.0.0.md)

#### Fixed
- **Critical**: Fixed intermittent race condition in multi-logger test scenarios
- Fixed XML documentation warnings

#### Added
- Comprehensive stress tests for race condition detection
- Enhanced diagnostic capabilities for async buffering
- Better test coverage and reliability patterns

#### Changed
- **Breaking**: FileLogger now requires proper `DisposeAsync()` calls for data integrity
- Improved disposal patterns and lifecycle management
- Enhanced error handling and diagnostics

**📖 Full Details**: See [dev-history/v2.0.0.md](dev-history/v2.0.0.md) for complete release documentation, migration guide, and technical details.

**📦 Packages**: 
- `Open.Logging.Extensions` v2.0.0
- `Open.Logging.Extensions.SpectreConsole` v2.0.0

---

## [1.x.x] - Previous Versions
- Legacy releases (details available in git history)
