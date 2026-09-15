using UnityEngine;
using System;

public class AdsManager : MonoBehaviour
{
    public Action OnUserEarnedReward;

    public void RequestInterstitial()
    {
        // Advertising is intentionally disabled until production ad credentials
        // and the complete mobile ads SDK are configured.
    }

    public void ShowInterstitial()
    {
        // No-op in the ad-free production build.
    }

    public void RequestRewardedAd()
    {
        // No-op in the ad-free production build.
    }

    public void ShowRewardedAd()
    {
        // Preserve the gameplay reward while no ad provider is configured.
        OnUserEarnedReward?.Invoke();
    }
}
