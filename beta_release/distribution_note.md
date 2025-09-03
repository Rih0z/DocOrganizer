# DocOrganizer Beta Test Distribution Guide

## 📦 Single EXE Distribution Policy

### 🎯 **Distribution Method**
- **Main EXE**: `release/DocOrganizer.exe` (307MB)
- **Distribution**: Provide entire `release/` folder
- **Update Policy**: Continuous updates to single DocOrganizer.exe file

### 📂 **Folder Structure for Distribution**
```
DocOrganizer-Beta/
├── DocOrganizer.exe          (307MB - Main application)
├── DocOrganizer.pdb          (Debug symbols)
├── DEBUG_LOG.txt             (Performance monitoring)
├── STARTUP_LOG.txt           (Application startup logs)
├── sample_files/             (Test PDF files)
│   ├── document_*.pdf        (5 sample PDFs)
├── beta_test_guide.md        (Testing instructions)
└── feedback_form_template.md (Evaluation form)
```

### 🔄 **Update Process**
1. **Build**: Generate new DocOrganizer.exe via dotnet publish
2. **Replace**: Overwrite existing DocOrganizer.exe in release/
3. **Distribute**: Share updated release/ folder
4. **No Version Suffix**: Always use DocOrganizer.exe (no -BETA, -v3.0.025, etc.)

### ✅ **Benefits**
- **Simplicity**: Single file to manage and update
- **Clarity**: No confusion about which version to use
- **Efficiency**: Continuous improvement without file proliferation
- **User Experience**: Consistent filename across all updates

### 📋 **For Beta Testers**
- Always use the same DocOrganizer.exe file
- Replace with newer version when provided
- No need to track version numbers in filename
- Report issues referencing the date you received the file

---

**Distribution Ready**: release/DocOrganizer.exe + supporting files