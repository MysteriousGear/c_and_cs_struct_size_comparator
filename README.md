# C/C++ and C# Struct Size Comparator

This tool compares the memory sizes of C/C++ structs defined in header files with their C# counterparts. It helps identify size mismatches when working with interop between C++ and C#.

## Command Line Instructions

### Creating a New Solution

To create a new solution from the command line:

```bash
# Create a new solution file
dotnet new sln -n StructSizeComparer

# Create a new console project
dotnet new console -n StructSizeComparer
```

### Adding Project to Solution

To add an existing project to a solution:

```bash
# Add the project to the solution
dotnet sln StructSizeComparer.sln add StructSizeComparer.csproj
```

### Building the Project

To build the project from the command line:

```bash
# Navigate to the project directory if needed
# cd path\to\project

# Build the project in Debug mode
dotnet build

# Or build in Release mode
dotnet build -c Release
```

### Running the Project

To run the project from the command line:

```bash
# Run the compiled application
dotnet run

# Alternative: Run the executable directly
# .\bin\Debug\net7.0\StructSizeComparer.exe
```

## Project Structure

- **Program.cs** - Main program logic for comparing struct sizes
- **TestStructs.h** - C/C++ header file containing struct definitions
- **TestStructs.cs** - C# file containing struct definitions that mirror the C/C++ ones
- **struct_comparison.csv** - Output file showing size comparison results

## How It Works

The application works by:

1. Parsing C++ struct definitions from the header file
2. Calculating struct sizes based on member types and sizes
3. Using reflection to get C# struct sizes
4. Comparing the sizes and generating a CSV report

## Customizing

To add your own structs for comparison:

1. Add the C/C++ struct definition to TestStructs.h
2. Add the corresponding C# struct to TestStructs.cs with proper [StructLayout] attributes
3. Run the application to see the comparison