﻿
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using DG.Tweening;
using UnityEngine.UI;
using System;

[Serializable]
public class SerializeFieldData
{
    public string VarName;      //变量名
    public GameObject[] Targets;//关联的GameObject
    public string VarType;      //变量类型FullName,带有名字空间
    public int VarPrefix;//变量private/protect/public
    public SerializeFieldData(string varName, GameObject[] targets = null)
    {
        VarName = varName;
        Targets = targets ?? new GameObject[1];
    }
    public T GetComponent<T>(int idx) where T : Component
    {
        return Targets[idx].GetComponent<T>();
    }
    public T[] GetComponents<T>() where T : Component
    {
        T[] result = new T[Targets.Length];
        for (int i = 0; i < Targets.Length; i++)
        {
            result[i] = Targets[i].GetComponent<T>();
        }
        return result;
    }
}
public enum UIFormAnimationType
{
    Default = -1, //使用UIForm表配置的默认动画类型
    None,       //无动画
    FadeIn,     //透明淡入
    FadeOut,    //透明淡出
    ScaleIn,    //缩放淡入
    ScaleOut    //缩放淡出
}
public class UIFormBase : UIFormLogic
{
    [HideInInspector][SerializeField] SerializeFieldData[] _fields = new SerializeFieldData[0];
    [SerializeField] protected RectTransform topBar;
    public UIParams Params { get; private set; }
    public int Id => this.UIForm.SerialId;
    public bool Interactable
    {
        get
        {
            return canvasGroup.interactable;
        }
        set
        {
            canvasGroup.interactable = value;
        }
    }
    private CanvasGroup canvasGroup = null;
    protected Canvas UICanvas { get; private set; }
    private bool isOnEscape;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        InitUIProperties();
        Array.Clear(_fields, 0, _fields.Length);
        UICanvas = gameObject.GetOrAddComponent<Canvas>();
        canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
        RectTransform transform = GetComponent<RectTransform>();
        transform.anchorMin = Vector2.zero;
        transform.anchorMax = Vector2.one;
        transform.anchoredPosition = Vector2.zero;
        transform.sizeDelta = Vector2.zero;
        transform.localPosition = Vector3.zero;
        gameObject.GetOrAddComponent<GraphicRaycaster>();
        InitLocalization();
        FitHoleScreen();
    }
    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        Params = userData as UIParams;
        var cvs = GetComponent<Canvas>();
        cvs.overrideSorting = true;
        cvs.sortingOrder = Params.SortOrder ?? 0;
        Interactable = false;
        isOnEscape = Params.AllowEscapeClose ?? false;
        PlayUIAnimation(Params.AnimationOpen, OnUIShowComplete);
        Params.OpenCallback?.Invoke(this);
    }
    public SerializeFieldData[] GetFieldsProperties()
    {
        return _fields;
    }
    public void ModifyFieldsProperties(SerializeFieldData[] modified)
    {
        this._fields = modified;
    }

    protected virtual void InitUIProperties() { }
    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        if (isOnEscape && Input.GetKeyDown(KeyCode.Escape) && GF.UI.GetTopUIFormId() == this.UIForm.SerialId)
        {
            this.OnClickClose();
        }
    }
    protected override void OnClose(bool isShutdown, object userData)
    {
        DOTween.Kill(this);
        if (!isShutdown)
        {
            Params.CloseCallback?.Invoke(this);
            ReferencePool.Release(Params);
        }
        base.OnClose(isShutdown, userData);
    }
    protected void InitLocalization()
    {
        UIStringKey[] texts = GetComponentsInChildren<UIStringKey>(true);
        foreach (var t in texts)
        {
            if (t.TryGetComponent<TMPro.TextMeshProUGUI>(out var textMeshCom))
            {
                textMeshCom.text = GF.Localization.GetString(t.Key);
            }
            else if (t.TryGetComponent<Text>(out var textCom))
            {
                textCom.text = GF.Localization.GetString(t.Key);
            }
        }
    }
    private void FitHoleScreen()
    {
        if (topBar == null)
        {
            return;
        }
        float topSpace = Screen.height - Screen.safeArea.height;
        if (topSpace < 1f)
        {
            return;
        }
#if UNITY_IOS
        topSpace = 80;
#endif
        var pos = topBar.anchoredPosition;
        pos.y = -topSpace;
        topBar.anchoredPosition = pos;
    }
    private void PlayUIAnimation(UIFormAnimationType animType, GameFrameworkAction onAnimComplete)
    {
        if (null == canvasGroup)
        {
            onAnimComplete.Invoke();
            return;
        }
        switch (animType)
        {
            case UIFormAnimationType.None:
                onAnimComplete.Invoke();
                break;
            case UIFormAnimationType.FadeIn:
                DoFadeAnim(0, 1, 0.4f, onAnimComplete);
                break;
            case UIFormAnimationType.FadeOut:
                DoFadeAnim(1, 0, 0.2f, onAnimComplete);
                break;
                //case UIFormAnimationType.ScaleIn:
                //    break;
                //case UIFormAnimationType.ScaleOut:
                //    break;
        }
    }
    public void CloseUIWithAnim()
    {
        if (null == canvasGroup)
        {
            return;
        }
        Interactable = false;
        PlayUIAnimation(Params.AnimationClose, OnUIHideComplete);
    }


    public virtual void OnClickClose()
    {
        GF.Sound.PlayEffect("ui/ui_click.wav");
        GF.UI.CloseUIFormWithAnim(this.UIForm);
    }

    public void ClickUIButton(string bt_tag)
    {
        GF.Sound.PlayEffect("ui/ui_click.wav");
        OnButtonClick(this, bt_tag);
    }
    public void ClickUIButton(Button btSelf)
    {
        GF.Sound.PlayEffect("ui/ui_click.wav");
        OnButtonClick(this, btSelf);
    }
    protected virtual void OnButtonClick(object sender, string btId)
    {
        Params.ButtonClickCallback?.Invoke(sender, btId);
    }
    protected virtual void OnButtonClick(object sender, UnityEngine.UI.Button btSelf)
    {
    }
    protected virtual void OnUIShowComplete()
    {
        Interactable = true;

    }
    protected virtual void OnUIHideComplete()
    {
        GF.UI.CloseUIForm(this.UIForm);
    }

    #region 默认UI动画
    private void DoFadeAnim(float s, float e, float time, GameFrameworkAction onComplete = null)
    {
        canvasGroup.alpha = s;
        var fade = canvasGroup.DOFade(e, time);
        fade.SetEase(Ease.InOutFlash);
        fade.SetTarget(this);
        fade.SetUpdate(true);
        fade.onComplete = () =>
        {
            if (GF.UI.IsValidUIForm(this.UIForm))
            {
                onComplete?.Invoke();
            }
        };
    }
    #endregion
}
