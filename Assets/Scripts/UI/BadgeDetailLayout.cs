using UnityEngine;
using UnityEngine.UI;

namespace WWG
{
    /// <summary>
    /// 工牌正反面详情。两张原图的画面文字已经承载姓名、编号和背面手写线索，
    /// 因此展示图片时收起旧版正文，避免两套详情 UI 叠在同一区域。
    /// </summary>
    public static class BadgeDetailLayout
    {
        public const string ArtworkName = "Badge artwork";

        public static void Apply(GameObject infoPanel, Text body, bool showBadge)
        {
            if (infoPanel == null) return;

            Transform artwork = infoPanel.transform.Find(ArtworkName);
            if (artwork != null) artwork.gameObject.SetActive(showBadge);

            if (body != null) body.gameObject.SetActive(!showBadge);
        }
    }
}
