#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using NUnit.Framework.Interfaces;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.TestTools.TestRunner.GUI;

namespace QuestCommandRTS.Editor
{
    public static class RtsBatchTestRunner
    {
        private const string DefaultResultsPath = "Logs/EditModeResults.xml";
        private const string NUnitVersion = "3.5.0.0";
        private const string TimeFormat = "u";

        public static void RunEditModeTests()
        {
            string resultsPath = ResolveResultsPath(GetCommandLineOption("-testResults", "-rtsTestResults"), DefaultResultsPath);
            Directory.CreateDirectory(Path.GetDirectoryName(resultsPath) ?? ".");
            Filter filter = BuildEditModeFilter();

            BatchTestCallbacks callbacks = new BatchTestCallbacks(resultsPath);
            TestRunnerApi api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(callbacks);

            try
            {
                Debug.Log("[Command RTS Tests] Starting synchronous EditMode test run." + FormatFilter(filter));
                api.Execute(new ExecutionSettings(filter)
                {
                    runSynchronously = true
                });

                if (callbacks.Result == null)
                {
                    throw new InvalidOperationException("Unity TestRunnerApi completed without RunFinished callback.");
                }

                WriteResults(callbacks.Result, resultsPath);
                int total = callbacks.Result.PassCount + callbacks.Result.FailCount + callbacks.Result.SkipCount + callbacks.Result.InconclusiveCount;
                Debug.Log(
                    "[Command RTS Tests] Finished EditMode tests: total=" + total.ToString(CultureInfo.InvariantCulture) +
                    " passed=" + callbacks.Result.PassCount.ToString(CultureInfo.InvariantCulture) +
                    " failed=" + callbacks.Result.FailCount.ToString(CultureInfo.InvariantCulture) +
                    " skipped=" + callbacks.Result.SkipCount.ToString(CultureInfo.InvariantCulture) +
                    " inconclusive=" + callbacks.Result.InconclusiveCount.ToString(CultureInfo.InvariantCulture) +
                    " results=" + resultsPath);

                if (IsFailed(callbacks.Result))
                {
                    EditorApplication.Exit(1);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("[Command RTS Tests] EditMode test run failed.");
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
            finally
            {
                api.UnregisterCallbacks(callbacks);
                UnityEngine.Object.DestroyImmediate(api);
            }
        }

        private static string GetCommandLineOption(params string[] optionNames)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                for (int optionIndex = 0; optionIndex < optionNames.Length; optionIndex++)
                {
                    if (string.Equals(args[i], optionNames[optionIndex], StringComparison.OrdinalIgnoreCase))
                    {
                        return args[i + 1];
                    }
                }
            }

            return string.Empty;
        }

        private static string[] GetCommandLineValues(params string[] optionNames)
        {
            List<string> values = new List<string>();
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (!MatchesOption(args[i], optionNames))
                {
                    continue;
                }

                int valueIndex = i + 1;
                while (valueIndex < args.Length && !args[valueIndex].StartsWith("-", StringComparison.Ordinal))
                {
                    AddSplitValues(values, args[valueIndex]);
                    valueIndex++;
                }
            }

            return values.Count > 0 ? values.ToArray() : null;
        }

        private static bool MatchesOption(string candidate, string[] optionNames)
        {
            for (int i = 0; i < optionNames.Length; i++)
            {
                if (string.Equals(candidate, optionNames[i], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddSplitValues(List<string> values, string rawValue)
        {
            string[] parts = rawValue.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string trimmed = parts[i].Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    values.Add(trimmed);
                }
            }
        }

        private static Filter BuildEditModeFilter()
        {
            return new Filter
            {
                testMode = TestMode.EditMode,
                groupNames = GetCommandLineValues("-testFilter", "-editorTestsFilter", "-rtsTestFilter"),
                categoryNames = GetCommandLineValues("-testCategory", "-editorTestsCategories", "-rtsTestCategory"),
                assemblyNames = GetCommandLineValues("-assemblyNames", "-rtsTestAssembly")
            };
        }

        private static string FormatFilter(Filter filter)
        {
            return " filters=" + FormatValues(filter.groupNames) +
                " categories=" + FormatValues(filter.categoryNames) +
                " assemblies=" + FormatValues(filter.assemblyNames);
        }

        private static string FormatValues(string[] values)
        {
            return values == null || values.Length == 0 ? "<all>" : string.Join(",", values);
        }

        private static string ResolveResultsPath(string requestedPath, string fallbackPath)
        {
            string path = string.IsNullOrWhiteSpace(requestedPath) ? fallbackPath : requestedPath;
            return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
        }

        private static void WriteResults(ITestResultAdaptor result, string filePath)
        {
            TNode testRunNode = new TNode("test-run");
            int total = result.PassCount + result.FailCount + result.SkipCount + result.InconclusiveCount;
            testRunNode.AddAttribute("id", "2");
            testRunNode.AddAttribute("testcasecount", total.ToString(CultureInfo.InvariantCulture));
            testRunNode.AddAttribute("result", result.ResultState);
            testRunNode.AddAttribute("total", total.ToString(CultureInfo.InvariantCulture));
            testRunNode.AddAttribute("passed", result.PassCount.ToString(CultureInfo.InvariantCulture));
            testRunNode.AddAttribute("failed", result.FailCount.ToString(CultureInfo.InvariantCulture));
            testRunNode.AddAttribute("inconclusive", result.InconclusiveCount.ToString(CultureInfo.InvariantCulture));
            testRunNode.AddAttribute("skipped", result.SkipCount.ToString(CultureInfo.InvariantCulture));
            testRunNode.AddAttribute("asserts", result.AssertCount.ToString(CultureInfo.InvariantCulture));
            testRunNode.AddAttribute("engine-version", NUnitVersion);
            testRunNode.AddAttribute("clr-version", Environment.Version.ToString());
            testRunNode.AddAttribute("start-time", result.StartTime.ToString(TimeFormat));
            testRunNode.AddAttribute("end-time", result.EndTime.ToString(TimeFormat));
            testRunNode.AddAttribute("duration", result.Duration.ToString(CultureInfo.InvariantCulture));
            testRunNode.ChildNodes.Add(result.ToXml());

            using (StreamWriter streamWriter = File.CreateText(filePath))
            using (XmlWriter xmlWriter = XmlWriter.Create(streamWriter, new XmlWriterSettings { Indent = true }))
            {
                testRunNode.WriteTo(xmlWriter);
            }
        }

        private static bool IsFailed(ITestResultAdaptor result)
        {
            return result != null &&
                (result.FailCount > 0 || string.Equals(result.ResultState, "Failed", StringComparison.Ordinal) || result.ResultState.StartsWith("Failed:", StringComparison.Ordinal));
        }

        private sealed class BatchTestCallbacks : ICallbacks
        {
            private readonly string resultsPath;

            public BatchTestCallbacks(string resultsPath)
            {
                this.resultsPath = resultsPath;
            }

            public ITestResultAdaptor Result { get; private set; }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log("[Command RTS Tests] Run started: " + (testsToRun != null ? testsToRun.TestCaseCount.ToString(CultureInfo.InvariantCulture) : "unknown") + " test cases.");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                Result = result;
                Debug.Log("[Command RTS Tests] Run finished; writing results to " + resultsPath);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (IsFailed(result))
                {
                    Debug.LogError("[Command RTS Tests] Failed: " + result.FullName + "\n" + result.Message + "\n" + result.StackTrace);
                }
            }
        }
    }
}
#endif
