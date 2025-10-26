// Decompiled with JetBrains decompiler
// Type: Duckov.UI.View
// Assembly: TeamSoda.Duckov.Core, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: FDA9642D-7C8C-43D7-BA39-BA2AFEF5C9C5
// Assembly location: D:\SteamLibrary\steamapps\common\Escape from Duckov\Duckov_Data\Managed\TeamSoda.Duckov.Core.dll

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#nullable disable
namespace Duckov.UI;

public abstract class View : ManagedUIElement
{
  [HideInInspector]
  private static View _activeView;
  [SerializeField]
  private ViewTabs viewTabs;
  [SerializeField]
  private Button exitButton;
  [SerializeField]
  private string sfx_Open;
  [SerializeField]
  private string sfx_Close;
  private bool autoClose = true;

  public static View ActiveView
  {
    get => View._activeView;
    private set
    {
      View activeView1 = View._activeView;
      View._activeView = value;
      View activeView2 = View._activeView;
      if (!((UnityEngine.Object) activeView1 != (UnityEngine.Object) activeView2))
        return;
      Action activeViewChanged = View.OnActiveViewChanged;
      if (activeViewChanged == null)
        return;
      activeViewChanged();
    }
  }

  public static event Action OnActiveViewChanged;

  protected override void Awake()
  {
    base.Awake();
    if ((UnityEngine.Object) this.exitButton != (UnityEngine.Object) null)
      this.exitButton.onClick.AddListener(new UnityAction(((ManagedUIElement) this).Close));
    UIInputManager.OnNavigate += new Action<UIInputEventData>(this.OnNavigate);
    UIInputManager.OnConfirm += new Action<UIInputEventData>(this.OnConfirm);
    UIInputManager.OnCancel += new Action<UIInputEventData>(this.OnCancel);
    this.viewTabs = this.transform.parent.parent.GetComponent<ViewTabs>();
    if (!this.autoClose)
      return;
    this.Close();
  }

  protected override void OnDestroy()
  {
    base.OnDestroy();
    UIInputManager.OnNavigate -= new Action<UIInputEventData>(this.OnNavigate);
    UIInputManager.OnConfirm -= new Action<UIInputEventData>(this.OnConfirm);
    UIInputManager.OnCancel -= new Action<UIInputEventData>(this.OnCancel);
  }

  protected override void OnOpen()
  {
    this.autoClose = false;
    if ((UnityEngine.Object) View.ActiveView != (UnityEngine.Object) null && (UnityEngine.Object) View.ActiveView != (UnityEngine.Object) this)
      View.ActiveView.Close();
    View.ActiveView = this;
    ItemUIUtilities.Select((ItemDisplay) null);
    if ((UnityEngine.Object) this.viewTabs != (UnityEngine.Object) null)
      this.viewTabs.Show();
    if ((UnityEngine.Object) this.gameObject == (UnityEngine.Object) null)
      Debug.LogError((object) "GameObject不存在", (UnityEngine.Object) this.gameObject);
    InputManager.DisableInput(this.gameObject);
    AudioManager.Post(this.sfx_Open);
  }

  protected override void OnClose()
  {
    if ((UnityEngine.Object) View.ActiveView == (UnityEngine.Object) this)
      View.ActiveView = (View) null;
    InputManager.ActiveInput(this.gameObject);
    AudioManager.Post(this.sfx_Close);
  }

  internal virtual void TryQuit() => this.Close();

  public void OnNavigate(UIInputEventData eventData)
  {
    if (eventData.Used || (UnityEngine.Object) View.ActiveView != (UnityEngine.Object) this)
      return;
    this.OnNavigate(eventData.vector);
  }

  public void OnConfirm(UIInputEventData eventData)
  {
    if (eventData.Used || (UnityEngine.Object) View.ActiveView != (UnityEngine.Object) this)
      return;
    this.OnConfirm();
  }

  public void OnCancel(UIInputEventData eventData)
  {
    if (eventData.Used || (UnityEngine.Object) View.ActiveView == (UnityEngine.Object) null || (UnityEngine.Object) View.ActiveView != (UnityEngine.Object) this)
      return;
    this.OnCancel();
    if (eventData.Used)
      return;
    this.TryQuit();
    eventData.Use();
  }

  protected virtual void OnNavigate(Vector2 vector)
  {
  }

  protected virtual void OnConfirm()
  {
  }

  protected virtual void OnCancel()
  {
  }

  protected static T GetViewInstance<T>() where T : View => GameplayUIManager.GetViewInstance<T>();
}
