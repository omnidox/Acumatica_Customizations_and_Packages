using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PX.Common;
using PX.Data;

namespace PX.Web.UI
{
	// Token: 0x0200011C RID: 284
	[PXDesignerPropertyFilter("AutoCallBack\r\nDataField\r\nDataType\r\nType\r\nWidth", "AllowCheckAll\r\nAllowMove\r\nAllowResize\r\nAllowShowHide\r\nAllowSort\r\nLinkCommand\r\nMatrixMode\r\nNavigateParams\r\nTextAlign\r\nTextField\r\nTimeMode")]
	[Designer("PX.Web.UI.Design.PXLabelDesigner")]
	public class PXGridColumn : PXWebObject<PXGridColumn>, IStateProcessing, IDataSourceViewSchemaAccessor
	{
		// Token: 0x060021E7 RID: 8679 RVA: 0x0008F32B File Offset: 0x0008D52B
		public PXGridColumn()
		{
		}

		// Token: 0x060021E8 RID: 8680 RVA: 0x0008F35A File Offset: 0x0008D55A
		internal PXGridColumn(bool isTrack) : base(isTrack)
		{
		}

		// Token: 0x17000BB4 RID: 2996
		// (get) Token: 0x060021E9 RID: 8681 RVA: 0x0008F38A File Offset: 0x0008D58A
		// (set) Token: 0x060021EA RID: 8682 RVA: 0x0008F398 File Offset: 0x0008D598
		[NotifyParentProperty(true)]
		[DefaultValue(GridColumnType.NotSet)]
		[ScriptBrowsable]
		[Category("Base Property")]
		[Description("The type of column display.")]
		public virtual GridColumnType Type
		{
			get
			{
				return base.GetProp<GridColumnType>("Type", GridColumnType.NotSet);
			}
			set
			{
				base.SetProp<GridColumnType>("Type", value, GridColumnType.NotSet);
			}
		}

		// Token: 0x17000BB5 RID: 2997
		// (get) Token: 0x060021EB RID: 8683 RVA: 0x0008F3A7 File Offset: 0x0008D5A7
		// (set) Token: 0x060021EC RID: 8684 RVA: 0x0008F3B6 File Offset: 0x0008D5B6
		[NotifyParentProperty(true)]
		[DefaultValue(TypeCode.String)]
		[ScriptBrowsable]
		[Browsable(false)]
		[Description("The type of data within the column.")]
		public virtual TypeCode DataType
		{
			get
			{
				return base.GetProp<TypeCode>("DataType", TypeCode.String);
			}
			set
			{
				base.SetProp<TypeCode>("DataType", value, TypeCode.String);
				if (value == TypeCode.Decimal || value == TypeCode.Double || value == TypeCode.Single)
				{
					this.Decimals = 2;
				}
			}
		}

		// Token: 0x17000BB6 RID: 2998
		// (get) Token: 0x060021ED RID: 8685 RVA: 0x0008F3DC File Offset: 0x0008D5DC
		// (set) Token: 0x060021EE RID: 8686 RVA: 0x0008F3EF File Offset: 0x0008D5EF
		[DefaultValue(ValueDisplayMode.Value)]
		[ScriptBrowsable]
		[Category("Base Property")]
		[Description("The display mode of the column value.")]
		public ValueDisplayMode DisplayMode
		{
			get
			{
				return STM.GetProp<ValueDisplayMode>(base.ViewState, "DisplayMode", ValueDisplayMode.Value);
			}
			set
			{
				STM.SetProp<ValueDisplayMode>(base.ViewState, "DisplayMode", value, ValueDisplayMode.Value);
			}
		}

		// Token: 0x17000BB7 RID: 2999
		// (get) Token: 0x060021EF RID: 8687 RVA: 0x0008F403 File Offset: 0x0008D603
		// (set) Token: 0x060021F0 RID: 8688 RVA: 0x0008F411 File Offset: 0x0008D611
		[NotifyParentProperty(true)]
		[Category("Data")]
		[DefaultValue(false)]
		[Description("Indicates whether the column requires data entry.")]
		[Browsable(false)]
		public virtual bool Required
		{
			get
			{
				return base.GetProp<bool>("Required", false);
			}
			set
			{
				base.SetProp<bool>("Required", value, false);
			}
		}

		// Token: 0x17000BB8 RID: 3000
		// (get) Token: 0x060021F1 RID: 8689 RVA: 0x0008F420 File Offset: 0x0008D620
		// (set) Token: 0x060021F2 RID: 8690 RVA: 0x0008F42E File Offset: 0x0008D62E
		[NotifyParentProperty(true)]
		[DefaultValue(GridButtonDisplay.MouseOver)]
		[ScriptBrowsable]
		[Category("Behavior")]
		[Description("The mode how the cell buttons should be displayed in the column.")]
		[Browsable(false)]
		public virtual GridButtonDisplay ButtonDisplay
		{
			get
			{
				return base.GetProp<GridButtonDisplay>("ButtonDisplay", GridButtonDisplay.MouseOver);
			}
			set
			{
				base.SetProp<GridButtonDisplay>("ButtonDisplay", value, GridButtonDisplay.MouseOver);
			}
		}

		// Token: 0x17000BB9 RID: 3001
		// (get) Token: 0x060021F3 RID: 8691 RVA: 0x0008F440 File Offset: 0x0008D640
		// (set) Token: 0x060021F4 RID: 8692 RVA: 0x0008F464 File Offset: 0x0008D664
		[NotifyParentProperty(true)]
		[DefaultValue(typeof(bool?), "")]
		[Browsable(false)]
		[Category("Behavior")]
		[Description("Indicates whether the text should be editable in a multiline text area.")]
		public virtual bool? Multiline
		{
			get
			{
				return base.GetProp<bool?>("Multiline", null);
			}
			set
			{
				base.SetProp<bool?>("Multiline", value, null);
			}
		}

		// Token: 0x17000BBA RID: 3002
		// (get) Token: 0x060021F5 RID: 8693 RVA: 0x0008F486 File Offset: 0x0008D686
		// (set) Token: 0x060021F6 RID: 8694 RVA: 0x0008F494 File Offset: 0x0008D694
		[NotifyParentProperty(true)]
		[DefaultValue(false)]
		[ScriptBrowsable]
		[Category("Behavior")]
		[Description("Indicates whether the column shows a password.")]
		[Browsable(false)]
		public virtual bool IsPassword
		{
			get
			{
				return base.GetProp<bool>("IsPassword", false);
			}
			set
			{
				base.SetProp<bool>("IsPassword", value, false);
			}
		}

		// Token: 0x17000BBB RID: 3003
		// (get) Token: 0x060021F7 RID: 8695 RVA: 0x0008F4A3 File Offset: 0x0008D6A3
		// (set) Token: 0x060021F8 RID: 8696 RVA: 0x0008F4B1 File Offset: 0x0008D6B1
		[NotifyParentProperty(true)]
		[Category("Appearance")]
		[DefaultValue(false)]
		[ScriptBrowsable]
		[Description("Indicates whether the column inline editor value is rendered as column text.")]
		[Browsable(false)]
		public virtual bool RenderEditorText
		{
			get
			{
				return base.GetProp<bool>("RenderEditorText", false);
			}
			set
			{
				base.SetProp<bool>("RenderEditorText", value, false);
			}
		}

		// Token: 0x17000BBC RID: 3004
		// (get) Token: 0x060021F9 RID: 8697 RVA: 0x0008F4C0 File Offset: 0x0008D6C0
		// (set) Token: 0x060021FA RID: 8698 RVA: 0x0008F4D2 File Offset: 0x0008D6D2
		[NotifyParentProperty(true)]
		[DefaultValue("")]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		[Category("Appearance")]
		[Description("The formatting that is applied to the value.")]
		[Browsable(false)]
		public virtual string DisplayFormat
		{
			get
			{
				return base.GetProp<string>("DisplayFormat", string.Empty);
			}
			set
			{
				base.SetProp<string>("DisplayFormat", value, string.Empty);
			}
		}

		// Token: 0x17000BBD RID: 3005
		// (get) Token: 0x060021FB RID: 8699 RVA: 0x0008F4E5 File Offset: 0x0008D6E5
		// (set) Token: 0x060021FC RID: 8700 RVA: 0x0008F4F3 File Offset: 0x0008D6F3
		[NotifyParentProperty(true)]
		[Category("Ext. Property")]
		[DefaultValue(HorizontalAlign.NotSet)]
		[Description("The horizontal alignment of the contents in the cell.")]
		public HorizontalAlign TextAlign
		{
			get
			{
				return base.GetProp<HorizontalAlign>("TextAlign", HorizontalAlign.NotSet);
			}
			set
			{
				base.SetProp<HorizontalAlign>("TextAlign", value, HorizontalAlign.NotSet);
			}
		}

