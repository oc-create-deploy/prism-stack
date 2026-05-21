using UnityEngine;
using GoogleMobileAds.Api;
using System;

public class AdsManager : MonoBehaviour
{
    private BannerView bannerView;
    private InterstitialAd interstitial;
    private RewardedAd rewardedAd;

    public Action OnUserEarnedReward;

    void Start()
    {
        MobileAds.Initialize(initStatus => {
            RequestBanner();
            RequestInterstitial();
            RequestRewardedAd();
        });
    }

    private void RequestBanner()
    {
        string bannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
        bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Bottom);
        bannerView.LoadAd(new AdRequest());
    }

    public void RequestInterstitial()
    {
        string interstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";
        AdRequest request = new AdRequest();

        InterstitialAd.Load(interstitialAdUnitId, request,
            (InterstitialAd ad, LoadAdError loadError) => {
                if (loadError != null) {
                    Debug.LogError("Error carregant interstitial: " + loadError.GetMessage());
                    return;
                }
                interstitial = ad;
                interstitial.OnAdFullScreenContentClosed += () => {
                    Debug.Log("Interstitial tancat. Recarregant en background...");
                    RequestInterstitial();
                };
            }
        );
    }

    public void ShowInterstitial()
    {
        if (interstitial != null && interstitial.CanShowAd()) {
            interstitial.Show();
        } else {
            Debug.LogWarning("Interstitial no està disponible. Crida RequestInterstitial() i espera que es carregui.");
        }
    }

    public void RequestRewardedAd()
    {
        string rewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
        AdRequest request = new AdRequest();

        RewardedAd.Load(rewardedAdUnitId, request,
            (RewardedAd ad, LoadAdError loadError) => {
                if (loadError != null) {
                    Debug.LogError("Error carregant rewarded: " + loadError.GetMessage());
                    return;
                }
                rewardedAd = ad;

                rewardedAd.OnAdFullScreenContentClosed += () => {
                    Debug.Log("Rewarded tancat. Recarregant en background...");
                    RequestRewardedAd();
                };
            }
        );
    }

    public void ShowRewardedAd()
    {
        if (rewardedAd != null && rewardedAd.CanShowAd()) {
            rewardedAd.Show((Reward reward) => {
                Debug.Log($"Recompensa guanyada: {reward.Amount} {reward.Type}");
                OnUserEarnedReward?.Invoke();
            });
        } else {
            Debug.LogWarning("Rewarded no està disponible. Crida RequestRewardedAd() i espera que es carregui.");
        }
    }

    void OnDestroy()
    {
        if (bannerView != null) bannerView.Destroy();
        if (interstitial != null) interstitial.Destroy();
        if (rewardedAd != null) rewardedAd.Destroy();
    }
}