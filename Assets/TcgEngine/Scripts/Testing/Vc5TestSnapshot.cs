using System;
using System.Collections.Generic;
using System.Text;

namespace TcgEngine.Testing
{
    [Serializable]
    public class Vc5CardSnapshot
    {
        public string uid;
        public string card_id;
        public int player_id;
        public int slot_x;
        public int slot_y;
        public int slot_p;
        public int hp;
        public int attack;
        public List<Vc5StatusSnapshot> statuses = new List<Vc5StatusSnapshot>();
    }

    [Serializable]
    public class Vc5StatusSnapshot
    {
        public string type;
        public int value;
        public int duration;
    }

    [Serializable]
    public class Vc5PlayerSnapshot
    {
        public int player_id;
        public int hp;
        public int mana;
        public int hand_count;
        public int board_count;
        public int deck_count;
    }

    [Serializable]
    public class Vc5GameSnapshot
    {
        public int turn_count;
        public int current_player;
        public string phase;
        public string selector;
        public List<Vc5PlayerSnapshot> players = new List<Vc5PlayerSnapshot>();
        public List<Vc5CardSnapshot> board = new List<Vc5CardSnapshot>();
    }

    [Serializable]
    public class Vc5LogicTestCaseResult
    {
        public string name;
        public bool passed;
        public string message;
        public string expected;
        public string actual;
        public long duration_ms;
        public Vc5GameSnapshot snapshot;
    }

    [Serializable]
    public class Vc5LogicTestReport
    {
        public string generated_at;
        public string project_version;
        public int passed;
        public int failed;
        public int total;
        public List<Vc5LogicTestCaseResult> cases = new List<Vc5LogicTestCaseResult>();
    }

    public static class Vc5TestSnapshotUtil
    {
        public static string FormatReportMarkdown(Vc5LogicTestReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("# VC5 Logic Test Report");
            sb.AppendLine();
            sb.AppendLine($"- Generated: {report.generated_at}");
            sb.AppendLine($"- Unity: {report.project_version}");
            sb.AppendLine($"- Summary: **{report.passed}/{report.total} passed**, {report.failed} failed");
            sb.AppendLine();

            foreach (Vc5LogicTestCaseResult testCase in report.cases)
            {
                sb.AppendLine(testCase.passed ? "## PASS" : "## FAIL");
                sb.AppendLine($"### {testCase.name}");
                sb.AppendLine();
                if (!string.IsNullOrEmpty(testCase.message))
                    sb.AppendLine($"Message: {testCase.message}");
                if (!string.IsNullOrEmpty(testCase.expected))
                    sb.AppendLine($"Expected: `{testCase.expected}`");
                if (!string.IsNullOrEmpty(testCase.actual))
                    sb.AppendLine($"Actual: `{testCase.actual}`");
                sb.AppendLine($"Duration: {testCase.duration_ms} ms");
                if (testCase.snapshot != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("Game snapshot (see JSON report for full detail):");
                    sb.AppendLine($"- turn={testCase.snapshot.turn_count}, current_player={testCase.snapshot.current_player}, selector={testCase.snapshot.selector}");
                    sb.AppendLine($"- board units={testCase.snapshot.board.Count}");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
