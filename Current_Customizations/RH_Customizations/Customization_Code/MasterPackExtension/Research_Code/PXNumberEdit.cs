using System;
using System.ComponentModel;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using PX.Data;

namespace PX.Web.UI
{
	// Token: 0x020000DF RID: 223
	[ToolboxData("<{0}:PXNumberEdit runat=server></{0}:PXNumberEdit>")]
	[PXDesignerPropertyFilter("AutoCallBack\r\nDataField\r\nTabIndex\r\nValueType\r\nWidth", "DisplayFormat\r\nEnabled\r\nLabelID")]
	public class PXNumberEdit : PXTextEdit
	{
		// Token: 0x170008AA RID: 2218
		// (get) Token: 0x060018E3 RID: 6371 RVA: 0x00061A3B File Offset: 0x0005FC3B
		// (set) Token: 0x060018E4 RID: 6372 RVA: 0x00061A4C File Offset: 0x0005FC4C
		[TypeConverter(typeof(DecimalConverter))]
		[Browsable(false)]
		public override object Value
		{
			get
			{
				return this.GetTypedValue(this.Text, true);
			}
			set
			{
				object typedValue = this.GetTypedValue(value, false);
				if (typedValue == null || this.CheckRange(Convert.ToDouble(typedValue)))
				{
					if (typedValue == null || (!this.AllowNull && Convert.ToDouble(typedValue) == 0.0))
					{
						this.Text = string.Empty;
					}
					else
					{
						this.Text = Convert.ToString(typedValue, CultureInfo.InvariantCulture);
					}
					ControlHelper.SerializeProp(this, new string[]
					{
						"Text"
					});
				}
			}
		}

		// Token: 0x170008AB RID: 2219
		// (get) Token: 0x060018E5 RID: 6373 RVA: 0x00061AC1 File Offset: 0x0005FCC1
		// (set) Token: 0x060018E6 RID: 6374 RVA: 0x00061AC9 File Offset: 0x0005FCC9
		[Browsable(false)]
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

		// Token: 0x170008AC RID: 2220
		// (get) Token: 0x060018E7 RID: 6375 RVA: 0x00061AD2 File Offset: 0x0005FCD2
		[Browsable(false)]
		public override PXAutoSizeInfo AutoSize
		{
			get
			{
				return base.AutoSize;
			}
		}

		// Token: 0x170008AD RID: 2221
		// (get) Token: 0x060018E8 RID: 6376 RVA: 0x00061ADA File Offset: 0x0005FCDA
		// (set) Token: 0x060018E9 RID: 6377 RVA: 0x00061AEE File Offset: 0x0005FCEE
		[DefaultValue(TypeCode.Int32)]
		[RefreshProperties(RefreshProperties.Repaint)]
		[Browsable(false)]
		[Category("Base Property")]
		[Description("The value type of the control.")]
		[ScriptBrowsable]
		public TypeCode ValueType
		{
			get
			{
				return STM.GetProp<TypeCode>(this.ViewState, "ValueType", TypeCode.Int32);
			}
			set
			{
				if (this.CanEditType(value))
				{
					STM.SetProp<TypeCode>(this.ViewState, "ValueType", value, TypeCode.Int32);
				}
			}
		}

		// Token: 0x170008AE RID: 2222
		// (get) Token: 0x060018EA RID: 6378 RVA: 0x00061B0C File Offset: 0x0005FD0C
		// (set) Token: 0x060018EB RID: 6379 RVA: 0x00061B3A File Offset: 0x0005FD3A
		[DefaultValue(0)]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		[Category("Data")]
		[Description("Number of decimal places (precision).")]
		[Browsable(false)]
		public int Decimals
		{
			get
			{
				TypeCode valueType = this.ValueType;
				if (valueType - TypeCode.Single <= 2)
				{
					return STM.GetProp<int>(this.ViewState, "Decimals", 0);
				}
				return 0;
			}
			set
			{
				STM.SetProp<int>(this.ViewState, "Decimals", value, 0);
			}
		}

