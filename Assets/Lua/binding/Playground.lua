-- -*- coding: utf-8 -*-
local BindingHelper = require("res.BindingHelper")
local xluaUtil = require("xlua.util")
local TestModule = require("TestModule")

local waitInst = CS.UnityEngine.WaitForSeconds(1)
local coro = {}
local counter = 0

local export = {}

    function export.start()
        print("Playground start " .. tostring(component))
        coro = component:StartCoroutine(xluaUtil.cs_generator(export.updatePerSecond))
    end

    function export.update()
    end

    function export.onDestroy()
        component:StopCoroutine(coro)
    end

    function export.updatePerSecond()
        while true do
            export.testPrint()
            coroutine.yield(waitInst)
        end
    end

    function export.testPrint()
        print("testPrint called! ciallo~ "  .. tostring(TestModule.getValue()))
    end

BindingHelper.bindCSComponent(this, export)