		// Token: 0x17000BBE RID: 3006
		// (get) Token: 0x060021FD RID: 8701 RVA: 0x0008F502 File Offset: 0x0008D702
		// (set) Token: 0x060021FE RID: 8702 RVA: 0x0008F510 File Offset: 0x0008D710
		[NotifyParentProperty(true)]
		[Category("Appearance")]
		[DefaultValue(TextCase.NotSet)]
		[ScriptBrowsable]
		[Description("The case that is used when the column text is displayed or edited.")]
		[Browsable(false)]
		public virtual TextCase TextCase
		{
			get
			{
				return base.GetProp<TextCase>("TextCase", TextCase.NotSet);
			}
			set
			{
				base.SetProp<TextCase>("TextCase", value, TextCase.NotSet);
			}
		}

		// Token: 0x17000BBF RID: 3007
		// (get) Token: 0x060021FF RID: 8703 RVA: 0x0008F51F File Offset: 0x0008D71F
		// (set) Token: 0x06002200 RID: 8704 RVA: 0x0008F527 File Offset: 0x0008D727
		[NotifyParentProperty(true)]
		[DefaultValue("")]
		[ScriptBrowsable]
		[Category("Data")]
		[Description("The view name of the current column.")]
		[Browsable(false)]
		public string ViewName
		{
			get
			{
				return this.viewName;
			}
			set
			{
				this.viewName = value;
				if (!string.IsNullOrEmpty(this.viewName) && this.Level != null)
				{
					this.Level.Grid.PrimaryLevel.CreateTemplateEditor(GridStandardEditor.Selector);
				}
			}
		}

		// Token: 0x17000BC0 RID: 3008
		// (get) Token: 0x06002201 RID: 8705 RVA: 0x0008F55B File Offset: 0x0008D75B
		// (set) Token: 0x06002202 RID: 8706 RVA: 0x0008F56D File Offset: 0x0008D76D
		[TypeConverter("System.Web.UI.Design.DataSourceViewSchemaConverter")]
		[NotifyParentProperty(true)]
		[Category("Base Property")]
		[DefaultValue("")]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		[Description("The name of the data field to bind to the GridColumn object.")]
		public virtual string DataField
		{
			get
			{
				return base.GetProp<string>("DataField", string.Empty);
			}
			set
			{
				base.SetProp<string>("DataField", value, string.Empty);
			}
		}

		// Token: 0x17000BC1 RID: 3009
		// (get) Token: 0x06002203 RID: 8707 RVA: 0x0008F580 File Offset: 0x0008D780
		// (set) Token: 0x06002204 RID: 8708 RVA: 0x0008F592 File Offset: 0x0008D792
		[TypeConverter("System.Web.UI.Design.DataSourceViewSchemaConverter")]
		[NotifyParentProperty(true)]
		[Category("Ext. Property")]
		[DefaultValue("")]
		[ScriptBrowsable]
		[Description("The data field that should be used as the column text.")]
		public virtual string TextField
		{
			get
			{
				return base.GetProp<string>("TextField", string.Empty);
			}
			set
			{
				base.SetProp<string>("TextField", value, string.Empty);
			}
		}

		// Token: 0x17000BC2 RID: 3010
		// (get) Token: 0x06002205 RID: 8709 RVA: 0x0008F5A5 File Offset: 0x0008D7A5
		// (set) Token: 0x06002206 RID: 8710 RVA: 0x0008F5B7 File Offset: 0x0008D7B7
		[TypeConverter("System.Web.UI.Design.DataSourceViewSchemaConverter")]
		[NotifyParentProperty(true)]
		[Category("Ext. Property")]
		[DefaultValue("")]
		[ScriptBrowsable]
		[Description("The data field that should be used as the column value.")]
		public virtual string ValueField
		{
			get
			{
				return base.GetProp<string>("ValueField", string.Empty);
			}
			set
			{
				base.SetProp<string>("ValueField", value, string.Empty);
			}
		}

		// Token: 0x17000BC3 RID: 3011
		// (get) Token: 0x06002207 RID: 8711 RVA: 0x0008F5CA File Offset: 0x0008D7CA
		// (set) Token: 0x06002208 RID: 8712 RVA: 0x0008F5DC File Offset: 0x0008D7DC
		[TypeConverter("System.Web.UI.Design.DataSourceViewSchemaConverter")]
		[NotifyParentProperty(true)]
		[Browsable(false)]
		[DefaultValue("")]
		[Description("The data field that should be used in a sort expression.")]
		public virtual string SortField
		{
			get
			{
				return base.GetProp<string>("SortField", string.Empty);
			}
			set
			{
				base.SetProp<string>("SortField", value, string.Empty);
			}
		}

		// Token: 0x17000BC4 RID: 3012
		// (get) Token: 0x06002209 RID: 8713 RVA: 0x0008F5EF File Offset: 0x0008D7EF
		// (set) Token: 0x0600220A RID: 8714 RVA: 0x0008F601 File Offset: 0x0008D801
		[NotifyParentProperty(true)]
		[Category("Data")]
		[DefaultValue("")]
		[ScriptBrowsable]
		[Description("The unique string that is used to identify calculated columns.")]
		[Browsable(false)]
		public string Key
		{
			get
			{
				return base.GetProp<string>("Key", string.Empty);
			}
			set
			{
				base.SetProp<string>("Key", value, string.Empty);
			}
		}

		// Token: 0x17000BC5 RID: 3013
		// (get) Token: 0x0600220B RID: 8715 RVA: 0x0008F614 File Offset: 0x0008D814
		// (set) Token: 0x0600220C RID: 8716 RVA: 0x0008F622 File Offset: 0x0008D822
		[Browsable(false)]
		[Description("The object to be associated with a Column object.")]
		public object Tag
		{
			get
			{
				return base.GetProp<object>("Tag", null);
			}
			set
			{
				base.SetProp<object>("Tag", value, null);
			}
		}

		// Token: 0x17000BC6 RID: 3014
		// (get) Token: 0x0600220D RID: 8717 RVA: 0x0008F631 File Offset: 0x0008D831
		// (set) Token: 0x0600220E RID: 8718 RVA: 0x0008F63F File Offset: 0x0008D83F
		[NotifyParentProperty(true)]
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("Indicates whether the column renders links for navigation to specified ViewName.")]
		[Browsable(false)]
		public virtual bool HideViewLink
		{
			get
			{
				return base.GetProp<bool>("HideViewLink", false);
			}
			set
			{
				base.SetProp<bool>("HideViewLink", value, false);
			}
		}

		// Token: 0x17000BC7 RID: 3015
		// (get) Token: 0x0600220F RID: 8719 RVA: 0x0008F650 File Offset: 0x0008D850
		// (set) Token: 0x06002210 RID: 8720 RVA: 0x0008F6A5 File Offset: 0x0008D8A5
		[NotifyParentProperty(true)]
		[DefaultValue("")]
		[ScriptBrowsable]
		[Localizable(true)]
		[Category("Data")]
		[Description("The string displayed in the cells with null values.")]
		[Browsable(false)]
		public virtual string NullText
		{
			get
			{
				string prop = base.GetProp<string>("NullText", string.Empty);
				if (this.Level != null)
				{
					return ControlHelper.LocalizeValue(prop, this.Level.Grid.ID, "ColumnNullText", this.Level.Grid.Page, false, null);
				}
				return prop;
			}
			set
			{
				base.SetProp<string>("NullText", value, string.Empty);
			}
		}

		// Token: 0x17000BC8 RID: 3016
		// (get) Token: 0x06002211 RID: 8721 RVA: 0x0008F6B8 File Offset: 0x0008D8B8
		[Browsable(false)]
		public string NullTextFinal
		{
			get
			{
				if (this.NullText.Length > 0)
				{
					return this.NullText;
				}
				if (this.Level != null)
				{
					return this.Level.LayoutFinal.NullText;
				}
				return string.Empty;
			}
		}

		// Token: 0x17000BC9 RID: 3017
		// (get) Token: 0x06002212 RID: 8722 RVA: 0x0008F6ED File Offset: 0x0008D8ED
		// (set) Token: 0x06002213 RID: 8723 RVA: 0x0008F6FB File Offset: 0x0008D8FB
		[NotifyParentProperty(true)]
		[Category("Data")]
		[DefaultValue(0)]
		[ScriptBrowsable]
		[Description("The maximal number of characters that is allowed in cells.")]
		[Browsable(false)]
		public virtual int MaxLength
		{
			get
			{
				return base.GetProp<int>("MaxLength", 0);
			}
			set
			{
				base.SetProp<int>("MaxLength", value, 0);
			}
		}

		// Token: 0x17000BCA RID: 3018
		// (get) Token: 0x06002214 RID: 8724 RVA: 0x0008F70A File Offset: 0x0008D90A
		// (set) Token: 0x06002215 RID: 8725 RVA: 0x0008F718 File Offset: 0x0008D918
		[DefaultValue(false)]
		[ScriptBrowsable]
		[Category("Ext. Property")]
		[Description("Indicates whether only time part of the DateTime type can be entered in the column.")]
		public virtual bool TimeMode
		{
			get
			{
				return base.GetProp<bool>("TimeMode", false);
			}
			set
			{
				base.SetProp<bool>("TimeMode", value, false);
			}
		}

