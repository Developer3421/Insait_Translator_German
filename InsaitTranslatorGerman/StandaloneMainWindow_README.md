# Standalone Translator Window

This is a standalone, self-contained version of the main window from the Insait Translator: German project. It can be easily integrated into other Avalonia projects.

## Features

- Custom title bar with window controls (minimize, maximize, close)
- Translation interface with source and target text areas
- Basic file operations (open, save)
- Clipboard operations (copy, paste)
- Mock translation functionality (reverses text for demonstration)
- Responsive design with custom styling

## How to Use in Your Project

### 1. Copy Files

Copy the following files to your Avalonia project:
- `StandaloneMainWindow.axaml`
- `StandaloneMainWindow.axaml.cs`

### 2. Update Namespace

In `StandaloneMainWindow.axaml.cs`, update the namespace to match your project:

```csharp
namespace YourProjectNamespace;  // Change this to your namespace
```

### 3. Update XAML Class Reference

In `StandaloneMainWindow.axaml`, update the x:Class attribute:

```xml
x:Class="YourProjectNamespace.StandaloneMainWindow"
```

### 4. Add to Your App

In your `App.axaml.cs` or main application file, you can show the window:

```csharp
var window = new StandaloneMainWindow();
window.Show();
```

### 5. Customize

The window includes a simple `SimpleViewModel` class for demonstration. You can:
- Replace the ViewModel with your own
- Modify the styling and colors
- Add real translation functionality
- Integrate with your existing services

## Dependencies

This standalone window requires:
- Avalonia UI framework
- .NET 6.0 or higher

## Customization Notes

- All colors and styles are defined as resources in the XAML
- The window uses a custom title bar (no system decorations)
- Event handlers are basic implementations - replace with your logic
- The mock translation just reverses the input text

## License

This standalone window is provided as-is for integration into other projects. See the main project's LICENSE.md for usage terms.
