//	This script mines ore from asteroids.
//	before running this script, make sure to prepare as follows:
//	+enter bookmark for mining site and bookmark for station in the configuration section below.
//	+in the Overview create a preset which includes asteroids and rats and enter the name of that preset in the configuration section below at 'OverviewPreset'. The bot will make sure this preset is loaded when it needs to use the overview.
//	+set Overview to sort by distance with the nearest entry at the top.
//	+in the Inventory select the 'List' view.
//	+set the UI language to english.
//	+use a ship with an ore hold.
//	+put some light drones into your ships' drone bay. The bot will make use of them to attack rats when HP are too low (configurable) or it gets jammed.
//	+enable the info panel 'System info'. The bot will use the button in there to access bookmarks and asteroid belts.
//	+arrange windows to not occlude modules or info panels.
//	+in the ship UI, disable "Display Passive Modules" and disable "Display Empty Slots" and enable "Display Module Tooltips". The bot uses the module tooltips to automatically identify the properties of the modules.

using Parse = Sanderling.Parse;
using System.IO;
//	begin of configuration section ->

//	The bot uses the bookmarks from the menu which is opened from the button in the 'System info' panel.
//	Bookmark to station where ore should be transfered to item hangar.
DateTime StartTime = DateTime.Now;
TimeSpan SessionTime;
DateTime t1 = DateTime.Now;
DateTime t2 = Convert.ToDateTime("13:50");
int CurrentISKInt = 0;
int TransferredISKInt = 0;
Boolean stopTime = true; //Stop or not at t2-time
string StationBookmark = "Station";
//	Bookmark to place with asteroids. 
string MiningSiteBookmark = "Belt1"; 
string[] MiningSiteBookmarks = { "Belt1", "Belt2", "Belt3", "Belt4", "Belt5", "Belt6", "Belt7" };

//	The bot loads this preset to the active tab. 
string OverviewPreset = "miningx";

var ActivateHardener = true;

//	bot will start fighting (and stop mining) when hitpoints are lower. 
var DefenseEnterHitpointThresholdPercent = 85;
var DefenseExitHitpointThresholdPercent = 90;

var EmergencyWarpOutHitpointPercent = 40;

var FightAllRats = false;	//	when this is set to true, the bot will attack rats independent of shield hp.

//	<- end of configuration section

Func<object> BotStopActivity = () => new object();

Func<object> NextActivity = MainStep;