		// Token: 0x17000BCB RID: 3019
		// (get) Token: 0x06002216 RID: 8726 RVA: 0x0008F727 File Offset: 0x0008D927
		// (set) Token: 0x06002217 RID: 8727 RVA: 0x0008F735 File Offset: 0x0008D935
		[DefaultValue(0)]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		[Category("Data")]
		[Description("The number of decimal places (precision).")]
		[Browsable(false)]
		public int Decimals
		{
			get
			{
				return base.GetProp<int>("Decimals", 0);
			}
			set
			{
				if (TypeHelper.IsNumeric(this.DataType))
				{
					base.SetProp<int>("Decimals", value, 0);
				}
			}
		}

		// Token: 0x17000BCC RID: 3020
		// (get) Token: 0x06002218 RID: 8728 RVA: 0x0008F754 File Offset: 0x0008D954
		// (set) Token: 0x06002219 RID: 8729 RVA: 0x0008F7A0 File Offset: 0x0008D9A0
		[NotifyParentProperty(true)]
		[Category("Data")]
		[DefaultValue(null)]
		[TypeConverter(typeof(StringConverter))]
		[ScriptBrowsable]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Description("The default value that is used for the cells in the column when a new row is added.")]
		[Browsable(false)]
		public object DefaultValue
		{
			get
			{
				object result = null;
				if (!string.IsNullOrEmpty(this.DefValueText))
				{
					try
					{
						result = Convert.ChangeType(this.DefValueText, this.DataType, CultureInfo.InvariantCulture);
					}
					catch (Exception)
					{
					}
				}
				return result;
			}
			set
			{
				object obj = null;
				if (value != null && value.ToString() != string.Empty && value is IConvertible)
				{
					obj = Convert.ChangeType(value, this.DataType);
				}
				this.DefValueText = ((obj == null) ? string.Empty : ((string)Convert.ChangeType(obj, TypeCode.String, CultureInfo.InvariantCulture)));
			}
		}

		// Token: 0x17000BCD RID: 3021
		// (get) Token: 0x0600221A RID: 8730 RVA: 0x0008F7FB File Offset: 0x0008D9FB
		// (set) Token: 0x0600221B RID: 8731 RVA: 0x0008F80D File Offset: 0x0008DA0D
		[Browsable(false)]
		[DefaultValue("")]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public string DefValueText
		{
			get
			{
				return base.GetProp<string>("DefValueText", string.Empty);
			}
			set
			{
				base.SetProp<string>("DefValueText", value, string.Empty);
			}
		}

		// Token: 0x17000BCE RID: 3022
		// (get) Token: 0x0600221C RID: 8732 RVA: 0x0008F820 File Offset: 0x0008DA20
		// (set) Token: 0x0600221D RID: 8733 RVA: 0x0008F82E File Offset: 0x0008DA2E
		[NotifyParentProperty(true)]
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("Indicates whether the value has been HTML encoded before displaying.")]
		[Browsable(false)]
		public virtual bool HtmlEncode
		{
			get
			{
				return base.GetProp<bool>("HtmlEncode", false);
			}
			set
			{
				base.SetProp<bool>("HtmlEncode", value, false);
			}
		}

		// Token: 0x17000BCF RID: 3023
		// (get) Token: 0x0600221E RID: 8734 RVA: 0x0008F83D File Offset: 0x0008DA3D
		// (set) Token: 0x0600221F RID: 8735 RVA: 0x0008F84B File Offset: 0x0008DA4B
		[NotifyParentProperty(true)]
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("Indicates whether the value has been HTML decoded before displaying.")]
		[Browsable(false)]
		public virtual bool HtmlDecode
		{
			get
			{
				return base.GetProp<bool>("HtmlDecode", false);
			}
			set
			{
				base.SetProp<bool>("HtmlDecode", value, false);
			}
		}

		// Token: 0x17000BD0 RID: 3024
		// (get) Token: 0x06002220 RID: 8736 RVA: 0x0008F85A File Offset: 0x0008DA5A
		// (set) Token: 0x06002221 RID: 8737 RVA: 0x0008F868 File Offset: 0x0008DA68
		[NotifyParentProperty(true)]
		[DefaultValue(true)]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		[Category("Behavior")]
		[Description("Indicates whether the column is visible.")]
		[Browsable(false)]
		public virtual bool Visible
		{
			get
			{
				return base.GetProp<bool>("Visible", true);
			}
			set
			{
				base.SetProp<bool>("Visible", value, true);
			}
		}

		// Token: 0x17000BD1 RID: 3025
		// (get) Token: 0x06002222 RID: 8738 RVA: 0x0008F877 File Offset: 0x0008DA77
		// (set) Token: 0x06002223 RID: 8739 RVA: 0x0008F885 File Offset: 0x0008DA85
		[NotifyParentProperty(true)]
		[DefaultValue(false)]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		[Browsable(false)]
		public virtual bool SkipTabs
		{
			get
			{
				return base.GetProp<bool>("SkipTabs", false);
			}
			set
			{
				base.SetProp<bool>("SkipTabs", value, false);
			}
		}

		// Token: 0x17000BD2 RID: 3026
		// (get) Token: 0x06002224 RID: 8740 RVA: 0x0008F894 File Offset: 0x0008DA94
		// (set) Token: 0x06002225 RID: 8741 RVA: 0x0008F8A6 File Offset: 0x0008DAA6
		[NotifyParentProperty(true)]
		[DefaultValue(typeof(Unit), "70px")]
		[Category("Base Property")]
		[Description("The width of the column.")]
		public virtual Unit Width
		{
			get
			{
				return base.GetProp<Unit>("Width", PXGridColumn._defWidth);
			}
			set
			{
				base.SetProp<Unit>("Width", value, PXGridColumn._defWidth);
			}
		}

		// Token: 0x17000BD3 RID: 3027
		// (get) Token: 0x06002226 RID: 8742 RVA: 0x0008F8BC File Offset: 0x0008DABC
		[Browsable(false)]
		public Unit WidthFinal
		{
			get
			{
				if (!this.Width.IsEmpty)
				{
					return this.Width;
				}
				return this.Level.LayoutFinal.ColWidth;
			}
		}

		// Token: 0x17000BD4 RID: 3028
		// (get) Token: 0x06002227 RID: 8743 RVA: 0x0008F8F0 File Offset: 0x0008DAF0
		internal bool HasDefaultWidth
		{
			get
			{
				return this.Width == PXGridColumn._defWidth;
			}
		}

		// Token: 0x17000BD5 RID: 3029
		// (get) Token: 0x06002228 RID: 8744 RVA: 0x0008F902 File Offset: 0x0008DB02
		// (set) Token: 0x06002229 RID: 8745 RVA: 0x0008F910 File Offset: 0x0008DB10
		[Browsable(false)]
		[Category("Behavior")]
		[Themeable(false)]
		[DefaultValue(SortDirection.None)]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		public virtual SortDirection SortDirection
		{
			get
			{
				return base.GetProp<SortDirection>("SortDirection", SortDirection.None);
			}
			set
			{
				base.SetProp<SortDirection>("SortDirection", value, SortDirection.None);
			}
		}

		// Token: 0x17000BD6 RID: 3030
		// (get) Token: 0x0600222A RID: 8746 RVA: 0x0008F91F File Offset: 0x0008DB1F
		// (set) Token: 0x0600222B RID: 8747 RVA: 0x0008F927 File Offset: 0x0008DB27
		[Browsable(false)]
		[Themeable(false)]
		[DefaultValue(false)]
		[Description("Indicates whether the row recalculation callback should occur after the cell value has been modified.")]
		public virtual bool AutoCallBack
		{
			get
			{
				return this.CommitChanges;
			}
			set
			{
				this.CommitChanges = value;
			}
		}

		// Token: 0x0600222C RID: 8748 RVA: 0x0008F930 File Offset: 0x0008DB30
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected void ResetAutoCallBack()
		{
			this.AutoCallBack = false;
		}

		// Token: 0x0600222D RID: 8749 RVA: 0x0008F939 File Offset: 0x0008DB39
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected bool ShouldSerializeAutoCallBack()
		{
			return false;
		}

		// Token: 0x17000BD7 RID: 3031
		// (get) Token: 0x0600222E RID: 8750 RVA: 0x0008F93C File Offset: 0x0008DB3C
		// (set) Token: 0x0600222F RID: 8751 RVA: 0x0008F94A File Offset: 0x0008DB4A
		[Category("Base Property")]
		[DefaultValue(false)]
		[Themeable(false)]
		[ScriptBrowsable]
		[Description("Indicates whether the control performs commit callback after a column value has been changed.")]
		public virtual bool CommitChanges
		{
			get
			{
				return base.GetProp<bool>("CommitChanges", false);
			}
			set
			{
				base.SetProp<bool>("CommitChanges", value, false);
			}
		}

		// Token: 0x17000BD8 RID: 3032
		// (get) Token: 0x06002230 RID: 8752 RVA: 0x0008F959 File Offset: 0x0008DB59
		// (set) Token: 0x06002231 RID: 8753 RVA: 0x0008F967 File Offset: 0x0008DB67
		[NotifyParentProperty(true)]
		[DefaultValue(true)]
		[ScriptBrowsable]
		[Category("Behavior")]
		[Description("Indicates whether the null values are allowed in cells.")]
		[Browsable(false)]
		public virtual bool AllowNull
		{
			get
			{
				return base.GetProp<bool>("AllowNull", true);
			}
			set
			{
				base.SetProp<bool>("AllowNull", value, true);
			}
		}

		// Token: 0x17000BD9 RID: 3033
		// (get) Token: 0x06002232 RID: 8754 RVA: 0x0008F976 File Offset: 0x0008DB76
		// (set) Token: 0x06002233 RID: 8755 RVA: 0x0008F984 File Offset: 0x0008DB84
		[Category("Behavior")]
		[DefaultValue(true)]
		[NotifyParentProperty(true)]
		[Browsable(false)]
		public bool AllowOnDashboard
		{
			get
			{
				return base.GetProp<bool>("AllowOnDashboard", true);
			}
			set
			{
				base.SetProp<bool>("AllowOnDashboard", value, true);
			}
		}

		// Token: 0x17000BDA RID: 3034
		// (get) Token: 0x06002234 RID: 8756 RVA: 0x0008F994 File Offset: 0x0008DB94
		// (set) Token: 0x06002235 RID: 8757 RVA: 0x0008F9B8 File Offset: 0x0008DBB8
		[NotifyParentProperty(true)]
		[DefaultValue(typeof(bool?), "")]
		[ScriptBrowsable]
		[Category("Ext. Property")]
		[Description("Indicates whether the column can be resized.")]
		public virtual bool? AllowResize
		{
			get
			{
				return base.GetProp<bool?>("AllowResize", null);
			}
			set
			{
				base.SetProp<bool?>("AllowResize", value, null);
			}
		}

		// Token: 0x17000BDB RID: 3035
		// (get) Token: 0x06002236 RID: 8758 RVA: 0x0008F9DC File Offset: 0x0008DBDC
		// (set) Token: 0x06002237 RID: 8759 RVA: 0x0008FA00 File Offset: 0x0008DC00
		[NotifyParentProperty(true)]
		[DefaultValue(typeof(bool?), "")]
		[ScriptBrowsable]
		[Category("Ext. Property")]
		[Description("Indicates whether the column can be moved.")]
		public virtual bool? AllowMove
		{
			get
			{
				return base.GetProp<bool?>("AllowMove", null);
			}
			set
			{
				base.SetProp<bool?>("AllowMove", value, null);
			}
		}

		// Token: 0x17000BDC RID: 3036
		// (get) Token: 0x06002238 RID: 8760 RVA: 0x0008FA22 File Offset: 0x0008DC22
		// (set) Token: 0x06002239 RID: 8761 RVA: 0x0008FA30 File Offset: 0x0008DC30
		[NotifyParentProperty(true)]
		[DefaultValue(true)]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		[Category("Behavior")]
		[Description("Indicates whether the column can be updated.")]
		[Browsable(false)]
		public virtual bool AllowUpdate
		{
			get
			{
				return base.GetProp<bool>("AllowUpdate", true);
			}
			set
			{
				base.SetProp<bool>("AllowUpdate", value, true);
			}
		}

		// Token: 0x17000BDD RID: 3037
		// (get) Token: 0x0600223A RID: 8762 RVA: 0x0008FA40 File Offset: 0x0008DC40
		// (set) Token: 0x0600223B RID: 8763 RVA: 0x0008FA64 File Offset: 0x0008DC64
		[Category("Ext. Property")]
		[DefaultValue(typeof(bool?), "")]
		[ScriptBrowsable]
		[Description("Indicates whether the column uses individual properties of the cells.")]
		public bool? MatrixMode
		{
			get
			{
				return base.GetProp<bool?>("MatrixMode", null);
			}
			set
			{
				base.SetProp<bool?>("MatrixMode", value, null);
			}
		}

		// Token: 0x17000BDE RID: 3038
		// (get) Token: 0x0600223C RID: 8764 RVA: 0x0008FA86 File Offset: 0x0008DC86
		// (set) Token: 0x0600223D RID: 8765 RVA: 0x0008FA94 File Offset: 0x0008DC94
		[NotifyParentProperty(true)]
		[DefaultValue(true)]
		[ScriptBrowsable]
		[Category("Behavior")]
		[Description("Indicates whether the column can receive focus.")]
		[Browsable(false)]
		public virtual bool AllowFocus
		{
			get
			{
				return base.GetProp<bool>("AllowFocus", true);
			}
			set
			{
				base.SetProp<bool>("AllowFocus", value, true);
			}
		}

		// Token: 0x17000BDF RID: 3039
		// (get) Token: 0x0600223E RID: 8766 RVA: 0x0008FAA4 File Offset: 0x0008DCA4
		// (set) Token: 0x0600223F RID: 8767 RVA: 0x0008FAC8 File Offset: 0x0008DCC8
		[NotifyParentProperty(true)]
		[DefaultValue(typeof(bool?), "")]
		[ScriptBrowsable]
		[Category("Ext. Property")]
		[Description("Indicates whether the column can be sorted.")]
		public virtual bool? AllowSort
		{
			get
			{
				return base.GetProp<bool?>("AllowSort", null);
			}
			set
			{
				base.SetProp<bool?>("AllowSort", value, null);
			}
		}

		// Token: 0x17000BE0 RID: 3040
		// (get) Token: 0x06002240 RID: 8768 RVA: 0x0008FAEC File Offset: 0x0008DCEC
		// (set) Token: 0x06002241 RID: 8769 RVA: 0x0008FB10 File Offset: 0x0008DD10
		[NotifyParentProperty(true)]
		[DefaultValue(typeof(bool?), "")]
		[ScriptBrowsable]
		[Category("Ext. Property")]
		[Description("Indicates whether the column can be filtered.")]
		public virtual bool? AllowFilter
		{
			get
			{
				return base.GetProp<bool?>("AllowFilter", null);
			}
			set
			{
				base.SetProp<bool?>("AllowFilter", value, null);
			}
		}

		// Token: 0x17000BE1 RID: 3041
		// (get) Token: 0x06002242 RID: 8770 RVA: 0x0008FB32 File Offset: 0x0008DD32
		// (set) Token: 0x06002243 RID: 8771 RVA: 0x0008FB40 File Offset: 0x0008DD40
		[NotifyParentProperty(true)]
		[DefaultValue(AllowShowHide.True)]
		[ScriptBrowsable]
		[Category("Ext. Property")]
		[Description("Indicates whether the column visibility can be changed.")]
		public virtual AllowShowHide AllowShowHide
		{
			get
			{
				return base.GetProp<AllowShowHide>("AllowShowHide", AllowShowHide.True);
			}
			set
			{
				base.SetProp<AllowShowHide>("AllowShowHide", value, AllowShowHide.True);
			}
		}

		// Token: 0x17000BE2 RID: 3042
		// (get) Token: 0x06002244 RID: 8772 RVA: 0x0008FB4F File Offset: 0x0008DD4F
		// (set) Token: 0x06002245 RID: 8773 RVA: 0x0008FB5D File Offset: 0x0008DD5D
		[Category("Ext. Property")]
		[NotifyParentProperty(true)]
		[DefaultValue(false)]
		[ScriptBrowsable]
		[Description("Indicates whether the state of all rows can be changed for the check box column.")]
		public virtual bool AllowCheckAll
		{
			get
			{
				return base.GetProp<bool>("AllowCheckAll", false);
			}
			set
			{
				base.SetProp<bool>("AllowCheckAll", value, false);
			}
		}

		// Token: 0x17000BE3 RID: 3043
		// (get) Token: 0x06002246 RID: 8774 RVA: 0x0008FB6C File Offset: 0x0008DD6C
		// (set) Token: 0x06002247 RID: 8775 RVA: 0x0008FB7F File Offset: 0x0008DD7F
		[Browsable(false)]
		[DefaultValue(false)]
		[Description("Indicates whether the column allows drag&drop operations.")]
		public bool AllowDragDrop
		{
			get
			{
				return STM.GetProp<bool>(base.ViewState, "AllowDragDrop", false);
			}
			set
			{
				STM.SetProp<bool>(base.ViewState, "AllowDragDrop", value, false);
			}
		}

		// Token: 0x17000BE4 RID: 3044
		// (get) Token: 0x06002248 RID: 8776 RVA: 0x0008FB93 File Offset: 0x0008DD93
		// (set) Token: 0x06002249 RID: 8777 RVA: 0x0008FBA1 File Offset: 0x0008DDA1
		[DefaultValue(PXGeneratedColumnOption.NotSet)]
		[Category("Ext. Property")]
		[Description("The option that is used if grid columns are auto-generated.")]
		public virtual PXGeneratedColumnOption AutoGenerateOption
		{
			get
			{
				return base.GetProp<PXGeneratedColumnOption>("AutoGenerateOption", PXGeneratedColumnOption.NotSet);
			}
			set
			{
				base.SetProp<PXGeneratedColumnOption>("AutoGenerateOption", value, PXGeneratedColumnOption.NotSet);
			}
		}

		// Token: 0x17000BE5 RID: 3045
		// (get) Token: 0x0600224A RID: 8778 RVA: 0x0008FBB0 File Offset: 0x0008DDB0
		// (set) Token: 0x0600224B RID: 8779 RVA: 0x0008FBBE File Offset: 0x0008DDBE
		[Category("Behavior")]
		[DefaultValue(false)]
		[Description("Indicates whether the column should be exported during export procedure regardless of its visibility.")]
		[Browsable(false)]
		public bool ForceExport
		{
			get
			{
				return base.GetProp<bool>("ForceExport", false);
			}
			set
			{
				base.SetProp<bool>("ForceExport", value, false);
			}
		}

		// Token: 0x17000BE6 RID: 3046
		// (get) Token: 0x0600224C RID: 8780 RVA: 0x0008FBCD File Offset: 0x0008DDCD
		// (set) Token: 0x0600224D RID: 8781 RVA: 0x0008FBDF File Offset: 0x0008DDDF
		[DefaultValue("")]
		[ScriptBrowsable]
		[Category("Ext. Property")]
		[Description("The callback command for the link.")]
		public string LinkCommand
		{
			get
			{
				return base.GetProp<string>("LinkCommand", string.Empty);
			}
			set
			{
				base.SetProp<string>("LinkCommand", value, string.Empty);
			}
		}

		// Token: 0x17000BE7 RID: 3047
		// (get) Token: 0x0600224E RID: 8782 RVA: 0x0008FBF2 File Offset: 0x0008DDF2
		// (set) Token: 0x0600224F RID: 8783 RVA: 0x0008FBFA File Offset: 0x0008DDFA
		[DefaultValue(null)]
		[ScriptBrowsable]
		[Category("Ext. Property")]
		[Description("The ID of the control that executes popup command.")]
		public virtual string PopupCommandTarget { get; set; }

		// Token: 0x17000BE8 RID: 3048
		// (get) Token: 0x06002250 RID: 8784 RVA: 0x0008FC03 File Offset: 0x0008DE03
		// (set) Token: 0x06002251 RID: 8785 RVA: 0x0008FC0B File Offset: 0x0008DE0B
		[DefaultValue(null)]
		[ScriptBrowsable]
		[Category("Ext. Property")]
		[Description("The popup command name.")]
		public virtual string PopupCommand { get; set; }

		// Token: 0x17000BE9 RID: 3049
		// (get) Token: 0x06002252 RID: 8786 RVA: 0x0008FC14 File Offset: 0x0008DE14
		// (set) Token: 0x06002253 RID: 8787 RVA: 0x0008FC22 File Offset: 0x0008DE22
		[Category("Ext. Property")]
		[DefaultValue(false)]
		[Browsable(false)]
		[ScriptBrowsable]
		[Description("Indicates whether the column allows string values instead of actual data type.")]
		public virtual bool AllowStrings
		{
			get
			{
				return base.GetProp<bool>("AllowStrings", false);
			}
			set
			{
				base.SetProp<bool>("AllowStrings", value, false);
			}
		}

		// Token: 0x17000BEA RID: 3050
		// (get) Token: 0x06002254 RID: 8788 RVA: 0x0008FC31 File Offset: 0x0008DE31
		[Browsable(false)]
		public int Index
		{
			get
			{
				if (this.collection != null)
				{
					return this.collection.IndexOf(this);
				}
				return -1;
			}
		}

		// Token: 0x17000BEB RID: 3051
		// (get) Token: 0x06002255 RID: 8789 RVA: 0x0008FC49 File Offset: 0x0008DE49
		[Browsable(false)]
		public PXGridLevel Level
		{
			get
			{
				if (this.collection != null)
				{
					return this.collection.Level;
				}
				return null;
			}
		}

		// Token: 0x17000BEC RID: 3052
		// (get) Token: 0x06002256 RID: 8790 RVA: 0x0008FC60 File Offset: 0x0008DE60
		// (set) Token: 0x06002257 RID: 8791 RVA: 0x0008FC72 File Offset: 0x0008DE72
		[NotifyParentProperty(true)]
		[DefaultValue("")]
		[Category("Behavior")]
		[Description("The ID of the control for editing of column values.")]
		[Browsable(false)]
		public virtual string EditorID
		{
			get
			{
				return base.GetProp<string>("EditorID", string.Empty);
			}
			set
			{
				base.SetProp<string>("EditorID", value, string.Empty);
			}
		}

		// Token: 0x17000BED RID: 3053
		// (get) Token: 0x06002258 RID: 8792 RVA: 0x0008FC88 File Offset: 0x0008DE88
		[Browsable(false)]
		[DefaultValue(null)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[ScriptBrowsable]
		public string FormEditorID
		{
			get
			{
				if (this.collection == null || this.DataField.Length == 0)
				{
					return null;
				}
				IFieldEditor fieldEditor = this.Level.GetFieldEditor(this.DataField);
				if (fieldEditor == null)
				{
					return null;
				}
				return fieldEditor.ID;
			}
		}

		// Token: 0x17000BEE RID: 3054
		// (get) Token: 0x06002259 RID: 8793 RVA: 0x0008FCC9 File Offset: 0x0008DEC9
		// (set) Token: 0x0600225A RID: 8794 RVA: 0x0008FCD1 File Offset: 0x0008DED1
		[DefaultValue(true)]
		[Category("Ext. Property")]
		[Description("Allows 'null' value for the column if the bound data field is nullable or if the column has NullTextFinal property assigned.")]
		public bool SyncNullable { get; set; } = true;

		// Token: 0x17000BEF RID: 3055
		// (get) Token: 0x0600225B RID: 8795 RVA: 0x0008FCDA File Offset: 0x0008DEDA
		// (set) Token: 0x0600225C RID: 8796 RVA: 0x0008FCE2 File Offset: 0x0008DEE2
		[DefaultValue(true)]
		[Category("Ext. Property")]
		[Description("Allows the column to assign its 'Visible' value from the bound data field's 'Visible' value.")]
		public bool SyncVisible { get; set; } = true;

		// Token: 0x17000BF0 RID: 3056
		// (get) Token: 0x0600225D RID: 8797 RVA: 0x0008FCEB File Offset: 0x0008DEEB
		// (set) Token: 0x0600225E RID: 8798 RVA: 0x0008FCF3 File Offset: 0x0008DEF3
		[DefaultValue(true)]
		[Category("Ext. Property")]
		[Description("Makes sure the column cannot become visible if the bound data field is invisible.")]
		public bool SyncVisibility { get; set; } = true;

		// Token: 0x17000BF1 RID: 3057
		// (get) Token: 0x0600225F RID: 8799 RVA: 0x0008FCFC File Offset: 0x0008DEFC
		// (set) Token: 0x06002260 RID: 8800 RVA: 0x0008FD0E File Offset: 0x0008DF0E
		object IDataSourceViewSchemaAccessor.DataSourceViewSchema
		{
			get
			{
				return base.ViewState["DataSourceViewSchema"];
			}
			set
			{
				base.ViewState["DataSourceViewSchema"] = value;
			}
		}

		// Token: 0x17000BF2 RID: 3058
		// (get) Token: 0x06002261 RID: 8801 RVA: 0x0008FD21 File Offset: 0x0008DF21
		public bool IsDataKey
		{
			get
			{
				return this.Level != null && this.Level.DataKeyNames.Contains(this.DataField, StringComparer.OrdinalIgnoreCase);
			}
		}

		// Token: 0x17000BF3 RID: 3059
		// (get) Token: 0x06002262 RID: 8802 RVA: 0x0008FD48 File Offset: 0x0008DF48
		[NotifyParentProperty(true)]
		[Category("Appearance")]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Description("The object that contains the column header properties.")]
		[Browsable(false)]
		public PXGridHeader Header
		{
			get
			{
				if (this.header == null)
				{
					this.header = new PXGridHeader(this.isTrackingViewState);
				}
				return this.header;
			}
		}

		// Token: 0x17000BF4 RID: 3060
		// (get) Token: 0x06002263 RID: 8803 RVA: 0x0008FD69 File Offset: 0x0008DF69
		// (set) Token: 0x06002264 RID: 8804 RVA: 0x0008FD81 File Offset: 0x0008DF81
		[Category("Appearance")]
		[DefaultValue("")]
		[Description("The user-friendly name of the column.")]
		[Browsable(false)]
		public string Label
		{
			get
			{
				return base.GetProp<string>("Label", this.Header.Text);
			}
			set
			{
				base.SetProp<string>("Label", value, this.Header.Text);
			}
		}

		// Token: 0x06002265 RID: 8805 RVA: 0x0008FD9A File Offset: 0x0008DF9A
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected bool ShouldSerializeLabel()
		{
			return this.Header.Text != this.Label;
		}

		// Token: 0x06002266 RID: 8806 RVA: 0x0008FDB2 File Offset: 0x0008DFB2
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void ResetLabel()
		{
			this.Label = this.Header.Text;
		}

		// Token: 0x17000BF5 RID: 3061
		// (get) Token: 0x06002267 RID: 8807 RVA: 0x0008FDC5 File Offset: 0x0008DFC5
		[NotifyParentProperty(true)]
		[Category("Appearance")]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Description("The object that contains the column header properties.")]
		[Browsable(false)]
		public PXGridFooter Footer
		{
			get
			{
				if (this.footer == null)
				{
					this.footer = new PXGridFooter(this.isTrackingViewState);
				}
				return this.footer;
			}
		}

		// Token: 0x06002268 RID: 8808 RVA: 0x0008FDE6 File Offset: 0x0008DFE6
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected bool ShouldSerializeFooter()
		{
			return STM.HasData(this.footer);
		}

		// Token: 0x06002269 RID: 8809 RVA: 0x0008FDF3 File Offset: 0x0008DFF3
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void ResetFooter()
		{
			if (this.footer != null)
			{
				this.footer.Reset();
			}
		}

		// Token: 0x17000BF6 RID: 3062
		// (get) Token: 0x0600226A RID: 8810 RVA: 0x0008FE08 File Offset: 0x0008E008
		[NotifyParentProperty(true)]
		[Category("Data")]
		[Browsable(false)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Description("The object that contains the list of the possible values of the column.")]
		public PXValueItems ValueItems
		{
			get
			{
				if (this.valueItems == null)
				{
					this.valueItems = new PXValueItems(this.isTrackingViewState);
				}
				return this.valueItems;
			}
		}

		// Token: 0x0600226B RID: 8811 RVA: 0x0008FE29 File Offset: 0x0008E029
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected bool ShouldSerializeValueItems()
		{
			return STM.HasData(this.valueItems);
		}

		// Token: 0x0600226C RID: 8812 RVA: 0x0008FE36 File Offset: 0x0008E036
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void ResetValueItems()
		{
			if (this.valueItems != null)
			{
				this.valueItems.Reset();
			}
		}

		// Token: 0x17000BF7 RID: 3063
		// (get) Token: 0x0600226D RID: 8813 RVA: 0x0008FE4B File Offset: 0x0008E04B
		[DefaultValue(null)]
		[ScriptBrowsable]
		[Browsable(false)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Data")]
		[Description("The collection of the mask segments.")]
		public PXMaskItemCollection MaskItems
		{
			get
			{
				if (this.maskItems == null)
				{
					this.maskItems = new PXMaskItemCollection();
				}
				return this.maskItems;
			}
		}

		// Token: 0x17000BF8 RID: 3064
		// (get) Token: 0x0600226E RID: 8814 RVA: 0x0008FE68 File Offset: 0x0008E068
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Editor("PX.Web.UI.Design.PXParametersEditor", typeof(UITypeEditor))]
		[Category("Ext. Property")]
		[Description("The collection of the parameters that are used to navigate to the linked view.")]
		public PXParamCollection NavigateParams
		{
			get
			{
				PXGrid pxgrid = (this.Level != null) ? this.Level.Grid : null;
				if (this.navigateParams == null)
				{
					this.navigateParams = new PXParamCollection(pxgrid);
					if (this.isTrackingViewState)
					{
						((IStateManager)this.navigateParams).TrackViewState();
					}
				}
				if (this.navigateParams.Owner != pxgrid)
				{
					this.navigateParams.Owner = pxgrid;
				}
				return this.navigateParams;
			}
		}

		// Token: 0x17000BF9 RID: 3065
		// (get) Token: 0x0600226F RID: 8815 RVA: 0x0008FED4 File Offset: 0x0008E0D4
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Editor("PX.Web.UI.Design.PXParametersEditor", typeof(UITypeEditor))]
		[Category("Behavior")]
		[Description("The collection of the parameters for the linked view.")]
		[Browsable(false)]
		public PXParamCollection AdditionalParams
		{
			get
			{
				PXGrid pxgrid = (this.Level != null) ? this.Level.Grid : null;
				if (this.additionalParams == null)
				{
					this.additionalParams = new PXParamCollection(pxgrid);
					if (this.isTrackingViewState)
					{
						((IStateManager)this.additionalParams).TrackViewState();
					}
				}
				if (this.additionalParams.Owner != pxgrid)
				{
					this.additionalParams.Owner = pxgrid;
				}
				return this.additionalParams;
			}
		}

		// Token: 0x17000BFA RID: 3066
		// (get) Token: 0x06002270 RID: 8816 RVA: 0x0008FF3F File Offset: 0x0008E13F
		// (set) Token: 0x06002271 RID: 8817 RVA: 0x0008FF47 File Offset: 0x0008E147
		[Browsable(false)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[TemplateContainer(typeof(PXGridCellContainer))]
		[TemplateInstance(TemplateInstance.Multiple)]
		public ITemplate CellTemplate
		{
			get
			{
				return this.cellTemplate;
			}
			set
			{
				this.cellTemplate = value;
			}
		}

		// Token: 0x06002272 RID: 8818 RVA: 0x0008FF50 File Offset: 0x0008E150
		[Browsable(false)]
		internal WebControl GetCellTemplateContainer(PXGridRow row)
		{
			PXGridCellContainer pxgridCellContainer = new PXGridCellContainer(row);
			if (this.cellTemplate != null)
			{
				this.cellTemplate.InstantiateIn(pxgridCellContainer);
			}
			return pxgridCellContainer;
		}

		// Token: 0x17000BFB RID: 3067
		// (get) Token: 0x06002273 RID: 8819 RVA: 0x0008FF79 File Offset: 0x0008E179
		// (set) Token: 0x06002274 RID: 8820 RVA: 0x0008FF81 File Offset: 0x0008E181
		public string Language { get; set; }

		// Token: 0x17000BFC RID: 3068
		// (get) Token: 0x06002275 RID: 8821 RVA: 0x0008FF8A File Offset: 0x0008E18A
		[NotifyParentProperty(true)]
		[DefaultValue(null)]
		[Category("Appearance")]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Description("The style that is used to render cells of the column.")]
		[Browsable(false)]
		public PXCellStyle Style
		{
			get
			{
				if (this.style == null)
				{
					this.style = new PXCellStyle(this.isTrackingViewState);
				}
				return this.style;
			}
		}

		// Token: 0x17000BFD RID: 3069
		// (get) Token: 0x06002276 RID: 8822 RVA: 0x0008FFAB File Offset: 0x0008E1AB
		[NotifyParentProperty(true)]
		[DefaultValue(null)]
		[Category("Appearance")]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Description("The style that is used to render cell buttons of the column.")]
		[Browsable(false)]
		public PXStyle CellButtonStyle
		{
			get
			{
				if (this.cellButtonStyle == null)
				{
					this.cellButtonStyle = new PXStyle(this.isTrackingViewState);
				}
				return this.cellButtonStyle;
			}
		}

		// Token: 0x06002277 RID: 8823 RVA: 0x0008FFCC File Offset: 0x0008E1CC
		public override string ToString()
		{
			if (this.Header.Text != string.Empty)
			{
				return this.Header.Text;
			}
			if (!string.IsNullOrEmpty(this.Label))
			{
				return this.Label;
			}
			if (this.DataField != string.Empty)
			{
				return this.DataField;
			}
			return "Column [" + this.Type.ToString() + "]";
		}

		// Token: 0x17000BFE RID: 3070
		// (get) Token: 0x06002278 RID: 8824 RVA: 0x0009004C File Offset: 0x0008E24C
		// (set) Token: 0x06002279 RID: 8825 RVA: 0x00090054 File Offset: 0x0008E254
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public PXGridColumnCollection Collection
		{
			get
			{
				return this.collection;
			}
			internal set
			{
				this.collection = value;
			}
		}

		// Token: 0x0600227A RID: 8826 RVA: 0x00090060 File Offset: 0x0008E260
		protected internal object ResolveDefaultValue()
		{
			object obj = this.DefaultValue;
			if (!this.AllowNull)
			{
				if (this.DataType == TypeCode.DateTime)
				{
					if (obj == null)
					{
						obj = DateTime.Today;
					}
				}
				else if (TypeHelper.IsNumeric(this.DataType) && obj == null)
				{
					obj = 0;
				}
			}
			return obj;
		}

		// Token: 0x0600227B RID: 8827 RVA: 0x000900AE File Offset: 0x0008E2AE
		protected internal string GetKey()
		{
			if (this.DataField.Length <= 0)
			{
				return this.Key;
			}
			return this.DataField;
		}

		// Token: 0x0600227C RID: 8828 RVA: 0x000900CC File Offset: 0x0008E2CC
		internal bool GetMatrixMode()
		{
			if (this.MatrixMode == null)
			{
				return this.Level.Grid.MatrixMode;
			}
			return this.MatrixMode.Value;
		}

		// Token: 0x0600227D RID: 8829 RVA: 0x00090108 File Offset: 0x0008E308
		private string GetValueItemText(PXValueItemCollection vic, object value, TypeCode dataType, PXValueItemCollection extra = null)
		{
			int i = 0;
			while (i < vic.Count)
			{
				object obj = Convert.ChangeType(vic[i].Value, dataType, CultureInfo.InvariantCulture);
				if (obj != null && value.Equals(obj))
				{
					if (!string.IsNullOrEmpty(vic[i].DisplayValue))
					{
						return vic[i].DisplayValue;
					}
					return vic[i].Value;
				}
				else
				{
					i++;
				}
			}
			int num = 0;
			while (extra != null && num < extra.Count)
			{
				object obj2 = Convert.ChangeType(extra[num].Value, dataType, CultureInfo.InvariantCulture);
				if (obj2 != null && value.Equals(obj2))
				{
					if (!string.IsNullOrEmpty(extra[num].DisplayValue))
					{
						return extra[num].DisplayValue;
					}
					return extra[num].Value;
				}
				else
				{
					num++;
				}
			}
			return null;
		}

		// Token: 0x0600227E RID: 8830 RVA: 0x000901E3 File Offset: 0x0008E3E3
		private string Decode(string str)
		{
			if (str != null)
			{
				return str.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&amp;", "&");
			}
			return str;
		}

		// Token: 0x0600227F RID: 8831 RVA: 0x00090218 File Offset: 0x0008E418
		protected internal string GetValueText(object value, bool invariant)
		{
			if (value is PXFieldState)
			{
				value = ((PXFieldState)value).Value;
			}
			if (value == null || value.Equals(DBNull.Value))
			{
				return this.NullTextFinal;
			}
			string text;
			if (value is DateTime && value != null)
			{
				DateTime dateTime = (DateTime)value;
				if (dateTime.Hour == 0 && dateTime.Minute == 0 && dateTime.Second == 0 && dateTime.Millisecond == 0)
				{
					text = dateTime.ToString("MM/dd/yyyy", DateTimeFormatInfo.InvariantInfo);
				}
				else
				{
					text = dateTime.ToString("MM/dd/yyyy HH:mm:ss.fff", DateTimeFormatInfo.InvariantInfo);
				}
			}
			else
			{
				text = Convert.ToString(value, invariant ? CultureInfo.InvariantCulture : CultureInfo.CurrentCulture);
				if (this.DataType == TypeCode.String)
				{
					text = text.TrimEnd(Array.Empty<char>());
				}
			}
			if (this.Type == GridColumnType.Icon && value is string && !this.ValueItems.HasItems())
			{
				text = PXImages.ResolveImageUrl(text, this.Level.Grid);
			}
			if (this.HtmlEncode)
			{
				text = HttpUtility.HtmlEncode(text);
			}
			if (this.HtmlDecode)
			{
				text = this.Decode(text);
			}
			return text;
		}

		// Token: 0x06002280 RID: 8832 RVA: 0x00090334 File Offset: 0x0008E534
		protected internal string GetMarginText(bool footer)
		{
			string text = footer ? this.Footer.Text : this.Header.Text;
			string value = footer ? string.Empty : this.Header.ImageUrl;
			if (!footer && string.IsNullOrEmpty(text) && HttpContext.Current == null)
			{
				text = this.DataField;
			}
			if (text.Length > 0)
			{
				text = text.TrimEnd(Array.Empty<char>());
			}
			if (text == string.Empty)
			{
				if (string.IsNullOrEmpty(value))
				{
					if (!footer)
					{
						text = this.GetKey();
					}
					if (string.IsNullOrEmpty(text))
					{
						text = "&nbsp;";
					}
				}
			}
			else
			{
				if (this.HtmlEncode)
				{
					text = HttpUtility.HtmlEncode(text);
				}
				if (this.HtmlDecode)
				{
					text = this.Decode(text);
				}
			}
			return text;
		}

		// Token: 0x06002281 RID: 8833 RVA: 0x000903F0 File Offset: 0x0008E5F0
		protected internal string GetMarginToolTip(bool footer)
		{
			string text = footer ? this.Footer.ToolTip : this.Header.ToolTip;
			if (text.Length > 0)
			{
				text = text.TrimEnd(Array.Empty<char>());
			}
			if (this.HtmlEncode)
			{
				text = HttpUtility.HtmlEncode(text);
			}
			if (this.HtmlDecode)
			{
				text = this.Decode(text);
			}
			return text;
		}

		// Token: 0x06002282 RID: 8834 RVA: 0x00090450 File Offset: 0x0008E650
		protected internal string FormatValue(object value, PXGridCell cell)
		{
			if (value == null || value.Equals(DBNull.Value))
			{
				return this.NullText;
			}
			TypeCode dataType = this.DataType;
			bool flag = this.IsPassword;
			bool flag2 = this.TimeMode;
			string displayFormat = this.DisplayFormat;
			PXValueItems pxvalueItems = this.ValueItems;
			if (cell != null)
			{
				if (cell.DataType != TypeCode.Empty)
				{
					dataType = cell.DataType;
				}
				if (!string.IsNullOrEmpty(cell.DisplayFormat))
				{
					displayFormat = cell.DisplayFormat;
				}
				if (cell.IsPassword != null)
				{
					flag = cell.IsPassword.Value;
				}
				if (cell.TimeMode != null)
				{
					flag2 = cell.TimeMode.Value;
				}
				if (cell.ValueItems.Items.Count > 0 || (this.GetMatrixMode() && cell.DataType != TypeCode.Empty && cell.DataType != this.DataType))
				{
					pxvalueItems = cell.ValueItems;
				}
			}
			string text = string.Empty;
			CultureInfo invariantCulture = CultureInfo.InvariantCulture;
			bool flag3 = true;
			IFieldEditor fieldEditor = null;
			if (this.RenderEditorText && this.Type != GridColumnType.CheckBox)
			{
				fieldEditor = this.Level.GetFieldEditor(this.DataField);
			}
			if (this.Type == GridColumnType.Icon)
			{
				if (!pxvalueItems.HasItems())
				{
					text = Convert.ToString(value, CultureInfo.CurrentCulture);
					if (value is string)
					{
						text = PXImages.ResolveImageUrl(text, this.Level.Grid);
					}
				}
				else
				{
					text = this.GetValueItemText(pxvalueItems.Items, value, dataType, null);
				}
				return text;
			}
			if (this.RenderEditorText && fieldEditor != null)
			{
				try
				{
					text = fieldEditor.GetRenderText(value);
				}
				catch
				{
				}
				flag3 = false;
			}
			else if (this.Type == GridColumnType.DropDownList || pxvalueItems.Items.Count > 0)
			{
				PXValueItemCollection items = pxvalueItems.Items;
				flag3 = false;
				if (pxvalueItems.MultiSelect)
				{
					string[] array = Convert.ToString(value, CultureInfo.CurrentCulture).Split(new char[]
					{
						','
					});
					List<string> list = new List<string>();
					foreach (string text2 in array)
					{
						text = this.GetValueItemText(items, text2, dataType, (cell != null) ? cell.ExtraItems : null);
						list.Add(text ?? text2);
					}
					text = string.Join(", ", list.ToArray());
				}
				else
				{
					text = this.GetValueItemText(items, value, dataType, (cell != null) ? cell.ExtraItems : null);
				}
				if (text == null)
				{
					int defaultItem = pxvalueItems.DefaultItem;
					if (!pxvalueItems.Exclusive)
					{
						text = Convert.ToString(value, CultureInfo.CurrentCulture);
					}
					else if (defaultItem >= 0 && items.Count > defaultItem)
					{
						text = items[defaultItem].DisplayValue;
					}
				}
				if (text == null && cell != null)
				{
					text = Convert.ToString(value, CultureInfo.CurrentCulture);
				}
				if (text == null)
				{
					text = string.Empty;
				}
			}
			else
			{
				if (flag && dataType == TypeCode.String)
				{
					return "*******";
				}
				if (displayFormat != string.Empty)
				{
					flag3 = false;
					if (dataType == TypeCode.String)
					{
						StringBuilder stringBuilder;
						if (displayFormat == this.DisplayFormat)
						{
							if (this.encodedMask == null)
							{
								this.encodedMask = Mask.EncodeMask(displayFormat);
							}
							stringBuilder = this.encodedMask;
						}
						else
						{
							stringBuilder = Mask.EncodeMask(displayFormat);
						}
						text = Mask.Format(stringBuilder, value.ToString(), ' ');
					}
					else
					{
						text = string.Format("{0:" + displayFormat + "}", value);
					}
				}
				else if (dataType == TypeCode.DateTime)
				{
					text = string.Format(flag2 ? "{0:t}" : "{0:d}", value);
				}
				else
				{
					text = Convert.ToString(value, CultureInfo.CurrentCulture);
				}
			}
			if (text != string.Empty && dataType == TypeCode.String)
			{
				TextCase textCase = this.TextCase;
				if (textCase != TextCase.Upper)
				{
					if (textCase == TextCase.Lower)
					{
						text = text.ToLower();
					}
				}
				else
				{
					text = text.ToUpper();
				}
				if (this.MaxLength > 0 && flag3)
				{
					text = text.Substring(0, Math.Min(this.MaxLength, text.Length));
				}
				text = text.TrimEnd(Array.Empty<char>());
				if (this.HtmlEncode)
				{
					text = HttpUtility.HtmlEncode(text);
				}
				if (this.HtmlDecode)
				{
					text = this.Decode(text);
				}
			}
			return text;
		}

		// Token: 0x06002283 RID: 8835 RVA: 0x00090874 File Offset: 0x0008EA74
		protected internal string FormatValue(object value)
		{
			return this.FormatValue(value, null);
		}

		// Token: 0x06002284 RID: 8836 RVA: 0x00090880 File Offset: 0x0008EA80
		protected internal object GetValueFromText(string strValue)
		{
			object val = null;
			if (this.AllowNull && strValue == this.NullTextFinal)
			{
				val = null;
			}
			else
			{
				try
				{
					if (this.DataType != TypeCode.Boolean || !string.IsNullOrEmpty(strValue))
					{
						Guid guid;
						if (this.DataType == TypeCode.Object && Guid.TryParse(strValue, out guid))
						{
							val = guid;
						}
						else if (this.DataType == TypeCode.Object || this.DataType == TypeCode.String)
						{
							val = strValue;
						}
						else if (!string.IsNullOrEmpty(strValue))
						{
							val = TypeHelper.TryParse(strValue, this.DataType, CultureInfo.InvariantCulture);
						}
					}
				}
				catch (Exception)
				{
				}
			}
			return this.NormalizeValue(val);
		}

		// Token: 0x06002285 RID: 8837 RVA: 0x00090928 File Offset: 0x0008EB28
		internal string GetDisplayText(object value, object text)
		{
			string text2 = string.Empty;
			ValueDisplayMode displayMode = this.DisplayMode;
			if (displayMode != ValueDisplayMode.Text)
			{
				if (displayMode == ValueDisplayMode.Hint)
				{
					if (value == null || string.IsNullOrEmpty(value.ToString()))
					{
						return Convert.ToString(text);
					}
					if (text == null || string.IsNullOrWhiteSpace(text.ToString()))
					{
						return Convert.ToString(value);
					}
					text2 = string.Format("{0} - {1}", value, text);
				}
				else
				{
					text2 = Convert.ToString(text);
				}
			}
			else
			{
				text2 = Convert.ToString(text);
			}
			return text2.TrimEnd(Array.Empty<char>());
		}

		// Token: 0x06002286 RID: 8838 RVA: 0x000909A4 File Offset: 0x0008EBA4
		protected internal object NormalizeValue(object val)
		{
			PXFieldState pxfieldState = val as PXFieldState;
			if (pxfieldState != null)
			{
				val = pxfieldState.Value;
			}
			if (this.DataType == TypeCode.String && val != null)
			{
				val = val.ToInvariantString().TrimEnd(Array.Empty<char>());
			}
			return val;
		}

		// Token: 0x17000BFF RID: 3071
		// (get) Token: 0x06002287 RID: 8839 RVA: 0x000909E3 File Offset: 0x0008EBE3
		internal virtual string TextFieldColumn
		{
			get
			{
				if (this.DisplayMode == ValueDisplayMode.Value)
				{
					return this.TextField;
				}
				if (!this.DataField.EndsWith("_description", StringComparison.OrdinalIgnoreCase))
				{
					return this.DataField + "_description";
				}
				return this.DataField;
			}
		}

		// Token: 0x17000C00 RID: 3072
		// (get) Token: 0x06002288 RID: 8840 RVA: 0x00090A20 File Offset: 0x0008EC20
		protected override IList<IWebObject> StateObjects
		{
			get
			{
				if (this.stateObjects == null)
				{
					this.stateObjects = new List<IWebObject>();
					this.stateObjects.AddRange(new IWebObject[]
					{
						this.Style,
						this.CellButtonStyle,
						this.Header,
						this.Footer,
						this.ValueItems
					});
				}
				return this.stateObjects;
			}
		}

		// Token: 0x17000C01 RID: 3073
		// (get) Token: 0x06002289 RID: 8841 RVA: 0x00090A84 File Offset: 0x0008EC84
		// (set) Token: 0x0600228A RID: 8842 RVA: 0x00090A8C File Offset: 0x0008EC8C
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

		// Token: 0x04000991 RID: 2449
		private string viewName = string.Empty;

		// Token: 0x04000997 RID: 2455
		public bool SyncText = true;

		// Token: 0x04000998 RID: 2456
		public bool DynamicValueItems;

		// Token: 0x04000999 RID: 2457
		internal bool HasError;

		// Token: 0x0400099A RID: 2458
		internal bool IsStateColumn;

		// Token: 0x0400099C RID: 2460
		internal PXTableCell HeaderCell;

		// Token: 0x0400099D RID: 2461
		internal PXTableCell HeaderCellStat;

		// Token: 0x0400099E RID: 2462
		internal PXTableCell FooterCell;

		// Token: 0x0400099F RID: 2463
		internal bool FilterPosted;

		// Token: 0x040009A0 RID: 2464
		internal bool VisiblePosted;

		// Token: 0x040009A1 RID: 2465
		internal bool VisibleLoaded;

		// Token: 0x040009A2 RID: 2466
		internal bool VisibleSynchronized;

		// Token: 0x040009A3 RID: 2467
		internal bool Generated;

		// Token: 0x040009A4 RID: 2468
		internal bool FormatGenerated;

		// Token: 0x040009A5 RID: 2469
		private const TypeCode _defDataType = TypeCode.String;

		// Token: 0x040009A6 RID: 2470
		private const GridColumnType _defType = GridColumnType.NotSet;

		// Token: 0x040009A7 RID: 2471
		private const ValueDisplayMode _defDisplayMode = ValueDisplayMode.Value;

		// Token: 0x040009A8 RID: 2472
		private const TextCase _defCase = TextCase.NotSet;

		// Token: 0x040009A9 RID: 2473
		private const SortDirection _defSort = SortDirection.None;

		// Token: 0x040009AA RID: 2474
		private const HorizontalAlign _defAlign = HorizontalAlign.NotSet;

		// Token: 0x040009AB RID: 2475
		private const GridButtonDisplay _defButDisplay = GridButtonDisplay.MouseOver;

		// Token: 0x040009AC RID: 2476
		private const int _defDecimals = 0;

		// Token: 0x040009AD RID: 2477
		private const int _defDecimalsForDecimal = 2;

		// Token: 0x040009AE RID: 2478
		private const AllowShowHide _defShowHide = AllowShowHide.True;

		// Token: 0x040009AF RID: 2479
		private static readonly Unit _defWidth = Unit.Pixel(70);

		// Token: 0x040009B0 RID: 2480
		private List<IWebObject> stateObjects;

		// Token: 0x040009B1 RID: 2481
		private PXGridColumnCollection collection;

		// Token: 0x040009B2 RID: 2482
		private PXCellStyle style;

		// Token: 0x040009B3 RID: 2483
		private PXStyle cellButtonStyle;

		// Token: 0x040009B4 RID: 2484
		private PXGridHeader header;

		// Token: 0x040009B5 RID: 2485
		private PXGridFooter footer;

		// Token: 0x040009B6 RID: 2486
		private PXValueItems valueItems;

		// Token: 0x040009B7 RID: 2487
		private StringBuilder encodedMask;

		// Token: 0x040009B8 RID: 2488
		private PXParamCollection navigateParams;

		// Token: 0x040009B9 RID: 2489
		private PXParamCollection additionalParams;

		// Token: 0x040009BA RID: 2490
		private PXMaskItemCollection maskItems;

		// Token: 0x040009BB RID: 2491
		private ITemplate cellTemplate;

		// Token: 0x040009BC RID: 2492
		private PxFieldStateProcessor _StateProcessinig;
	}
}
