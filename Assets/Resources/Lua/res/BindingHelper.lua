-- -*- coding: utf-8 -*-

local export = {}

local unityComponentMessages = {
    "awake",
    "onEnable",
    "start",
    "fixedUpdate",
    "update",
    "lateUpdate",
    "onDisable",
    "onDestroy",
}

function export.bindCSComponent(this, t)
    this["__inner_binding_scope__"] = t

    for _, msg in ipairs(unityComponentMessages) do
        if t[msg] ~= nil then
            this[msg] = function() t[msg]() end -- 绑定函数到 scriptScope 中
        end
    end
end

return export