for(;;)
{
	MemoryUpdate();
      SessionTime =  DateTime.Now - StartTime;
	Host.Log("Session time: " + SessionTime.Hours.ToString() + ":" + SessionTime.Minutes.ToString().PadLeft(2,'0') +
		", ore hold: " + OreHoldFillPercent + "%" +
		", range: " + MiningRange +
		", modules (off): " + SetModuleMiner?.Length + "(" + SetModuleMinerInactive?.Length + ")" +
		", shield: " + ShieldHpPercent + "%" +
//		", EWO: " + EmergencyWarpOutEnabled.ToString() + 
//		", JLA: " + JammedLastAge +
//  CurrentISKInt=Convert.ToInt32(Math.Round(/(DateTime.Now-StartTime).TotalSeconds*60*60,0));
		", ISK: " + Math.Round((TransferredISKInt+CurrentISKInt)/(double)1000,0) + 
		"k (" + Math.Round((TransferredISKInt+CurrentISKInt)/(DateTime.Now-StartTime).TotalSeconds*60*60/1000,0) + 
		"k/h), rats: " + ListRatOverviewEntry?.Length +
		", ateroids: " + ListAsteroidOverviewEntry?.Length +
		", offload count: " + OffloadCount);
		Log("Session time: " + SessionTime.Hours.ToString() + ":" + SessionTime.Minutes.ToString().PadLeft(2,'0') +
		", ore hold: " + OreHoldFillPercent + "%" +
		", range: " + MiningRange +
		", modules (off): " + SetModuleMiner?.Length + "(" + SetModuleMinerInactive?.Length + ")" +
		", shield: " + ShieldHpPercent + "%" +
//		", EWO: " + EmergencyWarpOutEnabled.ToString() + 
//		", JLA: " + JammedLastAge +
//  CurrentISKInt=Convert.ToInt32(Math.Round(/(DateTime.Now-StartTime).TotalSeconds*60*60,0));
		", ISK: " + Math.Round((TransferredISKInt+CurrentISKInt)/(double)1000,0) + 
		"k (" + Math.Round((TransferredISKInt+CurrentISKInt)/(DateTime.Now-StartTime).TotalSeconds*60*60/1000,0) + 
		"k/h), rats: " + ListRatOverviewEntry?.Length +
		", ateroids: " + ListAsteroidOverviewEntry?.Length +
		", offload count: " + OffloadCount);
//		", nextAct: " + NextActivity?.Method?.Name);
        t1 = DateTime.Now;
        if ((t1.Hour == t2.Hour) && (t1.Minute > t2.Minute) && stopTime)
        {
//          Host.Log(t1.ToString("HH:mm"));
         Host.Log("Shutting down by time");
         InitiateDockToOrWarpToBookmark(StationBookmark);
	  Host.Delay(60000);
          Sanderling.KeyboardPressCombined(new[]{ VirtualKeyCode.LMENU, VirtualKeyCode.SHIFT, VirtualKeyCode.VK_Q});
System.Diagnostics.Process.Start("Shutdown", "-s -t 10");
          Host.Break();
        }

	CloseModalUIElement();
	
	if(EmergencyWarpOutEnabled)
		if(!(Measurement?.IsDocked ?? false))
		{
			InitiateDockToOrWarpToBookmark(StationBookmark);
			continue;
		}
	
	NextActivity = NextActivity?.Invoke() as Func<object>;

	if(BotStopActivity == NextActivity)
		break;
	
	if(null == NextActivity)
		NextActivity = MainStep;
	
	Host.Delay(1111);
}

//	seconds since ship was jammed.
long? JammedLastAge => Jammed ? 0 : (Host.GetTimeContinuousMilli() - JammedLastTime) / 1000;

int?	ShieldHpPercent => ShipUi?.HitpointsAndEnergy?.Shield / 10;

bool	DefenseExit =>
	(Measurement?.IsDocked ?? false) ||
	!(0 < ListRatOverviewEntry?.Length)	||
	(DefenseExitHitpointThresholdPercent < ShieldHpPercent && !(JammedLastAge < 40) &&
	!(FightAllRats && 0 < ListRatOverviewEntry?.Length));

bool	DefenseEnter =>
	!DefenseExit	||
	!(DefenseEnterHitpointThresholdPercent < ShieldHpPercent) || JammedLastAge < 10;

bool	OreHoldFilledForOffload => 97 < OreHoldFillPercent;

Int64?	JammedLastTime = null;
bool	EmergencyWarpOutEnabled	= false;
int? LastCheckOreHoldFillPercent = null;

int OffloadCount = 0;

Func<object>	MainStep()
{
	if(Measurement?.IsDocked ?? false)
	{
		UnloadToHangar();

		if (EmergencyWarpOutEnabled)
			return BotStopActivity;

		Undock();
	}
	
	if(DefenseEnter)
	{
		Host.Log("enter defense.");
		return DefenseStep;
	}

	EnsureOverviewTypeSelectionLoaded();

	EnsureWindowInventoryOpenOreHold();

	if(ReadyForManeuver)
	{
		DroneEnsureInBay();

		if(OreHoldFilledForOffload)
		{
			if(ReadyForManeuver)
				InitiateDockToOrWarpToBookmark(StationBookmark);
			return MainStep;
		}
		
		if(!(0 < ListAsteroidOverviewEntry?.Length))
		{
                	Random rnd = new Random();
            		int randomNumber = rnd.Next(0, MiningSiteBookmarks.Length);
//			InitiateDockToOrWarpToBookmark(MiningSiteBookmark);
			InitiateDockToOrWarpToBookmark(MiningSiteBookmarks[randomNumber]);
		}
	}

	ModuleMeasureAllTooltip();

	if(ActivateHardener)
		ActivateHardenerExecute();

	return InBeltMineStep;
}

