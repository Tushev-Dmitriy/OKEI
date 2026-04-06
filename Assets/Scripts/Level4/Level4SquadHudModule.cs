using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Level4SquadHudModule : MonoBehaviour
{
    private RectTransform _squadHudRoot;
    private TMP_Text _squadHudTitleText;
    private TMP_Text _squadHudCountText;
    private TMP_Text _squadHudHintText;
    private Button _squadHudClearButton;
    private CanvasGroup _squadHudCanvasGroup;
    private Coroutine _squadHudFadeCoroutine;
    private readonly Dictionary<RobotType, TMP_Text> _squadHudRoleCountTexts = new();
    private readonly Dictionary<RobotType, Image> _squadHudRoleIconImages = new();

    public Button ClearButton => _squadHudClearButton;

    public void UpdateSquadHud(Level4FlowController flow)
    {
        if (flow == null)
            return;

        if (_squadHudRoot == null)
            EnsureSquadHud(flow);

        if (_squadHudRoot == null)
            return;

        flow.BuildSquadHudSnapshot(
            out bool visible,
            out int limit,
            out bool canClear,
            out int attackers,
            out int healers,
            out int defenders,
            out int bases,
            out int total);

        SetSquadHudVisible(flow, visible);
        if (!visible)
            return;

        if (_squadHudTitleText != null)
            _squadHudTitleText.text = string.Empty;

        if (_squadHudCountText != null)
            _squadHudCountText.text = $"Выбрано: {total}/{limit}";

        if (_squadHudHintText != null)
            _squadHudHintText.text = string.Empty;

        SetSquadHudRoleCount(RobotType.Attacker, attackers);
        SetSquadHudRoleCount(RobotType.Healer, healers);
        SetSquadHudRoleCount(RobotType.Defender, defenders);
        SetSquadHudRoleCount(RobotType.Base, bases);

        if (_squadHudClearButton != null)
            _squadHudClearButton.interactable = canClear;
    }

    public void EnsureSquadHud(Level4FlowController flow)
    {
        if (flow == null)
            return;

        _squadHudRoleCountTexts.Clear();
        _squadHudRoleIconImages.Clear();

        if (!TryResolveExistingSquadHud(flow))
            return;

        if (_squadHudClearButton != null)
        {
            _squadHudClearButton.onClick.RemoveListener(flow.HandleSquadHudClearClicked);
            _squadHudClearButton.onClick.AddListener(flow.HandleSquadHudClearClicked);
        }

        if (_squadHudRoot != null)
        {
            _squadHudCanvasGroup = _squadHudRoot.GetComponent<CanvasGroup>();
            if (_squadHudCanvasGroup == null)
                _squadHudCanvasGroup = _squadHudRoot.gameObject.AddComponent<CanvasGroup>();
        }

        UpdateSquadHud(flow);
    }

    private void SetSquadHudRoleCount(RobotType robotType, int value)
    {
        if (_squadHudRoleCountTexts.TryGetValue(robotType, out TMP_Text countText) && countText != null)
            countText.text = value.ToString();
    }

    private bool TryResolveExistingSquadHud(Level4FlowController flow)
    {
        Transform hudRoot = FindObjectsByType<Transform>(FindObjectsSortMode.None)
            .FirstOrDefault(t => t != null && t.name == "Level4SquadHUD");

        if (hudRoot == null)
        {
            RectTransform[] allTransforms = Resources.FindObjectsOfTypeAll<RectTransform>();
            for (int i = 0; i < allTransforms.Length; i++)
            {
                RectTransform candidate = allTransforms[i];
                if (candidate == null || candidate.name != "Level4SquadHUD")
                    continue;

                Scene scene = candidate.gameObject.scene;
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                hudRoot = candidate;
                break;
            }
        }

        if (hudRoot == null)
            return false;

        _squadHudRoot = hudRoot as RectTransform;
        if (_squadHudRoot == null)
            return false;

        _squadHudTitleText = FindHudText("TitleText") ?? FindHudTextContains("title");
        _squadHudCountText = FindHudText("CountText") ?? FindHudTextContains("count");
        _squadHudHintText = FindHudText("HintText") ?? FindHudTextContains("hint");
        BindExistingRoleCounter(RobotType.Attacker, "Role_Attacker");
        BindExistingRoleCounter(RobotType.Healer, "Role_Healer");
        BindExistingRoleCounter(RobotType.Defender, "Role_Defender");
        BindExistingRoleCounter(RobotType.Base, "Role_Base");

        Transform clearButton = FindDescendantByName(_squadHudRoot, "ClearButton");
        _squadHudClearButton = clearButton != null
            ? clearButton.GetComponent<Button>()
            : _squadHudRoot.GetComponentsInChildren<Button>(true).FirstOrDefault();

        if (_squadHudTitleText == null)
            _squadHudTitleText = _squadHudRoot.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(t => t != null && t.name.IndexOf("title", StringComparison.OrdinalIgnoreCase) >= 0);

        if (_squadHudHintText == null)
            _squadHudHintText = _squadHudRoot.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(t => t != null && t.name.IndexOf("hint", StringComparison.OrdinalIgnoreCase) >= 0);

        if (_squadHudCountText == null)
            _squadHudCountText = _squadHudRoot.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(t => t != null && t.name.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0);

        RefreshSquadHudRoleIcons(flow);
        return true;
    }

    private TMP_Text FindHudText(string childName)
    {
        if (_squadHudRoot == null)
            return null;

        Transform child = FindDescendantByName(_squadHudRoot, childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private TMP_Text FindHudTextContains(string token)
    {
        if (_squadHudRoot == null || string.IsNullOrWhiteSpace(token))
            return null;

        TMP_Text[] texts = _squadHudRoot.GetComponentsInChildren<TMP_Text>(true);
        return texts.FirstOrDefault(t => t != null && t.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void BindExistingRoleCounter(RobotType type, string rootName)
    {
        if (_squadHudRoot == null)
            return;

        Transform roleRoot = FindDescendantByName(_squadHudRoot, rootName);
        if (roleRoot == null)
            return;

        Transform iconTransform = FindDescendantByName(roleRoot, "Icon");
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null)
                _squadHudRoleIconImages[type] = iconImage;
        }

        Transform countTransform = FindDescendantByName(roleRoot, "CountText");
        if (countTransform != null)
        {
            TMP_Text countText = countTransform.GetComponent<TMP_Text>();
            if (countText != null)
                _squadHudRoleCountTexts[type] = countText;
            return;
        }

        TMP_Text fallback = roleRoot.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault();
        if (fallback != null)
            _squadHudRoleCountTexts[type] = fallback;
    }

    private static Transform FindDescendantByName(Transform root, string name)
    {
        if (root == null || string.IsNullOrWhiteSpace(name))
            return null;

        if (string.Equals(root.name, name, StringComparison.Ordinal))
            return root;

        foreach (Transform child in root)
        {
            Transform nested = FindDescendantByName(child, name);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void SetSquadHudVisible(Level4FlowController flow, bool visible)
    {
        if (flow == null || _squadHudRoot == null)
            return;

        if (_squadHudCanvasGroup == null)
            _squadHudCanvasGroup = _squadHudRoot.GetComponent<CanvasGroup>();

        if (_squadHudCanvasGroup == null)
        {
            _squadHudRoot.gameObject.SetActive(visible);
            return;
        }

        if (visible && !_squadHudRoot.gameObject.activeSelf)
            _squadHudRoot.gameObject.SetActive(true);

        if (_squadHudFadeCoroutine != null)
            StopCoroutine(_squadHudFadeCoroutine);

        _squadHudFadeCoroutine = StartCoroutine(FadeSquadHudRoutine(flow, visible));
    }

    private IEnumerator FadeSquadHudRoutine(Level4FlowController flow, bool visible)
    {
        if (flow == null || _squadHudCanvasGroup == null || _squadHudRoot == null)
            yield break;

        float start = _squadHudCanvasGroup.alpha;
        float end = visible ? 1f : 0f;
        float duration = Mathf.Max(0.01f, flow.SquadHudFadeDuration);
        float t = 0f;

        _squadHudCanvasGroup.interactable = visible;
        _squadHudCanvasGroup.blocksRaycasts = visible;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _squadHudCanvasGroup.alpha = Mathf.Lerp(start, end, t / duration);
            yield return null;
        }

        _squadHudCanvasGroup.alpha = end;

        if (!visible)
            _squadHudRoot.gameObject.SetActive(false);

        _squadHudFadeCoroutine = null;
    }

    private void RefreshSquadHudRoleIcons(Level4FlowController flow)
    {
        if (flow == null)
            return;

        RobotUnlockManager unlockManager = flow.UnlockManager;
        foreach ((RobotType type, Image iconImage) in _squadHudRoleIconImages)
        {
            if (iconImage == null || unlockManager == null)
                continue;

            RobotConfigSO config = unlockManager.GetRobotConfig(type);
            if (config != null && config.robotIcon != null)
                iconImage.sprite = config.robotIcon;
        }
    }
}
