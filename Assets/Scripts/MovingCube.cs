namespace  UnityLearning {

using XLua;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityLearning.Utils;

public class MovingCube : MonoBehaviour
{
    private Action   m_luaStart;
    private Action   m_luaUpdate;
    private Action   m_luaOnDestroy;
    private LuaTable m_scriptScope;

    public const string bindingModuleName = "binding.MovingCube";

    void Awake() {
        m_scriptScope = LuaUtils.CreateBindingScope(this, bindingModuleName, true);
        m_scriptScope.Get("start", out m_luaStart);
        m_scriptScope.Get("update", out m_luaUpdate);
        m_scriptScope.Get("onDestroy", out m_luaOnDestroy);
    }

    void Start() {
        m_luaStart?.Invoke();
    }

    void Update() {
        m_luaUpdate?.Invoke();
    }

    void OnDestroy() {
        m_luaOnDestroy?.Invoke();
        // 释放 lua 引用
        m_luaStart     = null;
        m_luaUpdate    = null;
        m_luaOnDestroy = null;
        m_scriptScope?.Dispose();
    }
}

}