void CloseModalUIElement()
{
	var	ButtonClose =
		ModalUIElement?.ButtonText?.FirstOrDefault(button => (button?.Text).RegexMatchSuccessIgnoreCase("close|no|ok"));
		
	Sanderling.MouseClickLeft(ButtonClose);
}

void DroneLaunch()
{
	Host.Log("launch drones.");
	Sanderling.MouseClickRight(DronesInBayListEntry);
	Sanderling.MouseClickLeft(Menu?.FirstOrDefault()?.EntryFirstMatchingRegexPattern("launch", RegexOptions.IgnoreCase));
}

void DroneEnsureInBay()
{
	if(0 == DronesInSpaceCount)
		return;

	DroneReturnToBay();
	
	Host.Delay(4444);
}

void DroneReturnToBay()
{
	Host.Log("return drones to bay.");
	Sanderling.MouseClickRight(DronesInSpaceListEntry);
	Sanderling.MouseClickLeft(Menu?.FirstOrDefault()?.EntryFirstMatchingRegexPattern("return.*bay", RegexOptions.IgnoreCase));
}

Func<object>	DefenseStep()
{
	if(DefenseExit)
	{
		Host.Log("exit defense.");
		return null;
	}

	if (!(0 < DronesInSpaceCount))
		DroneLaunch();

	EnsureOverviewTypeSelectionLoaded();

	var SetRatName =
		ListRatOverviewEntry?.Select(entry => Regex.Split(entry?.Name ?? "", @"\s+")?.FirstOrDefault())
		?.Distinct()
		?.ToArray();
	
	var SetRatTarget = Measurement?.Target?.Where(target =>
		SetRatName?.Any(ratName => target?.TextRow?.Any(row => row.RegexMatchSuccessIgnoreCase(ratName)) ?? false) ?? false);
	
	var RatTargetNext = SetRatTarget?.OrderBy(target => target?.DistanceMax ?? int.MaxValue)?.FirstOrDefault();
	
	if(null == RatTargetNext)
	{
		Host.Log("no rat targeted.");
		Sanderling.MouseClickRight(ListRatOverviewEntry?.FirstOrDefault());
		Sanderling.MouseClickLeft(MenuEntryLockTarget);
	}
	else
	{
		Host.Log("rat targeted. sending drones.");
		Sanderling.MouseClickLeft(RatTargetNext);
		Sanderling.MouseClickRight(DronesInSpaceListEntry);
		Sanderling.MouseClickLeft(Menu?.FirstOrDefault()?.EntryFirstMatchingRegexPattern("engage", RegexOptions.IgnoreCase));
	}
	
	return DefenseStep;
}

