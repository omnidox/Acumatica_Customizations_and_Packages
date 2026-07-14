using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Web.UI;
using System.Web.UI.Design;
using System.Web.UI.WebControls;
using PX.Data;

namespace PX.Web.UI
{
	// Token: 0x0200012F RID: 303
	public class PXGridLevel : PXWebObject<PXGridLevel>
	{
		// Token: 0x0600231F RID: 8991 RVA: 0x00091DA7 File Offset: 0x0008FFA7
		public PXGridLevel()
		{
		}

		// Token: 0x06002320 RID: 8992 RVA: 0x00091DBA File Offset: 0x0008FFBA
		internal PXGridLevel(bool isTrack) : base(isTrack)
		{
		}

		// Token: 0x17000C3C RID: 3132
		// (get) Token: 0x06002321 RID: 8993 RVA: 0x00091DCE File Offset: 0x0008FFCE
		// (set) Token: 0x06002322 RID: 8994 RVA: 0x00091DE0 File Offset: 0x0008FFE0
		[NotifyParentProperty(true)]
		[Category("Data")]
		[DefaultValue("")]
		[TypeConverter(typeof(DataMemberConverter))]
		[Description("The table or view that is used for importing.")]
		public string ImportDataMember
		{
			get
			{
				return base.GetProp<string>("ImportDataMember", string.Empty);
			}
			set
			{
				if (value != this.ImportDataMember)
				{
					base.SetProp<string>("ImportDataMember", value, string.Empty);
				}
			}
		}

		// Token: 0x17000C3D RID: 3133
		// (get) Token: 0x06002323 RID: 8995 RVA: 0x00091E01 File Offset: 0x00090001
		// (set) Token: 0x06002324 RID: 8996 RVA: 0x00091E13 File Offset: 0x00090013
		[NotifyParentProperty(true)]
		[Category("Data")]
		[DefaultValue("")]
		[TypeConverter(typeof(DataMemberConverter))]
		[Description("The table or view that is used for binding against.")]
		public string DataMember
		{
			get
			{
				return base.GetProp<string>("DataMember", string.Empty);
			}
			set
			{
				if (value != this.DataMember)
				{
					base.SetProp<string>("DataMember", value, string.Empty);
					this.currentViewValid = false;
				}
			}
		}

		// Token: 0x17000C3E RID: 3134
		// (get) Token: 0x06002325 RID: 8997 RVA: 0x00091E3C File Offset: 0x0009003C
		// (set) Token: 0x06002326 RID: 8998 RVA: 0x00091E9C File Offset: 0x0009009C
		[NotifyParentProperty(true)]
		[Category("Data")]
		[DefaultValue(null)]
		[TypeConverter(typeof(StringArrayConverter))]
		[Editor("System.Web.UI.Design.WebControls.DataFieldEditor", typeof(UITypeEditor))]
		[Description("A comma-separated list of key fields in the data source.")]
		public string[] DataKeyNames
		{
			get
			{
				if (this.dataKeyNames == null)
				{
					PXDataSourceView pxdataSourceView = this.GetData() as PXDataSourceView;
					try
					{
						if (pxdataSourceView != null)
						{
							this.dataKeyNames = (pxdataSourceView.GetKeyNames() ?? new string[0]);
						}
					}
					catch (Exception)
					{
					}
				}
				return this.dataKeyNames ?? new string[0];
			}
			set
			{
			}
		}

		// Token: 0x06002327 RID: 8999 RVA: 0x00091E9E File Offset: 0x0009009E
		internal void SetDataKeyNames(params string[] kn)
		{
			this.dataKeyNames = kn;
		}

		// Token: 0x17000C3F RID: 3135
		// (get) Token: 0x06002328 RID: 9000 RVA: 0x00091EA7 File Offset: 0x000900A7
		// (set) Token: 0x06002329 RID: 9001 RVA: 0x00091EB5 File Offset: 0x000900B5
		[NotifyParentProperty(true)]
		[Category("Behavior")]
		[DefaultValue(true)]
		[Description("Indicates whether the level is visible.")]
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

