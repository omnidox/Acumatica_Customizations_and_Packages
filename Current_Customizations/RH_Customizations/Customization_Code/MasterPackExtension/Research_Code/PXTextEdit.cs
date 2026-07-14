using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PX.Data;

namespace PX.Web.UI
{
	// Token: 0x020000EA RID: 234
	[ToolboxData("<{0}:PXTextEdit runat=server></{0}:PXTextEdit>")]
	[Designer("PX.Web.UI.Design.PXFieldControlDesigner")]
	[DefaultProperty("Value")]
	[DefaultEvent("ValueChanged")]
	[PXDesignerPropertyFilter("AutoCallBack\r\nDataField\r\nHeight\r\nSkinID\r\nTabIndex\r\nWidth", "AutoSize\r\nEnabled\r\nLabelID\r\nTextAlign\r\nTextMode")]
	public class PXTextEdit : PXWebControl, IEditableTextControl, ITextControl, IFieldEditor, IStateProcessing, IPostBackDataHandler, ILabeledControl, ICallbackEventHandler, IDataSourceViewSchemaAccessor, IPXCallbackUpdatable, ISizedControl, IAutoSizedControl, IDefaultLocaleUser
	{
		// Token: 0x06001ADF RID: 6879 RVA: 0x00068F8C File Offset: 0x0006718C
		static PXTextEdit()
		{
			PXTextEdit.EventValueChanged = new object();
			PXTextEdit.EventCallBack = new object();
		}

		// Token: 0x14000083 RID: 131
		// (add) Token: 0x06001AE0 RID: 6880 RVA: 0x00068FAC File Offset: 0x000671AC
		// (remove) Token: 0x06001AE1 RID: 6881 RVA: 0x00068FBF File Offset: 0x000671BF
		[Category("Action")]
		[Description("Occurs when the button receives a server callback.")]
		public event PXCallBackEventHandler CallBack
		{
			add
			{
				base.Events.AddHandler(PXTextEdit.EventCallBack, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXTextEdit.EventCallBack, value);
			}
		}

		// Token: 0x14000084 RID: 132
		// (add) Token: 0x06001AE2 RID: 6882 RVA: 0x00068FD2 File Offset: 0x000671D2
		// (remove) Token: 0x06001AE3 RID: 6883 RVA: 0x00068FE5 File Offset: 0x000671E5
		[Category("Action")]
		[Description("Occurs when the value of the editor changes between posts to the server.")]
		public event EventHandler ValueChanged
		{
			add
			{
				base.Events.AddHandler(PXTextEdit.EventValueChanged, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXTextEdit.EventValueChanged, value);
			}
		}

		// Token: 0x14000085 RID: 133
		// (add) Token: 0x06001AE4 RID: 6884 RVA: 0x00068FF8 File Offset: 0x000671F8
		// (remove) Token: 0x06001AE5 RID: 6885 RVA: 0x0006900B File Offset: 0x0006720B
		[Category("Action")]
		[Description("Occurs when the text of the editor changes between posts to the server.")]
		public event EventHandler TextChanged
		{
			add
			{
				base.Events.AddHandler(PXTextEdit.EventTextChanged, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXTextEdit.EventTextChanged, value);
			}
		}

		// Token: 0x14000086 RID: 134
		// (add) Token: 0x06001AE6 RID: 6886 RVA: 0x00069020 File Offset: 0x00067220
		// (remove) Token: 0x06001AE7 RID: 6887 RVA: 0x00069054 File Offset: 0x00067254
		[Category("DataSource")]
		[Description("Occurs when DataSource prepares a command.")]
		public event PXCommandPreparing DSCommandPreparing
		{
			add
			{
				IPXGraphAccessor graphAccessor = this.GraphAccessor;
				if (graphAccessor != null)
				{
					graphAccessor.DataGraph.CommandPreparing.AddHandler(graphAccessor.DataMember, this.DataField, value);
				}
			}
			remove
			{
				IPXGraphAccessor graphAccessor = this.GraphAccessor;
				if (graphAccessor != null)
				{
					graphAccessor.DataGraph.CommandPreparing.RemoveHandler(graphAccessor.DataMember, this.DataField, value);
				}
			}
		}

		// Token: 0x14000087 RID: 135
		// (add) Token: 0x06001AE8 RID: 6888 RVA: 0x00069088 File Offset: 0x00067288
		// (remove) Token: 0x06001AE9 RID: 6889 RVA: 0x000690BC File Offset: 0x000672BC
		[Category("DataSource")]
		[Description("Occurs before a field of the DataSource view is defaulted.")]
		public event PXFieldDefaulting DSFieldDefaulting
		{
			add
			{
				IPXGraphAccessor graphAccessor = this.GraphAccessor;
				if (graphAccessor != null)
				{
					graphAccessor.DataGraph.FieldDefaulting.AddHandler(graphAccessor.DataMember, this.DataField, value);
				}
			}
			remove
			{
				IPXGraphAccessor graphAccessor = this.GraphAccessor;
				if (graphAccessor != null)
				{
					graphAccessor.DataGraph.FieldDefaulting.RemoveHandler(graphAccessor.DataMember, this.DataField, value);
				}
			}
		}

		// Token: 0x14000088 RID: 136
		// (add) Token: 0x06001AEA RID: 6890 RVA: 0x000690F0 File Offset: 0x000672F0
		// (remove) Token: 0x06001AEB RID: 6891 RVA: 0x00069124 File Offset: 0x00067324
		[Category("DataSource")]
		[Description("Occurs before a field of the DataSource view is updated.")]
		public event PXFieldUpdating DSFieldUpdating
		{
			add
			{
				IPXGraphAccessor graphAccessor = this.GraphAccessor;
				if (graphAccessor != null)
				{
					graphAccessor.DataGraph.FieldUpdating.AddHandler(graphAccessor.DataMember, this.DataField, value);
				}
			}
			remove
			{
				IPXGraphAccessor graphAccessor = this.GraphAccessor;
				if (graphAccessor != null)
				{
					graphAccessor.DataGraph.FieldUpdating.RemoveHandler(graphAccessor.DataMember, this.DataField, value);
				}
			}
		}

		// Token: 0x14000089 RID: 137
		// (add) Token: 0x06001AEC RID: 6892 RVA: 0x00069158 File Offset: 0x00067358
		// (remove) Token: 0x06001AED RID: 6893 RVA: 0x0006918C File Offset: 0x0006738C
		[Category("DataSource")]
		[Description("Occurs after a field of the DataSource view has been updated.")]
		public event PXFieldUpdated DSFieldUpdated
		{
			add
			{
				IPXGraphAccessor graphAccessor = this.GraphAccessor;
				if (graphAccessor != null)
				{
					graphAccessor.DataGraph.FieldUpdated.AddHandler(graphAccessor.DataMember, this.DataField, value);
				}
			}
			remove
			{
				IPXGraphAccessor graphAccessor = this.GraphAccessor;
				if (graphAccessor != null)
				{
					graphAccessor.DataGraph.FieldUpdated.RemoveHandler(graphAccessor.DataMember, this.DataField, value);
				}
			}
		}

		// Token: 0x1400008A RID: 138
		// (add) Token: 0x06001AEE RID: 6894 RVA: 0x000691C0 File Offset: 0x000673C0
		// (remove) Token: 0x06001AEF RID: 6895 RVA: 0x000691F4 File Offset: 0x000673F4
		[Category("DataSource")]
		[Description("Occurs when a field of the DataSource view is selected.")]
		public event PXFieldSelecting DSFieldSelecting
		{
			add
			{
				IPXGraphAccessor graphAccessor = this.GraphAccessor;
				if (graphAccessor != null)
				{
					graphAccessor.DataGraph.FieldSelecting.AddHandler(graphAccessor.DataMember, this.DataField, value);
				}
			}
			remove
			{
				IPXGraphAccessor graphAccessor = this.GraphAccessor;
				if (graphAccessor != null)
				{
					graphAccessor.DataGraph.FieldSelecting.RemoveHandler(graphAccessor.DataMember, this.DataField, value);
				}
			}
		}

