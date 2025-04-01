using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

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
            
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File not found - {filePath}");
                return structSizes;
            }

            string[] lines = File.ReadAllLines(filePath);
            
            string currentStructName = null;
            int? structSize = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

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
                    }
                }
                // Check for struct end and size comment
                else if (line.StartsWith("};") && currentStructName != null)
                {
                    // Extract size from comment if available
                    if (line.Contains("Total:") && line.Contains("bytes"))
                    {
                        string sizeText = line.Substring(line.IndexOf("Total:") + 6);
                        sizeText = sizeText.Substring(0, sizeText.IndexOf("bytes")).Trim();
                        if (int.TryParse(sizeText, out int size))
                        {
                            structSize = size;
                        }
                    }

                    // Add the struct to the dictionary if we have both name and size
                    if (currentStructName != null && structSize.HasValue)
                    {
                        structSizes[currentStructName] = structSize.Value;
                        Console.WriteLine($"  Found C++ struct: {currentStructName}, Size: {structSize.Value} bytes");
                    }

                    // Reset for next struct
                    currentStructName = null;
                    structSize = null;
                }
            }

            return structSizes;
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