		// Token: 0x17000C40 RID: 3136
		// (get) Token: 0x0600232A RID: 9002 RVA: 0x00091EC4 File Offset: 0x000900C4
		// (set) Token: 0x0600232B RID: 9003 RVA: 0x00091ED6 File Offset: 0x000900D6
		[NotifyParentProperty(true)]
		[Category("Data")]
		[DefaultValue("")]
		[Description("The string that is used to access the item in the GridLevels collection.")]
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

		// Token: 0x17000C41 RID: 3137
		// (get) Token: 0x0600232C RID: 9004 RVA: 0x00091EE9 File Offset: 0x000900E9
		// (set) Token: 0x0600232D RID: 9005 RVA: 0x00091EFB File Offset: 0x000900FB
		[NotifyParentProperty(true)]
		[Category("Data")]
		[DefaultValue("")]
		[Description("The system sort order for the level.")]
		public string SortOrder
		{
			get
			{
				return base.GetProp<string>("SortOrder", string.Empty);
			}
			set
			{
				base.SetProp<string>("SortOrder", value, string.Empty);
			}
		}

		// Token: 0x17000C42 RID: 3138
		// (get) Token: 0x0600232E RID: 9006 RVA: 0x00091F0E File Offset: 0x0009010E
		// (set) Token: 0x0600232F RID: 9007 RVA: 0x00091F16 File Offset: 0x00090116
		[Browsable(false)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[TemplateContainer(typeof(PXGrid))]
		[TemplateInstance(TemplateInstance.Single)]
		public ITemplate RowTemplate
		{
			get
			{
				return this.rowTemplate;
			}
			set
			{
				this.rowTemplate = value;
			}
		}

		// Token: 0x17000C43 RID: 3139
		// (get) Token: 0x06002330 RID: 9008 RVA: 0x00091F20 File Offset: 0x00090120
		[Browsable(false)]
		public WebControl RowTemplateContainer
		{
			get
			{
				if (this.rowTemplateContainer == null)
				{
					if (this.Index == 0)
					{
						this.rowTemplateContainer = new PXLayoutGenerator(this.Grid, this.Grid.ContentLayout, true);
						if (this.rowTemplate != null)
						{
							this.rowTemplate.InstantiateIn(this.rowTemplateContainer);
						}
					}
					else
					{
						this.rowTemplateContainer = new WebControl(HtmlTextWriterTag.Div);
						if (this.rowTemplate != null)
						{
							this.rowTemplate.InstantiateIn(this.rowTemplateContainer);
						}
					}
				}
				return this.rowTemplateContainer;
			}
		}

		// Token: 0x17000C44 RID: 3140
		// (get) Token: 0x06002331 RID: 9009 RVA: 0x00091FA1 File Offset: 0x000901A1
		[Browsable(false)]
		public Dictionary<string, WebControl> TemplateEditors
		{
			get
			{
				if (this.templateEditors == null)
				{
					this.templateEditors = new Dictionary<string, WebControl>();
					this.FindTemplateEditors(this.RowTemplateContainer);
				}
				return this.templateEditors;
			}
		}

		// Token: 0x06002332 RID: 9010 RVA: 0x00091FC8 File Offset: 0x000901C8
		public void InvalidateTemplateEditors()
		{
			this.templateEditors = null;
		}

		// Token: 0x17000C45 RID: 3141
		// (get) Token: 0x06002333 RID: 9011 RVA: 0x00091FD1 File Offset: 0x000901D1
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

		// Token: 0x17000C46 RID: 3142
		// (get) Token: 0x06002334 RID: 9012 RVA: 0x00091FE9 File Offset: 0x000901E9
		[Browsable(false)]
		public PXGrid Grid
		{
			get
			{
				if (this.collection != null)
				{
					return this.collection.Grid;
				}
				return null;
			}
		}

		// Token: 0x17000C47 RID: 3143
		// (get) Token: 0x06002335 RID: 9013 RVA: 0x00092000 File Offset: 0x00090200
		// (set) Token: 0x06002336 RID: 9014 RVA: 0x00092027 File Offset: 0x00090227
		[NotifyParentProperty(true)]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Data")]
		[Description("The collection of the level columns.")]
		public PXGridColumnCollection Columns
		{
			get
			{
				if (this.columns == null)
				{
					this.columns = new PXGridColumnCollection();
					this.columns.Level = this;
				}
				return this.columns;
			}
			internal set
			{
				this.columns = value;
				this.columns.Level = this;
			}
		}

		// Token: 0x06002337 RID: 9015 RVA: 0x0009203C File Offset: 0x0009023C
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected bool ShouldSerializeColumns()
		{
			return this.columns != null && this.columns.Count > 0;
		}

		// Token: 0x06002338 RID: 9016 RVA: 0x00092056 File Offset: 0x00090256
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void ResetColumns()
		{
			if (this.columns != null)
			{
				this.columns.Clear();
			}
		}

		// Token: 0x17000C48 RID: 3144
		// (get) Token: 0x06002339 RID: 9017 RVA: 0x0009206B File Offset: 0x0009026B
		[NotifyParentProperty(true)]
		[Category("Behavior")]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Description("The properties of the level mode.")]
		public PXGridLevelMode Mode
		{
			get
			{
				if (this.mode == null)
				{
					this.mode = new PXGridLevelMode(this.isTrackingViewState);
					this.mode.ResetDefaultValues();
				}
				return this.mode;
			}
		}

