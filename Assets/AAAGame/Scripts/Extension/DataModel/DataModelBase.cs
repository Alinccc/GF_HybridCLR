using GameFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataModelBase : IReference
{
    public int Id { get; private set; } = 0;

    protected virtual void OnInit() { }

    /// <summary>
    /// ���������ʱ�Զ�����OnClear,���������ñ�������,���⸴�ö���ʱ����Ĭ����ֵ(������)
    /// </summary>
    protected virtual void OnClear() { }
    public void Init(int id)
    {
        this.Id = id;

        OnInit();
    }
    public void Clear()
    {
        this.Id = 0;
    }

    internal void Shutdown()
    {
        ReferencePool.Release(this);
    }
}
