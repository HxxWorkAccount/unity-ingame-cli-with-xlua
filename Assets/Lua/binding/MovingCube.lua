-- -*- coding: utf-8 -*-
local BindingHelper = require("res.BindingHelper")
local xluaUtil = require("xlua.util")

local export = {}

    function export.start()
        print("MovingCube start " .. tostring(component))
    end

    function export.update()
        print("MovingCube update")
        component.transform.position = component.transform.position + CS.UnityEngine.Vector3(0, 0.01, 0)
    end

    function export.onDestroy()
        print("MovingCube onDestroy " .. tostring(component))
    end

BindingHelper.bindCSComponent(this, export)