		// Token: 0x1400008B RID: 139
		// (add) Token: 0x06001AF0 RID: 6896 RVA: 0x00069228 File Offset: 0x00067428
		// (remove) Token: 0x06001AF1 RID: 6897 RVA: 0x0006925C File Offset: 0x0006745C
		[Category("DataSource")]
		[Description("Occurs when a field of the DataSource view is being verified.")]
		public event PXFieldVerifying DSFieldVerifying
		{
			add
			{
				IPXGraphAccessor graphAccessor = this.GraphAccessor;
				if (graphAccessor != null)
				{
					graphAccessor.DataGraph.FieldVerifying.AddHandler(graphAccessor.DataMember, this.DataField, value);
				}
			}
			remove
			{
				IPXGraphAccessor graphAccessor = this.GraphAccessor;
				if (graphAccessor != null)
				{
					graphAccessor.DataGraph.FieldVerifying.RemoveHandler(graphAccessor.DataMember, this.DataField, value);
				}
			}
		}

		// Token: 0x1400008C RID: 140
		// (add) Token: 0x06001AF2 RID: 6898 RVA: 0x00069290 File Offset: 0x00067490
		// (remove) Token: 0x06001AF3 RID: 6899 RVA: 0x000692C4 File Offset: 0x000674C4
		[Category("DataSource")]
		[Description("Occurs when DataSource throws an exception.")]
		public event PXExceptionHandling DSExceptionHandling
		{
			add
			{
				IPXGraphAccessor graphAccessor = this.GraphAccessor;
				if (graphAccessor != null)
				{
					graphAccessor.DataGraph.ExceptionHandling.AddHandler(graphAccessor.DataMember, this.DataField, value);
				}
			}
			remove
			{
				IPXGraphAccessor graphAccessor = this.GraphAccessor;
				if (graphAccessor != null)
				{
					graphAccessor.DataGraph.ExceptionHandling.RemoveHandler(graphAccessor.DataMember, this.DataField, value);
				}
			}
		}

		// Token: 0x17000966 RID: 2406
		// (get) Token: 0x06001AF4 RID: 6900 RVA: 0x000692F8 File Offset: 0x000674F8
		protected IPXGraphAccessor GraphAccessor
		{
			get
			{
				return base.BindingContainer as IPXGraphAccessor;
			}
		}

		// Token: 0x17000967 RID: 2407
		// (get) Token: 0x06001AF5 RID: 6901 RVA: 0x00069305 File Offset: 0x00067505
		// (set) Token: 0x06001AF6 RID: 6902 RVA: 0x00069318 File Offset: 0x00067518
		public override bool IsClientControl
		{
			get
			{
				return this.TextMode == TextBoxMode.Color || base.IsClientControl;
			}
			set
			{
				base.IsClientControl = value;
			}
		}

