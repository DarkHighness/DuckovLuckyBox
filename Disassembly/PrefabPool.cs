// Decompiled with JetBrains decompiler
// Type: Duckov.Utilities.PrefabPool`1
// Assembly: TeamSoda.Duckov.Utilities, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 2FB2E036-87CC-4F61-8242-0D823A41ECA0
// Assembly location: D:\SteamLibrary\steamapps\common\Escape from Duckov\Duckov_Data\Managed\TeamSoda.Duckov.Utilities.dll

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.Pool;

#nullable disable
namespace Duckov.Utilities;

public class PrefabPool<T> where T : Component
{
  public readonly T Prefab;
  public Transform poolParent;
  private Action<T> onGet;
  private Action<T> onRelease;
  private Action<T> onDestroy;
  private Action<T> onCreate;
  public readonly bool CollectionCheck;
  public readonly int DefaultCapacity;
  public readonly int MaxSize;
  private readonly ObjectPool<T> pool;
  private List<T> activeObjects;

  public ReadOnlyCollection<T> ActiveEntries => this.activeObjects.AsReadOnly();

  public PrefabPool(
    T prefab,
    Transform poolParent = null,
    Action<T> onGet = null,
    Action<T> onRelease = null,
    Action<T> onDestroy = null,
    bool collectionCheck = true,
    int defaultCapacity = 10,
    int maxSize = 10000,
    Action<T> onCreate = null)
  {
    this.Prefab = prefab;
    prefab.gameObject.SetActive(false);
    if ((UnityEngine.Object) poolParent == (UnityEngine.Object) null)
      poolParent = prefab.transform.parent;
    this.poolParent = poolParent;
    this.onGet = onGet;
    this.onRelease = onRelease;
    this.onDestroy = onDestroy;
    this.CollectionCheck = collectionCheck;
    this.DefaultCapacity = defaultCapacity;
    this.MaxSize = maxSize;
    this.onCreate = onCreate;
    this.pool = new ObjectPool<T>(new Func<T>(this.CreateInstance), new Action<T>(this.OnGet), new Action<T>(this.OnRelease), new Action<T>(this.OnDestroy), collectionCheck, defaultCapacity, maxSize);
    this.activeObjects = new List<T>();
  }

  public T Get(Transform setParent = null)
  {
    if ((UnityEngine.Object) setParent == (UnityEngine.Object) null)
      setParent = this.poolParent;
    T obj = this.pool.Get();
    if ((bool) (UnityEngine.Object) setParent)
    {
      obj.transform.SetParent(setParent, false);
      obj.transform.SetAsLastSibling();
    }
    return obj;
  }

  public void Release(T item)
  {
    this.pool.Release(item);
    if (!(item is IPoolable poolable))
      return;
    poolable.NotifyReleased();
  }

  private T CreateInstance()
  {
    T instance = UnityEngine.Object.Instantiate<T>(this.Prefab);
    Action<T> onCreate = this.onCreate;
    if (onCreate != null)
      onCreate(instance);
    return instance;
  }

  private void OnGet(T item)
  {
    this.activeObjects.Add(item);
    item.gameObject.SetActive(true);
    if (item is IPoolable poolable)
      poolable.NotifyPooled();
    Action<T> onGet = this.onGet;
    if (onGet == null)
      return;
    onGet(item);
  }

  private void OnRelease(T item)
  {
    this.activeObjects.Remove(item);
    Action<T> onRelease = this.onRelease;
    if (onRelease != null)
      onRelease(item);
    if (!((UnityEngine.Object) item != (UnityEngine.Object) null))
      return;
    item.gameObject.SetActive(false);
    item.transform.SetParent(this.poolParent);
  }

  private void OnDestroy(T item)
  {
    Action<T> onDestroy = this.onDestroy;
    if (onDestroy != null)
      onDestroy(item);
    UnityEngine.Object.Destroy((UnityEngine.Object) item.gameObject);
  }

  public void ReleaseAll()
  {
    this.activeObjects.RemoveAll((Predicate<T>) (e => (UnityEngine.Object) e == (UnityEngine.Object) null));
    foreach (T obj in this.activeObjects.ToArray())
      this.Release(obj);
  }

  public T Find(Predicate<T> predicate)
  {
    foreach (T activeObject in this.activeObjects)
    {
      if (predicate(activeObject))
        return activeObject;
    }
    return default (T);
  }

  public int ReleaseAll(Predicate<T> predicate)
  {
    List<T> objList = new List<T>();
    foreach (T activeObject in this.activeObjects)
    {
      if (predicate(activeObject))
        objList.Add(activeObject);
    }
    foreach (T obj in objList)
      this.Release(obj);
    return objList.Count;
  }
}
