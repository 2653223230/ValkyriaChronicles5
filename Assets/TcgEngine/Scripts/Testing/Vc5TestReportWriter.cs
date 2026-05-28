using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace TcgEngine.Testing
{
    public static class Vc5TestReportWriter
    {
        private static Vc5LogicTestReport currentReport;
        private static readonly string ReportDir = Path.Combine(Application.dataPath, "..", "TestResults");

        public static void BeginSession()
        {
            currentReport = new Vc5LogicTestReport
            {
                generated_at = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                project_version = Application.unityVersion,
                cases = new System.Collections.Generic.List<Vc5LogicTestCaseResult>()
            };
        }

        public static void Record(string testName, bool passed, string message, string expected, string actual, Vc5GameSnapshot snapshot, long durationMs)
        {
            if (currentReport == null)
                BeginSession();

            currentReport.cases.Add(new Vc5LogicTestCaseResult
            {
                name = testName,
                passed = passed,
                message = message ?? string.Empty,
                expected = expected ?? string.Empty,
                actual = actual ?? string.Empty,
                snapshot = snapshot,
                duration_ms = durationMs
            });
        }

        public static void FlushToDisk()
        {
            if (currentReport == null)
                return;

            int passed = 0;
            foreach (Vc5LogicTestCaseResult testCase in currentReport.cases)
            {
                if (testCase.passed)
                    passed++;
            }

            currentReport.passed = passed;
            currentReport.failed = currentReport.cases.Count - passed;
            currentReport.total = currentReport.cases.Count;

            Directory.CreateDirectory(ReportDir);

            string jsonPath = Path.Combine(ReportDir, "vc5_logic_latest.json");
            string mdPath = Path.Combine(ReportDir, "vc5_logic_latest.md");
            File.WriteAllText(jsonPath, BuildJson(currentReport), Encoding.UTF8);
            File.WriteAllText(mdPath, Vc5TestSnapshotUtil.FormatReportMarkdown(currentReport), Encoding.UTF8);

            Debug.Log($"[Vc5Tests] Report written: {jsonPath} ({currentReport.passed}/{currentReport.total} passed)");
        }

        private static string BuildJson(Vc5LogicTestReport report)
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append($"  \"generated_at\": {Quote(report.generated_at)},\n");
            sb.Append($"  \"project_version\": {Quote(report.project_version)},\n");
            sb.Append($"  \"passed\": {report.passed},\n");
            sb.Append($"  \"failed\": {report.failed},\n");
            sb.Append($"  \"total\": {report.total},\n");
            sb.Append("  \"cases\": [\n");

            for (int i = 0; i < report.cases.Count; i++)
            {
                Vc5LogicTestCaseResult testCase = report.cases[i];
                sb.Append("    {\n");
                sb.Append($"      \"name\": {Quote(testCase.name)},\n");
                sb.Append($"      \"passed\": {(testCase.passed ? "true" : "false")},\n");
                sb.Append($"      \"message\": {Quote(testCase.message)},\n");
                sb.Append($"      \"expected\": {Quote(testCase.expected)},\n");
                sb.Append($"      \"actual\": {Quote(testCase.actual)},\n");
                sb.Append($"      \"duration_ms\": {testCase.duration_ms},\n");
                sb.Append("      \"snapshot\": ");
                sb.Append(SnapshotToJson(testCase.snapshot));
                sb.Append("\n    }");
                if (i < report.cases.Count - 1)
                    sb.Append(',');
                sb.Append('\n');
            }

            sb.Append("  ]\n}");
            return sb.ToString();
        }

        private static string SnapshotToJson(Vc5GameSnapshot snapshot)
        {
            if (snapshot == null)
                return "null";

            var sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append($"        \"turn_count\": {snapshot.turn_count},\n");
            sb.Append($"        \"current_player\": {snapshot.current_player},\n");
            sb.Append($"        \"phase\": {Quote(snapshot.phase)},\n");
            sb.Append($"        \"selector\": {Quote(snapshot.selector)},\n");
            sb.Append("        \"players\": [");

            for (int i = 0; i < snapshot.players.Count; i++)
            {
                Vc5PlayerSnapshot player = snapshot.players[i];
                if (i > 0) sb.Append(',');
                sb.Append("\n          {");
                sb.Append($"\"player_id\":{player.player_id},\"hp\":{player.hp},\"mana\":{player.mana},");
                sb.Append($"\"hand_count\":{player.hand_count},\"board_count\":{player.board_count},\"deck_count\":{player.deck_count}");
                sb.Append('}');
            }

            sb.Append("\n        ],\n        \"board\": [");

            for (int i = 0; i < snapshot.board.Count; i++)
            {
                Vc5CardSnapshot card = snapshot.board[i];
                if (i > 0) sb.Append(',');
                sb.Append("\n          {");
                sb.Append($"\"uid\":{Quote(card.uid)},\"card_id\":{Quote(card.card_id)},\"player_id\":{card.player_id},");
                sb.Append($"\"slot_x\":{card.slot_x},\"slot_y\":{card.slot_y},\"slot_p\":{card.slot_p},");
                sb.Append($"\"hp\":{card.hp},\"attack\":{card.attack},\"statuses\":[");
                for (int s = 0; s < card.statuses.Count; s++)
                {
                    Vc5StatusSnapshot status = card.statuses[s];
                    if (s > 0) sb.Append(',');
                    sb.Append("{");
                    sb.Append($"\"type\":{Quote(status.type)},\"value\":{status.value},\"duration\":{status.duration}");
                    sb.Append('}');
                }
                sb.Append("]}");
            }

            sb.Append("\n        ]\n      }");
            return sb.ToString();
        }

        private static string Quote(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";

            return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") + "\"";
        }
    }
}