Func<object> InBeltMineStep()
{
if (null != WindowInventory)
{
  var EstISK = WindowInventory?.LabelText?.FirstOrDefault(c => c?.Text?.RegexMatchSuccess(@"Est", RegexOptions.IgnoreCase) ?? false);
  if (null != EstISK)
  {
var EstISKtxt = EstISK.Text.Replace("�", String.Empty);
//Host.Log(EstISK);
//Host.Log(EstISK.Substring(0,EstISK.IndexOf("ISK")));
//EstISKInt = Convert.ToInt32(EstISK.Substring(0,EstISK.IndexOf("ISK")));
//Host.Log(EstISKInt);
//Host.Delay(4000);
//Host.Log(Math.Round(EstISKInt/(DateTime.Now-StartTime).TotalSeconds*60*60,0));
 CurrentISKInt = Convert.ToInt32(EstISKtxt.Substring(0,EstISKtxt.IndexOf("ISK")));
 }
}
//Host.Break();
	if (DefenseEnter)
	{
		Host.Log("enter defense.");
		return DefenseStep;
	}

        if (!(0 < MiningRange))
        {
        	ModuleMeasureAllTooltip();
        }

	EnsureWindowInventoryOpenOreHold();

	EnsureOverviewTypeSelectionLoaded();

	if(OreHoldFilledForOffload)
		return null;

	if(!(0 < SetModuleMinerInactive?.Length))
	{
		Host.Delay(7777);
		return InBeltMineStep;
	}
	
	var	SetTargetAsteroidInRange	=
		SetTargetAsteroid?.Where(target => target?.DistanceMax <= MiningRange)?.ToArray();
	Host.Log("targeted asteroids in range: " + SetTargetAsteroidInRange?.Length);
	if(0 < SetTargetAsteroidInRange?.Length)
	{
		var TargetAsteroidInputFocus	=
			SetTargetAsteroidInRange?.FirstOrDefault(target => target?.IsSelected ?? false);

		if(null == TargetAsteroidInputFocus)
			Sanderling.MouseClickLeft(SetTargetAsteroid?.FirstOrDefault());

		foreach (var Module in SetModuleMinerInactive)
			ModuleToggle(Module);
		return InBeltMineStep;
	}

	var	AsteroidOverviewEntry	= ListAsteroidOverviewEntry?.FirstOrDefault();

	Host.Log("next asteroid: " + AsteroidOverviewEntry?.Name + " , Distance: " + AsteroidOverviewEntry?.DistanceMax);

	if(null == AsteroidOverviewEntry)
	{
		Host.Log("no asteroid available");
		return null;
	}

	if(!(AsteroidOverviewEntry.DistanceMax < MiningRange))
	{
		Host.Log("out of range, approaching");
		ClickMenuEntryOnMenuRoot(AsteroidOverviewEntry, "approach");
	}
	else
	{
		Host.Log("locking");
		ClickMenuEntryOnMenuRoot(AsteroidOverviewEntry, "^lock");
	}
	
	return InBeltMineStep;
}


Sanderling.Parse.IMemoryMeasurement	Measurement	=>
	Sanderling?.MemoryMeasurementParsed?.Value;

IWindow ModalUIElement =>
	Measurement?.EnumerateReferencedUIElementTransitive()?.OfType<IWindow>()?.Where(window => window?.isModal ?? false)
	?.OrderByDescending(window => window?.InTreeIndex ?? int.MinValue)
	?.FirstOrDefault();	
	
Sanderling.Interface.MemoryStruct.IMenu[] Menu => Measurement?.Menu;

IShipUi ShipUi => Measurement?.ShipUi;

bool Jammed => ShipUi?.EWarElement?.Any(EwarElement => (EwarElement?.EWarType).RegexMatchSuccess("electronic")) ?? false;

Sanderling.Interface.MemoryStruct.IMenuEntry MenuEntryLockTarget =>
	Menu?.FirstOrDefault()?.Entry?.FirstOrDefault(entry => entry.Text.RegexMatchSuccessIgnoreCase("^lock"));

Sanderling.Parse.IWindowOverview	WindowOverview	=>
	Measurement?.WindowOverview?.FirstOrDefault();

Sanderling.Parse.IWindowInventory	WindowInventory	=>
	Measurement?.WindowInventory?.FirstOrDefault();

IWindowDroneView	WindowDrones	=>
	Measurement?.WindowDroneView?.FirstOrDefault();

ITreeViewEntry InventoryActiveShipOreHold =>
	WindowInventory?.ActiveShipEntry?.TreeEntryFromCargoSpaceType(ShipCargoSpaceTypeEnum.OreHold);

IInventoryCapacityGauge OreHoldCapacityMilli =>
	(InventoryActiveShipOreHold?.IsSelected ?? false) ? WindowInventory?.SelectedRightInventoryCapacityMilli : null;

