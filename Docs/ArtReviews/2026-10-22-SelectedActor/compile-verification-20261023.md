# 最后提示文案编译证据

核验日期：2026-10-23。以下摘自现有Unity Editor.log，工作目录为Prototypes/VC5PvE。此项是编译核验，不是重跑游戏测试。

BattleHud.cs修改时间：2026-10-22 22:22:11；VC5PvE.Runtime.dll生成时间：2026-10-22 23:11:38。编译包含3条未使用变量/字段警告，无编译错误。

```text
[ScriptCompilation] Requested script compilation because: Assetdatabase observed changes in script compilation related files
AssetDatabase: script compilation time: 0.084713s
[Licensing::Client] Successfully resolved entitlements
Starting: D:\unity\安装位置\2021.3.33f1c1\Editor\Data\bee_backend.exe --profile="Library/Bee/backend_profiler0.traceevents" --stdin-canary --dagfile="Library/Bee/1900b0aE.dag" --continue-on-failure ScriptAssemblies
WorkingDir: E:/unity/git_newest/ValkyriaChronicles5/Prototypes/VC5PvE
ExitCode: 0 Duration: 2s117ms
[366/378    1s] Csc Library/Bee/artifacts/1900b0aE.dag/VC5PvE.Runtime.dll (+2 others)
Assets\VC5PvE\UI\CampaignCampView.cs(112,23): warning CS0219: The variable 'tileW' is assigned but its value is never used

Assets\VC5PvE\UI\CampaignCampView.cs(112,33): warning CS0219: The variable 'tileH' is assigned but its value is never used

Assets\VC5PvE\UI\BattleHud.cs(27,22): warning CS0414: The field 'BattleHud.livePreview' is assigned but its value is never used
[369/378    0s] ILPostProcess Library/Bee/artifacts/1900b0aE.dag/post-processed/VC5PvE.Editor.dll (+pdb)
- Starting ILPostProcessorRunner

- Starting ILPostProcessor 'zzzUnity.Burst.CodeGen.BurstILPostProcessor' on VC5PvE.Editor.dll

- Finished ILPostProcessor 'zzzUnity.Burst.CodeGen.BurstILPostProcessor' on VC5PvE.Editor.dll in 0.000626 seconds

- Finished ILPostProcessorRunner in 0.051522 seconds
[370/378    0s] ILPostProcess Library/Bee/artifacts/1900b0aE.dag/post-processed/VC5PvE.PlayTests.dll (+pdb)
- Starting ILPostProcessorRunner

- Starting ILPostProcessor 'zzzUnity.Burst.CodeGen.BurstILPostProcessor' on VC5PvE.PlayTests.dll

- Finished ILPostProcessor 'zzzUnity.Burst.CodeGen.BurstILPostProcessor' on VC5PvE.PlayTests.dll in 0.000555 seconds

- Finished ILPostProcessorRunner in 0.052195 seconds
[371/378    0s] ILPostProcess Library/Bee/artifacts/1900b0aE.dag/post-processed/VC5PvE.Runtime.dll (+pdb)
- Starting ILPostProcessorRunner

- Starting ILPostProcessor 'zzzUnity.Burst.CodeGen.BurstILPostProcessor' on VC5PvE.Runtime.dll

- Finished ILPostProcessor 'zzzUnity.Burst.CodeGen.BurstILPostProcessor' on VC5PvE.Runtime.dll in 0.00072 seconds

- Finished ILPostProcessorRunner in 0.054542 seconds
[374/378    0s] CopyFiles Library/ScriptAssemblies/VC5PvE.Runtime.pdb
[377/378    0s] CopyFiles Library/ScriptAssemblies/VC5PvE.Runtime.dll
*** Tundra build success (2.08 seconds), 6 items updated, 378 evaluated
Assets\VC5PvE\UI\CampaignCampView.cs(112,23): warning CS0219: The variable 'tileW' is assigned but its value is never used
Assets\VC5PvE\UI\CampaignCampView.cs(112,33): warning CS0219: The variable 'tileH' is assigned but its value is never used
Assets\VC5PvE\UI\BattleHud.cs(27,22): warning CS0414: The field 'BattleHud.livePreview' is assigned but its value is never used
Reloading assemblies after forced synchronous recompile.
Begin MonoManager ReloadAssembly
Symbol file LoadedFromMemory is not a mono symbol file
Native extension for WindowsStandalone target not found
Native extension for Android target not found
Refreshing native plugins compatible for Editor in 5.02 ms, found 0 plugins.
Preloading 0 native plugins for Editor in 0.00 ms.
[MODES] ModeService[none].Initialize
[MODES] ModeService[none].LoadModes
[MODES] Loading mode Default (0) for mode-current-id-森钟战术
Mono: successfully reloaded assembly
- Completed reload, in  3.496 seconds
Domain Reload Profiling:
	ReloadAssembly (3497ms)
		BeginReloadAssembly (711ms)
			ExecutionOrderSort (0ms)
			DisableScriptedObjects (66ms)
			BackupInstance (0ms)
			ReleaseScriptingObjects (0ms)
			CreateAndSetChildDomain (274ms)
		EndReloadAssembly (2602ms)
			LoadAssemblies (230ms)
			RebuildTransferFunctionScriptingTraits (0ms)
			SetupTypeCache (237ms)
```
