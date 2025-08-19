# DocOrganizer Code Style and Conventions

## Language Standards
- **Language**: C# with .NET 6.0
- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`)
- **Implicit Usings**: Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)
- **Unsafe Code**: Allowed for performance-critical operations

## Naming Conventions
- **Classes**: PascalCase (e.g., `DocumentManagementViewModel`, `PdfService`)
- **Methods**: PascalCase (e.g., `LoadPagesAsync`, `ProcessFilesAsync`)
- **Properties**: PascalCase (e.g., `ThumbnailImage`, `IsLoading`)
- **Fields**: camelCase with underscore prefix for private fields (e.g., `_logger`, `_pdfService`)
- **Interfaces**: PascalCase with 'I' prefix (e.g., `IPdfService`, `IImageLoaderService`)
- **Constants**: UPPER_CASE (e.g., `MAX_FILE_SIZE`)
- **Async Methods**: Always suffix with 'Async' (e.g., `LoadFileAsync`, `SaveDocumentAsync`)

## File Organization
- **Namespace Structure**: Follows folder structure (e.g., `DocOrganizer.UI.ViewModels.V3`)
- **One Class Per File**: Each class in its own file with matching filename
- **Folder Structure**: 
  - `/Models` - Data models and DTOs
  - `/ViewModels` - MVVM ViewModels  
  - `/Views` - WPF Views and UserControls
  - `/Services` - Business logic services
  - `/Interfaces` - Service contracts

## MVVM Pattern Guidelines
- **ViewModels**: Inherit from `ObservableObject` (CommunityToolkit.Mvvm)
- **Commands**: Use `RelayCommand` and `AsyncRelayCommand` from CommunityToolkit
- **Properties**: Use `[ObservableProperty]` attribute for auto-generation
- **Data Binding**: All UI updates through property binding, no direct UI manipulation

## Clean Architecture Principles
- **Domain Layer** (`Core`): Pure business logic, no dependencies
- **Application Layer**: Use cases and application services
- **Infrastructure Layer**: External concerns (file I/O, PDF processing)
- **Presentation Layer** (`UI`): WPF views and ViewModels

## Dependency Injection
- Use Microsoft.Extensions.DependencyInjection
- Register services in `App.xaml.cs` 
- Constructor injection in ViewModels and services
- Interface-based dependencies

## Error Handling
- Use structured exception handling with try-catch blocks
- Log errors using Serilog with appropriate log levels
- Provide user-friendly error messages
- Graceful degradation for non-critical errors

## Testing Standards
- **Unit Tests**: Cover business logic and ViewModels
- **Integration Tests**: Test cross-layer functionality
- **Test Naming**: Should_ExpectedBehavior_When_Condition
- **Arrange-Act-Assert**: Standard test structure
- **Mocking**: Use interfaces for testability

## Documentation
- **XML Comments**: For public APIs and complex methods
- **README**: Keep up-to-date with current functionality
- **Code Comments**: Explain "why" not "what"
- **Architecture Decision Records**: Document major design decisions

## Performance Considerations
- **Async/Await**: Use for I/O operations
- **Memory Management**: Proper disposal of resources
- **Observable Collections**: Use for UI-bound lists
- **Image Processing**: Stream-based processing for large files

## Version Control
- **Commit Messages**: Clear, descriptive messages
- **Branch Strategy**: Main branch for stable releases
- **PR Reviews**: Required for all changes
- **Git Ignore**: Exclude bin/, obj/, .vs/ directories