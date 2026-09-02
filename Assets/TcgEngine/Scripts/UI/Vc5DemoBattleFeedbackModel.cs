using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TcgEngine.UI
{
    public class Vc5DemoBattleFeedbackModel
    {
        private readonly int capacity;
        private readonly List<string> actions = new List<string>();

        public Vc5DemoBattleFeedbackModel(int capacity = 3)
        {
            this.capacity = capacity < 1 ? 1 : capacity;
        }

        public void AddAction(string action)
        {
            if (string.IsNullOrWhiteSpace(action))
                return;

            actions.Add(action);
            while (actions.Count > capacity)
                actions.RemoveAt(0);
        }

        public IReadOnlyList<string> GetActions()
        {
            return actions;
        }

        public static void GetScoreAwards(
            int localCount, int opponentCount, out int localAward, out int opponentAward)
        {
            localAward = localCount > opponentCount ? 2 : localCount == opponentCount ? 1 : 0;
            opponentAward = opponentCount > localCount ? 2 : localCount == opponentCount ? 1 : 0;
        }

        public static string BuildScoreSummary(
            int localScore, int opponentScore, int localZoneCount, int opponentZoneCount)
        {
            GetScoreAwards(localZoneCount, opponentZoneCount, out int localAward, out int opponentAward);
            return "我方 " + localScore + "/9   AI " + opponentScore + "/9\n"
                + "得分区  我方 " + localZoneCount + " | AI " + opponentZoneCount + "\n"
                + "若现在结算：我方 +" + localAward + "，AI +" + opponentAward;
        }

        public static string BuildAiCardMessage(string title, int manaCost)
        {
            string safeTitle = string.IsNullOrWhiteSpace(title) ? "未知卡牌" : title;
            return "AI 打出【" + safeTitle + "】（" + manaCost + "费）";
        }

        public static string BuildShortDescription(string description, int maxLength = 42)
        {
            if (string.IsNullOrWhiteSpace(description))
                return "";
            string plain = Regex.Replace(description, "<.*?>", "");
            plain = plain.Replace("\r", "").Replace("\n", "").Trim();
            if (plain.Length > maxLength)
                plain = plain.Substring(0, maxLength - 1) + "…";
            return plain;
        }

        public static string BuildScoreChangeMessage(int localDelta, int opponentDelta)
        {
            if (localDelta > 0 && opponentDelta > 0)
                return "双方得分  我方 +" + localDelta + "，AI +" + opponentDelta;
            if (localDelta > 0)
                return "我方得分  +" + localDelta;
            if (opponentDelta > 0)
                return "AI 得分  +" + opponentDelta;
            return "比分未变化";
        }

        public static string BuildEndReason(
            bool localWon, int localScore, int opponentScore, int localDeckCount, int opponentDeckCount)
        {
            if (localWon && localScore >= 9)
                return "胜利：我方达到 9 分";
            if (!localWon && opponentScore >= 9)
                return "失败：AI 达到 9 分";
            if (localWon && opponentDeckCount == 0)
                return "胜利：AI 需要抽牌时牌库为空";
            if (!localWon && localDeckCount == 0)
                return "失败：我方需要抽牌时牌库为空";
            return localWon ? "胜利" : "失败";
        }
    }
}