int? OreHoldFillPercent => (int?)((OreHoldCapacityMilli?.Used * 100) / OreHoldCapacityMilli?.Max);

Tab OverviewPresetTabActive =>
	WindowOverview?.PresetTab
	?.OrderByDescending(tab => tab?.LabelColorOpacityMilli ?? 0)
	?.FirstOrDefault();

string OverviewTypeSelectionName =>
	WindowOverview?.Caption?.RegexMatchIfSuccess(@"\(([^\)]*)\)")?.Groups?[1]?.Value;

Parse.IOverviewEntry[] ListRatOverviewEntry => WindowOverview?.ListView?.Entry?.Where(entry =>
		(entry?.MainIconIsRed ?? false)	&& (entry?.IsAttackingMe ?? false))
		?.OrderBy(entry => entry?.DistanceMax ?? int.MaxValue)
		?.ToArray();

Parse.IOverviewEntry[] ListAsteroidOverviewEntry =>
	WindowOverview?.ListView?.Entry
	?.Where(entry => null != OreTypeFromAsteroidName(entry?.Name))
	?.OrderBy(entry => entry.DistanceMax ?? int.MaxValue)
	?.ToArray();
	

DroneViewEntryGroup DronesInBayListEntry =>
	WindowDrones?.ListView?.Entry?.OfType<DroneViewEntryGroup>()?.FirstOrDefault(Entry => null != Entry?.Caption?.Text?.RegexMatchIfSuccess(@"Drones in bay", RegexOptions.IgnoreCase));

DroneViewEntryGroup DronesInSpaceListEntry =>
	WindowDrones?.ListView?.Entry?.OfType<DroneViewEntryGroup>()?.FirstOrDefault(Entry => null != Entry?.Caption?.Text?.RegexMatchIfSuccess(@"Drones in Local Space", RegexOptions.IgnoreCase));

int?	DronesInSpaceCount => DronesInSpaceListEntry?.Caption?.Text?.AsDroneLabel()?.Status?.TryParseInt();

bool ReadyForManeuverNot =>
	Measurement?.ShipUi?.Indication?.Any(indication =>
		(indication?.Text).RegexMatchSuccessIgnoreCase("warp|docking")) ?? false;

bool ReadyForManeuver => !ReadyForManeuverNot && !(Measurement?.IsDocked ?? true);

Sanderling.Parse.IShipUiTarget[] SetTargetAsteroid =>
	Measurement?.Target?.Where(target =>
		target?.TextRow?.Any(textRow => textRow.RegexMatchSuccessIgnoreCase("asteroid")) ?? false)?.ToArray();

Sanderling.Interface.MemoryStruct.IListEntry	WindowInventoryItem	=>
	WindowInventory?.SelectedRightInventory?.ListView?.Entry?.FirstOrDefault();

Sanderling.Accumulation.IShipUiModule[] SetModuleMiner =>
	Sanderling.MemoryMeasurementAccu?.Value?.ShipUiModule?.Where(module => module?.TooltipLast?.Value?.IsMiner ?? false)?.ToArray();

Sanderling.Accumulation.IShipUiModule[] SetModuleMinerInactive	 =>
	SetModuleMiner?.Where(module => !(module?.RampActive ?? false))?.ToArray();

int?	MiningRange => SetModuleMiner?.Select(module =>
	module?.TooltipLast?.Value?.RangeOptimal ?? module?.TooltipLast?.Value?.RangeMax ?? module?.TooltipLast?.Value?.RangeWithin ?? 0)?.DefaultIfEmpty(0)?.Min();;

//	extract the ore type from the name as seen in overview. "Asteroid (Plagioclase)"
string OreTypeFromAsteroidName(string AsteroidName)	=>
	AsteroidName.ValueFromRegexMatchGroupAtIndex(@"Asteroid \(([^\)]+)", 0);

