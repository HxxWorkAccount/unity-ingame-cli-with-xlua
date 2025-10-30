-- -*- coding: utf-8 -*-
local BindingHelper = require("res.BindingHelper")
local xluaUtil = require("xlua.util")

local direction = CS.UnityEngine.Vector3(0.01, 0.01, 0)

local export = {}

    function export.start()
        print("MovingCube start " .. tostring(component))
    end

    function export.update()
        -- component.transform.position = component.transform.position + CS.UnityEngine.Vector3(0, 0.01, 0)
        -- component.transform.position = component.transform.position + direction
    end

    function export.onDestroy()
        print("MovingCube onDestroy " .. tostring(component))
    end

    function export.switchDirection()
        direction = -direction
    end

BindingHelper.bindCSComponent(this, export)
