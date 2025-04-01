using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace StructSizeComparer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Header Size Comparison Tool");
                Console.WriteLine("==========================");
                
                // Get the application directory to ensure files are found regardless of working directory
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string projectDirectory = GetProjectDirectory();
                
                Console.WriteLine($"App Directory: {appDirectory}");
                Console.WriteLine($"Project Directory: {projectDirectory}");
                
                // Paths to header files - look in both possible locations
                string cppHeaderPath = Path.Combine(projectDirectory, "TestStructs.h");
                if (!File.Exists(cppHeaderPath))
                {
                    cppHeaderPath = "TestStructs.h"; // Fallback to current directory
                }
                
                Console.WriteLine($"Using header file: {cppHeaderPath}");

                // Parse the C++ header file
                var cppStructSizes = ParseCppHeader(cppHeaderPath);
                Console.WriteLine($"Found {cppStructSizes.Count} structs in C++ header.");

                // Get C# struct sizes via reflection
                var csStructSizes = GetCSharpStructSizes();
                Console.WriteLine($"Found {csStructSizes.Count} structs in C# header.");

                // Generate comparison report - save to both project directory and current directory
                // to ensure it's created regardless of how the app is launched
                string outputPath = Path.Combine(projectDirectory, "struct_comparison.csv");
                GenerateCsvReport(cppStructSizes, csStructSizes, outputPath);
                
                // Also save to current directory as a backup
                string currentDirOutputPath = Path.Combine(Directory.GetCurrentDirectory(), "struct_comparison.csv");
                if (currentDirOutputPath != outputPath)
                {
                    GenerateCsvReport(cppStructSizes, csStructSizes, currentDirOutputPath);
                    Console.WriteLine($"Also saved a copy to '{currentDirOutputPath}'");
                }
                
                Console.WriteLine($"Comparison complete. Results saved to '{outputPath}'");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
        
        // Helper method to find the project directory
        static string GetProjectDirectory()
        {
            // Start with the current directory
            string directory = Directory.GetCurrentDirectory();
            
            // If we're running from the build output directory, go up to find the project directory
            if (directory.Contains("bin") && directory.Contains("Debug"))
            {
                // Typical bin path pattern: ...\bin\Debug\net7.0\...
                // Try to go up to the project root
                string parent = directory;
                for (int i = 0; i < 3; i++) // Go up 3 levels from bin\Debug\net7.0
                {
                    parent = Path.GetDirectoryName(parent);
                    if (parent == null) break;
                }
                
                if (parent != null && File.Exists(Path.Combine(parent, "StructSizeComparer.csproj")))
                {
                    return parent; // Found the project directory
                }
            }
            
            // Check if we're already in the project directory
            if (File.Exists(Path.Combine(directory, "StructSizeComparer.csproj")))
            {
                return directory;
            }
            
            // Default to current directory if we couldn't determine project directory
            return directory;
        }

        static Dictionary<string, int> ParseCppHeader(string filePath)
        {
            Console.WriteLine($"Parsing C++ header file: {filePath}");
            
            var structSizes = new Dictionary<string, int>();
            var constants = new Dictionary<string, int>(); // Track defined constants
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File not found - {filePath}");
                return structSizes;
            }

            string[] lines = File.ReadAllLines(filePath);
            
            string currentStructName = null;
            List<string> currentStructMembers = null;
            Dictionary<string, int> nestedStructSizes = new Dictionary<string, int>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                // Skip comments and empty lines
                if (line.StartsWith("//") || string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                // Look for constants (macros or global constants)
                if (line.StartsWith("#define "))
                {
                    string[] parts = line.Substring("#define ".Length).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 && int.TryParse(parts[1], out int value))
                    {
                        constants[parts[0]] = value;
                        Console.WriteLine($"  Found constant: {parts[0]} = {value}");
                    }
                }
                else if (line.Contains("const ") && line.Contains("="))
                {
                    int equalPos = line.IndexOf('=');
                    if (equalPos > 0)
                    {
                        string beforeEqual = line.Substring(0, equalPos).Trim();
                        string afterEqual = line.Substring(equalPos + 1).Trim();
                        
                        // Extract constant name
                        string[] parts = beforeEqual.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            string constName = parts[parts.Length - 1];
                            
                            // Try to parse value
                            string valueStr = afterEqual;
                            if (valueStr.EndsWith(";"))
                            {
                                valueStr = valueStr.Substring(0, valueStr.Length - 1);
                            }
                            
                            if (int.TryParse(valueStr, out int value))
                            {
                                constants[constName] = value;
                                Console.WriteLine($"  Found constant: {constName} = {value}");
                            }
                        }
                    }
                }

                // Check for struct declaration
                if (line.StartsWith("struct ") && !line.EndsWith(";"))
                {
                    // Extract struct name
                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        currentStructName = parts[1];
                        if (currentStructName.EndsWith("{"))
                        {
                            currentStructName = currentStructName.Substring(0, currentStructName.Length - 1).Trim();
                        }
                        currentStructMembers = new List<string>();
                    }
                }
                // Collect struct members
                else if (currentStructName != null && !line.StartsWith("}") && !line.StartsWith("struct "))
                {
                    // Remove inline comments
                    int commentIndex = line.IndexOf("//");
                    if (commentIndex > 0)
                    {
                        line = line.Substring(0, commentIndex).Trim();
                    }
                    
                    // Add valid members (skip preprocessor directives, empty lines, etc.)
                    if (line.EndsWith(";") && !line.StartsWith("#"))
                    {
                        currentStructMembers.Add(line);
                    }
                }
                // Check for struct end
                else if (line.StartsWith("};") && currentStructName != null && currentStructMembers != null)
                {
                    // Calculate struct size from its members
                    int structSize = CalculateStructSize(currentStructName, currentStructMembers, nestedStructSizes, constants);
                    structSizes[currentStructName] = structSize;
                    
                    // Save this struct's size for potential nested struct references
                    nestedStructSizes[currentStructName] = structSize;
                    
                    Console.WriteLine($"  Found C++ struct: {currentStructName}, Calculated Size: {structSize} bytes");
                    
                    // Reset for next struct
                    currentStructName = null;
                    currentStructMembers = null;
                }
            }

            return structSizes;
        }

        static int CalculateStructSize(string structName, List<string> members, Dictionary<string, int> knownStructSizes, Dictionary<string, int> constants)
        {
            int totalSize = 0;

            foreach (string member in members)
            {
                string trimmedMember = member.Trim().TrimEnd(';');
                
                // Handle array declarations: Type name[size];
                if (trimmedMember.Contains("[") && trimmedMember.Contains("]"))
                {
                    // Get position of opening and closing brackets
                    int openBracketPos = trimmedMember.IndexOf('[');
                    int closeBracketPos = trimmedMember.IndexOf(']');
                    
                    if (openBracketPos > 0 && closeBracketPos > openBracketPos)
                    {
                        // Extract the parts: type name[size]
                        string beforeBracket = trimmedMember.Substring(0, openBracketPos).Trim();
                        string arraySizeName = trimmedMember.Substring(openBracketPos + 1, closeBracketPos - openBracketPos - 1).Trim();
                        
                        // Split the beforeBracket to get type and variable name
                        string[] parts = beforeBracket.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 1)
                        {
                            string typeName = parts[0];
                            int typeSize = GetCppTypeSize(typeName, knownStructSizes);
                            int arraySize = 1; // Default size
                            
                            // Try to parse array size directly
                            if (int.TryParse(arraySizeName, out int directSize))
                            {
                                arraySize = directSize;
                            }
                            // Check if it's a named constant
                            else if (constants.TryGetValue(arraySizeName, out int constSize))
                            {
                                arraySize = constSize;
                                Console.WriteLine($"  Using constant {arraySizeName} = {constSize} for array size");
                            }
                            else
                            {
                                // Could not determine size
                                Console.WriteLine($"  Warning: Could not determine size for array '{arraySizeName}', assuming size 1");
                            }
                            
                            totalSize += typeSize * arraySize;
                        }
                    }
                }
                // Handle regular declarations: Type name;
                else
                {
                    string[] parts = trimmedMember.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        string typeName = parts[0];
                        
                        // Check if the type is a previously defined struct
                        int typeSize = GetCppTypeSize(typeName, knownStructSizes);
                        totalSize += typeSize;
                    }
                }
            }
            
            return totalSize;
        }

        static int GetCppTypeSize(string typeName, Dictionary<string, int> knownStructSizes)
        {
            // Check for known structs first
            if (knownStructSizes.TryGetValue(typeName, out int structSize))
            {
                Console.WriteLine($"  Using size {structSize} for struct type {typeName}");
                return structSize;
            }
            
            // Common C++ primitive type sizes
            switch (typeName.ToLower())
            {
                // Integer types
                case "bool": 
                case "uint8_t":
                case "int8_t":
                case "char": 
                case "byte": return 1;
                
                case "short": 
                case "int16_t": 
                case "unsigned short":
                case "uint16_t": return 2;
                
                case "int": 
                case "int32_t":
                case "unsigned":
                case "unsigned int":
                case "uint32_t": return 4;
                
                case "long": return 4; // 32-bit on Windows
                
                case "long long":
                case "int64_t":
                case "unsigned long long":
                case "uint64_t": return 8;
                
                // Floating point types
                case "float": return 4;
                case "double": return 8;
                case "long double": return 8;
                
                // Pointer types (assuming 64-bit architecture)
                default:
                    if (typeName.EndsWith("*"))
                        return 8; // 64-bit pointer
                    
                    // For unknown types, log a warning and assume 4 bytes
                    Console.WriteLine($"Warning: Unknown type '{typeName}', assuming 4 bytes");
                    return 4;
            }
        }

        static Dictionary<string, int> GetCSharpStructSizes()
        {
            Console.WriteLine("Getting C# struct sizes via reflection");
            
            var structSizes = new Dictionary<string, int>();
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Get all types in the current assembly that are structs
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsValueType && !type.IsPrimitive && !type.IsEnum && type.Namespace == "StructSizeComparer")
                {
                    try
                    {
                        int size;
                        
                        // First try to use the Size() method if available
                        MethodInfo sizeMethod = type.GetMethod("Size", BindingFlags.Public | BindingFlags.Static);
                        if (sizeMethod != null)
                        {
                            size = (int)sizeMethod.Invoke(null, null);
                            Console.WriteLine($"  Found C# struct: {type.Name}, Size: {size} bytes (via Size() method)");
                        }
                        else
                        {
                            // Create a properly initialized instance of the struct for Marshal.SizeOf
                            object instance = CreateInitializedInstance(type);
                            size = Marshal.SizeOf(instance);
                            Console.WriteLine($"  Found C# struct: {type.Name}, Size: {size} bytes (via Marshal.SizeOf)");
                        }
                        
                        structSizes[type.Name] = size;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Error getting size for {type.Name}: {ex.Message}");
                    }
                }
            }

            return structSizes;
        }

        static object CreateInitializedInstance(Type type)
        {
            // Create instance
            object instance = Activator.CreateInstance(type);
            
            // Initialize all array fields
            foreach (FieldInfo field in type.GetFields())
            {
                if (field.FieldType.IsArray)
                {
                    // Get array size from MarshalAs attribute
                    int size = 1; // Default size
                    var marshalAttr = field.GetCustomAttribute<MarshalAsAttribute>();
                    if (marshalAttr != null && marshalAttr.SizeConst > 0)
                    {
                        size = marshalAttr.SizeConst;
                    }
                    
                    // Create and set array
                    Array array = Array.CreateInstance(field.FieldType.GetElementType(), size);
                    field.SetValue(instance, array);
                }
            }
            
            return instance;
        }

        static void GenerateCsvReport(Dictionary<string, int> cppStructSizes, Dictionary<string, int> csStructSizes, string outputFilePath)
        {
            Console.WriteLine($"Generating CSV report to: {outputFilePath}");
            
            var csv = new StringBuilder();
            csv.AppendLine("Struct Name,Size in .h (bytes),Size in .cs (bytes),Notes");

            // Create a combined list of all struct names
            var allStructNames = new HashSet<string>(cppStructSizes.Keys);
            allStructNames.UnionWith(csStructSizes.Keys);

            foreach (var structName in allStructNames)
            {
                bool inCpp = cppStructSizes.TryGetValue(structName, out int cppSize);
                bool inCs = csStructSizes.TryGetValue(structName, out int csSize);
                string notes = "";

                if (!inCpp || !inCs)
                {
                    notes = "incomplete pair";
                }
                else if (cppSize != csSize)
                {
                    notes = "not equal";
                }

                csv.AppendLine($"{structName},{(inCpp ? cppSize.ToString() : "N/A")},{(inCs ? csSize.ToString() : "N/A")},{notes}");
            }

            try 
            {
                File.WriteAllText(outputFilePath, csv.ToString());
                Console.WriteLine($"Successfully wrote {csv.Length} characters to {outputFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing CSV file: {ex.Message}");
            }
        }
    }
}
