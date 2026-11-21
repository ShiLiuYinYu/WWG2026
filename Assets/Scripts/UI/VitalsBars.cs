using UnityEngine;
using UnityEngine.UI;

namespace WWG
{
    public sealed class VitalsBars : MonoBehaviour
    {
        private const float DrainSeconds = 0.3f;

        public Image sanFill;
        public Text sanValue;
        public Image healthFill;
        public Text healthValue;

        private BarState san;
        private BarState health;

        public void SetSan(int current, int max)
        {
            SetBar(sanFill, sanValue, "SAN", ref san, current, max);
        }

        public void SetHealth(int current, int max)
        {
            SetBar(healthFill, healthValue, "HP", ref health, current, max);
        }

        private void Update()
        {
            AdvanceDrain(sanFill, ref san);
            AdvanceDrain(healthFill, ref health);
        }

        private void OnDisable()
        {
            SnapToTarget(sanFill, ref san);
            SnapToTarget(healthFill, ref health);
        }

        private void SetBar(Image fill, Text value, string label, ref BarState bar, int current, int max)
        {
            if (bar.Initialized && bar.Current == current && bar.Max == max) return;

            float target = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            bool animate = fill != null && isActiveAndEnabled && bar.Initialized &&
                bar.Max == max && target < bar.Target;

            bar.Initialized = true;
            bar.Current = current;
            bar.Max = max;
            bar.Target = target;
            if (value != null) value.text = $"{label} {current}/{max}";

            if (animate)
            {
                bar.Start = fill.fillAmount;
                bar.Elapsed = 0f;
            }
            else
            {
                SnapToTarget(fill, ref bar);
            }
        }

        private static void AdvanceDrain(Image fill, ref BarState bar)
        {
            if (fill == null || !bar.Initialized || bar.Elapsed >= DrainSeconds) return;

            bar.Elapsed = Mathf.Min(bar.Elapsed + Time.unscaledDeltaTime, DrainSeconds);
            fill.fillAmount = Mathf.SmoothStep(bar.Start, bar.Target, bar.Elapsed / DrainSeconds);
        }

        private static void SnapToTarget(Image fill, ref BarState bar)
        {
            bar.Elapsed = DrainSeconds;
            if (fill != null && bar.Initialized) fill.fillAmount = bar.Target;
        }

        private struct BarState
        {
            public bool Initialized;
            public int Current;
            public int Max;
            public float Start;
            public float Target;
            public float Elapsed;
        }
    }
}
