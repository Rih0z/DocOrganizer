# DocOrganizer Project Overview

## Purpose
DocOrganizer is a professional-grade PDF editing and document management application for Windows that combines the familiar user experience of CubePDF Utility with advanced document organization features. It serves as a modernized, enhanced alternative to CubePDF Utility with additional features like auto-orientation detection, image-to-PDF conversion, and automated document organization.

## Key Features
- **PDF Operations**: Load, edit, merge, split, rotate, delete PDF pages
- **Image Support**: Convert HEIC, JPG, PNG, JPEG files to PDF
- **Drag & Drop**: Intuitive file handling with visual feedback
- **Auto-Orientation**: Automatic detection and correction of image orientation
- **Auto-Update**: GitHub Releases integration for seamless updates
- **Document Organization**: Smart naming, batch processing, template system

## Tech Stack
- **Language**: C# with .NET 6.0
- **Framework**: WPF (Windows Presentation Foundation)
- **Architecture**: Clean Architecture with MVVM pattern
- **UI Pattern**: MVVM with CommunityToolkit.Mvvm
- **Dependencies**: 
  - PDFsharp for PDF processing
  - SkiaSharp for image processing  
  - Microsoft.Extensions.DependencyInjection for IoC
  - Serilog for logging
  - Microsoft.Xaml.Behaviors.Wpf for behaviors

## Project Structure
```
DocOrganizer/
├── src/
│   ├── DocOrganizer.Core/          # Domain layer (entities, interfaces)
│   ├── DocOrganizer.Application/   # Application layer (use cases, services)
│   ├── DocOrganizer.Infrastructure/# Infrastructure layer (PDF, file I/O)
│   └── DocOrganizer.UI/            # Presentation layer (WPF, ViewModels)
├── tests/                          # Unit and integration tests
├── scripts/                        # Build and automation scripts
├── docs/                          # Documentation
├── sample/                        # Test files (HEIC, JPG, PNG, etc.)
├── release/                       # Build output directory
└── tmp/                          # Temporary analysis/debug files
```

## Current Status
- **Version**: 2.2.0
- **State**: Production-ready with V3 architecture improvements in progress
- **Build Target**: Single-file, self-contained Windows x64 executable (~200MB)
- **Testing**: Comprehensive test suite with automated scripts