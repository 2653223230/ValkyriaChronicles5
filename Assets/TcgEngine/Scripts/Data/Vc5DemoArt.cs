using UnityEngine;

namespace TcgEngine
{
    public static partial class Vc5DemoBootstrap
    {
        public static bool HasDemoArt(string id)
        {
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

            Sprite art = Resources.Load<Sprite>("VC5/DemoArt/" + card.id);
            card.art_full = art;
            if (card.IsCharacter())
                card.art_board = art;
        }
    }
}
