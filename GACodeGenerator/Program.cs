using System;
using System.Text.Json;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Nodes;

namespace GACodeGenerator
{
    static class Program
    {
        static int Main(string[] argsArray)
        {
            try
            {
                var config = new ConfigurationBuilder()
                    .AddCommandLine(argsArray)
                    .Build();

                string masterFile = config["MasterFile"];
                if (masterFile != null)
                {
                    if (!ProcessMasterFile(masterFile))
                        return 1;
                }
                else
                {
                    string gaFile = config["GAFile"];
                    string outputDir = config["OutDir"];

                    if (!ProcessGAFile(gaFile, outputDir))
                        return 1;
                }
            }
            catch(Exception exception)
            {
                Console.WriteLine("Got exception...\n" + exception.ToString());
                return 1;
            }

            return 0;
        }

        static bool ProcessMasterFile(string masterFile)
        {
            string jsonText = File.ReadAllText(masterFile);
            JsonDocument jsonDoc = JsonDocument.Parse(jsonText);

            JsonElement jsonSubConfigList;
            if (!jsonDoc.RootElement.TryGetProperty("sub_config_list", out jsonSubConfigList))
            {
                Console.WriteLine("No \"sub_config_list\" found.");
                return false;
            }

            for(int i = 0; i < jsonSubConfigList.GetArrayLength(); i++)
            {
                JsonElement jsonSubConfig = jsonSubConfigList[i];

                JsonElement jsonConfigFile;
                if(!jsonSubConfig.TryGetProperty("config_file", out jsonConfigFile))
                {
                    Console.WriteLine("No \"config_file\" found in entry.");
                    return false;
                }

                string gaFile = Path.Combine(Path.GetDirectoryName(masterFile), jsonConfigFile.GetString());

                JsonElement jsonOutputDir;
                if(!jsonSubConfig.TryGetProperty("output_dir", out jsonOutputDir))
                {
                    Console.WriteLine("No \"output_dir\" found in entry.");
                    return false;
                }

                string outDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(masterFile), jsonOutputDir.GetString()));

                Console.WriteLine($"Processing {gaFile}...");

                if(!ProcessGAFile(gaFile, outDir))
                    return false;
            }

            return true;
        }

        static bool ProcessGAFile(string gaFile, string outputDir)
        {
            string nameSpace;
            List<GAClass>? gaClassList = GenerateGAClassList(gaFile, out nameSpace);
            if (gaClassList == null)
                return false;

            for (int i = 0; i < gaClassList.Count; i++)
            {
                GAClass gaClass = gaClassList[i];
                gaClass.GenerateSourceCode(outputDir, nameSpace, gaClassList);
            }

            return true;
        }

        static List<GAClass>? GenerateGAClassList(string gaFile, out string nameSpace)
        {
            nameSpace = "";

            if (gaFile == null)
            {
                Console.WriteLine("Expected --GAFile=<file-path> to be given.");
                return null;
            }

            string jsonText = File.ReadAllText(gaFile);
            JsonDocument jsonDoc = JsonDocument.Parse(jsonText);

            JsonElement jsonNamespace;
            if (!jsonDoc.RootElement.TryGetProperty("namespace", out jsonNamespace))
            {
                Console.WriteLine("Did not find \"namespace\" in the GA file.");
                return null;
            }

            nameSpace = jsonNamespace.GetString()!;

            JsonElement jsonClassList;
            if (!jsonDoc.RootElement.TryGetProperty("class_list", out jsonClassList))
            {
                Console.WriteLine("Did not find \"class_list\" in the GA file.");
                return null;
            }

            List<GAClass> gaClassList = new List<GAClass>();

            for (int i = 0; i < jsonClassList.GetArrayLength(); i++)
            {
                JsonElement jsonClass = jsonClassList[i];

                JsonElement jsonName;
                if (!jsonClass.TryGetProperty("name", out jsonName))
                {
                    Console.WriteLine("Did not find \"name\" in class definition.");
                    return null;
                }

                string? gaClassName = jsonName.GetString();
                if (gaClassName == null)
                {
                    Console.WriteLine("Class name was null.");
                    return null;
                }

                JsonElement jsonBasisList;
                if (!jsonClass.TryGetProperty("basis", out jsonBasisList))
                {
                    Console.WriteLine("Did not find \"basis\" in class definition.");
                    return null;
                }

                HashSet<string> basisSet = new HashSet<string>();

                for (int j = 0; j < jsonBasisList.GetArrayLength(); j++)
                {
                    JsonElement jsonBasisElement = jsonBasisList[j];
                    basisSet.Add(jsonBasisElement.GetString()!);
                }

                GAClass gaClass = new GAClass(gaClassName, basisSet);
                gaClassList.Add(gaClass);
            }

            return gaClassList;
        }
    }
}