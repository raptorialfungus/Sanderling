﻿using Bib3;
using BotEngine.Common;
using System.Linq;
using MemoryStruct = Sanderling.Interface.MemoryStruct;
using Bib3.Geometrik;
using System;

namespace Sanderling.Parse
{
	public interface IMemoryMeasurement : MemoryStruct.IMemoryMeasurement
	{
		new IShipUiTarget[] Target { get; }

		new IModuleButtonTooltip ModuleButtonTooltip { get; }

		new IWindowOverview[] WindowOverview { get; }

		new IWindowInventory[] WindowInventory { get; }

		new IWindowAgentDialogue[] WindowAgentDialogue { get; }

		new INeocom Neocom { get; }

		bool? IsDocked { get; }

		bool? IsUnDocking { get; }
	}

	public class MemoryMeasurement : IMemoryMeasurement
	{
		MemoryStruct.IMemoryMeasurement Raw;

		public string UserDefaultLocaleName => Raw?.UserDefaultLocaleName;

		public IShipUiTarget[] Target { set; get; }

		public IModuleButtonTooltip ModuleButtonTooltip { set; get; }

		public IWindowOverview[] WindowOverview { set; get; }

		public IWindowInventory[] WindowInventory { set; get; }

		public IWindowAgentDialogue[] WindowAgentDialogue { set; get; }

		public INeocom Neocom { set; get; }

		public MemoryStruct.IUIElementText[] AbovemainMessage => Raw?.AbovemainMessage;

		public MemoryStruct.PanelGroup[] AbovemainPanelEveMenu => Raw?.AbovemainPanelEveMenu;

		public MemoryStruct.PanelGroup[] AbovemainPanelGroup => Raw?.AbovemainPanelGroup;

		public MemoryStruct.IUIElement InfoPanelButtonIncursions => Raw?.InfoPanelButtonIncursions;

		public MemoryStruct.IUIElement InfoPanelButtonCurrentSystem => Raw?.InfoPanelButtonCurrentSystem;

		public MemoryStruct.IUIElement InfoPanelButtonMissions => Raw?.InfoPanelButtonMissions;

		public MemoryStruct.IUIElement InfoPanelButtonRoute => Raw?.InfoPanelButtonRoute;

		public MemoryStruct.IInfoPanelSystem InfoPanelCurrentSystem => Raw?.InfoPanelCurrentSystem;

		public MemoryStruct.IInfoPanelMissions InfoPanelMissions => Raw?.InfoPanelMissions;

		public MemoryStruct.IInfoPanelRoute InfoPanelRoute => Raw?.InfoPanelRoute;

		public MemoryStruct.IMenu[] Menu => Raw?.Menu;

		public MemoryStruct.IContainer[] Tooltip => Raw?.Tooltip;

		MemoryStruct.INeocom MemoryStruct.IMemoryMeasurement.Neocom => Neocom;

		public MemoryStruct.IShipUi ShipUi => Raw?.ShipUi;

		public MemoryStruct.IWindow SystemMenu => Raw?.SystemMenu;

		MemoryStruct.IShipUiTarget[] MemoryStruct.IMemoryMeasurement.Target => Target;

		public MemoryStruct.IContainer[] Utilmenu => Raw?.Utilmenu;

		public string VersionString => Raw?.VersionString;

		public MemoryStruct.WindowAgentBrowser[] WindowAgentBrowser => Raw?.WindowAgentBrowser;

		MemoryStruct.IWindowAgentDialogue[] MemoryStruct.IMemoryMeasurement.WindowAgentDialogue => WindowAgentDialogue;

		public MemoryStruct.WindowChatChannel[] WindowChatChannel => Raw?.WindowChatChannel;

		public MemoryStruct.IWindowDroneView[] WindowDroneView => Raw?.WindowDroneView;

		public MemoryStruct.WindowFittingMgmt[] WindowFittingMgmt => Raw?.WindowFittingMgmt;

		public MemoryStruct.WindowShipFitting[] WindowShipFitting => Raw?.WindowShipFitting;

		MemoryStruct.IWindowOverview[] MemoryStruct.IMemoryMeasurement.WindowOverview => WindowOverview;

		MemoryStruct.IWindowInventory[] MemoryStruct.IMemoryMeasurement.WindowInventory => WindowInventory;

		public MemoryStruct.WindowItemSell[] WindowItemSell => Raw?.WindowItemSell;

		public MemoryStruct.WindowMarketAction[] WindowMarketAction => Raw?.WindowMarketAction;

		public MemoryStruct.IWindow[] WindowOther => Raw?.WindowOther;

		public MemoryStruct.WindowPeopleAndPlaces[] WindowPeopleAndPlaces => Raw?.WindowPeopleAndPlaces;

		public MemoryStruct.WindowRegionalMarket[] WindowRegionalMarket => Raw?.WindowRegionalMarket;

		public MemoryStruct.IWindowSelectedItemView[] WindowSelectedItemView => Raw?.WindowSelectedItemView;

		public MemoryStruct.WindowStack[] WindowStack => Raw?.WindowStack;

		public MemoryStruct.IWindowStation[] WindowStation => Raw?.WindowStation;

		public MemoryStruct.IWindowSurveyScanView[] WindowSurveyScanView => Raw?.WindowSurveyScanView;

		public MemoryStruct.WindowTelecom[] WindowTelecom => Raw?.WindowTelecom;

		MemoryStruct.IContainer MemoryStruct.IMemoryMeasurement.ModuleButtonTooltip => ModuleButtonTooltip;

		public bool? IsDocked { private set; get; }

		public bool? IsUnDocking { private set; get; }

		public Vektor2DInt ScreenSize => Raw?.ScreenSize ?? default(Vektor2DInt);

		MemoryMeasurement()
		{
		}

		public MemoryMeasurement(MemoryStruct.IMemoryMeasurement Raw)
		{
			this.Raw = Raw;

			if (null == Raw)
			{
				return;
			}

			Culture.InvokeInParseCulture(() =>
			{
				Target = Raw?.Target?.Select(ShipUiExtension.Parse)?.ToArray();

				ModuleButtonTooltip = Raw?.ModuleButtonTooltip?.ParseAsModuleButtonTooltip();

				WindowOverview = Raw?.WindowOverview?.Select(OverviewExtension.Parse)?.ToArray();

				WindowInventory = Raw?.WindowInventory?.Select(InventoryExtension.Parse)?.ToArray();

				WindowAgentDialogue = Raw?.WindowAgentDialogue?.Select(DialogueMissionExtension.Parse)?.ToArray();

				var ShipUi = Raw?.ShipUi;

				var SetWindowStation = Raw?.WindowStation;

				if (!SetWindowStation.IsNullOrEmpty())
				{
					IsDocked = true;
				}

				if (null != ShipUi ||
					(Raw?.WindowOverview?.WhereNotDefault()?.Any() ?? false))
				{
					IsDocked = false;
				}

				if (!(IsDocked ?? true))
				{
					IsUnDocking = false;
				}

				if (SetWindowStation?.Any(WindowStationLobby => WindowStationLobby?.LabelText?.Any(LabelText =>
					 LabelText?.Text?.RegexMatchSuccess(@"abort\s*undock|undocking") ?? false) ?? false) ?? false)
				{
					IsUnDocking = true;
				}

				Neocom = Raw?.Neocom?.Parse();
			});
		}
	}
}
