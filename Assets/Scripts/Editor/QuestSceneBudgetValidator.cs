#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace QuestCommandRTS.Editor
{
    public static class QuestSceneBudgetValidator
    {
        [MenuItem("Tools/Quest RTS/Validate Quest Scene Budget")]
        public static void ValidateQuestSceneBudget()
        {
            List<RtsSceneBudgetItem> results = BuildGeneratedQuestSceneBudgetReport();
            bool failed = false;
            for (int i = 0; i < results.Count; i++)
            {
                RtsSceneBudgetItem item = results[i];
                string message = item.Label + ": " + item.Detail;
                if (item.Passed)
                {
                    Debug.Log("[Quest RTS Budget] PASS - " + message);
                }
                else
                {
                    failed = true;
                    Debug.LogError("[Quest RTS Budget] FAIL - " + message);
                }
            }

            if (failed)
            {
                Debug.LogError("[Quest RTS Budget] Generated Quest scene budget validation found blocking local-budget issues. This does not replace headset profiling.");
            }
            else
            {
                Debug.Log("[Quest RTS Budget] Generated Quest scene budget validation completed. Headset frame timing still requires device profiling.");
            }
        }

        internal static List<RtsSceneBudgetItem> BuildGeneratedQuestSceneBudgetReport()
        {
            RtsRuntimeModeResolver.ForceModeForTests(RtsRuntimeMode.QuestVr);
            GameObject root = new GameObject("Quest Scene Budget Runtime");
            RtsGame game = root.AddComponent<RtsGame>();
            try
            {
                game.Initialize();
                Physics.SyncTransforms();
                return RtsSceneBudgetReport.BuildQuestBudget(game);
            }
            finally
            {
                RtsRuntimeModeResolver.ForceModeForTests(null);
                Object.DestroyImmediate(root);
            }
        }
    }
}
#endif
