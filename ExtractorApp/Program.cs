using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ExtractorApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string sourceFile = @"r:\HDD R\ZC SYMLINK\USERS\source\repos\MMT Tools\MainWindow.xaml.cs";
            string projectDir = @"r:\HDD R\ZC SYMLINK\USERS\source\repos\MMT Tools";

            var fileContent = File.ReadAllText(sourceFile, Encoding.UTF8);
            var tree = CSharpSyntaxTree.ParseText(fileContent);
            var root = tree.GetRoot();

            // Keys are Tab names, Values are lists of keywords associated with them.
            // Any method containing the keyword will be moved to the respective Tab.
            var tabMappings = new Dictionary<string, string[]>
            {
                { "TabPopular", new[] {
                    "IDM", "BtnFixIDMExtension", "BtnCrackIDM", "WinRAR",
                    "BID", "BtnRunBIDActivation", "ActivateWindows", "PauseWindowsUpdate",
                    "Vcredist", "DirectX", "Java", "OpenAL", "3DPChip", "3DPNet", "RevoUninstaller"
                } },
                { "TabOffice", new[] { "OfficeToolPlus", "OfficeSoftmaker", "ActivateOffice", "Fonts", "NotepadPP" } },
                { "TabMultimedia", new[] { "PotPlayer", "FastStone", "Foxit", "Bandiview", "AdvancedCodec" } },
                { "TabSystem", new[] { "MMTApps", "DISMPP", "ComfortClipboardPro", "FolderSize", "MklinkMMT", "DefenderControl", "PowerISO", "VPN1111", "Teracopy", "GoogleDrive", "NetLimiter" } },
                { "TabPartition", new[] { "AomeiPartition", "PartitionAssistant", "DiskGenius" } },
                { "TabGaming", new[] { "ProcessLasso", "Throttlestop", "MSIAfterburner", "LeagueOfLegends", "Porofessor" } },
                { "TabBrowser", new[] { "Chrome", "CocCoc", "Edge" } },
                { "TabRemoteDesktop", new[] { "Ultraviewer", "TeamViewerQS", "TeamViewerFull", "TeamViewer", "AnyDesk" } },
                { "TabButtons", new[] { "DPIMinus", "DPIPlus", "CboDPIValue", "BtnDonate", "BtnSelectAll", "BtnSelectNone", "BtnSelectNoneAllTabs", "BtnInstall", "BtnStop", "BtnDownloadPage", "BtnExit" } },
            };

            var toRemove = new List<SyntaxNode>();
            var tabMethods = new Dictionary<string, List<MethodDeclarationSyntax>>();

            foreach (var tab in tabMappings.Keys)
            {
                tabMethods[tab] = new List<MethodDeclarationSyntax>();
            }

            var classDecls = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            var classDecl = classDecls.FirstOrDefault(c => c.Identifier.Text == "MainWindow");
            
            if (classDecl == null)
            {
                Console.WriteLine("MainWindow class not found.");
                return;
            }

            foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
            {
                string methodName = method.Identifier.Text;
                
                // Exclude some core methods
                if (methodName == "Window_Loaded" || 
                    methodName == "MainWindow_Closing" || 
                    methodName == "UpdateStatus" || 
                    methodName == "GetColor" || 
                    methodName == "ScrollToBottom" || 
                    methodName == "StartAutomatedProcessAsync" || 
                    methodName == "RunAutomatedProcessAsync" ||
                    methodName == "GetGMTPCFolder" ||
                    methodName == "DownloadWithProgressAsync" ||
                    methodName == "Window_KeyDown" ||
                    methodName == "Window_MouseWheel" ||
                    methodName == "AddDefenderExclusion" ||
                    methodName == "RemoveDefenderExclusion" ||
                    methodName == "PopulateSystemInfo" ||
                    methodName == "UpdateInstallButtonState")
                {
                    continue; // Do not move core methods
                }

                bool matched = false;
                foreach (var kvp in tabMappings)
                {
                    foreach(var keyword in kvp.Value)
                    {
                        if (methodName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        {
                            tabMethods[kvp.Key].Add(method);
                            toRemove.Add(method);
                            matched = true;
                            Console.WriteLine($"Matched method {methodName} -> {kvp.Key} (Rule: {keyword})");
                            break;
                        }
                    }
                    if (matched) break;
                }
                
                if (!matched)
                {
                    Console.WriteLine($"Unmatched method: {methodName}");
                }
            }

            Console.WriteLine($"Total methods to move: {toRemove.Count}");

            if (toRemove.Count == 0)
            {
                Console.WriteLine("No methods matched.");
                return;
            }

            // Remove methods from the original SyntaxTree
            var newRoot = root.RemoveNodes(toRemove, SyntaxRemoveOptions.KeepNoTrivia);
            var utf8BOM = new UTF8Encoding(true);
            
            Console.WriteLine("Writing original file...");
            File.WriteAllText(sourceFile, newRoot.ToFullString(), utf8BOM);

            foreach (var kvp in tabMethods)
            {
                if (kvp.Value.Count == 0) continue;

                var sb = new StringBuilder();
                sb.AppendLine("using System;");
                sb.AppendLine("using System.Collections.Generic;");
                sb.AppendLine("using System.Diagnostics;");
                sb.AppendLine("using System.IO;");
                sb.AppendLine("using System.Net;");
                sb.AppendLine("using System.Runtime.InteropServices;");
                sb.AppendLine("using System.Security.Principal;");
                sb.AppendLine("using System.Management;");
                sb.AppendLine("using Microsoft.Win32;");
                sb.AppendLine("using System.Threading;");
                sb.AppendLine("using System.Threading.Tasks;");
                sb.AppendLine("using System.Windows;");
                sb.AppendLine("using System.Windows.Media;");
                sb.AppendLine("using System.Windows.Input;");
                sb.AppendLine("using System.Net.Http;");
                sb.AppendLine("using System.Windows.Controls;");
                sb.AppendLine("using System.Windows.Data;");
                sb.AppendLine("");
                sb.AppendLine("namespace MMT_Tools");
                sb.AppendLine("{");
                sb.AppendLine("    public partial class MainWindow : Window");
                sb.AppendLine("    {");
                
                foreach (var method in kvp.Value)
                {
                    sb.AppendLine(method.ToFullString());
                }
                
                sb.AppendLine("    }");
                sb.AppendLine("}");

                string newFileName = Path.Combine(projectDir, $"MainWindow.{kvp.Key}.cs");
                File.WriteAllText(newFileName, sb.ToString(), utf8BOM);
                Console.WriteLine($"Created {newFileName} with {kvp.Value.Count} methods.");
            }
        }
    }
}
