using UnityEngine;
using UnityEngine.UI;

namespace WWG
{
    public sealed class VitalsBars : MonoBehaviour
    {
        public Image sanFill;
        public Text sanValue;
        public Image healthFill;
        public Text healthValue;

        public void SetSan(int current, int max) { SetBar(sanFill, sanValue, current, max); }
        public void SetHealth(int current, int max) { SetBar(healthFill, healthValue, current, max); }

        private static void SetBar(Image fill, Text value, int current, int max)
        {
            if (fill != null)
            {
                // fillAmount 只对 Filled 类型的 Image 生效，Simple/Sliced 下条永远是满的。
                if (fill.type != Image.Type.Filled)
                {
                    fill.type = Image.Type.Filled;
                    fill.fillMethod = Image.FillMethod.Horizontal;
                    fill.fillOrigin = (int)Image.OriginHorizontal.Left;
                }
                fill.fillAmount = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            }
            if (value != null) value.text = current + "/" + max;
        }
    }
}