		// Token: 0x0600233A RID: 9018 RVA: 0x00092097 File Offset: 0x00090297
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected bool ShouldSerializeMode()
		{
			return STM.HasData(this.mode);
		}

		// Token: 0x0600233B RID: 9019 RVA: 0x000920A4 File Offset: 0x000902A4
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void ResetMode()
		{
			if (this.mode != null)
			{
				this.mode.Reset();
			}
		}

		// Token: 0x17000C49 RID: 3145
		// (get) Token: 0x0600233C RID: 9020 RVA: 0x000920B9 File Offset: 0x000902B9
		[Browsable(false)]
		public PXGridLevelMode ModeFinal
		{
			get
			{
				if (this.modeFinal == null)
				{
					this.ResolveLevelPropeties();
				}
				return this.modeFinal;
			}
		}

		// Token: 0x17000C4A RID: 3146
		// (get) Token: 0x0600233D RID: 9021 RVA: 0x000920D0 File Offset: 0x000902D0
		[NotifyParentProperty(true)]
		[Category("Appearance")]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Description("The properties of the level appearance.")]
		public PXGridLevelLayout Layout
		{
			get
			{
				if (this.layout == null)
				{
					this.layout = new PXGridLevelLayout(this.isTrackingViewState, this.Grid);
					this.layout.ResetDefaultValues();
				}
				if (this.layout.Owner == null)
				{
					this.layout.Owner = this.Grid;
				}
				return this.layout;
			}
		}

		// Token: 0x0600233E RID: 9022 RVA: 0x0009212B File Offset: 0x0009032B
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected bool ShouldSerializeLayout()
		{
			return STM.HasData(this.layout);
		}

		// Token: 0x0600233F RID: 9023 RVA: 0x00092138 File Offset: 0x00090338
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void ResetLayout()
		{
			if (this.layout != null)
			{
				this.layout.Reset();
			}
		}

		// Token: 0x17000C4B RID: 3147
		// (get) Token: 0x06002340 RID: 9024 RVA: 0x0009214D File Offset: 0x0009034D
		[Browsable(false)]
		public PXGridLevelLayout LayoutFinal
		{
			get
			{
				if (this.layoutFinal == null)
				{
					this.ResolveLevelPropeties();
				}
				return this.layoutFinal;
			}
		}

