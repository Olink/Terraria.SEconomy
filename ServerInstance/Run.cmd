@echo off
xcopy /y /i ..\BuildReferences\TShock\*.dll ServerPlugins
xcopy /y /i ..\BuildReferences\TShock\*.exe ServerPlugins
xcopy /y /i ..\Wolfje.TPlugins\Wolfje.TPlugin.Bank\bin\Debug\Wolfje.Plugins.Seconomy.dll ServerPlugins
xcopy /y /i ..\BuildReferences\TerrariaServer\*.exe .
TerrariaServer.exe