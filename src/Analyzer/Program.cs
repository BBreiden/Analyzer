using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Analyzer
{
    class Program
    {
        static async System.Threading.Tasks.Task Main(string[] args)
        {
            Console.WriteLine("ANALYZER");
            if (args.Length == 0)
            {
                Console.WriteLine("Missing project file.");
                return;
            }

            var path = Path.Combine(Environment.CurrentDirectory, args[0]);
            if (!File.Exists(path))
            {
                Console.WriteLine("File not found. " + path);
                return;
            }

            if (!TryRegisterMSBuild()) return;

            var (proj, failures) = await OpenProject(path);
            if (failures > 0)
            {
                Console.WriteLine($"ERROR: Obeserved {failures} failures while opening the project.");
                return;
            }
            Console.WriteLine("Project successfully loaded.");

            ReportProjectRefs(proj);

            var comp = await CompileProject(proj);
            RunAnalysis(comp);
        }

        private static void RunAnalysis(Compilation comp)
        {
            Console.WriteLine("Running analysis:");
            foreach (var tree in comp.SyntaxTrees)
            {
                Console.WriteLine($"+++++ {tree.FilePath}");
                var w = new Walker(comp.GetSemanticModel(tree));
                w.Visit(tree.GetRoot());
            }
        }

        private static void ReportProjectRefs(Project proj)
        {
            Console.WriteLine("Project references:");
            foreach (var r in proj.MetadataReferences)
            {
                Console.WriteLine($"    {r.Display}");
            }
        }

        /// <summary>
        /// Returns the project and the number of failure events received while opening the project.
        /// </summary>
        private static async Task<(Project, int)> OpenProject(string path)
        {
            var errCount = 0;
            using var ws = MSBuildWorkspace.Create();
            ws.WorkspaceFailed += (_, e) =>
            {
                if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                {
                    Console.WriteLine($"{e.Diagnostic.Kind} {e.Diagnostic.Message}");
                    errCount++;
                }
            };
            
            Console.WriteLine($"Opening project: {path}");
            var proj = await ws.OpenProjectAsync(path);
            
            return (proj, errCount);
        }

        private static async Task<Compilation> CompileProject(Project proj)
        {
            Console.WriteLine($"Compiling project: {proj.Name}");
            var comp = await proj.GetCompilationAsync();
            var items = comp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            if (items.Length > 0)
            {
                foreach (var item in items)
                {
                    Console.WriteLine($"{item.Severity} {item.GetMessage()} {item.ToString()}");
                }
            }
            Console.WriteLine($"Compiled with {items.Length} messages.");
            return comp;
        }

        private static bool TryRegisterMSBuild()
        {

            PrintMSBuildEnv();

            var vsinstances = MSBuildLocator.QueryVisualStudioInstances().ToArray();
            if (vsinstances.Length == 0)
            {
                Console.WriteLine("No MSBuild instance found.");
                return false;
            }

            if (vsinstances.Length > 1)
            {
                var list = vsinstances.Select(i => i.MSBuildPath);
                Console.WriteLine($"Multiple instances of MSBuild found: {string.Join(", ", list)}");
                return false;
            }

            var inst = vsinstances[0];
            Console.WriteLine($"MSBuild: {inst.Name} {inst.Version} {inst.MSBuildPath}");
            MSBuildLocator.RegisterInstance(vsinstances[0]);

            PrintMSBuildEnv();

            return true;
        }

        private static void PrintMSBuildEnv()
        {
            Console.WriteLine("MSBuild environment:");
            foreach (var env in Environment.GetEnvironmentVariables().Keys)
            {
                if (env.ToString().ToUpper().StartsWith("MSBUILD"))
                    Console.WriteLine($"    {env} = {Environment.GetEnvironmentVariable(env.ToString())}");
            }
        }
    }
}