void ClickMenuEntryOnMenuRoot(IUIElement MenuRoot, string MenuEntryRegexPattern)
{
	Sanderling.MouseClickRight(MenuRoot);
	
	var Menu = Measurement?.Menu?.FirstOrDefault();
	
	var	MenuEntry = Menu?.EntryFirstMatchingRegexPattern(MenuEntryRegexPattern, RegexOptions.IgnoreCase);
	
	Sanderling.MouseClickLeft(MenuEntry);
}

void EnsureWindowInventoryOpen()
{
	if (null != WindowInventory)
		return;

	Host.Log("open Inventory.");
	Sanderling.MouseClickLeft(Measurement?.Neocom?.InventoryButton);
}

void EnsureWindowInventoryOpenOreHold()
{
	EnsureWindowInventoryOpen();
	
	if(!(InventoryActiveShipOreHold?.IsSelected ?? false))
		Sanderling.MouseClickLeft(InventoryActiveShipOreHold);
}

void UnloadToHangar()
{
	Host.Log("unload to hangar.");

	EnsureWindowInventoryOpenOreHold();

	for (;;)
	{
		var	OreHoldItem = WindowInventory?.SelectedRightInventory?.ListView?.Entry?.FirstOrDefault();
		var	ItemHangar = WindowInventory?.ItemHangarEntry?.LabelText?.Largest();

		if (null == ItemHangar)
			Host.Log("error: item hangar not found");

		if(null == OreHoldItem)
			//	0 items in OreHold
			break;
		
		Sanderling.MouseDragAndDrop(OreHoldItem, ItemHangar);
	}
	TransferredISKInt += CurrentISKInt;
      CurrentISKInt = 0;
}

bool InitiateDockToOrWarpToBookmark(string Bookmark)
{
	Host.Log("dock to or warp to bookmark: '" + Bookmark + "'");
	
	var ListSurroundingsButton = Measurement?.InfoPanelCurrentSystem?.ListSurroundingsButton;
	
	Sanderling.MouseClickRight(ListSurroundingsButton);
	
	var BookmarkMenuEntry = Measurement?.Menu?.FirstOrDefault()?.EntryFirstMatchingRegexPattern("^" + Bookmark + "$", RegexOptions.IgnoreCase);

	if(null == BookmarkMenuEntry)
	{
		Host.Log("menu entry not found for bookmark: '" + Bookmark + "'");
		return true;
	}

	Sanderling.MouseClickLeft(BookmarkMenuEntry);

	var Menu = Measurement?.Menu?.ElementAtOrDefault(1);
	var DockMenuEntry = Menu?.EntryFirstMatchingRegexPattern("dock",RegexOptions.IgnoreCase);
	var WarpMenuEntry = Menu?.EntryFirstMatchingRegexPattern(@"warp.*within\s*0",RegexOptions.IgnoreCase);
	var ApproachEntry = Menu?.EntryFirstMatchingRegexPattern(@"approach",RegexOptions.IgnoreCase);

	var MenuEntry = DockMenuEntry ?? WarpMenuEntry;
	
	if(null == MenuEntry)
	{
		if(null != ApproachEntry)
		{
			Host.Log("found menu entry '" + ApproachEntry.Text + "'. Assuming we are already there.");
			return false;
		}

		Host.Log("no suitable menu entry found");
		return true;
	}
	
	Host.Log("initiating " + MenuEntry.Text);
	Sanderling.MouseClickLeft(MenuEntry);

	return false;
}

void Undock()
{
	Sanderling.MouseClickLeft(Measurement?.WindowStation?.FirstOrDefault()?.UndockButton);
	
	Host.Log("waiting for undocking to complete.");
	while(Measurement?.IsDocked ?? true)	 Host.Delay(1111);

	Host.Delay(4444);
	Sanderling.InvalidateMeasurement();
}

