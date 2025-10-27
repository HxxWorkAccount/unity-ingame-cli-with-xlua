-- -*- coding: utf-8 -*-
local export = {}

local REPLACE_WHEN_MATCH = {
    ["__index"] = true,
    ["__newindex"] = true,
}

local function replaceModule(oldModule, newModule, cacheOldModule)
    --/* 递归替换 oldModule 的"函数"为 newModule 对应函数，其他不变 */--

    if type(oldModule) ~= "table" or type(newModule) ~= "table" then
        print("replaceModule failed: oldModule or newModule is not a table")
        return
    end

    local function doReplace(oldTable, newTable)
        for key, value in next, oldTable do
            if REPLACE_WHEN_MATCH[key] then -- 直接替换
                oldTable[key] = newTable[key]
            elseif newTable[key] == nil then -- 不删除旧函数
                goto continue
            elseif type(value) ~= type(newTable[key]) then -- 排除类型不匹配
                goto continue
            elseif type(value) == "function" then
                oldTable[key] = newTable[key]
            elseif type(value) == "table" and value ~= newTable[key] then
                doReplace(value, newTable[key])
            end
            ::continue::
        end

        -- 添加新增函数
        for key, value in next, newModule do
            if type(value) == "function" and oldTable[key] == nil then
                oldTable[key] = value
            end
        end
    end

    -- 更新旧模块函数
    if cacheOldModule then -- 用弱表缓存旧模块，这样才能多次 hotfix
        local weakOldModules = oldModule["__weak_old_modules__"]
        if weakOldModules == nil then
            weakOldModules = {}
            setmetatable(weakOldModules, { __mode = "k" })
        end
        oldModule["__weak_old_modules__"] = nil
        weakOldModules[oldModule] = true
        for m in pairs(weakOldModules) do
            doReplace(m, newModule)
        end
        newModule["__weak_old_modules__"] = weakOldModules
    else -- 如果不 cache 就只是替换 oldmodule，不做额外处理
        doReplace(oldModule, newModule)
    end
end

function export.hotfixByRequire(moduleName)
    if package.loaded[moduleName] == nil then
        return
    end
    local oldModule = package.loaded[moduleName]
    package.loaded[moduleName] = nil
    require(moduleName)
    replaceModule(oldModule, package.loaded[moduleName], true)
end

function export.hotfixByByte(moduleName)
    if package.loaded[moduleName] == nil then
        return
    end
    local oldModule = package.loaded[moduleName]
    local newModule = require("__hotreload_temp_script__")
    package.loaded["__hotreload_temp_script__"] = nil -- hotfix 完要释放
    assert(newModule ~= nil, "hotfixByByte: newModule is nil")
    package.loaded[moduleName] = newModule
    replaceModule(oldModule, newModule, true)
end

function export.createScopeFromBinding(bindingModuleName)
    local module = package.loaded[bindingModuleName]
    if module == nil then
        print("createScopeFromBinding failed: module not loaded: " .. bindingModuleName)
        return nil
    end
    local scope = {}
    -- 继承环境
    setmetatable(scope, getmetatable(module))
    scope.component = module.component
    scope.global = module.global
    return scope
end

function export.hotfixBinding(moduleName)
    local oldModule = package.loaded[moduleName]
    local newModule = __hotreload_temp_binding__
    __hotreload_temp_binding__ = nil

    if oldModule == nil or newModule == nil then
        return
    end

    replaceModule(oldModule["__inner_binding_scope__"], newModule["__inner_binding_scope__"], false)
end

return export
