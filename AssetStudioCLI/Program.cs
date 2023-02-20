using System.Windows.Forms;
using System.CommandLine;
using AssetStudio;
using AssetStudioGUI;

namespace AssetStudioCLI
{
    static class Program
    {
        [STAThread]
        static async Task<int> Main(string[] args)
        {
            var inputOption = new Option<FileInfo>(
                name: "--input",
                description: "Unity file to extract assets from or folder containing unity files.")
            { IsRequired = true };

            var outputOption = new Option<FileInfo>(
                name: "--output",
                description: "Output folder location.")
            { IsRequired = true };

            var exporttypeOption = new Option<ExportType>(
                name: "--exporttype",
                description: "How to export the assets.",
                getDefaultValue: () => ExportType.Convert)
                .FromAmong("convert", "dump", "raw");

            var filtertypeOption = new Option<IEnumerable<ClassIDType>>(
                name: "--filter",
                description: "Asset type to include in the export.",
                getDefaultValue: () => new List<ClassIDType>() { ClassIDType.MonoBehaviour, ClassIDType.Shader, ClassIDType.TextAsset, ClassIDType.Texture2D })
            {
                Arity = ArgumentArity.OneOrMore, AllowMultipleArgumentsPerToken = true
            }
                .FromAmong("monobehaviour", "shader", "textasset", "texture2d");

            var groupbyOption = new Option<string>(
                name: "--groupby",
                description: "How the exported assets will be arranged in the output directory.",
                getDefaultValue: () => "donotgroup")
                .FromAmong("donotgroup", "typename", "container", "sourcefile", "original");

            var removesuffixOption = new Option<bool>(
                name: "--removesuffix",
                description: "Remove the '.asset' and '.prefab' extension from the filename.",
                getDefaultValue: () => false);


            var rootCommand = new RootCommand("CLI application for AssetStudio")
            {
                exporttypeOption,
                inputOption,
                outputOption,
                filtertypeOption,
                groupbyOption,
                removesuffixOption
            };

            rootCommand.SetHandler((input, output, exportType, filterType, groupBy, removeSuffix) =>
            {
                var groups = new List<string>() { "typename", "container", "sourcefile", "original", "donotgroup" };
                var indexGroup = groups.IndexOf(groupBy);

                Studio.assetsManager.LoadFolder(input.FullName);
                Studio.BuildAssetData();
                Studio.BuildClassStructure();
                Studio.exportableAssets = Studio.exportableAssets.FindAll(x => filterType.Contains(x.Type));
                AssetStudioGUI.Properties.Settings.Default.assetGroupOption = indexGroup;
                AssetStudioGUI.Properties.Settings.Default.removesuffix = removeSuffix;
                Studio.ExportAssets(output.FullName, Studio.exportableAssets, exportType, input.FullName);
                Console.WriteLine("Press any key to cancel the process...");
                Console.ReadKey();
                Console.WriteLine($"Exporting {Studio.exportableAssets.Count} assets in '{output.FullName}'...");
            }, inputOption, outputOption, exporttypeOption, filtertypeOption, groupbyOption, removesuffixOption);

            return await rootCommand.InvokeAsync(args);
        }
    }

}

