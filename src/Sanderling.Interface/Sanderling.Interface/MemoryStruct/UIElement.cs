﻿using Bib3.Geometrik;
using BotEngine;
using System;

namespace Sanderling.Interface.MemoryStruct
{
	public interface IObjectIdInMemory : IObjectIdInt64
	{
	}

	public interface IUIElement : IObjectIdInMemory
	{
		RectInt Region { get; }

		/// <summary>
		/// Element occludes other Elements with lower Value.
		/// </summary>
		int? InTreeIndex { get; }

		int? ChildLastInTreeIndex { get; }

		/// <summary>
		/// Region used to select or open contextmenu.
		/// </summary>
		IUIElement RegionInteraction { get; }
	}

	public interface IUIElementText : IUIElement
	{
		string Text { get; }
	}

	public interface IUIElementInputText : IUIElementText
	{
	}

	public interface ISelectable
	{
		bool? IsSelected { get; }
	}

	public interface IExpandable
	{
		IUIElement ExpandToggleButton { get; }

		bool? IsExpanded { get; }
	}

	public class ObjectIdInMemory : ObjectIdInt64, IObjectIdInMemory
	{
		ObjectIdInMemory()
		{
		}

		public ObjectIdInMemory(IObjectIdInt64 Base)
			: base(Base)
		{
		}

		public ObjectIdInMemory(Int64 Id)
			: base(Id)
		{
		}
	}

	public class UIElement : ObjectIdInMemory, IUIElement
	{
		public RectInt Region { set; get; }

		public int? InTreeIndex { set; get; }

		public int? ChildLastInTreeIndex { set; get; }

		virtual public IUIElement RegionInteraction => this;

		public UIElement()
			:
			this((IUIElement)null)
		{
		}

		public UIElement(IObjectIdInMemory Base)
			:
			base(Base)
		{
		}

		public UIElement(IUIElement Base)
			:
			this((IObjectIdInMemory)Base)
		{
			Region = Base?.Region ?? RectInt.Empty;

			InTreeIndex = Base?.InTreeIndex;
			ChildLastInTreeIndex = Base?.ChildLastInTreeIndex;
		}

		public override string ToString() =>
			this.SensorObjectToString();
	}

	public class UIElementText : UIElement, IUIElementText
	{
		public string Text { set; get; }

		public UIElementText(
			IUIElement Base,
			string Text = null)
			:
			base(Base)
		{
			this.Text = Text;
		}

		public UIElementText(IUIElementText Base)
			:
			this(Base, Base?.Text)
		{
		}

		public UIElementText()
		{
		}
	}

	public class UIElementInputText : UIElementText, IUIElementInputText
	{
		public UIElementInputText(IUIElement Base, string Label = null)
			:
			base(Base, Label)
		{
		}

		public UIElementInputText(IUIElementText Base)
			:
			this(Base, Base?.Text)
		{
		}

		public UIElementInputText()
		{
		}
	}
}
