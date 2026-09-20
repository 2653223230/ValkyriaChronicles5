using UnityEngine;

namespace TcgEngine
{
    public static partial class Vc5DemoBootstrap
    {
        public static bool HasDemoArt(string id)
        {
            if (id == Vc5BAI1Rules.Frontliner || id == Vc5BAI1Rules.Flanker || id == Vc5BAI1Rules.Rifleman) return true;
            if (id != null && id.StartsWith(Vc5R4Rules.Prefix)) return true;
            switch (id)
            {
                case "vc5_demo_c3_tactical_move":
                case "vc5_demo_c3_forced_march":
                case "vc5_demo_c3_temp_calibration":
                case "vc5_demo_c3_scope_upgrade":
                case "vc5_demo_c3_fire_coverage":
                case "vc5_demo_c3_heavy_break":
                case "vc5_demo_c3_weakpoint_snipe":
                case "vc5_demo_c3_mobile_shot":
                case "vc5_demo_c3_sniper":
                case "vc5_demo_c3_fire_guard":
                case "vc5_demo_c3_mobile_ranger":
                case "vc5_demo_cavalry":
                case "vc5_demo_assassin":
                case "vc5_demo_scout":
                    return true;
                default:
                    return false;
            }
        }

        private static void ApplyDemoArt(CardData card)
        {
            if (!HasDemoArt(card.id))
                return;

            string artId = card.id;
            if (artId == Vc5BAI1Rules.Frontliner) artId = "vc5_demo_cavalry";
            if (artId == Vc5BAI1Rules.Flanker) artId = "vc5_demo_assassin";
            if (artId == Vc5BAI1Rules.Rifleman) artId = "vc5_demo_scout";
            if (artId.StartsWith(Vc5R4Rules.Prefix))
            {
                if (!card.id.EndsWith("cover_deploy") && !card.id.EndsWith("watch_deploy"))
                {
                    artId = artId.Replace(Vc5R4Rules.Prefix, Vc5C3Rules.Prefix);
                    if (card.id == Vc5R4Rules.Commander) artId = Vc5C3Rules.Guard;
                    if (card.id.EndsWith("cover_order")) artId = Vc5C3Rules.Guard;
                    if (card.id.EndsWith("fire_order")) artId = Vc5C3Rules.Sniper;
                    if (card.id.EndsWith("advance_order")) artId = Vc5C3Rules.Ranger;
                }
            }
            Sprite art = Resources.Load<Sprite>("VC5/DemoArt/" + artId);
            card.art_full = art;
            if (card.IsCharacter())
                card.art_board = art;
        }
    }
}