		// Token: 0x170008AF RID: 2223
		// (get) Token: 0x060018EC RID: 6380 RVA: 0x00061B4E File Offset: 0x0005FD4E
		// (set) Token: 0x060018ED RID: 6381 RVA: 0x00061B6C File Offset: 0x0005FD6C
		[DefaultValue(-9007199254740991.0)]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		[Category("Data")]
		[Description("The minimal value that can be entered.")]
		[Browsable(false)]
		public double MinValue
		{
			get
			{
				return STM.GetProp<double>(this.ViewState, "MinValue", -9007199254740991.0);
			}
			set
			{
				STM.SetProp<double>(this.ViewState, "MinValue", value, -9007199254740991.0);
				object value2 = this.Value;
				if (value2 != null && Convert.ToDouble(value2) < value)
				{
					this.Value = value;
				}
			}
		}

		// Token: 0x170008B0 RID: 2224
		// (get) Token: 0x060018EE RID: 6382 RVA: 0x00061BB2 File Offset: 0x0005FDB2
		// (set) Token: 0x060018EF RID: 6383 RVA: 0x00061BD0 File Offset: 0x0005FDD0
		[DefaultValue(9007199254740991.0)]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		[Category("Data")]
		[Description("The maximal value that can be entered.")]
		[Browsable(false)]
		public double MaxValue
		{
			get
			{
				return STM.GetProp<double>(this.ViewState, "MaxValue", 9007199254740991.0);
			}
			set
			{
				STM.SetProp<double>(this.ViewState, "MaxValue", value, 9007199254740991.0);
				object value2 = this.Value;
				if (value2 != null && Convert.ToDouble(value2) > value)
				{
					this.Value = value;
				}
			}
		}

