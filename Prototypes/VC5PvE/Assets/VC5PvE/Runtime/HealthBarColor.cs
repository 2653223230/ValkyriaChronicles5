using UnityEngine;
namespace VC5PvE
{
    public static class HealthBarColor
    {
        public static Color Evaluate(float ratio)
        {
            ratio=Mathf.Clamp01(ratio);
            Color red=new Color(.96f,.22f,.18f),yellow=new Color(1f,.79f,.22f),green=new Color(.34f,.90f,.57f);
            if(ratio<.25f)return Color.Lerp(new Color(.75f,.10f,.12f),red,ratio*4f);
            return ratio<.5f?Color.Lerp(red,yellow,(ratio-.25f)*4f):Color.Lerp(yellow,green,(ratio-.5f)*2f);
        }
    }
}
