using System;

namespace Destiny2.Common.Utilities
{
    public static class Easings
    {
        public static float EaseInSine(float x)
        {
            return 1f - MathF.Cos((x * MathF.PI) / 2f);
        }

        public static float EaseOutSine(float x)
        {
            return MathF.Sin((x * MathF.PI) / 2f);
        }

        public static float EaseInOutSine(float x)
        {
            return -(MathF.Cos(MathF.PI * x) - 1f) / 2f;
        }

        public static float EaseInQuad(float x)
        {
            return x * x;
        }

        public static float EaseOutQuad(float x)
        {
            return 1f - (1f - x) * (1f - x);
        }

        public static float EaseInOutQuad(float x)
        {
            return x < 0.5f ? 2f * x * x : 1f - MathF.Pow(-2f * x + 2f, 2f) / 2f;
        }

        public static float EaseInCubic(float x)
        {
            return x * x * x;
        }

        public static float EaseOutCubic(float x)
        {
            return 1f - MathF.Pow(1f - x, 3f);
        }

        public static float EaseInOutCubic(float x)
        {
            return x < 0.5f ? 4f * x * x * x : 1f - MathF.Pow(-2f * x + 2f, 3f) / 2f;
        }

        public static float EaseInQuint(float x)
        {
            return x * x * x * x * x;
        }

        public static float EaseOutQuint(float x)
        {
            return 1f - MathF.Pow(1f - x, 5f);
        }

        public static float EaseInOutQuint(float x)
        {
            return x < 0.5f ? 16f * x * x * x * x * x : 1f - MathF.Pow(-2f * x + 2f, 5f) / 2f;
        }

        public static float EaseInCirc(float x)
        {
            return 1f - MathF.Sqrt(1f - MathF.Pow(x, 2f));
        }

        public static float EaseOutCirc(float x)
        {
            return MathF.Sqrt(1f - MathF.Pow(x - 1f, 2f));
        }

        public static float EaseInOutCirc(float x)
        {
            return x < 0.5f
              ? (1f - MathF.Sqrt(1f - MathF.Pow(2f * x, 2f))) / 2f
              : (MathF.Sqrt(1f - MathF.Pow(-2f * x + 2f, 2f)) + 1f) / 2f;
        }

        public static float EaseOutElastic(float x)
        {
            const float c4 = (2f * MathF.PI) / 3f;
            return x == 0f
              ? 0f
              : x == 1f
              ? 1f
              : MathF.Pow(2f, -10f * x) * MathF.Sin((x * 10f - 0.75f) * c4) + 1f;
        }
    }
}