		// Token: 0x17000C4C RID: 3148
		// (get) Token: 0x06002341 RID: 9025 RVA: 0x00092163 File Offset: 0x00090363
		[NotifyParentProperty(true)]
		[Category("Appearance")]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Description("The properties of the level images.")]
		public PXGridLevelImages Images
		{
			get
			{
				if (this.images == null)
				{
					this.images = new PXGridLevelImages(this.isTrackingViewState);
				}
				return this.images;
			}
		}

		// Token: 0x06002342 RID: 9026 RVA: 0x00092184 File Offset: 0x00090384
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected bool ShouldSerializeImages()
		{
			return STM.HasData(this.images);
		}

		// Token: 0x06002343 RID: 9027 RVA: 0x00092191 File Offset: 0x00090391
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void ResetImages()
		{
			if (this.images != null)
			{
				this.images.Reset();
			}
		}

		// Token: 0x17000C4D RID: 3149
		// (get) Token: 0x06002344 RID: 9028 RVA: 0x000921A6 File Offset: 0x000903A6
		[Browsable(false)]
		public PXGridLevelImages ImagesFinal
		{
			get
			{
				if (this.imagesFinal == null)
				{
					this.ResolveLevelPropeties();
				}
				return this.imagesFinal;
			}
		}

		// Token: 0x17000C4E RID: 3150
		// (get) Token: 0x06002345 RID: 9029 RVA: 0x000921BC File Offset: 0x000903BC
		[NotifyParentProperty(true)]
		[Category("Appearance")]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Description("The properties of the level styles.")]
		public PXGridLevelStyles Styles
		{
			get
			{
				if (this.styles == null)
				{
					this.styles = new PXGridLevelStyles(this.isTrackingViewState);
				}
				return this.styles;
			}
		}

		// Token: 0x06002346 RID: 9030 RVA: 0x000921DD File Offset: 0x000903DD
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected bool ShouldSerializeStyles()
		{
			return STM.HasData(this.styles);
		}

		// Token: 0x06002347 RID: 9031 RVA: 0x000921EA File Offset: 0x000903EA
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void ResetStyles()
		{
			if (this.styles != null)
			{
				this.styles.Reset();
			}
		}

		// Token: 0x06002348 RID: 9032 RVA: 0x00092200 File Offset: 0x00090400
		public override string ToString()
		{
			if (this.Key != string.Empty)
			{
				return this.Key;
			}
			return "Level [" + this.Index.ToString() + "]";
		}

		// Token: 0x17000C4F RID: 3151
		// (get) Token: 0x06002349 RID: 9033 RVA: 0x00092243 File Offset: 0x00090443
		// (set) Token: 0x0600234A RID: 9034 RVA: 0x0009224B File Offset: 0x0009044B
		protected internal PXGridLevelCollection Collection
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

		// Token: 0x17000C50 RID: 3152
		// (get) Token: 0x0600234B RID: 9035 RVA: 0x00092254 File Offset: 0x00090454
		protected internal bool NeedRenderForm
		{
			get
			{
				PXGrid grid = this.Grid;
				if (PXGraph.ProxyIsActive || PXGraph.GeneratorIsActive || grid.RenderDefaultEditors || grid.MatrixMode)
				{
					return true;
				}
				if (this.Index == 0 && grid.AllowSearch)
				{
					return true;
				}
				PXGridLevelMode pxgridLevelMode = this.ModeFinal;
				if (pxgridLevelMode.AllowUpdate.GetValueOrDefault() || pxgridLevelMode.AllowAddNew.GetValueOrDefault() || pxgridLevelMode.AllowFormEdit.GetValueOrDefault())
				{
					return true;
				}
				foreach (object obj in this.Columns)
				{
					PXGridColumn pxgridColumn = (PXGridColumn)obj;
					if (pxgridColumn.RenderEditorText && !string.IsNullOrEmpty(pxgridColumn.FormEditorID))
					{
						return true;
					}
				}
				return false;
			}
		}

		// Token: 0x17000C51 RID: 3153
		// (get) Token: 0x0600234C RID: 9036 RVA: 0x0009233C File Offset: 0x0009053C
		protected internal bool NeedRowTemplate
		{
			get
			{
				return this.ModeFinal.AllowAddNew.GetValueOrDefault() || this.Grid.RenderDefaultEditors || (this.Index == 0 && this.Grid.AdjustPageSize > GridPageSizeMode.None);
			}
		}

		// Token: 0x17000C52 RID: 3154
		// (get) Token: 0x0600234D RID: 9037 RVA: 0x00092385 File Offset: 0x00090585
		private bool IsBoundUsingDataSourceID
		{
			get
			{
				return this.Grid != null && !string.IsNullOrEmpty(this.Grid.DataSourceID);
			}
		}

		// Token: 0x0600234E RID: 9038 RVA: 0x000923A4 File Offset: 0x000905A4
		protected internal DataSourceView GetData()
		{
			if (this.Grid == null && string.IsNullOrEmpty(this.Grid.DataSourceID))
			{
				return null;
			}
			if (this.Index == 0)
			{
				this.currentView = this.Grid.GetDataInternal();
			}
			else if (!this.currentViewValid || (this.Grid.Site != null && this.Grid.Site.DesignMode))
			{
				DataSourceView view = this.Grid.GetDataSourceInternal().GetView(this.DataMember);
				if (view == null)
				{
					throw new InvalidOperationException(Msg.GetLocal("The view, which the PXGrid control '{0}' has requested, cannot be found. Check that the DataMember property is valid.", new object[]
					{
						this.Grid.ID
					}));
				}
				this.currentView = view;
				this.currentViewValid = true;
			}
			return this.currentView;
		}

		// Token: 0x0600234F RID: 9039 RVA: 0x00092464 File Offset: 0x00090664
		protected internal bool GetUploadEnabled()
		{
			PXGrid grid = this.Grid;
			PXGraph pxgraph = (grid != null) ? grid.DataGraph : null;
			string text = (!string.IsNullOrEmpty(this.ImportDataMember)) ? this.ImportDataMember : this.DataMember;
			return pxgraph != null && !string.IsNullOrEmpty(text) && PXImportAttribute.GetEnabled(pxgraph, text);
		}

		// Token: 0x06002350 RID: 9040 RVA: 0x000924B4 File Offset: 0x000906B4
		protected internal bool GetAllowAddNew()
		{
			PXGridLevelMode pxgridLevelMode = this.ModeFinal;
			if (this.IsBoundUsingDataSourceID)
			{
				DataSourceView data = this.GetData();
				return pxgridLevelMode.AllowAddNew.GetValueOrDefault() && (data == null || data.CanInsert);
			}
			return pxgridLevelMode.AllowAddNew.GetValueOrDefault();
		}

		// Token: 0x06002351 RID: 9041 RVA: 0x00092504 File Offset: 0x00090704
		protected internal bool GetAllowDelete()
		{
			PXGridLevelMode pxgridLevelMode = this.ModeFinal;
			if (this.IsBoundUsingDataSourceID)
			{
				DataSourceView data = this.GetData();
				return pxgridLevelMode.AllowDelete.GetValueOrDefault() && (data == null || data.CanDelete);
			}
			return pxgridLevelMode.AllowDelete.GetValueOrDefault();
		}

		// Token: 0x06002352 RID: 9042 RVA: 0x00092554 File Offset: 0x00090754
		protected internal bool GetAllowUpdate()
		{
			PXGridLevelMode pxgridLevelMode = this.ModeFinal;
			if (this.IsBoundUsingDataSourceID)
			{
				DataSourceView data = this.GetData();
				return pxgridLevelMode.AllowUpdate.GetValueOrDefault() && (data == null || data.CanUpdate);
			}
			return pxgridLevelMode.AllowUpdate.GetValueOrDefault();
		}

		// Token: 0x06002353 RID: 9043 RVA: 0x000925A4 File Offset: 0x000907A4
		protected internal bool GetAllowSort()
		{
			PXGridLevelMode pxgridLevelMode = this.ModeFinal;
			if (this.IsBoundUsingDataSourceID)
			{
				DataSourceView data = this.GetData();
				return pxgridLevelMode.AllowSort.GetValueOrDefault() && (data == null || data.CanSort);
			}
			return pxgridLevelMode.AllowSort.GetValueOrDefault();
		}

		// Token: 0x06002354 RID: 9044 RVA: 0x000925F4 File Offset: 0x000907F4
		protected internal void ResolveLevelPropeties()
		{
			if (this.imagesFinal == null)
			{
				this.imagesFinal = new PXGridLevelImages();
				this.modeFinal = new PXGridLevelMode();
				this.layoutFinal = new PXGridLevelLayout(this.Grid);
			}
			this.imagesFinal.Reset();
			this.imagesFinal.CopyFrom(this.Images);
			this.imagesFinal.MergeWith(this.Grid.Images);
			this.imagesFinal.ResolveUrl(this.Grid);
			this.modeFinal.Reset();
			this.modeFinal.CopyFrom(this.Mode);
			this.modeFinal.MergeWith(this.Grid.Mode);
			if (this.Index == 0 && string.IsNullOrEmpty(this.Mode.AutoInsertField))
			{
				this.modeFinal.AutoInsertField = this.Grid.Mode.AutoInsertField;
			}
			this.layoutFinal.Reset();
			this.layoutFinal.CopyFrom(this.Layout);
			this.layoutFinal.MergeWith(this.Grid.Layout);
		}

		// Token: 0x06002355 RID: 9045 RVA: 0x0009270C File Offset: 0x0009090C
		protected internal string GetSortExpression()
		{
			List<string> list = string.IsNullOrEmpty(this.SortOrder) ? new List<string>() : this.SortOrder.Split(new char[]
			{
				','
			}).ToList<string>();
			int num = 0;
			foreach (object obj in this.Columns)
			{
				PXGridColumn pxgridColumn = (PXGridColumn)obj;
				if (pxgridColumn.SortDirection != SortDirection.None)
				{
					string text = (!string.IsNullOrEmpty(pxgridColumn.SortField)) ? pxgridColumn.SortField : ((!string.IsNullOrEmpty(pxgridColumn.TextField)) ? pxgridColumn.TextFieldColumn : pxgridColumn.DataField);
					if (pxgridColumn.SortDirection == SortDirection.Descending)
					{
						text += " desc";
					}
					list.Insert(num++, text);
				}
			}
			return string.Join(",", list.ToArray());
		}

		// Token: 0x06002356 RID: 9046 RVA: 0x00092804 File Offset: 0x00090A04
		protected internal IFieldEditor GetFieldEditor(string dataField)
		{
			Dictionary<string, WebControl> dictionary = this.TemplateEditors;
			dataField = dataField.ToLowerInvariant();
			if (!dictionary.ContainsKey(dataField))
			{
				return null;
			}
			return dictionary[dataField] as IFieldEditor;
		}

		// Token: 0x06002357 RID: 9047 RVA: 0x00092837 File Offset: 0x00090A37
		public WebControl GetStandardEditor(GridStandardEditor type)
		{
			switch (type)
			{
			case GridStandardEditor.Date:
				return this.dateEditor;
			case GridStandardEditor.DropDown:
				return this.dropEditor;
			case GridStandardEditor.Selector:
				return this.selEditor;
			case GridStandardEditor.SegmentMask:
				return this.segMaskEditor;
			default:
				return null;
			}
		}

		// Token: 0x06002358 RID: 9048 RVA: 0x00092870 File Offset: 0x00090A70
		private void FindTemplateEditors(Control parent)
		{
			foreach (object obj in parent.Controls)
			{
				Control control = (Control)obj;
				IFieldEditor fieldEditor = control as IFieldEditor;
				if (fieldEditor != null)
				{
					string key = (fieldEditor.DataField.Length > 0) ? fieldEditor.DataField.ToLowerInvariant() : fieldEditor.ID;
					if (!this.templateEditors.ContainsKey(key))
					{
						this.templateEditors.Add(key, (WebControl)fieldEditor);
					}
				}
				else
				{
					bool flag = control is PXPanel || control is Panel || control is PXGroupBox;
					if ((control.HasControls() || flag) && !(control is PXGrid))
					{
						this.FindTemplateEditors(control);
					}
				}
			}
		}

		// Token: 0x06002359 RID: 9049 RVA: 0x00092954 File Offset: 0x00090B54
		internal void CreateTemplateEditor(GridStandardEditor type)
		{
			WebControl webControl = null;
			switch (type)
			{
			case GridStandardEditor.Date:
				if (this.dateEditor == null)
				{
					webControl = (this.dateEditor = new PXDateTimeEdit());
					webControl.ID = "ed";
				}
				break;
			case GridStandardEditor.DropDown:
				if (this.dropEditor == null)
				{
					webControl = (this.dropEditor = new PXDropDown());
					webControl.ID = "ec";
				}
				break;
			case GridStandardEditor.Selector:
				if (this.selEditor == null)
				{
					webControl = (this.selEditor = new PXSelector());
					((PXSelector)webControl).DataSourceID = this.Grid.DataSourceID;
					((PXSelector)webControl).CallBackMode.PostData = PostDataMode.Container;
					((PXSelector)webControl).AutoGenerateColumns = true;
					webControl.ID = "es";
				}
				break;
			case GridStandardEditor.SegmentMask:
				if (this.segMaskEditor == null)
				{
					webControl = (this.segMaskEditor = new PXSegmentMask());
					((PXSegmentMask)webControl).DataSourceID = this.Grid.DataSourceID;
					((PXSegmentMask)webControl).CallBackMode.PostData = PostDataMode.Container;
					((PXSegmentMask)webControl).AutoGenerateColumns = true;
					((PXSegmentMask)webControl).SelectMode = MaskSelectMode.Segment;
					webControl.ID = "em";
				}
				break;
			}
			if (webControl != null)
			{
				this.AppendFieldEditor(webControl);
			}
		}

		// Token: 0x0600235A RID: 9050 RVA: 0x00092A90 File Offset: 0x00090C90
		internal void AppendFieldEditor(WebControl editor)
		{
			if (editor != null && !this.TemplateEditors.ContainsKey(editor.ID))
			{
				((IPXScriptControl)editor).RegisterFlags = (ScriptRegisterFlag)14;
				IFieldEditor fieldEditor = editor as IFieldEditor;
				string key = (fieldEditor != null && !string.IsNullOrEmpty(fieldEditor.DataField)) ? fieldEditor.DataField.ToLowerInvariant() : editor.ID;
				this.TemplateEditors.Add(key, editor);
				this.RowTemplateContainer.Controls.Add(editor);
				editor.Style[HtmlTextWriterStyle.Visibility] = "hidden";
				editor.ApplyStyleSheetSkin(this.Grid.Page);
			}
		}

		// Token: 0x17000C53 RID: 3155
		// (get) Token: 0x0600235B RID: 9051 RVA: 0x00092B30 File Offset: 0x00090D30
		protected override IList<IWebObject> StateObjects
		{
			get
			{
				if (this.stateObjects == null)
				{
					this.stateObjects = new List<IWebObject>();
				}
				this.stateObjects.Clear();
				this.stateObjects.AddRange(new IWebObject[]
				{
					this.Mode,
					this.Layout,
					this.Images,
					this.Styles,
					this.Columns
				});
				return this.stateObjects;
			}
		}

		// Token: 0x0600235C RID: 9052 RVA: 0x00092B9F File Offset: 0x00090D9F
		protected internal object SaveControlState()
		{
			return new object[]
			{
				this.dataKeyNames
			};
		}

		// Token: 0x0600235D RID: 9053 RVA: 0x00092BB0 File Offset: 0x00090DB0
		protected internal void LoadControlState(object savedState)
		{
			object[] array = (object[])savedState;
			if (array[0] != null)
			{
				this.dataKeyNames = (string[])array[0];
			}
		}

		// Token: 0x040009E7 RID: 2535
		internal PXTableCell HeaderCorner;

		// Token: 0x040009E8 RID: 2536
		internal PXTableCell HeaderCornerStat;

		// Token: 0x040009E9 RID: 2537
		internal PXTableCell FooterCorner;

		// Token: 0x040009EA RID: 2538
		internal PXButton FormOkButton;

		// Token: 0x040009EB RID: 2539
		internal PXButton FormCancelButton;

		// Token: 0x040009EC RID: 2540
		internal PXTableCell FooterCell;

		// Token: 0x040009ED RID: 2541
		internal PXTableCell HeaderCell;

		// Token: 0x040009EE RID: 2542
		internal PXFormView FormView;

		// Token: 0x040009EF RID: 2543
		internal PXTable EditForm;

		// Token: 0x040009F0 RID: 2544
		internal List<PXButton> FormButtons = new List<PXButton>();

		// Token: 0x040009F1 RID: 2545
		private const string _dateEditorID = "ed";

		// Token: 0x040009F2 RID: 2546
		private const string _dropEditorID = "ec";

		// Token: 0x040009F3 RID: 2547
		private const string _selEditorID = "es";

		// Token: 0x040009F4 RID: 2548
		private const string _segMaskEditorID = "em";

		// Token: 0x040009F5 RID: 2549
		private List<IWebObject> stateObjects;

		// Token: 0x040009F6 RID: 2550
		private PXGridLevelMode mode;

		// Token: 0x040009F7 RID: 2551
		private PXGridLevelImages images;

		// Token: 0x040009F8 RID: 2552
		private PXGridLevelLayout layout;

		// Token: 0x040009F9 RID: 2553
		private PXGridLevelMode modeFinal;

		// Token: 0x040009FA RID: 2554
		private PXGridLevelImages imagesFinal;

		// Token: 0x040009FB RID: 2555
		private PXGridLevelLayout layoutFinal;

		// Token: 0x040009FC RID: 2556
		private PXGridLevelStyles styles;

		// Token: 0x040009FD RID: 2557
		private PXGridLevelCollection collection;

		// Token: 0x040009FE RID: 2558
		private PXGridColumnCollection columns;

		// Token: 0x040009FF RID: 2559
		private ITemplate rowTemplate;

		// Token: 0x04000A00 RID: 2560
		private WebControl rowTemplateContainer;

		// Token: 0x04000A01 RID: 2561
		private Dictionary<string, WebControl> templateEditors;

		// Token: 0x04000A02 RID: 2562
		private WebControl dateEditor;

		// Token: 0x04000A03 RID: 2563
		private WebControl dropEditor;

		// Token: 0x04000A04 RID: 2564
		private WebControl selEditor;

		// Token: 0x04000A05 RID: 2565
		private WebControl segMaskEditor;

		// Token: 0x04000A06 RID: 2566
		private string[] dataKeyNames;

		// Token: 0x04000A07 RID: 2567
		private bool currentViewValid;

		// Token: 0x04000A08 RID: 2568
		private DataSourceView currentView;
	}
}
