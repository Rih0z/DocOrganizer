# Task Completion Checklist for DocOrganizer

## Pre-Development Checklist
- [ ] Read CLAUDE.md principles (第1条-第15条)
- [ ] Execute `git pull origin main` to sync latest changes
- [ ] Run Windows environment build to ensure clean state
- [ ] Review existing code using Serena MCP tools for context

## During Development
- [ ] Follow Clean Architecture principles
- [ ] Implement proper MVVM pattern
- [ ] Add appropriate logging with Serilog
- [ ] Write unit tests for new functionality
- [ ] Use dependency injection for services
- [ ] Handle errors gracefully with user-friendly messages

## Code Quality Checks
- [ ] Run all tests: `dotnet test`
- [ ] Verify build: `dotnet build --configuration Release`
- [ ] Check code style compliance
- [ ] Ensure proper nullable reference handling
- [ ] Validate async/await usage for I/O operations

## Testing Requirements
- [ ] Unit tests pass with minimum 80% coverage
- [ ] Manual testing with sample files
- [ ] Run automated test scripts:
  - [ ] `.\scripts\test\QuickAutoTest.ps1`
  - [ ] `.\scripts\test\ComprehensiveTest.ps1`
  - [ ] `.\scripts\test\drag-drop-test.ps1`
- [ ] Verify drag & drop functionality works (no admin privileges)
- [ ] Test with various file formats (HEIC, JPG, PNG, PDF)

## Build and Deployment
- [ ] Execute full publish command:
  ```bash
  dotnet publish src/DocOrganizer.UI/DocOrganizer.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release
  ```
- [ ] Verify EXE generation: Check `release/DocOrganizer.exe` exists (~200MB)
- [ ] Test EXE startup from Explorer (not admin mode)
- [ ] Validate all core features work in published version

## Documentation Updates
- [ ] Update README.md if new features added
- [ ] Add/update XML documentation for public APIs
- [ ] Document any breaking changes
- [ ] Update version numbers if necessary

## Version Control
- [ ] Commit changes with descriptive message
- [ ] Push to remote repository: `git push origin main`
- [ ] Create GitHub release if major feature (use build-and-release.ps1)

## Bug Fixes (第15条 Compliance)
- [ ] Analyze root cause using Serena MCP tools
- [ ] Create analysis document in tmp/ folder
- [ ] Report findings to user before implementing fix
- [ ] Get user approval for proposed solution
- [ ] Implement fix and verify resolution
- [ ] Update or remove analysis document after user confirms fix

## Final Verification
- [ ] All automated tests pass
- [ ] Manual testing complete
- [ ] No console errors or exceptions
- [ ] Performance acceptable for large files
- [ ] UI responsive and intuitive
- [ ] Drag & drop functionality working properly

## Success Criteria
- ✅ Feature works as expected in published EXE
- ✅ No regression in existing functionality  
- ✅ Code follows established patterns and conventions
- ✅ Adequate test coverage maintained
- ✅ User experience remains intuitive
- ✅ Performance meets requirements

## Post-Completion
- [ ] Notify user of completion with EXE path
- [ ] Document lessons learned for future development
- [ ] Archive analysis documents if bug was fixed
- [ ] Plan next iteration if applicable