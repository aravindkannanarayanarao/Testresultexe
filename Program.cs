using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.VisualBasic;

class Program
{
    public static List<string> controls = new List<string>();  // Ensure list is initialized

    static void Main(string[] args)
    {
        string filePath = "../../../index.html";
        string baseDirectory = "../../../ControlIndex";
        string pathxml = @"../../../Reports/SfMaps";
        List<string> versions = new List<string>();
        string inputVersion = "";

        if (File.Exists(filePath))
        {
            versions = ExtractExistingVersions(filePath);
        }

        Console.WriteLine("Enter new versions to add (type 'done' to finish):");
        while (true)
        {
            string input = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input) || input.ToLower() == "done") break;

            if (!versions.Contains(input)) // Avoid duplicate entries
            {
                versions.Add(input);
                inputVersion = input;
                CreateVersionFolder(baseDirectory, input, controls);
            }
        }

        GenerateIndexHtml(versions, filePath);
        Console.WriteLine("index.html updated successfully!");

        // Generate XML-based summary
        if (!string.IsNullOrEmpty(inputVersion))
        {
            GenerateXMLFileForControls(pathxml, baseDirectory, inputVersion, controls);
        }
    }

    static List<string> ExtractExistingVersions(string filePath)
    {
        List<string> existingVersions = new List<string>();
        string[] lines = File.ReadAllLines(filePath);

        foreach (string line in lines)
        {
            if (line.Contains("ControlIndex/"))
            {
                int start = line.IndexOf("ControlIndex/") + 13;
                int end = line.IndexOf("/index.html", start);
                if (start > 12 && end > start)
                {
                    string version = line.Substring(start, end - start);
                    existingVersions.Add(version);
                }
            }
        }
        return existingVersions;
    }

    static void CreateVersionFolder(string baseDirectory, string version, List<string> controls)
    {
        string versionPath = Path.Combine(baseDirectory, version);
        if (!Directory.Exists(versionPath))
        {
            Directory.CreateDirectory(versionPath);
        }

        Console.WriteLine("Enter control names for version " + version + " (type 'done' to finish):");
        while (true)
        {
            string controlInput = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(controlInput) || controlInput.ToLower() == "done") break;
            controls.Add(controlInput);
        }

        CreateVersionIndex(versionPath, version, controls);
        Console.WriteLine("Updated folder: " + versionPath);
    }

    static void GenerateXMLFileForControls(string pathxml, string baseDirectory, string version, List<string> controls)
    {
        List<string> directoryPaths = new List<string>();
        foreach (var control in controls)
        {
            string path = Path.Combine("../../../Reports", version, control);
            directoryPaths.Add(path);
        }

        List<string> xmlFiles = new List<string>();
        foreach (var directoryPath in directoryPaths)
        {
            if (Directory.Exists(directoryPath))
            {
                xmlFiles.AddRange(Directory.GetFiles(directoryPath, "*.xml"));
            }
        }

        if (xmlFiles.Count == 0)
        {
            Console.WriteLine("No XML files found in the directories. Exiting...");
            return;
        }

        int totalTests = 0, passedTests = 0, failedTests = 0, skippedTests = 0;
        double totalDuration = 0;
        var controlResults = new Dictionary<string, (int total, int passed, int failed, int skipped)>();

        foreach (string filePath in xmlFiles)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);

                var countersNode = xmlDoc.GetElementsByTagName("Counters")[0];
                var timesNode = xmlDoc.GetElementsByTagName("Times")[0];
                var testCases = xmlDoc.GetElementsByTagName("UnitTestResult");

                totalTests += int.Parse(countersNode?.Attributes?["total"]?.Value ?? "0");
                passedTests += int.Parse(countersNode?.Attributes?["passed"]?.Value ?? "0");
                failedTests += int.Parse(countersNode?.Attributes?["failed"]?.Value ?? "0");
                skippedTests += int.Parse(countersNode?.Attributes?["skipped"]?.Value ?? "0");

                DateTime startTime = DateTime.Parse(timesNode?.Attributes?["start"]?.Value ?? DateTime.Now.ToString());
                DateTime finishTime = DateTime.Parse(timesNode?.Attributes?["finish"]?.Value ?? DateTime.Now.ToString());
                totalDuration += (finishTime - startTime).TotalSeconds;

                foreach (XmlNode testCase in testCases)
                {
                    string status = testCase.Attributes?["outcome"]?.Value;
                    string testName = testCase.Attributes?["testName"]?.Value?.Split('_')[0] ?? "Unknown";

                    if (!controlResults.ContainsKey(testName))
                        controlResults[testName] = (0, 0, 0, 0);

                    var (total, passed, failed, skipped) = controlResults[testName];
                    total++;
                    if (status == "Passed") passed++;
                    if (status == "Failed") failed++;
                    if (status == "Skipped") skipped++;
                    controlResults[testName] = (total, passed, failed, skipped);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {filePath}: {ex.Message}");
            }
        }

        // Generate HTML report
        foreach (var control in controls)
        {
            var htmlpage = control+".html";
        string outputFilePath = Path.Combine(baseDirectory, version,htmlpage);
        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath) ?? baseDirectory);
        File.WriteAllText(outputFilePath, GenerateHtmlReport(totalTests, passedTests, failedTests, skippedTests, totalDuration, controlResults));

        Console.WriteLine($"HTML summary generated: {outputFilePath}");
        }
    }