		// Token: 0x170008B1 RID: 2225
		// (get) Token: 0x060018F0 RID: 6384 RVA: 0x00061C16 File Offset: 0x0005FE16
		// (set) Token: 0x060018F1 RID: 6385 RVA: 0x00061C2D File Offset: 0x0005FE2D
		[Category("Data")]
		[DefaultValue("")]
		[ScriptBrowsable]
		[Browsable(false)]
		[Description("The format of the date that is used for the edit mode when the control has input focus.")]
		public virtual string EditFormat
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "EditFormat", string.Empty);
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "EditFormat", value, string.Empty);
			}
		}

		// Token: 0x170008B2 RID: 2226
		// (get) Token: 0x060018F2 RID: 6386 RVA: 0x00061C45 File Offset: 0x0005FE45
		// (set) Token: 0x060018F3 RID: 6387 RVA: 0x00061C5C File Offset: 0x0005FE5C
		[Category("Ext. Property")]
		[DefaultValue("")]
		[ScriptBrowsable]
		[Description("The format of the date that is used for the display mode when the control has no input focus.")]
		public virtual string DisplayFormat
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "DisplayFormat", string.Empty);
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "DisplayFormat", value, string.Empty);
			}
		}

		// Token: 0x170008B3 RID: 2227
		// (get) Token: 0x060018F4 RID: 6388 RVA: 0x00061C74 File Offset: 0x0005FE74
		// (set) Token: 0x060018F5 RID: 6389 RVA: 0x00061C87 File Offset: 0x0005FE87
		[DefaultValue(false)]
		public override bool AllowNull
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "AllowNull", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "AllowNull", value, false);
			}
		}

		// Token: 0x170008B4 RID: 2228
		// (get) Token: 0x060018F6 RID: 6390 RVA: 0x00061C9B File Offset: 0x0005FE9B
		// (set) Token: 0x060018F7 RID: 6391 RVA: 0x00061CB6 File Offset: 0x0005FEB6
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public NumberFormatInfo NumberFormat
		{
			get
			{
				if (this.numberFormat == null)
				{
					return CultureInfo.CurrentCulture.NumberFormat;
				}
				return this.numberFormat;
			}
			set
			{
				this.numberFormat = value;
			}
		}

		// Token: 0x170008B5 RID: 2229
		// (get) Token: 0x060018F8 RID: 6392 RVA: 0x00061CBF File Offset: 0x0005FEBF
		// (set) Token: 0x060018F9 RID: 6393 RVA: 0x00061CD2 File Offset: 0x0005FED2
		[DefaultValue(HorizontalAlign.Right)]
		[Browsable(false)]
		public override HorizontalAlign TextAlign
		{
			get
			{
				return STM.GetProp<HorizontalAlign>(this.ViewState, "TextAlign", HorizontalAlign.Right);
			}
			set
			{
				STM.SetProp<HorizontalAlign>(this.ViewState, "TextAlign", value, HorizontalAlign.Right);
			}
		}

		// Token: 0x170008B6 RID: 2230
		// (get) Token: 0x060018FA RID: 6394 RVA: 0x00061CE6 File Offset: 0x0005FEE6
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override TextBoxMode TextMode
		{
			get
			{
				return TextBoxMode.SingleLine;
			}
		}

		// Token: 0x170008B7 RID: 2231
		// (get) Token: 0x060018FB RID: 6395 RVA: 0x00061CE9 File Offset: 0x0005FEE9
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override int Rows
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x170008B8 RID: 2232
		// (get) Token: 0x060018FC RID: 6396 RVA: 0x00061CEC File Offset: 0x0005FEEC
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override int Columns
		{
			get
			{
				return 0;
			}
		}

		// Token: 0x170008B9 RID: 2233
		// (get) Token: 0x060018FD RID: 6397 RVA: 0x00061CEF File Offset: 0x0005FEEF
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override bool Wrap
		{
			get
			{
				return false;
			}
		}

		// Token: 0x170008BA RID: 2234
		// (get) Token: 0x060018FE RID: 6398 RVA: 0x00061CF2 File Offset: 0x0005FEF2
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override string ValidateExp
		{
			get
			{
				return string.Empty;
			}
		}

		// Token: 0x060018FF RID: 6399 RVA: 0x00061CF9 File Offset: 0x0005FEF9
		public override bool CanEditType(TypeCode type)
		{
			return type - TypeCode.SByte <= 10;
		}

		// Token: 0x06001900 RID: 6400 RVA: 0x00061D08 File Offset: 0x0005FF08
		public override string GetRenderText(object value)
		{
			if (this.AllowNull && value == null)
			{
				return this.NullText;
			}
			NumberFormatInfo provider = this.GetNumberFormat(false);
			TypeCode valueType = this.ValueType;
			string text;
			if (valueType - TypeCode.Single <= 2)
			{
				text = ((!string.IsNullOrEmpty(this.DisplayFormat)) ? this.DisplayFormat : ("N" + this.Decimals.ToString()));
				decimal d = Convert.ToDecimal(value);
				if (text.Contains("%"))
				{
					d /= 100m;
				}
				return d.ToString(text, provider);
			}
			text = ((!string.IsNullOrEmpty(this.DisplayFormat)) ? this.DisplayFormat : "D");
			string result;
			try
			{
				long num = Convert.ToInt64(value);
				if (text.Contains("%"))
				{
					num /= 100L;
				}
				result = num.ToString(text, provider);
			}
			catch
			{
				result = "0";
			}
			return result;
		}

		// Token: 0x06001901 RID: 6401 RVA: 0x00061DFC File Offset: 0x0005FFFC
		protected override object GetPostValue(string t)
		{
			return this.GetTypedValue(t, false);
		}

		// Token: 0x06001902 RID: 6402 RVA: 0x00061E08 File Offset: 0x00060008
		public override void SynchronizeState(PXFieldState state)
		{
			PXIntState pxintState = state as PXIntState;
			PXDecimalState pxdecimalState = state as PXDecimalState;
			PXDoubleState pxdoubleState = state as PXDoubleState;
			if (pxintState != null)
			{
				this.MinValue = (double)pxintState.MinValue;
				this.MaxValue = (double)pxintState.MaxValue;
			}
			else if (pxdecimalState != null)
			{
				this.MinValue = (double)pxdecimalState.MinValue;
				this.MaxValue = (double)pxdecimalState.MaxValue;
			}
			else if (pxdoubleState != null)
			{
				this.MinValue = pxdoubleState.MinValue;
				this.MaxValue = pxdoubleState.MaxValue;
			}
			this.ValueType = Type.GetTypeCode(state.DataType);
			this.Decimals = ((state.Precision > 0) ? state.Precision : 0);
			base.SynchronizeState(state);
		}

		// Token: 0x170008BB RID: 2235
		// (get) Token: 0x06001903 RID: 6403 RVA: 0x00061EBD File Offset: 0x000600BD
		public override string ClientTagKey
		{
			get
			{
				return "qp-number-editor";
			}
		}

		// Token: 0x06001904 RID: 6404 RVA: 0x00061EC4 File Offset: 0x000600C4
		protected override void RegisterScriptModules(JSManager sm)
		{
			base.RegisterScriptModules(sm);
			sm.RegisterModule(JS.NetTypeKey, "PX.Web.UI.Scripts.px_netType.js");
			sm.RegisterModule(typeof(PXNumberEdit), "PX.Web.UI.Scripts.px_numbEdit.js");
		}

		// Token: 0x06001905 RID: 6405 RVA: 0x00061EF4 File Offset: 0x000600F4
		private object GetTypedValue(object val, bool invariant)
		{
			if (!this.IsNullValue(val))
			{
				int typeCode = (int)Convert.GetTypeCode(val);
				object value = val;
				if (typeCode == 18)
				{
					NumberFormatInfo numberFormatInfo = this.GetNumberFormat(invariant > false);
					string s = val.ToString().Replace(numberFormatInfo.PercentSymbol, string.Empty);
					NumberStyles style = NumberStyles.Any;
					if (this.Decimals > 15)
					{
						decimal d;
						if (!decimal.TryParse(s, style, numberFormatInfo, out d))
						{
							d = decimal.Parse(s, style, this.GetNumberFormat(!invariant));
						}
						value = decimal.Round(d, this.Decimals);
					}
					else
					{
						double value2;
						if (!double.TryParse(s, style, numberFormatInfo, out value2))
						{
							value2 = double.Parse(s, style, this.GetNumberFormat(!invariant));
						}
						value = Math.Round(value2, this.Decimals);
					}
				}
				return Convert.ChangeType(value, this.ValueType);
			}
			if (this.AllowNull)
			{
				return null;
			}
			return Convert.ChangeType(0, this.ValueType);
		}

		// Token: 0x06001906 RID: 6406 RVA: 0x00061FDB File Offset: 0x000601DB
		private bool CheckRange(double val)
		{
			return val >= this.MinValue && val <= this.MaxValue;
		}

		// Token: 0x06001907 RID: 6407 RVA: 0x00061FF4 File Offset: 0x000601F4
		private NumberFormatInfo GetNumberFormat(bool invariant)
		{
			if (!invariant)
			{
				return this.NumberFormat;
			}
			return NumberFormatInfo.InvariantInfo;
		}

		// Token: 0x04000686 RID: 1670
		private NumberFormatInfo numberFormat;

		// Token: 0x04000687 RID: 1671
		private const int _defDecimals = 0;

		// Token: 0x04000688 RID: 1672
		private const double _defMinValue = -9007199254740991.0;

		// Token: 0x04000689 RID: 1673
		private const double _defMaxValue = 9007199254740991.0;

		// Token: 0x0400068A RID: 1674
		private const HorizontalAlign _defTextAlign = HorizontalAlign.Right;

		// Token: 0x0400068B RID: 1675
		private const TypeCode _defType = TypeCode.Int32;
	}
}
