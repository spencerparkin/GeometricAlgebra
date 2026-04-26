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

                string gaFile = config["GAFile"];
                string outputDir = config["OutDir"];

                string nameSpace;
                List<GAClass>? gaClassList = GenerateGAClassList(gaFile, out nameSpace);
                if (gaClassList == null)
                    return 1;

                for(int i = 0; i < gaClassList.Count; i++)
                {
                    GAClass gaClass = gaClassList[i];
                    gaClass.GenerateSourceCode(outputDir, nameSpace, gaClassList);
                }
            }
            catch(Exception exception)
            {
                Console.WriteLine("Got exception...\n" + exception.ToString());
                return 1;
            }

            return 0;
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