static void CreateVersionIndex(string versionPath, string version, List<string> controls)
    {
        using (StreamWriter writer = new StreamWriter(Path.Combine(versionPath, "index.html")))
        {
            writer.WriteLine("<!DOCTYPE html>");
            writer.WriteLine("<html lang=\"en\">");
            writer.WriteLine("<head>");
            writer.WriteLine("    <meta charset=\"UTF-8\">");
            writer.WriteLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            writer.WriteLine("    <title>MAUI Syncfusion Controls - Test Reports</title>");
            writer.WriteLine("    <style>");
            writer.WriteLine("        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f4f4f4; }");
            writer.WriteLine("        table { width: 100%; border-collapse: collapse; margin-top: 20px; background: white; }");
            writer.WriteLine("        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            writer.WriteLine("        th { background-color: #007bff; color: white; }");
            writer.WriteLine("        .passed { color: green; font-weight: bold; }");
            writer.WriteLine("        .failed { color: red; font-weight: bold; }");
            writer.WriteLine("        a { text-decoration: none; color: #007bff; font-weight: bold; }");
            writer.WriteLine("        a:hover { text-decoration: underline; }");
            writer.WriteLine("    </style>");
            writer.WriteLine("</head>");
            writer.WriteLine("<body>");
            writer.WriteLine($"    <h2 style=\"text-align: center;\">MAUI Syncfusion Controls - Test Reports (Version {version})</h2>");
            writer.WriteLine("    <h3>Test Summary</h3>");
            writer.WriteLine("    <table>");
            writer.WriteLine("        <thead>");
            writer.WriteLine("            <tr>");
            writer.WriteLine("                <th>Control Name</th>");
            writer.WriteLine("                <th>Test Cases</th>");
            writer.WriteLine("                <th>Passed</th>");
            writer.WriteLine("                <th>Failed</th>");
            writer.WriteLine("                <th>Skipped</th>");
            writer.WriteLine("            </tr>");
            writer.WriteLine("        </thead>");
            writer.WriteLine("        <tbody>");
            
            foreach (var control in controls)
            {
                writer.WriteLine($"            <tr>");
                writer.WriteLine($"                <td><a href='{control}.html'>{control}</a></td>");
                writer.WriteLine($"                <td>0</td>");
                writer.WriteLine($"                <td class='passed'>0</td>");
                writer.WriteLine($"                <td class='failed'>0</td>");
                writer.WriteLine($"                <td>0</td>");
                writer.WriteLine($"            </tr>");
            }
            
            
            writer.WriteLine("        </tbody>");
            writer.WriteLine("    </table>");
            writer.WriteLine("</body>");
            writer.WriteLine("</html>");
        }
    }




    static string GenerateHtmlReport(int totalTests, int passedTests, int failedTests, int skippedTests, double totalDuration, Dictionary<string, (int total, int passed, int failed, int skipped)> controlResults)
    {
        string htmlContent = $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <title>Test Results Summary</title>
    <style>
        body {{ font-family: Arial, sans-serif; margin: 20px; background-color: #f8f9fa; }}
        table {{ width: 100%; border-collapse: collapse; background: white; }}
        th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; }}
        th {{ background-color: #007bff; color: white; }}
        .passed {{ color: green; }} 
        .failed {{ color: red; }}
    </style>
</head>
<body>
    <h2>Test Results Summary</h2>
    <p>Total Tests: {totalTests}</p>
    <p>Passed: <span class='passed'>{passedTests}</span></p>
    <p>Failed: <span class='failed'>{failedTests}</span></p>
    <p>Skipped: {skippedTests}</p>
    <p>Execution Time: {totalDuration:F2} seconds</p>
</body>
</html>";
        return htmlContent;
    }
     static void GenerateIndexHtml(List<string> versions, string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.WriteLine("<!DOCTYPE html>");
            writer.WriteLine("<html lang=\"en\">");
            writer.WriteLine("<head>");
            writer.WriteLine("    <meta charset=\"UTF-8\">");
            writer.WriteLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            writer.WriteLine("    <title>Syncfusion Test Results</title>");
            writer.WriteLine("    <style>");
            writer.WriteLine("        body { font-family: Arial, sans-serif; margin: 20px; background-color: #f4f4f4; }");
            writer.WriteLine("        h2 { text-align: center; }");
            writer.WriteLine("        ul { list-style-type: none; padding: 0; text-align: center; }");
            writer.WriteLine("        li { margin: 10px 0; }");
            writer.WriteLine("        a { text-decoration: none; color: #007bff; font-size: 18px; font-weight: bold; }");
            writer.WriteLine("        a:hover { text-decoration: underline; }");
            writer.WriteLine("    </style>");
            writer.WriteLine("</head>");
            writer.WriteLine("<body>");
            writer.WriteLine("    <h2>Syncfusion Test Results</h2>");
            writer.WriteLine("    <h3 style=\"text-align: center;\">Versions</h3>");
            writer.WriteLine("    <ul>");

            foreach (var version in versions)
            {
                writer.WriteLine($"        <li><a href='ControlIndex/{version}/index.html'>{version}</a></li>");
            }
            
            writer.WriteLine("    </ul>");
            writer.WriteLine("</body>");
            writer.WriteLine("</html>");
        }
    }
}