void ModuleMeasureAllTooltip()
{
	Host.Log("measure tooltips of all modules.");

	for (;;)
	{
		var NextModule = Sanderling.MemoryMeasurementAccu?.Value?.ShipUiModule?.FirstOrDefault(m => null == m?.TooltipLast);

		if(null == NextModule)
			break;

		Host.Log("measure module.");
		//	take multiple measurements of module tooltip to reduce risk to keep bad read tooltip.
		Sanderling.MouseMove(NextModule);
		Sanderling.WaitForMeasurement();
		Sanderling.MouseMove(NextModule);
	}
}

void ActivateHardenerExecute()
{
	var	SubsetModuleHardener =
		Sanderling.MemoryMeasurementAccu?.Value?.ShipUiModule
		?.Where(module => module?.TooltipLast?.Value?.IsHardener ?? false);

	var	SubsetModuleToToggle =
		SubsetModuleHardener
		?.Where(module => !(module?.RampActive ?? false));

	foreach (var Module in SubsetModuleToToggle.EmptyIfNull())
		ModuleToggle(Module);
}

void ModuleToggle(Sanderling.Accumulation.IShipUiModule Module)
{
	var ToggleKey = Module?.TooltipLast?.Value?.ToggleKey;

	Host.Log("toggle module using " + (null == ToggleKey ? "mouse" : Module?.TooltipLast?.Value?.ToggleKeyTextLabel?.Text));

	if(null == ToggleKey)
		Sanderling.MouseClickLeft(Module);
	else
		Sanderling.KeyboardPressCombined(ToggleKey);
}

void EnsureOverviewTypeSelectionLoaded()
{
	if(null == OverviewPresetTabActive || null == WindowOverview || null == OverviewPreset)
		return;

	if(string.Equals(OverviewTypeSelectionName, OverviewPreset, StringComparison.OrdinalIgnoreCase))
		return;

	Host.Log("loading preset '" + OverviewPreset + "' to overview (current selection is '" + OverviewTypeSelectionName + "').");
	Sanderling.MouseClickRight(OverviewPresetTabActive);
	Sanderling.MouseClickLeft(Menu?.FirstOrDefault()?.EntryFirstMatchingRegexPattern("load.*preset", RegexOptions.IgnoreCase));
	var PresetMenuEntry = Menu?.ElementAtOrDefault(1)?.EntryFirstMatchingRegexPattern(@"^\s*" + Regex.Escape(OverviewPreset) + @"\s*$", RegexOptions.IgnoreCase);

	if(null == PresetMenuEntry)
	{
		Host.Log("error: menu entry '" + OverviewPreset + "' not found");
		return;
	}

	Sanderling.MouseClickLeft(PresetMenuEntry);
}

void MemoryUpdate()
{
	EmergencyWarpOutUpdate();
	JammedLastTimeUpdate();
	OffloadCountUpdate();
}

void JammedLastTimeUpdate()
{
	if(Jammed)
		JammedLastTime	= Host.GetTimeContinuousMilli();
}

bool MeasurementEmergencyWarpOutEnter =>
	!(Measurement?.IsDocked ?? false) && !(EmergencyWarpOutHitpointPercent < ShieldHpPercent);

void EmergencyWarpOutUpdate()
{
	if (!MeasurementEmergencyWarpOutEnter)
		return;

	//	measure multiple times to avoid being scared off by noise from a single measurement. 
	Sanderling.InvalidateMeasurement();

	if (!MeasurementEmergencyWarpOutEnter)
		return;

	EmergencyWarpOutEnabled	= true;
}

void OffloadCountUpdate()
{
	var	OreHoldFillPercentSynced	= OreHoldFillPercent;

	if(!OreHoldFillPercentSynced.HasValue)
		return;

	if(0 == OreHoldFillPercentSynced && OreHoldFillPercentSynced < LastCheckOreHoldFillPercent)
		++OffloadCount;

	LastCheckOreHoldFillPercent = OreHoldFillPercentSynced;
}
        public void Log(string ss)
        {
            StreamWriter logwriter = File.AppendText("log.txt");
            logwriter.Write(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + " " + ss + "\n");
            logwriter.Close();
        }