		// Token: 0x17000968 RID: 2408
		// (get) Token: 0x06001AF7 RID: 6903 RVA: 0x00069321 File Offset: 0x00067521
		// (set) Token: 0x06001AF8 RID: 6904 RVA: 0x00069338 File Offset: 0x00067538
		[DefaultValue("")]
		[Browsable(false)]
		[Localizable(true)]
		public virtual string Text
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "Text", string.Empty);
			}
			set
			{
				if (value != null)
				{
					value = value.TrimEnd(Array.Empty<char>());
				}
				STM.SetProp<string>(this.ViewState, "Text", value, string.Empty);
			}
		}

		// Token: 0x17000969 RID: 2409
		// (get) Token: 0x06001AF9 RID: 6905 RVA: 0x00069360 File Offset: 0x00067560
		// (set) Token: 0x06001AFA RID: 6906 RVA: 0x000693A6 File Offset: 0x000675A6
		[Category("Data")]
		[DefaultValue("")]
		[ScriptBrowsable]
		[Localizable(true)]
		[Description("The text that is displayed when the control contains null.")]
		[Browsable(false)]
		public virtual string NullText
		{
			get
			{
				string prop = STM.GetProp<string>(this.ViewState, "NullText", string.Empty);
				if (!base.DesignMode)
				{
					return ControlHelper.LocalizeValue(prop, this.ID, "NullText", this.Page, false, null);
				}
				return prop;
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "NullText", value, string.Empty);
			}
		}

		// Token: 0x1700096A RID: 2410
		// (get) Token: 0x06001AFB RID: 6907 RVA: 0x000693BE File Offset: 0x000675BE
		// (set) Token: 0x06001AFC RID: 6908 RVA: 0x000693D5 File Offset: 0x000675D5
		[Category("Data")]
		[DefaultValue("")]
		[ScriptBrowsable]
		[Description("The regular expression that is used to validate control text.")]
		[Browsable(false)]
		public virtual string ValidateExp
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "ValidateExp", string.Empty);
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "ValidateExp", value, string.Empty);
			}
		}

		// Token: 0x1700096B RID: 2411
		// (get) Token: 0x06001AFD RID: 6909 RVA: 0x000693F0 File Offset: 0x000675F0
		// (set) Token: 0x06001AFE RID: 6910 RVA: 0x00069436 File Offset: 0x00067636
		[Category("Data")]
		[DefaultValue("")]
		[ScriptBrowsable]
		[Description("HTML placeholder attribute. ")]
		[Browsable(false)]
		public virtual string Placeholder
		{
			get
			{
				string prop = STM.GetProp<string>(this.ViewState, "Placeholder", string.Empty);
				if (!base.DesignMode)
				{
					return ControlHelper.LocalizeValue(prop, this.ID, "Placeholder", this.Page, false, null);
				}
				return prop;
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "Placeholder", value, string.Empty);
			}
		}

		// Token: 0x1700096C RID: 2412
		// (get) Token: 0x06001AFF RID: 6911 RVA: 0x0006944E File Offset: 0x0006764E
		// (set) Token: 0x06001B00 RID: 6912 RVA: 0x00069461 File Offset: 0x00067661
		[Category("Behavior")]
		[DefaultValue(false)]
		[ScriptBrowsable]
		[Description("Indicates whether the control allows a user to type only ASCII characters.")]
		[Browsable(false)]
		public virtual bool AsciiCharSet
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "AsciiCharSet", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "AsciiCharSet", value, false);
			}
		}

		// Token: 0x1700096D RID: 2413
		// (get) Token: 0x06001B01 RID: 6913 RVA: 0x00069475 File Offset: 0x00067675
		// (set) Token: 0x06001B02 RID: 6914 RVA: 0x00069488 File Offset: 0x00067688
		[Category("Behavior")]
		[DefaultValue(0)]
		[Description("The width of the text box in characters.")]
		[Browsable(false)]
		public virtual int Columns
		{
			get
			{
				return STM.GetProp<int>(this.ViewState, "Columns", 0);
			}
			set
			{
				STM.SetProp<int>(this.ViewState, "Columns", value, 0);
			}
		}

		// Token: 0x1700096E RID: 2414
		// (get) Token: 0x06001B03 RID: 6915 RVA: 0x0006949C File Offset: 0x0006769C
		// (set) Token: 0x06001B04 RID: 6916 RVA: 0x000694AF File Offset: 0x000676AF
		[Category("Behavior")]
		[DefaultValue(0)]
		[Description("The number of lines to display for a multiline text box.")]
		[Browsable(false)]
		public virtual int Rows
		{
			get
			{
				return STM.GetProp<int>(this.ViewState, "Rows", 0);
			}
			set
			{
				STM.SetProp<int>(this.ViewState, "Rows", value, 0);
			}
		}

		// Token: 0x1700096F RID: 2415
		// (get) Token: 0x06001B05 RID: 6917 RVA: 0x000694C3 File Offset: 0x000676C3
		// (set) Token: 0x06001B06 RID: 6918 RVA: 0x000694D6 File Offset: 0x000676D6
		[Category("Behavior")]
		[DefaultValue(0)]
		[Description("The maximal nuber of characters that can be entered.")]
		[Browsable(false)]
		public virtual int MaxLength
		{
			get
			{
				return STM.GetProp<int>(this.ViewState, "MaxLength", 0);
			}
			set
			{
				STM.SetProp<int>(this.ViewState, "MaxLength", value, 0);
			}
		}

		// Token: 0x17000970 RID: 2416
		// (get) Token: 0x06001B07 RID: 6919 RVA: 0x000694EC File Offset: 0x000676EC
		// (set) Token: 0x06001B08 RID: 6920 RVA: 0x00069510 File Offset: 0x00067710
		[DefaultValue(null)]
		[Category("Ext. Property")]
		[Description("Indicates whether the control needs to render linked label.")]
		public virtual bool? SuppressLabel
		{
			get
			{
				return base.GetProp<bool?>("SuppressLabel", null);
			}
			set
			{
				base.SetProp<bool?>("SuppressLabel", value, null);
			}
		}

		// Token: 0x17000971 RID: 2417
		// (get) Token: 0x06001B09 RID: 6921 RVA: 0x00069532 File Offset: 0x00067732
		// (set) Token: 0x06001B0A RID: 6922 RVA: 0x00069545 File Offset: 0x00067745
		[Category("Behavior")]
		[DefaultValue(false)]
		[ScriptBrowsable(ScriptBrowsable.Dynamic)]
		[Description("Indicates whether the text in the control can be changed.")]
		[Browsable(false)]
		public virtual bool ReadOnly
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "ReadOnly", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "ReadOnly", value, false);
			}
		}

		// Token: 0x17000972 RID: 2418
		// (get) Token: 0x06001B0B RID: 6923 RVA: 0x00069559 File Offset: 0x00067759
		// (set) Token: 0x06001B0C RID: 6924 RVA: 0x0006956C File Offset: 0x0006776C
		[Category("Ext. Property")]
		[DefaultValue(TextBoxMode.SingleLine)]
		[Description("The behavior mode of the text box.")]
		public virtual TextBoxMode TextMode
		{
			get
			{
				return STM.GetProp<TextBoxMode>(this.ViewState, "TextMode", TextBoxMode.SingleLine);
			}
			set
			{
				STM.SetProp<TextBoxMode>(this.ViewState, "TextMode", value, TextBoxMode.SingleLine);
			}
		}

		// Token: 0x17000973 RID: 2419
		// (get) Token: 0x06001B0D RID: 6925 RVA: 0x00069580 File Offset: 0x00067780
		// (set) Token: 0x06001B0E RID: 6926 RVA: 0x00069593 File Offset: 0x00067793
		[Category("Behavior")]
		[DefaultValue(true)]
		[Description("Indicates whether the text should wrap.")]
		[Browsable(false)]
		public virtual bool Wrap
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "Wrap", true);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "Wrap", value, true);
			}
		}

		// Token: 0x17000974 RID: 2420
		// (get) Token: 0x06001B0F RID: 6927 RVA: 0x000695A7 File Offset: 0x000677A7
		// (set) Token: 0x06001B10 RID: 6928 RVA: 0x000695BA File Offset: 0x000677BA
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("Indicates whether the properties should be automatically loaded from the server during callback request.")]
		[Browsable(false)]
		public virtual bool CallbackUpdatable
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "CallbackUpdatable", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "CallbackUpdatable", value, false);
			}
		}

		// Token: 0x17000975 RID: 2421
		// (get) Token: 0x06001B11 RID: 6929 RVA: 0x000695CE File Offset: 0x000677CE
		// (set) Token: 0x06001B12 RID: 6930 RVA: 0x000695E1 File Offset: 0x000677E1
		[Category("Behavior")]
		[Themeable(false)]
		[DefaultValue(false)]
		[ScriptBrowsable]
		[Description("Indicates whether automatic postback to the server should be performed after the text has been modified.")]
		[Browsable(false)]
		public virtual bool AutoPostBack
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "AutoPostBack", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "AutoPostBack", value, false);
			}
		}

		// Token: 0x17000976 RID: 2422
		// (get) Token: 0x06001B13 RID: 6931 RVA: 0x000695F5 File Offset: 0x000677F5
		// (set) Token: 0x06001B14 RID: 6932 RVA: 0x00069608 File Offset: 0x00067808
		[DefaultValue(true)]
		[Category("Behavior")]
		[ScriptBrowsable]
		[Description("The style of selection on start edit mode.")]
		[Browsable(false)]
		public virtual bool SelectOnFocus
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "SelectOnFocus", true);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "SelectOnFocus", value, true);
			}
		}

		// Token: 0x17000977 RID: 2423
		// (get) Token: 0x06001B15 RID: 6933 RVA: 0x0006961C File Offset: 0x0006781C
		// (set) Token: 0x06001B16 RID: 6934 RVA: 0x0006962F File Offset: 0x0006782F
		[DefaultValue(false)]
		[Category("Behavior")]
		[ScriptBrowsable]
		[Description("Indicates whether the default response to the Enter key press is disabled.")]
		[Browsable(false)]
		public virtual bool HideEnterKey
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "HideEnterKey", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "HideEnterKey", value, false);
			}
		}

		// Token: 0x17000978 RID: 2424
		// (get) Token: 0x06001B17 RID: 6935 RVA: 0x00069643 File Offset: 0x00067843
		// (set) Token: 0x06001B18 RID: 6936 RVA: 0x00069656 File Offset: 0x00067856
		[DefaultValue(HorizontalAlign.NotSet)]
		[Category("Ext. Property")]
		[Description("The horizontal alignment of the text.")]
		public virtual HorizontalAlign TextAlign
		{
			get
			{
				return STM.GetProp<HorizontalAlign>(this.ViewState, "TextAlign", HorizontalAlign.NotSet);
			}
			set
			{
				STM.SetProp<HorizontalAlign>(this.ViewState, "TextAlign", value, HorizontalAlign.NotSet);
			}
		}

		// Token: 0x17000979 RID: 2425
		// (get) Token: 0x06001B19 RID: 6937 RVA: 0x0006966A File Offset: 0x0006786A
		// (set) Token: 0x06001B1A RID: 6938 RVA: 0x00069681 File Offset: 0x00067881
		[DefaultValue("")]
		[Themeable(false)]
		[IDReferenceProperty]
		[Browsable(false)]
		[TypeConverter("PX.Web.UI.Design.PXLabelControlConverter")]
		[Category("Ext. Property")]
		[Description("The identifier of the control label.")]
		public virtual string LabelID
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "LabelID", string.Empty);
			}
			set
			{
				if (value != this.LabelID)
				{
					this.label = null;
					STM.SetProp<string>(this.ViewState, "LabelID", value, string.Empty);
				}
			}
		}

		// Token: 0x1700097A RID: 2426
		// (get) Token: 0x06001B1B RID: 6939 RVA: 0x000696B0 File Offset: 0x000678B0
		private ITextControl Label
		{
			get
			{
				if (this.LabelID.Length > 0 && this.label == null)
				{
					this.label = (ControlHelper.FindControl(this, this.LabelID) as ITextControl);
					PXLabel pxlabel = this.label as PXLabel;
					if (pxlabel != null)
					{
						pxlabel.AssociatedClientID = this.InputClientID;
					}
				}
				return this.label;
			}
		}

		// Token: 0x1700097B RID: 2427
		// (get) Token: 0x06001B1C RID: 6940 RVA: 0x0006970B File Offset: 0x0006790B
		// (set) Token: 0x06001B1D RID: 6941 RVA: 0x00069722 File Offset: 0x00067922
		[DefaultValue("")]
		[Category("Appearance")]
		[Description("The text of the control label.")]
		[Browsable(false)]
		public virtual string LabelText
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "LabelText", string.Empty);
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "LabelText", value, string.Empty);
			}
		}

		// Token: 0x1700097C RID: 2428
		// (get) Token: 0x06001B1E RID: 6942 RVA: 0x0006973A File Offset: 0x0006793A
		// (set) Token: 0x06001B1F RID: 6943 RVA: 0x0006974D File Offset: 0x0006794D
		[Category("Ext. Property")]
		[DefaultValue(typeof(Unit), "")]
		[Description("The width of the control label.")]
		public virtual Unit LabelWidth
		{
			get
			{
				return base.GetProp<Unit>("LabelWidth", this._defLabelWidth);
			}
			set
			{
				base.SetProp<Unit>("LabelWidth", value, this._defLabelWidth);
			}
		}

		// Token: 0x1700097D RID: 2429
		// (get) Token: 0x06001B20 RID: 6944 RVA: 0x00069761 File Offset: 0x00067961
		// (set) Token: 0x06001B21 RID: 6945 RVA: 0x00069778 File Offset: 0x00067978
		[DefaultValue(":")]
		[Category("Appearance")]
		[Browsable(false)]
		[Description("The symbol sequence that ends the control label.")]
		public virtual string LabelPostfix
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "LabelPostfix", ":");
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "LabelPostfix", value, ":");
			}
		}

		// Token: 0x1700097E RID: 2430
		// (get) Token: 0x06001B22 RID: 6946 RVA: 0x00069790 File Offset: 0x00067990
		// (set) Token: 0x06001B23 RID: 6947 RVA: 0x000697A2 File Offset: 0x000679A2
		object IDataSourceViewSchemaAccessor.DataSourceViewSchema
		{
			get
			{
				return this.ViewState["DataSourceViewSchema"];
			}
			set
			{
				this.ViewState["DataSourceViewSchema"] = value;
			}
		}

		// Token: 0x1700097F RID: 2431
		// (get) Token: 0x06001B24 RID: 6948 RVA: 0x000697B5 File Offset: 0x000679B5
		protected bool Disabled
		{
			get
			{
				return !this.Enabled || this.ReadOnly;
			}
		}

		// Token: 0x17000980 RID: 2432
		// (get) Token: 0x06001B25 RID: 6949 RVA: 0x000697C7 File Offset: 0x000679C7
		// (set) Token: 0x06001B26 RID: 6950 RVA: 0x000697DA File Offset: 0x000679DA
		[ScriptBrowsable]
		[Browsable(false)]
		[Category("Appearance")]
		[Description("The color for the disabled control.")]
		public virtual string DisableColor
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "DisableColor", null);
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "DisableColor", value, null);
			}
		}

		// Token: 0x17000981 RID: 2433
		// (get) Token: 0x06001B27 RID: 6951 RVA: 0x000697EE File Offset: 0x000679EE
		// (set) Token: 0x06001B28 RID: 6952 RVA: 0x00069805 File Offset: 0x00067A05
		[Category("Base Property")]
		[DefaultValue("")]
		[TypeConverter(typeof(PXControlSizeConverter))]
		[Description("The control width.")]
		public virtual string Size
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "Size", string.Empty);
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "Size", value, string.Empty);
			}
		}

		// Token: 0x17000982 RID: 2434
		// (get) Token: 0x06001B29 RID: 6953 RVA: 0x0006981D File Offset: 0x00067A1D
		// (set) Token: 0x06001B2A RID: 6954 RVA: 0x00069830 File Offset: 0x00067A30
		[DefaultValue(false)]
		[Themeable(false)]
		[ScriptBrowsable]
		[Category("Ext. Property")]
		[Description("indicates whether the control gets the Enabled state from the callback target.")]
		public virtual bool SyncStateWithCommand
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "SyncStateWithCommand", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "SyncStateWithCommand", value, false);
			}
		}

		// Token: 0x17000983 RID: 2435
		// (get) Token: 0x06001B2B RID: 6955 RVA: 0x00069844 File Offset: 0x00067A44
		// (set) Token: 0x06001B2C RID: 6956 RVA: 0x00069857 File Offset: 0x00067A57
		[DefaultValue(false)]
		[Themeable(false)]
		[Category("Ext. Property")]
		[Description("Indicates whether the spell checking should be disabled in the multiline mode")]
		public virtual bool DisableSpellcheck
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "DisableSpellcheck", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "DisableSpellcheck", value, false);
			}
		}

		// Token: 0x17000984 RID: 2436
		// (get) Token: 0x06001B2D RID: 6957 RVA: 0x0006986B File Offset: 0x00067A6B
		// (set) Token: 0x06001B2E RID: 6958 RVA: 0x00069873 File Offset: 0x00067A73
		[ScriptBrowsable(ScriptBrowsable.Dynamic)]
		[Browsable(true)]
		[Category("Ext. Property")]
		public override bool Enabled
		{
			get
			{
				return base.Enabled;
			}
			set
			{
				base.Enabled = value;
			}
		}

		// Token: 0x17000985 RID: 2437
		// (get) Token: 0x06001B2F RID: 6959 RVA: 0x0006987C File Offset: 0x00067A7C
		// (set) Token: 0x06001B30 RID: 6960 RVA: 0x00069884 File Offset: 0x00067A84
		[Browsable(false)]
		public override short TabIndex
		{
			get
			{
				return base.TabIndex;
			}
			set
			{
				base.TabIndex = value;
			}
		}

		// Token: 0x17000986 RID: 2438
		// (get) Token: 0x06001B31 RID: 6961 RVA: 0x0006988D File Offset: 0x00067A8D
		// (set) Token: 0x06001B32 RID: 6962 RVA: 0x00069895 File Offset: 0x00067A95
		[Category("Ext. Property")]
		public override Unit Width
		{
			get
			{
				return base.Width;
			}
			set
			{
				base.Width = value;
			}
		}

		// Token: 0x17000987 RID: 2439
		// (get) Token: 0x06001B33 RID: 6963 RVA: 0x0006989E File Offset: 0x00067A9E
		// (set) Token: 0x06001B34 RID: 6964 RVA: 0x000698A6 File Offset: 0x00067AA6
		[Category("Ext. Property")]
		public override Unit Height
		{
			get
			{
				return base.Height;
			}
			set
			{
				base.Height = value;
			}
		}

		// Token: 0x06001B35 RID: 6965 RVA: 0x000698AF File Offset: 0x00067AAF
		protected bool ShouldSerializeID()
		{
			return string.IsNullOrEmpty(this.DataField) || !(this.ID == "ed" + this.DataField);
		}

		// Token: 0x17000988 RID: 2440
		// (get) Token: 0x06001B36 RID: 6966 RVA: 0x000698DF File Offset: 0x00067ADF
		[Category("Appearance")]
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Browsable(false)]
		[Description("The amount of space to be inserted between the text and the edges of the input field.")]
		public WebEdges Padding
		{
			get
			{
				return ((PXStyle)base.ControlStyle).Padding;
			}
		}

		// Token: 0x17000989 RID: 2441
		// (get) Token: 0x06001B37 RID: 6967 RVA: 0x000698F1 File Offset: 0x00067AF1
		[Category("Appearance")]
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Browsable(false)]
		[Description("The color, width, and style for each side of the border.")]
		public WebBorder Border
		{
			get
			{
				return ((PXStyle)base.ControlStyle).Border;
			}
		}

		// Token: 0x1700098A RID: 2442
		// (get) Token: 0x06001B38 RID: 6968 RVA: 0x00069903 File Offset: 0x00067B03
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Ext. Property")]
		[Description("The properties of the auto-size mode of the control.")]
		public virtual PXAutoSizeInfo AutoSize
		{
			get
			{
				if (this.autoSize == null)
				{
					this.autoSize = new PXAutoSizeInfo(base.IsTrackingViewState);
				}
				return this.autoSize;
			}
		}

		// Token: 0x1700098B RID: 2443
		// (get) Token: 0x06001B39 RID: 6969 RVA: 0x00069924 File Offset: 0x00067B24
		[Category("Behavior")]
		[DefaultValue(null)]
		[ScriptBrowsable]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Browsable(false)]
		[Description("The list of client-side events that should be processed by the application.")]
		public PXValControlEvents ClientEvents
		{
			get
			{
				if (this.clientEvents == null)
				{
					this.clientEvents = new PXValControlEvents(base.IsTrackingViewState);
				}
				return this.clientEvents;
			}
		}

		// Token: 0x1700098C RID: 2444
		// (get) Token: 0x06001B3A RID: 6970 RVA: 0x00069945 File Offset: 0x00067B45
		[DefaultValue(null)]
		[Themeable(false)]
		[ScriptBrowsable]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Ext. Property")]
		[Description("The settings of the callback request.")]
		public virtual PXCallbackSettings AutoCallBack
		{
			get
			{
				if (this.autoCallBack == null)
				{
					this.autoCallBack = new PXCallbackSettings(this, base.IsTrackingViewState);
				}
				return this.autoCallBack;
			}
		}

		// Token: 0x1700098D RID: 2445
		// (get) Token: 0x06001B3B RID: 6971 RVA: 0x00069967 File Offset: 0x00067B67
		[DefaultValue(null)]
		[Themeable(false)]
		[ScriptBrowsable]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Ext. Property")]
		[Description("The settings of the callback request.")]
		public virtual PXCallbackSettings LinkCommand
		{
			get
			{
				if (this.linkCommand == null)
				{
					this.linkCommand = new PXCallbackSettings(this, base.IsTrackingViewState);
				}
				return this.linkCommand;
			}
		}

		// Token: 0x1700098E RID: 2446
		// (get) Token: 0x06001B3C RID: 6972 RVA: 0x00069989 File Offset: 0x00067B89
		// (set) Token: 0x06001B3D RID: 6973 RVA: 0x0006999C File Offset: 0x00067B9C
		[DefaultValue(ActionType.None)]
		[Themeable(false)]
		[Description("The action type of the callback.")]
		[Browsable(false)]
		public virtual ActionType OnValueChange
		{
			get
			{
				return STM.GetProp<ActionType>(this.ViewState, "OnValueChange", ActionType.None);
			}
			set
			{
				STM.SetProp<ActionType>(this.ViewState, "OnValueChange", value, ActionType.None);
			}
		}

		// Token: 0x1700098F RID: 2447
		// (get) Token: 0x06001B3E RID: 6974 RVA: 0x000699B0 File Offset: 0x00067BB0
		// (set) Token: 0x06001B3F RID: 6975 RVA: 0x000699C3 File Offset: 0x00067BC3
		[Category("Base Property")]
		[DefaultValue(false)]
		[Themeable(false)]
		[Description("Indicates whether the control performs commit callback after its value has been changed.")]
		public virtual bool CommitChanges
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "CommitChanges", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "CommitChanges", value, false);
			}
		}

		// Token: 0x17000990 RID: 2448
		// (get) Token: 0x06001B40 RID: 6976 RVA: 0x000699D7 File Offset: 0x00067BD7
		// (set) Token: 0x06001B41 RID: 6977 RVA: 0x000699F6 File Offset: 0x00067BF6
		[Bindable(true)]
		[TypeConverter(typeof(StringConverter))]
		[ScriptBrowsable(ScriptBrowsable.Dynamic)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Category("Data")]
		[Description("The value of the control.")]
		[Browsable(false)]
		public virtual object Value
		{
			get
			{
				if (this.AllowNull && this.Text.Length == 0)
				{
					return null;
				}
				return this.Text;
			}
			set
			{
				this.Text = (this.IsNullValue(value) ? string.Empty : value.ToString());
				ControlHelper.SerializeProp(this, new string[]
				{
					"Text"
				});
			}
		}

		// Token: 0x06001B42 RID: 6978 RVA: 0x00069A28 File Offset: 0x00067C28
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual void ResetValue()
		{
			this.Value = string.Empty;
		}

		// Token: 0x06001B43 RID: 6979 RVA: 0x00069A35 File Offset: 0x00067C35
		[EditorBrowsable(EditorBrowsableState.Never)]
		[Browsable(false)]
		protected virtual bool ShouldSerializeValue()
		{
			return !string.IsNullOrEmpty(this.Text);
		}

		// Token: 0x17000991 RID: 2449
		// (get) Token: 0x06001B44 RID: 6980 RVA: 0x00069A45 File Offset: 0x00067C45
		bool IFieldEditor.ValuePosted
		{
			get
			{
				return this.valuePosted;
			}
		}

		// Token: 0x17000992 RID: 2450
		// (get) Token: 0x06001B45 RID: 6981 RVA: 0x00069A4D File Offset: 0x00067C4D
		// (set) Token: 0x06001B46 RID: 6982 RVA: 0x00069A55 File Offset: 0x00067C55
		[Browsable(false)]
		public PxFieldStateProcessor StateProcessinig
		{
			get
			{
				return this._StateProcessinig;
			}
			set
			{
				this._StateProcessinig = value;
			}
		}

		// Token: 0x17000993 RID: 2451
		// (get) Token: 0x06001B47 RID: 6983 RVA: 0x00069A5E File Offset: 0x00067C5E
		// (set) Token: 0x06001B48 RID: 6984 RVA: 0x00069A75 File Offset: 0x00067C75
		[Category("Base Property")]
		[DefaultValue(false)]
		[TypeConverter("System.Web.UI.Design.DataSourceViewSchemaConverter")]
		public string SmartSuggestionMode
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "SmartSuggestionMode", string.Empty);
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "SmartSuggestionMode", value, string.Empty);
			}
		}

		// Token: 0x17000994 RID: 2452
		// (get) Token: 0x06001B49 RID: 6985 RVA: 0x00069A8D File Offset: 0x00067C8D
		// (set) Token: 0x06001B4A RID: 6986 RVA: 0x00069AA4 File Offset: 0x00067CA4
		[Category("Base Property")]
		[DefaultValue("")]
		[TypeConverter("System.Web.UI.Design.DataSourceViewSchemaConverter")]
		public string DataField
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "DataField", string.Empty);
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "DataField", value, string.Empty);
			}
		}

		// Token: 0x17000995 RID: 2453
		// (get) Token: 0x06001B4B RID: 6987 RVA: 0x00069ABC File Offset: 0x00067CBC
		// (set) Token: 0x06001B4C RID: 6988 RVA: 0x00069ACF File Offset: 0x00067CCF
		[Category("Data")]
		[DefaultValue(true)]
		[ScriptBrowsable]
		[Description("Indicates whether null values are allowed.")]
		[Browsable(false)]
		public virtual bool AllowNull
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "AllowNull", true);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "AllowNull", value, true);
			}
		}

		// Token: 0x17000996 RID: 2454
		// (get) Token: 0x06001B4D RID: 6989 RVA: 0x00069AE3 File Offset: 0x00067CE3
		// (set) Token: 0x06001B4E RID: 6990 RVA: 0x00069AF6 File Offset: 0x00067CF6
		[Category("Data")]
		[DefaultValue(false)]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		[Description("Indicates whether the control requires data entry.")]
		[Browsable(false)]
		public virtual bool Required
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "Required", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "Required", value, false);
			}
		}

		// Token: 0x06001B4F RID: 6991 RVA: 0x00069B0C File Offset: 0x00067D0C
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual void SynchronizeState(PXFieldState state)
		{
			ITextControl textControl = this.Label;
			if (textControl != null)
			{
				PXLabel pxlabel = textControl as PXLabel;
				if (pxlabel != null)
				{
					pxlabel.Hidden = !state.Visible;
				}
				else
				{
					WebControl webControl = textControl as WebControl;
					if (webControl != null && !state.Visible)
					{
						webControl.Style[HtmlTextWriterStyle.Display] = "none";
					}
				}
				textControl.Text = state.DisplayName + this.LabelPostfix;
			}
			else if (ControlHelper.NeedLabelTextSync(this))
			{
				this.LabelText = state.DisplayName + this.LabelPostfix;
			}
			if (this.SyncStateWithCommand)
			{
				this.ReadOnly = false;
			}
			if (state.Length > 0)
			{
				this.MaxLength = state.Length;
			}
			if (state != null && state.Enabled)
			{
				PXStringState pxstringState = state as PXStringState;
				if (pxstringState != null)
				{
					this.AsciiCharSet = !pxstringState.IsUnicode;
					this.Language = pxstringState.Language;
					return;
				}
			}
			else
			{
				this.DefaultLocale = string.Empty;
			}
		}

		// Token: 0x17000997 RID: 2455
		// (get) Token: 0x06001B50 RID: 6992 RVA: 0x00069BFA File Offset: 0x00067DFA
		// (set) Token: 0x06001B51 RID: 6993 RVA: 0x00069C02 File Offset: 0x00067E02
		[ScriptBrowsable(ScriptBrowsable.Always)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		[PXDefault("")]
		[DefaultValue(null)]
		public string Language { get; set; }

		// Token: 0x17000998 RID: 2456
		// (get) Token: 0x06001B52 RID: 6994 RVA: 0x00069C0B File Offset: 0x00067E0B
		// (set) Token: 0x06001B53 RID: 6995 RVA: 0x00069C13 File Offset: 0x00067E13
		[ScriptBrowsable(ScriptBrowsable.Always)]
		[DefaultValue(null)]
		public string LocalizationInfo { get; set; }

		// Token: 0x06001B54 RID: 6996 RVA: 0x00069C1C File Offset: 0x00067E1C
		public override string ToString()
		{
			return this.ClientID;
		}

		// Token: 0x06001B55 RID: 6997 RVA: 0x00069C24 File Offset: 0x00067E24
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual string GetRenderText(object value)
		{
			if (this.AllowNull && value == null)
			{
				return this.NullText;
			}
			return value.ToString();
		}

		// Token: 0x06001B56 RID: 6998 RVA: 0x00069C3E File Offset: 0x00067E3E
		[EditorBrowsable(EditorBrowsableState.Never)]
		public virtual bool CanEditType(TypeCode type)
		{
			return type == TypeCode.String;
		}

		// Token: 0x06001B57 RID: 6999 RVA: 0x00069C45 File Offset: 0x00067E45
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected virtual object GetPostValue(string text)
		{
			if (!this.IsNullValue(text))
			{
				return text;
			}
			if (this.AllowNull)
			{
				return null;
			}
			return string.Empty;
		}

		// Token: 0x06001B58 RID: 7000 RVA: 0x00069C64 File Offset: 0x00067E64
		protected virtual bool IsNullValue(object value)
		{
			if (value == null || value == DBNull.Value)
			{
				return true;
			}
			if (Convert.GetTypeCode(value) == TypeCode.String)
			{
				string text = (string)value;
				if ((this.AllowNull && text == this.NullText) || text.Length == 0)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x17000999 RID: 2457
		// (get) Token: 0x06001B59 RID: 7001 RVA: 0x00069CB0 File Offset: 0x00067EB0
		public override string ClientTagKey
		{
			get
			{
				if (this.TextMode == TextBoxMode.Color)
				{
					return "qp-color-picker";
				}
				return "qp-text-editor";
			}
		}

		// Token: 0x06001B5A RID: 7002 RVA: 0x00069CC8 File Offset: 0x00067EC8
		protected override Dictionary<string, object> GetRenderSettings()
		{
			Dictionary<string, object> renderSettings = base.GetRenderSettings();
			PXCallbackSettings pxcallbackSettings = this.LinkCommand;
			PXWebControl.AddRenderProperty<TextBoxMode>(renderSettings, "type", this.TextMode, TextBoxMode.SingleLine);
			PXWebControl.AddRenderProperty<bool>(renderSettings, "readOnly", this.ReadOnly, false);
			PXWebControl.AddRenderProperty<int>(renderSettings, "maxLength", this.MaxLength, 0);
			PXWebControl.AddRenderProperty<HorizontalAlign>(renderSettings, "textAlign", this.TextAlign, HorizontalAlign.NotSet);
			PXWebControl.AddRenderProperty<bool>(renderSettings, "spellcheck", !this.DisableSpellcheck, true);
			PXWebControl.AddRenderProperty<bool>(renderSettings, "hasEditLink", pxcallbackSettings.Enabled && !string.IsNullOrEmpty(pxcallbackSettings.Command), false);
			PXWebControl.AddRenderProperty<string>(renderSettings, "placeholder", this.Placeholder, string.Empty);
			PXWebControl.AddRenderProperty<string>(renderSettings, "smartSuggestionMode", this.SmartSuggestionMode, string.Empty);
			if (this.ShouldSerializeValue())
			{
				PXWebControl.AddRenderProperty<object>(renderSettings, "value", (this.Value != null) ? this.Value : string.Empty, string.Empty);
			}
			if (this.TextMode == TextBoxMode.MultiLine)
			{
				PXWebControl.AddRenderProperty<bool>(renderSettings, "wrap", this.Wrap, true);
				PXWebControl.AddRenderProperty<int>(renderSettings, "rows", this.Rows, 0);
				PXWebControl.AddRenderProperty<int>(renderSettings, "cols", this.Columns, 0);
			}
			return renderSettings;
		}

		// Token: 0x1700099A RID: 2458
		// (get) Token: 0x06001B5B RID: 7003 RVA: 0x00069DFE File Offset: 0x00067FFE
		protected override HtmlTextWriterTag TagKey
		{
			get
			{
				if (this.HasEditLink)
				{
					return HtmlTextWriterTag.A;
				}
				if (this.TextMode != TextBoxMode.MultiLine)
				{
					return HtmlTextWriterTag.Input;
				}
				return HtmlTextWriterTag.Textarea;
			}
		}

		// Token: 0x06001B5C RID: 7004 RVA: 0x00069E18 File Offset: 0x00068018
		protected override void Render(HtmlTextWriter writer)
		{
			if (this.Page != null)
			{
				this.Page.VerifyRenderingInServerForm(this);
			}
			ControlHelper.AppendSizeCss(this, this.Size);
			if (!this.IsClientControl)
			{
				ControlHelper.AppendDisabledCss(this);
			}
			ControlHelper.RenderFieldStart(writer, this, !this.Hidden);
			this.RenderClientBeginTag(writer);
			this.RenderEditor(writer);
			this.RenderClientEndTag(writer);
			ControlHelper.RenderFieldEnd(writer, this);
		}

		// Token: 0x06001B5D RID: 7005 RVA: 0x00069E80 File Offset: 0x00068080
		protected virtual void RenderEditor(HtmlTextWriter writer)
		{
			if (this.HasEditLink)
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Href, "javascript:void 0");
				if (this.TextAlign != HorizontalAlign.NotSet)
				{
					writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, this.TextAlign.ToString());
				}
			}
			this.RenderBeginTag(writer);
			if (this.TextMode == TextBoxMode.MultiLine || this.HasEditLink)
			{
				HttpUtility.HtmlEncode(this.GetRenderText(this.Value), writer);
			}
			this.RenderEndTag(writer);
		}

		// Token: 0x06001B5E RID: 7006 RVA: 0x00069EF8 File Offset: 0x000680F8
		protected override void AddAttributesToRender(HtmlTextWriter writer)
		{
			if (this.UniqueID != null)
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Name, this.UniqueID);
			}
			if (this.TagKey != HtmlTextWriterTag.Input && this.TagKey != HtmlTextWriterTag.Textarea)
			{
				base.AddAttributesToRender(writer);
				return;
			}
			this.AddInputAttributes(writer);
			base.AddAttributesToRender(writer);
		}

		// Token: 0x06001B5F RID: 7007 RVA: 0x00069F48 File Offset: 0x00068148
		protected virtual void AddInputAttributes(HtmlTextWriter writer)
		{
			switch (this.TextMode)
			{
			case TextBoxMode.SingleLine:
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Type, "text");
				string renderText = this.GetRenderText(this.Value);
				if (!string.IsNullOrEmpty(renderText))
				{
					writer.AddAttribute(HtmlTextWriterAttribute.Value, renderText);
				}
				break;
			}
			case TextBoxMode.MultiLine:
				if (this.Rows > 0)
				{
					writer.AddAttribute(HtmlTextWriterAttribute.Rows, this.Rows.ToString());
				}
				if (this.Columns > 0)
				{
					writer.AddAttribute(HtmlTextWriterAttribute.Cols, this.Columns.ToString());
				}
				if (!this.Wrap)
				{
					writer.AddAttribute(HtmlTextWriterAttribute.Wrap, "off");
				}
				break;
			case TextBoxMode.Password:
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Type, "password");
				string renderText2 = this.GetRenderText(this.Value);
				if (!string.IsNullOrEmpty(renderText2))
				{
					writer.AddAttribute(HtmlTextWriterAttribute.Value, renderText2);
				}
				break;
			}
			case TextBoxMode.Color:
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Type, "color");
				string renderText3 = this.GetRenderText(this.Value);
				if (!string.IsNullOrEmpty(renderText3))
				{
					writer.AddAttribute(HtmlTextWriterAttribute.Value, renderText3);
				}
				break;
			}
			}
			if (this.MaxLength > 0)
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Maxlength, this.MaxLength.ToString());
			}
			if (this.TextMode != TextBoxMode.MultiLine && this.Columns > 0)
			{
				writer.AddAttribute(HtmlTextWriterAttribute.Size, this.Columns.ToString());
			}
			if (this.DisableSpellcheck)
			{
				writer.AddAttribute("spellcheck", "false");
			}
			if (this.ReadOnly)
			{
				writer.AddAttribute(HtmlTextWriterAttribute.ReadOnly, "readonly");
			}
			if (this.TextAlign != HorizontalAlign.NotSet)
			{
				writer.AddStyleAttribute(HtmlTextWriterStyle.TextAlign, this.TextAlign.ToString());
			}
		}

		// Token: 0x06001B60 RID: 7008 RVA: 0x0006A0F4 File Offset: 0x000682F4
		protected virtual void OnValueChanged(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[PXTextEdit.EventTextChanged];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
			eventHandler = (EventHandler)base.Events[PXTextEdit.EventValueChanged];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		// Token: 0x06001B61 RID: 7009 RVA: 0x0006A144 File Offset: 0x00068344
		protected virtual void OnCallBack(PXCallBackEventArgs e)
		{
			PXCallBackEventHandler pxcallBackEventHandler = (PXCallBackEventHandler)base.Events[PXTextEdit.EventCallBack];
			if (pxcallBackEventHandler != null)
			{
				pxcallBackEventHandler(this, e);
			}
		}

		// Token: 0x1700099B RID: 2459
		// (get) Token: 0x06001B62 RID: 7010 RVA: 0x0006A174 File Offset: 0x00068374
		protected bool HasEditLink
		{
			get
			{
				PXCallbackSettings pxcallbackSettings = this.LinkCommand;
				return !this.Enabled && pxcallbackSettings.Enabled && !string.IsNullOrEmpty(pxcallbackSettings.Command);
			}
		}

		// Token: 0x06001B63 RID: 7011 RVA: 0x0006A1A8 File Offset: 0x000683A8
		protected override object SaveViewState()
		{
			object[] array = new object[2];
			array[0] = base.SaveViewState();
			STM.SaveState(array, new object[]
			{
				this.clientEvents
			});
			return array;
		}

		// Token: 0x06001B64 RID: 7012 RVA: 0x0006A1DC File Offset: 0x000683DC
		protected override void LoadViewState(object savedState)
		{
			if (savedState != null)
			{
				object[] array = (object[])savedState;
				if (array[0] != null)
				{
					base.LoadViewState(array[0]);
				}
				STM.LoadState(array, new object[]
				{
					this.ClientEvents
				});
			}
		}

		// Token: 0x06001B65 RID: 7013 RVA: 0x0006A216 File Offset: 0x00068416
		protected override void TrackViewState()
		{
			base.TrackViewState();
			STM.TrackState(this.clientEvents);
		}

		// Token: 0x1700099C RID: 2460
		// (get) Token: 0x06001B66 RID: 7014 RVA: 0x0006A229 File Offset: 0x00068429
		protected override bool RenderTemplateData
		{
			get
			{
				return false;
			}
		}

		// Token: 0x06001B67 RID: 7015 RVA: 0x0006A22C File Offset: 0x0006842C
		protected override void RegisterScriptModules(JSManager sm)
		{
			sm.RegisterModule(typeof(PXTextEdit), "PX.Web.UI.Scripts.px_textEdit.js");
		}

		// Token: 0x06001B68 RID: 7016 RVA: 0x0006A244 File Offset: 0x00068444
		protected override void RegisterScriptProperties(JSObject obj)
		{
			if (obj.BaseObject == this)
			{
				IPXDataControl bindingContainer = ControlHelper.GetBindingContainer(this);
				if (bindingContainer != null)
				{
					obj.Append("BindingContainer", ((Control)bindingContainer).ClientID);
				}
				if (this.Label != null)
				{
					obj.Append("LabelID", ((Control)this.Label).ClientID);
				}
				if (this.TextMode == TextBoxMode.MultiLine && this.AutoSize.Enabled)
				{
					obj.Append("AutoSize", this.AutoSize);
				}
				ActionType actionType = this.OnValueChange;
				if (actionType == ActionType.None && this.CommitChanges)
				{
					actionType = ActionType.Commit;
				}
				if (actionType != ActionType.None)
				{
					obj.Append("OnValueChange", actionType);
				}
				if (!string.IsNullOrEmpty(this.Language))
				{
					obj.Append("DataField", this.DataField, string.Empty);
				}
			}
		}

		// Token: 0x06001B69 RID: 7017 RVA: 0x0006A312 File Offset: 0x00068512
		protected virtual void RegisterCallbackProperties(JSObject obj)
		{
			if (!string.IsNullOrEmpty(this.Language))
			{
				obj.Append("DataField", this.DataField, string.Empty);
			}
		}

		// Token: 0x06001B6A RID: 7018 RVA: 0x0006A337 File Offset: 0x00068537
		void IPXCallbackUpdatable.RegisterProperties(JSObject obj)
		{
			this.RegisterCallbackProperties(obj);
		}

		// Token: 0x1700099D RID: 2461
		// (get) Token: 0x06001B6B RID: 7019 RVA: 0x0006A340 File Offset: 0x00068540
		string ILabeledControl.InputClientID
		{
			get
			{
				return this.InputClientID;
			}
		}

		// Token: 0x1700099E RID: 2462
		// (get) Token: 0x06001B6C RID: 7020 RVA: 0x0006A348 File Offset: 0x00068548
		// (set) Token: 0x06001B6D RID: 7021 RVA: 0x0006A350 File Offset: 0x00068550
		string ILabeledControl.LabelCss { get; set; }

		// Token: 0x1700099F RID: 2463
		// (get) Token: 0x06001B6E RID: 7022 RVA: 0x0006A359 File Offset: 0x00068559
		// (set) Token: 0x06001B6F RID: 7023 RVA: 0x0006A361 File Offset: 0x00068561
		string ILabeledControl.ControlCss { get; set; }

		// Token: 0x170009A0 RID: 2464
		// (get) Token: 0x06001B70 RID: 7024 RVA: 0x0006A36A File Offset: 0x0006856A
		// (set) Token: 0x06001B71 RID: 7025 RVA: 0x0006A372 File Offset: 0x00068572
		string ILabeledControl.WrapperCss { get; set; }

		// Token: 0x170009A1 RID: 2465
		// (get) Token: 0x06001B72 RID: 7026 RVA: 0x0006A37B File Offset: 0x0006857B
		protected virtual string PostDataKey
		{
			get
			{
				return this.UniqueID;
			}
		}

		// Token: 0x170009A2 RID: 2466
		// (get) Token: 0x06001B73 RID: 7027 RVA: 0x0006A383 File Offset: 0x00068583
		protected virtual string InputClientID
		{
			get
			{
				return this.ClientID;
			}
		}

		// Token: 0x06001B74 RID: 7028 RVA: 0x0006A38C File Offset: 0x0006858C
		protected virtual bool LoadPostData(string key, NameValueCollection postCollection)
		{
			PXStateParser.Parse(this, postCollection, new PXStateChangingHandler(this.ProcessStateChange));
			object value = this.Value;
			object obj = postCollection[this.PostDataKey];
			if (obj != null)
			{
				obj = this.GetPostValue(obj.ToString());
				this.valuePosted = true;
				if (!object.Equals(value, obj))
				{
					this.Value = obj;
					return true;
				}
			}
			return false;
		}

		// Token: 0x06001B75 RID: 7029 RVA: 0x0006A3EC File Offset: 0x000685EC
		protected virtual void RaisePostDataChangedEvent()
		{
			this.OnValueChanged(EventArgs.Empty);
		}

		// Token: 0x06001B76 RID: 7030 RVA: 0x0006A3F9 File Offset: 0x000685F9
		protected virtual void ProcessStateChange(PXStateChangingArgs arg)
		{
		}

		// Token: 0x06001B77 RID: 7031 RVA: 0x0006A3FB File Offset: 0x000685FB
		void IPostBackDataHandler.RaisePostDataChangedEvent()
		{
			this.RaisePostDataChangedEvent();
		}

		// Token: 0x06001B78 RID: 7032 RVA: 0x0006A403 File Offset: 0x00068603
		bool IPostBackDataHandler.LoadPostData(string key, NameValueCollection postCollection)
		{
			return this.LoadPostData(key, postCollection);
		}

		// Token: 0x06001B79 RID: 7033 RVA: 0x0006A40D File Offset: 0x0006860D
		void ICallbackEventHandler.RaiseCallbackEvent(string arg)
		{
			this.callback = PXCallbackManager.GetInstance(this);
			this.callback.ProcessCallBack(arg, new PXCallbackExecMethod(this.ExecuteCallback));
		}

		// Token: 0x06001B7A RID: 7034 RVA: 0x0006A434 File Offset: 0x00068634
		protected virtual void ExecuteCallback(PXCallbackCommand cmd, string strData)
		{
			PXCallBackEventArgs pxcallBackEventArgs = new PXCallBackEventArgs(cmd, strData);
			this.OnCallBack(pxcallBackEventArgs);
			this.callbackResult = pxcallBackEventArgs.Result;
		}

		// Token: 0x06001B7B RID: 7035 RVA: 0x0006A45C File Offset: 0x0006865C
		string ICallbackEventHandler.GetCallbackResult()
		{
			return this.callback.GetCallbackResult(new PXCallbackResultMethod(this.GetCallbackResult));
		}

		// Token: 0x06001B7C RID: 7036 RVA: 0x0006A476 File Offset: 0x00068676
		protected virtual string GetCallbackResult(PXCallbackCommand cmd)
		{
			return this.callbackResult;
		}

		// Token: 0x170009A3 RID: 2467
		// (get) Token: 0x06001B7D RID: 7037 RVA: 0x0006A47E File Offset: 0x0006867E
		// (set) Token: 0x06001B7E RID: 7038 RVA: 0x0006A486 File Offset: 0x00068686
		[DefaultValue(null)]
		[ScriptBrowsable]
		[Browsable(false)]
		public virtual string DefaultLocale { get; set; }

		// Token: 0x06001B7F RID: 7039 RVA: 0x0006A48F File Offset: 0x0006868F
		void IDefaultLocaleUser.SetDefaultLocale(string name)
		{
			this.DefaultLocale = name;
		}

		// Token: 0x0400070A RID: 1802
		private PxFieldStateProcessor _StateProcessinig;

		// Token: 0x04000711 RID: 1809
		private PXValControlEvents clientEvents;

		// Token: 0x04000712 RID: 1810
		private string callbackResult;

		// Token: 0x04000713 RID: 1811
		private PXCallbackSettings autoCallBack;

		// Token: 0x04000714 RID: 1812
		private PXCallbackSettings linkCommand;

		// Token: 0x04000715 RID: 1813
		protected PXCallbackManager callback;

		// Token: 0x04000716 RID: 1814
		private ITextControl label;

		// Token: 0x04000717 RID: 1815
		protected bool valuePosted;

		// Token: 0x04000718 RID: 1816
		private PXAutoSizeInfo autoSize;

		// Token: 0x04000719 RID: 1817
		private const TextBoxMode _defTextMode = TextBoxMode.SingleLine;

		// Token: 0x0400071A RID: 1818
		private const HorizontalAlign _defTextAlign = HorizontalAlign.NotSet;

		// Token: 0x0400071B RID: 1819
		private Unit _defLabelWidth = Unit.Empty;

		// Token: 0x0400071C RID: 1820
		private const ActionType _defAction = ActionType.None;

		// Token: 0x0400071D RID: 1821
		private const string _defLabelPostfix = ":";

		// Token: 0x0400071E RID: 1822
		protected static readonly object EventValueChanged;

		// Token: 0x0400071F RID: 1823
		protected static readonly object EventTextChanged = new object();

		// Token: 0x04000720 RID: 1824
		protected static readonly object EventCallBack;
	}
}
