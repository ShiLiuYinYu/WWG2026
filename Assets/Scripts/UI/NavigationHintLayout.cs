using UnityEngine;
using UnityEngine.UI;

namespace WWG
{
    internal static class NavigationHintLayout
    {
        private const float ArrowGap = 10f;
        private const float ScreenMargin = 12f;

        internal static Image FindArrow(Transform hotspot)
        {
            Image marked = hotspot.Find("Navigation arrow")?.GetComponent<Image>();
            if (marked != null) return marked;
            Image image = hotspot.parent != null ? hotspot.parent.GetComponent<Image>() : null;
            return image != null && image.sprite != null && image.sprite.name.StartsWith("WhiteArrow")
                ? image
                : null;
        }

        internal static void Place(Text caption, Image arrow)
        {
            RectTransform captionRect = caption.rectTransform;
            RectTransform parent = (RectTransform)captionRect.parent;
            RectTransform arrowRect = arrow.rectTransform;
            Rect imageRect = arrow.GetPixelAdjustedRect();
            if (arrow.preserveAspect && arrow.sprite != null)
            {
                Vector2 spriteSize = arrow.sprite.rect.size;
                float scale = Mathf.Min(imageRect.width / spriteSize.x, imageRect.height / spriteSize.y);
                Vector2 fittedSize = spriteSize * scale;
                imageRect.position += Vector2.Scale(imageRect.size - fittedSize, arrowRect.pivot);
                imageRect.size = fittedSize;
            }

            Matrix4x4 arrowToParent = parent.worldToLocalMatrix * arrowRect.localToWorldMatrix;
            Vector3 arrowCenter = arrowToParent.MultiplyPoint3x4(imageRect.center);
            // Project both axes so rotated sprites use their visible vertical extent.
            Vector3 halfWidth = arrowToParent.MultiplyVector(new Vector3(imageRect.width * 0.5f, 0f, 0f));
            Vector3 halfHeight = arrowToParent.MultiplyVector(new Vector3(0f, imageRect.height * 0.5f, 0f));
            float verticalExtent = Mathf.Abs(halfWidth.y) + Mathf.Abs(halfHeight.y);

            Rect viewport = parent.rect;
            float halfTextWidth = caption.preferredWidth * 0.5f;
            float halfTextHeight = caption.preferredHeight * 0.5f;
            float horizontalInset = Mathf.Min(halfTextWidth + ScreenMargin, viewport.width * 0.5f);
            float verticalInset = Mathf.Min(halfTextHeight + ScreenMargin, viewport.height * 0.5f);
            float distance = verticalExtent + ArrowGap + halfTextHeight;
            float bottom = viewport.yMin + verticalInset;
            float y = arrowCenter.y - distance;
            if (y < bottom) y = arrowCenter.y + distance;

            Vector2 position = new Vector2(
                Mathf.Clamp(arrowCenter.x, viewport.xMin + horizontalInset, viewport.xMax - horizontalInset),
                Mathf.Clamp(y, bottom, viewport.yMax - verticalInset));
            Vector2 captionCenter = parent.InverseTransformPoint(captionRect.TransformPoint(captionRect.rect.center));
            captionRect.anchoredPosition += position - captionCenter;
        }
    }
}
