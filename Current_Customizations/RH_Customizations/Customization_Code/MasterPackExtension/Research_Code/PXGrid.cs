using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using System.Web.UI.Design;
using System.Web.UI.WebControls;
using System.Xml;
using PX.Common;
using PX.Common.Extensions;
using PX.Data;
using PX.Olap.Maintenance;
using PX.SM;
using PX.Translation;
using PX.UI.Common.Grid.Export;
using PX.Web.UI.Tools;

namespace PX.Web.UI
{
	// Token: 0x0200010E RID: 270
	[DefaultProperty("Layout")]
	[Designer("PX.Web.UI.Design.PXGridDesigner")]
	[ToolboxData("<{0}:PXGrid Width=400px Height=400px runat=server></{0}:PXGrid>")]
	public class PXGrid : CompositeDataBoundControl, IPXDataControl, IPXDataBoundControl, IPXPagedControl, ICommandSource, IPXGraphAccessor, ICSSProvider, IUrlResolutionService, IPXScriptControl, IPostBackDataHandler, IPostBackEventHandler, IPXDynamicControl, IPXCallbackHandler, ICallbackEventHandler, IPXCallbackUpdatable, IPXDataBindableControl, IControlsContainer, IAutoSizedControl, IAutoHideControl
	{
		// Token: 0x06001E68 RID: 7784 RVA: 0x000769A0 File Offset: 0x00074BA0
		public PXGrid()
		{
			this.errorState = new KeyValuePair<ErrorState, string>(ErrorState.None, string.Empty);
			this.internalFilterRows = new List<PXFilterRow>();
		}

		// Token: 0x06001E69 RID: 7785 RVA: 0x00076A08 File Offset: 0x00074C08
		static PXGrid()
		{
			PXGrid.RowDataBoundEvent = new object();
			PXGrid.InitRowEvent = new object();
			PXGrid.DeletedEvent = new object();
			PXGrid.DeletingEvent = new object();
			PXGrid.InsertingEvent = new object();
			PXGrid.InsertedEvent = new object();
			PXGrid.UpdatingEvent = new object();
			PXGrid.UpdatedEvent = new object();
			PXGrid.PageIndexChangingEvent = new object();
			PXGrid.PageIndexChangedEvent = new object();
			PXGrid.CommandEvent = new object();
			PXGrid.RefetchRowEvent = new object();
			PXGrid.SelectEvent = new object();
			PXGrid.SyncCellStateEvent = new object();
			PXGrid.CommitChangesEvent = new object();
			PXGrid.NoteShowEvent = new object();
			PXGrid.NoteSaveEvent = new object();
			PXGrid.CallBackEvent = new object();
			PXGrid.ButtonClickEvent = new object();
			PXGrid.FilesMenuShowEvent = new object();
			PXGrid.FileSaveEvent = new object();
			PXGrid.LayoutSaveEvent = new object();
			PXGrid.LayoutResetEvent = new object();
			PXGrid.LayoutLoadEvent = new object();
			PXGrid.BeforeSyncStateEvent = new object();
			PXGrid.AfterSyncStateEvent = new object();
			PXGrid.ColumnsGeneratedEvent = new object();
			PXGrid.BeforeNavigateEvent = new object();
			PXGrid.BeforeGenerateColumnsEvent = new object();
			PXGrid.BeforeExportToExcelEvent = new object();
		}

		// Token: 0x1400009F RID: 159
		// (add) Token: 0x06001E6A RID: 7786 RVA: 0x00076B74 File Offset: 0x00074D74
		// (remove) Token: 0x06001E6B RID: 7787 RVA: 0x00076B87 File Offset: 0x00074D87
		[Category("Data")]
		[Description("Occurs before data is selected from the data source.")]
		public event PXSelectEventHandler Select
		{
			add
			{
				base.Events.AddHandler(PXGrid.SelectEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.SelectEvent, value);
			}
		}

		// Token: 0x140000A0 RID: 160
		// (add) Token: 0x06001E6C RID: 7788 RVA: 0x00076B9A File Offset: 0x00074D9A
		// (remove) Token: 0x06001E6D RID: 7789 RVA: 0x00076BAD File Offset: 0x00074DAD
		[Category("Data")]
		[Description("Occurs after the grid columns have been generated.")]
		public event EventHandler ColumnsGenerated
		{
			add
			{
				base.Events.AddHandler(PXGrid.ColumnsGeneratedEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.ColumnsGeneratedEvent, value);
			}
		}

		// Token: 0x140000A1 RID: 161
		// (add) Token: 0x06001E6E RID: 7790 RVA: 0x00076BC0 File Offset: 0x00074DC0
		// (remove) Token: 0x06001E6F RID: 7791 RVA: 0x00076BF8 File Offset: 0x00074DF8
		[Category("Action")]
		[Description("Occurs after the grid's edit controls have been created.")]
		public event EventHandler EditorsCreated;

		// Token: 0x140000A2 RID: 162
		// (add) Token: 0x06001E70 RID: 7792 RVA: 0x00076C2D File Offset: 0x00074E2D
		// (remove) Token: 0x06001E71 RID: 7793 RVA: 0x00076C40 File Offset: 0x00074E40
		[Category("Data")]
		[Description("Occurs on the cell state synchronization.")]
		public event PXSyncCellStateEventHandler SyncCellState
		{
			add
			{
				base.Events.AddHandler(PXGrid.SyncCellStateEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.SyncCellStateEvent, value);
			}
		}

		// Token: 0x140000A3 RID: 163
		// (add) Token: 0x06001E72 RID: 7794 RVA: 0x00076C53 File Offset: 0x00074E53
		// (remove) Token: 0x06001E73 RID: 7795 RVA: 0x00076C66 File Offset: 0x00074E66
		[Category("Action")]
		[Description("Occurs before the command execution.")]
		public event PXCommandEventHandler Command
		{
			add
			{
				base.Events.AddHandler(PXGrid.CommandEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.CommandEvent, value);
			}
		}

		// Token: 0x140000A4 RID: 164
		// (add) Token: 0x06001E74 RID: 7796 RVA: 0x00076C79 File Offset: 0x00074E79
		// (remove) Token: 0x06001E75 RID: 7797 RVA: 0x00076C8C File Offset: 0x00074E8C
		[Category("Action")]
		[Description("Occurs when the grid action bar receives a server callback.")]
		public event PXCallBackEventHandler CallBack
		{
			add
			{
				base.Events.AddHandler(PXGrid.CallBackEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.CallBackEvent, value);
			}
		}

		// Token: 0x140000A5 RID: 165
		// (add) Token: 0x06001E76 RID: 7798 RVA: 0x00076C9F File Offset: 0x00074E9F
		// (remove) Token: 0x06001E77 RID: 7799 RVA: 0x00076CB2 File Offset: 0x00074EB2
		[Category("Action")]
		[Description("Occurs when the grid action button is clicked.")]
		public event PXToolBarClickEventHandler ButtonClick
		{
			add
			{
				base.Events.AddHandler(PXGrid.ButtonClickEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.ButtonClickEvent, value);
			}
		}

		// Token: 0x140000A6 RID: 166
		// (add) Token: 0x06001E78 RID: 7800 RVA: 0x00076CC5 File Offset: 0x00074EC5
		// (remove) Token: 0x06001E79 RID: 7801 RVA: 0x00076CD8 File Offset: 0x00074ED8
		[Category("Action")]
		[Description("Occurs after the grid commit row has been changed.")]
		public event EventHandler CommitChanges
		{
			add
			{
				base.Events.AddHandler(PXGrid.CommitChangesEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.CommitChangesEvent, value);
			}
		}

		// Token: 0x140000A7 RID: 167
		// (add) Token: 0x06001E7A RID: 7802 RVA: 0x00076CEB File Offset: 0x00074EEB
		// (remove) Token: 0x06001E7B RID: 7803 RVA: 0x00076CFE File Offset: 0x00074EFE
		[Category("Data")]
		[Description("Occurs after a data item has been bound to the grid row.")]
		public event PXGridRowEventHandler RowDataBound
		{
			add
			{
				base.Events.AddHandler(PXGrid.RowDataBoundEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.RowDataBoundEvent, value);
			}
		}

		// Token: 0x140000A8 RID: 168
		// (add) Token: 0x06001E7C RID: 7804 RVA: 0x00076D11 File Offset: 0x00074F11
		// (remove) Token: 0x06001E7D RID: 7805 RVA: 0x00076D24 File Offset: 0x00074F24
		[Category("Data")]
		[Description("Occurs when a row needs its values to be initialized during callback.")]
		public event PXDBInsertEventHandler InitRow
		{
			add
			{
				base.Events.AddHandler(PXGrid.InitRowEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.InitRowEvent, value);
			}
		}

		// Token: 0x140000A9 RID: 169
		// (add) Token: 0x06001E7E RID: 7806 RVA: 0x00076D37 File Offset: 0x00074F37
		// (remove) Token: 0x06001E7F RID: 7807 RVA: 0x00076D4A File Offset: 0x00074F4A
		[Category("Data")]
		[Description("Occurs when the values of a row needs to be refetched during callback.")]
		public event PXDBUpdateEventHandler RefetchRow
		{
			add
			{
				base.Events.AddHandler(PXGrid.RefetchRowEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.RefetchRowEvent, value);
			}
		}

		// Token: 0x140000AA RID: 170
		// (add) Token: 0x06001E80 RID: 7808 RVA: 0x00076D5D File Offset: 0x00074F5D
		// (remove) Token: 0x06001E81 RID: 7809 RVA: 0x00076D70 File Offset: 0x00074F70
		[Category("Data")]
		[Description("Occurs before the control synchronizes the columns state with the data source.")]
		public event EventHandler BeforeSyncState
		{
			add
			{
				base.Events.AddHandler(PXGrid.BeforeSyncStateEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.BeforeSyncStateEvent, value);
			}
		}

		// Token: 0x140000AB RID: 171
		// (add) Token: 0x06001E82 RID: 7810 RVA: 0x00076D83 File Offset: 0x00074F83
		// (remove) Token: 0x06001E83 RID: 7811 RVA: 0x00076D96 File Offset: 0x00074F96
		[Category("Data")]
		[Description("Occurs after the control has synchronized the columns state with the data source.")]
		public event EventHandler AfterSyncState
		{
			add
			{
				base.Events.AddHandler(PXGrid.AfterSyncStateEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.AfterSyncStateEvent, value);
			}
		}

		// Token: 0x140000AC RID: 172
		// (add) Token: 0x06001E84 RID: 7812 RVA: 0x00076DA9 File Offset: 0x00074FA9
		// (remove) Token: 0x06001E85 RID: 7813 RVA: 0x00076DBC File Offset: 0x00074FBC
		[Category("Data")]
		[Description("Occurs before a Delete command is executed on the data source.")]
		public event PXDBDeleteEventHandler RowDeleting
		{
			add
			{
				base.Events.AddHandler(PXGrid.DeletingEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.DeletingEvent, value);
			}
		}

		// Token: 0x140000AD RID: 173
		// (add) Token: 0x06001E86 RID: 7814 RVA: 0x00076DCF File Offset: 0x00074FCF
		// (remove) Token: 0x06001E87 RID: 7815 RVA: 0x00076DE2 File Offset: 0x00074FE2
		[Category("Data")]
		[Description("Occurs after a Delete command has been executed on the data source.")]
		public event PXDBDeletedEventHandler RowDeleted
		{
			add
			{
				base.Events.AddHandler(PXGrid.DeletedEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.DeletedEvent, value);
			}
		}

		// Token: 0x140000AE RID: 174
		// (add) Token: 0x06001E88 RID: 7816 RVA: 0x00076DF5 File Offset: 0x00074FF5
		// (remove) Token: 0x06001E89 RID: 7817 RVA: 0x00076E08 File Offset: 0x00075008
		[Category("Data")]
		[Description("Occurs before an Insert command is executed on the data source.")]
		public event PXDBInsertEventHandler RowInserting
		{
			add
			{
				base.Events.AddHandler(PXGrid.InsertingEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.InsertingEvent, value);
			}
		}

		// Token: 0x140000AF RID: 175
		// (add) Token: 0x06001E8A RID: 7818 RVA: 0x00076E1B File Offset: 0x0007501B
		// (remove) Token: 0x06001E8B RID: 7819 RVA: 0x00076E2E File Offset: 0x0007502E
		[Category("Data")]
		[Description("Occurs after an Insert command has been executed on the data source.")]
		public event PXDBInsertedEventHandler RowInserted
		{
			add
			{
				base.Events.AddHandler(PXGrid.InsertedEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.InsertedEvent, value);
			}
		}

		// Token: 0x140000B0 RID: 176
		// (add) Token: 0x06001E8C RID: 7820 RVA: 0x00076E41 File Offset: 0x00075041
		// (remove) Token: 0x06001E8D RID: 7821 RVA: 0x00076E54 File Offset: 0x00075054
		[Category("Data")]
		[Description("Occurs before an Update command has been executed on the data source.")]
		public event PXDBUpdateEventHandler RowUpdating
		{
			add
			{
				base.Events.AddHandler(PXGrid.UpdatingEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.UpdatingEvent, value);
			}
		}

		// Token: 0x140000B1 RID: 177
		// (add) Token: 0x06001E8E RID: 7822 RVA: 0x00076E67 File Offset: 0x00075067
		// (remove) Token: 0x06001E8F RID: 7823 RVA: 0x00076E7A File Offset: 0x0007507A
		[Category("Data")]
		[Description("Occurs after an Update command has been executed on the data source.")]
		public event PXDBUpdatedEventHandler RowUpdated
		{
			add
			{
				base.Events.AddHandler(PXGrid.UpdatedEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.UpdatedEvent, value);
			}
		}

		// Token: 0x140000B2 RID: 178
		// (add) Token: 0x06001E90 RID: 7824 RVA: 0x00076E8D File Offset: 0x0007508D
		// (remove) Token: 0x06001E91 RID: 7825 RVA: 0x00076EA0 File Offset: 0x000750A0
		[Category("Action")]
		[Description("Occurs when the page index of PXGrid is being changed.")]
		public event PXPageChangeEventHandler PageIndexChanging
		{
			add
			{
				base.Events.AddHandler(PXGrid.PageIndexChangingEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.PageIndexChangingEvent, value);
			}
		}

		// Token: 0x140000B3 RID: 179
		// (add) Token: 0x06001E92 RID: 7826 RVA: 0x00076EB3 File Offset: 0x000750B3
		// (remove) Token: 0x06001E93 RID: 7827 RVA: 0x00076EC6 File Offset: 0x000750C6
		[Category("Action")]
		[Description("Occurs when the page index of PXGrid has been changed.")]
		public event EventHandler PageIndexChanged
		{
			add
			{
				base.Events.AddHandler(PXGrid.PageIndexChangedEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.PageIndexChangedEvent, value);
			}
		}

		// Token: 0x140000B4 RID: 180
		// (add) Token: 0x06001E94 RID: 7828 RVA: 0x00076ED9 File Offset: 0x000750D9
		// (remove) Token: 0x06001E95 RID: 7829 RVA: 0x00076EEC File Offset: 0x000750EC
		[Category("Action")]
		[Description("Occurs when the control obtains the Note value.")]
		public event PXNoteEventHandler NoteShow
		{
			add
			{
				base.Events.AddHandler(PXGrid.NoteShowEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.NoteShowEvent, value);
			}
		}

		// Token: 0x140000B5 RID: 181
		// (add) Token: 0x06001E96 RID: 7830 RVA: 0x00076EFF File Offset: 0x000750FF
		// (remove) Token: 0x06001E97 RID: 7831 RVA: 0x00076F12 File Offset: 0x00075112
		[Category("Action")]
		[Description("Occurs when the control needs to save the Note value.")]
		public event PXNoteEventHandler NoteSave
		{
			add
			{
				base.Events.AddHandler(PXGrid.NoteSaveEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.NoteSaveEvent, value);
			}
		}

		// Token: 0x140000B6 RID: 182
		// (add) Token: 0x06001E98 RID: 7832 RVA: 0x00076F25 File Offset: 0x00075125
		// (remove) Token: 0x06001E99 RID: 7833 RVA: 0x00076F38 File Offset: 0x00075138
		[Category("Action")]
		[Description("Occurs when the control obtains the list of attached files.")]
		public event PXFilesMenuEventHandler FilesMenuShow
		{
			add
			{
				base.Events.AddHandler(PXGrid.FilesMenuShowEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.FilesMenuShowEvent, value);
			}
		}

		// Token: 0x140000B7 RID: 183
		// (add) Token: 0x06001E9A RID: 7834 RVA: 0x00076F4B File Offset: 0x0007514B
		// (remove) Token: 0x06001E9B RID: 7835 RVA: 0x00076F5E File Offset: 0x0007515E
		[Category("Action")]
		[Description("Occurs when the uploaded file needs to be saved to the database.")]
		public event PXFileUploadEventHandler FileSave
		{
			add
			{
				base.Events.AddHandler(PXGrid.FileSaveEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.FileSaveEvent, value);
			}
		}

		// Token: 0x140000B8 RID: 184
		// (add) Token: 0x06001E9C RID: 7836 RVA: 0x00076F71 File Offset: 0x00075171
		// (remove) Token: 0x06001E9D RID: 7837 RVA: 0x00076F84 File Offset: 0x00075184
		[Category("Action")]
		[Description("Occurs when the control saves the column layout.")]
		public event PXGridLayoutEventHandler LayoutSave
		{
			add
			{
				base.Events.AddHandler(PXGrid.LayoutSaveEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.LayoutSaveEvent, value);
			}
		}

		// Token: 0x140000B9 RID: 185
		// (add) Token: 0x06001E9E RID: 7838 RVA: 0x00076F97 File Offset: 0x00075197
		// (remove) Token: 0x06001E9F RID: 7839 RVA: 0x00076FAA File Offset: 0x000751AA
		[Category("Action")]
		[Description("Occurs when the control resets the column layout.")]
		public event PXGridLayoutEventHandler LayoutReset
		{
			add
			{
				base.Events.AddHandler(PXGrid.LayoutResetEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.LayoutResetEvent, value);
			}
		}

		// Token: 0x140000BA RID: 186
		// (add) Token: 0x06001EA0 RID: 7840 RVA: 0x00076FBD File Offset: 0x000751BD
		// (remove) Token: 0x06001EA1 RID: 7841 RVA: 0x00076FD0 File Offset: 0x000751D0
		[Category("Action")]
		[Description("Occurs when the control loads the column layout.")]
		public event PXGridLayoutEventHandler LayoutLoad
		{
			add
			{
				base.Events.AddHandler(PXGrid.LayoutLoadEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.LayoutLoadEvent, value);
			}
		}

		// Token: 0x140000BB RID: 187
		// (add) Token: 0x06001EA2 RID: 7842 RVA: 0x00076FE3 File Offset: 0x000751E3
		// (remove) Token: 0x06001EA3 RID: 7843 RVA: 0x00076FF6 File Offset: 0x000751F6
		[Category("Action")]
		[Description("Occurs before the navigation is performed.")]
		public event PXGridNavigateEventHandler BeforeNavigate
		{
			add
			{
				base.Events.AddHandler(PXGrid.BeforeNavigateEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.BeforeNavigateEvent, value);
			}
		}

		// Token: 0x140000BC RID: 188
		// (add) Token: 0x06001EA4 RID: 7844 RVA: 0x00077009 File Offset: 0x00075209
		// (remove) Token: 0x06001EA5 RID: 7845 RVA: 0x0007701C File Offset: 0x0007521C
		[Category("Action")]
		[Description("Occurs before the columns are generated.")]
		public event EventHandler BeforeGenerateColumns
		{
			add
			{
				base.Events.AddHandler(PXGrid.BeforeGenerateColumnsEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.BeforeGenerateColumnsEvent, value);
			}
		}

		// Token: 0x140000BD RID: 189
		// (add) Token: 0x06001EA6 RID: 7846 RVA: 0x0007702F File Offset: 0x0007522F
		// (remove) Token: 0x06001EA7 RID: 7847 RVA: 0x00077042 File Offset: 0x00075242
		[Category("Action")]
		[Description("Occurs before the export to excel is executed.")]
		public event PXGridExportToExcelEventHandler BeforeExportToExcel
		{
			add
			{
				base.Events.AddHandler(PXGrid.BeforeExportToExcelEvent, value);
			}
			remove
			{
				base.Events.RemoveHandler(PXGrid.BeforeExportToExcelEvent, value);
			}
		}

		// Token: 0x17000A98 RID: 2712
		// (get) Token: 0x06001EA8 RID: 7848 RVA: 0x00077055 File Offset: 0x00075255
		// (set) Token: 0x06001EA9 RID: 7849 RVA: 0x0007706C File Offset: 0x0007526C
		[DefaultValue("")]
		[Category("Behavior")]
		[Description("The ID of the master control.")]
		[Browsable(false)]
		public virtual string DependsOnControlIDs
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "DependsOnControlIDs", string.Empty);
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "DependsOnControlIDs", value, string.Empty);
			}
		}

		// Token: 0x17000A99 RID: 2713
		// (get) Token: 0x06001EAA RID: 7850 RVA: 0x00077084 File Offset: 0x00075284
		// (set) Token: 0x06001EAB RID: 7851 RVA: 0x00077097 File Offset: 0x00075297
		[Category("Ext. Property")]
		[DefaultValue(false)]
		[Description("Indicates whether the grid synchronizes its position with the DataSource control.")]
		public bool SyncPosition
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "SyncPosition", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "SyncPosition", value, false);
			}
		}

		// Token: 0x17000A9A RID: 2714
		// (get) Token: 0x06001EAC RID: 7852 RVA: 0x000770AB File Offset: 0x000752AB
		// (set) Token: 0x06001EAD RID: 7853 RVA: 0x000770BE File Offset: 0x000752BE
		[Browsable(false)]
		[DefaultValue(false)]
		[Description("Indicates whether the grid synchronizes the active row index with current data item of the graph.")]
		public bool SyncPositionWithGraph
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "SyncPositionWithGraph", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "SyncPositionWithGraph", value, false);
			}
		}

		// Token: 0x17000A9B RID: 2715
		// (get) Token: 0x06001EAE RID: 7854 RVA: 0x000770D2 File Offset: 0x000752D2
		// (set) Token: 0x06001EAF RID: 7855 RVA: 0x000770E5 File Offset: 0x000752E5
		[Browsable(false)]
		[DefaultValue(false)]
		[Description("Indicates whether the grid has priority in synchronise position with DataSource control.")]
		public bool SyncPositionPriority
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "SyncPositionPriority", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "SyncPositionPriority", value, false);
			}
		}

		// Token: 0x17000A9C RID: 2716
		// (get) Token: 0x06001EB0 RID: 7856 RVA: 0x000770F9 File Offset: 0x000752F9
		// (set) Token: 0x06001EB1 RID: 7857 RVA: 0x0007710C File Offset: 0x0007530C
		[Category("Ext. Property")]
		[DefaultValue(false)]
		[ScriptBrowsable]
		[Description("Indicates whether the grid stores the active position by DataKey of the current record.")]
		public bool KeepPosition
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "KeepPosition", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "KeepPosition", value, false);
			}
		}

		// Token: 0x17000A9D RID: 2717
		// (get) Token: 0x06001EB2 RID: 7858 RVA: 0x00077120 File Offset: 0x00075320
		// (set) Token: 0x06001EB3 RID: 7859 RVA: 0x00077133 File Offset: 0x00075333
		[Category("Ext. Property")]
		[DefaultValue(false)]
		[Description("Indicates whether the grid should preserve active sorts and filters between page reloads.")]
		public bool PreserveSortsAndFilters
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "PreserveSortsAndFilters", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "PreserveSortsAndFilters", value, false);
			}
		}

		// Token: 0x17000A9E RID: 2718
		// (get) Token: 0x06001EB4 RID: 7860 RVA: 0x00077147 File Offset: 0x00075347
		// (set) Token: 0x06001EB5 RID: 7861 RVA: 0x0007715A File Offset: 0x0007535A
		[Category("Ext. Property")]
		[DefaultValue(false)]
		[Description("Indicates whether the grid stores active page index by current ScreenID.")]
		public bool PreservePageIndex
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "PreservePageIndex", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "PreservePageIndex", value, false);
			}
		}

		// Token: 0x17000A9F RID: 2719
		// (get) Token: 0x06001EB6 RID: 7862 RVA: 0x0007716E File Offset: 0x0007536E
		internal bool PreservePageFinal
		{
			get
			{
				return this.PreservePageIndex && this.AllowPaging && this.ActionBar.PagerSettings.Mode > GridPagerMode.NextPrevFirstLast;
			}
		}

		// Token: 0x17000AA0 RID: 2720
		// (get) Token: 0x06001EB7 RID: 7863 RVA: 0x00077195 File Offset: 0x00075395
		// (set) Token: 0x06001EB8 RID: 7864 RVA: 0x0007719D File Offset: 0x0007539D
		[Browsable(false)]
		public override object DataSource
		{
			get
			{
				return base.DataSource;
			}
			set
			{
				base.DataSource = value;
			}
		}

		// Token: 0x17000AA1 RID: 2721
		// (get) Token: 0x06001EB9 RID: 7865 RVA: 0x000771A6 File Offset: 0x000753A6
		// (set) Token: 0x06001EBA RID: 7866 RVA: 0x000771AE File Offset: 0x000753AE
		[Browsable(false)]
		public override string AccessKey
		{
			get
			{
				return base.AccessKey;
			}
			set
			{
				base.AccessKey = value;
			}
		}

		// Token: 0x17000AA2 RID: 2722
		// (get) Token: 0x06001EBB RID: 7867 RVA: 0x000771B7 File Offset: 0x000753B7
		// (set) Token: 0x06001EBC RID: 7868 RVA: 0x000771BF File Offset: 0x000753BF
		[Browsable(false)]
		public override Color BackColor
		{
			get
			{
				return base.BackColor;
			}
			set
			{
				base.BackColor = value;
			}
		}

		// Token: 0x17000AA3 RID: 2723
		// (get) Token: 0x06001EBD RID: 7869 RVA: 0x000771C8 File Offset: 0x000753C8
		// (set) Token: 0x06001EBE RID: 7870 RVA: 0x000771D0 File Offset: 0x000753D0
		[Browsable(false)]
		public override Color BorderColor
		{
			get
			{
				return base.BorderColor;
			}
			set
			{
				base.BorderColor = value;
			}
		}

		// Token: 0x17000AA4 RID: 2724
		// (get) Token: 0x06001EBF RID: 7871 RVA: 0x000771D9 File Offset: 0x000753D9
		// (set) Token: 0x06001EC0 RID: 7872 RVA: 0x000771E1 File Offset: 0x000753E1
		[Browsable(false)]
		public override Unit BorderWidth
		{
			get
			{
				return base.BorderWidth;
			}
			set
			{
				base.BorderWidth = value;
			}
		}

		// Token: 0x17000AA5 RID: 2725
		// (get) Token: 0x06001EC1 RID: 7873 RVA: 0x000771EA File Offset: 0x000753EA
		// (set) Token: 0x06001EC2 RID: 7874 RVA: 0x000771F2 File Offset: 0x000753F2
		[Browsable(false)]
		public override BorderStyle BorderStyle
		{
			get
			{
				return base.BorderStyle;
			}
			set
			{
				base.BorderStyle = value;
			}
		}

		// Token: 0x17000AA6 RID: 2726
		// (get) Token: 0x06001EC3 RID: 7875 RVA: 0x000771FB File Offset: 0x000753FB
		// (set) Token: 0x06001EC4 RID: 7876 RVA: 0x00077203 File Offset: 0x00075403
		[Browsable(false)]
		public override string CssClass
		{
			get
			{
				return base.CssClass;
			}
			set
			{
				base.CssClass = value;
			}
		}

		// Token: 0x17000AA7 RID: 2727
		// (get) Token: 0x06001EC5 RID: 7877 RVA: 0x0007720C File Offset: 0x0007540C
		// (set) Token: 0x06001EC6 RID: 7878 RVA: 0x00077214 File Offset: 0x00075414
		[Browsable(false)]
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

		// Token: 0x17000AA8 RID: 2728
		// (get) Token: 0x06001EC7 RID: 7879 RVA: 0x0007721D File Offset: 0x0007541D
		// (set) Token: 0x06001EC8 RID: 7880 RVA: 0x00077225 File Offset: 0x00075425
		[Browsable(false)]
		public override bool EnableTheming
		{
			get
			{
				return base.EnableTheming;
			}
			set
			{
				base.EnableTheming = value;
			}
		}

		// Token: 0x17000AA9 RID: 2729
		// (get) Token: 0x06001EC9 RID: 7881 RVA: 0x0007722E File Offset: 0x0007542E
		[Browsable(false)]
		public override FontInfo Font
		{
			get
			{
				return base.Font;
			}
		}

		// Token: 0x17000AAA RID: 2730
		// (get) Token: 0x06001ECA RID: 7882 RVA: 0x00077236 File Offset: 0x00075436
		// (set) Token: 0x06001ECB RID: 7883 RVA: 0x0007723E File Offset: 0x0007543E
		[Browsable(false)]
		public override Color ForeColor
		{
			get
			{
				return base.ForeColor;
			}
			set
			{
				base.ForeColor = value;
			}
		}

		// Token: 0x17000AAB RID: 2731
		// (get) Token: 0x06001ECC RID: 7884 RVA: 0x00077247 File Offset: 0x00075447
		// (set) Token: 0x06001ECD RID: 7885 RVA: 0x0007724F File Offset: 0x0007544F
		[Category("Base Property")]
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

		// Token: 0x17000AAC RID: 2732
		// (get) Token: 0x06001ECE RID: 7886 RVA: 0x00077258 File Offset: 0x00075458
		// (set) Token: 0x06001ECF RID: 7887 RVA: 0x00077260 File Offset: 0x00075460
		[Category("Base Property")]
		public override string SkinID
		{
			get
			{
				return base.SkinID;
			}
			set
			{
				base.SkinID = value;
			}
		}

		// Token: 0x17000AAD RID: 2733
		// (get) Token: 0x06001ED0 RID: 7888 RVA: 0x00077269 File Offset: 0x00075469
		// (set) Token: 0x06001ED1 RID: 7889 RVA: 0x00077271 File Offset: 0x00075471
		[Category("Ext. Property")]
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

		// Token: 0x17000AAE RID: 2734
		// (get) Token: 0x06001ED2 RID: 7890 RVA: 0x0007727A File Offset: 0x0007547A
		// (set) Token: 0x06001ED3 RID: 7891 RVA: 0x00077282 File Offset: 0x00075482
		[Browsable(false)]
		public override string ToolTip
		{
			get
			{
				return base.ToolTip;
			}
			set
			{
				base.ToolTip = value;
			}
		}

		// Token: 0x17000AAF RID: 2735
		// (get) Token: 0x06001ED4 RID: 7892 RVA: 0x0007728B File Offset: 0x0007548B
		// (set) Token: 0x06001ED5 RID: 7893 RVA: 0x00077293 File Offset: 0x00075493
		[Category("Base Property")]
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

		// Token: 0x17000AB0 RID: 2736
		// (get) Token: 0x06001ED6 RID: 7894 RVA: 0x0007729C File Offset: 0x0007549C
		// (set) Token: 0x06001ED7 RID: 7895 RVA: 0x000772A4 File Offset: 0x000754A4
		[Category("Base Property")]
		public override string ID
		{
			get
			{
				return base.ID;
			}
			set
			{
				base.ID = value;
			}
		}

		// Token: 0x17000AB1 RID: 2737
		// (get) Token: 0x06001ED8 RID: 7896 RVA: 0x000772AD File Offset: 0x000754AD
		// (set) Token: 0x06001ED9 RID: 7897 RVA: 0x000772B5 File Offset: 0x000754B5
		[Browsable(false)]
		public override bool EnableViewState
		{
			get
			{
				return base.EnableViewState;
			}
			set
			{
				base.EnableViewState = value;
			}
		}

		// Token: 0x17000AB2 RID: 2738
		// (get) Token: 0x06001EDA RID: 7898 RVA: 0x000772BE File Offset: 0x000754BE
		// (set) Token: 0x06001EDB RID: 7899 RVA: 0x000772C6 File Offset: 0x000754C6
		[Browsable(false)]
		public override bool Visible
		{
			get
			{
				return base.Visible;
			}
			set
			{
				base.Visible = value;
			}
		}

		// Token: 0x17000AB3 RID: 2739
		// (get) Token: 0x06001EDC RID: 7900 RVA: 0x000772CF File Offset: 0x000754CF
		// (set) Token: 0x06001EDD RID: 7901 RVA: 0x000772D7 File Offset: 0x000754D7
		[Browsable(false)]
		public override ClientIDMode ClientIDMode
		{
			get
			{
				return base.ClientIDMode;
			}
			set
			{
				base.ClientIDMode = value;
			}
		}

		// Token: 0x17000AB4 RID: 2740
		// (get) Token: 0x06001EDE RID: 7902 RVA: 0x000772E0 File Offset: 0x000754E0
		// (set) Token: 0x06001EDF RID: 7903 RVA: 0x000772E8 File Offset: 0x000754E8
		[Browsable(false)]
		public override ViewStateMode ViewStateMode
		{
			get
			{
				return base.ViewStateMode;
			}
			set
			{
				base.ViewStateMode = value;
			}
		}

		// Token: 0x17000AB5 RID: 2741
		// (get) Token: 0x06001EE0 RID: 7904 RVA: 0x000772F1 File Offset: 0x000754F1
		// (set) Token: 0x06001EE1 RID: 7905 RVA: 0x00077304 File Offset: 0x00075504
		[DefaultValue(false)]
		[Category("Ext. Property")]
		[ScriptBrowsable]
		[Description("Indicates whether the grid should restrict fields in the query.")]
		public bool RestrictFields
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "RestrictFields", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "RestrictFields", value, false);
			}
		}

		// Token: 0x17000AB6 RID: 2742
		// (get) Token: 0x06001EE2 RID: 7906 RVA: 0x00077318 File Offset: 0x00075518
		// (set) Token: 0x06001EE3 RID: 7907 RVA: 0x0007736D File Offset: 0x0007556D
		[DefaultValue("")]
		[Category("Appearance")]
		[Description("The header of the blank filter.")]
		[Browsable(false)]
		public virtual string BlankFilterHeader
		{
			get
			{
				string prop = STM.GetProp<string>(this.ViewState, "BlankFilterHeader", string.Empty);
				if (!base.DesignMode && !string.IsNullOrWhiteSpace(prop) && HttpContext.Current != null)
				{
					return ControlHelper.LocalizeValue(prop, this.ID, "BlankFilterHeader", this.Page, false, null);
				}
				return prop;
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "BlankFilterHeader", value, string.Empty);
			}
		}

		// Token: 0x17000AB7 RID: 2743
		// (get) Token: 0x06001EE4 RID: 7908 RVA: 0x00077385 File Offset: 0x00075585
		// (set) Token: 0x06001EE5 RID: 7909 RVA: 0x00077398 File Offset: 0x00075598
		[ScriptBrowsable]
		[DefaultValue(true)]
		[Browsable(false)]
		[Description("Indicates whether the grid extends the button editors in the inline edit mode.")]
		public bool ExtendButtonEditors
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "ExtendButtonEditors", true);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "ExtendButtonEditors", value, true);
			}
		}

		// Token: 0x17000AB8 RID: 2744
		// (get) Token: 0x06001EE6 RID: 7910 RVA: 0x000773AC File Offset: 0x000755AC
		// (set) Token: 0x06001EE7 RID: 7911 RVA: 0x000773BF File Offset: 0x000755BF
		[DefaultValue(GridFeedbackModeOverride.EnableInquiry)]
		[Browsable(false)]
		public GridFeedbackModeOverride FeedbackMode
		{
			get
			{
				return STM.GetProp<GridFeedbackModeOverride>(this.ViewState, "FeedbackMode", GridFeedbackModeOverride.EnableInquiry);
			}
			set
			{
				STM.SetProp<GridFeedbackModeOverride>(this.ViewState, "FeedbackMode", value, GridFeedbackModeOverride.EnableInquiry);
			}
		}

		// Token: 0x17000AB9 RID: 2745
		// (get) Token: 0x06001EE8 RID: 7912 RVA: 0x000773D3 File Offset: 0x000755D3
		[DefaultValue(null)]
		[Browsable(false)]
		[ScriptBrowsable]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Description("The list of messages to show when the grid is empty")]
		public EmptyMessages EmptyMsg
		{
			get
			{
				if (this._emptyMsg == null)
				{
					this._emptyMsg = new EmptyMessages(this, base.DesignMode);
				}
				return this._emptyMsg;
			}
		}

		// Token: 0x17000ABA RID: 2746
		// (get) Token: 0x06001EE9 RID: 7913 RVA: 0x000773F8 File Offset: 0x000755F8
		[ScriptBrowsable(Mode = ScriptBrowsable.Always)]
		[DefaultValue(-1)]
		[Browsable(false)]
		public int EmptyMessageMode
		{
			get
			{
				if (this.FeedbackMode == GridFeedbackModeOverride.DisableAll)
				{
					return 0;
				}
				if (this.FeedbackMode == GridFeedbackModeOverride.ForceInquiry)
				{
					return 2;
				}
				PXView pxview = null;
				if (this.StoredFastFilter != null || !string.IsNullOrEmpty(this.fastFilter))
				{
					return 1;
				}
				PXPage pxpage = this.Page as PXPage;
				PXDataSource pxdataSource = (pxpage != null) ? pxpage.DefaultDataSource : null;
				if (this.FeedbackMode != GridFeedbackModeOverride.ForceDataEntry)
				{
					if (pxdataSource != null && pxdataSource.ID == this.DataSourceID && pxdataSource.PageLoadBehavior == PXPageLoadBehavior.PopulateSavedValues)
					{
						return 2;
					}
					PXPage pxpage2 = this.Page as PXPage;
					PXGraph pxgraph;
					if (pxpage2 == null)
					{
						pxgraph = null;
					}
					else
					{
						PXDataSource defaultDataSource = pxpage2.DefaultDataSource;
						pxgraph = ((defaultDataSource != null) ? defaultDataSource.DataGraph : null);
					}
					bool flag = false;
					foreach (object obj in pxgraph.Actions)
					{
						if (((DictionaryEntry)obj).Value.GetType().FullName.StartsWith("PX.Data.PXInsert"))
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						return 2;
					}
				}
				if (this.FeedbackMode == GridFeedbackModeOverride.EnableAll || this.FeedbackMode == GridFeedbackModeOverride.ForceDataEntry)
				{
					PXPage pxpage3 = this.Page as PXPage;
					bool flag2;
					if (pxpage3 == null)
					{
						flag2 = false;
					}
					else
					{
						PXDataSource defaultDataSource2 = pxpage3.DefaultDataSource;
						bool? flag3;
						if (defaultDataSource2 == null)
						{
							flag3 = null;
						}
						else
						{
							PXGraph dataGraph = defaultDataSource2.DataGraph;
							if (dataGraph == null)
							{
								flag3 = null;
							}
							else
							{
								PXViewCollection views = dataGraph.Views;
								flag3 = ((views != null) ? new bool?(views.TryGetValue(this.DataMember, out pxview)) : null);
							}
						}
						bool? flag4 = flag3;
						flag2 = flag4.GetValueOrDefault();
					}
					if (flag2)
					{
						PXCache cache = pxview.Cache;
						if (cache != null && cache.AllowDelete && cache.AllowUpdate && !cache.AllowInsert)
						{
							return 3;
						}
					}
					return 4;
				}
				return 0;
			}
		}

		// Token: 0x17000ABB RID: 2747
		// (get) Token: 0x06001EEA RID: 7914 RVA: 0x000775BC File Offset: 0x000757BC
		// (set) Token: 0x06001EEB RID: 7915 RVA: 0x000775C4 File Offset: 0x000757C4
		[ScriptBrowsable]
		[DefaultValue(null)]
		[Browsable(false)]
		public string AddCommandName { get; set; }

		// Token: 0x17000ABC RID: 2748
		// (get) Token: 0x06001EEC RID: 7916 RVA: 0x000775CD File Offset: 0x000757CD
		// (set) Token: 0x06001EED RID: 7917 RVA: 0x000775E0 File Offset: 0x000757E0
		[Category("Behavior")]
		[DefaultValue(GridViewMode.Flat)]
		[ScriptBrowsable]
		[Description("The mode of the grid data representation.")]
		[Browsable(false)]
		public GridViewMode ViewMode
		{
			get
			{
				return STM.GetProp<GridViewMode>(this.ViewState, "ViewMode", GridViewMode.Flat);
			}
			set
			{
				STM.SetProp<GridViewMode>(this.ViewState, "ViewMode", value, GridViewMode.Flat);
			}
		}

		// Token: 0x17000ABD RID: 2749
		// (get) Token: 0x06001EEE RID: 7918 RVA: 0x000775F4 File Offset: 0x000757F4
		// (set) Token: 0x06001EEF RID: 7919 RVA: 0x00077649 File Offset: 0x00075849
		[Category("Base Property")]
		[DefaultValue("")]
		[Description("The text of the grid caption.")]
		[Localizable(true)]
		public string Caption
		{
			get
			{
				string prop = STM.GetProp<string>(this.ViewState, "Caption", string.Empty);
				if (!base.DesignMode && !string.IsNullOrWhiteSpace(prop) && HttpContext.Current != null)
				{
					return ControlHelper.LocalizeValue(prop, this.ID, "Caption", this.Page, false, null);
				}
				return prop;
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "Caption", value, string.Empty);
			}
		}

		// Token: 0x17000ABE RID: 2750
		// (get) Token: 0x06001EF0 RID: 7920 RVA: 0x00077661 File Offset: 0x00075861
		// (set) Token: 0x06001EF1 RID: 7921 RVA: 0x00077674 File Offset: 0x00075874
		[Category("Base Property")]
		[DefaultValue(true)]
		[Description("The visibility of the grid caption.")]
		public bool CaptionVisible
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "CaptionVisible", true);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "CaptionVisible", value, true);
			}
		}

		// Token: 0x17000ABF RID: 2751
		// (get) Token: 0x06001EF2 RID: 7922 RVA: 0x00077688 File Offset: 0x00075888
		// (set) Token: 0x06001EF3 RID: 7923 RVA: 0x0007769B File Offset: 0x0007589B
		[Category("Ext. Property")]
		[DefaultValue(ColumnGeneration.None)]
		[Description("The generation mode of grid columns.")]
		public ColumnGeneration AutoGenerateColumns
		{
			get
			{
				return STM.GetProp<ColumnGeneration>(this.ViewState, "AutoGenerateColumns", ColumnGeneration.None);
			}
			set
			{
				STM.SetProp<ColumnGeneration>(this.ViewState, "AutoGenerateColumns", value, ColumnGeneration.None);
			}
		}

		// Token: 0x17000AC0 RID: 2752
		// (get) Token: 0x06001EF4 RID: 7924 RVA: 0x000776AF File Offset: 0x000758AF
		// (set) Token: 0x06001EF5 RID: 7925 RVA: 0x000776C2 File Offset: 0x000758C2
		[Category("Base Property")]
		[DefaultValue(false)]
		[ScriptBrowsable]
		[Description("Indicates whether the grid should adjust columns automatically.")]
		public virtual bool AutoAdjustColumns
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "AutoAdjustColumns", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "AutoAdjustColumns", value, false);
			}
		}

		// Token: 0x17000AC1 RID: 2753
		// (get) Token: 0x06001EF6 RID: 7926 RVA: 0x000776D6 File Offset: 0x000758D6
		// (set) Token: 0x06001EF7 RID: 7927 RVA: 0x000776E9 File Offset: 0x000758E9
		[DefaultValue(true)]
		[ScriptBrowsable]
		[Browsable(false)]
		[Description("Indicates whether the grid should save column layout automatically.")]
		public virtual bool AutoSaveLayout
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "AutoSaveLayout", true);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "AutoSaveLayout", value, true);
			}
		}

		// Token: 0x17000AC2 RID: 2754
		// (get) Token: 0x06001EF8 RID: 7928 RVA: 0x000776FD File Offset: 0x000758FD
		// (set) Token: 0x06001EF9 RID: 7929 RVA: 0x00077710 File Offset: 0x00075910
		[Category("Base Property")]
		[DefaultValue(false)]
		[ScriptBrowsable]
		[Description("Indicates whether the grid uses individual properties of the cells.")]
		public bool MatrixMode
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "MatrixMode", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "MatrixMode", value, false);
			}
		}

		// Token: 0x17000AC3 RID: 2755
		// (get) Token: 0x06001EFA RID: 7930 RVA: 0x00077724 File Offset: 0x00075924
		// (set) Token: 0x06001EFB RID: 7931 RVA: 0x00077737 File Offset: 0x00075937
		[Category("Behavior")]
		[DefaultValue(true)]
		[ScriptBrowsable]
		[Description("Indicates whether the inline editor is always visible.")]
		[Browsable(false)]
		public bool FloatingEditor
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "FloatingEditor", true);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "FloatingEditor", value, true);
			}
		}

		// Token: 0x17000AC4 RID: 2756
		// (get) Token: 0x06001EFC RID: 7932 RVA: 0x0007774B File Offset: 0x0007594B
		// (set) Token: 0x06001EFD RID: 7933 RVA: 0x0007775E File Offset: 0x0007595E
		[Category("Base Property")]
		[DefaultValue(false)]
		[ScriptBrowsable]
		[Description("Indicates whether the grid caches the row change operations.")]
		public bool BatchUpdate
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "BatchUpdate", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "BatchUpdate", value, false);
			}
		}

		// Token: 0x17000AC5 RID: 2757
		// (get) Token: 0x06001EFE RID: 7934 RVA: 0x00077772 File Offset: 0x00075972
		// (set) Token: 0x06001EFF RID: 7935 RVA: 0x00077789 File Offset: 0x00075989
		[Category("Ext. Property")]
		[DefaultValue("")]
		[UrlProperty("*.aspx")]
		[Editor("System.Web.UI.Design.UrlEditor", typeof(UITypeEditor))]
		[Description("The URL to navigate to when the row edit button is clicked.")]
		public string EditPageUrl
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "EditPageUrl", string.Empty);
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "EditPageUrl", value, string.Empty);
			}
		}

		// Token: 0x17000AC6 RID: 2758
		// (get) Token: 0x06001F00 RID: 7936 RVA: 0x000777A1 File Offset: 0x000759A1
		// (set) Token: 0x06001F01 RID: 7937 RVA: 0x000777B4 File Offset: 0x000759B4
		[DefaultValue(true)]
		[Category("Behavior")]
		[Browsable(false)]
		[ScriptBrowsable]
		[Description("Indicates whether the control checks if the data has been changed before page unload.")]
		public bool CheckChanges
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "CheckChanges", true);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "CheckChanges", value, true);
			}
		}

		// Token: 0x17000AC7 RID: 2759
		// (get) Token: 0x06001F02 RID: 7938 RVA: 0x000777C8 File Offset: 0x000759C8
		// (set) Token: 0x06001F03 RID: 7939 RVA: 0x000777DB File Offset: 0x000759DB
		[DefaultValue(MarkRequiredMode.True)]
		[Category("Ext. Property")]
		[ScriptBrowsable]
		[Description("Indicates whether the control marks the required field controls.")]
		public MarkRequiredMode MarkRequired
		{
			get
			{
				return STM.GetProp<MarkRequiredMode>(this.ViewState, "MarkRequired", MarkRequiredMode.True);
			}
			set
			{
				STM.SetProp<MarkRequiredMode>(this.ViewState, "MarkRequired", value, MarkRequiredMode.True);
			}
		}

		// Token: 0x17000AC8 RID: 2760
		// (get) Token: 0x06001F04 RID: 7940 RVA: 0x000777EF File Offset: 0x000759EF
		// (set) Token: 0x06001F05 RID: 7941 RVA: 0x00077802 File Offset: 0x00075A02
		[Category("Behavior")]
		[DefaultValue(true)]
		[Description("Indicates whether the styles should be merged from parent to child.")]
		[Browsable(false)]
		public bool MergeStyles
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "MergeStyles", true);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "MergeStyles", value, true);
			}
		}

		// Token: 0x17000AC9 RID: 2761
		// (get) Token: 0x06001F06 RID: 7942 RVA: 0x00077816 File Offset: 0x00075A16
		// (set) Token: 0x06001F07 RID: 7943 RVA: 0x00077829 File Offset: 0x00075A29
		[Category("Appearance")]
		[DefaultValue(GridScrollBars.Auto)]
		[Description("The visibility and position of scroll bars in the PXGrid control.")]
		[Browsable(false)]
		public GridScrollBars ScrollBars
		{
			get
			{
				return STM.GetProp<GridScrollBars>(this.ViewState, "ScrollBars", GridScrollBars.Auto);
			}
			set
			{
				STM.SetProp<GridScrollBars>(this.ViewState, "ScrollBars", value, GridScrollBars.Auto);
			}
		}

		// Token: 0x17000ACA RID: 2762
		// (get) Token: 0x06001F08 RID: 7944 RVA: 0x0007783D File Offset: 0x00075A3D
		// (set) Token: 0x06001F09 RID: 7945 RVA: 0x00077854 File Offset: 0x00075A54
		[Category("Appearance")]
		[DefaultValue("The control has no data to render.")]
		[Localizable(true)]
		[Description("The text that is displayed when there is no data in the grid.")]
		[Browsable(false)]
		public string NoDataText
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "NoDataText", "The control has no data to render.");
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "NoDataText", value, "The control has no data to render.");
			}
		}

		// Token: 0x17000ACB RID: 2763
		// (get) Token: 0x06001F0A RID: 7946 RVA: 0x0007786C File Offset: 0x00075A6C
		// (set) Token: 0x06001F0B RID: 7947 RVA: 0x0007787F File Offset: 0x00075A7F
		[Browsable(false)]
		[DefaultValue(GridDropMode.False)]
		[ScriptBrowsable]
		[Description("Indicates whether the control can accept data that a user drags onto it.")]
		public GridDropMode AllowDrop
		{
			get
			{
				return STM.GetProp<GridDropMode>(this.ViewState, "AllowDrop", GridDropMode.False);
			}
			set
			{
				STM.SetProp<GridDropMode>(this.ViewState, "AllowDrop", value, GridDropMode.False);
			}
		}

		// Token: 0x17000ACC RID: 2764
		// (get) Token: 0x06001F0C RID: 7948 RVA: 0x00077893 File Offset: 0x00075A93
		// (set) Token: 0x06001F0D RID: 7949 RVA: 0x000778AA File Offset: 0x00075AAA
		[DefaultValue("")]
		[Themeable(false)]
		[Category("Behavior")]
		[TypeConverter("PX.Web.UI.Design.PXMenuConverter")]
		[Description("The ID of the context menu associated with the control.")]
		[Browsable(false)]
		public string ContextMenuID
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "ContextMenuID", string.Empty);
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "ContextMenuID", value, string.Empty);
			}
		}

		// Token: 0x17000ACD RID: 2765
		// (get) Token: 0x06001F0E RID: 7950 RVA: 0x000778C2 File Offset: 0x00075AC2
		// (set) Token: 0x06001F0F RID: 7951 RVA: 0x000778D5 File Offset: 0x00075AD5
		[Category("Appearance")]
		[DefaultValue(true)]
		[Description("Indicates whether the local menu can be shown by the grid.")]
		[Browsable(false)]
		public virtual bool LocalMenu
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "LocalMenu", true);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "LocalMenu", value, true);
			}
		}

		// Token: 0x17000ACE RID: 2766
		// (get) Token: 0x06001F10 RID: 7952 RVA: 0x000778EC File Offset: 0x00075AEC
		// (set) Token: 0x06001F11 RID: 7953 RVA: 0x00077918 File Offset: 0x00075B18
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Base Property")]
		[ScriptBrowsable]
		[Description("The URLs of the imagas for the basic export button.")]
		[Browsable(false)]
		public PXGridExportImages ExportImages
		{
			get
			{
				PXGridExportImages result;
				if ((result = this._exportImages) == null)
				{
					PXGridExportImages pxgridExportImages = new PXGridExportImages();
					pxgridExportImages.Owner = this;
					PXGridExportImages pxgridExportImages2 = pxgridExportImages;
					this._exportImages = pxgridExportImages;
					result = pxgridExportImages2;
				}
				return result;
			}
			set
			{
				this._exportImages = value;
				if (this._exportImages != null)
				{
					this._exportImages.Owner = this;
				}
			}
		}

		// Token: 0x17000ACF RID: 2767
		// (get) Token: 0x06001F12 RID: 7954 RVA: 0x00077935 File Offset: 0x00075B35
		// (set) Token: 0x06001F13 RID: 7955 RVA: 0x00077948 File Offset: 0x00075B48
		[Category("Ext. Property")]
		[DefaultValue(false)]
		[Description("Indicates whether the control needs to repaint its columns.")]
		public bool RepaintColumns
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "RepaintColumns", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "RepaintColumns", value, false);
			}
		}

		// Token: 0x17000AD0 RID: 2768
		// (get) Token: 0x06001F14 RID: 7956 RVA: 0x0007795C File Offset: 0x00075B5C
		// (set) Token: 0x06001F15 RID: 7957 RVA: 0x0007796F File Offset: 0x00075B6F
		[Browsable(false)]
		[DefaultValue(false)]
		[Description("Indicates whether the control needs to generate columns before repaint.")]
		public bool GenerateColumnsBeforeRepaint
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "GenerateColumnsBeforeRepaint", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "GenerateColumnsBeforeRepaint", value, false);
			}
		}

		// Token: 0x17000AD1 RID: 2769
		// (get) Token: 0x06001F16 RID: 7958 RVA: 0x00077983 File Offset: 0x00075B83
		// (set) Token: 0x06001F17 RID: 7959 RVA: 0x000779AF File Offset: 0x00075BAF
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public PXGridCell ActiveCell
		{
			get
			{
				if (this.activeCell != null)
				{
					return this.activeCell;
				}
				if (this.activeRow != null)
				{
					return this.activeRow.Cells[0];
				}
				return null;
			}
			set
			{
				this.activeCell = value;
				if (value != null)
				{
					this.activeRow = value.Row;
				}
			}
		}

		// Token: 0x17000AD2 RID: 2770
		// (get) Token: 0x06001F18 RID: 7960 RVA: 0x000779C7 File Offset: 0x00075BC7
		// (set) Token: 0x06001F19 RID: 7961 RVA: 0x000779E3 File Offset: 0x00075BE3
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public PXGridRow ActiveRow
		{
			get
			{
				if (this.activeCell != null)
				{
					return this.activeCell.Row;
				}
				return this.activeRow;
			}
			set
			{
				this.activeRow = value;
				if (value != null && this.activeCell != null)
				{
					this.activeCell = value.Cells[this.activeCell.Index];
					return;
				}
				this.activeCell = null;
			}
		}

		// Token: 0x17000AD3 RID: 2771
		// (get) Token: 0x06001F1A RID: 7962 RVA: 0x00077A1B File Offset: 0x00075C1B
		// (set) Token: 0x06001F1B RID: 7963 RVA: 0x00077A2E File Offset: 0x00075C2E
		[Browsable(false)]
		[DefaultValue(false)]
		public bool RenderExternalActions
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "RenderExternalActions", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "RenderExternalActions", value, false);
			}
		}

		// Token: 0x17000AD4 RID: 2772
		// (get) Token: 0x06001F1C RID: 7964 RVA: 0x00077A42 File Offset: 0x00075C42
		// (set) Token: 0x06001F1D RID: 7965 RVA: 0x00077A55 File Offset: 0x00075C55
		[Browsable(false)]
		[DefaultValue(FilterSelectorType.Tabs)]
		public FilterSelectorType FilterSelector
		{
			get
			{
				return STM.GetProp<FilterSelectorType>(this.ViewState, "FilterSelector", FilterSelectorType.Tabs);
			}
			set
			{
				STM.SetProp<FilterSelectorType>(this.ViewState, "FilterSelector", value, FilterSelectorType.Tabs);
			}
		}

		// Token: 0x17000AD5 RID: 2773
		// (get) Token: 0x06001F1E RID: 7966 RVA: 0x00077A69 File Offset: 0x00075C69
		// (set) Token: 0x06001F1F RID: 7967 RVA: 0x00077A7C File Offset: 0x00075C7C
		[Browsable(false)]
		[DefaultValue(false)]
		[Description("Indicates whether the control needs to show the advanced filter toolbar.")]
		public bool ShowFilterToolbar
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "ShowFilterToolbar", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "ShowFilterToolbar", value, false);
			}
		}

		// Token: 0x17000AD6 RID: 2774
		// (get) Token: 0x06001F20 RID: 7968 RVA: 0x00077A90 File Offset: 0x00075C90
		internal bool ShowFilterToolbarFinal
		{
			get
			{
				return this.ShowFilterToolbar && !PXBaseDataSource.RedirectHelper.IsPopupLayer(this.Page);
			}
		}

		// Token: 0x17000AD7 RID: 2775
		// (get) Token: 0x06001F21 RID: 7969 RVA: 0x00077AAA File Offset: 0x00075CAA
		// (set) Token: 0x06001F22 RID: 7970 RVA: 0x00077AC1 File Offset: 0x00075CC1
		[Browsable(false)]
		public string TemporaryFilterCaption
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "TemporaryFilterCaption", PXGrid._defTemporaryFilterCaption);
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "TemporaryFilterCaption", value, PXGrid._defTemporaryFilterCaption);
			}
		}

		// Token: 0x06001F23 RID: 7971 RVA: 0x00077AD9 File Offset: 0x00075CD9
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected bool ShouldSerializeTemporaryFilterCaption()
		{
			return this.TemporaryFilterCaption != PXGrid._defTemporaryFilterCaption;
		}

		// Token: 0x06001F24 RID: 7972 RVA: 0x00077AEB File Offset: 0x00075CEB
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected void ResetTemporaryFilterCaption()
		{
			this.TemporaryFilterCaption = PXGrid._defTemporaryFilterCaption;
		}

		// Token: 0x17000AD8 RID: 2776
		// (get) Token: 0x06001F25 RID: 7973 RVA: 0x00077AF8 File Offset: 0x00075CF8
		// (set) Token: 0x06001F26 RID: 7974 RVA: 0x00077B0B File Offset: 0x00075D0B
		[Category("Ext. Property")]
		[DefaultValue(false)]
		[Description("Indicates whether notes are exported to Excel.")]
		public bool ExportNotes
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "ExportNotes", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "ExportNotes", value, false);
			}
		}

		// Token: 0x17000AD9 RID: 2777
		// (get) Token: 0x06001F27 RID: 7975 RVA: 0x00077B1F File Offset: 0x00075D1F
		// (set) Token: 0x06001F28 RID: 7976 RVA: 0x00077B32 File Offset: 0x00075D32
		[Browsable(false)]
		[DefaultValue(false)]
		[Description("Indicates whether the control allows to show pivot tables inside itself.")]
		public bool AllowPivotTable
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "AllowPivotTable", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "AllowPivotTable", value, false);
			}
		}

		// Token: 0x17000ADA RID: 2778
		// (get) Token: 0x06001F29 RID: 7977 RVA: 0x00077B46 File Offset: 0x00075D46
		// (set) Token: 0x06001F2A RID: 7978 RVA: 0x00077B5D File Offset: 0x00075D5D
		[Browsable(false)]
		[UrlProperty("*.aspx")]
		[Editor("System.Web.UI.Design.UrlEditor", typeof(UITypeEditor))]
		[DefaultValue("")]
		[Description("The URL of the page that will allow to edit pivot table settings.")]
		public string EditPivotTableUrl
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "EditPivotTableUrl", "");
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "EditPivotTableUrl", value, "");
			}
		}

		// Token: 0x17000ADB RID: 2779
		// (get) Token: 0x06001F2B RID: 7979 RVA: 0x00077B75 File Offset: 0x00075D75
		// (set) Token: 0x06001F2C RID: 7980 RVA: 0x00077B88 File Offset: 0x00075D88
		[DefaultValue(true)]
		[Browsable(false)]
		[Description("Indicates whether the control allows auto-hide.")]
		public virtual bool AllowAutoHide
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "AllowAutoHide", true);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "AllowAutoHide", value, true);
			}
		}

		// Token: 0x17000ADC RID: 2780
		// (get) Token: 0x06001F2D RID: 7981 RVA: 0x00077B9C File Offset: 0x00075D9C
		bool IAutoHideControl.Hidden
		{
			get
			{
				return this.hidden;
			}
		}

		// Token: 0x06001F2E RID: 7982 RVA: 0x00077BA4 File Offset: 0x00075DA4
		bool IAutoHideControl.CalculateVisibility()
		{
			this.hidden = !this.CalculateVisibility();
			return this.hidden;
		}

		// Token: 0x06001F2F RID: 7983 RVA: 0x00077BBC File Offset: 0x00075DBC
		protected virtual bool CalculateVisibility()
		{
			PXDataSourceView pxdataSourceView = this.GetDataView() as PXDataSourceView;
			if (pxdataSourceView != null && !pxdataSourceView.CanSelect)
			{
				return false;
			}
			bool result = false;
			foreach (object obj in this.Columns)
			{
				PXGridColumn pxgridColumn = (PXGridColumn)obj;
				bool flag = (pxgridColumn.VisibleLoaded || pxgridColumn.VisiblePosted) && !pxgridColumn.VisibleSynchronized;
				if ((pxgridColumn.Visible || flag) && !pxgridColumn.DataField.Contains("__"))
				{
					result = true;
					break;
				}
			}
			return result;
		}

		// Token: 0x17000ADD RID: 2781
		// (get) Token: 0x06001F30 RID: 7984 RVA: 0x00077C6C File Offset: 0x00075E6C
		Control IControlsContainer.ChildrenContainer
		{
			get
			{
				if (this.PrimaryLevel == null)
				{
					return null;
				}
				return this.PrimaryLevel.RowTemplateContainer;
			}
		}

		// Token: 0x17000ADE RID: 2782
		// (get) Token: 0x06001F31 RID: 7985 RVA: 0x00077C83 File Offset: 0x00075E83
		// (set) Token: 0x06001F32 RID: 7986 RVA: 0x00077C90 File Offset: 0x00075E90
		ITemplate IControlsContainer.Template
		{
			get
			{
				return this.PrimaryLevel.RowTemplate;
			}
			set
			{
				this.PrimaryLevel.RowTemplate = value;
			}
		}

		// Token: 0x17000ADF RID: 2783
		// (get) Token: 0x06001F33 RID: 7987 RVA: 0x00077C9E File Offset: 0x00075E9E
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		[Category("Data")]
		[Description("The collection of the grid levels.")]
		[Browsable(false)]
		public PXGridLevelCollection Levels
		{
			get
			{
				if (this.levels == null)
				{
					this.levels = new PXGridLevelCollection(this);
					if (base.IsTrackingViewState)
					{
						((IStateManager)this.levels).TrackViewState();
					}
				}
				return this.levels;
			}
		}

		// Token: 0x06001F34 RID: 7988 RVA: 0x00077CCD File Offset: 0x00075ECD
		[EditorBrowsable(EditorBrowsableState.Never)]
		protected bool ShouldSerializeLevels()
		{
			return this.levels != null && this.levels.Count > 0;
		}

		// Token: 0x06001F35 RID: 7989 RVA: 0x00077CE7 File Offset: 0x00075EE7
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void ResetLevels()
		{
			if (this.levels != null)
			{
				while (this.levels.Count > 1)
				{
					this.levels.RemoveAt(this.levels.Count - 1);
				}
			}
		}

		// Token: 0x17000AE0 RID: 2784
		// (get) Token: 0x06001F36 RID: 7990 RVA: 0x00077D19 File Offset: 0x00075F19
		[Category("Data")]
		[Description("The primary level object.")]
		[Browsable(false)]
		public PXGridLevel PrimaryLevel
		{
			get
			{
				if (this.Levels.Count == 0)
				{
					this.Levels.Add(new PXGridLevel());
				}
				return this.Levels[0];
			}
		}

		// Token: 0x17000AE1 RID: 2785
		// (get) Token: 0x06001F37 RID: 7991 RVA: 0x00077D45 File Offset: 0x00075F45
		[Category("Base Property")]
		[Description("The collection of the primary level columns.")]
		public PXGridColumnCollection Columns
		{
			get
			{
				return this.PrimaryLevel.Columns;
			}
		}

		// Token: 0x17000AE2 RID: 2786
		// (get) Token: 0x06001F38 RID: 7992 RVA: 0x00077D52 File Offset: 0x00075F52
		[Browsable(false)]
		public PXGridRowCollection Rows
		{
			get
			{
				if (this.rows == null)
				{
					this.rows = new PXGridRowCollection();
					this.rows.Level = this.PrimaryLevel;
				}
				return this.rows;
			}
		}

		// Token: 0x17000AE3 RID: 2787
		// (get) Token: 0x06001F39 RID: 7993 RVA: 0x00077D7E File Offset: 0x00075F7E
		// (set) Token: 0x06001F3A RID: 7994 RVA: 0x00077D9F File Offset: 0x00075F9F
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Appearance")]
		[Description("The style properties of the grid local menu.")]
		[Browsable(false)]
		public PXMenuStyles MenuStyles
		{
			get
			{
				if (this.menuStyles == null)
				{
					this.menuStyles = new PXMenuStyles(base.IsTrackingViewState);
				}
				return this.menuStyles;
			}
			internal set
			{
				this.menuStyles = value;
			}
		}

		// Token: 0x17000AE4 RID: 2788
		// (get) Token: 0x06001F3B RID: 7995 RVA: 0x00077DA8 File Offset: 0x00075FA8
		// (set) Token: 0x06001F3C RID: 7996 RVA: 0x00077DC9 File Offset: 0x00075FC9
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Appearance")]
		[Description("The properties of the images of the grid local menu.")]
		[Browsable(false)]
		public PXMenuImages MenuImages
		{
			get
			{
				if (this.menuImages == null)
				{
					this.menuImages = new PXMenuImages(base.IsTrackingViewState);
				}
				return this.menuImages;
			}
			internal set
			{
				this.menuImages = value;
			}
		}

		// Token: 0x17000AE5 RID: 2789
		// (get) Token: 0x06001F3D RID: 7997 RVA: 0x00077DD2 File Offset: 0x00075FD2
		[DefaultValue(null)]
		[Category("Ext. Property")]
		[ScriptBrowsable]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Description("The list of client-side events that should be processed by the application.")]
		public PXGridEvents ClientEvents
		{
			get
			{
				if (this.clientEvents == null)
				{
					this.clientEvents = new PXGridEvents(base.IsTrackingViewState);
				}
				return this.clientEvents;
			}
		}

		// Token: 0x17000AE6 RID: 2790
		// (get) Token: 0x06001F3E RID: 7998 RVA: 0x00077DF3 File Offset: 0x00075FF3
		[Category("Behavior")]
		[DefaultValue(null)]
		[ScriptBrowsable]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Description("Determines the type of filters and transitions that should be used when pop-up elements are displayed in IE 5.5 and above.")]
		[Browsable(false)]
		public PXExpandEffects ExpandEffects
		{
			get
			{
				if (this.expandEffects == null)
				{
					this.expandEffects = new PXExpandEffects(base.IsTrackingViewState);
				}
				return this.expandEffects;
			}
		}

		// Token: 0x17000AE7 RID: 2791
		// (get) Token: 0x06001F3F RID: 7999 RVA: 0x00077E14 File Offset: 0x00076014
		[DefaultValue(null)]
		[ScriptBrowsable]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Base Property")]
		[Description("The propertis of the auto-size mode of the grid.")]
		public PXAutoSizeInfo AutoSize
		{
			get
			{
				if (this.autoSize == null)
				{
					this.autoSize = new PXAutoSizeInfo(base.IsTrackingViewState);
					this.autoSize.SetDefaultValues(0, 100);
				}
				return this.autoSize;
			}
		}

		// Token: 0x17000AE8 RID: 2792
		// (get) Token: 0x06001F40 RID: 8000 RVA: 0x00077E43 File Offset: 0x00076043
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Base Property")]
		[Description("The appearance properties of the grid action bar.")]
		public PXGridActionBar ActionBar
		{
			get
			{
				if (this.actionBar == null)
				{
					this.actionBar = new PXGridActionBar(this, base.IsTrackingViewState);
				}
				return this.actionBar;
			}
		}

		// Token: 0x17000AE9 RID: 2793
		// (get) Token: 0x06001F41 RID: 8001 RVA: 0x00077E65 File Offset: 0x00076065
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Appearance")]
		[Description("The common styles properties of the grid.")]
		[Browsable(false)]
		public PXGridStyles GridStyles
		{
			get
			{
				if (this.gridStyles == null)
				{
					this.gridStyles = new PXGridStyles(base.IsTrackingViewState);
				}
				return this.gridStyles;
			}
		}

		// Token: 0x17000AEA RID: 2794
		// (get) Token: 0x06001F42 RID: 8002 RVA: 0x00077E86 File Offset: 0x00076086
		// (set) Token: 0x06001F43 RID: 8003 RVA: 0x00077E8E File Offset: 0x0007608E
		[DefaultValue(false)]
		[Category("Appearance")]
		[Description("Indicates whether shortcuts on filter controls (in savable mode) should be displayed.")]
		[Browsable(false)]
		public bool FilterShortCuts
		{
			get
			{
				return this.filterShortCuts;
			}
			set
			{
				if (this.filterEditor != null)
				{
					this.filterEditor.ShowShortcut = value;
				}
				this.filterShortCuts = value;
			}
		}

		// Token: 0x17000AEB RID: 2795
		// (get) Token: 0x06001F44 RID: 8004 RVA: 0x00077EAB File Offset: 0x000760AB
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Ext. Property")]
		[Description("The properties of the grid callback commands.")]
		public PXGridCallbacks CallbackCommands
		{
			get
			{
				if (this.callbacks == null)
				{
					this.callbacks = new PXGridCallbacks(this, base.IsTrackingViewState);
				}
				return this.callbacks;
			}
		}

		// Token: 0x17000AEC RID: 2796
		// (get) Token: 0x06001F45 RID: 8005 RVA: 0x00077ECD File Offset: 0x000760CD
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Editor("PX.Web.UI.Design.PXParametersEditor", typeof(UITypeEditor))]
		[Category("Ext. Property")]
		[Description("The collection of parameters for page editing.")]
		public PXParamCollection EditPageParams
		{
			get
			{
				if (this.editPageParams == null)
				{
					this.editPageParams = new PXParamCollection(this);
					if (base.IsTrackingViewState)
					{
						((IStateManager)this.editPageParams).TrackViewState();
					}
				}
				return this.editPageParams;
			}
		}

		// Token: 0x17000AED RID: 2797
		// (get) Token: 0x06001F46 RID: 8006 RVA: 0x00077EFC File Offset: 0x000760FC
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Base Property")]
		[Description("The layout properties for the row template form.")]
		public PXLayoutSettings ContentLayout
		{
			get
			{
				if (this.contentLayout == null)
				{
					this.contentLayout = new PXLayoutSettings(SpacingDirection.Around);
				}
				return this.contentLayout;
			}
		}

		// Token: 0x17000AEE RID: 2798
		// (get) Token: 0x06001F47 RID: 8007 RVA: 0x00077F18 File Offset: 0x00076118
		[DefaultValue(null)]
		[ScriptBrowsable]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Ext. Property")]
		[Description("The settings of the grid row change callback request.")]
		public PXCallbackSettingsExt AutoCallBack
		{
			get
			{
				if (this.autoCallBack == null)
				{
					this.autoCallBack = new PXCallbackSettingsExt(this, base.IsTrackingViewState);
				}
				return this.autoCallBack;
			}
		}

		// Token: 0x17000AEF RID: 2799
		// (get) Token: 0x06001F48 RID: 8008 RVA: 0x00077F3A File Offset: 0x0007613A
		[DefaultValue(null)]
		[ScriptBrowsable]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Ext. Property")]
		[Description("The settings of the grid data change callback request.")]
		public PXCallbackSettings OnChangeCommand
		{
			get
			{
				if (this.onChangeCommand == null)
				{
					this.onChangeCommand = new PXCallbackSettings(this, base.IsTrackingViewState);
				}
				return this.onChangeCommand;
			}
		}

		// Token: 0x17000AF0 RID: 2800
		// (get) Token: 0x06001F49 RID: 8009 RVA: 0x00077F5C File Offset: 0x0007615C
		[NotifyParentProperty(true)]
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Browsable(false)]
		[Editor("PX.Web.UI.Design.PXToolBarItemsEditor", typeof(UITypeEditor))]
		[Description("The collection of the additional toolbar items in files dialog.")]
		public virtual PXToolBarItemCollection FilesDialogToolbarItems
		{
			get
			{
				if (this.filesDialogToolbarItems == null)
				{
					this.filesDialogToolbarItems = new PXToolBarItemCollection(this);
				}
				return this.filesDialogToolbarItems;
			}
		}

		// Token: 0x17000AF1 RID: 2801
		// (get) Token: 0x06001F4A RID: 8010 RVA: 0x00077F78 File Offset: 0x00076178
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Base Property")]
		[Description("The properties of the default level mode.")]
		public PXGridLevelMode Mode
		{
			get
			{
				if (this.mode == null)
				{
					this.mode = new PXGridLevelMode(base.IsTrackingViewState);
				}
				return this.mode;
			}
		}

		// Token: 0x17000AF2 RID: 2802
		// (get) Token: 0x06001F4B RID: 8011 RVA: 0x00077F99 File Offset: 0x00076199
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Base Property")]
		[Description("The properties of the default level appearance.")]
		public PXGridLevelLayout Layout
		{
			get
			{
				if (this.layout == null)
				{
					this.layout = new PXGridLevelLayout(base.IsTrackingViewState, this);
				}
				return this.layout;
			}
		}

		// Token: 0x17000AF3 RID: 2803
		// (get) Token: 0x06001F4C RID: 8012 RVA: 0x00077FBB File Offset: 0x000761BB
		// (set) Token: 0x06001F4D RID: 8013 RVA: 0x00077FDC File Offset: 0x000761DC
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Appearance")]
		[Description("The properties of the default level styles.")]
		[Browsable(false)]
		public PXGridLevelStyles LevelStyles
		{
			get
			{
				if (this.levelStyles == null)
				{
					this.levelStyles = new PXGridLevelStyles(base.IsTrackingViewState);
				}
				return this.levelStyles;
			}
			internal set
			{
				this.levelStyles = value;
			}
		}

		// Token: 0x17000AF4 RID: 2804
		// (get) Token: 0x06001F4E RID: 8014 RVA: 0x00077FE5 File Offset: 0x000761E5
		// (set) Token: 0x06001F4F RID: 8015 RVA: 0x00078000 File Offset: 0x00076200
		[DefaultValue(null)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Appearance")]
		[Description("The properties of the images of the default grid level.")]
		[Browsable(false)]
		public PXGridLevelImages Images
		{
			get
			{
				if (this.images == null)
				{
					this.images = new PXGridLevelImages();
				}
				return this.images;
			}
			internal set
			{
				this.images = value;
			}
		}

		// Token: 0x06001F50 RID: 8016 RVA: 0x0007800C File Offset: 0x0007620C
		protected override void OnInit(EventArgs e)
		{
			ControlHelper.StoreControlInCache(this.Page, this);
			PageInfo.Current.AddBoundControl(this);
			this.Columns.CaptureAspxColumns();
			base.OnInit(e);
			if (this.levels == null || this.levels.Count == 0)
			{
				this.Levels.Add(new PXGridLevel());
				ControlHelper.SerializeProp(this, new string[]
				{
					"Levels"
				});
			}
			if (this.AllowPaging && this.ActionBar.PagerSettings.Mode >= GridPagerMode.Numeric)
			{
				PXToolBarLabel pxtoolBarLabel = new PXToolBarLabel
				{
					Key = "countWarn",
					Visible = false
				};
				pxtoolBarLabel.ActionBar.ToolBarVisible = ActionVisible.Bottom;
				pxtoolBarLabel.ActionBar.GroupIndex = (pxtoolBarLabel.ActionBar.Order = 0);
				WebControl webControl = new WebControl(HtmlTextWriterTag.Div);
				webControl.Attributes["error"] = 1.ToString();
				webControl.ToolTip = Msg.GetLocal("System was not able to get record count. Number pages mode is unavailable. Export to excel is limited to 1000 records.");
				WebControl webControl2 = ControlHelper.RenderSpriteImage(webControl, null, "control", "Warning");
				webControl2.CssClass += " count-warn";
				webControl.Controls.Add(new LiteralControl(Msg.GetLocal("System was not able to get record count.")));
				pxtoolBarLabel.Control = webControl;
				this.ActionBar.CustomItems.Insert(0, pxtoolBarLabel);
				if (this.StoredSqlTimeout)
				{
					this.CalculatePagerMode();
				}
			}
			if (this.Page != null)
			{
				base.RequiresDataBinding = false;
			}
		}

		// Token: 0x06001F51 RID: 8017 RVA: 0x00078184 File Offset: 0x00076384
		protected virtual void OnRowDataBound(PXGridRowEventArgs e)
		{
			PXGridRowEventHandler pxgridRowEventHandler = (PXGridRowEventHandler)base.Events[PXGrid.RowDataBoundEvent];
			if (pxgridRowEventHandler != null)
			{
				pxgridRowEventHandler(this, e);
			}
		}

		// Token: 0x06001F52 RID: 8018 RVA: 0x000781B4 File Offset: 0x000763B4
		protected virtual void OnInitRow(PXDBInsertEventArgs e)
		{
			PXDBInsertEventHandler pxdbinsertEventHandler = (PXDBInsertEventHandler)base.Events[PXGrid.InitRowEvent];
			if (pxdbinsertEventHandler != null)
			{
				pxdbinsertEventHandler(this, e);
			}
		}

		// Token: 0x06001F53 RID: 8019 RVA: 0x000781E4 File Offset: 0x000763E4
		protected virtual void OnRefetchRow(PXDBUpdateEventArgs e)
		{
			PXDBUpdateEventHandler pxdbupdateEventHandler = (PXDBUpdateEventHandler)base.Events[PXGrid.RefetchRowEvent];
			if (pxdbupdateEventHandler != null)
			{
				pxdbupdateEventHandler(this, e);
			}
		}

		// Token: 0x06001F54 RID: 8020 RVA: 0x00078214 File Offset: 0x00076414
		protected virtual void OnCommand(PXCommandEventArgs e)
		{
			PXCommandEventHandler pxcommandEventHandler = (PXCommandEventHandler)base.Events[PXGrid.CommandEvent];
			if (pxcommandEventHandler != null)
			{
				pxcommandEventHandler(this, e);
			}
		}

		// Token: 0x06001F55 RID: 8021 RVA: 0x00078244 File Offset: 0x00076444
		protected virtual void OnBeforeSyncState(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[PXGrid.BeforeSyncStateEvent];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		// Token: 0x06001F56 RID: 8022 RVA: 0x00078274 File Offset: 0x00076474
		protected virtual void OnAfterSyncState(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[PXGrid.AfterSyncStateEvent];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		// Token: 0x06001F57 RID: 8023 RVA: 0x000782A4 File Offset: 0x000764A4
		protected virtual void OnRowDeleting(PXDBDeleteEventArgs e)
		{
			PXDBDeleteEventHandler pxdbdeleteEventHandler = (PXDBDeleteEventHandler)base.Events[PXGrid.DeletingEvent];
			if (pxdbdeleteEventHandler != null)
			{
				pxdbdeleteEventHandler(this, e);
			}
		}

		// Token: 0x06001F58 RID: 8024 RVA: 0x000782D4 File Offset: 0x000764D4
		protected virtual void OnRowDeleted(PXDBDeletedEventArgs e)
		{
			PXDBDeletedEventHandler pxdbdeletedEventHandler = (PXDBDeletedEventHandler)base.Events[PXGrid.DeletedEvent];
			if (pxdbdeletedEventHandler != null)
			{
				pxdbdeletedEventHandler(this, e);
			}
		}

		// Token: 0x06001F59 RID: 8025 RVA: 0x00078304 File Offset: 0x00076504
		protected virtual void OnRowInserting(PXDBInsertEventArgs e)
		{
			PXDBInsertEventHandler pxdbinsertEventHandler = (PXDBInsertEventHandler)base.Events[PXGrid.InsertingEvent];
			if (pxdbinsertEventHandler != null)
			{
				pxdbinsertEventHandler(this, e);
			}
		}

		// Token: 0x06001F5A RID: 8026 RVA: 0x00078334 File Offset: 0x00076534
		protected virtual void OnRowInserted(PXDBInsertedEventArgs e)
		{
			PXDBInsertedEventHandler pxdbinsertedEventHandler = (PXDBInsertedEventHandler)base.Events[PXGrid.InsertedEvent];
			if (pxdbinsertedEventHandler != null)
			{
				pxdbinsertedEventHandler(this, e);
			}
		}

		// Token: 0x06001F5B RID: 8027 RVA: 0x00078364 File Offset: 0x00076564
		protected virtual void OnRowUpdating(PXDBUpdateEventArgs e)
		{
			PXDBUpdateEventHandler pxdbupdateEventHandler = (PXDBUpdateEventHandler)base.Events[PXGrid.UpdatingEvent];
			if (pxdbupdateEventHandler != null)
			{
				pxdbupdateEventHandler(this, e);
			}
		}

		// Token: 0x06001F5C RID: 8028 RVA: 0x00078394 File Offset: 0x00076594
		protected virtual void OnRowUpdated(PXDBUpdatedEventArgs e)
		{
			PXDBUpdatedEventHandler pxdbupdatedEventHandler = (PXDBUpdatedEventHandler)base.Events[PXGrid.UpdatedEvent];
			if (pxdbupdatedEventHandler != null)
			{
				pxdbupdatedEventHandler(this, e);
			}
		}

		// Token: 0x06001F5D RID: 8029 RVA: 0x000783C4 File Offset: 0x000765C4
		protected virtual void OnPageIndexChanging(PXPageChangeEventArgs e)
		{
			PXPageChangeEventHandler pxpageChangeEventHandler = (PXPageChangeEventHandler)base.Events[PXGrid.PageIndexChangingEvent];
			if (pxpageChangeEventHandler != null)
			{
				pxpageChangeEventHandler(this, e);
			}
		}

		// Token: 0x06001F5E RID: 8030 RVA: 0x000783F4 File Offset: 0x000765F4
		protected virtual void OnPageIndexChanged(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[PXGrid.PageIndexChangedEvent];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		// Token: 0x06001F5F RID: 8031 RVA: 0x00078424 File Offset: 0x00076624
		protected virtual void OnNoteShow(PXNoteEventArgs e)
		{
			PXNoteEventHandler pxnoteEventHandler = (PXNoteEventHandler)base.Events[PXGrid.NoteShowEvent];
			if (pxnoteEventHandler != null)
			{
				pxnoteEventHandler(this, e);
			}
		}

		// Token: 0x06001F60 RID: 8032 RVA: 0x00078454 File Offset: 0x00076654
		protected virtual void OnNoteSave(PXNoteEventArgs e)
		{
			PXNoteEventHandler pxnoteEventHandler = (PXNoteEventHandler)base.Events[PXGrid.NoteSaveEvent];
			if (pxnoteEventHandler != null)
			{
				pxnoteEventHandler(this, e);
			}
		}

		// Token: 0x06001F61 RID: 8033 RVA: 0x00078484 File Offset: 0x00076684
		protected virtual void OnFilesMenuShow(PXFilesMenuEventArgs e)
		{
			PXFilesMenuEventHandler pxfilesMenuEventHandler = (PXFilesMenuEventHandler)base.Events[PXGrid.FilesMenuShowEvent];
			if (pxfilesMenuEventHandler != null)
			{
				pxfilesMenuEventHandler(this, e);
			}
		}

		// Token: 0x06001F62 RID: 8034 RVA: 0x000784B4 File Offset: 0x000766B4
		protected virtual void OnFileSave(PXFileUploadEventArgs e)
		{
			PXFileUploadEventHandler pxfileUploadEventHandler = (PXFileUploadEventHandler)base.Events[PXGrid.FileSaveEvent];
			if (pxfileUploadEventHandler != null)
			{
				pxfileUploadEventHandler(this, e);
			}
		}

		// Token: 0x06001F63 RID: 8035 RVA: 0x000784E4 File Offset: 0x000766E4
		protected virtual void OnLayoutSave(PXGridLayoutEventArgs e)
		{
			PXGridLayoutEventHandler pxgridLayoutEventHandler = (PXGridLayoutEventHandler)base.Events[PXGrid.LayoutSaveEvent];
			if (pxgridLayoutEventHandler != null)
			{
				pxgridLayoutEventHandler(this, e);
			}
		}

		// Token: 0x06001F64 RID: 8036 RVA: 0x00078514 File Offset: 0x00076714
		protected virtual void OnLayoutReset(PXGridLayoutEventArgs e)
		{
			PXGridLayoutEventHandler pxgridLayoutEventHandler = (PXGridLayoutEventHandler)base.Events[PXGrid.LayoutResetEvent];
			if (pxgridLayoutEventHandler != null)
			{
				pxgridLayoutEventHandler(this, e);
			}
		}

		// Token: 0x06001F65 RID: 8037 RVA: 0x00078544 File Offset: 0x00076744
		protected virtual void OnLayoutLoad(PXGridLayoutEventArgs e)
		{
			PXGridLayoutEventHandler pxgridLayoutEventHandler = (PXGridLayoutEventHandler)base.Events[PXGrid.LayoutLoadEvent];
			if (pxgridLayoutEventHandler != null)
			{
				pxgridLayoutEventHandler(this, e);
			}
		}

		// Token: 0x06001F66 RID: 8038 RVA: 0x00078574 File Offset: 0x00076774
		protected virtual void OnCommitChanges(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[PXGrid.CommitChangesEvent];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		// Token: 0x06001F67 RID: 8039 RVA: 0x000785A4 File Offset: 0x000767A4
		protected virtual void OnColumnsGenerated(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[PXGrid.ColumnsGeneratedEvent];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		// Token: 0x06001F68 RID: 8040 RVA: 0x000785D4 File Offset: 0x000767D4
		protected virtual void OnBeforeNavigate(PXGridNavigateEventArgs e)
		{
			PXGridNavigateEventHandler pxgridNavigateEventHandler = (PXGridNavigateEventHandler)base.Events[PXGrid.BeforeNavigateEvent];
			if (pxgridNavigateEventHandler != null)
			{
				pxgridNavigateEventHandler(this, e);
			}
		}

		// Token: 0x06001F69 RID: 8041 RVA: 0x00078604 File Offset: 0x00076804
		protected virtual void OnBeforeGenerateColumns(EventArgs e)
		{
			EventHandler eventHandler = (EventHandler)base.Events[PXGrid.BeforeGenerateColumnsEvent];
			if (eventHandler != null)
			{
				eventHandler(this, e);
			}
		}

		// Token: 0x06001F6A RID: 8042 RVA: 0x00078634 File Offset: 0x00076834
		protected virtual void OnBeforeExportToExcel(PXGridExportToExcelEventArgs e)
		{
			PXGridExportToExcelEventHandler pxgridExportToExcelEventHandler = (PXGridExportToExcelEventHandler)base.Events[PXGrid.BeforeExportToExcelEvent];
			if (pxgridExportToExcelEventHandler != null)
			{
				pxgridExportToExcelEventHandler(this, e);
			}
		}

		// Token: 0x06001F6B RID: 8043 RVA: 0x00078664 File Offset: 0x00076864
		private PXGraph CloneDataGraph()
		{
			PXGraph pxgraph = this.DataGraph.Clone(true);
			PXView pxview = this.DataGraph.Views[this.DataMember];
			PXCache cache = pxgraph.Caches[pxview.CacheType];
			foreach (object obj in this.Columns)
			{
				PXGridColumn pxgridColumn = (PXGridColumn)obj;
				PXUIFieldAttribute.SetVisible(cache, null, pxgridColumn.DataField, pxgridColumn.Visible);
				string displayName = PXUIFieldAttribute.GetDisplayName(cache, pxgridColumn.DataField);
				if (pxgridColumn.Visible && !string.Equals(displayName, pxgridColumn.Header.Text, StringComparison.Ordinal))
				{
					PXUIFieldAttribute.SetDisplayName(cache, pxgridColumn.DataField, pxgridColumn.Header.Text);
				}
			}
			return pxgraph;
		}

		// Token: 0x06001F6C RID: 8044 RVA: 0x00078750 File Offset: 0x00076950
		private void ExecuteExportProcess(PXGrid.ExportProcessInfo info)
		{
			string a = info.Command.ToLower();
			if (!(a == "start"))
			{
				if (!(a == "abort"))
				{
					a == "checkstatus";
					return;
				}
				PXLongOperation.AsyncAbort(info.UID);
				return;
			}
			else
			{
				PXGridExportToExcelEventArgs pxgridExportToExcelEventArgs = new PXGridExportToExcelEventArgs();
				this.OnBeforeExportToExcel(pxgridExportToExcelEventArgs);
				if (pxgridExportToExcelEventArgs.Cancel)
				{
					return;
				}
				this.ExcelExportTop = pxgridExportToExcelEventArgs.ExportTop;
				PXExecutionContext context = new PXExecutionContext(PXExecutionContext.Current);
				context.Bag.Add("GRID_FILTERS", this.ReadFiltersFromSession());
				string type = info.Type;
				string urlForUpdates = null;
				if (!string.IsNullOrEmpty(type))
				{
					urlForUpdates = this.GetUrlForUpdates();
				}
				string screenId = this.GetScreenIdForExport();
				string company = PXAccess.GetCompanyName() ?? string.Empty;
				IEnumerable<PXGridViewParameters> prepareParameters = this.ReadControlsValues();
				IEnumerable<GridExportFilter> prepareFilters = this.ReadFiltersValues();
				bool isGenInq = this.IsGenericInq;
				string caption = this.GetScreenCaption();
				PXGraph graphClone = this.CloneDataGraph();
				UserSessionHelper userSessionHelper = UserSessionHelper.Capture();
				PXLongOperation.StartOperation(info.UID, delegate()
				{
					bool allowPaging = this.AllowPaging;
					try
					{
						Thread.Sleep(5000);
						this.AllowPaging = false;
						using (PXExecutionContext.Scope.Instantiate(context))
						{
							PXGraph.ProxyIsActive = true;
							PXGraph.ExportIsActive = true;
							PXDatabase.ReadBranchRestricted = false;
							PXDataSource pxdataSource = this.GetDataSource() as PXDataSource;
							if (pxdataSource != null)
							{
								pxdataSource.BypassFetchData = false;
								pxdataSource._DataGraph = graphClone;
							}
							this.BypassNoteLoading = !this.ExportNotes;
							this.ExcelExport = true;
							if (!PageInfo.Current.DataboundControls.Contains(this))
							{
								PageInfo.Current.DataboundControls.Add(this);
							}
							this.StoreSortsAndFilters();
							this.DataBind();
							this.SaveExportData(userSessionHelper, urlForUpdates, screenId, company, prepareParameters, prepareFilters, isGenInq, caption);
							this.ExcelExport = false;
							this.BypassNoteLoading = false;
						}
					}
					finally
					{
						this.AllowPaging = allowPaging;
					}
				});
				return;
			}
		}

		// Token: 0x06001F6D RID: 8045 RVA: 0x00078898 File Offset: 0x00076A98
		private string GetScreenCaption()
		{
			PXSiteMapNode currentNode = PXSiteMap.CurrentNode;
			if (this.IsGenericInq && ((currentNode != null) ? currentNode.ScreenID : null) == "00000000")
			{
				return this.Page.Title;
			}
			return currentNode.With((PXSiteMapNode _) => _.Title) ?? this.Caption;
		}

		// Token: 0x06001F6E RID: 8046 RVA: 0x00078906 File Offset: 0x00076B06
		private string GetScreenIdForExport()
		{
			if (!this.IsGenericInq)
			{
				return PXContext.GetScreenID();
			}
			return PXContext.GetSlot<Guid?>("__GEN_INQ_DESIGN_ID__").With((Guid? _) => _.Value.ToString());
		}

		// Token: 0x06001F6F RID: 8047 RVA: 0x00078944 File Offset: 0x00076B44
		internal static int DefaultColWidth(TypeCode type, int maxLen)
		{
			int num = 70;
			if (type != TypeCode.Boolean)
			{
				switch (type)
				{
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					num = 100;
					break;
				case TypeCode.DateTime:
					num = 90;
					break;
				case TypeCode.String:
					if (maxLen > 10)
					{
						num = 280;
						if (maxLen <= 150)
						{
							num = 250;
						}
						if (maxLen <= 100)
						{
							num = 220;
						}
						if (maxLen <= 50)
						{
							num = 180;
						}
						if (maxLen <= 30)
						{
							num = 140;
						}
					}
					else
					{
						num = maxLen * 12;
					}
					if (num < 70)
					{
						num = 70;
					}
					if (num > 280)
					{
						num = 280;
					}
					break;
				}
			}
			else
			{
				num = 60;
			}
			return num;
		}

		// Token: 0x06001F70 RID: 8048 RVA: 0x000789E8 File Offset: 0x00076BE8
		internal static void SetColumnProperties(PXGridColumn col, PXFieldState fs)
		{
			TypeCode typeCode = Type.GetTypeCode(fs.DataType);
			col.DataField = fs.Name;
			col.DataType = typeCode;
			col.AllowNull = fs.Nullable;
			col.AllowUpdate = !fs.IsReadOnly;
			col.MaxLength = ((fs.Length > 0) ? fs.Length : 0);
			col.Decimals = ((fs.Precision > 0) ? fs.Precision : 0);
			PXGrid.CalculateColumnWidth(col, fs);
			if (typeCode == TypeCode.Boolean)
			{
				col.Type = GridColumnType.CheckBox;
				col.TextAlign = HorizontalAlign.Center;
			}
		}

		// Token: 0x06001F71 RID: 8049 RVA: 0x00078A78 File Offset: 0x00076C78
		internal static void CalculateColumnWidth(PXGridColumn col, PXFieldState fs)
		{
			int num = (fs.Length > 0) ? fs.Length : 0;
			PXSegmentedState pxsegmentedState = fs as PXSegmentedState;
			if (pxsegmentedState != null && pxsegmentedState != null && pxsegmentedState.Segments.Length != 0)
			{
				num = 0;
				foreach (PXSegment pxsegment in pxsegmentedState.Segments)
				{
					num += (int)pxsegment.Length;
				}
			}
			PXStringState pxstringState = fs as PXStringState;
			if (pxstringState != null)
			{
				string[] allowedLabels = pxstringState.AllowedLabels;
				if (allowedLabels != null && allowedLabels.Length != 0)
				{
					foreach (string text in pxstringState.AllowedLabels)
					{
						if (text != null && text.Length > num)
						{
							num = text.Length;
						}
					}
				}
			}
			PXIntState pxintState = fs as PXIntState;
			if (pxintState != null)
			{
				string[] allowedLabels3 = pxintState.AllowedLabels;
				if (allowedLabels3 != null && allowedLabels3.Length != 0)
				{
					foreach (string text2 in pxintState.AllowedLabels)
					{
						if (text2 != null && text2.Length > num)
						{
							num = text2.Length;
						}
					}
				}
			}
			if (pxintState != null && num == 0 && (fs.SelectorMode & PXSelectorMode.DisplayModeText) != PXSelectorMode.Undefined)
			{
				num = 20;
			}
			int num2 = PXGrid.DefaultColWidth(((fs.SelectorMode & PXSelectorMode.DisplayModeText) != PXSelectorMode.Undefined) ? TypeCode.String : col.DataType, num);
			bool flag = col.DataType == TypeCode.Boolean;
			bool flag2 = string.IsNullOrEmpty(col.Header.Text) || (flag && col.AllowCheckAll);
			if (!flag2 && (col.DataType == TypeCode.String || flag))
			{
				int num3 = col.Header.Text.Split(new char[]
				{
					' '
				}).Max(delegate(string s)
				{
					if (s == null)
					{
						return 0;
					}
					return s.Length;
				});
				num2 = Math.Max(num2, (int)Math.Ceiling(7.0 * (double)((num3 > 20) ? 20 : num3)) + 35);
				if (flag && num2 > 80)
				{
					num2 = 80;
				}
			}
			else if (flag2 && flag)
			{
				num2 = 30;
			}
			col.Width = ((num2 > 280) ? 280 : num2);
		}

		// Token: 0x06001F72 RID: 8050 RVA: 0x00078CA0 File Offset: 0x00076EA0
		protected internal PXGridRow GetRowByID(string id)
		{
			if (id == null || id.Length == 0)
			{
				return null;
			}
			id = id.Substring(this.ClientID.Length + 1);
			string[] array = id.Split(new char[]
			{
				'_'
			});
			PXGridRow pxgridRow = null;
			PXGridRowCollection pxgridRowCollection = this.Rows;
			for (int i = 1; i < array.Length; i++)
			{
				int num = int.Parse(array[i]);
				if (num < 0 || num >= pxgridRowCollection.Count)
				{
					break;
				}
				pxgridRow = pxgridRowCollection[num];
				if (pxgridRow != null && pxgridRow.Expandable && i < array.Length - 1)
				{
					pxgridRowCollection = pxgridRow.Rows;
				}
				else if (i < array.Length - 1)
				{
					pxgridRow = null;
					break;
				}
			}
			return pxgridRow;
		}

		// Token: 0x06001F73 RID: 8051 RVA: 0x00078D43 File Offset: 0x00076F43
		internal void ResetColumnsGenerated()
		{
			this.IsColumnsGenerated = false;
			this.columnsSynchronized = false;
		}

		// Token: 0x06001F74 RID: 8052 RVA: 0x00078D53 File Offset: 0x00076F53
		internal IDataSource GetDataSourceInternal()
		{
			return this.GetDataSource();
		}

		// Token: 0x06001F75 RID: 8053 RVA: 0x00078D5B File Offset: 0x00076F5B
		internal DataSourceView GetDataInternal()
		{
			return this.GetDataView();
		}

		// Token: 0x06001F76 RID: 8054 RVA: 0x00078D64 File Offset: 0x00076F64
		internal void SetProperties(PXGridProperties gp, bool appearance)
		{
			this.AllowPaging = gp.AllowPaging;
			this.AllowSearch = gp.AllowSearch;
			this.PageSize = gp.PageSize;
			this.PageIndex = gp.PageIndex;
			this.FastFilterFields = gp.FastFilterFields;
			this.PrimaryLevel.Columns = gp.Columns;
			this.IsColumnsGenerated = false;
			if (appearance)
			{
				this.layout = gp.Layout;
				this.ActionBar.PagerSettings = gp.PagerSettings;
				this.ActionBar.ToolBarSkin = gp.ToolBarSkin;
				this.ActionBar.ActionsText = gp.ActionsText;
			}
		}

		// Token: 0x17000AF5 RID: 2805
		// (get) Token: 0x06001F77 RID: 8055 RVA: 0x00078E07 File Offset: 0x00077007
		// (set) Token: 0x06001F78 RID: 8056 RVA: 0x00078E0F File Offset: 0x0007700F
		internal bool BypassNoteLoading { get; set; }

		// Token: 0x17000AF6 RID: 2806
		// (get) Token: 0x06001F79 RID: 8057 RVA: 0x00078E18 File Offset: 0x00077018
		// (set) Token: 0x06001F7A RID: 8058 RVA: 0x00078E20 File Offset: 0x00077020
		internal bool ExcelExport { get; set; }

		// Token: 0x17000AF7 RID: 2807
		// (get) Token: 0x06001F7B RID: 8059 RVA: 0x00078E29 File Offset: 0x00077029
		// (set) Token: 0x06001F7C RID: 8060 RVA: 0x00078E31 File Offset: 0x00077031
		internal int ExcelExportTop { get; set; }

		// Token: 0x17000AF8 RID: 2808
		// (get) Token: 0x06001F7D RID: 8061 RVA: 0x00078E3A File Offset: 0x0007703A
		// (set) Token: 0x06001F7E RID: 8062 RVA: 0x00078E4D File Offset: 0x0007704D
		[Category("Behavior")]
		[DefaultValue(true)]
		[Description("Indicates whether client-side scripts are enabled.")]
		[Browsable(false)]
		public bool EnableClientScript
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "EnableScript", true);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "EnableScript", value, true);
			}
		}

		// Token: 0x17000AF9 RID: 2809
		// (get) Token: 0x06001F7F RID: 8063 RVA: 0x00078E61 File Offset: 0x00077061
		string IPXScriptControl.ClientClassName
		{
			get
			{
				return this.GetClassName();
			}
		}

		// Token: 0x06001F80 RID: 8064 RVA: 0x00078E69 File Offset: 0x00077069
		protected virtual string GetClassName()
		{
			return base.GetType().Name;
		}

		// Token: 0x17000AFA RID: 2810
		// (get) Token: 0x06001F81 RID: 8065 RVA: 0x00078E76 File Offset: 0x00077076
		// (set) Token: 0x06001F82 RID: 8066 RVA: 0x00078E89 File Offset: 0x00077089
		ScriptRegisterFlag IPXScriptControl.RegisterFlags
		{
			get
			{
				if (this.scriptFlags == ScriptRegisterFlag.NotSet)
				{
					return ScriptRegisterFlag.All;
				}
				return this.scriptFlags;
			}
			set
			{
				this.scriptFlags = value;
			}
		}

		// Token: 0x17000AFB RID: 2811
		// (get) Token: 0x06001F83 RID: 8067 RVA: 0x00078E92 File Offset: 0x00077092
		bool IPXScriptControl.RenderTemplateData
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06001F84 RID: 8068 RVA: 0x00078E98 File Offset: 0x00077098
		void IPXScriptControl.RegisterModules(JSManager sm)
		{
			sm.RegisterModule(JS.NetTypeKey, "PX.Web.UI.Scripts.px_netType.js");
			sm.RegisterModule(typeof(PXCheckBox), "PX.Web.UI.Scripts.px_checkBox.js");
			sm.RegisterModule(typeof(PXTextEdit), "PX.Web.UI.Scripts.px_textEdit.js");
			sm.RegisterModule(typeof(PXMaskEdit), "PX.Web.UI.Scripts.px_maskEdit.js");
			sm.RegisterModule(typeof(PXNumberEdit), "PX.Web.UI.Scripts.px_numbEdit.js");
			sm.RegisterModule(typeof(PXComboBox), "PX.Web.UI.Scripts.px_comboBox.js");
			sm.RegisterModule(typeof(PXDropDown), "PX.Web.UI.Scripts.px_dropDown.js");
			sm.RegisterModule(typeof(PXSelectorBase), "PX.Web.UI.Scripts.px_selectorBase.js");
			sm.RegisterModule(typeof(PXSelector), "PX.Web.UI.Scripts.px_selector.js");
			sm.RegisterModule(typeof(PXSegmentMask), "PX.Web.UI.Scripts.px_segmentMask.js");
			sm.RegisterModule(typeof(PXButtonEdit), "PX.Web.UI.Scripts.px_buttonEdit.js");
			sm.RegisterModule(typeof(PXDateTimeEdit), "PX.Web.UI.Scripts.px_dateEdit.js");
			sm.RegisterModule(typeof(PXSmartPanel), "PX.Web.UI.Scripts.px_smartPanel.js");
			sm.RegisterModule(typeof(PXInputBox), "PX.Web.UI.Scripts.px_inputBox.js");
			sm.RegisterModule(typeof(PXActionBar), "PX.Web.UI.Scripts.px_actionBar.js");
			sm.RegisterModule(typeof(PXFilterEditor), "PX.Web.UI.Scripts.px_filterEditor.js");
			sm.RegisterModule(typeof(PXBoundPanel), "PX.Web.UI.Scripts.px_boundPanel.js");
			sm.RegisterModule(typeof(PXFormView), "PX.Web.UI.Scripts.px_formView.js");
			sm.RegisterModule(typeof(PXFilterEditor), "PX.Web.UI.Scripts.px_filterDialog.js");
			sm.RegisterModule(typeof(PXColumnsDialog), "PX.Web.UI.Scripts.px_columnsDialog.js");
			sm.RegisterModule(typeof(PXGrid), "PX.Web.UI.Scripts.px_inlineEdit.js");
			sm.RegisterModule(typeof(PXPivotTable), "PX.Web.UI.Scripts.px_pivotTable.js");
			if (!string.IsNullOrEmpty(this.FilesField))
			{
				sm.RegisterModule(typeof(PXFileUpload), "PX.Web.UI.Scripts.px_fileUpload.js");
				sm.RegisterModule(typeof(PXUploadDialog), "PX.Web.UI.Scripts.px_uploadDialog.js");
			}
			Type typeFromHandle = typeof(PXGrid);
			sm.RegisterModule(typeFromHandle, "PX.Web.UI.Scripts.px_grid.js");
			sm.RegisterModule(typeFromHandle, "PX.Web.UI.Scripts.px_gridEvents.js");
			sm.RegisterModule(typeFromHandle, "PX.Web.UI.Scripts.px_gridLevel.js");
			sm.RegisterModule(typeFromHandle, "PX.Web.UI.Scripts.px_gridColumn.js");
			sm.RegisterModule(typeFromHandle, "PX.Web.UI.Scripts.px_gridRow.js");
			sm.RegisterModule(typeFromHandle, "PX.Web.UI.Scripts.px_gridCell.js");
			sm.RegisterModule(typeFromHandle, "PX.Web.UI.Scripts.px_gridEdit.js");
			sm.RegisterModule(typeFromHandle, "PX.Web.UI.Scripts.px_gridDialogs.js");
		}

		// Token: 0x06001F85 RID: 8069 RVA: 0x00079110 File Offset: 0x00077310
		void IPXScriptControl.RegisterProperties(JSObject obj)
		{
			if (obj.BaseObject == this)
			{
				if (!JSManager.TemplateMode)
				{
					this.CalculateNoteFileIndicators();
				}
				if (this.ContextMenuID.Length > 0)
				{
					PXMenu pxmenu = ControlHelper.FindControl(this, this.ContextMenuID) as PXMenu;
					if (pxmenu == null)
					{
						pxmenu = (ControlHelper.FindControl(this.ContextMenuID, this.Page) as PXMenu);
					}
					if (pxmenu != null)
					{
						obj.Append("ContextMenuID", pxmenu.ClientID);
					}
				}
				if (this.FastFilterID.Length > 0)
				{
					IFieldEditor fieldEditor = ControlHelper.FindControl(this.FastFilterID, this.Page) as IFieldEditor;
					if (fieldEditor != null)
					{
						obj.Append("FastFilterID", fieldEditor.ClientID);
					}
				}
				if (this.ShowFilterToolbarFinal)
				{
					obj.Append("IsSharedFilterEditable", this.IsSharedFiltersEditable, false);
				}
				obj.Append("LayoutLoaded", this.layoutLoaded, false);
				obj.Append("DefaultAction", this.ActionBar.DefaultAction, string.Empty);
				obj.Append("AllowNote", this.AllowNote, true);
				if (this.PreservePageFinal)
				{
					obj.Append("PreservePage", true);
				}
				if (this.ActionBar.PagerVisible != ActionVisible.False)
				{
					obj.Append("PagerMode", this.ActionBar.PagerSettings.Mode, GridPagerMode.NextPrevFirstLast);
				}
				if (this.AllowSearch)
				{
					string value = this.StyleManager.ResolveCssClass(PXGrid.GridStyle.SearchEditor);
					string value2 = this.StyleManager.ResolveCssClass(PXGrid.GridStyle.SearchText);
					obj.Append("SearchCss", value, string.Empty);
					obj.Append("SearchTextCss", value2, string.Empty);
				}
				obj.Append("GridViewText", Msg.GetLocal("Grid View"));
				if (this.FilterID != null)
				{
					obj.Append("FilterID", this.FilterID.Value.ToString());
				}
				ClientScriptManager clientScript = this.Page.ClientScript;
				obj.Append("XslFile", clientScript.GetWebResourceUrl(typeof(PXGrid), "PX.Web.UI.Xslt.px_grid.xslt"));
				obj.Append("XslEditFile", clientScript.GetWebResourceUrl(typeof(PXGrid), "PX.Web.UI.Xslt.px_gridEdit.xslt"));
				if (!string.IsNullOrEmpty(this.EditPageUrl))
				{
					obj.Append("HasEditPage", true);
				}
				obj.Append("Callbacks", ((IPXCallbackHandler)this).CallbackCommands);
				obj.Append("PasteCommand", this.CallbackCommands.PasteCommand, string.Empty);
				obj.Append("RecordNumMsg", Msg.GetLocal("{0} of {1} records"));
				obj.Append("EditStr", Msg.GetLocal("Edit"));
				if (this.UploadCommandState.Enabled)
				{
					obj.Append("uploadCommand", this.DataMember + "$Upload");
				}
				obj.Append("exportCommand", this.ExportCommandState.Enabled);
				obj.Append("externalToolBarID", this.ExternalToolBarID, string.Empty);
				if (!string.IsNullOrEmpty(this.FileUploaderId))
				{
					obj.Append("fileDialogId", this.ExternalUploader.With((PXFilesDialog _) => _.ClientID), string.Empty);
				}
				obj.Append("timeStr", Msg.GetLocal("Time"));
				obj.Append("rowChangedMsg", Msg.GetLocal("Data in the current row has been changed. Do you want to save changes?"));
				obj.Append("gridChangedMsg", Msg.GetLocal("The grid has unsaved changes. Do you want to save changes?"));
				obj.Append("filterAppliedMsg", Msg.GetLocal("Filter Applied"));
				obj.Append("filterAllMsg", Msg.GetLocal("All"));
				obj.Append("filterEmptyMsg", Msg.GetLocal("Is Empty"));
				obj.Append("filterNameMsg", Msg.GetLocal("Enter new filter name here"));
				obj.Append("filterRemoveMsg", Msg.GetLocal("The current filter with all conditions will be deleted"));
				obj.Append("filterChangesInSharedMsg", Msg.GetLocal("You have modified the shared filter. Do you want to save your changes?"));
				if (!string.IsNullOrEmpty(this.DataMember))
				{
					obj.Append("dataMember", this.DataMember);
				}
				if (JSManager.TemplateMode)
				{
					obj.Append("filterNames", PXEnumDescriptionAttribute.GetNames(typeof(PXCondition)));
				}
				if (this.AllowPivotTable)
				{
					obj.Append("EditPivotTableUrl", base.ResolveUrl(this.EditPivotTableUrl), string.Empty);
					if (this.filterPivots != null)
					{
						obj.Append("FilterPivots", this.filterPivots);
						return;
					}
				}
			}
			else
			{
				if (obj.BaseObject is PXGridLevel)
				{
					PXGridLevel pxgridLevel = (PXGridLevel)obj.BaseObject;
					string nullText = pxgridLevel.LayoutFinal.NullText;
					obj.Append("DataKey", string.Join(",", pxgridLevel.DataKeyNames), string.Empty);
					if (nullText.Length > 0)
					{
						obj.Append("NullText", nullText);
					}
					JSObject jsobject = (JSObject)JSConverter.Convert(pxgridLevel.ModeFinal, this);
					jsobject.Append("AllowAddNew", pxgridLevel.GetAllowAddNew(), true);
					jsobject.Append("AllowUpdate", pxgridLevel.GetAllowUpdate(), true);
					jsobject.Append("AllowDelete", pxgridLevel.GetAllowDelete(), true);
					jsobject.Append("AllowSort", pxgridLevel.GetAllowSort(), true);
					obj.Append("Mode", jsobject);
					return;
				}
				if (obj.BaseObject is PXGridColumn)
				{
					PXGridColumn pxgridColumn = (PXGridColumn)obj.BaseObject;
					obj.Append("HeaderAction", pxgridColumn.Header.ClickAction, GridHeaderAction.NotSet);
					obj.Append("FooterSummary", pxgridColumn.Footer.SummaryType, GridFooterSummary.NotSet);
					obj.Append("TextFieldColumn", pxgridColumn.TextFieldColumn, string.Empty);
					if (!pxgridColumn.Visible)
					{
						obj.Append("Width", pxgridColumn.Width);
					}
					if (this.MarkRequired != MarkRequiredMode.False)
					{
						obj.Append("Required", pxgridColumn.Required, false);
					}
					if (!string.IsNullOrEmpty(pxgridColumn.EditorID))
					{
						Control control = pxgridColumn.Level.RowTemplateContainer.FindControl(pxgridColumn.EditorID);
						if (control == null)
						{
							control = ControlHelper.FindControl(pxgridColumn.EditorID, this.Page);
						}
						if (control != null)
						{
							obj.Append("EditorID", control.ClientID, string.Empty);
							return;
						}
					}
					else if (pxgridColumn.DataType == TypeCode.DateTime)
					{
						DateTimeFormatInfo dateTimeFormat = CultureInfo.CurrentUICulture.DateTimeFormat;
						obj.Append("DateMask", PXDateTimeEdit.MakeEncodedPattern(pxgridColumn.DisplayFormat, dateTimeFormat));
					}
				}
			}
		}

		// Token: 0x17000AFC RID: 2812
		// (get) Token: 0x06001F86 RID: 8070 RVA: 0x00079808 File Offset: 0x00077A08
		// (set) Token: 0x06001F87 RID: 8071 RVA: 0x00079832 File Offset: 0x00077A32
		private PXGrid.ExportProcessInfo CurrentExportProcess
		{
			get
			{
				object obj;
				PXExecutionContext.Current.Bag.TryGetValue("_GRID_EXPORT_PORCESS_KEY_", out obj);
				return obj as PXGrid.ExportProcessInfo;
			}
			set
			{
				PXExecutionContext.Current.Bag["_GRID_EXPORT_PORCESS_KEY_"] = value;
			}
		}

		// Token: 0x06001F88 RID: 8072 RVA: 0x00079849 File Offset: 0x00077A49
		void IPXScriptControl.RegisterVariables(JSManager sm)
		{
		}

		// Token: 0x06001F89 RID: 8073 RVA: 0x0007984C File Offset: 0x00077A4C
		void IPXCallbackUpdatable.RegisterProperties(JSObject obj)
		{
			if (obj.BaseObject == this)
			{
				bool flag = ControlHelper.IsReloadPage(this);
				if (flag)
				{
					obj.Append("LayoutLoaded", this.layoutLoaded);
					List<PXFilterRow> filterRowsInSession = this.FilterRowsInSession;
					if (filterRowsInSession != null)
					{
						IEnumerable<PXFilterRow> enumerable = from f in filterRowsInSession
						where PXGrid.GetFilterType(f) == PXGrid.FilterRowType.Column
						select f;
						if (enumerable.Count<PXFilterRow>() > 0)
						{
							obj.Append("ColumnFilters", this.CreateFilterRowsScriptObject(enumerable, false));
						}
					}
				}
				if (this.FilterID != null)
				{
					obj.Append("FilterID", this.FilterID.Value.ToString());
				}
				bool? flag2 = this.colsFilterActive;
				bool flag3 = false;
				if (flag2.GetValueOrDefault() == flag3 & flag2 != null)
				{
					obj.Append("colsFilterActive", this.colsFilterActive);
				}
				if (!string.IsNullOrEmpty(this.FastFilter))
				{
					obj.Append("fastFilter", this.FastFilter);
				}
				if (this.StoredSqlTimeout)
				{
					obj.Append("SqlTimeout", true);
				}
				if (this.AllowFilter)
				{
					if (this.ShowFilterToolbarFinal)
					{
						bool flag4 = false;
						bool flag5;
						List<PXFilterRow> quickFilterRows = this.GetQuickFilterRows(out flag4, out flag5);
						obj.Append("IsFilterSavable", this.IsScreenInSiteMap, true);
						bool flag6;
						obj.Append("IsFilterEditable", this.IsFilterEditable(out flag6), true);
						obj.Append("IsFilterShared", flag6, false);
						obj.Append("HasAdvancedFilters", flag5, false);
						obj.Append("IsFilterChanged", flag4, false);
						if (quickFilterRows != null && quickFilterRows.Count > 0)
						{
							obj.Append("QuickFilters", this.CreateFilterRowsScriptObject(quickFilterRows, true));
						}
						if (flag && this.QuickFiltersInSession != null)
						{
							obj.Append("QuickFiltersA", this.CreateFilterRowsScriptObject(this.QuickFiltersInSession, true));
						}
					}
					else
					{
						obj.Append("HasAdvancedFilters", this.HasAdvancedFilters(), false);
					}
				}
				if (this.AllowPivotTable && (flag || this.reloadFilters))
				{
					string name = "FilterPivots";
					Dictionary<Guid, int> dictionary = this.filterPivots;
					obj.Append(name, (dictionary != null && dictionary.Count > 0) ? this.filterPivots : null);
				}
				PXDataSourceView pxdataSourceView = this.GetDataView() as PXDataSourceView;
				if (this.AllowAutoHide && pxdataSourceView != null)
				{
					((IAutoHideControl)this).CalculateVisibility();
					obj.Append("Hidden", this.hidden);
				}
				if (this.AllowSearch)
				{
					obj.Append("SearchValue", this.SearchValue, null);
				}
				if (this.SyncPositionWithGraph && this.DataGraph.Views[this.DataMember].Cache.ActiveRow != null)
				{
					if (this.ActiveRow != null)
					{
						obj.Append("ActiveRowIndex", this.ActiveRow.Index);
					}
					else if (this.PreservePageFinal && (ControlHelper.IsReloadPage(this) || this.adjustOnReload))
					{
						obj.Append("ActiveRowIndex", -1);
					}
				}
				if (this.Levels != null && this.Levels.Count > 0 && !string.IsNullOrEmpty(this.Levels[0].DataMember))
				{
					obj.Append("dataMember", this.Levels[0].DataMember);
				}
				bool flag7 = PXContext.PXIdentity.User.IsInRole(PXAccess.GetAdministratorRoles().First<string>());
				bool flag8 = false;
				if (flag7 && !string.IsNullOrEmpty(this.DataMember))
				{
					PXDataSource pxdataSource = this.GetDataSource() as PXDataSource;
					bool flag9;
					if (pxdataSource == null)
					{
						flag9 = false;
					}
					else
					{
						GridPreferences.ColumnPref[] defaultGridPreferences = pxdataSource.GetDefaultGridPreferences(this.DataMember);
						flag9 = ((defaultGridPreferences != null) ? new bool?(defaultGridPreferences.Any<GridPreferences.ColumnPref>()) : null).GetValueOrDefault();
					}
					flag8 = flag9;
				}
				if (flag7)
				{
					obj.Append("DelDefaultsVisible", true);
				}
				if (flag8)
				{
					obj.Append("DelDefaultsEnabled", true);
					return;
				}
			}
			else
			{
				if (obj.BaseObject is PXGridLevel)
				{
					PXGridLevel pxgridLevel = (PXGridLevel)obj.BaseObject;
					obj.Append("AllowAddNew", pxgridLevel.GetAllowAddNew(), true);
					obj.Append("AllowUpdate", pxgridLevel.GetAllowUpdate(), true);
					obj.Append("AllowDelete", pxgridLevel.GetAllowDelete(), true);
					obj.Append("AllowSort", pxgridLevel.GetAllowSort(), true);
					obj.Append("FooterVisible", pxgridLevel.LayoutFinal.FooterVisible, false);
					obj.Append("IsUploadEnabled", pxgridLevel.GetUploadEnabled(), true);
					if (ControlHelper.IsReloadPage(this))
					{
						obj.Append("DataKey", string.Join(",", pxgridLevel.DataKeyNames), string.Empty);
					}
					string name2 = "AllowSkipTab";
					PXBaseDataSource pxbaseDataSource = this.dataSource as PXBaseDataSource;
					obj.Append(name2, !(((pxbaseDataSource != null) ? pxbaseDataSource.DataGraph : null) is PXGenericInqGrph), true);
					return;
				}
				if (obj.BaseObject is PXGridColumn)
				{
					PXGridColumn pxgridColumn = (PXGridColumn)obj.BaseObject;
					IFieldEditor fieldEditor = null;
					if (!string.IsNullOrEmpty(pxgridColumn.FormEditorID))
					{
						fieldEditor = pxgridColumn.Level.GetFieldEditor(pxgridColumn.DataField);
						if (fieldEditor is IPXCallbackUpdatable)
						{
							JSObject callbackObject = JSManager.GetCallbackObject((IPXCallbackUpdatable)fieldEditor);
							obj.Append("Control", callbackObject);
						}
					}
					if (pxgridColumn.Type == GridColumnType.DropDownList || fieldEditor is PXDropDown)
					{
						obj.Append("DefaultValue", pxgridColumn.DefaultValue);
					}
					obj.Append("FooterText", pxgridColumn.Footer.Text, string.Empty);
					if (this.IsLayoutReset || this.IsLayoutSave)
					{
						obj.Append("ServerIndex", pxgridColumn.Index);
					}
					if (pxgridColumn.HasError)
					{
						obj.Append("HasError", pxgridColumn.HasError);
					}
					if (this.MarkRequired == MarkRequiredMode.Dynamic)
					{
						obj.Append("Required", pxgridColumn.Required, false);
					}
					if (pxgridColumn.DynamicValueItems)
					{
						obj.Append("valueItems", pxgridColumn.ValueItems.Items);
					}
				}
			}
		}

		// Token: 0x17000AFD RID: 2813
		// (get) Token: 0x06001F8A RID: 8074 RVA: 0x00079ED1 File Offset: 0x000780D1
		// (set) Token: 0x06001F8B RID: 8075 RVA: 0x00079ED4 File Offset: 0x000780D4
		bool IPXCallbackUpdatable.CallbackUpdatable
		{
			get
			{
				return true;
			}
			set
			{
			}
		}

		// Token: 0x06001F8C RID: 8076 RVA: 0x00079ED8 File Offset: 0x000780D8
		private List<JSObject> CreateFilterRowsScriptObject(IEnumerable<PXFilterRow> list, bool condDescr = false)
		{
			List<PXFilterRow> list2 = new List<PXFilterRow>();
			Func<PXFilterRow, string> func = delegate(PXFilterRow fr)
			{
				string text = (fr.Value != null) ? fr.Value.ToString() : "<null>";
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
				return "<null>";
			};
			using (IEnumerator<PXFilterRow> enumerator = list.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					PXFilterRow fr = enumerator.Current;
					PXFilterRow pxfilterRow = list2.Find((PXFilterRow f) => f.DataField == fr.DataField);
					if (pxfilterRow != null)
					{
						PXFilterRow pxfilterRow2 = new PXFilterRow(pxfilterRow);
						pxfilterRow2.Value = func(pxfilterRow2) + "," + func(fr);
						pxfilterRow2.Condition = PXCondition.IN;
						list2[list2.IndexOf(pxfilterRow)] = pxfilterRow2;
					}
					else
					{
						list2.Add(fr);
					}
				}
			}
			string[] array = condDescr ? PXEnumDescriptionAttribute.GetNames(typeof(PXCondition)) : null;
			List<JSObject> list3 = new List<JSObject>();
			foreach (PXFilterRow pxfilterRow3 in list2)
			{
				JSObject jsobject = new JSObject(pxfilterRow3);
				jsobject.Append("DataField", pxfilterRow3.DataField);
				jsobject.Append("Condition", Enum.GetName(typeof(PXCondition), pxfilterRow3.Condition));
				if (condDescr)
				{
					jsobject.Append("CondText", array[(int)pxfilterRow3.Condition]);
				}
				jsobject.Append("Value", Convert.ToString(pxfilterRow3.Value, CultureInfo.InvariantCulture));
				jsobject.Append("Value2", Convert.ToString(pxfilterRow3.Value2, CultureInfo.InvariantCulture));
				list3.Add(jsobject);
			}
			return list3;
		}

		// Token: 0x06001F8D RID: 8077 RVA: 0x0007A0C0 File Offset: 0x000782C0
		protected override object SaveViewState()
		{
			object[] array = new object[10];
			array[0] = base.SaveViewState();
			STM.SaveState(array, new object[]
			{
				this.levels,
				this.gridStyles,
				this.mode,
				this.layout,
				this.levelStyles,
				this.images,
				this.clientEvents,
				this.expandEffects,
				this.clientState
			});
			return array;
		}

		// Token: 0x06001F8E RID: 8078 RVA: 0x0007A140 File Offset: 0x00078340
		protected override void LoadViewState(object savedState)
		{
			if (savedState != null)
			{
				object[] array = (object[])savedState;
				if (array[0] != null)
				{
					base.LoadViewState(array[0]);
				}
				if (array[9] != null)
				{
					((IStateManager)this.ClientState).LoadViewState(array[9]);
				}
				this.RestoreColsOrder(null);
				STM.LoadState(array, new object[]
				{
					this.Levels,
					this.GridStyles,
					this.Mode,
					this.Layout,
					this.LevelStyles,
					this.Images,
					this.ClientEvents,
					this.ExpandEffects
				});
			}
		}

		// Token: 0x06001F8F RID: 8079 RVA: 0x0007A1D8 File Offset: 0x000783D8
		protected override void TrackViewState()
		{
			base.TrackViewState();
			STM.TrackState(new object[]
			{
				this.levels,
				this.gridStyles,
				this.mode,
				this.layout,
				this.levelStyles,
				this.images,
				this.clientEvents,
				this.expandEffects,
				this.clientState
			});
		}

		// Token: 0x17000AFE RID: 2814
		// (get) Token: 0x06001F90 RID: 8080 RVA: 0x0007A248 File Offset: 0x00078448
		string[] ICommandSource.Commands
		{
			get
			{
				return new string[]
				{
					"PageNext",
					"PagePrev",
					"PageFirst",
					"PageLast",
					"Refresh",
					"AddNew",
					"Save",
					"Delete",
					"Search",
					"AdjustColumns",
					"EditRecord",
					"NoteShow",
					"FilterShow",
					"FilterSet",
					"ExportExcel",
					"FilesMenu",
					"LayoutSave",
					"LayoutReset"
				};
			}
		}

		// Token: 0x06001F91 RID: 8081 RVA: 0x0007A2F8 File Offset: 0x000784F8
		bool ICommandSource.ExecuteCommand(PXCommandEventArgs e)
		{
			if (e.CausesValidation && this.Page != null)
			{
				this.Page.Validate(e.ValidationGroup);
				if (!this.Page.IsValid)
				{
					return false;
				}
			}
			this.OnCommand(e);
			string commandName = e.CommandName;
			if (commandName != null)
			{
				switch (commandName.Length)
				{
				case 4:
					if (commandName == "Save")
					{
						if (e.CommandArgument is string)
						{
							((IPXDataControl)this).CommitDataChanges((string)e.CommandArgument);
						}
					}
					break;
				case 7:
					if (commandName == "Refresh")
					{
						this.DataBind();
					}
					break;
				case 8:
					switch (commandName[4])
					{
					case 'L':
						if (commandName == "PageLast")
						{
							this.PerformPage((this.PageCount > 0) ? (this.PageCount - 1) : -1);
						}
						break;
					case 'N':
						if (commandName == "PageNext")
						{
							this.PerformPage(this.PageIndex + 1);
						}
						break;
					case 'P':
						if (commandName == "PagePrev")
						{
							this.PerformPage(this.PageIndex - 1);
						}
						break;
					}
					break;
				case 9:
					if (commandName == "PageFirst")
					{
						this.PerformPage(0);
					}
					break;
				case 10:
				{
					char c = commandName[0];
					if (c != 'E')
					{
						if (c == 'L')
						{
							if (commandName == "LayoutSave")
							{
								this.SaveLayout((string)e.CommandArgument);
							}
						}
					}
					else if (commandName == "EditRecord")
					{
						string url = this.EditPageParams.GetQueryUrl(this.EditPageUrl, this.Context, this);
						url = PXPageCache.FixPageUrl(ControlHelper.FixHideScriptUrl(url, null));
						this.Context.Response.Redirect(url, true);
					}
					break;
				}
				case 11:
					if (commandName == "LayoutReset")
					{
						this.ResetLayout();
					}
					break;
				}
			}
			return true;
		}

		// Token: 0x06001F92 RID: 8082 RVA: 0x0007A55C File Offset: 0x0007875C
		PXCommandState ICommandSource.GetCommandState(string commandName)
		{
			if (!this.HasDataSource || this.GetDataView() == null)
			{
				return PXCommandState.Empty;
			}
			PXGridLevel primaryLevel = this.PrimaryLevel;
			if (commandName != null)
			{
				switch (commandName.Length)
				{
				case 4:
					if (!(commandName == "Save"))
					{
						goto IL_543;
					}
					return new PXCommandState(true, this.BatchUpdate);
				case 5:
					goto IL_543;
				case 6:
				{
					char c = commandName[0];
					if (c <= 'D')
					{
						if (c != 'A')
						{
							if (c != 'D')
							{
								goto IL_543;
							}
							if (!(commandName == "Delete"))
							{
								goto IL_543;
							}
							return new PXCommandState(true, primaryLevel.GetAllowDelete() && this.Rows.Count > 0);
						}
						else
						{
							if (!(commandName == "AddNew"))
							{
								goto IL_543;
							}
							return new PXCommandState(true, primaryLevel.GetAllowAddNew());
						}
					}
					else if (c != 'S')
					{
						if (c != 'U')
						{
							goto IL_543;
						}
						if (!(commandName == "Upload"))
						{
							goto IL_543;
						}
						if (base.DesignMode || string.IsNullOrEmpty(this.DataMember))
						{
							return new PXCommandState(this.Mode.AllowUpload.GetValueOrDefault(), true);
						}
						return this.UploadCommandState;
					}
					else
					{
						if (!(commandName == "Search"))
						{
							goto IL_543;
						}
						return new PXCommandState(true, this.AllowSearch);
					}
					break;
				}
				case 7:
					if (!(commandName == "Refresh"))
					{
						goto IL_543;
					}
					return new PXCommandState(true, this.HasDataSource);
				case 8:
				{
					char c = commandName[4];
					switch (c)
					{
					case 'L':
						if (!(commandName == "PageLast"))
						{
							goto IL_543;
						}
						break;
					case 'M':
					case 'O':
						goto IL_543;
					case 'N':
						if (!(commandName == "PageNext"))
						{
							goto IL_543;
						}
						break;
					case 'P':
						if (!(commandName == "PagePrev"))
						{
							goto IL_543;
						}
						goto IL_334;
					default:
						if (c != 'S')
						{
							goto IL_543;
						}
						if (!(commandName == "NoteShow"))
						{
							goto IL_543;
						}
						this.GetDataView();
						return new PXCommandState(true, this.Rows.Count > 0 && this.AllowNote);
					}
					return new PXCommandState(true, !this.IsLastPage);
				}
				case 9:
				{
					char c = commandName[4];
					if (c <= 'P')
					{
						if (c != 'F')
						{
							if (c != 'P')
							{
								goto IL_543;
							}
							if (!(commandName == "EditPivot"))
							{
								goto IL_543;
							}
							return new PXCommandState(this.AllowPivotTable, !string.IsNullOrEmpty(this.EditPivotTableUrl));
						}
						else if (!(commandName == "PageFirst"))
						{
							goto IL_543;
						}
					}
					else if (c != 'e')
					{
						if (c != 's')
						{
							goto IL_543;
						}
						if (!(commandName == "FilesMenu"))
						{
							goto IL_543;
						}
						goto IL_462;
					}
					else
					{
						if (!(commandName == "FilterSet"))
						{
							goto IL_543;
						}
						return new PXCommandState(true, this.AllowFilter && (this.FilterID != null || this.FilterRows.Count > 0));
					}
					break;
				}
				case 10:
				{
					char c = commandName[0];
					switch (c)
					{
					case 'D':
					{
						if (!(commandName == "DelDefault"))
						{
							goto IL_543;
						}
						bool flag = PXContext.PXIdentity.User.IsInRole(PXAccess.GetAdministratorRoles().First<string>());
						bool enabled = false;
						if (flag && !string.IsNullOrEmpty(this.DataMember))
						{
							PXDataSource pxdataSource = this.GetDataSource() as PXDataSource;
							bool flag2;
							if (pxdataSource == null)
							{
								flag2 = false;
							}
							else
							{
								GridPreferences.ColumnPref[] defaultGridPreferences = pxdataSource.GetDefaultGridPreferences(this.DataMember);
								flag2 = ((defaultGridPreferences != null) ? new bool?(defaultGridPreferences.Any<GridPreferences.ColumnPref>()) : null).GetValueOrDefault();
							}
							enabled = flag2;
						}
						return new PXCommandState(flag, enabled);
					}
					case 'E':
						if (!(commandName == "EditRecord"))
						{
							goto IL_543;
						}
						return new PXCommandState(true, !string.IsNullOrEmpty(this.EditPageUrl) || this.Mode.AllowFormEdit.GetValueOrDefault());
					case 'F':
						if (commandName == "FilterShow")
						{
							return new PXCommandState(true, this.AllowFilter);
						}
						if (!(commandName == "FilterSave"))
						{
							goto IL_543;
						}
						goto IL_521;
					default:
						if (c != 'L')
						{
							goto IL_543;
						}
						if (!(commandName == "LayoutSave"))
						{
							goto IL_543;
						}
						goto IL_462;
					}
					break;
				}
				case 11:
				{
					char c = commandName[0];
					if (c != 'E')
					{
						if (c != 'L')
						{
							goto IL_543;
						}
						if (!(commandName == "LayoutReset"))
						{
							goto IL_543;
						}
						return new PXCommandState(true, this.layoutLoaded);
					}
					else
					{
						if (!(commandName == "ExportExcel"))
						{
							goto IL_543;
						}
						return new PXCommandState(true, this.Columns.Count > 0 && (this.IsPivotMode || this.Rows.Count > 0));
					}
					break;
				}
				case 12:
					if (!(commandName == "FilterRemove"))
					{
						goto IL_543;
					}
					goto IL_521;
				case 13:
					if (!(commandName == "AdjustColumns"))
					{
						goto IL_543;
					}
					return new PXCommandState(true, true);
				default:
					goto IL_543;
				}
				IL_334:
				return new PXCommandState(true, !this.IsFirstPage);
				IL_462:
				return new PXCommandState(true, true);
				IL_521:
				return new PXCommandState(true, true);
			}
			IL_543:
			return PXCommandState.Empty;
		}

		// Token: 0x17000AFF RID: 2815
		// (get) Token: 0x06001F93 RID: 8083 RVA: 0x0007AAB4 File Offset: 0x00078CB4
		internal PXCommandState UploadCommandState
		{
			get
			{
				string cmdName = this.DataMember + "$Upload";
				PXCommandState pxcommandState = (this.GetDataSource() as ICommandSource).With((ICommandSource _) => _.GetCommandState(cmdName));
				bool visible = (pxcommandState == null || pxcommandState.Visible) && this.Mode.AllowUpload.GetValueOrDefault();
				bool enabled = pxcommandState == null || pxcommandState.Enabled;
				return new PXCommandState(visible, enabled);
			}
		}

		// Token: 0x17000B00 RID: 2816
		// (get) Token: 0x06001F94 RID: 8084 RVA: 0x0007AB30 File Offset: 0x00078D30
		internal PXCommandState ExportCommandState
		{
			get
			{
				string cmdName = this.DataMember + "$ExportExcel";
				return (this.GetDataSource() as ICommandSource).Return((ICommandSource _) => _.GetCommandState(cmdName), new PXCommandState(true, true));
			}
		}

		// Token: 0x06001F95 RID: 8085 RVA: 0x0007AB7C File Offset: 0x00078D7C
		public void InitColumnsLayout()
		{
			if (!string.IsNullOrEmpty(this.DataMember) && !this.suppressBinding)
			{
				PXDataSourceView pxdataSourceView = this.PrimaryLevel.GetData() as PXDataSourceView;
				if (!this.IsColumnsGenerated || (pxdataSourceView != null && pxdataSourceView.CanSelectChanged))
				{
					this.OnBeforeGenerateColumns(EventArgs.Empty);
					if (this.AutoGenerateColumns != ColumnGeneration.None)
					{
						this.GenerateColumns();
					}
					this.CreateSystemColumns();
					this.IsColumnsGenerated = true;
					this.OnColumnsGenerated(EventArgs.Empty);
					if (!this.IsLayoutReset && !this.IsColumnsDialogLoad && !PXGraph.ProxyIsActive)
					{
						this.layoutLoaded = this.LoadLayout();
					}
				}
			}
		}

		// Token: 0x06001F96 RID: 8086 RVA: 0x0007AC1C File Offset: 0x00078E1C
		internal bool LoadLayout()
		{
			bool result = false;
			PXDataSource pxdataSource = this.GetDataSource() as PXDataSource;
			if (pxdataSource != null)
			{
				int count = this.Columns.Count - 1;
				bool flag;
				GridPreferences.ColumnPref[] gridPreferences = pxdataSource.GetGridPreferences(this.DataMember, out flag);
				if (gridPreferences != null)
				{
					PXGrid.ReorderColumns(count, gridPreferences, this.Columns);
					result = flag;
				}
			}
			PXGridLayoutEventArgs e = new PXGridLayoutEventArgs(this.Columns);
			this.OnLayoutLoad(e);
			return result;
		}

		// Token: 0x06001F97 RID: 8087 RVA: 0x0007AC84 File Offset: 0x00078E84
		internal PXGridColumnCollection GetDefaultColumns()
		{
			PXGridColumnCollection pxgridColumnCollection = new PXGridColumnCollection();
			pxgridColumnCollection.CopyFrom(this.Columns);
			PXDataSource pxdataSource = this.GetDataSource() as PXDataSource;
			if (pxdataSource != null)
			{
				GridPreferences.ColumnPref[] defaultGridPreferences = pxdataSource.GetDefaultGridPreferences(this.DataMember);
				if (defaultGridPreferences != null)
				{
					PXGrid.ReorderColumns(pxgridColumnCollection.Count - 1, defaultGridPreferences, pxgridColumnCollection);
				}
			}
			return pxgridColumnCollection;
		}

		// Token: 0x06001F98 RID: 8088 RVA: 0x0007ACD4 File Offset: 0x00078ED4
		private static void ReorderColumns(int count, GridPreferences.ColumnPref[] cols, PXGridColumnCollection columns)
		{
			for (int i = 0; i < cols.Length; i++)
			{
				PXGridColumn pxgridColumn = columns[cols[i].DataField];
				if (pxgridColumn != null)
				{
					if (cols[i].SkipTab != null)
					{
						pxgridColumn.SkipTabs = cols[i].SkipTab.Value;
					}
					if (cols[i].Visible != null)
					{
						pxgridColumn.Visible = cols[i].Visible.Value;
						pxgridColumn.VisibleLoaded = true;
					}
					if (cols[i].Width != null)
					{
						pxgridColumn.Width = Unit.Pixel(cols[i].Width.Value);
					}
					columns.Remove(pxgridColumn);
					columns.Insert((cols[i].Order > count) ? count : cols[i].Order, pxgridColumn);
				}
			}
		}

		// Token: 0x06001F99 RID: 8089 RVA: 0x0007ADA0 File Offset: 0x00078FA0
		private bool SaveLayout(string columnsData)
		{
			if (!string.IsNullOrEmpty(columnsData))
			{
				string[] array = columnsData.Split(new char[]
				{
					'|'
				});
				this.RestoreColsOrder(array[0]);
				if (array.Length > 1)
				{
					string[] array2 = array[1].Split(new char[]
					{
						','
					});
					int num = 0;
					while (num < array2.Length && num < this.Columns.Count)
					{
						this.Columns[num].Visible = (int.Parse(array2[num]) > 0);
						num++;
					}
				}
				if (array.Length > 2)
				{
					string[] array3 = array[2].Split(new char[]
					{
						','
					});
					int num2 = 0;
					while (num2 < array3.Length && num2 < this.Columns.Count)
					{
						this.Columns[num2].SkipTabs = (int.Parse(array3[num2]) > 0);
						num2++;
					}
				}
			}
			PXDataSource pxdataSource = this.GetDataSource() as PXDataSource;
			PXGridLayoutEventArgs pxgridLayoutEventArgs = new PXGridLayoutEventArgs(this.Columns);
			if (pxdataSource != null)
			{
				this.OnLayoutSave(pxgridLayoutEventArgs);
				GridPreferences.ColumnPref[] array4 = pxdataSource.GetGridPreferences(this.DataMember);
				Dictionary<string, GridPreferences.ColumnPref> dictionary = null;
				if (array4 != null)
				{
					dictionary = new Dictionary<string, GridPreferences.ColumnPref>();
					foreach (GridPreferences.ColumnPref columnPref in array4)
					{
						if (!dictionary.ContainsKey(columnPref.DataField))
						{
							dictionary.Add(columnPref.DataField, columnPref);
						}
					}
				}
				PXGridColumnCollection columns = this.Columns;
				array4 = new GridPreferences.ColumnPref[columns.Count];
				for (int j = 0; j < columns.Count; j++)
				{
					PXGridColumn pxgridColumn = this.Columns[j];
					string key = pxgridColumn.GetKey();
					if (dictionary != null && dictionary.ContainsKey(key))
					{
						array4[j] = dictionary[key];
					}
					else
					{
						array4[j] = new GridPreferences.ColumnPref(key);
					}
					array4[j].Order = j;
					if (pxgridColumn.VisiblePosted)
					{
						array4[j].Visible = new bool?(pxgridColumn.Visible);
						pxgridColumn.VisibleLoaded = true;
					}
					array4[j].Width = new int?((int)pxgridColumn.Width.Value);
					array4[j].SkipTab = new bool?(pxgridColumn.SkipTabs);
				}
				return pxdataSource.SetGridPreferences(this.DataMember, array4);
			}
			this.OnLayoutSave(pxgridLayoutEventArgs);
			return pxgridLayoutEventArgs.Complete;
		}

		// Token: 0x06001F9A RID: 8090 RVA: 0x0007B004 File Offset: 0x00079204
		private bool ResetLayout()
		{
			PXDataSource pxdataSource = this.GetDataSource() as PXDataSource;
			PXGridLayoutEventArgs pxgridLayoutEventArgs = new PXGridLayoutEventArgs(this.Columns);
			if (pxdataSource != null)
			{
				this.OnLayoutReset(pxgridLayoutEventArgs);
				bool result = pxdataSource.ResetGridPreferences(this.DataMember);
				GridPreferences.ColumnPref[] defaultGridPreferences = pxdataSource.GetDefaultGridPreferences(this.DataMember);
				if (defaultGridPreferences != null)
				{
					PXGrid.ReorderColumns(this.Columns.Count - 1, defaultGridPreferences, this.Columns);
				}
				return result;
			}
			this.OnLayoutReset(pxgridLayoutEventArgs);
			return pxgridLayoutEventArgs.Complete;
		}

		// Token: 0x17000B01 RID: 2817
		// (get) Token: 0x06001F9B RID: 8091 RVA: 0x0007B078 File Offset: 0x00079278
		private bool IsLayoutSave
		{
			get
			{
				return ControlHelper.IsCallbackCommand(this, "LayoutSave");
			}
		}

		// Token: 0x17000B02 RID: 2818
		// (get) Token: 0x06001F9C RID: 8092 RVA: 0x0007B085 File Offset: 0x00079285
		private bool IsLayoutReset
		{
			get
			{
				return ControlHelper.IsCallbackCommand(this, "LayoutReset");
			}
		}

		// Token: 0x17000B03 RID: 2819
		// (get) Token: 0x06001F9D RID: 8093 RVA: 0x0007B092 File Offset: 0x00079292
		private bool IsColumnsDialogLoad
		{
			get
			{
				return ControlHelper.IsCallbackCommand(this, "ColumnsDialog");
			}
		}

		// Token: 0x140000BE RID: 190
		// (add) Token: 0x06001F9E RID: 8094 RVA: 0x0007B09F File Offset: 0x0007929F
		// (remove) Token: 0x06001F9F RID: 8095 RVA: 0x0007B0C0 File Offset: 0x000792C0
		[Category("DataSource")]
		[Description("Occurs before a Select command is executed on the data source.")]
		public event PXRowSelecting DSRowSelecting
		{
			add
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowSelecting.AddHandler(this.DataMember, value);
				}
			}
			remove
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowSelecting.RemoveHandler(this.DataMember, value);
				}
			}
		}

		// Token: 0x140000BF RID: 191
		// (add) Token: 0x06001FA0 RID: 8096 RVA: 0x0007B0E1 File Offset: 0x000792E1
		// (remove) Token: 0x06001FA1 RID: 8097 RVA: 0x0007B102 File Offset: 0x00079302
		[Category("DataSource")]
		[Description("Occurs after a Select command has been executed on the data source.")]
		public event PXRowSelected DSRowSelected
		{
			add
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowSelected.AddHandler(this.DataMember, value);
				}
			}
			remove
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowSelected.RemoveHandler(this.DataMember, value);
				}
			}
		}

		// Token: 0x140000C0 RID: 192
		// (add) Token: 0x06001FA2 RID: 8098 RVA: 0x0007B123 File Offset: 0x00079323
		// (remove) Token: 0x06001FA3 RID: 8099 RVA: 0x0007B144 File Offset: 0x00079344
		[Category("DataSource")]
		[Description("Occurs before an Insert command is executed on the data source.")]
		public event PXRowInserting DSRowInserting
		{
			add
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowInserting.AddHandler(this.DataMember, value);
				}
			}
			remove
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowInserting.RemoveHandler(this.DataMember, value);
				}
			}
		}

		// Token: 0x140000C1 RID: 193
		// (add) Token: 0x06001FA4 RID: 8100 RVA: 0x0007B165 File Offset: 0x00079365
		// (remove) Token: 0x06001FA5 RID: 8101 RVA: 0x0007B186 File Offset: 0x00079386
		[Category("DataSource")]
		[Description("Occurs after an Insert command has been executed on the data source.")]
		public event PXRowInserted DSRowInserted
		{
			add
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowInserted.AddHandler(this.DataMember, value);
				}
			}
			remove
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowInserted.RemoveHandler(this.DataMember, value);
				}
			}
		}

		// Token: 0x140000C2 RID: 194
		// (add) Token: 0x06001FA6 RID: 8102 RVA: 0x0007B1A7 File Offset: 0x000793A7
		// (remove) Token: 0x06001FA7 RID: 8103 RVA: 0x0007B1C8 File Offset: 0x000793C8
		[Category("DataSource")]
		[Description("Occurs before a Delete command is executed on the data source.")]
		public event PXRowDeleting DSRowDeleting
		{
			add
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowDeleting.AddHandler(this.DataMember, value);
				}
			}
			remove
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowDeleting.RemoveHandler(this.DataMember, value);
				}
			}
		}

		// Token: 0x140000C3 RID: 195
		// (add) Token: 0x06001FA8 RID: 8104 RVA: 0x0007B1E9 File Offset: 0x000793E9
		// (remove) Token: 0x06001FA9 RID: 8105 RVA: 0x0007B20A File Offset: 0x0007940A
		[Category("DataSource")]
		[Description("Occurs after a Delete command has been executed on the data source.")]
		public event PXRowDeleted DSRowDeleted
		{
			add
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowDeleted.AddHandler(this.DataMember, value);
				}
			}
			remove
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowDeleted.RemoveHandler(this.DataMember, value);
				}
			}
		}

		// Token: 0x140000C4 RID: 196
		// (add) Token: 0x06001FAA RID: 8106 RVA: 0x0007B22B File Offset: 0x0007942B
		// (remove) Token: 0x06001FAB RID: 8107 RVA: 0x0007B24C File Offset: 0x0007944C
		[Category("DataSource")]
		[Description("Occurs before an Update command is executed on the data source.")]
		public event PXRowUpdating DSRowUpdating
		{
			add
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowUpdating.AddHandler(this.DataMember, value);
				}
			}
			remove
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowUpdating.RemoveHandler(this.DataMember, value);
				}
			}
		}

		// Token: 0x140000C5 RID: 197
		// (add) Token: 0x06001FAC RID: 8108 RVA: 0x0007B26D File Offset: 0x0007946D
		// (remove) Token: 0x06001FAD RID: 8109 RVA: 0x0007B28E File Offset: 0x0007948E
		[Category("DataSource")]
		[Description("Occurs after an Update command has been executed on the data source.")]
		public event PXRowUpdated DSRowUpdated
		{
			add
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowUpdated.AddHandler(this.DataMember, value);
				}
			}
			remove
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowUpdated.RemoveHandler(this.DataMember, value);
				}
			}
		}

		// Token: 0x140000C6 RID: 198
		// (add) Token: 0x06001FAE RID: 8110 RVA: 0x0007B2AF File Offset: 0x000794AF
		// (remove) Token: 0x06001FAF RID: 8111 RVA: 0x0007B2D0 File Offset: 0x000794D0
		[Category("DataSource")]
		[Description("Occurs before a Persist command is executed on the data source.")]
		public event PXRowPersisting DSRowPersisting
		{
			add
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowPersisting.AddHandler(this.DataMember, value);
				}
			}
			remove
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowPersisting.RemoveHandler(this.DataMember, value);
				}
			}
		}

		// Token: 0x140000C7 RID: 199
		// (add) Token: 0x06001FB0 RID: 8112 RVA: 0x0007B2F1 File Offset: 0x000794F1
		// (remove) Token: 0x06001FB1 RID: 8113 RVA: 0x0007B312 File Offset: 0x00079512
		[Category("DataSource")]
		[Description("Occurs after a Persist command has been executed on the data source.")]
		public event PXRowPersisted DSRowPersisted
		{
			add
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowPersisted.AddHandler(this.DataMember, value);
				}
			}
			remove
			{
				if (this.DataGraph != null)
				{
					this.DataGraph.RowPersisted.RemoveHandler(this.DataMember, value);
				}
			}
		}

		// Token: 0x17000B04 RID: 2820
		// (get) Token: 0x06001FB2 RID: 8114 RVA: 0x0007B333 File Offset: 0x00079533
		// (set) Token: 0x06001FB3 RID: 8115 RVA: 0x0007B35C File Offset: 0x0007955C
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Browsable(false)]
		public string ImportDataMember
		{
			get
			{
				if (this.Levels.Count > 0)
				{
					return this.Levels[0].ImportDataMember;
				}
				return string.Empty;
			}
			set
			{
				if (this.Levels.Count > 0 && this.ImportDataMember != value)
				{
					this._pxview = null;
					this.Levels[0].ImportDataMember = value;
					ControlHelper.SerializeProp(this, new string[]
					{
						"Levels"
					});
					this.OnDataPropertyChanged();
				}
			}
		}

		// Token: 0x17000B05 RID: 2821
		// (get) Token: 0x06001FB4 RID: 8116 RVA: 0x0007B3B8 File Offset: 0x000795B8
		// (set) Token: 0x06001FB5 RID: 8117 RVA: 0x0007B3E0 File Offset: 0x000795E0
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[Category("Base Property")]
		[TypeConverter("PX.Web.UI.Design.PXDataMemberConverter")]
		public override string DataMember
		{
			get
			{
				if (this.Levels.Count > 0)
				{
					return this.Levels[0].DataMember;
				}
				return string.Empty;
			}
			set
			{
				if (this.Levels.Count > 0 && this.DataMember != value)
				{
					this._pxview = null;
					this.Levels[0].DataMember = value;
					ControlHelper.SerializeProp(this, new string[]
					{
						"Levels"
					});
					this.OnDataPropertyChanged();
				}
			}
		}

		// Token: 0x17000B06 RID: 2822
		// (get) Token: 0x06001FB6 RID: 8118 RVA: 0x0007B43C File Offset: 0x0007963C
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string[] DataKeyNames
		{
			get
			{
				if (this.Levels.Count > 0)
				{
					return this.Levels[0].DataKeyNames;
				}
				return new string[0];
			}
		}

		// Token: 0x17000B07 RID: 2823
		// (get) Token: 0x06001FB7 RID: 8119 RVA: 0x0007B464 File Offset: 0x00079664
		// (set) Token: 0x06001FB8 RID: 8120 RVA: 0x0007B495 File Offset: 0x00079695
		[ScriptBrowsable]
		[Category("Ext. Property")]
		public override string DataSourceID
		{
			get
			{
				string dataSourceID = base.DataSourceID;
				if (string.IsNullOrEmpty(dataSourceID) && !string.IsNullOrEmpty(this.DataMember))
				{
					dataSourceID = PXPage.GetDataSourceID(this);
				}
				return dataSourceID;
			}
			set
			{
				if (value != base.DataSourceID)
				{
					base.DataSourceID = value;
					this.dataSourceValid = false;
				}
			}
		}

		// Token: 0x17000B08 RID: 2824
		// (get) Token: 0x06001FB9 RID: 8121 RVA: 0x0007B4B3 File Offset: 0x000796B3
		// (set) Token: 0x06001FBA RID: 8122 RVA: 0x0007B4BC File Offset: 0x000796BC
		bool IPXDataControl.SuppressDataBinding
		{
			get
			{
				return this.suppressBinding;
			}
			set
			{
				if (!PXGraph.ProxyIsActive && !PXGraph.GeneratorIsActive)
				{
					this.suppressBinding = value;
					return;
				}
				PXBaseDataSource pxbaseDataSource = this.GetDataSource() as PXDataSource;
				if (pxbaseDataSource != null && (string.IsNullOrEmpty(this.DataMember) || pxbaseDataSource.DataGraph == null || !pxbaseDataSource.DataGraph.Views.ContainsKey(this.DataMember)))
				{
					this.suppressBinding = value;
				}
			}
		}

		// Token: 0x17000B09 RID: 2825
		// (get) Token: 0x06001FBB RID: 8123 RVA: 0x0007B522 File Offset: 0x00079722
		// (set) Token: 0x06001FBC RID: 8124 RVA: 0x0007B539 File Offset: 0x00079739
		[DefaultValue("")]
		[Themeable(false)]
		[Category("Data")]
		[TypeConverter(typeof(DataMemberConverter))]
		[Description("The view tha is used to select and edit filter names.")]
		[Browsable(false)]
		public virtual string FilterView
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "FilterView", string.Empty);
			}
			set
			{
				if (value != this.FilterView)
				{
					STM.SetProp<string>(this.ViewState, "FilterView", value, string.Empty);
					this.filterViewValid = false;
				}
			}
		}

		// Token: 0x17000B0A RID: 2826
		// (get) Token: 0x06001FBD RID: 8125 RVA: 0x0007B566 File Offset: 0x00079766
		// (set) Token: 0x06001FBE RID: 8126 RVA: 0x0007B57D File Offset: 0x0007977D
		[DefaultValue("")]
		[Themeable(false)]
		[Category("Data")]
		[TypeConverter(typeof(DataMemberConverter))]
		[Description("The view that is used to select and edit filter conditions.")]
		[Browsable(false)]
		public virtual string FilterRowsView
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "FilterRowsView", string.Empty);
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "FilterRowsView", value, string.Empty);
			}
		}

		// Token: 0x17000B0B RID: 2827
		// (get) Token: 0x06001FBF RID: 8127 RVA: 0x0007B595 File Offset: 0x00079795
		// (set) Token: 0x06001FC0 RID: 8128 RVA: 0x0007B5AC File Offset: 0x000797AC
		[DefaultValue("")]
		[Themeable(false)]
		[Category("Data")]
		[TypeConverter(typeof(DataMemberConverter))]
		[Description("The view that is used to select and edit the DataField filter.")]
		[Browsable(false)]
		public virtual string FilterSchemaView
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "FilterSchemaView", string.Empty);
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "FilterSchemaView", value, string.Empty);
			}
		}

		// Token: 0x17000B0C RID: 2828
		// (get) Token: 0x06001FC1 RID: 8129 RVA: 0x0007B5C4 File Offset: 0x000797C4
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public PXGraph DataGraph
		{
			get
			{
				PXDataSource pxdataSource = this.GetDataSource() as PXDataSource;
				if (pxdataSource != null && !string.IsNullOrEmpty(this.DataMember))
				{
					return pxdataSource.DataGraph;
				}
				return null;
			}
		}

		// Token: 0x17000B0D RID: 2829
		// (get) Token: 0x06001FC2 RID: 8130 RVA: 0x0007B5F5 File Offset: 0x000797F5
		[Browsable(false)]
		public Dictionary<string, WebControl> TemplateEditors
		{
			get
			{
				return this.PrimaryLevel.TemplateEditors;
			}
		}

		// Token: 0x17000B0E RID: 2830
		// (get) Token: 0x06001FC3 RID: 8131 RVA: 0x0007B602 File Offset: 0x00079802
		[Browsable(false)]
		public bool HasDataSource
		{
			get
			{
				return base.IsBoundUsingDataSourceID || this.DataSource != null;
			}
		}

		// Token: 0x17000B0F RID: 2831
		// (get) Token: 0x06001FC4 RID: 8132 RVA: 0x0007B618 File Offset: 0x00079818
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DataKey DataKey
		{
			get
			{
				if (this.FormViewMode)
				{
					PXFormView formView = this.Levels[this.formLevel.Value].FormView;
					if (formView != null && formView.DataKey.Value != null)
					{
						return formView.DataKey;
					}
				}
				if (this.dataKey == null)
				{
					this.dataKey = new DataKey(this.KeyTable);
				}
				return this.dataKey;
			}
		}

		// Token: 0x17000B10 RID: 2832
		// (get) Token: 0x06001FC5 RID: 8133 RVA: 0x0007B67F File Offset: 0x0007987F
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DataKey DataValues
		{
			get
			{
				if (this.dataValues == null)
				{
					this.dataValues = new DataKey(this.ValuesTable);
				}
				return this.dataValues;
			}
		}

		// Token: 0x17000B11 RID: 2833
		// (get) Token: 0x06001FC6 RID: 8134 RVA: 0x0007B6A0 File Offset: 0x000798A0
		internal OrderedDictionary KeyTable
		{
			get
			{
				if (this.keyTable == null && this.Levels.Count > 0)
				{
					this.keyTable = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
				}
				return this.keyTable;
			}
		}

		// Token: 0x17000B12 RID: 2834
		// (get) Token: 0x06001FC7 RID: 8135 RVA: 0x0007B6CE File Offset: 0x000798CE
		internal OrderedDictionary ValuesTable
		{
			get
			{
				if (this.valuesTable == null && this.Levels.Count > 0)
				{
					this.valuesTable = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
				}
				return this.valuesTable;
			}
		}

		// Token: 0x17000B13 RID: 2835
		// (get) Token: 0x06001FC8 RID: 8136 RVA: 0x0007B6FC File Offset: 0x000798FC
		// (set) Token: 0x06001FC9 RID: 8137 RVA: 0x0007B70F File Offset: 0x0007990F
		[Category("Behavior")]
		[DefaultValue(true)]
		[Description("Indicates whether the control repaints its content during callback request.")]
		[Browsable(false)]
		public bool AutoRepaint
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "AutoRepaint", true);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "AutoRepaint", value, true);
			}
		}

		// Token: 0x17000B14 RID: 2836
		// (get) Token: 0x06001FCA RID: 8138 RVA: 0x0007B724 File Offset: 0x00079924
		[DefaultValue(null)]
		[ScriptBrowsable]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Behavior")]
		[Description("The properties of the auto-size mode of the control.")]
		[Browsable(false)]
		public PXFilesDisplay FilesMenuUrls
		{
			get
			{
				if (this.filesDisplaySettings == null)
				{
					this.filesDisplaySettings = new PXFilesDisplay(base.IsTrackingViewState);
				}
				if (!base.DesignMode)
				{
					this.filesDisplaySettings.FileInfoUrl = base.ResolveUrl(this.filesDisplaySettings.FileInfoUrl);
					this.filesDisplaySettings.FilesDisplayUrl = base.ResolveUrl(this.filesDisplaySettings.FilesDisplayUrl);
					this.filesDisplaySettings.FilesListUrl = base.ResolveUrl(this.filesDisplaySettings.FilesListUrl);
				}
				return this.filesDisplaySettings;
			}
		}

		// Token: 0x17000B15 RID: 2837
		// (get) Token: 0x06001FCB RID: 8139 RVA: 0x0007B7AC File Offset: 0x000799AC
		// (set) Token: 0x06001FCC RID: 8140 RVA: 0x0007B7BF File Offset: 0x000799BF
		[Category("Base Property")]
		[DefaultValue(false)]
		[ScriptBrowsable]
		[Description("Indicates whether the paging feature is enabled.")]
		public virtual bool AllowPaging
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "AllowPaging", false);
			}
			set
			{
				if (value != this.AllowPaging)
				{
					STM.SetProp<bool>(this.ViewState, "AllowPaging", value, false);
					if (base.Initialized)
					{
						base.RequiresDataBinding = true;
					}
				}
			}
		}

		// Token: 0x17000B16 RID: 2838
		// (get) Token: 0x06001FCD RID: 8141 RVA: 0x0007B7EB File Offset: 0x000799EB
		// (set) Token: 0x06001FCE RID: 8142 RVA: 0x0007B7FE File Offset: 0x000799FE
		[Category("Base Property")]
		[DefaultValue(GridPageSizeMode.None)]
		[ScriptBrowsable]
		[Description("The mode of page size calculation of the grid.")]
		public virtual GridPageSizeMode AdjustPageSize
		{
			get
			{
				return STM.GetProp<GridPageSizeMode>(this.ViewState, "AdjustPageSize", GridPageSizeMode.None);
			}
			set
			{
				STM.SetProp<GridPageSizeMode>(this.ViewState, "AdjustPageSize", value, GridPageSizeMode.None);
			}
		}

		// Token: 0x17000B17 RID: 2839
		// (get) Token: 0x06001FCF RID: 8143 RVA: 0x0007B812 File Offset: 0x00079A12
		internal GridPageSizeMode AdjustPageSizeFinal
		{
			get
			{
				if (!this.AllowPaging)
				{
					return GridPageSizeMode.None;
				}
				return this.AdjustPageSize;
			}
		}

		// Token: 0x17000B18 RID: 2840
		// (get) Token: 0x06001FD0 RID: 8144 RVA: 0x0007B824 File Offset: 0x00079A24
		// (set) Token: 0x06001FD1 RID: 8145 RVA: 0x0007B82C File Offset: 0x00079A2C
		[DefaultValue(0)]
		[Bindable(true)]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		[Category("Paging")]
		[Description("The index of the displayed page.")]
		[Browsable(false)]
		public virtual int PageIndex
		{
			get
			{
				return this.pageIndex;
			}
			set
			{
				if (this.PageIndex != value)
				{
					this.pageIndex = value;
					if (base.Initialized)
					{
						base.RequiresDataBinding = true;
					}
				}
			}
		}

		// Token: 0x17000B19 RID: 2841
		// (get) Token: 0x06001FD2 RID: 8146 RVA: 0x0007B84D File Offset: 0x00079A4D
		// (set) Token: 0x06001FD3 RID: 8147 RVA: 0x0007B855 File Offset: 0x00079A55
		[DefaultValue(25)]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		[Category("Ext. Property")]
		[Description("The number of records to be displayed on the page.")]
		public virtual int PageSize
		{
			get
			{
				return this.pageSize;
			}
			set
			{
				if (value < 1)
				{
					throw new ArgumentOutOfRangeException("value");
				}
				if (this.PageSize != value)
				{
					this.pageSize = value;
					if (base.Initialized)
					{
						base.RequiresDataBinding = true;
					}
				}
			}
		}

		// Token: 0x17000B1A RID: 2842
		// (get) Token: 0x06001FD4 RID: 8148 RVA: 0x0007B888 File Offset: 0x00079A88
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual int PageCount
		{
			get
			{
				if (base.Site != null && base.Site.DesignMode)
				{
					return 20;
				}
				int num = this.TotalRowCount;
				if (!this.AllowPaging || num == 0)
				{
					return 1;
				}
				if (num < 0)
				{
					return -1;
				}
				int num2 = num + this.PageSize - 1;
				if (num2 >= 0)
				{
					return num2 / this.PageSize;
				}
				return 1;
			}
		}

		// Token: 0x17000B1B RID: 2843
		// (get) Token: 0x06001FD5 RID: 8149 RVA: 0x0007B8E0 File Offset: 0x00079AE0
		[Browsable(false)]
		[DefaultValue(true)]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		public bool IsFirstPage
		{
			get
			{
				if (!this.ActionBar.PagerSettings.TrackPosition)
				{
					return false;
				}
				if (!this.AllowPaging)
				{
					return true;
				}
				int pageCount = this.PageCount;
				int num = this.pageIndex;
				if (pageCount > 0)
				{
					return num == 0;
				}
				if (this.HasSearch)
				{
					return num < 0 && this.DataSourceCount <= this.PageSize;
				}
				if (num < 0)
				{
					return this.DataSourceCount <= this.PageSize;
				}
				return num == 0;
			}
		}

		// Token: 0x17000B1C RID: 2844
		// (get) Token: 0x06001FD6 RID: 8150 RVA: 0x0007B95C File Offset: 0x00079B5C
		[Browsable(false)]
		[DefaultValue(true)]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		public bool IsLastPage
		{
			get
			{
				if (!this.ActionBar.PagerSettings.TrackPosition)
				{
					return false;
				}
				if (!this.AllowPaging)
				{
					return true;
				}
				int pageCount = this.PageCount;
				int num = this.pageIndex;
				if (pageCount > 0)
				{
					return num == pageCount - 1;
				}
				if (this.HasSearch)
				{
					return num >= 0 && this.DataSourceCount <= this.PageSize;
				}
				if (num >= 0)
				{
					return this.DataSourceCount <= this.PageSize;
				}
				return this.pageIndex == -1;
			}
		}

		// Token: 0x17000B1D RID: 2845
		// (get) Token: 0x06001FD7 RID: 8151 RVA: 0x0007B9DF File Offset: 0x00079BDF
		[Browsable(false)]
		[DefaultValue(0)]
		[ScriptBrowsable(ScriptBrowsable.Always)]
		public int TotalRowCount
		{
			get
			{
				return this.totalRowCount;
			}
		}

		// Token: 0x17000B1E RID: 2846
		// (get) Token: 0x06001FD8 RID: 8152 RVA: 0x0007B9E7 File Offset: 0x00079BE7
		[Browsable(false)]
		public int DataSourceCount
		{
			get
			{
				return this.dataSourceCount;
			}
		}

		// Token: 0x17000B1F RID: 2847
		// (get) Token: 0x06001FD9 RID: 8153 RVA: 0x0007B9EF File Offset: 0x00079BEF
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[ScriptBrowsable]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public PXGridClientState ClientState
		{
			get
			{
				if (this.clientState == null)
				{
					this.clientState = new PXGridClientState(base.IsTrackingViewState);
				}
				return this.clientState;
			}
		}

		// Token: 0x17000B20 RID: 2848
		// (get) Token: 0x06001FDA RID: 8154 RVA: 0x0007BA10 File Offset: 0x00079C10
		[Browsable(false)]
		public bool FormViewMode
		{
			get
			{
				return this.formLevel != null;
			}
		}

		// Token: 0x17000B21 RID: 2849
		// (get) Token: 0x06001FDB RID: 8155 RVA: 0x0007BA1D File Offset: 0x00079C1D
		public bool NewRowActive
		{
			get
			{
				return this.newRowActive;
			}
		}

		// Token: 0x17000B22 RID: 2850
		// (get) Token: 0x06001FDC RID: 8156 RVA: 0x0007BA25 File Offset: 0x00079C25
		// (set) Token: 0x06001FDD RID: 8157 RVA: 0x0007BA38 File Offset: 0x00079C38
		[Category("Ext. Property")]
		[DefaultValue(false)]
		[ScriptBrowsable]
		[Description("Indicates whether the search feature is enabled.")]
		public virtual bool AllowSearch
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "AllowSearch", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "AllowSearch", value, false);
			}
		}

		// Token: 0x17000B23 RID: 2851
		// (get) Token: 0x06001FDE RID: 8158 RVA: 0x0007BA4C File Offset: 0x00079C4C
		// (set) Token: 0x06001FDF RID: 8159 RVA: 0x0007BA54 File Offset: 0x00079C54
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual object SearchValue
		{
			get
			{
				return this.searchValue;
			}
			set
			{
				if (value != null && value.ToString() != string.Empty)
				{
					PXGridColumn searchColumn = this.GetSearchColumn();
					if (searchColumn != null)
					{
						this.searchValue = searchColumn.GetValueFromText(value.ToString());
					}
					else
					{
						this.ResetSearch();
					}
				}
				else
				{
					this.ResetSearch();
				}
				if (base.Initialized)
				{
					base.RequiresDataBinding = true;
				}
			}
		}

		// Token: 0x17000B24 RID: 2852
		// (get) Token: 0x06001FE0 RID: 8160 RVA: 0x0007BAB1 File Offset: 0x00079CB1
		// (set) Token: 0x06001FE1 RID: 8161 RVA: 0x0007BAB9 File Offset: 0x00079CB9
		[Browsable(false)]
		[DefaultValue(false)]
		[ScriptBrowsable(ScriptBrowsable.Dynamic)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual bool ExternalSearch
		{
			get
			{
				return this.externalSearch;
			}
			set
			{
				this.externalSearch = value;
			}
		}

		// Token: 0x17000B25 RID: 2853
		// (get) Token: 0x06001FE2 RID: 8162 RVA: 0x0007BAC2 File Offset: 0x00079CC2
		protected bool HasSearch
		{
			get
			{
				return this.searchValue != null || this.ExternalSearch || (this.searchKey != null && this.searchKey.Length != 0);
			}
		}

		// Token: 0x06001FE3 RID: 8163 RVA: 0x0007BAEA File Offset: 0x00079CEA
		protected void ResetSearch()
		{
			this.searchValue = null;
			this.searchKey = null;
			this.ExternalSearch = false;
		}

		// Token: 0x17000B26 RID: 2854
		// (get) Token: 0x06001FE4 RID: 8164 RVA: 0x0007BB01 File Offset: 0x00079D01
		// (set) Token: 0x06001FE5 RID: 8165 RVA: 0x0007BB14 File Offset: 0x00079D14
		[Category("Ext. Property")]
		[DefaultValue(true)]
		[ScriptBrowsable]
		[Description("Indicates whether the filter feature is enabled.")]
		public virtual bool AllowFilter
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "AllowFilter", true);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "AllowFilter", value, true);
			}
		}

		// Token: 0x17000B27 RID: 2855
		// (get) Token: 0x06001FE6 RID: 8166 RVA: 0x0007BB28 File Offset: 0x00079D28
		// (set) Token: 0x06001FE7 RID: 8167 RVA: 0x0007BB3B File Offset: 0x00079D3B
		[DefaultValue(false)]
		[Category("Behavior")]
		[Description("Indicates whether the external filter control is used.")]
		[ScriptBrowsable]
		[Browsable(false)]
		public virtual bool ExternalFilter
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "ExternalFilter", false);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "ExternalFilter", value, false);
			}
		}

		// Token: 0x17000B28 RID: 2856
		// (get) Token: 0x06001FE8 RID: 8168 RVA: 0x0007BB4F File Offset: 0x00079D4F
		// (set) Token: 0x06001FE9 RID: 8169 RVA: 0x0007BB58 File Offset: 0x00079D58
		[Browsable(false)]
		public virtual Guid? FilterID
		{
			get
			{
				return this.filterID;
			}
			private set
			{
				if (value == null && this.filterActive && this.filterRows != null)
				{
					if (this.filterRows.Any((PXFilterRow f) => PXGrid.GetFilterType(f) == PXGrid.FilterRowType.FilterEditor))
					{
						value = new Guid?(PXGrid._FE_FILTER_ID);
					}
				}
				this.filterID = value;
			}
		}

		// Token: 0x17000B29 RID: 2857
		// (get) Token: 0x06001FEA RID: 8170 RVA: 0x0007BBBD File Offset: 0x00079DBD
		// (set) Token: 0x06001FEB RID: 8171 RVA: 0x0007BBC5 File Offset: 0x00079DC5
		internal string FastFilter
		{
			get
			{
				return this.fastFilter;
			}
			set
			{
				this.fastFilter = value;
			}
		}

		// Token: 0x17000B2A RID: 2858
		// (get) Token: 0x06001FEC RID: 8172 RVA: 0x0007BBCE File Offset: 0x00079DCE
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual List<PXFilterRow> FilterRows
		{
			get
			{
				if (this.filterRows == null)
				{
					this.filterRows = new List<PXFilterRow>();
				}
				return this.filterRows;
			}
		}

		// Token: 0x17000B2B RID: 2859
		// (get) Token: 0x06001FED RID: 8173 RVA: 0x0007BBE9 File Offset: 0x00079DE9
		[Browsable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual List<PXFilterRow> QuickFilters
		{
			get
			{
				if (this.quickFilters == null)
				{
					this.quickFilters = new List<PXFilterRow>();
				}
				return this.quickFilters;
			}
		}

		// Token: 0x17000B2C RID: 2860
		// (get) Token: 0x06001FEE RID: 8174 RVA: 0x0007BC04 File Offset: 0x00079E04
		// (set) Token: 0x06001FEF RID: 8175 RVA: 0x0007BC16 File Offset: 0x00079E16
		[DefaultValue(null)]
		[TypeConverter(typeof(StringArrayConverter))]
		[Editor("System.Web.UI.Design.WebControls.DataFieldEditor", typeof(UITypeEditor))]
		[Browsable(false)]
		[Description("The list of fields for the default quick filters. The list of fields names, which are separated by commas without spaces, such as InvoiceNbr,VendorID.")]
		public virtual string[] QuickFilterFields
		{
			get
			{
				return this.quickFilterFields ?? new string[0];
			}
			set
			{
				if (value != null)
				{
					this.quickFilterFields = (string[])value.Clone();
					return;
				}
				this.quickFilterFields = null;
			}
		}

		// Token: 0x06001FF0 RID: 8176 RVA: 0x0007BC34 File Offset: 0x00079E34
		private string GetQuickFiltersSessionKey()
		{
			return string.Format("{0}_{1}_quickFilters", ControlHelper.GetScreenID(), this.ClientID);
		}

		// Token: 0x06001FF1 RID: 8177 RVA: 0x0007BC4C File Offset: 0x00079E4C
		public void SaveQuickFiltersInSession()
		{
			this.FilterID = new Guid?(Guid.Empty);
			this.reloadFilters = (this.filterActive = true);
			this.QuickFiltersInSession = this.QuickFilters;
		}

		// Token: 0x17000B2D RID: 2861
		// (get) Token: 0x06001FF2 RID: 8178 RVA: 0x0007BC88 File Offset: 0x00079E88
		// (set) Token: 0x06001FF3 RID: 8179 RVA: 0x0007BCDC File Offset: 0x00079EDC
		private List<PXFilterRow> QuickFiltersInSession
		{
			get
			{
				List<PXFilterRow> list = PXContext.SessionTyped<PXSessionStateWebUI>().GridFilterRows[this.GetQuickFiltersSessionKey()];
				if (list != null)
				{
					return list.Select(delegate(PXFilterRow f)
					{
						if (f != null)
						{
							return (PXFilterRow)f.Clone();
						}
						return null;
					}).ToList<PXFilterRow>();
				}
				return null;
			}
			set
			{
				List<PXFilterRow> list;
				if (value != null)
				{
					list = value.Select(delegate(PXFilterRow f)
					{
						if (f != null)
						{
							return (PXFilterRow)f.Clone();
						}
						return null;
					}).ToList<PXFilterRow>();
				}
				else
				{
					list = null;
				}
				List<PXFilterRow> value2 = list;
				PXContext.SessionTyped<PXSessionStateWebUI>().GridFilterRows[this.GetQuickFiltersSessionKey()] = value2;
			}
		}

		// Token: 0x17000B2E RID: 2862
		// (get) Token: 0x06001FF4 RID: 8180 RVA: 0x0007BD30 File Offset: 0x00079F30
		// (set) Token: 0x06001FF5 RID: 8181 RVA: 0x0007BD42 File Offset: 0x00079F42
		[DefaultValue(null)]
		[TypeConverter(typeof(StringArrayConverter))]
		[Editor("System.Web.UI.Design.WebControls.DataFieldEditor", typeof(UITypeEditor))]
		[Category("Ext. Property")]
		[Description("The extra fields for fast filtering in the selector. The list of fields names, which are separated by commas without spaces, such as InvoiceNbr,VendorID.")]
		public virtual string[] FastFilterFields
		{
			get
			{
				return this.fastFilterFields ?? new string[0];
			}
			set
			{
				if (value != null)
				{
					this.fastFilterFields = (string[])value.Clone();
					return;
				}
				this.fastFilterFields = null;
			}
		}

		// Token: 0x17000B2F RID: 2863
		// (get) Token: 0x06001FF6 RID: 8182 RVA: 0x0007BD60 File Offset: 0x00079F60
		// (set) Token: 0x06001FF7 RID: 8183 RVA: 0x0007BD77 File Offset: 0x00079F77
		[DefaultValue("")]
		[Themeable(false)]
		[Category("Data")]
		[Browsable(false)]
		[TypeConverter("PX.Web.UI.Design.PXFieldEditorConverter")]
		[Description("The ID of the control that performs the fast filtering.")]
		public virtual string FastFilterID
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "FastFilterID", string.Empty);
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "FastFilterID", value, string.Empty);
			}
		}

		// Token: 0x17000B30 RID: 2864
		// (get) Token: 0x06001FF8 RID: 8184 RVA: 0x0007BD8F File Offset: 0x00079F8F
		[DefaultValue("*")]
		[ScriptBrowsable]
		[Browsable(false)]
		[Themeable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public string FastFilterWildcard
		{
			get
			{
				return "*";
			}
		}

		// Token: 0x17000B31 RID: 2865
		// (get) Token: 0x06001FF9 RID: 8185 RVA: 0x0007BD96 File Offset: 0x00079F96
		[DefaultValue(PXCondition.RLIKE)]
		[ScriptBrowsable]
		[Browsable(false)]
		[Themeable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public PXCondition FastFilterCondition
		{
			get
			{
				if (!base.DesignMode)
				{
					return SitePolicy.GridFastFilterCondition;
				}
				return PXCondition.RLIKE;
			}
		}

		// Token: 0x17000B32 RID: 2866
		// (get) Token: 0x06001FFA RID: 8186 RVA: 0x0007BDA8 File Offset: 0x00079FA8
		[DefaultValue(100)]
		[ScriptBrowsable]
		[Browsable(false)]
		[Themeable(false)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public int FastFilterMaxLength
		{
			get
			{
				if (!base.DesignMode)
				{
					return SitePolicy.GridFastFilterMaxLength.GetValueOrDefault(100);
				}
				return 100;
			}
		}

		// Token: 0x17000B33 RID: 2867
		// (get) Token: 0x06001FFB RID: 8187 RVA: 0x0007BDCF File Offset: 0x00079FCF
		// (set) Token: 0x06001FFC RID: 8188 RVA: 0x0007BDE8 File Offset: 0x00079FE8
		[Category("Data")]
		[DefaultValue("NoteText")]
		[Browsable(false)]
		[TypeConverter("System.Web.UI.Design.DataFieldConverter")]
		[Description("The data field that should be used as the source of the note of the control.")]
		public virtual string NoteField
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "NoteField", "NoteText");
			}
			set
			{
				this.NoteFieldSpecified = new bool?(this.NoteFieldSpecified ?? (!string.IsNullOrEmpty(value)));
				STM.SetProp<string>(this.ViewState, "NoteField", value, "NoteText");
			}
		}

		// Token: 0x17000B34 RID: 2868
		// (get) Token: 0x06001FFD RID: 8189 RVA: 0x0007BE38 File Offset: 0x0007A038
		// (set) Token: 0x06001FFE RID: 8190 RVA: 0x0007BE60 File Offset: 0x0007A060
		[DefaultValue(null)]
		[Category("Ext. Property")]
		public bool? NoteIndicator
		{
			get
			{
				return STM.GetProp<bool?>(this.ViewState, "NoteIndicator", null);
			}
			set
			{
				this.NoteFieldSpecified = new bool?(false);
				STM.SetProp<bool?>(this.ViewState, "NoteIndicator", value, null);
			}
		}

		// Token: 0x17000B35 RID: 2869
		// (get) Token: 0x06001FFF RID: 8191 RVA: 0x0007BE94 File Offset: 0x0007A094
		[DefaultValue("")]
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public string FilesField
		{
			get
			{
				if (!(this.FilesIndicator ?? this.NoteFieldSpecified.GetValueOrDefault()))
				{
					return "";
				}
				return "NoteFiles";
			}
		}

		// Token: 0x17000B36 RID: 2870
		// (get) Token: 0x06002000 RID: 8192 RVA: 0x0007BED4 File Offset: 0x0007A0D4
		// (set) Token: 0x06002001 RID: 8193 RVA: 0x0007BEFC File Offset: 0x0007A0FC
		[DefaultValue(typeof(bool?), "")]
		[Category("Ext. Property")]
		[Description("Indicates whether to show files indicator.")]
		public virtual bool? FilesIndicator
		{
			get
			{
				return STM.GetProp<bool?>(this.ViewState, "FilesIndicator", null);
			}
			set
			{
				STM.SetProp<bool?>(this.ViewState, "FilesIndicator", value, null);
			}
		}

		// Token: 0x17000B37 RID: 2871
		// (get) Token: 0x06002002 RID: 8194 RVA: 0x0007BF23 File Offset: 0x0007A123
		// (set) Token: 0x06002003 RID: 8195 RVA: 0x0007BF3A File Offset: 0x0007A13A
		[Category("Ext. Property")]
		[DefaultValue("")]
		[ScriptBrowsable]
		[TypeConverter("System.Web.UI.Design.DataFieldConverter")]
		[Description("The data field that should be used to show the text in the status bar.")]
		public string StatusField
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "StatusField", string.Empty);
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "StatusField", value, string.Empty);
			}
		}

		// Token: 0x17000B38 RID: 2872
		// (get) Token: 0x06002004 RID: 8196 RVA: 0x0007BF52 File Offset: 0x0007A152
		[Browsable(false)]
		internal virtual string NoteIDField
		{
			get
			{
				return this.noteIDField;
			}
		}

		// Token: 0x17000B39 RID: 2873
		// (get) Token: 0x06002005 RID: 8197 RVA: 0x0007BF5A File Offset: 0x0007A15A
		[Browsable(false)]
		internal virtual string NoteDocsField
		{
			get
			{
				return this.noteDocsField;
			}
		}

		// Token: 0x17000B3A RID: 2874
		// (get) Token: 0x06002006 RID: 8198 RVA: 0x0007BF64 File Offset: 0x0007A164
		[Browsable(false)]
		internal virtual bool AllowNote
		{
			get
			{
				return this.NoteIndicator.GetValueOrDefault() || (this.NoteField.Length > 0 && this.NoteFieldSpecified.GetValueOrDefault());
			}
		}

		// Token: 0x17000B3B RID: 2875
		// (get) Token: 0x06002007 RID: 8199 RVA: 0x0007BF9E File Offset: 0x0007A19E
		[DefaultValue(null)]
		[MergableProperty(false)]
		[Editor("PX.Web.UI.Design.PXParametersEditor", typeof(UITypeEditor))]
		[Category("Base Property")]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Description("The collection of parameters that are used by a Select query.")]
		public PXParamCollection Parameters
		{
			get
			{
				if (this.parameters == null)
				{
					this.parameters = new PXParamCollection(this);
				}
				return this.parameters;
			}
		}

		// Token: 0x17000B3C RID: 2876
		// (get) Token: 0x06002008 RID: 8200 RVA: 0x0007BFBA File Offset: 0x0007A1BA
		[DefaultValue(null)]
		[MergableProperty(false)]
		[Editor("PX.Web.UI.Design.PXSearchesEditor", typeof(UITypeEditor))]
		[Category("Data")]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Description("The collection of filter parameters that are used by a Select query.")]
		[Browsable(false)]
		public PXParamCollection Searches
		{
			get
			{
				if (this.searches == null)
				{
					this.searches = new PXParamCollection(this);
				}
				return this.searches;
			}
		}

		// Token: 0x17000B3D RID: 2877
		// (get) Token: 0x06002009 RID: 8201 RVA: 0x0007BFD8 File Offset: 0x0007A1D8
		private bool IsOwnCallback
		{
			get
			{
				if (this.Page != null && this.Page.IsCallback)
				{
					try
					{
						return this.Page.Request.Form["__CALLBACKID"].StartsWith(this.UniqueID);
					}
					catch (HttpException)
					{
					}
					return false;
				}
				return false;
			}
		}

		// Token: 0x17000B3E RID: 2878
		// (get) Token: 0x0600200A RID: 8202 RVA: 0x0007C038 File Offset: 0x0007A238
		// (set) Token: 0x0600200B RID: 8203 RVA: 0x0007C040 File Offset: 0x0007A240
		[Browsable(false)]
		[DefaultValue(null)]
		public string FileUploaderId { get; set; }

		// Token: 0x17000B3F RID: 2879
		// (get) Token: 0x0600200C RID: 8204 RVA: 0x0007C049 File Offset: 0x0007A249
		private PXFilesDialog ExternalUploader
		{
			get
			{
				if (this._uploadDialog == null && !string.IsNullOrEmpty(this.FileUploaderId))
				{
					this._uploadDialog = (ControlHelper.FindControl(this.FileUploaderId, this.Page.Controls) as PXFilesDialog);
				}
				return this._uploadDialog;
			}
		}

		// Token: 0x17000B40 RID: 2880
		// (get) Token: 0x0600200D RID: 8205 RVA: 0x0007C087 File Offset: 0x0007A287
		internal string UploaderID
		{
			get
			{
				return this.ClientID + "_upldr";
			}
		}

		// Token: 0x17000B41 RID: 2881
		// (get) Token: 0x0600200E RID: 8206 RVA: 0x0007C099 File Offset: 0x0007A299
		internal bool IsPivotMode
		{
			get
			{
				return this.pivotID != null;
			}
		}

		// Token: 0x0600200F RID: 8207 RVA: 0x0007C0A6 File Offset: 0x0007A2A6
		protected override void OnPagePreLoad(object sender, EventArgs e)
		{
			base.OnPagePreLoad(sender, e);
			if (this.Page != null && !this.Page.IsCallback && this.AdjustPageSizeFinal == GridPageSizeMode.None)
			{
				base.RequiresDataBinding = true;
			}
			this.pagePreLoadFired = true;
		}

		// Token: 0x06002010 RID: 8208 RVA: 0x0007C0DC File Offset: 0x0007A2DC
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			if (this.Page != null)
			{
				if (!this.pagePreLoadFired && !this.isDataBound && !this.Page.IsCallback && this.AdjustPageSizeFinal == GridPageSizeMode.None)
				{
					base.RequiresDataBinding = true;
				}
				if (!this.Page.IsPostBack)
				{
					this.LoadCookiesData();
				}
				if (this.FastFilterID.Length > 0)
				{
					Control control = ControlHelper.FindControl(this.FastFilterID, this.Page);
					if (control is PXWebControl)
					{
						((PXWebControl)control).IsClientControl = false;
					}
					if (control is PXBoundControl)
					{
						((PXBoundControl)control).IsClientControl = false;
					}
				}
			}
			if (this.AutoSize.Enabled && this.Page is PXPage)
			{
				((PXPage)this.Page).EnableFadeIn = true;
			}
			if (this.Page.IsCallback)
			{
				PXCallbackManager instance = PXCallbackManager.GetInstance();
				instance.PreProcessCallback += this.OnCanceling;
				instance.PostGetCallbackResult += this.PostGetCallbackResultHandler;
			}
		}

		// Token: 0x06002011 RID: 8209 RVA: 0x0007C1E4 File Offset: 0x0007A3E4
		private void PostGetCallbackResultHandler(PXCallbackManager sender, XmlWriter writer)
		{
			if (this.needUpdatePivot && this.pivotTable != null)
			{
				this.pivotTable.EnsureResetPivotTable();
				string clientData = ((IPXDataControl)this.pivotTable).GetClientData();
				PXCallbackManager.WriteControlData(writer, this.pivotTable, clientData);
			}
		}

		// Token: 0x06002012 RID: 8210 RVA: 0x0007C228 File Offset: 0x0007A428
		private void OnCanceling(PXCallbackManager sender, string data)
		{
			PXBaseDataSource pxbaseDataSource = sender.Control as PXBaseDataSource;
			if (this.DataGraph != null && pxbaseDataSource != null && pxbaseDataSource.ID == this.DataSourceID)
			{
				if ((from PXAction a in this.DataGraph.Actions.Values
				select a.GetState(null) as PXButtonState into s
				where s != null && s.SpecialType == PXSpecialButtonType.Cancel
				select s into a
				select a.Name).Contains(sender.ActiveCommand.Name))
				{
					if (this.HasDrillDownFilter)
					{
						this.reloadFilters = true;
						if (this.FilterID == PXGrid._DD_FILTER_ID)
						{
							this.FilterID = null;
						}
					}
					this.FastFilter = string.Empty;
					this.colsFilterActive = new bool?(false);
					this.FilterRowsInSession = null;
					this.filterRows = null;
					this.QuickFiltersInSession = null;
					this.quickFilters = null;
				}
			}
		}

		// Token: 0x06002013 RID: 8211 RVA: 0x0007C374 File Offset: 0x0007A574
		private void ClearClientFilters()
		{
			new List<PXFilterRow>();
			this.filterRows = (from f in this.FilterRows
			where PXGrid.GetFilterType(f) == PXGrid.FilterRowType.DrillDown
			select f).ToList<PXFilterRow>();
			List<PXFilterRow> filterRowsInSession = this.FilterRowsInSession;
			if (filterRowsInSession != null)
			{
				this.FilterRowsInSession = (from f in filterRowsInSession
				where PXGrid.GetFilterType(f) == PXGrid.FilterRowType.DrillDown
				select f).ToList<PXFilterRow>();
			}
		}

		// Token: 0x17000B42 RID: 2882
		// (get) Token: 0x06002014 RID: 8212 RVA: 0x0007C3F6 File Offset: 0x0007A5F6
		protected new DataSourceSelectArguments SelectArguments
		{
			get
			{
				if (this.arguments != null)
				{
					return this.arguments;
				}
				if (this.GetDataSource() is PXDataSource)
				{
					if (this.arguments == null)
					{
						this.arguments = this.CreateDataSourceSelectArguments();
					}
					return this.arguments;
				}
				return base.SelectArguments;
			}
		}

		// Token: 0x17000B43 RID: 2883
		// (get) Token: 0x06002015 RID: 8213 RVA: 0x0007C435 File Offset: 0x0007A635
		protected PXDSSelectArguments SelectArgumentsExt
		{
			get
			{
				if (this.argumentsExt == null)
				{
					this.argumentsExt = this.CreateSelectArgumentsExt();
				}
				return this.argumentsExt;
			}
		}

		// Token: 0x06002016 RID: 8214 RVA: 0x0007C454 File Offset: 0x0007A654
		protected override IDataSource GetDataSource()
		{
			if (!base.DesignMode && this.dataSourceValid && this.dataSource != null)
			{
				return this.dataSource;
			}
			IDataSource dataSource = null;
			string dataSourceID = this.DataSourceID;
			if (dataSourceID.Length > 0)
			{
				dataSource = (this.FindDataSourceQuickly(dataSourceID) ?? this.FindDataSource(dataSourceID));
				if (dataSource == null)
				{
					throw new HttpException(Msg.GetLocal("DataSourceID of {0} must be the ID of a control of the IDataSource type.", new object[]
					{
						this.ID
					}));
				}
				this.dataSource = dataSource;
				this.dataSourceValid = true;
			}
			return dataSource;
		}

		// Token: 0x06002017 RID: 8215 RVA: 0x0007C4D8 File Offset: 0x0007A6D8
		private IDataSource FindDataSourceQuickly(string id)
		{
			PageInfo pageInfo = PageInfo.Current;
			PageInfo.DataSourceInfo dataSourceInfo;
			if (pageInfo != null && pageInfo.DataSources.TryGetValue(id, out dataSourceInfo))
			{
				return dataSourceInfo.DataSource;
			}
			return null;
		}

		// Token: 0x06002018 RID: 8216 RVA: 0x0007C506 File Offset: 0x0007A706
		private IDataSource FindDataSource(string id)
		{
			return (ControlHelper.FindControl(this, id) as IDataSource) ?? (ControlHelper.FindControl(id, this.Page) as IDataSource);
		}

		// Token: 0x06002019 RID: 8217 RVA: 0x0007C52C File Offset: 0x0007A72C
		protected virtual DataSourceView GetFilterData()
		{
			if (!this.filterViewValid || base.DesignMode)
			{
				IDataSource dataSource = this.GetDataSource();
				if (dataSource != null)
				{
					DataSourceView view = dataSource.GetView(this.GetFilterView());
					if (view == null)
					{
						throw new InvalidOperationException(Msg.GetLocal("The view, which the PXFilterEditor control {0} has requested, cannot be found.", new object[]
						{
							this.ID
						}));
					}
					this.filterView = view;
					this.filterViewValid = true;
				}
			}
			return this.filterView;
		}

		// Token: 0x0600201A RID: 8218 RVA: 0x0007C598 File Offset: 0x0007A798
		private HashSet<string> GetFilterFields()
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (this.FilterID != null)
			{
				foreach (PXResult<FilterRow> r in PXSelectBase<FilterRow, PXSelect<FilterRow, Where<FilterRow.filterID, Equal<Required<FilterRow.filterID>>>>.Config>.Select(this.DataGraph, new object[]
				{
					this.FilterID
				}))
				{
					FilterRow filterRow = r;
					if (filterRow.DataField != null)
					{
						hashSet.Add(filterRow.DataField);
					}
				}
				hashSet.UnionWith(DynamicFilterManager.GetFilterFields(this.DataGraph.GetType().FullName, this.DataMember, this.FilterID.Value));
			}
			else
			{
				hashSet.UnionWith(from row in this.FilterRows
				select row.DataField);
			}
			return hashSet;
		}

		// Token: 0x0600201B RID: 8219 RVA: 0x0007C694 File Offset: 0x0007A894
		protected override void PerformSelect()
		{
			if (ControlHelper.NeedSuppressSelect(this) || this.FormViewMode)
			{
				return;
			}
			if (!string.IsNullOrEmpty(this.updateResult) && !this.NeedRepaintRows && ControlHelper.IsCallbackOwnerStrict(this))
			{
				return;
			}
			if (this.AllowFilter && !string.IsNullOrEmpty(this.GetFilterView()) && (!this.filterActiveLoaded || this.reloadFilters || ControlHelper.IsReloadPage(this)) && this.filterNames == null)
			{
				this.PerformSelectFilterNames();
			}
			else
			{
				this.SetFiltersFromSession();
			}
			if (this.IsPivotMode)
			{
				this.CreateChildControls(null, false);
				return;
			}
			if (this.AdjustPageSizeFinal == GridPageSizeMode.Auto && !this.pageAdjusted && !PXGraph.ProxyIsActive)
			{
				PXDataSourceView pxdataSourceView = this.GetDataView() as PXDataSourceView;
				if (pxdataSourceView != null)
				{
					this.SynchronizeColsState(pxdataSourceView);
					this.CreateChildControls(null, false);
					this.SetRenderState(this.renderState);
				}
				return;
			}
			if (this.GetDataSource() is PXDataSource)
			{
				if (this.DataSourceID.Length == 0)
				{
					this.OnDataBinding(EventArgs.Empty);
				}
				PXDataSourceView pxdataSourceView2 = this.GetDataView() as PXDataSourceView;
				if (this.DataGraph._InactiveViews.ContainsKey(pxdataSourceView2.Name))
				{
					((IPXDataControl)this).SuppressDataBinding = true;
					base.RequiresDataBinding = false;
					return;
				}
				this.arguments = this.CreateDataSourceSelectArguments();
				this.argumentsExt = this.CreateSelectArgumentsExt();
				PXSelectEventArgs pxselectEventArgs = new PXSelectEventArgs(this.arguments, this.argumentsExt);
				this.OnSelect(pxselectEventArgs);
				if (this.NoteField.Length > 0 && pxdataSourceView2 != null)
				{
					PXNoteState pxnoteState = pxdataSourceView2.GetStateExt(null, this.NoteField) as PXNoteState;
					if (pxnoteState != null)
					{
						this.noteIDField = pxnoteState.NoteIDField;
						pxdataSourceView2.SetValueExt(null, this.NoteField, null);
					}
				}
				if (this.FilesField.Length > 0 && pxdataSourceView2 != null)
				{
					PXNoteState pxnoteState2 = pxdataSourceView2.GetStateExt(null, this.FilesField) as PXNoteState;
					if (pxnoteState2 != null)
					{
						this.noteDocsField = pxnoteState2.NoteDocsField;
						pxdataSourceView2.SetValueExt(null, this.noteDocsField, null);
					}
				}
				if (pxselectEventArgs.Cancel)
				{
					return;
				}
				base.RequiresDataBinding = false;
				base.MarkAsDataBound();
				DataSourceViewSelectCallback dataSourceViewSelectCallback = new DataSourceViewSelectCallback(this.OnDataSourceViewSelect);
				if (this.ExcelExport)
				{
					dataSourceViewSelectCallback = new DataSourceViewSelectCallback(this.OnDataSourceViewSelectExcelExport);
				}
				try
				{
					if (this.RestrictFields)
					{
						this.SynchronizeColsState(pxdataSourceView2);
						this.columnsSynchronizedAfterBinding = true;
						using (new PXFieldScope(this.DataGraph.Views[pxdataSourceView2._ViewName], from f in (from PXGridColumn c in this.Columns
						select c.DataField).Concat(this.GetFilterFields())
						where !string.IsNullOrEmpty(f)
						select f, true))
						{
							pxdataSourceView2.Select(this.arguments, this.argumentsExt, dataSourceViewSelectCallback);
							goto IL_2E1;
						}
					}
					pxdataSourceView2.Select(this.arguments, this.argumentsExt, dataSourceViewSelectCallback);
					IL_2E1:
					return;
				}
				catch (PXDatabaseException ex) when (ex.ErrorCode == PXDbExceptions.Timeout && this.arguments.RetrieveTotalRowCount)
				{
					this.StoredSqlTimeout = true;
					this.PerformSelect();
					return;
				}
			}
			base.PerformSelect();
		}

		// Token: 0x0600201C RID: 8220 RVA: 0x0007C9E0 File Offset: 0x0007ABE0
		protected DataSourceView GetDataView()
		{
			if (this._pxview != null)
			{
				return this._pxview;
			}
			DataSourceView result = null;
			if (!this.suppressBinding)
			{
				this._pxview = ((result = this.GetData()) as PXDataSourceView);
				if (this._pxview != null && !string.IsNullOrEmpty(this.DataMember) && base.IsBoundUsingDataSourceID)
				{
					this._pxview.RefreshRequested += delegate(object sender, EventArgs arg)
					{
						this.NeedRepaintRows = true;
						if (this.KeepPosition)
						{
							this.searchKey = new object[this.DataKeyNames.Length];
							int num = Math.Max(this.DataKeyNames.Length, this.DataKey.Values.Count);
							for (int i = 0; i < num; i++)
							{
								this.searchKey[i] = this.DataKey[this.DataKeyNames[i]];
							}
							this.PageIndex = 0;
						}
					};
				}
			}
			return result;
		}

		// Token: 0x0600201D RID: 8221 RVA: 0x0007CA50 File Offset: 0x0007AC50
		private void OnDataSourceViewSelect(IEnumerable data)
		{
			if (this.DataSourceID.Length > 0)
			{
				this.OnDataBinding(EventArgs.Empty);
			}
			this.PerformDataBinding(data);
			this.OnDataBound(EventArgs.Empty);
			if (!this.StoredSqlTimeout && this.PreserveSortsAndFilters && !ControlHelper.IsReloadPage(this))
			{
				Dictionary<string, SortDirection> dictionary = (from col in this.PrimaryLevel.Columns.Items
				where col.SortDirection > SortDirection.None
				select col).ToDictionary((PXGridColumn col) => col.GetKey(), (PXGridColumn col) => col.SortDirection);
				this.StoredSorts = ((dictionary.Count > 0) ? dictionary : null);
			}
		}

		// Token: 0x0600201E RID: 8222 RVA: 0x0007CB34 File Offset: 0x0007AD34
		private void OnDataSourceViewSelectExcelExport(IEnumerable data)
		{
			if (this.DataSourceID.Length > 0)
			{
				this.OnDataBinding(EventArgs.Empty);
			}
			this.PerformDataBindingExcelExport(data);
			this.OnDataBound(EventArgs.Empty);
		}

		// Token: 0x0600201F RID: 8223 RVA: 0x0007CB64 File Offset: 0x0007AD64
		protected virtual void OnSelect(PXSelectEventArgs e)
		{
			PXSelectEventHandler pxselectEventHandler = (PXSelectEventHandler)base.Events[PXGrid.SelectEvent];
			if (pxselectEventHandler != null)
			{
				pxselectEventHandler(this, e);
			}
			if (this.argumentsExt != null)
			{
				this.InitializeSelectArgumentsExt(this.argumentsExt);
			}
		}

		// Token: 0x06002020 RID: 8224 RVA: 0x0007CBA8 File Offset: 0x0007ADA8
		private void PerformSelectEmpty(DataSourceViewSelectCallback callback)
		{
			bool flag = false;
			DataSourceView dataView = this.GetDataView();
			bool? flag2 = null;
			if (this.AllowSearch && this.HasSearch)
			{
				flag2 = new bool?(this.AllowSearch);
				this.AllowSearch = false;
				flag = true;
				if (this.pageIndex > 0)
				{
					this.pageIndex = -1;
				}
			}
			else if (this.AllowPaging && dataView.CanPage)
			{
				DataSourceSelectArguments selectArguments = this.SelectArguments;
				bool retrieveTotalRowCount = selectArguments.RetrieveTotalRowCount;
				flag = ((retrieveTotalRowCount && selectArguments.StartRowIndex >= selectArguments.TotalRowCount) || (!retrieveTotalRowCount && this.pageIndex != 0));
				if (flag)
				{
					this.pageIndex = 0;
				}
			}
			if (flag)
			{
				try
				{
					this.PerformSelect(callback);
				}
				finally
				{
					if (flag2.GetValueOrDefault())
					{
						this.AllowSearch = true;
						this.ResetSearch();
					}
				}
			}
		}

		// Token: 0x06002021 RID: 8225 RVA: 0x0007CC80 File Offset: 0x0007AE80
		private void PerformSelectLast(DataSourceViewSelectCallback callback)
		{
			if (!this.SelectArguments.RetrieveTotalRowCount && this.AllowSearch && this.HasSearch)
			{
				this.pageIndex = -1;
				this.AllowSearch = false;
				try
				{
					this.PerformSelect(callback);
				}
				finally
				{
					this.AllowSearch = true;
					this.ResetSearch();
				}
			}
		}

		// Token: 0x06002022 RID: 8226 RVA: 0x0007CCE0 File Offset: 0x0007AEE0
		private void PerformSelectFirst(DataSourceViewSelectCallback callback)
		{
			if (!this.SelectArguments.RetrieveTotalRowCount && this.AllowSearch && this.HasSearch)
			{
				this.pageIndex = 0;
				this.AllowSearch = false;
				try
				{
					this.PerformSelect(callback);
				}
				finally
				{
					this.AllowSearch = true;
					this.ResetSearch();
				}
			}
		}

		// Token: 0x06002023 RID: 8227 RVA: 0x0007CD40 File Offset: 0x0007AF40
		private void PerformSelect(DataSourceViewSelectCallback callback)
		{
			DataSourceView dataView = this.GetDataView();
			PXDataSourceView pxdataSourceView = dataView as PXDataSourceView;
			this.arguments = this.CreateDataSourceSelectArguments();
			if (pxdataSourceView != null)
			{
				this.argumentsExt = this.CreateSelectArgumentsExt();
				PXSelectEventArgs e = new PXSelectEventArgs(this.arguments, this.argumentsExt);
				this.OnSelect(e);
				pxdataSourceView.Select(this.arguments, this.argumentsExt, callback);
				return;
			}
			dataView.Select(this.arguments, callback);
		}

		// Token: 0x06002024 RID: 8228 RVA: 0x0007CDB0 File Offset: 0x0007AFB0
		protected override DataSourceSelectArguments CreateDataSourceSelectArguments()
		{
			DataSourceSelectArguments dataSourceSelectArguments = new DataSourceSelectArguments();
			this.InitializeSelectArguments(dataSourceSelectArguments);
			return dataSourceSelectArguments;
		}

		// Token: 0x06002025 RID: 8229 RVA: 0x0007CDCC File Offset: 0x0007AFCC
		private GridPagerMode CalculatePagerMode()
		{
			GridPagerMode gridPagerMode = this.ActionBar.PagerSettings.Mode;
			if (gridPagerMode > GridPagerMode.NextPrevFirstLast && this.StoredSqlTimeout)
			{
				this.storedPagerMode = new GridPagerMode?(this.ActionBar.PagerSettings.Mode);
				gridPagerMode = (this.ActionBar.PagerSettings.Mode = GridPagerMode.NextPrevFirstLast);
				this.ActionBar.PagerDisabled = true;
			}
			else if (this.storedPagerMode != null && !this.StoredSqlTimeout)
			{
				gridPagerMode = (this.ActionBar.PagerSettings.Mode = this.storedPagerMode.Value);
				this.ActionBar.PagerDisabled = false;
			}
			return gridPagerMode;
		}

		// Token: 0x06002026 RID: 8230 RVA: 0x0007CE78 File Offset: 0x0007B078
		protected virtual void InitializeSelectArguments(DataSourceSelectArguments sa)
		{
			DataSourceView dataView = this.GetDataView();
			if (dataView.CanSort)
			{
				sa.SortExpression = this.PrimaryLevel.GetSortExpression();
			}
			if (this.AllowPaging && dataView.CanPage)
			{
				if (dataView.CanRetrieveTotalRowCount)
				{
					GridPagerMode gridPagerMode = this.CalculatePagerMode();
					if (gridPagerMode - GridPagerMode.Numeric <= 2 && this.ActionBar.PagerVisible != ActionVisible.False)
					{
						sa.RetrieveTotalRowCount = true;
					}
				}
				if (!this.FormViewMode)
				{
					int num = (!sa.RetrieveTotalRowCount) ? 1 : 0;
					int num2 = this.pageIndex;
					sa.MaximumRows = this.pageSize + num;
					sa.StartRowIndex = num2 * this.pageSize - ((num2 < 0) ? num : 0);
				}
			}
			if (this.ExcelExport && this.ExcelExportTop > 0)
			{
				sa.MaximumRows = this.ExcelExportTop;
			}
			this.startRowIndex = sa.StartRowIndex;
		}

		// Token: 0x06002027 RID: 8231 RVA: 0x0007CF48 File Offset: 0x0007B148
		protected virtual PXDSSelectArguments CreateSelectArgumentsExt()
		{
			PXDSSelectArguments pxdsselectArguments = new PXDSSelectArguments();
			if (this.parameters != null)
			{
				pxdsselectArguments.Parameters = this.Parameters.GetValues(this.Context, this);
			}
			return pxdsselectArguments;
		}

		// Token: 0x06002028 RID: 8232 RVA: 0x0007CF7C File Offset: 0x0007B17C
		protected virtual void InitializeSelectArgumentsExt(PXDSSelectArguments sa)
		{
			if (this.SearchValue == null && !string.IsNullOrEmpty(this.searchStr))
			{
				this.SearchValue = this.searchStr;
			}
			if (this.AllowSearch && this.SearchValue != null)
			{
				PXGridColumn searchColumn = this.GetSearchColumn();
				if (searchColumn != null)
				{
					string key = string.IsNullOrEmpty(searchColumn.TextField) ? searchColumn.DataField : searchColumn.TextField;
					sa.Searches[key] = this.SearchValue;
				}
			}
			if (this.AllowSearch && this.searchKey != null)
			{
				string[] dataKeyNames = this.Levels[0].DataKeyNames;
				int num = 0;
				while (num < dataKeyNames.Length && num < this.searchKey.Length)
				{
					sa.Searches[dataKeyNames[num]] = this.searchKey[num];
					num++;
				}
			}
			if (this.AllowFilter)
			{
				Guid? guid;
				if (this.filterActive || this.colsFilterActive.GetValueOrDefault() || !this.Page.IsPostBack)
				{
					if (this.filterActive)
					{
						guid = this.FilterID;
						if (guid != null && guid.GetValueOrDefault().CompareTo(PXGrid.MinFilterId) >= 0)
						{
							sa.FilterID = this.FilterID;
						}
					}
					if (this.filterRows != null)
					{
						guid = this.FilterID;
						if (guid == null || guid.GetValueOrDefault().CompareTo(PXGrid.MinFilterId) < 0 || this.colsFilterActive.GetValueOrDefault())
						{
							sa.Filters = this.filterRows.Select(delegate(PXFilterRow f)
							{
								if (f == null)
								{
									return null;
								}
								PXFilterRow pxfilterRow = (PXFilterRow)f.Clone();
								if (pxfilterRow.Condition == PXCondition.IN || pxfilterRow.Condition == PXCondition.NI)
								{
									object value = pxfilterRow.Value;
									if (FilterVariable.GetVariableType((value != null) ? value.ToString() : null) == null)
									{
										pxfilterRow.Condition = ((pxfilterRow.Condition == PXCondition.IN) ? PXCondition.EQ : PXCondition.NE);
									}
								}
								return pxfilterRow;
							}).ToArray<PXFilterRow>();
						}
					}
				}
				bool flag = sa.Filters != null && sa.Filters.Length != 0;
				guid = this.FilterID;
				if ((guid == null || guid.GetValueOrDefault().CompareTo(PXGrid.MinFilterId) < 0) && this.QuickFilters.Count > 0)
				{
					List<PXFilterRow> list = (from f in this.QuickFilters
					where f.Condition != PXCondition.EQ || f.Value != null
					select (PXFilterRow)f.Clone()).ToList<PXFilterRow>();
					list.ForEach(delegate(PXFilterRow f)
					{
						PXGridColumn pxgridColumn = this.Columns[f.DataField];
						if (!string.IsNullOrEmpty((pxgridColumn != null) ? pxgridColumn.TextFieldColumn : null))
						{
							f.DataField = pxgridColumn.TextFieldColumn;
						}
					});
					if (flag)
					{
						sa.Filters[0].OpenBrackets++;
						sa.Filters[sa.Filters.Length - 1].CloseBrackets++;
						sa.Filters[sa.Filters.Length - 1].OrOperator = false;
						list.AddRange(sa.Filters);
					}
					sa.Filters = list.ToArray();
					flag = (sa.Filters.Length != 0);
				}
				if (this.FastFilterID.Length > 0)
				{
					IFieldEditor fieldEditor = ControlHelper.FindControl(this.FastFilterID, this.Page) as IFieldEditor;
					if (fieldEditor != null)
					{
						this.fastFilter = ((fieldEditor.Value == null) ? null : fieldEditor.Value.ToString());
					}
				}
				if (!string.IsNullOrEmpty(this.fastFilter))
				{
					List<PXFilterRow> fastFilterRows = this.GetFastFilterRows();
					if (fastFilterRows.Count > 0)
					{
						if (flag)
						{
							sa.Filters[0].OpenBrackets++;
							sa.Filters[sa.Filters.Length - 1].CloseBrackets++;
							fastFilterRows.AddRange(sa.Filters);
						}
						sa.Filters = fastFilterRows.ToArray();
					}
				}
			}
		}

		// Token: 0x06002029 RID: 8233 RVA: 0x0007D324 File Offset: 0x0007B524
		private List<PXFilterRow> GetFastFilterRows()
		{
			List<PXFilterRow> list = new List<PXFilterRow>();
			if (!string.IsNullOrEmpty(this.fastFilter))
			{
				PXCondition fastFilterCondition = this.FastFilterCondition;
				string[] array = this.fastFilter.Split(new char[]
				{
					' '
				});
				PXFilterRow pxfilterRow = null;
				foreach (string dataField in this.FastFilterFields)
				{
					bool flag = true;
					foreach (string text in array)
					{
						if (!string.IsNullOrEmpty(text))
						{
							string value = text.Replace(this.FastFilterWildcard, this.DataGraph.SqlDialect.WildcardAnything);
							pxfilterRow = new PXFilterRow(dataField, fastFilterCondition, value);
							if (flag)
							{
								pxfilterRow.OpenBrackets = 1;
								flag = false;
							}
							list.Add(pxfilterRow);
						}
					}
					if (pxfilterRow != null)
					{
						pxfilterRow.CloseBrackets = 1;
						pxfilterRow.OrOperator = true;
					}
				}
				if (list.Count > 0)
				{
					list[0].OpenBrackets = 2;
					list[list.Count - 1].CloseBrackets = 2;
					list[list.Count - 1].OrOperator = false;
				}
			}
			return list;
		}

		// Token: 0x0600202A RID: 8234 RVA: 0x0007D448 File Offset: 0x0007B648
		protected override void PerformDataBinding(IEnumerable data)
		{
			this.Rows.Clear();
			this.activeRow = null;
			this.activeCell = null;
			this.noteIDField = null;
			bool flag = false;
			if (!(this.GetDataSource() is PXDataSource))
			{
				PXSelectEventArgs pxselectEventArgs = new PXSelectEventArgs(this.SelectArguments, this.argumentsExt);
				this.OnSelect(pxselectEventArgs);
				flag = pxselectEventArgs.RetainSortAndFilters;
			}
			if (this.HasDataSource)
			{
				if (this.SelectArgumentsExt.Filters == null && !flag)
				{
					if (this.filterActive || this.colsFilterActive.GetValueOrDefault())
					{
						this.FilterRows.Clear();
					}
					this.StoredFastFilter = (this.fastFilter = null);
					this.colsFilterActive = new bool?(false);
				}
				Guid? guid = this.SelectArgumentsExt.FilterID;
				if (guid == null)
				{
					guid = this.FilterID;
					if (guid != null && guid.GetValueOrDefault().CompareTo(PXGrid.MinFilterId) > 0)
					{
						guid = null;
						this.FilterID = guid;
						if (this.PreserveSortsAndFilters)
						{
							guid = this.FilterID;
							this.StoredFilterID = new Guid?(guid ?? PXGrid._FE_FILTER_ID);
						}
					}
				}
				guid = this.FilterID;
				if (guid == null && this.FilterRows.Count == 0)
				{
					this.filterActive = false;
				}
				if (string.IsNullOrEmpty(this.SelectArguments.SortExpression) && !flag)
				{
					foreach (PXGridColumn pxgridColumn in this.PrimaryLevel.Columns.Items)
					{
						pxgridColumn.SortDirection = SortDirection.None;
					}
				}
				DataSourceView dataView = this.GetDataView();
				IEnumerator en = data.GetEnumerator();
				ICollection collection = data as ICollection;
				if (collection == null)
				{
					throw new HttpException(Msg.GetLocal("PXBoundPanel with ID {0} must have a data source that implements ICollection.", new object[]
					{
						this.ID
					}));
				}
				if (!this.columnsSynchronizedAfterBinding)
				{
					this.SynchronizeColsState(dataView);
					this.columnsSynchronizedAfterBinding = true;
				}
				DataSourceViewSelectCallback dataSourceViewSelectCallback = delegate(IEnumerable enData)
				{
					data = enData;
					en = data.GetEnumerator();
					collection = (data as ICollection);
				};
				if (collection.Count == 0)
				{
					this.PerformSelectEmpty(dataSourceViewSelectCallback);
				}
				else if (collection.Count < this.PageSize)
				{
					if (this.pageIndex >= 0)
					{
						this.PerformSelectLast(dataSourceViewSelectCallback);
					}
					else
					{
						this.PerformSelectFirst(dataSourceViewSelectCallback);
					}
				}
				this.dataSourceCount = collection.Count;
				this.totalRowCount = -1;
				DataSourceSelectArguments selectArguments = this.SelectArguments;
				if (selectArguments.RetrieveTotalRowCount)
				{
					this.totalRowCount = selectArguments.TotalRowCount;
					if (this.startRowIndex != selectArguments.StartRowIndex)
					{
						this.pageIndex = selectArguments.StartRowIndex / this.pageSize;
					}
					else if (this.PageCount > 0 && this.pageIndex >= this.PageCount)
					{
						this.pageIndex = this.PageCount - 1;
					}
				}
				else if (selectArguments.MaximumRows == 0)
				{
					this.totalRowCount = collection.Count;
				}
				this.InitColumnsLayout();
				if (this.Columns.Count > 0)
				{
					bool flag2 = this.AllowPaging && this.pageSize > 0;
					if (flag2 && this.pageIndex < 0)
					{
						int num = this.pageSize;
						while (this.dataSourceCount > num && en.MoveNext())
						{
							num++;
						}
					}
					PXGridRowEventArgs pxgridRowEventArgs = new PXGridRowEventArgs(null);
					int num2 = 0;
					while (en.MoveNext())
					{
						PXGridRow pxgridRow = new PXGridRow(dataView, en.Current, num2);
						this.Rows.Add(pxgridRow);
						pxgridRowEventArgs.Row = pxgridRow;
						this.OnRowDataBound(pxgridRowEventArgs);
						if (pxgridRowEventArgs.Cancel)
						{
							break;
						}
						if (this.SyncPositionWithGraph)
						{
							PXCache cache = this.DataGraph.Views[this.DataMember].Cache;
							IBqlTable bqlTable = cache.ActiveRow;
							if (bqlTable != null && cache.ObjectsEqual(bqlTable, PXResult.Unwrap(pxgridRow.DataItem, bqlTable.GetType())))
							{
								this.ActiveRow = pxgridRow;
							}
						}
						if (flag2 && ++num2 == this.pageSize)
						{
							break;
						}
					}
				}
				if (this.Rows.Count > 0)
				{
					PXGridClientState pxgridClientState = this.ClientState;
					if (pxgridClientState.ActiveRowID.Length > 0)
					{
						this.activeRow = this.GetRowByID(pxgridClientState.ActiveRowID);
					}
					if (this.activeRow != null)
					{
						int num3 = pxgridClientState.ActiveCell;
						this.activeCell = this.activeRow.Cells[(num3 >= 0 && num3 < this.Columns.Count) ? num3 : 0];
					}
					this.SyncCurrentPosition();
				}
				this.isDataBound = true;
			}
			if (this.Rows.Count == 0)
			{
				this.dataKey = null;
				this.keyTable = null;
				this.dataValues = null;
				this.valuesTable = null;
				if (this.SyncPosition)
				{
					this.SyncCurrentPosition(this.ID);
				}
			}
			else if ((this.keyTable == null || (this.keyTable.Count == 0 && this.DataKeyNames.Length != 0)) && !this.SyncPositionWithGraph)
			{
				if (this.activeRow == null)
				{
					this.activeRow = this.Rows[0];
				}
				this.keyTable = this.activeRow.KeyTable;
			}
			base.PerformDataBinding(data);
		}

		// Token: 0x0600202B RID: 8235 RVA: 0x0007D9B8 File Offset: 0x0007BBB8
		protected void PerformDataBindingExcelExport(IEnumerable data)
		{
			this.Rows.Clear();
			this.activeRow = null;
			this.activeCell = null;
			this.noteIDField = null;
			if (!(this.GetDataSource() is PXDataSource))
			{
				PXSelectEventArgs e = new PXSelectEventArgs(this.SelectArguments, this.argumentsExt);
				this.OnSelect(e);
			}
			if (this.HasDataSource)
			{
				DataSourceView dataView = this.GetDataView();
				if (!(data is ICollection))
				{
					throw new HttpException(Msg.GetLocal("PXBoundPanel with ID {0} must have a data source that implements ICollection.", new object[]
					{
						this.ID
					}));
				}
				if (!this.columnsSynchronizedAfterBinding)
				{
					this.SynchronizeColsState(dataView);
					this.columnsSynchronizedAfterBinding = true;
				}
				this.InitColumnsLayout();
				if (this.Columns.Count > 0)
				{
					this.exportItems = this.EnumerateRows(dataView, data);
				}
				this.isDataBound = true;
			}
			base.PerformDataBinding(data);
		}

		// Token: 0x0600202C RID: 8236 RVA: 0x0007DA87 File Offset: 0x0007BC87
		private IEnumerable<PXGridRow> EnumerateRows(DataSourceView view, IEnumerable data)
		{
			PXGridRowCollection collection = new PXGridRowCollection();
			collection.Level = this.PrimaryLevel;
			IEnumerator en = data.GetEnumerator();
			PXGridRowEventArgs arg = new PXGridRowEventArgs(null);
			int i = 0;
			while (en.MoveNext())
			{
				object dataItem = en.Current;
				PXGridRow pxgridRow = new PXGridRow(view, dataItem, i);
				pxgridRow.Collection = collection;
				arg.Row = pxgridRow;
				this.OnRowDataBound(arg);
				if (arg.Cancel)
				{
					break;
				}
				yield return pxgridRow;
			}
			yield break;
		}

		// Token: 0x0600202D RID: 8237 RVA: 0x0007DAA5 File Offset: 0x0007BCA5
		protected virtual void PerformSelectRow()
		{
			this.PerformSelectRow(null, null);
		}

		// Token: 0x0600202E RID: 8238 RVA: 0x0007DAB0 File Offset: 0x0007BCB0
		private void PerformSelectRow(DataSourceSelectArguments arg, PXDSSelectArguments argExt)
		{
			if (this.GetDataSource() is PXDataSource)
			{
				PXDataSourceView pxdataSourceView = this.GetDataView() as PXDataSourceView;
				if (arg == null)
				{
					arg = new DataSourceSelectArguments(0, 1);
				}
				if (argExt == null)
				{
					argExt = new PXDSSelectArguments();
				}
				string[] dataKeyNames = this.PrimaryLevel.DataKeyNames;
				arg.SortExpression = string.Empty;
				foreach (string text in dataKeyNames)
				{
					if (string.IsNullOrEmpty(arg.SortExpression))
					{
						arg.SortExpression = text;
					}
					else
					{
						DataSourceSelectArguments dataSourceSelectArguments = arg;
						dataSourceSelectArguments.SortExpression = dataSourceSelectArguments.SortExpression + "," + text;
					}
					argExt.Searches[text] = this.DataKey[text];
				}
				if (this.parameters != null)
				{
					argExt.Parameters = this.Parameters.GetValues(this.Context, this);
				}
				if (argExt.FilterID == null)
				{
					argExt.FilterID = this.FilterID;
				}
				DataSourceViewSelectCallback dataSourceViewSelectCallback = delegate(IEnumerable data)
				{
					this.rowDataItem = this.OnSelectRow(data);
				};
				this.rowDataItem = null;
				pxdataSourceView.Select(arg, argExt, dataSourceViewSelectCallback);
			}
		}

		// Token: 0x0600202F RID: 8239 RVA: 0x0007DBBC File Offset: 0x0007BDBC
		protected virtual object OnSelectRow(IEnumerable data)
		{
			IEnumerator enumerator = data.GetEnumerator();
			if (!enumerator.MoveNext())
			{
				return null;
			}
			return enumerator.Current;
		}

		// Token: 0x06002030 RID: 8240 RVA: 0x0007DBE0 File Offset: 0x0007BDE0
		protected virtual void PerformSelectFilterNames()
		{
			DataSourceView view = this.GetFilterData();
			DataSourceSelectArguments dataSourceSelectArguments = new DataSourceSelectArguments();
			Guid? fId = null;
			string fName = null;
			PXFilterRow[] fRows = null;
			bool setFromSession = false;
			foreach (PXBaseRedirectException.Filter filter in this.ReadFilters())
			{
				if (string.Compare(filter.DataMember, this.DataMember, StringComparison.OrdinalIgnoreCase) == 0)
				{
					fId = filter.ID;
					fName = filter.Name;
					fRows = filter.FilterRows;
					if (fId == null && fName == null && (fRows == null || fRows.Length == 0))
					{
						setFromSession = true;
						Guid? storedFilterID = null;
						this.StoredFilterID = storedFilterID;
						this.FilterID = storedFilterID;
						this.filterActive = false;
						break;
					}
					break;
				}
			}
			DataSourceViewSelectCallback dataSourceViewSelectCallback = delegate(IEnumerable data)
			{
				EntityHelper entityHelper = new EntityHelper(this.DataGraph);
				IEnumerator enumerator2 = data.GetEnumerator();
				bool flag = false;
				bool flag2 = false;
				while (enumerator2.MoveNext())
				{
					object dataItem = enumerator2.Current;
					Guid guid = (Guid)ControlHelper.GetPropertyValue(view, dataItem, "FilterID");
					string text = (string)ControlHelper.GetPropertyValue(view, dataItem, "FilterName");
					bool flag3 = (bool)ControlHelper.GetPropertyValue(view, dataItem, "IsDefault");
					bool flag4 = (bool)ControlHelper.GetPropertyValue(view, dataItem, "IsShared");
					bool flag5 = (bool)ControlHelper.GetPropertyValue(view, dataItem, "IsSystem");
					string strA = (string)ControlHelper.GetPropertyValue(view, dataItem, "UserName");
					Guid? noteID = (Guid?)ControlHelper.GetPropertyValue(view, dataItem, "RefNoteID");
					this.filterNames.Add(guid, text);
					if (noteID != null)
					{
						PivotTable pivotTable = entityHelper.GetEntityRow(typeof(PivotTable), noteID) as PivotTable;
						if (pivotTable != null)
						{
							this.filterPivots.Add(guid, pivotTable.PivotTableID.Value);
						}
					}
					bool flag6 = !flag4 && !flag5;
					if ((fId != null && fId.Value == guid) || (fName != null && string.Compare(text, fName, StringComparison.OrdinalIgnoreCase) == 0))
					{
						this.FilterID = new Guid?(guid);
						this.filterActive = true;
						flag = flag4;
						setFromSession = true;
					}
					else if (flag3 && this.FilterRows.Count == 0 && !setFromSession)
					{
						bool flag7 = string.Compare(strA, PXAccess.GetUserName(), StringComparison.OrdinalIgnoreCase) == 0;
						if (flag7)
						{
							flag2 = true;
						}
						bool flag8 = flag7 || !flag2;
						if ((flag6 || flag4 || !flag) && this.StoredFilterID == null && !this.filterActiveLoaded && flag8)
						{
							this.FilterID = new Guid?(guid);
							this.filterActive = true;
							flag = flag4;
						}
					}
				}
				if (fRows != null)
				{
					this.SetDrilldownFilters(fRows);
					if (fName != null)
					{
						this.TemporaryFilterCaption = fName;
					}
				}
				if (this.pivotID == null && this.FilterID != null && this.filterPivots.ContainsKey(this.FilterID.Value))
				{
					this.pivotID = new int?(this.filterPivots[this.FilterID.Value]);
				}
				if (this.filterSelector != null)
				{
					this.FillFilterSelector();
				}
			};
			this.filterNames = new Dictionary<Guid, string>();
			this.filterPivots = new Dictionary<Guid, int>();
			view.Select(dataSourceSelectArguments, dataSourceViewSelectCallback);
			DynamicFilterManager.AppendFilterNames(this.DataGraph.GetType().FullName, this.DataMember, this.filterNames);
		}

		// Token: 0x06002031 RID: 8241 RVA: 0x0007DD48 File Offset: 0x0007BF48
		private void SetFiltersFromSession()
		{
			PXFilterRow[] array = null;
			foreach (PXBaseRedirectException.Filter filter in this.ReadFilters())
			{
				if (string.Compare(filter.DataMember, this.DataMember, StringComparison.OrdinalIgnoreCase) == 0)
				{
					array = filter.FilterRows;
					break;
				}
			}
			if (array != null && array.Length != 0)
			{
				this.SetDrilldownFilters(array);
			}
		}

		// Token: 0x06002032 RID: 8242 RVA: 0x0007DDC0 File Offset: 0x0007BFC0
		private void SetDrilldownFilters(PXFilterRow[] fRows)
		{
			List<PXFilterRow> list = new List<PXFilterRow>();
			this.FilterRows.Clear();
			for (int i = 0; i < fRows.Length; i++)
			{
				PXFilterRow pxfilterRow = (PXFilterRow)fRows[i].Clone();
				pxfilterRow.Tag = 3U;
				list.Add(pxfilterRow);
			}
			Guid? storedFilterID = new Guid?(PXGrid._DD_FILTER_ID);
			this.StoredFilterID = storedFilterID;
			this.FilterID = storedFilterID;
			this.FilterRows.AddRange(list);
			this.AppendFiltersToSession(list, PXGrid.FilterRowType.DrillDown);
			this.filterActive = true;
		}

		// Token: 0x06002033 RID: 8243 RVA: 0x0007DE48 File Offset: 0x0007C048
		private IEnumerable<PXBaseRedirectException.Filter> ReadFilters()
		{
			object obj;
			PXBaseRedirectException.Filter[] result;
			if (PXExecutionContext.Current.Bag.TryGetValue("GRID_FILTERS", out obj) && (result = (obj as PXBaseRedirectException.Filter[])) != null)
			{
				return result;
			}
			return this.ReadFiltersFromSession();
		}

		// Token: 0x06002034 RID: 8244 RVA: 0x0007DE80 File Offset: 0x0007C080
		private PXBaseRedirectException.Filter[] ReadFiltersFromSession()
		{
			PXBaseRedirectException.Filter[] result;
			try
			{
				result = PXBaseDataSource.RedirectHelper.ReadFiltersFromSession(this.Page.Request.RawUrl.ToRelativeUrl(this.Page.Request.ApplicationPath, null, true)).ToArray<PXBaseRedirectException.Filter>();
			}
			catch
			{
				result = new PXBaseRedirectException.Filter[0];
			}
			return result;
		}

		// Token: 0x06002035 RID: 8245 RVA: 0x0007DEDC File Offset: 0x0007C0DC
		private void CalculateNoteFileIndicators()
		{
			if (this.indicatorsValid)
			{
				return;
			}
			PXDataSourceView pxdataSourceView = this.GetData() as PXDataSourceView;
			if (pxdataSourceView == null || !string.IsNullOrEmpty(this.DataMember))
			{
				bool? flag = this.NoteIndicator;
				bool flag2 = HttpContext.Current == null && !PXGraph.ProxyIsActive;
				if (flag == null && !flag2)
				{
					if (pxdataSourceView != null)
					{
						flag = new bool?(pxdataSourceView.GetStateExt(null, this.NoteField) != null);
					}
					this.NoteIndicator = flag;
				}
				flag = this.FilesIndicator;
				if (flag == null && !flag2)
				{
					if (pxdataSourceView != null)
					{
						flag = new bool?(pxdataSourceView.GetStateExt(null, "NoteFiles") != null);
					}
					this.FilesIndicator = flag;
				}
				this.indicatorsValid = true;
			}
		}

		// Token: 0x06002036 RID: 8246 RVA: 0x0007DF94 File Offset: 0x0007C194
		protected virtual void PerformDelete(IOrderedDictionary keys, IOrderedDictionary oldValues)
		{
			DataSourceView dataView = this.GetDataView();
			if (base.IsBoundUsingDataSourceID && !dataView.CanDelete)
			{
				return;
			}
			PXDBDeleteEventArgs pxdbdeleteEventArgs = new PXDBDeleteEventArgs(keys, oldValues);
			this.OnRowDeleting(pxdbdeleteEventArgs);
			if (pxdbdeleteEventArgs.Cancel)
			{
				return;
			}
			PXDBDeletedEventArgs e2 = null;
			DataSourceViewOperationCallback dataSourceViewOperationCallback = delegate(int affectedRecords, Exception ex)
			{
				e2 = new PXDBDeletedEventArgs(affectedRecords, ex);
				e2.Keys = keys;
				e2.Values = oldValues;
				this.OnRowDeleted(e2);
				if (ex != null && !e2.ExceptionHandled)
				{
					return false;
				}
				this.RequiresDataBinding = true;
				return true;
			};
			if (base.IsBoundUsingDataSourceID || this.DataSource is IDataSource)
			{
				dataView.Delete(keys, oldValues, dataSourceViewOperationCallback);
			}
		}

		// Token: 0x06002037 RID: 8247 RVA: 0x0007E034 File Offset: 0x0007C234
		protected virtual void PerformInsert(IOrderedDictionary values)
		{
			DataSourceView dataView = this.GetDataView();
			if (base.IsBoundUsingDataSourceID && !dataView.CanInsert)
			{
				return;
			}
			PXDBInsertEventArgs pxdbinsertEventArgs = new PXDBInsertEventArgs(values);
			this.OnRowInserting(pxdbinsertEventArgs);
			if (pxdbinsertEventArgs.Cancel)
			{
				return;
			}
			PXDBInsertedEventArgs e2 = null;
			DataSourceViewOperationCallback dataSourceViewOperationCallback = delegate(int affectedRecords, Exception ex)
			{
				e2 = new PXDBInsertedEventArgs(affectedRecords, ex);
				e2.Values = values;
				this.OnRowInserted(e2);
				if (ex != null && !e2.ExceptionHandled)
				{
					return false;
				}
				this.RequiresDataBinding = true;
				return true;
			};
			if (base.IsBoundUsingDataSourceID || this.DataSource is IDataSource)
			{
				dataView.Insert(values, dataSourceViewOperationCallback);
			}
		}

		// Token: 0x06002038 RID: 8248 RVA: 0x0007E0C0 File Offset: 0x0007C2C0
		protected virtual void PerformUpdate(IOrderedDictionary keys, IOrderedDictionary values, IOrderedDictionary oldValues)
		{
			DataSourceView dataView = this.GetDataView();
			if (dataView == null || (base.IsBoundUsingDataSourceID && !dataView.CanUpdate))
			{
				return;
			}
			PXDBUpdateEventArgs pxdbupdateEventArgs = new PXDBUpdateEventArgs(keys, oldValues, values);
			this.OnRowUpdating(pxdbupdateEventArgs);
			if (pxdbupdateEventArgs.Cancel)
			{
				return;
			}
			PXDBUpdatedEventArgs e2 = null;
			DataSourceViewOperationCallback dataSourceViewOperationCallback = delegate(int affectedRecords, Exception ex)
			{
				e2 = new PXDBUpdatedEventArgs(affectedRecords, ex);
				e2.Keys = keys;
				e2.OldValues = oldValues;
				e2.NewValues = values;
				this.OnRowUpdated(e2);
				if (ex != null && !e2.ExceptionHandled)
				{
					return false;
				}
				this.RequiresDataBinding = true;
				return true;
			};
			if (base.IsBoundUsingDataSourceID || this.DataSource is IDataSource)
			{
				dataView.Update(keys, values, oldValues, dataSourceViewOperationCallback);
				return;
			}
			dataSourceViewOperationCallback(1, null);
		}

		// Token: 0x06002039 RID: 8249 RVA: 0x0007E180 File Offset: 0x0007C380
		protected virtual void PerformPage(int newPage)
		{
			if (!this.AllowPaging)
			{
				return;
			}
			PXPageChangeEventArgs pxpageChangeEventArgs = new PXPageChangeEventArgs(newPage);
			this.OnPageIndexChanging(pxpageChangeEventArgs);
			if (!pxpageChangeEventArgs.Cancel)
			{
				if (base.IsBoundUsingDataSourceID)
				{
					if (pxpageChangeEventArgs.NewPageIndex <= 0 || (this.PageCount != -1 && pxpageChangeEventArgs.NewPageIndex >= this.PageCount))
					{
						return;
					}
					this.pageIndex = pxpageChangeEventArgs.NewPageIndex;
				}
				this.OnPageIndexChanged(EventArgs.Empty);
				base.RequiresDataBinding = true;
			}
		}

		// Token: 0x0600203A RID: 8250 RVA: 0x0007E1F4 File Offset: 0x0007C3F4
		private List<PXFilterRow> GetQuickFilterRows(out bool isDirty, out bool hasAdvanced)
		{
			List<PXFilterRow> list = new List<PXFilterRow>();
			isDirty = (hasAdvanced = false);
			Guid? guid = this.FilterID;
			if (guid != null && guid.GetValueOrDefault().CompareTo(PXGrid.MinFilterId) >= 0)
			{
				PXCache pxcache = this.DataGraph.Caches[typeof(FilterRow)];
				foreach (PXResult<FilterRow> r in this.GetQuickFilterResultset())
				{
					FilterRow filterRow = r;
					if (pxcache.GetStatus(filterRow) != PXEntryStatus.Notchanged)
					{
						isDirty = true;
					}
					string value = filterRow.IsUsed.GetValueOrDefault() ? filterRow.ValueSt : null;
					string value2 = filterRow.IsUsed.GetValueOrDefault() ? filterRow.ValueSt2 : null;
					list.Add(new PXFilterRow(filterRow.DataField, (PXCondition)filterRow.Condition.Value, value, value2, new FilterVariableType?(FilterVariableType.CurrentUser))
					{
						OrOperator = (filterRow.Operator.GetValueOrDefault() == 1),
						OpenBrackets = filterRow.OpenBrackets.GetValueOrDefault(),
						CloseBrackets = filterRow.CloseBrackets.GetValueOrDefault()
					});
				}
				PXResultset<FilterRow> advancedFilterResultset = this.GetAdvancedFilterResultset();
				hasAdvanced = (advancedFilterResultset.Count > 0);
				if (isDirty)
				{
					return list;
				}
				foreach (object obj in pxcache.Deleted)
				{
					guid = ((FilterRow)obj).FilterID;
					Guid? guid2 = this.FilterID;
					if (guid == guid2)
					{
						isDirty = true;
						break;
					}
				}
				using (IEnumerator<PXResult<FilterRow>> enumerator = advancedFilterResultset.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						PXResult<FilterRow> r2 = enumerator.Current;
						FilterRow item = r2;
						if (pxcache.GetStatus(item) != PXEntryStatus.Notchanged)
						{
							isDirty = true;
							break;
						}
					}
					return list;
				}
			}
			PXGrid.FilterRowType type = (this.FilterID == PXGrid._DD_FILTER_ID) ? PXGrid.FilterRowType.DrillDown : PXGrid.FilterRowType.FilterEditor;
			hasAdvanced = this.FilterRows.Any((PXFilterRow f) => PXGrid.GetFilterType(f) == type);
			list = this.QuickFiltersInSession;
			if (this.FilterID != PXGrid._DD_FILTER_ID)
			{
				if (list == null || list.Count == 0)
				{
					string[] array = this.quickFilterFields;
					if (array != null && array.Length != 0)
					{
						list = (from f in this.QuickFilterFields
						select new PXFilterRow(f, PXCondition.EQ)).ToList<PXFilterRow>();
					}
				}
				list = ((list != null) ? list.Where(delegate(PXFilterRow f)
				{
					PXGridColumn pxgridColumn = this.Columns[f.DataField];
					return pxgridColumn != null && pxgridColumn.Visible;
				}).ToList<PXFilterRow>() : null);
			}
			isDirty = ((list != null && list.Count > 0) | hasAdvanced);
			return list;
		}

		// Token: 0x0600203B RID: 8251 RVA: 0x0007E56C File Offset: 0x0007C76C
		private List<PXFilterRow> GetQuickFilterRows()
		{
			bool flag;
			bool flag2;
			return this.GetQuickFilterRows(out flag, out flag2);
		}

		// Token: 0x0600203C RID: 8252 RVA: 0x0007E584 File Offset: 0x0007C784
		private List<PXFilterRow> GetAdvancedFilterRows()
		{
			List<PXFilterRow> list = this.FilterRows;
			Guid? guid;
			if (this.FilterID != null && guid.GetValueOrDefault().CompareTo(PXGrid.MinFilterId) >= 0)
			{
				list = new List<PXFilterRow>();
				PXCache pxcache = this.DataGraph.Caches[typeof(FilterRow)];
				ParameterExpression parameterExpression;
				foreach (PXResult<FilterRow> r in this.GetAdvancedFilterResultset().Where(Expression.Lambda<Func<PXResult<FilterRow>, bool>>(Expression.Equal(Expression.Property(Expression.Call(parameterExpression, methodof(PXResult.GetItem()), Array.Empty<Expression>()), methodof(FilterRow.get_IsUsed())), Expression.Convert(Expression.Constant(true, typeof(bool)), typeof(bool?))), new ParameterExpression[]
				{
					parameterExpression
				})))
				{
					FilterRow filterRow = r;
					list.Add(new PXFilterRow(filterRow.DataField, (PXCondition)filterRow.Condition.Value, filterRow.ValueSt, filterRow.ValueSt2, new FilterVariableType?(FilterVariableType.CurrentUser))
					{
						OpenBrackets = filterRow.OpenBrackets.Value,
						CloseBrackets = filterRow.CloseBrackets.Value,
						OrOperator = (filterRow.Operator.GetValueOrDefault() == 1)
					});
				}
			}
			return list;
		}

		// Token: 0x0600203D RID: 8253 RVA: 0x0007E730 File Offset: 0x0007C930
		private bool HasAdvancedFilters()
		{
			PXGrid.<>c__DisplayClass808_0 CS$<>8__locals1 = new PXGrid.<>c__DisplayClass808_0();
			Guid? guid = this.FilterID;
			if (guid != null && guid.GetValueOrDefault().CompareTo(PXGrid.MinFilterId) >= 0)
			{
				return this.GetAdvancedFilterResultset().Count > 0;
			}
			PXGrid.<>c__DisplayClass808_0 CS$<>8__locals2 = CS$<>8__locals1;
			guid = this.FilterID;
			Guid dd_FILTER_ID = PXGrid._DD_FILTER_ID;
			CS$<>8__locals2.type = ((guid == dd_FILTER_ID) ? PXGrid.FilterRowType.DrillDown : PXGrid.FilterRowType.FilterEditor);
			return this.FilterRows.Any((PXFilterRow f) => PXGrid.GetFilterType(f) == CS$<>8__locals1.type);
		}

		// Token: 0x0600203E RID: 8254 RVA: 0x0007E7CA File Offset: 0x0007C9CA
		private PXResultset<FilterRow> GetQuickFilterResultset()
		{
			return PXSelectBase<FilterRow, PXSelect<FilterRow, Where<FilterRow.filterID, Equal<Required<FilterRow.filterID>>, And<FilterRow.filterType, Equal<FilterRow.filterType.quick>>>, OrderBy<Asc<FilterRow.filterID, Asc<FilterRow.filterRowNbr>>>>.Config>.Select(this.DataGraph, new object[]
			{
				this.FilterID
			});
		}

		// Token: 0x0600203F RID: 8255 RVA: 0x0007E7EB File Offset: 0x0007C9EB
		private PXResultset<FilterRow> GetAdvancedFilterResultset()
		{
			return PXSelectBase<FilterRow, PXSelect<FilterRow, Where<FilterRow.filterID, Equal<Required<FilterRow.filterID>>, And<FilterRow.filterType, Equal<FilterRow.filterType.advanced>>>, OrderBy<Asc<FilterRow.filterID, Asc<FilterRow.filterRowNbr>>>>.Config>.Select(this.DataGraph, new object[]
			{
				this.FilterID
			});
		}

		// Token: 0x06002040 RID: 8256 RVA: 0x0007E80C File Offset: 0x0007CA0C
		private void ClearQuickFilterResultset()
		{
			PXSelectBase<FilterRow, PXSelect<FilterRow, Where<FilterRow.filterID, Equal<Required<FilterRow.filterID>>, And<FilterRow.filterType, Equal<FilterRow.filterType.quick>>>, OrderBy<Asc<FilterRow.filterID, Asc<FilterRow.filterRowNbr>>>>.Config>.Clear(this.DataGraph);
		}

		// Token: 0x06002041 RID: 8257 RVA: 0x0007E81C File Offset: 0x0007CA1C
		private bool IsFilterEditable(out bool isShared)
		{
			isShared = false;
			if (!PXGrid.IsClientFilter(this.FilterID))
			{
				FilterHeader filterHeader = PXSelectBase<FilterHeader, PXSelect<FilterHeader, Where<FilterHeader.filterID, Equal<Required<FilterHeader.filterID>>>>.Config>.Select(this.DataGraph, new object[]
				{
					this.FilterID
				});
				if (filterHeader != null && filterHeader.IsShared.GetValueOrDefault())
				{
					isShared = true;
				}
				return filterHeader != null && !filterHeader.IsSystem.GetValueOrDefault() && (!filterHeader.IsShared.GetValueOrDefault() || this.IsSharedFiltersEditable);
			}
			return true;
		}

		// Token: 0x17000B44 RID: 2884
		// (get) Token: 0x06002042 RID: 8258 RVA: 0x0007E8A3 File Offset: 0x0007CAA3
		private bool IsSharedFiltersEditable
		{
			get
			{
				return PXSiteMap.Provider.FindSiteMapNodeByScreenID("CS209010") != null;
			}
		}

		// Token: 0x06002043 RID: 8259 RVA: 0x0007E8B8 File Offset: 0x0007CAB8
		private Guid? CreateFilterHeader(string filterName, bool isDefault, bool isShared, bool isPivot = false, bool? isHidden = null, Guid? oldFilterId = null)
		{
			DataSourceView view = this.GetFilterData();
			if (view == null || !view.CanInsert)
			{
				return null;
			}
			Guid? filterID = null;
			OrderedDictionary values = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
			values["FilterName"] = filterName;
			values["IsDefault"] = isDefault;
			values["IsShared"] = isShared;
			if (isHidden != null)
			{
				values["IsHidden"] = isHidden;
			}
			DataSourceViewOperationCallback dataSourceViewOperationCallback = delegate(int affectedRecords, Exception ex)
			{
				if (ex == null && affectedRecords > 0)
				{
					if (view is PXDataSourceView)
					{
						filterID = new Guid?((Guid)((PXGuidState)values["FilterID"]).Value);
					}
					else
					{
						filterID = new Guid?((Guid)values["FilterID"]);
					}
					return true;
				}
				return false;
			};
			view.Insert(values, dataSourceViewOperationCallback);
			filterID = new Guid?(this.PersistFilter(filterID));
			if (isPivot)
			{
				string screenId = (string)PXFieldState.UnwrapValue(values["ScreenID"]);
				PivotTable pivotTable;
				if (oldFilterId == null)
				{
					pivotTable = PivotMaint.CreateNew(screenId, filterID.Value);
				}
				else
				{
					pivotTable = PivotMaint.CopyExisting(screenId, oldFilterId.Value, filterID.Value);
				}
				if (pivotTable != null)
				{
					this.pivotID = pivotTable.PivotTableID;
				}
			}
			return filterID;
		}

		// Token: 0x06002044 RID: 8260 RVA: 0x0007EA14 File Offset: 0x0007CC14
		private void UpdateFilterHeader(string filterName, bool? isDefault, bool? isShared)
		{
			DataSourceView filterData = this.GetFilterData();
			if (filterData == null || !filterData.CanUpdate)
			{
				return;
			}
			OrderedDictionary orderedDictionary = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
			orderedDictionary["FilterID"] = this.FilterID.Value;
			orderedDictionary["IsDefault"] = ((isDefault != null) ? isDefault.Value : PXCache.NotSetValue);
			orderedDictionary["IsShared"] = ((isShared != null) ? isShared.Value : PXCache.NotSetValue);
			orderedDictionary["FilterName"] = filterName;
			OrderedDictionary orderedDictionary2 = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
			orderedDictionary2["FilterID"] = this.FilterID.Value;
			DataSourceViewOperationCallback dataSourceViewOperationCallback = (int affectedRecords, Exception ex) => ex == null;
			filterData.Update(orderedDictionary2, orderedDictionary, null, dataSourceViewOperationCallback);
		}

		// Token: 0x06002045 RID: 8261 RVA: 0x0007EB0C File Offset: 0x0007CD0C
		private void StoreQuickFilter(bool createNew, string filterName, bool isShared, bool isPivot, bool persist)
		{
			string filterRowsView = this.GetFilterRowsView();
			if (!this.DataGraph.AllowInsert(filterRowsView) || !this.DataGraph.AllowDelete(filterRowsView))
			{
				return;
			}
			if (createNew || PXGrid.IsClientFilter(this.FilterID))
			{
				Guid? guid = this.FilterID;
				bool isDefault = false;
				Guid? oldFilterId = guid;
				this.FilterID = this.CreateFilterHeader(filterName, isDefault, isShared, isPivot, null, oldFilterId);
				this.reloadFilters = (this.filterActive = true);
				if (!PXGrid.IsClientFilter(guid))
				{
					using (IEnumerator<PXResult<FilterRow>> enumerator = PXSelectBase<FilterRow, PXSelect<FilterRow, Where<FilterRow.filterID, Equal<Required<FilterRow.filterID>>, And<FilterRow.filterType, Equal<FilterRow.filterType.advanced>>>>.Config>.Select(this.DataGraph, new object[]
					{
						guid.Value
					}).GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							PXResult<FilterRow> r = enumerator.Current;
							FilterRow filterRow = r;
							OrderedDictionary orderedDictionary = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
							orderedDictionary["FilterID"] = this.FilterID;
							orderedDictionary["FilterRowNbr"] = filterRow.FilterRowNbr;
							orderedDictionary["FilterType"] = filterRow.FilterType;
							orderedDictionary["DataField"] = filterRow.DataField;
							orderedDictionary["IsUsed"] = filterRow.IsUsed;
							orderedDictionary["Condition"] = filterRow.Condition;
							orderedDictionary["Operator"] = filterRow.Operator;
							orderedDictionary["ValueSt"] = filterRow.ValueSt;
							orderedDictionary["ValueSt2"] = filterRow.ValueSt2;
							orderedDictionary["OpenBrackets"] = filterRow.OpenBrackets;
							orderedDictionary["CloseBrackets"] = filterRow.CloseBrackets;
							this.DataGraph.ExecuteInsert(filterRowsView, orderedDictionary, Array.Empty<object>());
						}
						goto IL_446;
					}
				}
				if (this.FilterRowsInSession != null)
				{
					PXGrid.FilterRowType filterType = (guid == PXGrid._DD_FILTER_ID) ? PXGrid.FilterRowType.DrillDown : PXGrid.FilterRowType.FilterEditor;
					IEnumerable<PXFilterRow> filterRowsInSession = this.FilterRowsInSession;
					Func<PXFilterRow, bool> predicate;
					Func<PXFilterRow, bool> <>9__0;
					if ((predicate = <>9__0) == null)
					{
						predicate = (<>9__0 = ((PXFilterRow f) => PXGrid.GetFilterType(f) == filterType));
					}
					foreach (PXFilterRow pxfilterRow in filterRowsInSession.Where(predicate))
					{
						OrderedDictionary orderedDictionary = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
						orderedDictionary["FilterID"] = this.FilterID;
						orderedDictionary["FilterType"] = 0;
						orderedDictionary["DataField"] = pxfilterRow.DataField;
						orderedDictionary["IsUsed"] = true;
						orderedDictionary["Condition"] = (byte)pxfilterRow.Condition;
						orderedDictionary["Operator"] = ((pxfilterRow.OrOperator > false) ? 1 : 0);
						orderedDictionary["OpenBrackets"] = pxfilterRow.OpenBrackets;
						orderedDictionary["CloseBrackets"] = pxfilterRow.CloseBrackets;
						if (pxfilterRow.Value != null)
						{
							orderedDictionary["ValueSt"] = Convert.ToString(pxfilterRow.Value, CultureInfo.InvariantCulture);
						}
						if (pxfilterRow.Value2 != null)
						{
							orderedDictionary["ValueSt2"] = Convert.ToString(pxfilterRow.Value2, CultureInfo.InvariantCulture);
						}
						this.DataGraph.ExecuteInsert(filterRowsView, orderedDictionary, Array.Empty<object>());
					}
				}
				this.QuickFiltersInSession = null;
				this.ClearClientFilters();
			}
			else
			{
				if (!string.IsNullOrEmpty(filterName))
				{
					this.UpdateFilterHeader(filterName, null, new bool?(isShared));
					this.reloadFilters = true;
				}
				foreach (PXResult<FilterRow> r2 in this.GetQuickFilterResultset())
				{
					FilterRow filterRow2 = r2;
					OrderedDictionary orderedDictionary = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
					orderedDictionary["FilterID"] = filterRow2.FilterID;
					orderedDictionary["FilterRowNbr"] = filterRow2.FilterRowNbr;
					this.DataGraph.ExecuteDelete(filterRowsView, orderedDictionary, orderedDictionary, Array.Empty<object>());
				}
			}
			IL_446:
			foreach (PXFilterRow pxfilterRow2 in this.QuickFilters)
			{
				PXGridColumn pxgridColumn = this.Columns[pxfilterRow2.DataField];
				bool flag = pxfilterRow2.Condition == PXCondition.EQ && pxfilterRow2.Value == null;
				OrderedDictionary orderedDictionary = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
				orderedDictionary["FilterID"] = this.FilterID;
				orderedDictionary["FilterType"] = 1;
				orderedDictionary["DataField"] = pxfilterRow2.DataField;
				orderedDictionary["Condition"] = (byte)pxfilterRow2.Condition;
				orderedDictionary["Operator"] = ((pxfilterRow2.OrOperator > false) ? 1 : 0);
				orderedDictionary["OpenBrackets"] = pxfilterRow2.OpenBrackets;
				orderedDictionary["CloseBrackets"] = pxfilterRow2.CloseBrackets;
				orderedDictionary["IsUsed"] = !flag;
				if (pxgridColumn != null && pxgridColumn.DataType == TypeCode.DateTime)
				{
					if (pxfilterRow2.Value != null)
					{
						orderedDictionary["ValueSt"] = pxgridColumn.GetValueText(pxfilterRow2.Value, true);
					}
					if (pxfilterRow2.Value2 != null)
					{
						orderedDictionary["ValueSt2"] = pxgridColumn.GetValueText(pxfilterRow2.Value2, true);
					}
				}
				else
				{
					if (pxfilterRow2.Value != null)
					{
						orderedDictionary["ValueSt"] = Convert.ToString(pxfilterRow2.Value, CultureInfo.InvariantCulture);
					}
					if (pxfilterRow2.Value2 != null)
					{
						orderedDictionary["ValueSt2"] = Convert.ToString(pxfilterRow2.Value2, CultureInfo.InvariantCulture);
					}
				}
				this.DataGraph.ExecuteInsert(filterRowsView, orderedDictionary, Array.Empty<object>());
			}
			if (persist)
			{
				this.PersistFilter(this.FilterID);
				this.ClearQuickFilterResultset();
			}
		}

		// Token: 0x06002046 RID: 8262 RVA: 0x0007F1AC File Offset: 0x0007D3AC
		protected virtual void RemoveFilter(Guid filterID)
		{
			DataSourceView filterData = this.GetFilterData();
			if (filterData == null || !filterData.CanDelete)
			{
				return;
			}
			DataSourceViewOperationCallback dataSourceViewOperationCallback = (int affectedRecords, Exception ex) => ex == null;
			OrderedDictionary orderedDictionary = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
			orderedDictionary["FilterID"] = filterID;
			PivotMaint.DeleteExisting(filterID);
			filterData.Delete(orderedDictionary, null, dataSourceViewOperationCallback);
			this.PersistFilter(new Guid?(filterID));
		}

		// Token: 0x06002047 RID: 8263 RVA: 0x0007F224 File Offset: 0x0007D424
		internal void OnFilterRemoved(Guid filterID)
		{
			if (filterID == this.filterID)
			{
				Guid? storedFilterID = null;
				this.StoredFilterID = storedFilterID;
				this.FilterID = storedFilterID;
				this.pivotID = null;
			}
		}

		// Token: 0x06002048 RID: 8264 RVA: 0x0007F278 File Offset: 0x0007D478
		private Guid PersistFilter(Guid? filterID)
		{
			PXAdapter pxadapter = new PXAdapter(new PXView(this.DataGraph, true, new Select<FilterHeader>()));
			pxadapter.Parameters = new object[]
			{
				filterID
			};
			foreach (object obj in this.DataGraph.Actions["__PersistFilter__"].Press(pxadapter))
			{
			}
			return (Guid)pxadapter.Parameters[0];
		}

		// Token: 0x06002049 RID: 8265 RVA: 0x0007F314 File Offset: 0x0007D514
		void IPostBackEventHandler.RaisePostBackEvent(string eventArgument)
		{
			if (this.Page != null && this.Page.IsCallback)
			{
				return;
			}
			if (eventArgument == "EditRecord")
			{
				PXCommandEventArgs e = new PXCommandEventArgs("EditRecord");
				((ICommandSource)this).ExecuteCommand(e);
			}
		}

		// Token: 0x0600204A RID: 8266 RVA: 0x0007F358 File Offset: 0x0007D558
		bool IPostBackDataHandler.LoadPostData(string key, NameValueCollection postCollection)
		{
			this.InitColumnsLayout();
			if (!this.columnsSynchronized)
			{
				this.SynchronizeColsStateExt();
			}
			if (!PXGraph.ProxyIsActive)
			{
				foreach (PXFilterRow item in this.internalFilterRows)
				{
					this.FilterRows.Remove(item);
				}
				this.internalFilterRows.Clear();
				this.stateChanges = PXStateParser.Parse(this, postCollection, new PXStateChangingHandler(this.ProcessStateChange), new PXStatePostHandler(this.ProcessStatePost));
				if (this.PreservePageFinal && (this.pageAdjusted || this.AdjustPageSizeFinal == GridPageSizeMode.None))
				{
					if ((ControlHelper.IsReloadPage(this) || this.adjustOnReload) && this.StoredRowNumber > 0)
					{
						this.PageIndex = this.StoredRowNumber / this.PageSize;
					}
					else
					{
						this.StoredRowNumber = this.PageIndex * this.PageSize + ((this.rowIndex > 0) ? this.rowIndex : 0);
					}
				}
			}
			if (!string.IsNullOrEmpty(this.searchStr))
			{
				this.SearchValue = this.searchStr;
			}
			if (this.itemStateChanged && !base.RequiresDataBinding)
			{
				this.CreateChildControls();
			}
			else
			{
				this.EnsureChildControls();
			}
			if (this.filterEditor != null)
			{
				PXGrid.ManualRecursiveLoadPostData(this.filterEditor, key, postCollection);
			}
			if (!string.IsNullOrEmpty(this.editingField) && this.IsOwnCallback)
			{
				this.SyncInlineSelectorState();
			}
			this.SyncCurrentPosition();
			List<Dictionary<string, object>> list = this.rowsToMove;
			if (list != null && list.Count > 0)
			{
				PXGraph dataGraph = this.DataGraph;
				PXCache pxcache = dataGraph.Caches[dataGraph.GetItemType(this.DataMember)];
				pxcache.RowsToMove = this.rowsToMove;
				pxcache.InsertPositionMode = this.insertPosMode;
				pxcache.InsertPosition = this.insertPos;
			}
			return (this.stateChanges != null && this.stateChanges.Count > 0) || this.AllowFilter;
		}

		// Token: 0x0600204B RID: 8267 RVA: 0x0007F554 File Offset: 0x0007D754
		private static void ManualRecursiveLoadPostData(Control control, string key, NameValueCollection postCollection)
		{
			if (control is IPostBackDataHandler)
			{
				((IPostBackDataHandler)control).LoadPostData(key, postCollection);
			}
			foreach (object obj in control.Controls)
			{
				PXGrid.ManualRecursiveLoadPostData((Control)obj, key, postCollection);
			}
		}

		// Token: 0x0600204C RID: 8268 RVA: 0x0007F5C4 File Offset: 0x0007D7C4
		private bool RestoreFiltersFromSession(PXGrid.FilterRowType type)
		{
			List<PXFilterRow> list = this.FilterRowsInSession;
			List<PXFilterRow> source = this.internalFilterRows;
			if (list != null)
			{
				list = (from f in list
				where PXGrid.GetFilterType(f) == type
				select f).ToList<PXFilterRow>();
				this.filterRows = (from f in this.FilterRows
				where PXGrid.GetFilterType(f) != type
				select f).Concat(list).ToList<PXFilterRow>();
				this.internalFilterRows = (from f in source
				where PXGrid.GetFilterType(f) != type
				select f).Concat(list).ToList<PXFilterRow>();
				return list.Count > 0;
			}
			return false;
		}

		// Token: 0x0600204D RID: 8269 RVA: 0x0007F660 File Offset: 0x0007D860
		private void AppendFiltersToSession(IEnumerable<PXFilterRow> rows, PXGrid.FilterRowType type)
		{
			List<PXFilterRow> list = this.FilterRowsInSession;
			if (list != null)
			{
				list = (from f in list
				where PXGrid.GetFilterType(f) != type
				select f).ToList<PXFilterRow>();
			}
			if (rows != null)
			{
				rows = rows.Where(delegate(PXFilterRow f)
				{
					PXFilterEditor.FilterRow filterRow = f as PXFilterEditor.FilterRow;
					return PXGrid.GetFilterType(f) == type && (filterRow == null || filterRow.IsUsed);
				}).ToList<PXFilterRow>();
			}
			if (list != null)
			{
				this.FilterRowsInSession = ((rows != null) ? list.Concat(rows).ToList<PXFilterRow>() : list);
				return;
			}
			this.FilterRowsInSession = ((rows != null) ? rows.ToList<PXFilterRow>() : null);
		}

		// Token: 0x0600204E RID: 8270 RVA: 0x0007F6E8 File Offset: 0x0007D8E8
		private static bool IsClientFilter(Guid? filterID)
		{
			return filterID == null || filterID == PXGrid._FE_FILTER_ID || filterID == PXGrid._DD_FILTER_ID;
		}

		// Token: 0x0600204F RID: 8271 RVA: 0x0007F744 File Offset: 0x0007D944
		void IPostBackDataHandler.RaisePostDataChangedEvent()
		{
			if (this.PreserveSortsAndFilters)
			{
				if (ControlHelper.IsReloadPage(this))
				{
					Guid? guid = this.FilterID;
					this.FilterID = ((guid != null) ? guid : this.StoredFilterID);
					bool flag;
					if (!this.filterActive)
					{
						guid = this.FilterID;
						flag = (guid != null);
					}
					else
					{
						flag = true;
					}
					this.filterActive = flag;
					this.fastFilter = this.StoredFastFilter;
					if (PXGrid.IsClientFilter(this.FilterID))
					{
						this.quickFilters = this.QuickFiltersInSession;
					}
					Dictionary<string, SortDirection> storedSorts = this.StoredSorts;
					if (storedSorts != null)
					{
						foreach (PXGridColumn pxgridColumn in this.PrimaryLevel.Columns.Items)
						{
							SortDirection sortDirection;
							if (storedSorts.TryGetValue(pxgridColumn.GetKey(), out sortDirection))
							{
								pxgridColumn.SortDirection = sortDirection;
							}
						}
					}
					if (this.RestoreFiltersFromSession(PXGrid.FilterRowType.Column))
					{
						this.colsFilterActive = new bool?(true);
					}
				}
				else if (!base.DesignMode)
				{
					Guid? guid = this.FilterID;
					this.StoredFilterID = new Guid?(guid ?? PXGrid._FE_FILTER_ID);
					this.AppendFiltersToSession(this.filterRows, PXGrid.FilterRowType.Column);
					this.StoredFastFilter = this.fastFilter;
					guid = this.FilterID;
					if (guid == null || guid.GetValueOrDefault().CompareTo(PXGrid.MinFilterId) < 0)
					{
						this.QuickFiltersInSession = this.QuickFilters;
					}
				}
			}
			if (PXGrid.IsClientFilter(this.FilterID))
			{
				PXGrid.<>c__DisplayClass827_0 CS$<>8__locals1 = new PXGrid.<>c__DisplayClass827_0();
				PXGrid.<>c__DisplayClass827_0 CS$<>8__locals2 = CS$<>8__locals1;
				Guid? guid = this.FilterID;
				Guid dd_FILTER_ID = PXGrid._DD_FILTER_ID;
				CS$<>8__locals2.filterType = ((guid == dd_FILTER_ID) ? PXGrid.FilterRowType.DrillDown : PXGrid.FilterRowType.FilterEditor);
				if (this.filterEditor != null && this.filterEditor.FilterLoaded)
				{
					string b = this.Page.Request.Form["__CALLBACKID"];
					guid = this.filterEditor.FilterID;
					Guid? guid2 = this.FilterID;
					if (guid == guid2 && this.UniqueID == b)
					{
						this.AppendFiltersToSession(this.filterEditor.FilterRows, CS$<>8__locals1.filterType);
					}
					bool flag2 = false;
					(from f in this.FilterRows
					where PXGrid.GetFilterType(f) == CS$<>8__locals1.filterType
					select f).ToArray<PXFilterRow>().ForEach(delegate(PXFilterRow f)
					{
						this.FilterRows.Remove(f);
					});
					foreach (PXFilterEditor.FilterRow filterRow in this.filterEditor.FilterRows)
					{
						if (filterRow.IsUsed)
						{
							PXFilterRow pxfilterRow = ((ICloneable)filterRow).Clone() as PXFilterRow;
							this.FilterRows.Add(pxfilterRow);
							this.internalFilterRows.Add(pxfilterRow);
							pxfilterRow.Tag = CS$<>8__locals1.filterType;
							if (!flag2)
							{
								pxfilterRow.OpenBrackets++;
								flag2 = true;
							}
						}
					}
					if (flag2)
					{
						this.FilterRows[this.FilterRows.Count - 1].CloseBrackets++;
						if (!this.filterActiveLoaded)
						{
							this.filterActive = true;
						}
					}
				}
				else if (this.RestoreFiltersFromSession(CS$<>8__locals1.filterType))
				{
					if (this.FilterID == null)
					{
						this.FilterID = new Guid?(PXGrid._FE_FILTER_ID);
					}
					if (!this.filterActiveLoaded)
					{
						this.filterActive = true;
					}
				}
			}
			if (this.stateChanges != null)
			{
				foreach (PXStateChangeInfo pxstateChangeInfo in this.stateChanges)
				{
					if (pxstateChangeInfo.Owner == this)
					{
						string propertyName = pxstateChangeInfo.PropertyName;
						if (!(propertyName == "PageIndex"))
						{
							if (propertyName == "SearchValue")
							{
								this.SearchValue = pxstateChangeInfo.ClientValue;
							}
						}
						else
						{
							this.OnPageIndexChanged(EventArgs.Empty);
						}
					}
				}
			}
			PXFilterEditor pxfilterEditor = this.filterEditor;
			if (pxfilterEditor != null)
			{
				if (!pxfilterEditor.FilterLoaded)
				{
					if (pxfilterEditor.FilterID == null)
					{
						pxfilterEditor.FilterID = this.FilterID;
					}
					if (pxfilterEditor.FilterID == PXGrid._FE_FILTER_ID)
					{
						pxfilterEditor.SetDataSource(this.FilterRowsInSession, PXGrid.FilterRowType.FilterEditor);
					}
					else if (pxfilterEditor.FilterID == PXGrid._DD_FILTER_ID)
					{
						pxfilterEditor.SetDataSource(this.FilterRowsInSession, PXGrid.FilterRowType.DrillDown);
					}
				}
				pxfilterEditor.RaisePostDataChangedEventInternal();
			}
			this.StoreSortsAndFilters();
		}

		// Token: 0x06002050 RID: 8272 RVA: 0x0007FC50 File Offset: 0x0007DE50
		private void StoreSortsAndFilters()
		{
			if (!this._selectorBound)
			{
				PXDataSource pxdataSource = this.GetDataSource() as PXDataSource;
				if (pxdataSource != null)
				{
					pxdataSource.StoreSorts(this.DataMember, delegate
					{
						object[] array;
						string[] array2;
						bool[] descendings;
						PXBaseDataSource.ExtractSearchValues(this.PrimaryLevel.GetSortExpression(), out array, out array2, out descendings);
						return new PXSortColumnsTuple(array2, descendings, array2 != null && !array2.Any<string>());
					});
					pxdataSource.StoreFilters(this.DataMember, delegate
					{
						PXDSSelectArguments pxdsselectArguments = this.CreateSelectArgumentsExt();
						this.InitializeSelectArgumentsExt(pxdsselectArguments);
						return new PXFilterTuple(pxdsselectArguments.FilterID, pxdsselectArguments.Filters);
					});
				}
			}
		}

		// Token: 0x06002051 RID: 8273 RVA: 0x0007FCA4 File Offset: 0x0007DEA4
		private void ProcessStatePost(PXStatePostArgs arg)
		{
			if (arg.Owner is PXGridColumn && arg.Name == "Visible" && !this.IsLayoutReset)
			{
				((PXGridColumn)arg.Owner).VisiblePosted = true;
			}
		}

		// Token: 0x06002052 RID: 8274 RVA: 0x0007FCE0 File Offset: 0x0007DEE0
		private bool SetFilterRowValue(List<PXFilterRow> owner, PXFilterRow row, object newVal, bool isValue)
		{
			bool flag = owner == this.FilterRows;
			PXGridColumn pxgridColumn = this.Columns[row.DataField];
			if (newVal != null && newVal.ToString().Length > 0)
			{
				if (pxgridColumn != null)
				{
					if (row.Condition != PXCondition.IN || !isValue)
					{
						TypeCode typeCode = string.IsNullOrEmpty(pxgridColumn.TextField) ? pxgridColumn.DataType : TypeCode.String;
						object obj = RelativeDatesManager.IsRelativeDatesString(newVal as string) ? newVal.ToString() : Convert.ChangeType(newVal, typeCode, CultureInfo.InvariantCulture);
						if (isValue)
						{
							row.Value = obj;
						}
						else
						{
							row.Value2 = obj;
						}
						return true;
					}
					row.Condition = PXCondition.EQ;
					if (pxgridColumn.ValueItems.MultiSelect)
					{
						object value = Convert.ChangeType(newVal, pxgridColumn.DataType, CultureInfo.InvariantCulture);
						row.Value = value;
					}
					else
					{
						string[] array = newVal.ToString().Split(new char[]
						{
							','
						});
						if (pxgridColumn.AllowNull && (array[0] == "<null>" || string.IsNullOrEmpty(array[0])))
						{
							row.Condition = PXCondition.ISNULL;
						}
						else
						{
							row.Value = Convert.ChangeType(array[0], pxgridColumn.DataType, CultureInfo.InvariantCulture);
						}
						if (array.Length > 1)
						{
							row.OpenBrackets++;
							row.OrOperator = true;
							for (int i = 1; i < array.Length; i++)
							{
								owner.Add(row = new PXFilterRow(row.DataField, PXCondition.EQ, null));
								row.OrOperator = true;
								if (flag)
								{
									row.Tag = PXGrid.FilterRowType.Column;
									this.internalFilterRows.Add(row);
								}
								if (pxgridColumn.AllowNull && (array[i] == "<null>" || string.IsNullOrEmpty(array[i])))
								{
									row.Condition = PXCondition.ISNULL;
								}
								else
								{
									row.Value = Convert.ChangeType(array[i], pxgridColumn.DataType, CultureInfo.InvariantCulture);
								}
							}
							row.CloseBrackets++;
							row.OrOperator = false;
						}
					}
				}
				else
				{
					string text = newVal.ToString();
					if (isValue)
					{
						row.Value = text;
					}
					else
					{
						row.Value2 = text;
					}
				}
				if (row.Value is string)
				{
					row.Value = row.Value.ToString().Trim();
				}
				if (row.Value2 is string)
				{
					row.Value2 = row.Value2.ToString().Trim();
				}
			}
			else if (row.Condition == PXCondition.IN)
			{
				row.Condition = PXCondition.EQ;
			}
			return false;
		}

		// Token: 0x06002053 RID: 8275 RVA: 0x0007FF60 File Offset: 0x0007E160
		private void ProcessStateChange(PXStateChangingArgs arg)
		{
			object newValue = arg.NewValue;
			if (arg.Owner == this.ClientState)
			{
				if (arg.Name == "ColumnsOrder")
				{
					this.ClientState.ColumnsOrder = (string)newValue;
					this.RestoreColsOrder(this.ClientState.ColumnsOrder);
					this.itemStateChanged = true;
					return;
				}
			}
			else
			{
				if (arg.Owner is PXGridColumn)
				{
					this.itemStateChanged = true;
					return;
				}
				if (arg.Owner is PXFilterRow)
				{
					PXFilterRow pxfilterRow = (PXFilterRow)arg.Owner;
					bool flag = this.FilterRows.Contains(pxfilterRow);
					List<PXFilterRow> owner = flag ? this.FilterRows : this.QuickFilters;
					if (flag)
					{
						pxfilterRow.Tag = PXGrid.FilterRowType.Column;
					}
					string name = arg.Name;
					if (!(name == "Condition"))
					{
						if (!(name == "Value") && !(name == "Value2"))
						{
							if (!(name == "DataField"))
							{
								return;
							}
							pxfilterRow.DataField = arg.NewValue.ToString();
							return;
						}
						else if (this.SetFilterRowValue(owner, pxfilterRow, arg.NewValue, arg.Name == "Value"))
						{
							arg.Cancel = true;
							return;
						}
					}
					else
					{
						this.colsFilterActive = new bool?(true);
						PXGridColumn pxgridColumn = this.Columns[pxfilterRow.DataField];
						if (pxgridColumn != null)
						{
							pxfilterRow.Condition = (PXCondition)Enum.Parse(typeof(PXCondition), arg.NewValue.ToString());
							if (pxfilterRow.Value != null)
							{
								this.SetFilterRowValue(owner, pxfilterRow, pxfilterRow.Value, true);
							}
							if (pxfilterRow.Value2 != null)
							{
								this.SetFilterRowValue(owner, pxfilterRow, pxfilterRow.Value2, false);
							}
							arg.Cancel = true;
							if (flag)
							{
								pxgridColumn.FilterPosted = true;
							}
						}
						if (flag)
						{
							this.internalFilterRows.Add(pxfilterRow);
							return;
						}
					}
				}
				else if (arg.Owner == this)
				{
					string name = arg.Name;
					if (name != null)
					{
						switch (name.Length)
						{
						case 7:
						{
							char c = name[0];
							if (c != 'D')
							{
								if (c != 'P')
								{
									return;
								}
								if (!(name == "PivotID"))
								{
									return;
								}
								this.pivotID = new int?(int.Parse(newValue.ToString()));
								this.itemStateChanged = true;
								return;
							}
							else
							{
								if (!(name == "DataKey"))
								{
									return;
								}
								if (newValue != null && this.Levels.Count > 0)
								{
									this.ExtractDataKey(this.KeyTable, newValue.ToString());
									return;
								}
							}
							break;
						}
						case 8:
						{
							char c = name[0];
							if (c != 'F')
							{
								if (c != 'I')
								{
									if (c != 'R')
									{
										return;
									}
									if (!(name == "RowIndex"))
									{
										return;
									}
									this.rowIndex = int.Parse(newValue.ToString());
									return;
								}
								else
								{
									if (!(name == "InputBox"))
									{
										return;
									}
									this.inputBoxLoaded = bool.Parse(newValue.ToString());
									return;
								}
							}
							else
							{
								if (!(name == "FilterID"))
								{
									return;
								}
								string text = arg.NewValue.ToString();
								if (text.Length > 0)
								{
									this.FilterID = new Guid?(Guid.Parse(text));
									return;
								}
							}
							break;
						}
						case 9:
						{
							char c = name[0];
							if (c <= 'I')
							{
								if (c != 'F')
								{
									if (c != 'I')
									{
										return;
									}
									if (!(name == "InsertPos"))
									{
										return;
									}
									if (newValue != null && this.Levels.Count > 0)
									{
										this.insertPos = new Dictionary<string, object>();
										this.ExtractDataKey(this.insertPos, newValue.ToString());
										return;
									}
								}
								else
								{
									if (!(name == "FormLevel"))
									{
										return;
									}
									this.formLevel = new int?(int.Parse(newValue.ToString()));
									if (this.formLevel != null)
									{
										PXGridLevel pxgridLevel = this.Levels[this.formLevel.Value];
										if (pxgridLevel.FormView != null)
										{
											pxgridLevel.FormView.AutoRepaint = true;
										}
										this.AutoRepaint = false;
										return;
									}
								}
							}
							else if (c != 'P')
							{
								if (c != 'S')
								{
									return;
								}
								if (!(name == "SearchKey"))
								{
									return;
								}
								if (newValue != null && this.Levels.Count > 0)
								{
									string[] dataKeyNames = this.Levels[0].DataKeyNames;
									string[] array = newValue.ToString().Split(new char[]
									{
										'|'
									});
									this.searchKey = new object[array.Length];
									for (int i = 0; i < array.Length; i++)
									{
										if (i >= dataKeyNames.Length)
										{
											return;
										}
										PXGridColumn pxgridColumn2 = this.Levels[0].Columns[dataKeyNames[i]];
										if (pxgridColumn2 != null)
										{
											this.searchKey[i] = pxgridColumn2.GetValueFromText(array[i]);
										}
									}
								}
							}
							else
							{
								if (!(name == "PageIndex"))
								{
									return;
								}
								PXPageChangeEventArgs pxpageChangeEventArgs = new PXPageChangeEventArgs((int)newValue);
								this.OnPageIndexChanging(pxpageChangeEventArgs);
								arg.Cancel = pxpageChangeEventArgs.Cancel;
								arg.StoreChange = !pxpageChangeEventArgs.Cancel;
								return;
							}
							break;
						}
						case 10:
						{
							char c = name[0];
							if (c != 'D')
							{
								if (c != 'F')
								{
									if (c != 'R')
									{
										return;
									}
									if (!(name == "RowsToMove"))
									{
										return;
									}
									if (newValue != null && this.Levels.Count > 0)
									{
										this.rowsToMove = new List<Dictionary<string, object>>();
										foreach (string text2 in newValue.ToString().Split(new char[]
										{
											'&'
										}))
										{
											if (!string.IsNullOrEmpty(text2))
											{
												Dictionary<string, object> dictionary = new Dictionary<string, object>();
												this.ExtractDataKey(dictionary, text2);
												this.rowsToMove.Add(dictionary);
											}
										}
										return;
									}
								}
								else
								{
									if (!(name == "FastFilter"))
									{
										return;
									}
									this.fastFilter = ((newValue != null) ? newValue.ToString() : string.Empty);
									return;
								}
							}
							else
							{
								if (!(name == "DataValues"))
								{
									return;
								}
								if (newValue != null && newValue.ToString() != string.Empty && this.Levels.Count > 0 && !this.IsLayoutReset && !this.IsColumnsDialogLoad)
								{
									XmlDocument xmlDocument = new XmlDocument
									{
										XmlResolver = null
									};
									xmlDocument.LoadXml(newValue.ToString());
									this.newRowActive = !string.IsNullOrEmpty(xmlDocument.DocumentElement.GetAttribute("IsNew"));
									this.ExtractRowData(xmlDocument.DocumentElement, this.PrimaryLevel, this.KeyTable, this.ValuesTable, null);
									return;
								}
							}
							break;
						}
						case 11:
						{
							char c = name[0];
							if (c != 'I')
							{
								if (c != 'S')
								{
									return;
								}
								if (!(name == "SearchValue"))
								{
									return;
								}
								this.searchStr = ((newValue != null) ? newValue.ToString() : string.Empty);
								return;
							}
							else
							{
								if (!(name == "InsertAfter"))
								{
									return;
								}
								this.insertPosMode = true;
								return;
							}
							break;
						}
						case 12:
						{
							char c = name[1];
							if (c <= 'a')
							{
								if (c != 'E')
								{
									if (c != 'a')
									{
										return;
									}
									if (!(name == "PageAdjusted"))
									{
										return;
									}
									this.pageAdjusted = true;
									return;
								}
								else
								{
									if (!(name == "PEditVisible"))
									{
										return;
									}
									this.pivotEditorVisible = true;
									return;
								}
							}
							else if (c != 'd')
							{
								if (c != 'i')
								{
									return;
								}
								if (!(name == "FilterActive"))
								{
									return;
								}
								this.filterActive = bool.Parse(newValue.ToString());
								this.filterActiveLoaded = true;
								if (this.filterActive && this.filterEditor == null)
								{
									this.itemStateChanged = true;
									return;
								}
							}
							else
							{
								if (!(name == "EditingField"))
								{
									return;
								}
								if (!this.IsLayoutReset && !this.IsColumnsDialogLoad)
								{
									this.editingField = newValue.ToString();
								}
							}
							break;
						}
						case 13:
							if (!(name == "TotalRowCount"))
							{
								return;
							}
							this.totalRowCount = int.Parse(newValue.ToString());
							return;
						case 14:
							if (!(name == "AdjustOnReload"))
							{
								return;
							}
							this.adjustOnReload = true;
							return;
						case 15:
							if (!(name == "IsFilterVisited"))
							{
								return;
							}
							this.isFilterVisited = bool.Parse(newValue.ToString());
							return;
						default:
							return;
						}
					}
				}
			}
		}

		// Token: 0x06002054 RID: 8276 RVA: 0x000807E0 File Offset: 0x0007E9E0
		private void ExtractDataKey(IDictionary dic, string v)
		{
			string[] dataKeyNames = this.Levels[0].DataKeyNames;
			string[] array = v.Split(new char[]
			{
				'|'
			});
			int num = 0;
			while (num < array.Length && num < dataKeyNames.Length)
			{
				PXGridColumn pxgridColumn = this.Levels[0].Columns[dataKeyNames[num]];
				if (pxgridColumn != null)
				{
					dic[dataKeyNames[num]] = pxgridColumn.GetValueFromText(array[num]);
				}
				num++;
			}
		}

		// Token: 0x17000B45 RID: 2885
		// (get) Token: 0x06002055 RID: 8277 RVA: 0x00080854 File Offset: 0x0007EA54
		List<PXCallbackCommand> IPXCallbackHandler.CallbackCommands
		{
			get
			{
				if (this.callbackCommands == null)
				{
					List<PXCallbackCommand> list = new List<PXCallbackCommand>();
					list.Add(this.CallbackCommands.Refresh);
					list.Add(this.CallbackCommands.Save);
					list.Add(this.CallbackCommands.FetchRow);
					list.Add(this.CallbackCommands.InitRow);
					list.Add(this.CallbackCommands.NoteShow);
					list.Add(this.CallbackCommands.NoteSave);
					list.Add(this.CallbackCommands.FilterShow);
					list.Add(this.CallbackCommands.ExportExcel);
					list.Add(this.CallbackCommands.FilesMenu);
					list.Add(this.CallbackCommands.LayoutSave);
					list.Add(this.CallbackCommands.LayoutReset);
					list.Add(this.CallbackCommands.Navigate);
					PXCallbackCommand pxcallbackCommand = new PXCallbackCommand("FilterDialog");
					pxcallbackCommand.SetDefault(true, RepaintMode.None, false, PostDataMode.Self, true);
					list.Add(pxcallbackCommand);
					pxcallbackCommand = new PXCallbackCommand("ColumnsDialog");
					pxcallbackCommand.SetDefault(true, RepaintMode.None, false, PostDataMode.Self, true);
					list.Add(pxcallbackCommand);
					if (PXContext.PXIdentity.User.IsInRole(PXAccess.GetAdministratorRoles().First<string>()))
					{
						PXCallbackCommand pxcallbackCommand2 = new PXCallbackCommand("DelDefault");
						pxcallbackCommand2.SetDefault(true, RepaintMode.None, false, PostDataMode.Self, false);
						list.Add(pxcallbackCommand2);
					}
					pxcallbackCommand = new PXCallbackCommand("FilterSave");
					pxcallbackCommand.SetDefault(true, RepaintMode.None, false, PostDataMode.Self, true);
					list.Add(pxcallbackCommand);
					this.callbackCommands = list;
				}
				return this.callbackCommands;
			}
		}

		// Token: 0x06002056 RID: 8278 RVA: 0x000809DC File Offset: 0x0007EBDC
		private void SyncCurrentPosition(string id)
		{
			if (!string.IsNullOrEmpty(id))
			{
				string[] array = id.Split(new char[]
				{
					','
				}, StringSplitOptions.RemoveEmptyEntries);
				PageInfo.DataSourceInfo dataSourceInfo;
				if (!string.IsNullOrEmpty(this.DataSourceID) && PageInfo.Current.DataSources.TryGetValue(this.DataSourceID, out dataSourceInfo) && dataSourceInfo.DataSource is PXDataSource)
				{
					PXDataSource pxdataSource = (PXDataSource)dataSourceInfo.DataSource;
					foreach (string id2 in array)
					{
						pxdataSource.SynchronizeControl(id2);
					}
				}
			}
		}

		// Token: 0x06002057 RID: 8279 RVA: 0x00080A65 File Offset: 0x0007EC65
		private void SyncCurrentPosition()
		{
			if (this.SyncPosition && this.DataValues.Values.Count > 0)
			{
				this.SyncCurrentPosition(this.ID);
			}
		}

		// Token: 0x06002058 RID: 8280 RVA: 0x00080A90 File Offset: 0x0007EC90
		void ICallbackEventHandler.RaiseCallbackEvent(string arg)
		{
			if (this.FormViewMode)
			{
				string a = arg.FirstSegment('|');
				if (a == "Refresh" || a == "Save")
				{
					PXFormView formView = this.Levels[this.formLevel.Value].FormView;
					if (formView != null)
					{
						((ICallbackEventHandler)formView).RaiseCallbackEvent(arg);
					}
					return;
				}
			}
			this.callback = PXCallbackManager.GetInstance(this);
			this.callback.ProcessCallBack(arg, new PXCallbackExecMethod(this.ExecuteCallback));
		}

		// Token: 0x06002059 RID: 8281 RVA: 0x00080B14 File Offset: 0x0007ED14
		private void ExecuteCallback(PXCallbackCommand cmd, string strData)
		{
			this.SyncCurrentPosition(this.DependsOnControlIDs);
			string name = cmd.Name;
			if (name != null)
			{
				switch (name.Length)
				{
				case 4:
					if (!(name == "Save"))
					{
						return;
					}
					this.updateError = false;
					((IPXDataControl)this).CommitDataChanges(strData);
					return;
				case 5:
				case 6:
					break;
				case 7:
				{
					char c = name[0];
					if (c != 'I')
					{
						if (c != 'R')
						{
							return;
						}
						if (!(name == "Refresh"))
						{
							return;
						}
						if (strData == "ClearCache" && !string.IsNullOrEmpty(this.DataMember))
						{
							this.DataGraph.Views[this.DataMember].Cache.Clear();
						}
						if (!this.IsPivotMode)
						{
							this.NeedRepaintRows = true;
							return;
						}
					}
					else
					{
						if (!(name == "InitRow"))
						{
							return;
						}
						if (!string.IsNullOrEmpty(strData))
						{
							XmlDocument xmlDocument = new XmlDocument
							{
								XmlResolver = null
							};
							xmlDocument.LoadXml(strData);
							this.updateResult = this.InitRowData(xmlDocument.DocumentElement);
							return;
						}
						this.updateResult = this.InitRowData();
						return;
					}
					break;
				}
				case 8:
				{
					char c = name[1];
					if (c != 'a')
					{
						if (c != 'e')
						{
							if (c != 'o')
							{
								return;
							}
							if (name == "NoteShow")
							{
								cmd.Tag = this.GetNote();
								return;
							}
							if (!(name == "NoteSave"))
							{
								return;
							}
							this.SaveNote(strData);
							return;
						}
						else
						{
							if (!(name == "FetchRow"))
							{
								return;
							}
							if (!string.IsNullOrEmpty(strData))
							{
								XmlDocument xmlDocument2 = new XmlDocument
								{
									XmlResolver = null
								};
								xmlDocument2.LoadXml(strData);
								this.updateResult = this.RefetchRowData(xmlDocument2.DocumentElement);
								return;
							}
							if (this.ValuesTable.Count > 0)
							{
								OrderedDictionary values = PXGrid.CloneDictionary(this.ValuesTable);
								this.updateResult = this.RefetchRowData(this.KeyTable, values);
								return;
							}
						}
					}
					else
					{
						if (!(name == "Navigate"))
						{
							return;
						}
						cmd.Tag = this.PrimaryLevel.Columns[strData];
						if (!this.SyncPosition && this.DataValues.Values.Count > 0)
						{
							this.SyncCurrentPosition(this.ID);
							return;
						}
					}
					break;
				}
				case 9:
					name == "FilesMenu";
					return;
				case 10:
				{
					char c = name[0];
					if (c != 'D')
					{
						if (c != 'F')
						{
							if (c != 'L')
							{
								return;
							}
							if (!(name == "LayoutSave"))
							{
								return;
							}
							string[] array = strData.Split(new char[]
							{
								'@'
							});
							if (array.Length == 2)
							{
								strData = array[1];
							}
							else
							{
								this.NeedRepaintRows = true;
							}
							cmd.Tag = this.SaveLayout(strData);
							return;
						}
						else
						{
							if (!(name == "FilterSave"))
							{
								return;
							}
							if (!string.IsNullOrEmpty(strData))
							{
								string[] array2 = strData.Split(new char[]
								{
									'|'
								});
								bool createNew = int.Parse(array2[0]) == 1;
								bool isShared = int.Parse(array2[1]) == 1;
								bool isPivot = array2.Length > 3 && int.Parse(array2[3]) > 0;
								this.StoreQuickFilter(createNew, array2[2], isShared, isPivot, true);
							}
							else
							{
								this.StoreQuickFilter(false, null, false, false, false);
							}
							this.NeedRepaintRows = true;
							return;
						}
					}
					else
					{
						if (!(name == "DelDefault"))
						{
							return;
						}
						PXDataSource pxdataSource = this.GetDataSource() as PXDataSource;
						if (pxdataSource == null)
						{
							return;
						}
						pxdataSource.ResetGridDefaultPreferences(this.DataMember);
					}
					break;
				}
				case 11:
				{
					char c = name[0];
					if (c != 'E')
					{
						if (c != 'L')
						{
							return;
						}
						if (!(name == "LayoutReset"))
						{
							return;
						}
						cmd.Tag = this.ResetLayout();
						this.NeedRepaintRows = true;
						return;
					}
					else
					{
						if (!(name == "ExportExcel"))
						{
							return;
						}
						PXGrid.ExportProcessInfo exportProcessInfo = PXGrid.ExportProcessInfo.Parse(strData);
						this.CurrentExportProcess = exportProcessInfo;
						this.ExecuteExportProcess(exportProcessInfo);
						return;
					}
					break;
				}
				case 12:
					if (!(name == "FilterRemove"))
					{
						return;
					}
					if (this.FilterID != null)
					{
						this.RemoveFilter(this.FilterID.Value);
						Guid? storedFilterID = null;
						this.StoredFilterID = storedFilterID;
						this.FilterID = storedFilterID;
						this.pivotID = null;
						if (this.RestoreFiltersFromSession(PXGrid.FilterRowType.FilterEditor))
						{
							this.FilterID = new Guid?(PXGrid._FE_FILTER_ID);
							this.filterActive = true;
						}
						this.NeedRepaintRows = (this.reloadFilters = true);
						return;
					}
					break;
				default:
					return;
				}
			}
		}

		// Token: 0x0600205A RID: 8282 RVA: 0x00080F9C File Offset: 0x0007F19C
		string ICallbackEventHandler.GetCallbackResult()
		{
			if (this.callback != null)
			{
				return this.callback.GetCallbackResult(new PXCallbackResultMethod(this.GetCallbackResult));
			}
			PXFormView formView = this.Levels[this.formLevel.Value].FormView;
			if (formView == null)
			{
				return string.Empty;
			}
			return ((ICallbackEventHandler)formView).GetCallbackResult();
		}

		// Token: 0x0600205B RID: 8283 RVA: 0x00080FF4 File Offset: 0x0007F1F4
		private string GetCallbackResult(PXCallbackCommand cmd)
		{
			string text = string.Empty;
			string name = cmd.Name;
			if (name != null)
			{
				switch (name.Length)
				{
				case 4:
					if (!(name == "Save"))
					{
						return text;
					}
					break;
				case 5:
				case 6:
				case 14:
				case 15:
				case 16:
				case 17:
					return text;
				case 7:
				{
					char c = name[0];
					if (c != 'I')
					{
						if (c != 'R')
						{
							return text;
						}
						if (!(name == "Refresh"))
						{
							return text;
						}
						if (!this.isDataBound || base.RequiresDataBinding)
						{
							this.DataBind();
						}
						return ((IPXDataControl)this).GetClientData();
					}
					else
					{
						if (!(name == "InitRow"))
						{
							return text;
						}
						return this.updateResult;
					}
					break;
				}
				case 8:
				{
					char c = name[1];
					if (c != 'a')
					{
						if (c != 'e')
						{
							if (c != 'o')
							{
								return text;
							}
							if (!(name == "NoteShow"))
							{
								return text;
							}
							StringWriter textWriter = new StringWriter();
							XmlWriter writer = ControlHelper.CreateXmlWriter(this, textWriter);
							this.RenderNote(writer, (string)cmd.Tag);
							return ControlHelper.CloseXmlWriter(writer, textWriter);
						}
						else if (!(name == "FetchRow"))
						{
							return text;
						}
					}
					else
					{
						if (!(name == "Navigate"))
						{
							return text;
						}
						PXGridColumn pxgridColumn = (PXGridColumn)cmd.Tag;
						PXDataSource pxdataSource = this.GetDataSource() as PXDataSource;
						if (pxdataSource == null || cmd.Tag == null || string.IsNullOrEmpty(pxgridColumn.ViewName))
						{
							return text;
						}
						this.OnBeforeNavigate(new PXGridNavigateEventArgs(pxgridColumn));
						PXDataSourceView pxdataSourceView = ((IDataSource)pxdataSource).GetView(pxgridColumn.ViewName) as PXDataSourceView;
						if (pxdataSourceView == null)
						{
							return text;
						}
						IOrderedDictionary values = pxgridColumn.NavigateParams.GetValues(this.Context, this);
						if (values != null && values.Count > 0)
						{
							text = pxdataSource.GetEditUrl(pxgridColumn.ViewName, values);
						}
						else
						{
							object obj = this.DataValues[pxgridColumn.DataField];
							if (obj == null)
							{
								obj = this.DataKey[pxgridColumn.DataField];
							}
							string[] keyNames = pxdataSourceView.GetKeyNames();
							string key = keyNames[keyNames.Length - 1];
							text = pxdataSource.GetEditUrl(pxgridColumn.ViewName, key, obj);
						}
						if (!string.IsNullOrEmpty(text))
						{
							IOrderedDictionary values2 = pxgridColumn.AdditionalParams.GetValues(this.Context, this);
							if (values2.Count > 0)
							{
								PXBaseDataSource.RedirectHelper.WritePredefindedValues(this.Page, text, values2);
							}
							return base.ResolveUrl(text);
						}
						return text;
					}
					break;
				}
				case 9:
					if (!(name == "FilesMenu"))
					{
						return text;
					}
					return this.RenderFilesDialog();
				case 10:
				{
					char c = name[0];
					if (c != 'F')
					{
						if (c != 'L')
						{
							return text;
						}
						if (!(name == "LayoutSave"))
						{
							return text;
						}
						goto IL_477;
					}
					else
					{
						if (name == "FilterShow")
						{
							StringWriter textWriter = new StringWriter();
							XmlWriter writer = ControlHelper.CreateXmlWriter(this, textWriter);
							this.RenderFilterBox(writer);
							return ControlHelper.CloseXmlWriter(writer, textWriter);
						}
						if (!(name == "FilterSave"))
						{
							return text;
						}
						goto IL_346;
					}
					break;
				}
				case 11:
				{
					char c = name[0];
					if (c != 'E')
					{
						if (c != 'L')
						{
							return text;
						}
						if (!(name == "LayoutReset"))
						{
							return text;
						}
						goto IL_477;
					}
					else
					{
						if (!(name == "ExportExcel"))
						{
							return text;
						}
						return this.CurrentExportProcess.With((PXGrid.ExportProcessInfo _) => _.ToString());
					}
					break;
				}
				case 12:
				{
					char c = name[6];
					if (c != 'D')
					{
						if (c != 'R')
						{
							return text;
						}
						if (!(name == "FilterRemove"))
						{
							return text;
						}
						goto IL_346;
					}
					else
					{
						if (!(name == "FilterDialog"))
						{
							return text;
						}
						return this.RenderFilterDialog();
					}
					break;
				}
				case 13:
					if (!(name == "ColumnsDialog"))
					{
						return text;
					}
					return this.RenderColumnsDialog();
				case 18:
					if (!(name == "CreateProviderFile"))
					{
						return text;
					}
					if (this.Page.Items["CommandCreateProviderFile"] == null)
					{
						this.Page.Items["CommandCreateProviderFile"] = true;
						try
						{
							this.PerformSelectRow();
							object data = (this.rowDataItem is PXResult) ? ((PXResult)this.rowDataItem)[0] : this.rowDataItem;
							PXBlobStorage.FileAttachmentProvider.AddFile(PXNoteAttribute.GetNoteIDReadonly(this.DataGraph.Views[this.DataMember].Cache, data, null) ?? Guid.Empty);
							return text;
						}
						catch (PXBaseRedirectException data2)
						{
							PXBaseDataSource.RedirectHelper redirectHelper = new PXBaseDataSource.RedirectHelper(this.Page, this.DataGraph, this.DataMember, PXPageLoadBehavior.PopulateSavedValues, this.DataSourceID);
							this.DataGraph.Caches[typeof(NoteDoc)].ClearQueryCache();
							redirectHelper.TryRedirect(data2);
							throw;
						}
						goto IL_477;
					}
					return text;
				default:
					return text;
				}
				if (this.NeedRepaintRows && !this.updateError)
				{
					return this.RenderRepaintRowsXml();
				}
				return this.updateResult;
				IL_346:
				if (this.NeedRepaintRows)
				{
					return ((IPXDataControl)this).GetClientData();
				}
				return text;
				IL_477:
				if (this.NeedRepaintRows)
				{
					this.RepaintColumns = true;
					text = ((IPXDataControl)this).GetClientData();
				}
			}
			return text;
		}

		// Token: 0x0600205C RID: 8284 RVA: 0x000815C4 File Offset: 0x0007F7C4
		private string GetFilterRowsSessionKey()
		{
			return string.Format("{0}_{1}_filterRows", ControlHelper.GetScreenID(), this.ClientID);
		}

		// Token: 0x17000B46 RID: 2886
		// (get) Token: 0x0600205D RID: 8285 RVA: 0x000815DC File Offset: 0x0007F7DC
		// (set) Token: 0x0600205E RID: 8286 RVA: 0x00081630 File Offset: 0x0007F830
		private List<PXFilterRow> FilterRowsInSession
		{
			get
			{
				List<PXFilterRow> list = PXContext.SessionTyped<PXSessionStateWebUI>().GridFilterRows[this.GetFilterRowsSessionKey()];
				if (list != null)
				{
					return list.Select(delegate(PXFilterRow f)
					{
						if (f != null)
						{
							return (PXFilterRow)f.Clone();
						}
						return null;
					}).ToList<PXFilterRow>();
				}
				return null;
			}
			set
			{
				List<PXFilterRow> list;
				if (value != null)
				{
					list = value.Select(delegate(PXFilterRow f)
					{
						if (f != null)
						{
							return (PXFilterRow)f.Clone();
						}
						return null;
					}).ToList<PXFilterRow>();
				}
				else
				{
					list = null;
				}
				List<PXFilterRow> value2 = list;
				PXContext.SessionTyped<PXSessionStateWebUI>().GridFilterRows[this.GetFilterRowsSessionKey()] = value2;
			}
		}

		// Token: 0x0600205F RID: 8287 RVA: 0x00081684 File Offset: 0x0007F884
		private string GetStoredFilterIDSessionKey()
		{
			string arg = base.DesignMode ? "00000000" : ControlHelper.GetScreenID();
			return string.Format("{0}_{1}_activeFilterID", arg, this.ClientID);
		}

		// Token: 0x17000B47 RID: 2887
		// (get) Token: 0x06002060 RID: 8288 RVA: 0x000816B7 File Offset: 0x0007F8B7
		// (set) Token: 0x06002061 RID: 8289 RVA: 0x000816D0 File Offset: 0x0007F8D0
		private Guid? StoredFilterID
		{
			get
			{
				return PXContext.SessionTyped<PXSessionStateWebUI>().GridActiveFilterID[this.GetStoredFilterIDSessionKey()];
			}
			set
			{
				if (this.StoredFilterID != value)
				{
					this.StoredSqlTimeout = false;
				}
				PXContext.SessionTyped<PXSessionStateWebUI>().GridActiveFilterID[this.GetStoredFilterIDSessionKey()] = value;
			}
		}

		// Token: 0x06002062 RID: 8290 RVA: 0x00081737 File Offset: 0x0007F937
		private string GetStoredSortsSessionKey()
		{
			return string.Format("{0}_{1}_activeSorts", ControlHelper.GetScreenID(), this.ClientID);
		}

		// Token: 0x06002063 RID: 8291 RVA: 0x0008174E File Offset: 0x0007F94E
		private string GetStoredRowNumberKey()
		{
			return string.Format("{0}_{1}_rowNumber", ControlHelper.GetScreenID(), this.ClientID);
		}

		// Token: 0x17000B48 RID: 2888
		// (get) Token: 0x06002064 RID: 8292 RVA: 0x00081765 File Offset: 0x0007F965
		// (set) Token: 0x06002065 RID: 8293 RVA: 0x0008177C File Offset: 0x0007F97C
		private Dictionary<string, SortDirection> StoredSorts
		{
			get
			{
				return PXContext.SessionTyped<PXSessionStateWebUI>().GridActiveSorts[this.GetStoredSortsSessionKey()];
			}
			set
			{
				PXContext.SessionTyped<PXSessionStateWebUI>().GridActiveSorts[this.GetStoredSortsSessionKey()] = value;
			}
		}

		// Token: 0x17000B49 RID: 2889
		// (get) Token: 0x06002066 RID: 8294 RVA: 0x00081794 File Offset: 0x0007F994
		// (set) Token: 0x06002067 RID: 8295 RVA: 0x000817AB File Offset: 0x0007F9AB
		private int StoredRowNumber
		{
			get
			{
				return PXContext.SessionTyped<PXSessionStateWebUI>().GridRowNumber[this.GetStoredRowNumberKey()];
			}
			set
			{
				PXContext.SessionTyped<PXSessionStateWebUI>().GridRowNumber[this.GetStoredRowNumberKey()] = value;
			}
		}

		// Token: 0x06002068 RID: 8296 RVA: 0x000817C3 File Offset: 0x0007F9C3
		private string GetStoredFastFilterSessionKey()
		{
			if (!base.DesignMode)
			{
				return string.Format("{0}_{1}_fastFilter", ControlHelper.GetScreenID(), this.ClientID);
			}
			return "fastFilter";
		}

		// Token: 0x17000B4A RID: 2890
		// (get) Token: 0x06002069 RID: 8297 RVA: 0x000817E8 File Offset: 0x0007F9E8
		// (set) Token: 0x0600206A RID: 8298 RVA: 0x000817FF File Offset: 0x0007F9FF
		private string StoredFastFilter
		{
			get
			{
				return (string)PXContext.Session[this.GetStoredFastFilterSessionKey()];
			}
			set
			{
				PXContext.Session.SetString(this.GetStoredFastFilterSessionKey(), value);
			}
		}

		// Token: 0x0600206B RID: 8299 RVA: 0x00081812 File Offset: 0x0007FA12
		private string GetStoredSqlTimeoutSessionKey()
		{
			if (!base.DesignMode)
			{
				return string.Format("{0}_{1}_sqlTimeout", ControlHelper.GetScreenID(), this.ClientID);
			}
			return "sqlTimeout";
		}

		// Token: 0x17000B4B RID: 2891
		// (get) Token: 0x0600206C RID: 8300 RVA: 0x00081837 File Offset: 0x0007FA37
		// (set) Token: 0x0600206D RID: 8301 RVA: 0x0008184E File Offset: 0x0007FA4E
		private bool StoredSqlTimeout
		{
			get
			{
				return PXContext.SessionTyped<PXSessionStateWebUI>().GridSqlTimeout[this.GetStoredSqlTimeoutSessionKey()];
			}
			set
			{
				PXContext.SessionTyped<PXSessionStateWebUI>().GridSqlTimeout[this.GetStoredSqlTimeoutSessionKey()] = value;
			}
		}

		// Token: 0x17000B4C RID: 2892
		// (get) Token: 0x0600206E RID: 8302 RVA: 0x00081868 File Offset: 0x0007FA68
		private bool HasDrillDownFilter
		{
			get
			{
				List<PXFilterRow> filterRowsInSession = this.FilterRowsInSession;
				if (filterRowsInSession != null)
				{
					return filterRowsInSession.Any((PXFilterRow f) => PXGrid.GetFilterType(f) == PXGrid.FilterRowType.DrillDown);
				}
				return false;
			}
		}

		// Token: 0x0600206F RID: 8303 RVA: 0x000818A8 File Offset: 0x0007FAA8
		string IPXDataControl.GetClientData()
		{
			if (this.updateError)
			{
				return this.updateResult;
			}
			((IPXDataBoundControl)this).EnsureDataBoundInternal();
			StringWriter textWriter = new StringWriter();
			XmlWriter xmlWriter = ControlHelper.CreateXmlWriter(this, textWriter);
			PXGridLevel primaryLevel = this.PrimaryLevel;
			bool flag = ControlHelper.IsReloadPage(this);
			string callbackData = JSManager.GetCallbackData(this);
			if (!string.IsNullOrEmpty(callbackData))
			{
				xmlWriter.WriteAttributeString("Props", callbackData);
			}
			if (flag || this.reloadFilters)
			{
				if (this.filterNames != null)
				{
					List<string> list = new List<string>();
					foreach (KeyValuePair<Guid, string> keyValuePair in this.filterNames)
					{
						list.Add(keyValuePair.Key.ToString() + "|" + keyValuePair.Value);
					}
					if (this.HasDrillDownFilter)
					{
						list.Add(PXGrid._DD_FILTER_ID.ToString() + "|" + Msg.GetLocal("Drilldown"));
						if (this.FilterID == null)
						{
							this.FilterID = new Guid?(PXGrid._DD_FILTER_ID);
						}
					}
					xmlWriter.WriteAttributeString("Filters", string.Join(";", list.ToArray()));
				}
				else
				{
					xmlWriter.WriteAttributeString("Filters", string.Empty);
				}
			}
			if (flag)
			{
				PXGraph dataGraph = this.DataGraph;
				if (dataGraph != null)
				{
					List<string> list2 = new List<string>();
					foreach (KeyValuePair<Guid, string> keyValuePair2 in DynamicFilterManager.GetFilterInfo(dataGraph.GetType().FullName, this.DataMember))
					{
						list2.Add(keyValuePair2.Key.ToString() + "|" + keyValuePair2.Value);
					}
					if (list2.Count > 0)
					{
						xmlWriter.WriteAttributeString("DynamicFilters", string.Join(";", list2.ToArray()));
					}
				}
				xmlWriter.WriteAttributeString("Reload", "1");
			}
			if (this.IsPivotMode)
			{
				this.needUpdatePivot = !this.pivotEditorVisible;
			}
			else
			{
				this.RenderRowsXml(xmlWriter, this.Rows, !flag);
			}
			if ((this.RepaintColumns && !this.needUpdatePivot) || flag)
			{
				this.RegisterComplexStyles(false);
				PXTable pxtable = this.RenderColHeaderTable(primaryLevel);
				PXTable pxtable2 = null;
				this.Controls.Add(pxtable);
				if (primaryLevel.LayoutFinal.FooterVisible.GetValueOrDefault())
				{
					pxtable2 = this.RenderColFooterTable(primaryLevel);
					this.Controls.Add(pxtable2);
				}
				this.SetColMarginStyles(primaryLevel);
				if (pxtable2 != null)
				{
					xmlWriter.WriteStartElement("FooterHtml");
					xmlWriter.WriteComment(ControlHelper.RenderControlHtml(pxtable2, false));
					xmlWriter.WriteEndElement();
					this.Controls.Remove(pxtable2);
				}
				xmlWriter.WriteStartElement("HeaderHtml");
				xmlWriter.WriteComment(ControlHelper.RenderControlHtml(pxtable, false));
				xmlWriter.WriteEndElement();
				this.Controls.Remove(pxtable);
				xmlWriter.WriteStartElement("Columns");
				xmlWriter.WriteAttributeString("Props", JSManager.GetObjectData(this, this.Columns));
				foreach (object obj in primaryLevel.Columns)
				{
					PXGridColumn col = (PXGridColumn)obj;
					this.RenderColumnXml(xmlWriter, col);
				}
				xmlWriter.WriteEndElement();
				if (primaryLevel.NeedRowTemplate)
				{
					xmlWriter.WriteStartElement("RowTemplate");
					this.RenderRowTemplateXml(xmlWriter, primaryLevel);
					xmlWriter.WriteEndElement();
				}
			}
			if (this.AllowPaging)
			{
				this.RenderPagerHtml(xmlWriter);
			}
			return ControlHelper.CloseXmlWriter(xmlWriter, textWriter);
		}

		// Token: 0x06002070 RID: 8304 RVA: 0x00081C8C File Offset: 0x0007FE8C
		void IPXDataControl.CommitDataChanges(string clientData)
		{
			if (!string.IsNullOrEmpty(clientData))
			{
				XmlDocument xmlDocument = new XmlDocument
				{
					XmlResolver = null
				};
				xmlDocument.LoadXml(clientData);
				this.updateResult = this.CommitClientChanges(xmlDocument.DocumentElement);
				this.OnCommitChanges(EventArgs.Empty);
			}
		}

		// Token: 0x17000B4D RID: 2893
		// (get) Token: 0x06002071 RID: 8305 RVA: 0x00081CD2 File Offset: 0x0007FED2
		// (set) Token: 0x06002072 RID: 8306 RVA: 0x00081CDA File Offset: 0x0007FEDA
		bool IPXDataControl.PendingUpdate
		{
			get
			{
				return this.pendingUpdate;
			}
			set
			{
				this.pendingUpdate = value;
			}
		}

		// Token: 0x17000B4E RID: 2894
		// (get) Token: 0x06002073 RID: 8307 RVA: 0x00081CE3 File Offset: 0x0007FEE3
		KeyValuePair<ErrorState, string> IPXDataControl.ErrorState
		{
			get
			{
				return this.errorState;
			}
		}

		// Token: 0x06002074 RID: 8308 RVA: 0x00081CEC File Offset: 0x0007FEEC
		IEnumerable<AttachedFile> IPXDataControl.GetAttachedFiles()
		{
			bool flag;
			return this.CreateFilesDialog().GetAttachedFiles(out flag);
		}

		// Token: 0x06002075 RID: 8309 RVA: 0x00081D08 File Offset: 0x0007FF08
		void IPXDataBoundControl.EnsureDataBoundInternal()
		{
			if (!this.isDataBound && this.Page.IsCallback)
			{
				base.RequiresDataBinding = true;
			}
			if (base.RequiresDataBinding)
			{
				if (this.GenerateColumnsBeforeRepaint && this.RepaintColumns)
				{
					this.IsColumnsGenerated = false;
					this.InitColumnsLayout();
				}
				this.EnsureDataBound();
			}
		}

		// Token: 0x06002076 RID: 8310 RVA: 0x00081D5C File Offset: 0x0007FF5C
		private void RenderPagerHtml(XmlWriter writer)
		{
			if (this.AllowPaging && this.ActionBar.PagerVisible != ActionVisible.False)
			{
				PXActionBar pxactionBar = new PXActionBar(this, ActionVisible.Top);
				pxactionBar.ID = ((this.ActionBar.PagerVisible == ActionVisible.Bottom) ? "ab" : "at");
				pxactionBar.Settings = this.ActionBar;
				this.Controls.Add(pxactionBar);
				WebControl control = pxactionBar.RengerPager(!this.ActionBar.ActionsVisible);
				((ICSSProvider)pxactionBar).RegisterCss();
				pxactionBar.SetPagerStyles();
				writer.WriteStartElement("Pager");
				writer.WriteComment(ControlHelper.RenderControlHtml(control, true));
				writer.WriteEndElement();
				this.Controls.Remove(pxactionBar);
			}
		}

		// Token: 0x06002077 RID: 8311 RVA: 0x00081E10 File Offset: 0x00080010
		private string RenderRepaintRowsXml()
		{
			if (!this.isDataBound || base.RequiresDataBinding)
			{
				this.DataBind();
			}
			StringWriter textWriter = new StringWriter();
			XmlWriter xmlWriter = ControlHelper.CreateXmlWriter(this, textWriter);
			xmlWriter.WriteAttributeString("Repaint", "1");
			JSObject jsobject = new JSObject(this);
			jsobject.Append("PageIndex");
			jsobject.Append("PageSize");
			jsobject.Append("TotalRowCount");
			jsobject.Append("IsFirstPage");
			jsobject.Append("IsLastPage");
			xmlWriter.WriteAttributeString("Props", JSScriptSerializer.SerializeObject(jsobject));
			this.RenderRowsXml(xmlWriter, this.Rows, true);
			if (this.AllowPaging)
			{
				this.RenderPagerHtml(xmlWriter);
			}
			return ControlHelper.CloseXmlWriter(xmlWriter, textWriter);
		}

		// Token: 0x06002078 RID: 8312 RVA: 0x00081EC4 File Offset: 0x000800C4
		private string InitRowData(OrderedDictionary values)
		{
			StringWriter stringWriter = new StringWriter();
			XmlWriter xmlWriter = ControlHelper.CreateXmlWriter(null, stringWriter);
			PXGridLevel primaryLevel = this.PrimaryLevel;
			int inserted = 0;
			bool flag = this.GetDataSource() is PXDataSource;
			PXDBInsertedEventHandler value = delegate(object sender, PXDBInsertedEventArgs e)
			{
				inserted = e.AffectedRows;
			};
			if (this.insertPos != null)
			{
				PXGraph dataGraph = this.DataGraph;
				dataGraph.Caches[dataGraph.GetItemType(this.DataMember)].InsertPosition = this.insertPos;
			}
			try
			{
				if (flag)
				{
					this.RowInserted += value;
					foreach (object key in values.Keys.ToArray<object>())
					{
						values[key] = null;
					}
					this.PerformInsert(values);
				}
				else
				{
					PXDBInsertEventArgs pxdbinsertEventArgs = new PXDBInsertEventArgs(values);
					this.OnInitRow(pxdbinsertEventArgs);
					inserted = ((!pxdbinsertEventArgs.Cancel) ? 1 : 0);
				}
			}
			catch (Exception ex)
			{
				if (ex is PXDialogRequiredException || ex is PXPopupRedirectException)
				{
					throw ex;
				}
				this.CreateRowXmlNode(xmlWriter, null, ex);
				inserted = -1;
			}
			finally
			{
				if (flag)
				{
					this.RowInserted -= value;
				}
			}
			if (inserted >= 0)
			{
				this.CreateRowXmlNode(xmlWriter, null, inserted, primaryLevel, values, true);
				if (flag)
				{
					foreach (string key2 in this.PrimaryLevel.DataKeyNames)
					{
						object obj = values[key2];
						if (obj is PXFieldState)
						{
							((PXFieldState)obj).Value = null;
						}
						else
						{
							values[key2] = null;
						}
					}
				}
			}
			xmlWriter.Close();
			return stringWriter.ToString();
		}

		// Token: 0x06002079 RID: 8313 RVA: 0x0008207C File Offset: 0x0008027C
		private string InitRowData()
		{
			OrderedDictionary values = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
			this.CreateValuesFromColumns(this.PrimaryLevel, null, values);
			return this.InitRowData(values);
		}

		// Token: 0x0600207A RID: 8314 RVA: 0x000820AC File Offset: 0x000802AC
		private string InitRowData(XmlElement rowNode)
		{
			OrderedDictionary values = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
			this.ExtractRowData(rowNode, this.PrimaryLevel, null, values, null);
			return this.InitRowData(values);
		}

		// Token: 0x0600207B RID: 8315 RVA: 0x000820DC File Offset: 0x000802DC
		private string RefetchRowData(OrderedDictionary keys, OrderedDictionary values)
		{
			StringWriter stringWriter = new StringWriter();
			XmlWriter xmlWriter = ControlHelper.CreateXmlWriter(null, stringWriter);
			PXGridLevel primaryLevel = this.PrimaryLevel;
			int updated = 0;
			bool flag = this.GetDataSource() is PXDataSource;
			PXDBUpdatedEventHandler value = delegate(object sender, PXDBUpdatedEventArgs e)
			{
				updated = e.AffectedRows;
			};
			try
			{
				if (flag)
				{
					this.RowUpdated += value;
					this.PerformUpdate(keys, values, null);
				}
				else
				{
					PXDBUpdateEventArgs pxdbupdateEventArgs = new PXDBUpdateEventArgs(keys, null, values);
					this.OnRefetchRow(pxdbupdateEventArgs);
					updated = ((!pxdbupdateEventArgs.Cancel) ? 1 : 0);
				}
			}
			catch (Exception ex)
			{
				if (ex is PXDialogRequiredException || ex is PXPopupRedirectException)
				{
					throw ex;
				}
				this.CreateRowXmlNode(xmlWriter, null, ex);
				updated = -1;
			}
			finally
			{
				if (flag)
				{
					this.RowUpdated -= value;
				}
			}
			if (updated >= 0)
			{
				this.CreateRowXmlNode(xmlWriter, null, updated, primaryLevel, values, true);
			}
			xmlWriter.Close();
			return stringWriter.ToString();
		}

		// Token: 0x0600207C RID: 8316 RVA: 0x000821E0 File Offset: 0x000803E0
		private string RefetchRowData(XmlElement rowNode)
		{
			OrderedDictionary keys = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
			OrderedDictionary values = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
			this.ExtractRowData(rowNode, this.PrimaryLevel, keys, values, null);
			return this.RefetchRowData(keys, values);
		}

		// Token: 0x0600207D RID: 8317 RVA: 0x0008221C File Offset: 0x0008041C
		private string CommitClientChanges(XmlElement changes)
		{
			StringWriter stringWriter = new StringWriter();
			XmlWriter xmlWriter = ControlHelper.CreateXmlWriter(null, stringWriter);
			PXGridLevel primaryLevel = this.PrimaryLevel;
			xmlWriter.WriteStartElement("UpdateResult");
			xmlWriter.WriteStartElement("Rows");
			this.suppressBinding = false;
			this.CommitDeletedRows(primaryLevel, changes, xmlWriter);
			if (this.CommitModifiedRows(primaryLevel, changes, xmlWriter) > 0 && this.BatchUpdate)
			{
				this.SyncCurrentPosition();
			}
			this.CommitInsertedRows(primaryLevel, changes, xmlWriter);
			xmlWriter.WriteEndElement();
			xmlWriter.WriteEndElement();
			xmlWriter.Close();
			return stringWriter.ToString();
		}

		// Token: 0x0600207E RID: 8318 RVA: 0x000822A4 File Offset: 0x000804A4
		private int CommitDeletedRows(PXGridLevel level, XmlElement changes, XmlWriter writer)
		{
			OrderedDictionary keys = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
			OrderedDictionary oldValues = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
			int affectedRows = 0;
			int affected = 0;
			PXDBDeletedEventHandler @object = delegate(object sender, PXDBDeletedEventArgs e)
			{
				affected = e.AffectedRows;
				if (e.AffectedRows > 0)
				{
					affectedRows += e.AffectedRows;
				}
			};
			try
			{
				this.RowDeleted += @object.Invoke;
				XmlNode xmlNode = changes.SelectSingleNode("./Deleted");
				if (xmlNode != null)
				{
					foreach (object obj in xmlNode.ChildNodes)
					{
						XmlElement xmlElement = (XmlElement)obj;
						this.ExtractRowData(xmlElement, level, keys, null, oldValues);
						try
						{
							this.PerformDelete(keys, oldValues);
							this.CreateRowXmlNode(writer, xmlElement, affected, null, null, false, new int?(4));
						}
						catch (PXDialogRequiredException)
						{
							throw;
						}
						catch (Exception ex)
						{
							this.CreateRowXmlNode(writer, xmlElement, ex);
						}
					}
					this.SyncCurrentPosition();
				}
			}
			finally
			{
				this.RowDeleted -= @object.Invoke;
			}
			return affectedRows;
		}

		// Token: 0x0600207F RID: 8319 RVA: 0x000823E4 File Offset: 0x000805E4
		private int CommitModifiedRows(PXGridLevel level, XmlElement changes, XmlWriter writer)
		{
			OrderedDictionary keys = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
			OrderedDictionary values = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
			OrderedDictionary oldValues = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
			int affectedRows = 0;
			int affected = 0;
			PXDBUpdatedEventHandler @object = delegate(object sender, PXDBUpdatedEventArgs e)
			{
				affected = e.AffectedRows;
				if (e.AffectedRows > 0)
				{
					affectedRows += e.AffectedRows;
				}
			};
			try
			{
				this.RowUpdated += @object.Invoke;
				XmlNode xmlNode = changes.SelectSingleNode("./Modified");
				if (xmlNode != null)
				{
					foreach (object obj in xmlNode.ChildNodes)
					{
						XmlElement xmlElement = (XmlElement)obj;
						this.ExtractRowData(xmlElement, level, keys, values, oldValues);
						try
						{
							this.PerformUpdate(keys, values, oldValues);
							this.CreateRowXmlNode(writer, xmlElement, affected, level, values, true, new int?(1));
						}
						catch (PXDialogRequiredException)
						{
							throw;
						}
						catch (Exception ex)
						{
							this.CreateRowXmlNode(writer, xmlElement, ex);
						}
					}
				}
			}
			finally
			{
				this.RowUpdated -= @object.Invoke;
			}
			return affectedRows;
		}

		// Token: 0x06002080 RID: 8320 RVA: 0x00082528 File Offset: 0x00080728
		private int CommitInsertedRows(PXGridLevel level, XmlElement changes, XmlWriter writer)
		{
			OrderedDictionary keys = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
			OrderedDictionary values = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
			OrderedDictionary oldValues = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
			int affectedRows = 0;
			int affected = 0;
			PXDBInsertedEventHandler value = delegate(object sender, PXDBInsertedEventArgs e)
			{
				affected = e.AffectedRows;
				if (e.AffectedRows > 0)
				{
					affectedRows += e.AffectedRows;
				}
			};
			PXDBUpdatedEventHandler value2 = delegate(object sender, PXDBUpdatedEventArgs e)
			{
				affected = e.AffectedRows;
				if (e.AffectedRows > 0)
				{
					affectedRows += e.AffectedRows;
				}
			};
			bool valueOrDefault = level.ModeFinal.AllowAddNew.GetValueOrDefault();
			try
			{
				if (valueOrDefault)
				{
					this.RowUpdated += value2;
				}
				else
				{
					this.RowInserted += value;
				}
				XmlNode xmlNode = changes.SelectSingleNode("./Inserted");
				if (xmlNode != null)
				{
					foreach (object obj in xmlNode.ChildNodes)
					{
						XmlElement xmlElement = (XmlElement)obj;
						this.ExtractRowData(xmlElement, level, keys, values, oldValues);
						try
						{
							if (valueOrDefault)
							{
								this.PerformUpdate(keys, values, oldValues);
							}
							else
							{
								this.PerformInsert(values);
							}
							this.CreateRowXmlNode(writer, xmlElement, affected, level, values, false, new int?(3));
						}
						catch (PXDialogRequiredException)
						{
							throw;
						}
						catch (Exception ex)
						{
							this.CreateRowXmlNode(writer, xmlElement, ex);
						}
					}
				}
			}
			finally
			{
				if (valueOrDefault)
				{
					this.RowUpdated -= value2;
				}
				else
				{
					this.RowInserted -= value;
				}
			}
			return affectedRows;
		}

		// Token: 0x06002081 RID: 8321 RVA: 0x000826A8 File Offset: 0x000808A8
		internal void ExtractRowData(XmlElement rowNode, PXGridLevel level, IOrderedDictionary keys, IOrderedDictionary values, IOrderedDictionary oldValues)
		{
			HashSet<string> hashSet = new HashSet<string>(level.DataKeyNames, StringComparer.OrdinalIgnoreCase);
			if (keys != null)
			{
				keys.Clear();
			}
			if (values != null)
			{
				values.Clear();
			}
			if (oldValues != null)
			{
				oldValues.Clear();
			}
			XmlNode xmlNode = rowNode.SelectSingleNode("./Cells");
			if (xmlNode == null)
			{
				return;
			}
			PXGridColumnCollection columns = level.Columns;
			this.InitColumnsLayout();
			bool flag = false;
			int num = 0;
			while (num < xmlNode.ChildNodes.Count && num < columns.Count)
			{
				XmlElement xmlElement = xmlNode.ChildNodes[num] as XmlElement;
				string attribute = xmlElement.GetAttribute("Key");
				PXGridColumn pxgridColumn;
				if (!string.IsNullOrEmpty(attribute))
				{
					flag = true;
					pxgridColumn = level.Columns[attribute];
				}
				else
				{
					pxgridColumn = level.Columns[num];
				}
				string dataField = pxgridColumn.DataField;
				if (dataField.Length != 0)
				{
					string attribute2 = xmlElement.GetAttribute("Value");
					object valueFromText = pxgridColumn.GetValueFromText(attribute2);
					if (values != null)
					{
						values[dataField] = valueFromText;
						if (!string.IsNullOrEmpty(pxgridColumn.TextField))
						{
							values[pxgridColumn.TextFieldColumn] = null;
						}
					}
					if (xmlElement.HasAttribute("OldValue"))
					{
						valueFromText = pxgridColumn.GetValueFromText(xmlElement.GetAttribute("OldValue"));
					}
					if (oldValues != null)
					{
						oldValues[dataField] = valueFromText;
					}
					if (keys != null && hashSet.Contains(dataField))
					{
						keys[dataField] = valueFromText;
					}
				}
				num++;
			}
			if (flag && values != null)
			{
				this.CreateValuesFromColumns(level, keys, values);
			}
			if (values != null && !string.IsNullOrEmpty(rowNode.GetAttribute("IsNew")))
			{
				values[PXCache.IsNewRow] = PXCache.NotSetValue;
			}
		}

		// Token: 0x06002082 RID: 8322 RVA: 0x0008285C File Offset: 0x00080A5C
		private void CreateValuesFromColumns(PXGridLevel level, IOrderedDictionary keys, IOrderedDictionary values)
		{
			List<string> list = new List<string>(level.DataKeyNames);
			foreach (object obj in level.Columns)
			{
				PXGridColumn pxgridColumn = (PXGridColumn)obj;
				if (keys != null && list.Contains(pxgridColumn.DataField) && !keys.Contains(pxgridColumn.DataField))
				{
					keys[pxgridColumn.DataField] = this.DataValues[pxgridColumn.DataField];
				}
				if (values != null && !values.Contains(pxgridColumn.DataField))
				{
					values[pxgridColumn.DataField] = PXCache.NotSetValue;
					if (!string.IsNullOrEmpty(pxgridColumn.TextField))
					{
						values[pxgridColumn.TextFieldColumn] = PXCache.NotSetValue;
					}
				}
			}
		}

		// Token: 0x06002083 RID: 8323 RVA: 0x0008293C File Offset: 0x00080B3C
		private void CreateRowXmlNode(XmlWriter writer, XmlNode srcRow, int affected, PXGridLevel level, IOrderedDictionary values, bool extended)
		{
			this.CreateRowXmlNode(writer, srcRow, affected, level, values, extended, null);
		}

		// Token: 0x06002084 RID: 8324 RVA: 0x00082964 File Offset: 0x00080B64
		private void CreateRowXmlNode(XmlWriter writer, XmlNode srcRow, int affected, PXGridLevel level, IOrderedDictionary values, bool extended, int? status)
		{
			writer.WriteStartElement("Row");
			if (srcRow != null)
			{
				string value = srcRow.Attributes["i"].Value;
				writer.WriteAttributeString("i", value);
			}
			if (status != null)
			{
				writer.WriteAttributeString("Status", status.Value.ToString());
			}
			writer.WriteAttributeString("Affected", ((affected > 0) ? affected : 0).ToString());
			if (values == null)
			{
				writer.WriteEndElement();
				return;
			}
			string text = null;
			string text2 = null;
			string text3 = null;
			foreach (object obj in values)
			{
				PXFieldState pxfieldState = ((DictionaryEntry)obj).Value as PXFieldState;
				if (pxfieldState != null)
				{
					switch (pxfieldState.ErrorLevel)
					{
					case PXErrorLevel.RowInfo:
						text = pxfieldState.Error;
						break;
					case PXErrorLevel.RowWarning:
						text2 = pxfieldState.Error;
						break;
					case PXErrorLevel.Error:
						if (affected == 0 && string.IsNullOrEmpty(text3))
						{
							text3 = pxfieldState.Error;
						}
						if (affected == 0)
						{
							this.updateError = true;
						}
						break;
					case PXErrorLevel.RowError:
						text3 = pxfieldState.Error;
						break;
					}
				}
			}
			if (!string.IsNullOrEmpty(text3))
			{
				writer.WriteAttributeString("Error", PXGrid.EncodeLineBreak(text3));
			}
			if (!string.IsNullOrEmpty(text2))
			{
				writer.WriteAttributeString("Warning", PXGrid.EncodeLineBreak(text2));
			}
			if (!string.IsNullOrEmpty(text))
			{
				writer.WriteAttributeString("Info", PXGrid.EncodeLineBreak(text));
			}
			foreach (object obj2 in values)
			{
				DictionaryEntry dictionaryEntry = (DictionaryEntry)obj2;
				PXGridColumn pxgridColumn = level.Columns[dictionaryEntry.Key.ToString()];
				if (pxgridColumn != null && values.Contains(dictionaryEntry.Key) && dictionaryEntry.Value != PXCache.NotSetValue)
				{
					writer.WriteStartElement("Cell");
					writer.WriteAttributeString("DataField", dictionaryEntry.Key.ToString());
					object value2 = pxgridColumn.NormalizeValue(dictionaryEntry.Value);
					string valueText = pxgridColumn.GetValueText(value2, true);
					writer.WriteAttributeString("Value", PXGrid.CleanUpXmlString(valueText));
					if (!string.IsNullOrEmpty(pxgridColumn.TextField))
					{
						object obj3 = values[pxgridColumn.TextFieldColumn];
						if (obj3 is PXFieldState)
						{
							obj3 = ((PXFieldState)obj3).Value;
						}
						writer.WriteAttributeString("Text", pxgridColumn.GetDisplayText(value2, obj3));
					}
					else if (status != null && status.Value != 4)
					{
						string text4 = pxgridColumn.FormatValue(value2);
						if (valueText != text4)
						{
							writer.WriteAttributeString("Text", text4);
						}
					}
					PXFieldState pxfieldState2 = dictionaryEntry.Value as PXFieldState;
					if (pxfieldState2 != null)
					{
						writer.WriteAttributeString("ReadOnly", pxfieldState2.IsReadOnly.ToString());
						if ((pxfieldState2.ErrorLevel == PXErrorLevel.Error || pxfieldState2.ErrorLevel == PXErrorLevel.Warning) && !string.IsNullOrEmpty(pxfieldState2.Error))
						{
							writer.WriteAttributeString(pxfieldState2.IsWarning ? "Warning" : "Error", PXGrid.EncodeLineBreak(pxfieldState2.Error));
						}
						if (extended && pxgridColumn.GetMatrixMode())
						{
							this.WriteCellState(writer, pxfieldState2);
						}
					}
					writer.WriteEndElement();
				}
			}
			writer.WriteEndElement();
		}

		// Token: 0x06002085 RID: 8325 RVA: 0x00082D1C File Offset: 0x00080F1C
		private void WriteCellState(XmlWriter writer, PXFieldState state)
		{
			TypeCode typeCode = Type.GetTypeCode(state.DataType);
			if (typeCode != TypeCode.Empty)
			{
				string localName = "DataType";
				int num = (int)typeCode;
				writer.WriteAttributeString(localName, num.ToString());
			}
			if (state.Precision >= 0)
			{
				writer.WriteAttributeString("Decimals", state.Precision.ToString());
			}
			if (!string.IsNullOrEmpty(state.ViewName))
			{
				writer.WriteAttributeString("ViewName", state.ViewName);
			}
			if (!string.IsNullOrEmpty(state.DescriptionName))
			{
				writer.WriteAttributeString("TextField", state.DescriptionName);
			}
			if (!string.IsNullOrEmpty(state.ValueField))
			{
				writer.WriteAttributeString("ValueField", state.ValueField);
			}
			PXStringState pxstringState = state as PXStringState;
			if (pxstringState != null)
			{
				if (!string.IsNullOrEmpty(pxstringState.InputMask))
				{
					writer.WriteAttributeString("DisplayFormat", pxstringState.InputMask);
				}
				if (pxstringState.AllowedValues != null && pxstringState.AllowedValues.Length != 0)
				{
					writer.WriteStartElement("ValueItems");
					if (!pxstringState.ExclusiveValues)
					{
						writer.WriteAttributeString("Exclusive", bool.FalseString);
					}
					if (pxstringState.MultiSelect)
					{
						writer.WriteAttributeString("MultiSelect", bool.TrueString);
					}
					for (int i = 0; i < pxstringState.AllowedValues.Length; i++)
					{
						writer.WriteStartElement("ValueItem");
						writer.WriteAttributeString("Value", pxstringState.AllowedValues[i]);
						if (!string.IsNullOrEmpty(pxstringState.AllowedLabels[i]))
						{
							writer.WriteAttributeString("DisplayValue", pxstringState.AllowedLabels[i]);
						}
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
					return;
				}
			}
			else
			{
				PXIntState pxintState = state as PXIntState;
				if (pxintState != null && pxintState.AllowedValues != null && pxintState.AllowedValues.Length != 0)
				{
					writer.WriteStartElement("ValueItems");
					if (!pxintState.ExclusiveValues)
					{
						writer.WriteAttributeString("Exclusive", bool.FalseString);
					}
					for (int j = 0; j < pxintState.AllowedValues.Length; j++)
					{
						writer.WriteStartElement("ValueItem");
						writer.WriteAttributeString("Value", pxintState.AllowedValues[j].ToString(CultureInfo.InvariantCulture));
						if (!string.IsNullOrEmpty(pxintState.AllowedLabels[j]))
						{
							writer.WriteAttributeString("DisplayValue", pxintState.AllowedLabels[j]);
						}
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
				}
			}
		}

		// Token: 0x06002086 RID: 8326 RVA: 0x00082F6C File Offset: 0x0008116C
		private void CreateRowXmlNode(XmlWriter writer, XmlNode srcRow, Exception ex)
		{
			writer.WriteStartElement("Row");
			if (srcRow != null)
			{
				string value = srcRow.Attributes["i"].Value;
				writer.WriteAttributeString("i", value);
			}
			writer.WriteAttributeString("Error", ex.Message);
			writer.WriteEndElement();
			this.updateError = true;
		}

		// Token: 0x06002087 RID: 8327 RVA: 0x00082FC7 File Offset: 0x000811C7
		private string GetNote()
		{
			if (this.NoteField.Length > 0)
			{
				this.PerformSelectRow();
			}
			return this.GetNoteInt(this.rowDataItem);
		}

		// Token: 0x06002088 RID: 8328 RVA: 0x00082FEC File Offset: 0x000811EC
		private string GetNoteInt(object dataitem)
		{
			string note = string.Empty;
			if (this.NoteField.Length > 0 && dataitem != null)
			{
				PXDataSourceView pxdataSourceView = this.GetDataView() as PXDataSourceView;
				if (pxdataSourceView != null)
				{
					PXNoteState pxnoteState = pxdataSourceView.GetStateExt(dataitem, this.NoteField) as PXNoteState;
					if (pxnoteState != null)
					{
						note = (string)pxnoteState.Value;
					}
				}
				else if (dataitem is DataRowView)
				{
					object obj = ((DataRowView)dataitem)[this.NoteField];
					if (obj != null)
					{
						note = obj.ToString();
					}
				}
				else
				{
					PropertyDescriptor propertyDescriptor = TypeDescriptor.GetProperties(dataitem).Find(this.NoteField, false);
					if (propertyDescriptor != null)
					{
						note = propertyDescriptor.GetValue(dataitem).ToString();
					}
				}
			}
			PXNoteEventArgs pxnoteEventArgs = new PXNoteEventArgs(note);
			this.OnNoteShow(pxnoteEventArgs);
			return pxnoteEventArgs.Note;
		}

		// Token: 0x06002089 RID: 8329 RVA: 0x000830AC File Offset: 0x000812AC
		private void RenderNote(XmlWriter writer, string text)
		{
			if (!string.IsNullOrEmpty(text))
			{
				text = ControlHelper.GetRidOfInvalidCharacters(text);
			}
			if (this.inputBoxLoaded)
			{
				writer.WriteAttributeString("Note", text);
				return;
			}
			PXInputBox pxinputBox = new PXInputBox();
			pxinputBox.ID = this.ClientID + base.IdSeparator.ToString() + "ib";
			pxinputBox.Text = text;
			pxinputBox.Caption = Msg.GetLocal("Enter Record Note");
			pxinputBox.Width = Unit.Pixel(500);
			pxinputBox.Height = Unit.Pixel(200);
			this.Page.Controls.Add(pxinputBox);
			pxinputBox.ApplyStyleSheetSkin(this.Page);
			JSManager.SetControlListName("_inputBox", this.Page);
			JSManager.Register(pxinputBox);
			writer.WriteComment(ControlHelper.RenderControl(pxinputBox));
		}

		// Token: 0x0600208A RID: 8330 RVA: 0x00083180 File Offset: 0x00081380
		private void SaveNote(string text)
		{
			if (this.NoteField.Length > 0)
			{
				this.PerformSelectRow();
				if (this.rowDataItem != null)
				{
					PXDataSourceView pxdataSourceView = this.GetDataView() as PXDataSourceView;
					if (pxdataSourceView != null)
					{
						PXDataSource ds = this.GetDataSource() as PXDataSource;
						if (ds != null && !ds.GetCommandStates().Any((PXBaseDataSource.CommandState _) => _.Visible && _.Name == ds.SaveCommandName))
						{
							string key = string.IsNullOrEmpty(this.DataMember) ? this.DataGraph.PrimaryView : this.DataMember;
							PXNoteAttribute.ForcePassThrow(this.DataGraph.Views[key].Cache, null);
						}
						if (pxdataSourceView.GetStateExt(this.rowDataItem, this.NoteField) is PXNoteState)
						{
							pxdataSourceView.SetValueExt(this.rowDataItem, this.NoteField, text);
						}
					}
				}
			}
			PXNoteEventArgs e = new PXNoteEventArgs(text);
			this.OnNoteSave(e);
		}

		// Token: 0x0600208B RID: 8331 RVA: 0x00083278 File Offset: 0x00081478
		private PXFilesDialog CreateFilesDialog()
		{
			if (this._filesDialog == null)
			{
				PXFilesDialog pxfilesDialog = this._filesDialog = new PXFilesDialog();
				pxfilesDialog.ID = "fb";
				pxfilesDialog.Caption = Msg.GetLocal("Files");
				pxfilesDialog.DataSourceID = this.DataSourceID;
				pxfilesDialog.DataMember = this.DataMember;
				pxfilesDialog.ApplyStyleSheetSkin(this.Page);
				pxfilesDialog.SyncDataItem += this.FilesDialog_SyncDataItem;
				if (this.filesDialogToolbarItems != null)
				{
					pxfilesDialog.ToolbarItems = this.filesDialogToolbarItems;
				}
			}
			if (!this.Controls.Contains(this._filesDialog))
			{
				this.Controls.Add(this._filesDialog);
			}
			return this._filesDialog;
		}

		// Token: 0x0600208C RID: 8332 RVA: 0x0008332B File Offset: 0x0008152B
		private void FilesDialog_SyncDataItem(object sender, EventArgs e)
		{
			this.SyncCurrentPosition(this.ID);
		}

		// Token: 0x0600208D RID: 8333 RVA: 0x00083339 File Offset: 0x00081539
		private string RenderFilesDialog()
		{
			PXFilesDialog pxfilesDialog = this.CreateFilesDialog();
			pxfilesDialog.DataBind();
			((IPXDynamicControl)pxfilesDialog).SetRenderState(true);
			PXStyleManager.RegisterRecursive(pxfilesDialog);
			JSManager.SetControlListName("_filesBox", this.Page);
			JSManager.RegisterRecursive(pxfilesDialog);
			return ControlHelper.RenderControl(pxfilesDialog);
		}

		// Token: 0x0600208E RID: 8334 RVA: 0x00083370 File Offset: 0x00081570
		private PXFilterDialog CreateFilterDialog()
		{
			if (this._filterDialog == null)
			{
				PXFilterDialog pxfilterDialog = this._filterDialog = new PXFilterDialog();
				pxfilterDialog.LinkedGrid = this;
				pxfilterDialog.ID = "fd";
				pxfilterDialog.Position = PanelPosition.UnderOwner;
				pxfilterDialog.CaptionVisible = false;
				pxfilterDialog.ApplyStyleSheetSkin(this.Page);
			}
			if (!this.Controls.Contains(this._filterDialog))
			{
				this.Controls.Add(this._filterDialog);
			}
			return this._filterDialog;
		}

		// Token: 0x0600208F RID: 8335 RVA: 0x000833E8 File Offset: 0x000815E8
		private string RenderFilterDialog()
		{
			PXFilterDialog pxfilterDialog = this.CreateFilterDialog();
			((IPXDynamicControl)pxfilterDialog).SetRenderState(true);
			PXStyleManager.RegisterRecursive(pxfilterDialog);
			JSManager.SetControlListName("_filterBox", this.Page);
			JSManager.RegisterRecursive(pxfilterDialog);
			return ControlHelper.RenderControl(pxfilterDialog);
		}

		// Token: 0x06002090 RID: 8336 RVA: 0x00083418 File Offset: 0x00081618
		private PXColumnsDialog CreateColumnsDialog()
		{
			if (this._columnsDialog == null)
			{
				PXColumnsDialog pxcolumnsDialog = this._columnsDialog = new PXColumnsDialog();
				pxcolumnsDialog.LinkedGrid = this;
				pxcolumnsDialog.ID = "cd";
				pxcolumnsDialog.Caption = Msg.GetLocal("Column Configuration");
				pxcolumnsDialog.AllowResize = false;
				pxcolumnsDialog.ApplyStyleSheetSkin(this.Page);
			}
			if (!base.DesignMode)
			{
				this._columnsDialog.CanSave = this.IsScreenInSiteMap;
			}
			if (!this.Controls.Contains(this._columnsDialog))
			{
				this.Controls.Add(this._columnsDialog);
			}
			return this._columnsDialog;
		}

		// Token: 0x06002091 RID: 8337 RVA: 0x000834B2 File Offset: 0x000816B2
		private string RenderColumnsDialog()
		{
			PXColumnsDialog pxcolumnsDialog = this.CreateColumnsDialog();
			((IPXDynamicControl)pxcolumnsDialog).SetRenderState(true);
			PXStyleManager.RegisterRecursive(pxcolumnsDialog);
			JSManager.SetControlListName("_columnsBox", this.Page);
			JSManager.RegisterRecursive(pxcolumnsDialog);
			return ControlHelper.RenderControl(pxcolumnsDialog);
		}

		// Token: 0x06002092 RID: 8338 RVA: 0x000834E4 File Offset: 0x000816E4
		private static OrderedDictionary CloneDictionary(IOrderedDictionary source)
		{
			OrderedDictionary orderedDictionary = new OrderedDictionary(source.Count);
			foreach (object obj in source)
			{
				DictionaryEntry dictionaryEntry = (DictionaryEntry)obj;
				orderedDictionary.Add(dictionaryEntry.Key, dictionaryEntry.Value);
			}
			return orderedDictionary;
		}

		// Token: 0x06002093 RID: 8339 RVA: 0x00083554 File Offset: 0x00081754
		internal string GetFilterView()
		{
			string text = this.FilterView;
			if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(this.DataMember))
			{
				PXDataSource pxdataSource = this.GetDataSource() as PXDataSource;
				string text2 = this.DataMember + "$FilterHeader";
				if (pxdataSource != null && pxdataSource.DataGraph != null && pxdataSource.DataGraph.Views.ContainsKey(text2))
				{
					return text2;
				}
			}
			return text;
		}

		// Token: 0x06002094 RID: 8340 RVA: 0x000835BC File Offset: 0x000817BC
		private string GetFilterRowsView()
		{
			string filterRowsView = this.FilterRowsView;
			if (string.IsNullOrEmpty(filterRowsView))
			{
				PXDataSource pxdataSource = this.GetDataSource() as PXDataSource;
				string text = this.DataMember + "$FilterRow";
				if (pxdataSource != null && pxdataSource.DataGraph != null)
				{
					if (pxdataSource.DataGraph.Views.ContainsKey(text))
					{
						return text;
					}
					if (!pxdataSource.DataGraph.Views.ContainsKey("$GlobalFilterRow$"))
					{
						pxdataSource.DataGraph.Views.Add("$GlobalFilterRow$", new PXFilterDetailView(pxdataSource.DataGraph, "$GlobalFilterRow$", Array.Empty<Type>()));
					}
					return "$GlobalFilterRow$";
				}
			}
			return filterRowsView;
		}

		// Token: 0x06002095 RID: 8341 RVA: 0x00083660 File Offset: 0x00081860
		internal PXGridColumn GetSearchColumn()
		{
			PXGridLevel primaryLevel = this.PrimaryLevel;
			int num = primaryLevel.DataKeyNames.Length;
			string text = (num > 0) ? primaryLevel.DataKeyNames[num - 1] : null;
			PXGridColumn result = null;
			foreach (object obj in primaryLevel.Columns)
			{
				PXGridColumn pxgridColumn = (PXGridColumn)obj;
				if (pxgridColumn.SortDirection != SortDirection.None)
				{
					result = pxgridColumn;
					break;
				}
				if (text != null && pxgridColumn.DataField == text)
				{
					result = pxgridColumn;
				}
			}
			return result;
		}

		// Token: 0x06002096 RID: 8342 RVA: 0x00083704 File Offset: 0x00081904
		private void LoadCookiesData()
		{
			string cookieValue = ControlHelper.GetCookieValue(this);
			if (!string.IsNullOrEmpty(cookieValue))
			{
				string[] array = cookieValue.Split(new char[]
				{
					','
				});
				int num = -1;
				if (int.TryParse(array[0], out num))
				{
					this.PageIndex = num;
				}
			}
		}

		// Token: 0x06002097 RID: 8343 RVA: 0x00083748 File Offset: 0x00081948
		private void SyncInlineSelectorState()
		{
			PXSelector pxselector = (PXSelector)this.PrimaryLevel.GetStandardEditor(GridStandardEditor.Selector);
			PXSegmentMask pxsegmentMask = (PXSegmentMask)this.PrimaryLevel.GetStandardEditor(GridStandardEditor.SegmentMask);
			if (pxselector == null && pxsegmentMask == null)
			{
				return;
			}
			PXFieldState pxfieldState = this.GetCellEditorState() as PXFieldState;
			if (pxfieldState != null)
			{
				PXSegmentedState pxsegmentedState = pxfieldState as PXSegmentedState;
				PXSelectorBase pxselectorBase = pxselector;
				if (pxsegmentedState != null && pxsegmentedState.Segments != null && !pxsegmentedState.ValidCombos)
				{
					pxselectorBase = pxsegmentMask;
				}
				PXGridColumn pxgridColumn = this.Columns[this.editingField];
				if (pxselectorBase != null)
				{
					PXFormView formView = this.PrimaryLevel.FormView;
					if (formView != null)
					{
						formView.EnsureChildControlsInternal();
					}
					if (pxgridColumn != null && pxgridColumn.DisplayMode != ValueDisplayMode.Value && !pxgridColumn.GetMatrixMode())
					{
						pxselectorBase.DisplayMode = pxgridColumn.DisplayMode;
					}
					pxselectorBase.DataMember = null;
					((IFieldEditor)pxselectorBase).SynchronizeState(pxfieldState);
				}
			}
		}

		// Token: 0x06002098 RID: 8344 RVA: 0x00083818 File Offset: 0x00081A18
		private object GetCellEditorState()
		{
			PXSyncCellStateEventHandler pxsyncCellStateEventHandler = (PXSyncCellStateEventHandler)base.Events[PXGrid.SyncCellStateEvent];
			if (pxsyncCellStateEventHandler != null)
			{
				PXSyncCellStateEventArgs pxsyncCellStateEventArgs = new PXSyncCellStateEventArgs(null, this.DataKey, this.DataValues);
				pxsyncCellStateEventHandler(this, pxsyncCellStateEventArgs);
				return pxsyncCellStateEventArgs.State;
			}
			return this.GetOriginalCellEditorState();
		}

		// Token: 0x06002099 RID: 8345 RVA: 0x00083868 File Offset: 0x00081A68
		private object GetOriginalCellEditorState()
		{
			PXDataSourceView pxdataSourceView = this.GetDataView() as PXDataSourceView;
			if (pxdataSourceView != null)
			{
				this.PerformSelectRowWithEvent();
				if (this.rowDataItem == null && (pxdataSourceView.CanDelete || this.DataKey.Value != null))
				{
					OrderedDictionary keys = PXGrid.CloneDictionary(this.DataKey.Values);
					OrderedDictionary orderedDictionary = PXGrid.CloneDictionary(this.DataValues.Values);
					bool flag = true;
					try
					{
						this.PerformUpdate(keys, orderedDictionary, null);
					}
					catch (Exception)
					{
						flag = false;
					}
					if (flag)
					{
						this.keyTable = keys;
						this.dataKey = null;
						this.PerformSelectRowWithEvent();
						this.PerformDelete(keys, orderedDictionary);
					}
				}
				return pxdataSourceView.GetStateExt(this.rowDataItem, this.editingField);
			}
			return null;
		}

		// Token: 0x0600209A RID: 8346 RVA: 0x00083924 File Offset: 0x00081B24
		private void PerformSelectRowWithEvent()
		{
			this.arguments = this.CreateDataSourceSelectArguments();
			this.argumentsExt = this.CreateSelectArgumentsExt();
			PXSelectEventArgs e = new PXSelectEventArgs(this.arguments, this.argumentsExt);
			this.OnSelect(e);
			this.arguments.MaximumRows = 1;
			this.PerformSelectRow(this.arguments, this.argumentsExt);
		}

		// Token: 0x0600209B RID: 8347 RVA: 0x00083980 File Offset: 0x00081B80
		private void RenderFilterBox(XmlWriter writer)
		{
			PXFilterEditor pxfilterEditor = this.filterEditor;
			if (pxfilterEditor == null)
			{
				pxfilterEditor = (this.filterEditor = this.CreateFilterEditor());
				this.Controls.Add(pxfilterEditor);
			}
			((IPXScriptControl)pxfilterEditor).RegisterFlags = ScriptRegisterFlag.NotSet;
			((IPXDynamicControl)pxfilterEditor).SetRenderState(true);
			pxfilterEditor.ApplyStyleSheetSkin(this.Page);
			pxfilterEditor.RenderDrillDownFilter = this.HasDrillDownFilter;
			pxfilterEditor.FilterID = this.FilterID;
			pxfilterEditor.DataBind();
			PXStyleManager.RegisterRecursive(pxfilterEditor);
			JSManager.SetControlListName("_filterBox", this.Page);
			JSManager.RegisterRecursive(pxfilterEditor);
			writer.WriteAttributeString("Filter", ControlHelper.RenderControl(pxfilterEditor));
		}

		// Token: 0x0600209C RID: 8348 RVA: 0x00083A18 File Offset: 0x00081C18
		private PXFilterEditor CreateFilterEditor()
		{
			PXFilterEditor pxfilterEditor = new PXFilterEditor();
			pxfilterEditor.ID = "fe";
			pxfilterEditor.Width = Unit.Pixel(700);
			pxfilterEditor.Height = Unit.Pixel(200);
			pxfilterEditor.Caption = "Filter Settings";
			if (!base.DesignMode)
			{
				pxfilterEditor.CanSave = this.IsScreenInSiteMap;
			}
			if (this.Page != null && this.Page.IsCallback)
			{
				pxfilterEditor.GridData = this;
				if (this.GetDataSource() != null)
				{
					pxfilterEditor.DataSourceID = this.DataSourceID;
					pxfilterEditor.FilterRowsView = this.GetFilterRowsView();
					string value = this.GetFilterView();
					if (!string.IsNullOrEmpty(value))
					{
						pxfilterEditor.FilterView = value;
					}
				}
			}
			return pxfilterEditor;
		}

		// Token: 0x17000B4F RID: 2895
		// (get) Token: 0x0600209D RID: 8349 RVA: 0x00083AC8 File Offset: 0x00081CC8
		private bool IsScreenInSiteMap
		{
			get
			{
				PXSiteMapNode currentNode = PXSiteMap.CurrentNode;
				return currentNode != null && !string.IsNullOrEmpty(currentNode.ScreenID);
			}
		}

		// Token: 0x0600209E RID: 8350 RVA: 0x00083AF0 File Offset: 0x00081CF0
		public object GetState(string fieldName)
		{
			PXDataSourceView pxdataSourceView = this.GetDataView() as PXDataSourceView;
			if (pxdataSourceView != null)
			{
				return pxdataSourceView.GetFilterStateExt(null, fieldName);
			}
			return null;
		}

		// Token: 0x0600209F RID: 8351 RVA: 0x00083B18 File Offset: 0x00081D18
		private void RestoreColsOrder(string orderStr)
		{
			if (string.IsNullOrEmpty(orderStr))
			{
				orderStr = this.ClientState.ColumnsOrder;
			}
			if (string.IsNullOrEmpty(orderStr))
			{
				return;
			}
			string[] array = orderStr.Split(new char[]
			{
				'|'
			});
			int num = 0;
			while (num < this.Levels.Count && num < array.Length)
			{
				PXGridColumnCollection columns = this.Levels[num].Columns;
				Hashtable hashtable = new Hashtable();
				ArrayList arrayList = new ArrayList();
				for (int i = 0; i < columns.Count; i++)
				{
					PXGridColumn pxgridColumn = columns[i];
					string key = pxgridColumn.GetKey();
					if (key.Length > 0 && !hashtable.ContainsKey(key))
					{
						hashtable.Add(key, pxgridColumn);
						arrayList.Add(key);
					}
				}
				orderStr = string.Join(",", (string[])arrayList.ToArray(typeof(string)));
				if (orderStr != array[num] && array[num].Length > 0)
				{
					string[] array2 = array[num].Split(new char[]
					{
						','
					});
					for (int j = 0; j < array2.Length; j++)
					{
						PXGridColumn pxgridColumn = hashtable[array2[j]] as PXGridColumn;
						if (pxgridColumn != null)
						{
							columns.Remove(pxgridColumn);
							columns.Insert((j > columns.Count) ? columns.Count : j, pxgridColumn);
						}
					}
				}
				num++;
			}
		}

		// Token: 0x060020A0 RID: 8352 RVA: 0x00083C80 File Offset: 0x00081E80
		private string GetUrlForUpdates()
		{
			HttpContext httpContext = HttpContext.Current;
			if (httpContext == null)
			{
				throw new InvalidOperationException("HttpContext is expected.");
			}
			string text = this.IsGenericInq ? this.GetGenInqWebQueryUrl() : this.GetUniversalWebQueryUrl();
			text = httpContext.Request.GetWebsiteUrl() + text;
			return PXGrid.AppendCompanyId(text);
		}

		// Token: 0x17000B50 RID: 2896
		// (get) Token: 0x060020A1 RID: 8353 RVA: 0x00083CD0 File Offset: 0x00081ED0
		private bool IsGenericInq
		{
			get
			{
				return HttpContext.Current.Request.RawUrl.ToLower().Contains("/genericinquiry.aspx");
			}
		}

		// Token: 0x060020A2 RID: 8354 RVA: 0x00083CF0 File Offset: 0x00081EF0
		private string GetGenInqWebQueryUrl()
		{
			return PXUrl.ToAbsoluteUrl("~/Export/GenInqExcelQuery.axd", true);
		}

		// Token: 0x060020A3 RID: 8355 RVA: 0x00083CFD File Offset: 0x00081EFD
		private string GetUniversalWebQueryUrl()
		{
			return PXUrl.ToAbsoluteUrl("~/Export/ExcelQuery.axd", true);
		}

		// Token: 0x060020A4 RID: 8356 RVA: 0x00083D0C File Offset: 0x00081F0C
		private static string AppendCompanyId(string url)
		{
			string companyName = PXAccess.GetCompanyName();
			if (companyName == null)
			{
				return url;
			}
			return url + (url.Contains('?') ? '&' : '?').ToString() + "companyid=" + Uri.EscapeUriString(companyName);
		}

		// Token: 0x060020A5 RID: 8357 RVA: 0x00083D50 File Offset: 0x00081F50
		private void SaveExportData(UserSessionHelper userSessionHelper, string urlForUpdates, string screenId, string company, IEnumerable<PXGridViewParameters> selectParams, IEnumerable<GridExportFilter> selectFilters, bool isGenInq, string caption)
		{
			IPXPrepareExport ipxprepareExport = this.DataGraph as IPXPrepareExport;
			PXGridExportInfo pxgridExportInfo = new PXGridExportInfo(PXAccess.GetUserID());
			pxgridExportInfo.UrlForUpdates = urlForUpdates;
			pxgridExportInfo.ScreenId = screenId;
			pxgridExportInfo.ViewName = this.DataMember;
			pxgridExportInfo.Company = company;
			pxgridExportInfo.IsGenInquiry = isGenInq;
			pxgridExportInfo.Name = caption;
			foreach (GridColumnExportInfo item in this.ExportColumns())
			{
				pxgridExportInfo.Columns.Add(item);
			}
			int num = 0;
			string text = WebConfigurationManager.AppSettings["maxExportRows"];
			if (text != null)
			{
				int.TryParse(text, out num);
			}
			if (this.exportItems == null)
			{
				return;
			}
			int num2 = 0;
			foreach (PXGridRow pxgridRow in this.exportItems)
			{
				if (!string.IsNullOrEmpty(pxgridRow.ErrorText) || pxgridRow.HasNote)
				{
					this.ActiveRow = pxgridRow;
					pxgridExportInfo.Rows[num2] = new GridRowExportInfo(pxgridRow.ErrorText, this.GetNoteInt(pxgridRow.DataItem));
				}
				int num3 = 0;
				int num4 = 0;
				foreach (object obj in this.Columns)
				{
					PXGridColumn pxgridColumn = (PXGridColumn)obj;
					if (pxgridColumn.ForceExport)
					{
						num4++;
					}
					else
					{
						PXGridCell pxgridCell = pxgridRow.Cells[num4];
						GridColumnExportInfo value = pxgridExportInfo.Columns[num3];
						if (string.IsNullOrEmpty(value.Language))
						{
							object value2 = pxgridCell.Value;
							bool flag = pxgridColumn.MatrixMode.GetValueOrDefault() ? (pxgridCell.ValueItems != null && pxgridCell.ValueItems.Items.Count > 0) : (pxgridColumn.ValueItems != null && pxgridColumn.ValueItems.Items.Count > 0);
							if (value.DataType == TypeCode.String || pxgridColumn.Type == GridColumnType.DropDownList || flag || pxgridColumn.DisplayMode == ValueDisplayMode.Text || !string.IsNullOrEmpty(pxgridColumn.TextField))
							{
								value2 = pxgridCell.FormattedText;
								if (value.DataType != TypeCode.String)
								{
									IList<GridColumnExportInfo> columns = pxgridExportInfo.Columns;
									int index = num3;
									value = new GridColumnExportInfo(value.DataField, value.Caption, value.Width, TypeCode.String, value.Decimals, value.DisplayFormat, null);
									columns[index] = value;
								}
							}
							if (ipxprepareExport != null)
							{
								value2 = ipxprepareExport.PrepareValue(pxgridRow.DataItem, pxgridColumn.DataField);
							}
							pxgridExportInfo.Cells.Add(new GridCellExportInfo(num2, num3, value2, pxgridCell.DisplayFormat));
							num3++;
						}
						else
						{
							string[] array = this.DataGraph.GetValueExt(this.DataMember, pxgridRow.DataItem, value.DataField + "Translations") as string[];
							if (array == null)
							{
								int num5 = 1;
								while (num3 + num5 < pxgridExportInfo.Columns.Count && string.Equals(pxgridExportInfo.Columns[num3 + num5].DataField, value.DataField, StringComparison.OrdinalIgnoreCase))
								{
									num5++;
								}
								array = new string[num5];
							}
							foreach (string value3 in array)
							{
								pxgridExportInfo.Cells.Add(new GridCellExportInfo(num2, num3, value3, pxgridCell.DisplayFormat));
								num3++;
							}
						}
						num4++;
					}
				}
				num2++;
				if (num > 0 && num2 >= num)
				{
					break;
				}
			}
			foreach (PXGridViewParameters item2 in selectParams)
			{
				pxgridExportInfo.SelectParameters.Add(item2);
			}
			foreach (GridExportFilter item3 in selectFilters)
			{
				pxgridExportInfo.SelectFilters.Add(item3);
			}
			userSessionHelper.PostValue("GridExportData", pxgridExportInfo);
		}

		// Token: 0x060020A6 RID: 8358 RVA: 0x0008420C File Offset: 0x0008240C
		private IEnumerable<GridExportFilter> ReadFiltersValues()
		{
			List<GridExportFilter> list = new List<GridExportFilter>();
			if (this.FilterID != null && this.FilterID != PXGrid._FE_FILTER_ID)
			{
				using (IEnumerator<PXResult<FilterRow>> enumerator = PXSelectBase<FilterRow, PXSelect<FilterRow, Where<FilterRow.filterID, Equal<Required<FilterRow.filterID>>, And<FilterRow.isUsed, Equal<True>>>>.Config>.Select(this.DataGraph, new object[]
				{
					this.FilterID.Value
				}).GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						FilterRow row = enumerator.Current;
						string dataField = row.DataField;
						PXCache pxcache = PXFilterDetailView.TargetCache(this.DataGraph, new Guid?(this.FilterID.Value), ref dataField);
						if (this.Columns[row.DataField] != null)
						{
							list.Add(new GridExportFilter(row.OpenBrackets.GetValueOrDefault(), row.DataField, row.DataField, (int)row.Condition.Value, pxcache.ValueFromString(dataField, row.ValueSt), row.ValueSt.With((string _) => this.Columns[row.DataField].FormatValue(_)), pxcache.ValueFromString(dataField, row.ValueSt2), row.ValueSt2.With((string _) => this.Columns[row.DataField].FormatValue(_)), row.CloseBrackets.GetValueOrDefault(), row.Operator.GetValueOrDefault() == 1));
						}
					}
					return list;
				}
			}
			if (this.FilterRows != null && this.FilterRows.Count > 0)
			{
				for (int i = 0; i < this.FilterRows.Count; i++)
				{
					PXFilterRow row = this.FilterRows[i];
					list.Add(new GridExportFilter(row.OpenBrackets, row.DataField, row.DataField, (int)row.Condition, row.Value, row.Value.With(delegate(object _)
					{
						if (this.Columns[row.DataField] == null)
						{
							return _.ToString();
						}
						return this.Columns[row.DataField].FormatValue(_);
					}), row.Value2, row.Value2.With(delegate(object _)
					{
						if (this.Columns[row.DataField] == null)
						{
							return _.ToString();
						}
						return this.Columns[row.DataField].FormatValue(_);
					}), row.CloseBrackets, row.OrOperator));
				}
			}
			return list;
		}

		// Token: 0x060020A7 RID: 8359 RVA: 0x0008451C File Offset: 0x0008271C
		private IEnumerable<PXGridViewParameters> ReadControlsValues()
		{
			List<PXGridViewParameters> list = new List<PXGridViewParameters>();
			for (int i = 0; i < PageInfo.Current.DataboundControls.Count; i++)
			{
				DataBoundControl dataBoundControl = PageInfo.Current.DataboundControls[i];
				if (dataBoundControl == this)
				{
					break;
				}
				string dataMember = dataBoundControl.DataMember;
				if (!string.IsNullOrWhiteSpace(dataMember) && dataBoundControl.Parent != null)
				{
					dataBoundControl.Page = this.Page;
					dataBoundControl.DataBind();
					string dsId = dataBoundControl.DataSourceID ?? string.Empty;
					PageInfo.IGraphInfo graphInfo = PageInfo.Current.DataSources.FirstOrDefault((KeyValuePair<string, PageInfo.DataSourceInfo> k) => dsId.Equals(k.Key, StringComparison.OrdinalIgnoreCase)).With((KeyValuePair<string, PageInfo.DataSourceInfo> _) => _.Value).With((PageInfo.DataSourceInfo _) => _.Graph);
					Dictionary<string, object> dictionary = new Dictionary<string, object>();
					Dictionary<string, object> dictionary2 = new Dictionary<string, object>();
					if (graphInfo != null && !graphInfo.DataGraph._InactiveViews.ContainsKey(dataMember))
					{
						PXCache cache = graphInfo.DataGraph.Views[dataMember].Cache;
						foreach (string text in ((IEnumerable<string>)cache.Keys))
						{
							dictionary[text] = cache.GetValue(cache.Current, text);
						}
						object obj = cache.CreateInstance();
						foreach (string text2 in cache.Fields)
						{
							if (!dictionary.ContainsKey(text2))
							{
								object obj2;
								cache.RaiseFieldDefaulting(text2, obj, out obj2);
								if (obj2 != null)
								{
									try
									{
										cache.RaiseFieldUpdating(text2, obj, ref obj2);
									}
									catch (OutOfMemoryException)
									{
										throw;
									}
									catch (StackOverflowException)
									{
										throw;
									}
									catch (Exception)
									{
									}
									cache.SetValue(obj, text2, obj2);
								}
							}
						}
						foreach (string text3 in cache.Fields)
						{
							if (!dictionary.ContainsKey(text3))
							{
								PXFieldState pxfieldState = cache.GetStateExt(obj, text3) as PXFieldState;
								if (pxfieldState != null)
								{
									object value = pxfieldState.Value ?? pxfieldState.DefaultValue;
									dictionary2.Add(text3.ToLower(), value);
								}
							}
						}
					}
					Dictionary<string, KeyValuePair<string, KeyValuePair<string, bool>>> dictionary3 = new Dictionary<string, KeyValuePair<string, KeyValuePair<string, bool>>>();
					foreach (object obj3 in this.SelectAllChildFieldEditors(dataBoundControl))
					{
						IFieldEditor fieldEditor = (IFieldEditor)obj3;
						if (!string.IsNullOrWhiteSpace(fieldEditor.DataField))
						{
							string text4;
							if ((text4 = (fieldEditor as ILabeledControl).With((ILabeledControl _) => _.LabelText)) == null)
							{
								text4 = (fieldEditor as PXCheckBox).With((PXCheckBox _) => _.Text);
							}
							string text5 = text4;
							if (string.IsNullOrWhiteSpace(text5))
							{
								text5 = fieldEditor.DataField;
							}
							object obj4 = fieldEditor.Value;
							bool value2 = false;
							object obj5;
							if (dictionary2.TryGetValue(fieldEditor.DataField.ToLower(), out obj5))
							{
								if (!object.Equals((obj5 as string).Return((string _) => _.Trim(), obj5), obj4))
								{
									if (RelativeDatesManager.IsRelativeDatesString(obj5 as string) && obj4 is DateTime)
									{
										DateTime dateTime = (DateTime)obj4;
										if (RelativeDatesManager.EvaluateAsDateTime(obj5 as string).Value.Date == dateTime.Date)
										{
											goto IL_3C6;
										}
									}
									if (!(fieldEditor is PXCheckBox) || !object.Equals(obj4, false) || obj5 != null)
									{
										goto IL_3C9;
									}
								}
								IL_3C6:
								value2 = true;
							}
							IL_3C9:
							PXSelector pxselector = fieldEditor as PXSelector;
							if (pxselector != null && pxselector.InputMask != null && obj4 is string)
							{
								obj4 = Mask.Format(pxselector.InputMask, obj4.ToString());
							}
							PXDropDown pxdropDown = fieldEditor as PXDropDown;
							if (pxdropDown != null && obj4 is string)
							{
								obj4 = pxdropDown.Text;
							}
							string key = obj4.With((object _) => _.ToString());
							dictionary3[fieldEditor.DataField] = new KeyValuePair<string, KeyValuePair<string, bool>>(text5, new KeyValuePair<string, bool>(key, value2));
						}
					}
					if (dictionary3.Count > 0)
					{
						PXGridViewParameters pxgridViewParameters = new PXGridViewParameters(dataMember);
						foreach (KeyValuePair<string, KeyValuePair<string, KeyValuePair<string, bool>>> keyValuePair in dictionary3)
						{
							string key2 = keyValuePair.Key;
							string key3 = keyValuePair.Value.Key;
							bool value3 = keyValuePair.Value.Value.Value;
							string key4 = keyValuePair.Value.Value.Key;
							GridExportParameter param = new GridExportParameter(key2, key3, value3, key4);
							pxgridViewParameters.Add(param);
						}
						list.Add(pxgridViewParameters);
					}
				}
			}
			return list;
		}

		// Token: 0x060020A8 RID: 8360 RVA: 0x00084B3C File Offset: 0x00082D3C
		private IEnumerable SelectAllChildFieldEditors(Control container)
		{
			foreach (object obj in container.Controls)
			{
				Control control = (Control)obj;
				if (control is IFieldEditor)
				{
					yield return control;
				}
				foreach (object obj2 in this.SelectAllChildFieldEditors(control))
				{
					yield return obj2;
				}
				IEnumerator enumerator2 = null;
				control = null;
			}
			IEnumerator enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x060020A9 RID: 8361 RVA: 0x00084B53 File Offset: 0x00082D53
		private IEnumerable<GridColumnExportInfo> ExportColumns()
		{
			foreach (object obj in this.Columns)
			{
				PXGridColumn col = (PXGridColumn)obj;
				if (!col.ForceExport)
				{
					TypeCode dataType = col.DataType;
					if (!TypeHelper.IsNumeric(dataType) && dataType != TypeCode.DateTime)
					{
						dataType = TypeCode.String;
					}
					double width = col.Visible ? col.Width.Value : 0.0;
					string format = col.DisplayFormat;
					IFieldEditor fieldEditor;
					if (col.RenderEditorText && (fieldEditor = col.Level.GetFieldEditor(col.DataField)) != null)
					{
						if (fieldEditor is PXDateTimeEdit)
						{
							PXDateTimeEdit pxdateTimeEdit = (PXDateTimeEdit)fieldEditor;
							format = (pxdateTimeEdit.TimeMode ? "HH:mm" : pxdateTimeEdit.DisplayPattern);
						}
						else if (fieldEditor is PXMaskEdit)
						{
							PXMaskEdit pxmaskEdit = (PXMaskEdit)fieldEditor;
							if (!string.IsNullOrEmpty(pxmaskEdit.InputMask))
							{
								format = pxmaskEdit.InputMask;
							}
						}
					}
					PXStringState pxstringState = this.DataGraph.GetStateExt(this.DataMember, null, col.DataField) as PXStringState;
					string[] array;
					if (pxstringState != null && !string.IsNullOrEmpty(pxstringState.Language) && (array = (this.DataGraph.GetValueExt(this.DataMember, null, col.DataField + "Translations") as string[])) != null)
					{
						foreach (string text in array)
						{
							yield return new GridColumnExportInfo(col.DataField, col.Header.Text + " " + text.ToUpper(), width, dataType, col.Decimals, format, text);
						}
						string[] array2 = null;
					}
					else
					{
						yield return new GridColumnExportInfo(col.DataField, col.Header.Text, width, dataType, col.Decimals, format, null);
					}
					format = null;
					col = null;
				}
			}
			IEnumerator enumerator = null;
			yield break;
			yield break;
		}

		// Token: 0x060020AA RID: 8362 RVA: 0x00084B63 File Offset: 0x00082D63
		internal static PXGrid.FilterRowType GetFilterType(PXFilterRow filter)
		{
			return (PXGrid.FilterRowType)Convert.ToUInt32(filter.With((PXFilterRow _) => _.Tag));
		}

		// Token: 0x060020AB RID: 8363 RVA: 0x00084B90 File Offset: 0x00082D90
		protected override void OnPreRender(EventArgs e)
		{
			this.EnsureDataBound();
			this.SetRenderState(this.renderState);
			this.StyleManager.Register();
			if (this.EnableClientScript)
			{
				this.Page.RegisterRequiresPostBack(this);
				JSManager.Register(this);
				if (!string.IsNullOrEmpty(this.EditPageUrl))
				{
					PostBackOptions postBackOptions = new PostBackOptions(this, string.Empty);
					postBackOptions.ActionUrl = base.ResolveUrl(this.EditPageUrl);
					this.Page.ClientScript.GetPostBackEventReference(postBackOptions);
				}
			}
			base.OnPreRender(e);
		}

		// Token: 0x17000B51 RID: 2897
		// (get) Token: 0x060020AC RID: 8364 RVA: 0x00084C18 File Offset: 0x00082E18
		protected override HtmlTextWriterTag TagKey
		{
			get
			{
				return HtmlTextWriterTag.Table;
			}
		}

		// Token: 0x060020AD RID: 8365 RVA: 0x00084C1C File Offset: 0x00082E1C
		protected override void AddAttributesToRender(HtmlTextWriter writer)
		{
			writer.AddAttribute(HtmlTextWriterAttribute.Name, this.UniqueID);
			if (string.IsNullOrEmpty(base.Style["position"]))
			{
				writer.AddStyleAttribute(HtmlTextWriterStyle.Position, "relative");
			}
			if (!this.Layout.WrapText.GetValueOrDefault())
			{
				this.CssClass += " nobr";
			}
			base.AddAttributesToRender(writer);
		}

		// Token: 0x060020AE RID: 8366 RVA: 0x00084C90 File Offset: 0x00082E90
		protected override Style CreateControlStyle()
		{
			if (this.style == null)
			{
				this.style = new PXTableStyle();
				this.style.CellPadding = (this.style.CellSpacing = 0);
				this.style.Layout = TableLayout.Fixed;
			}
			return this.style;
		}

		// Token: 0x060020AF RID: 8367 RVA: 0x00084CDC File Offset: 0x00082EDC
		internal void EnsureChildControlsInternal()
		{
			base.EnsureChildControls();
		}

		// Token: 0x060020B0 RID: 8368 RVA: 0x00084CE4 File Offset: 0x00082EE4
		protected override void CreateChildControls()
		{
			if (this.ViewState["_!ItemCount"] == null)
			{
				this.ViewState["_!ItemCount"] = 0;
			}
			base.CreateChildControls();
		}

		// Token: 0x060020B1 RID: 8369 RVA: 0x00084D14 File Offset: 0x00082F14
		protected internal void RecreateChildControls()
		{
			base.ChildControlsCreated = false;
			this.CreateChildControls();
		}

		// Token: 0x060020B2 RID: 8370 RVA: 0x00084D24 File Offset: 0x00082F24
		protected override int CreateChildControls(IEnumerable dataSource, bool dataBinding)
		{
			this.Controls.Clear();
			if (this.CaptionVisible && !string.IsNullOrEmpty(this.Caption))
			{
				this.Controls.Add(this.RenderCaption());
			}
			PXGridLevel primaryLevel = this.PrimaryLevel;
			if (this.TemplateEditMode)
			{
				this.Controls.Add(this.CreateTemplateEditRow(this.Levels[this.TemplateLevel]));
				return 0;
			}
			foreach (object obj in this.Levels)
			{
				((PXGridLevel)obj).ResolveLevelPropeties();
			}
			if (!base.DesignMode && !this.suppressBinding)
			{
				if (!dataBinding)
				{
					this.InitColumnsLayout();
				}
				if (!this.columnsSynchronized || dataBinding)
				{
					this.SynchronizeColsStateExt();
				}
			}
			this.CorrectTollbarButtons();
			if (!base.DesignMode)
			{
				this.MarkStateColumns();
			}
			if (!base.DesignMode && this.ActionBar.CustomItems.Count > 0)
			{
				this.FillDependOnGridFromDS();
			}
			bool flag = this.ActionBar.Position == ActionsPosition.TopAndBottom;
			if (this.ActionBar.Position == ActionsPosition.Top || flag)
			{
				this.RenderToolsRow(true);
			}
			if (this.ShowFilterToolbarFinal)
			{
				this.RenderFilterToolsRow();
			}
			if (!dataBinding || !base.DesignMode)
			{
				TableRow tableRow = this.RenderStationaryMargin(primaryLevel, false);
				if (!primaryLevel.LayoutFinal.HeaderVisible.GetValueOrDefault())
				{
					tableRow.Style[HtmlTextWriterStyle.Display] = "none";
				}
				this.Controls.Add(tableRow);
			}
			if (dataBinding || !base.DesignMode)
			{
				TableRow tableRow2 = this.RenderDataTableRow();
				this.Controls.Add(tableRow2);
				if (this.AllowPivotTable && (!this.Page.IsCallback || this.pivotID != null))
				{
					this.RenderPivotTable(tableRow2.Cells[0]);
				}
			}
			else
			{
				TableRow tableRow3 = new TableRow();
				tableRow3.Cells.Add(new TableCell());
				ControlHelper.CopyHeight(tableRow3, this);
				this.Controls.Add(tableRow3);
			}
			if (primaryLevel.LayoutFinal.FooterVisible.GetValueOrDefault())
			{
				this.Controls.Add(this.RenderStationaryMargin(primaryLevel, true));
			}
			if (this.ActionBar.Position == ActionsPosition.Bottom || flag)
			{
				this.RenderToolsRow(false);
			}
			if (!base.DesignMode)
			{
				this.RenderRowEditForms();
				this.RenderMenuControls();
				this.RenderFilterEditor();
				this.RenderImportPanel();
				this.RenderFilterSaveDialog();
				string name = this.UniqueID + "$fb";
				if (ControlHelper.IsCallbackOwner(name) || ControlHelper.IsPostbackOwner(name))
				{
					this.CreateFilesDialog();
				}
				if (ControlHelper.IsCallbackOwner(this.UniqueID + "$fd"))
				{
					this.CreateFilterDialog();
				}
			}
			return this.Rows.Count;
		}

		// Token: 0x060020B3 RID: 8371 RVA: 0x00085004 File Offset: 0x00083204
		protected virtual void PrepareControlHierarchy()
		{
			PXGridLevel colMarginStyles = this.Levels[0];
			this.SetColMarginStyles(colMarginStyles);
			if (base.DesignMode)
			{
				this.SetRowsStyles(this.Rows);
			}
			if (this.ActionBar.Position != ActionsPosition.None)
			{
				this.SetToolsStyles();
			}
			if (this.captionCell != null)
			{
				this.StyleManager.SetStyleObject(this.captionCell, PXGrid.GridStyle.Caption);
			}
			if (this.contentCell != null)
			{
				this.StyleManager.SetStyleObject(this.contentCell, PXGrid.GridStyle.ContentCell);
			}
			if (this.formControls != null)
			{
				this.SetRowFormStyles();
			}
			if (this.toolsTop != null && this.toolsTop.ToolBar != null && this.toolsTop.ToolBar.Items.Count == 0)
			{
				this.toolsTop.ToolBar.Visible = false;
			}
			if (this.toolsBottom != null && this.toolsBottom.ToolBar != null && this.toolsBottom.ToolBar.Items.Count == 0)
			{
				this.toolsBottom.ToolBar.Visible = false;
			}
			if (this.AllowAutoHide && !base.DesignMode)
			{
				((IAutoHideControl)this).CalculateVisibility();
				if (this.hidden)
				{
					base.Style[HtmlTextWriterStyle.Display] = "none";
				}
			}
		}

		// Token: 0x060020B4 RID: 8372 RVA: 0x00085144 File Offset: 0x00083344
		protected override void Render(HtmlTextWriter writer)
		{
			if (base.DesignMode)
			{
				this.EnsureChildControls();
			}
			if (!base.DesignMode)
			{
				foreach (PXMenu pxmenu in this.menuControls)
				{
					pxmenu.Visible = false;
				}
				foreach (PXTable pxtable in this.formControls)
				{
					pxtable.Visible = false;
				}
				if (this.filterEditor != null)
				{
					this.filterEditor.Visible = false;
				}
				if (this.importPanel != null)
				{
					this.importPanel.Visible = false;
				}
				if (this.filterSavePanel != null)
				{
					this.filterSavePanel.Visible = false;
				}
			}
			if (this.HasControls())
			{
				this.PrepareControlHierarchy();
			}
			base.Render(writer);
			if (!base.DesignMode)
			{
				foreach (PXTable pxtable2 in this.formControls)
				{
					pxtable2.Visible = true;
					pxtable2.RenderControl(writer);
				}
				foreach (PXMenu pxmenu2 in this.menuControls)
				{
					pxmenu2.Visible = true;
					pxmenu2.RenderControl(writer);
				}
				if (this.importPanel != null)
				{
					this.importPanel.Visible = true;
					this.importPanel.RenderControl(writer);
				}
				if (this.filterSavePanel != null)
				{
					this.filterSavePanel.Visible = true;
					this.filterSavePanel.RenderControl(writer);
				}
				string str = this.RenderXmlData().Replace("--", "%2d");
				WebControl webControl = new WebControl(HtmlTextWriterTag.Span);
				webControl.ID = this.ClientID + base.ClientIDSeparator.ToString() + "xml";
				webControl.Style[HtmlTextWriterStyle.Display] = "none";
				webControl.Controls.Add(new LiteralControl("<!-- " + str + " -->"));
				webControl.RenderControl(writer);
			}
		}

		// Token: 0x060020B5 RID: 8373 RVA: 0x00085394 File Offset: 0x00083594
		private void RenderPivotTable(WebControl owner)
		{
			if (this.pivotDS == null && this.Page.IsCallback)
			{
				PXPivotDataSource pxpivotDataSource = new PXPivotDataSource
				{
					ID = "pivotDS",
					Visible = false,
					DataScreenGraph = this.DataGraph,
					PivotControlID = "pivotT",
					PivotScreenID = PXContext.GetScreenID().Replace(".", "")
				};
				this.pivotDS = pxpivotDataSource;
			}
			if (this.pivotTable == null)
			{
				PXPivotTable pt = new PXPivotTable
				{
					ID = "pivotT",
					AllowChangeLayout = false,
					AutoRepaint = false,
					ShowInactiveFields = false,
					ShowZeroValues = true,
					Width = Unit.Percentage(100.0)
				};
				pt.AutoSize.Enabled = true;
				pt.Style[HtmlTextWriterStyle.Display] = "none";
				pt.ApplyStyleSheetSkin(this.Page);
				if (this.Page.IsCallback)
				{
					pt.DataSourceID = "pivotDS";
				}
				this.pivotTable = pt;
				Action<bool> setFilter = delegate(bool useFastFilter)
				{
					List<PXFilterRow> list = pt.FilterRows;
					list.Clear();
					if (this.filterActive)
					{
						list.AddRange(this.GetAdvancedFilterRows());
					}
					List<PXFilterRow> list2 = (from f in this.GetQuickFilterRows()
					where f.Condition != PXCondition.EQ || f.Value != null
					select (PXFilterRow)f.Clone()).ToList<PXFilterRow>();
					list2.ForEach(delegate(PXFilterRow f)
					{
						PXGridColumn pxgridColumn = this.Columns[f.DataField];
						if (pxgridColumn != null && !string.IsNullOrEmpty(pxgridColumn.TextFieldColumn))
						{
							f.DataField = pxgridColumn.TextFieldColumn;
						}
					});
					pt.QuickFilters = list2;
					if (useFastFilter)
					{
						List<PXFilterRow> fastFilterRows = this.GetFastFilterRows();
						if (fastFilterRows != null && fastFilterRows.Count > 0)
						{
							if (list.Count > 0)
							{
								list[0].OpenBrackets++;
								list[list.Count - 1].CloseBrackets++;
								list[list.Count - 1].OrOperator = false;
							}
							list.AddRange(fastFilterRows);
						}
					}
				};
				pt.Select += delegate(object sender, PXSelectEventArgs e)
				{
					setFilter(true);
				};
				pt.BeforeDrillDown += delegate(object sender, EventArgs e)
				{
					setFilter(false);
				};
				pt.AfterDrillDown += delegate(object sender, CancelEventArgs e)
				{
					PXCallbackManager.GetInstance().ActiveCommand.RepaintControlsIDs = this.ID;
					this.reloadFilters = true;
					this.pivotID = null;
					e.Cancel = true;
				};
			}
			if (this.pivotDS != null)
			{
				if (this.pivotID != null)
				{
					this.pivotDS.PivotTableID = new int?(this.pivotID.Value);
				}
				owner.Controls.Add(this.pivotDS);
				((IPXDataControl)this.pivotTable).SuppressDataBinding = true;
			}
			owner.Controls.Add(this.pivotTable);
			if (this.pivotDS != null)
			{
				((IPXDataControl)this.pivotTable).SuppressDataBinding = false;
				this.pivotDS.EnsureChildControlsInternal();
			}
			if (!string.IsNullOrEmpty(this.EditPivotTableUrl))
			{
				WebControl webControl = new WebControl(HtmlTextWriterTag.Iframe)
				{
					ID = "pivotE"
				};
				webControl.Attributes["frameborder"] = "0";
				webControl.Style[HtmlTextWriterStyle.Display] = "none";
				webControl.Width = (webControl.Height = Unit.Percentage(100.0));
				owner.Controls.Add(webControl);
			}
		}

		// Token: 0x17000B52 RID: 2898
		// (get) Token: 0x060020B6 RID: 8374 RVA: 0x0008560C File Offset: 0x0008380C
		private PXStyleManager StyleManager
		{
			get
			{
				if (this.styleManager == null)
				{
					this.styleManager = new PXStyleManager(this);
					this.styleManager.GetStyleMethod = new PXGetStyleMethod(this.ResolveStyle);
					this.styleManager.GetCssClassMethod = new PXGetCssClassMethod(this.ResolveCssClass);
				}
				return this.styleManager;
			}
		}

		// Token: 0x17000B53 RID: 2899
		// (get) Token: 0x060020B7 RID: 8375 RVA: 0x00085661 File Offset: 0x00083861
		// (set) Token: 0x060020B8 RID: 8376 RVA: 0x00085674 File Offset: 0x00083874
		bool ICSSProvider.RenderCss
		{
			get
			{
				return STM.GetProp<bool>(this.ViewState, "RenderCss", true);
			}
			set
			{
				STM.SetProp<bool>(this.ViewState, "RenderCss", value, true);
			}
		}

		// Token: 0x060020B9 RID: 8377 RVA: 0x00085688 File Offset: 0x00083888
		void ICSSProvider.RegisterCss()
		{
			this.StyleManager.Register(typeof(PXGrid.GridStyle), Array.Empty<Enum>());
			this.RegisterComplexStyles(true);
		}

		// Token: 0x060020BA RID: 8378 RVA: 0x000856AC File Offset: 0x000838AC
		PXStyle ICSSProvider.GetStyle(Enum styleType)
		{
			ActionsPosition position = this.ActionBar.Position;
			bool flag = true;
			switch ((PXGrid.GridStyle)styleType)
			{
			case PXGrid.GridStyle.ToolsCell:
				flag = (position != ActionsPosition.None || this.AllowPaging);
				break;
			case PXGrid.GridStyle.ToolsBottom:
				flag = (position == ActionsPosition.Bottom || position == ActionsPosition.TopAndBottom);
				break;
			case PXGrid.GridStyle.SearchEditor:
			case PXGrid.GridStyle.SearchText:
				flag = this.AllowSearch;
				break;
			}
			PXStyle result = null;
			if (flag)
			{
				result = this.GetStyle((PXGrid.GridStyle)styleType);
			}
			return result;
		}

		// Token: 0x060020BB RID: 8379 RVA: 0x00085723 File Offset: 0x00083923
		string ICSSProvider.GetClassName(Enum styleType)
		{
			return this.ClientID + "_" + styleType.ToString();
		}

		// Token: 0x060020BC RID: 8380 RVA: 0x0008573C File Offset: 0x0008393C
		private void RegisterComplexStyles(bool registerRows)
		{
			PXStyleManager pxstyleManager = this.StyleManager;
			this.RegisterLevelStyles(null);
			Type typeFromHandle = typeof(PXGrid.ColumnStyle);
			string[] names = Enum.GetNames(typeFromHandle);
			for (int i = 0; i < this.Levels.Count; i++)
			{
				PXGridLevel pxgridLevel = this.Levels[i];
				this.RegisterLevelStyles(pxgridLevel);
				for (int j = 0; j < pxgridLevel.Columns.Count; j++)
				{
					PXGridColumn col = pxgridLevel.Columns[j];
					foreach (string value in names)
					{
						PXGrid.ColumnStyle columnStyle = (PXGrid.ColumnStyle)Enum.Parse(typeFromHandle, value);
						PXStyle originalColumnStyle = this.GetOriginalColumnStyle(col, columnStyle);
						if (originalColumnStyle.HasStyleAttributes())
						{
							string columnClassName = this.GetColumnClassName(col, columnStyle);
							pxstyleManager.RegisterCssClass(originalColumnStyle, columnClassName, this.GetColumnStyleKey(col, columnStyle));
						}
					}
				}
			}
			if (registerRows)
			{
				for (int l = 0; l < this.Rows.Count; l++)
				{
					PXGridRow pxgridRow = this.Rows[l];
					if (pxgridRow.Style.HasStyleAttributes())
					{
						string rowClassName = this.GetRowClassName(pxgridRow);
						pxstyleManager.RegisterCssClass(pxgridRow.Style, rowClassName, this.GetRowStyleKey(pxgridRow));
					}
				}
			}
		}

		// Token: 0x060020BD RID: 8381 RVA: 0x00085888 File Offset: 0x00083A88
		private void RegisterLevelStyles(PXGridLevel level)
		{
			Type typeFromHandle = typeof(PXGrid.LevelStyle);
			string[] names = Enum.GetNames(typeFromHandle);
			PXGridLevelStyles styles = (level != null) ? level.Styles : this.LevelStyles;
			int index = (level != null) ? level.Index : -1;
			foreach (string value in names)
			{
				PXGrid.LevelStyle levelStyle = (PXGrid.LevelStyle)Enum.Parse(typeFromHandle, value);
				PXStyle pxstyle = this.GetOrignalLevelStyle(styles, levelStyle);
				if (pxstyle.ShouldSerialize())
				{
					pxstyle = this.ResolveLevelStyle(level, levelStyle);
				}
				string levelClassName = this.GetLevelClassName(level, levelStyle);
				string levelStyleKey = this.GetLevelStyleKey(levelStyle, index);
				this.StyleManager.RegisterCssClass(pxstyle, levelClassName, levelStyleKey);
			}
		}

		// Token: 0x060020BE RID: 8382 RVA: 0x00085934 File Offset: 0x00083B34
		private PXStyle ResolveStyle(Enum type)
		{
			PXStyle pxstyle = this.GetStyle((PXGrid.GridStyle)type);
			if ((PXGrid.GridStyle)type == PXGrid.GridStyle.ToolsBottom)
			{
				pxstyle.MergeWith(this.GetStyle(PXGrid.GridStyle.ToolsCell));
			}
			return pxstyle;
		}

		// Token: 0x060020BF RID: 8383 RVA: 0x00085968 File Offset: 0x00083B68
		private string ResolveCssClass(Enum type)
		{
			List<string> list = new List<string>();
			this.AppendCssClass(list, new Enum[]
			{
				type
			});
			if ((PXGrid.GridStyle)type == PXGrid.GridStyle.ToolsBottom)
			{
				this.AppendCssClass(list, new Enum[]
				{
					PXGrid.GridStyle.ToolsCell
				});
			}
			return string.Join(" ", list.ToArray());
		}

		// Token: 0x060020C0 RID: 8384 RVA: 0x000859BC File Offset: 0x00083BBC
		private PXStyle GetStyle(PXGrid.GridStyle type)
		{
			PXCellStyle pxcellStyle = new PXCellStyle();
			switch (type)
			{
			case PXGrid.GridStyle.ToolsCell:
				pxcellStyle.CopyFrom(this.GridStyles.ToolsCell);
				break;
			case PXGrid.GridStyle.ToolsBottom:
				pxcellStyle.CopyFrom(this.GridStyles.ToolsBottom);
				break;
			case PXGrid.GridStyle.Caption:
				pxcellStyle.CopyFrom(this.GridStyles.Caption);
				break;
			case PXGrid.GridStyle.SearchEditor:
				pxcellStyle.CopyFrom(this.GridStyles.SearchEditor);
				break;
			case PXGrid.GridStyle.SearchText:
				pxcellStyle.CopyFrom(this.GridStyles.SearchText);
				break;
			case PXGrid.GridStyle.HeaderCell:
				pxcellStyle.CopyFrom(this.GridStyles.HeaderCell);
				break;
			case PXGrid.GridStyle.ContentCell:
				pxcellStyle.CopyFrom(this.GridStyles.ContentCell);
				break;
			}
			return pxcellStyle;
		}

		// Token: 0x060020C1 RID: 8385 RVA: 0x00085A7C File Offset: 0x00083C7C
		private PXStyle ResolveLevelStyle(PXGridLevel level, PXGrid.LevelStyle type)
		{
			int index = (level != null) ? level.Index : -1;
			string levelStyleKey = this.GetLevelStyleKey(type, index);
			PXStyle pxstyle = this.StyleManager.GetCachedStyle(levelStyleKey);
			if (pxstyle == null)
			{
				if (level == null)
				{
					pxstyle = this.GetLevelStyle(this.LevelStyles, type, this.MergeStyles, false);
				}
				else
				{
					pxstyle = this.GetLevelStyle(level.Styles, type, this.MergeStyles, false);
					if (pxstyle != null)
					{
						PXStyle levelStyle = this.GetLevelStyle(this.LevelStyles, type, this.MergeStyles, false);
						pxstyle.MergeWith(levelStyle);
					}
				}
				this.NormalizeLevelStyle(level, pxstyle, type);
				this.StyleManager.AddCachedStyle(pxstyle, levelStyleKey);
			}
			return pxstyle;
		}

		// Token: 0x060020C2 RID: 8386 RVA: 0x00085B14 File Offset: 0x00083D14
		private string ResolveLevelCssClass(PXGridLevel level, PXGrid.LevelStyle type)
		{
			string levelStyleKey = this.GetLevelStyleKey(type, level.Index);
			string text = this.StyleManager.GetCachedCss(levelStyleKey);
			if (text == null)
			{
				text = this.GetLevelCssClass(type, level.Index);
				if (string.IsNullOrEmpty(text))
				{
					text = this.GetLevelCssClass(type, -1);
				}
				this.StyleManager.AddCachedCss(text, levelStyleKey);
			}
			return text;
		}

		// Token: 0x060020C3 RID: 8387 RVA: 0x00085B6C File Offset: 0x00083D6C
		private string GetLevelCssClass(PXGrid.LevelStyle type, int levelIndex)
		{
			string levelStyleKey = this.GetLevelStyleKey(type, levelIndex);
			string text = this.StyleManager.GetCssClass(levelStyleKey);
			if (string.IsNullOrEmpty(text) && this.MergeStyles)
			{
				PXGrid.LevelStyle? topStyle = this.GetTopStyle(type);
				if (topStyle != null)
				{
					text = this.GetLevelCssClass(topStyle.Value, levelIndex);
				}
			}
			return text;
		}

		// Token: 0x060020C4 RID: 8388 RVA: 0x00085BC0 File Offset: 0x00083DC0
		private PXStyle GetLevelStyle(PXGridLevelStyles stl, PXGrid.LevelStyle type, bool merge, bool normalize)
		{
			PXCellStyle pxcellStyle = new PXCellStyle();
			pxcellStyle.CopyFrom(this.GetOrignalLevelStyle(stl, type));
			if (merge)
			{
				switch (type)
				{
				case PXGrid.LevelStyle.AltRow:
				case PXGrid.LevelStyle.SelRow:
				case PXGrid.LevelStyle.ActiveRow:
					pxcellStyle.MergeWithStyles(new Style[]
					{
						stl.Row
					});
					break;
				case PXGrid.LevelStyle.ActiveCell:
					pxcellStyle.MergeWithStyles(new Style[]
					{
						stl.ActiveRow,
						stl.Row
					});
					break;
				case PXGrid.LevelStyle.RowSelector:
					pxcellStyle.MergeWithStyles(new Style[]
					{
						stl.Header,
						stl.Row
					});
					break;
				case PXGrid.LevelStyle.Header:
					pxcellStyle.MergeWithStyles(new Style[]
					{
						stl.Row
					});
					break;
				case PXGrid.LevelStyle.Footer:
					pxcellStyle.MergeWithStyles(new Style[]
					{
						stl.Header,
						stl.Row
					});
					break;
				case PXGrid.LevelStyle.SelHeader:
					pxcellStyle.MergeWithStyles(new Style[]
					{
						stl.Header,
						stl.Row
					});
					break;
				case PXGrid.LevelStyle.SelFooter:
					pxcellStyle.MergeWithStyles(new Style[]
					{
						stl.Footer,
						stl.Header,
						stl.Row
					});
					break;
				}
			}
			if (normalize)
			{
				this.NormalizeLevelStyle(null, pxcellStyle, type);
			}
			return pxcellStyle;
		}

		// Token: 0x060020C5 RID: 8389 RVA: 0x00085D10 File Offset: 0x00083F10
		private PXStyle GetOrignalLevelStyle(PXGridLevelStyles styles, PXGrid.LevelStyle type)
		{
			switch (type)
			{
			case PXGrid.LevelStyle.Row:
				return styles.Row;
			case PXGrid.LevelStyle.AltRow:
				return styles.AlternateRow;
			case PXGrid.LevelStyle.SelRow:
				return styles.SelectedRow;
			case PXGrid.LevelStyle.ActiveCell:
				return styles.ActiveCell;
			case PXGrid.LevelStyle.ActiveRow:
				return styles.ActiveRow;
			case PXGrid.LevelStyle.Error:
				return styles.Error;
			case PXGrid.LevelStyle.Warning:
				return styles.Warning;
			case PXGrid.LevelStyle.RowForm:
				return styles.RowForm;
			case PXGrid.LevelStyle.RowSelector:
				return styles.RowSelector;
			case PXGrid.LevelStyle.Header:
				return styles.Header;
			case PXGrid.LevelStyle.Footer:
				return styles.Footer;
			case PXGrid.LevelStyle.SelHeader:
				return styles.SelectedHeader;
			case PXGrid.LevelStyle.SelFooter:
				return styles.SelectedFooter;
			case PXGrid.LevelStyle.CellButton:
				return styles.CellButton;
			case PXGrid.LevelStyle.CellEditor:
				return styles.CellEditor;
			case PXGrid.LevelStyle.EditorText:
				return styles.EditorText;
			case PXGrid.LevelStyle.ReadOnlyCell:
				return styles.ReadOnlyCell;
			default:
				return null;
			}
		}

		// Token: 0x060020C6 RID: 8390 RVA: 0x00085DE4 File Offset: 0x00083FE4
		private PXGrid.LevelStyle? GetTopStyle(PXGrid.LevelStyle styleType)
		{
			switch (styleType)
			{
			case PXGrid.LevelStyle.AltRow:
				return new PXGrid.LevelStyle?(PXGrid.LevelStyle.Row);
			case PXGrid.LevelStyle.SelRow:
				return new PXGrid.LevelStyle?(PXGrid.LevelStyle.Row);
			case PXGrid.LevelStyle.ActiveCell:
				return new PXGrid.LevelStyle?(PXGrid.LevelStyle.ActiveRow);
			case PXGrid.LevelStyle.ActiveRow:
				return new PXGrid.LevelStyle?(PXGrid.LevelStyle.Row);
			case PXGrid.LevelStyle.RowSelector:
				return new PXGrid.LevelStyle?(PXGrid.LevelStyle.Header);
			case PXGrid.LevelStyle.Header:
				return new PXGrid.LevelStyle?(PXGrid.LevelStyle.Row);
			case PXGrid.LevelStyle.Footer:
				return new PXGrid.LevelStyle?(PXGrid.LevelStyle.Header);
			case PXGrid.LevelStyle.SelHeader:
				return new PXGrid.LevelStyle?(PXGrid.LevelStyle.Header);
			case PXGrid.LevelStyle.SelFooter:
				return new PXGrid.LevelStyle?(PXGrid.LevelStyle.Footer);
			}
			return null;
		}

		// Token: 0x060020C7 RID: 8391 RVA: 0x00085E78 File Offset: 0x00084078
		private PXStyle ResolveColumnStyle(PXGridColumn col, PXGrid.ColumnStyle type)
		{
			PXCellStyle pxcellStyle = new PXCellStyle();
			pxcellStyle.CopyFrom(this.GetOriginalColumnStyle(col, type));
			PXGrid.LevelStyle topStyle = this.GetTopStyle(type);
			pxcellStyle.MergeWith(this.ResolveLevelStyle(col.Level, topStyle));
			this.NormalizeColumnStyle(col, pxcellStyle, type);
			return pxcellStyle;
		}

		// Token: 0x060020C8 RID: 8392 RVA: 0x00085EC0 File Offset: 0x000840C0
		private string ResolveColumnCssClass(PXGridColumn col, PXGrid.ColumnStyle type)
		{
			string columnStyleKey = this.GetColumnStyleKey(col, type);
			string text = this.StyleManager.GetCachedCss(columnStyleKey);
			if (text == null)
			{
				List<string> list = new List<string>();
				this.AppendCssClass(list, new string[]
				{
					columnStyleKey
				});
				text = this.ResolveLevelCssClass(col.Level, this.GetTopStyle(type));
				if (!string.IsNullOrEmpty(text))
				{
					list.Add(text);
				}
				text = string.Join(" ", list.ToArray());
				this.StyleManager.AddCachedCss(text, columnStyleKey);
			}
			return text;
		}

		// Token: 0x060020C9 RID: 8393 RVA: 0x00085F40 File Offset: 0x00084140
		private PXStyle GetOriginalColumnStyle(PXGridColumn col, PXGrid.ColumnStyle type)
		{
			switch (type)
			{
			case PXGrid.ColumnStyle.Row:
				return col.Style;
			case PXGrid.ColumnStyle.Header:
				return col.Header.Style;
			case PXGrid.ColumnStyle.Footer:
				return col.Footer.Style;
			case PXGrid.ColumnStyle.CellButton:
				return col.CellButtonStyle;
			default:
				return null;
			}
		}

		// Token: 0x060020CA RID: 8394 RVA: 0x00085F8C File Offset: 0x0008418C
		private PXGrid.LevelStyle GetTopStyle(PXGrid.ColumnStyle styleType)
		{
			switch (styleType)
			{
			case PXGrid.ColumnStyle.Row:
				return PXGrid.LevelStyle.Row;
			case PXGrid.ColumnStyle.Header:
				return PXGrid.LevelStyle.Header;
			case PXGrid.ColumnStyle.Footer:
				return PXGrid.LevelStyle.Footer;
			case PXGrid.ColumnStyle.CellButton:
				return PXGrid.LevelStyle.CellButton;
			default:
				return PXGrid.LevelStyle.Row;
			}
		}

		// Token: 0x060020CB RID: 8395 RVA: 0x00085FB4 File Offset: 0x000841B4
		private PXStyle ResolveCellStyle(PXGridCell cell, PXGrid.CellType cellType)
		{
			PXCellStyle pxcellStyle = new PXCellStyle();
			PXGridColumn column = cell.Column;
			switch (cellType)
			{
			case PXGrid.CellType.Normal:
				pxcellStyle.CopyFrom(this.GetOriginalColumnStyle(column, PXGrid.ColumnStyle.Row));
				pxcellStyle.MergeWith(this.ResolveLevelStyle(column.Level, PXGrid.LevelStyle.Row));
				break;
			case PXGrid.CellType.Alt:
				pxcellStyle.CopyFrom(this.GetOriginalColumnStyle(column, PXGrid.ColumnStyle.Row));
				pxcellStyle.MergeWith(this.ResolveLevelStyle(column.Level, PXGrid.LevelStyle.AltRow));
				break;
			case PXGrid.CellType.ActiveRow:
				pxcellStyle.CopyFrom(this.ResolveLevelStyle(column.Level, PXGrid.LevelStyle.ActiveRow));
				break;
			case PXGrid.CellType.ActiveCell:
				pxcellStyle.CopyFrom(this.ResolveLevelStyle(column.Level, PXGrid.LevelStyle.ActiveCell));
				break;
			case PXGrid.CellType.ReadOnly:
				pxcellStyle.CopyFrom(this.GetOriginalColumnStyle(column, PXGrid.ColumnStyle.Row));
				pxcellStyle.MergeWith(this.ResolveLevelStyle(column.Level, PXGrid.LevelStyle.Row));
				pxcellStyle.MergeWith(this.ResolveLevelStyle(column.Level, PXGrid.LevelStyle.ReadOnlyCell));
				break;
			}
			this.NormalizeCellStyle(cell, pxcellStyle, cellType);
			return pxcellStyle;
		}

		// Token: 0x060020CC RID: 8396 RVA: 0x000860A0 File Offset: 0x000842A0
		private string ResolveCellCssClass(PXGridCell cell, PXGrid.CellType cellType)
		{
			PXGridColumn column = cell.Column;
			PXGrid.LevelStyle topStyle = this.GetTopStyle(cellType);
			List<string> list = new List<string>();
			this.AppendCssClass(list, new string[]
			{
				this.ResolveLevelCssClass(column.Level, topStyle)
			});
			bool flag = false;
			if (cellType > PXGrid.CellType.Alt)
			{
				if (cellType == PXGrid.CellType.ReadOnly)
				{
					this.AppendCssClass(list, new string[]
					{
						this.ResolveLevelCssClass(column.Level, PXGrid.LevelStyle.Row)
					});
					flag = true;
				}
			}
			else
			{
				flag = true;
			}
			if (flag)
			{
				this.AppendCssClass(list, new string[]
				{
					this.GetColumnStyleKey(column, PXGrid.ColumnStyle.Row)
				});
			}
			return string.Join(" ", list.ToArray());
		}

		// Token: 0x060020CD RID: 8397 RVA: 0x00086139 File Offset: 0x00084339
		private PXGrid.LevelStyle GetTopStyle(PXGrid.CellType cellType)
		{
			switch (cellType)
			{
			case PXGrid.CellType.Alt:
				return PXGrid.LevelStyle.AltRow;
			case PXGrid.CellType.ActiveRow:
				return PXGrid.LevelStyle.ActiveRow;
			case PXGrid.CellType.ActiveCell:
				return PXGrid.LevelStyle.ActiveCell;
			case PXGrid.CellType.ReadOnly:
				return PXGrid.LevelStyle.ReadOnlyCell;
			default:
				return PXGrid.LevelStyle.Row;
			}
		}

		// Token: 0x060020CE RID: 8398 RVA: 0x00086160 File Offset: 0x00084360
		private void NormalizeLevelStyle(PXGridLevel level, PXStyle s, PXGrid.LevelStyle type)
		{
			if (!s.HasStyleAttributes() && !string.IsNullOrEmpty(s.CssClass))
			{
				return;
			}
			PXGridLevelLayout pxgridLevelLayout = (level != null) ? level.LayoutFinal : this.Layout;
			bool flag = false;
			switch (type)
			{
			case PXGrid.LevelStyle.Row:
			case PXGrid.LevelStyle.AltRow:
			case PXGrid.LevelStyle.SelRow:
			case PXGrid.LevelStyle.ActiveCell:
			case PXGrid.LevelStyle.ActiveRow:
			case PXGrid.LevelStyle.Error:
			case PXGrid.LevelStyle.Warning:
				s.Height = pxgridLevelLayout.RowHeight;
				s.Width = Unit.Empty;
				if (type != PXGrid.LevelStyle.ActiveCell && type != PXGrid.LevelStyle.ActiveRow)
				{
					this.NormalizeGridLines(s, pxgridLevelLayout);
				}
				flag = true;
				break;
			case PXGrid.LevelStyle.RowSelector:
				s.Height = pxgridLevelLayout.RowHeight;
				s.Width = pxgridLevelLayout.RowSelectorsWidth;
				s.Padding.Reset();
				break;
			case PXGrid.LevelStyle.Header:
			case PXGrid.LevelStyle.Footer:
			case PXGrid.LevelStyle.SelHeader:
			case PXGrid.LevelStyle.SelFooter:
				s.Width = Unit.Empty;
				flag = true;
				break;
			case PXGrid.LevelStyle.CellEditor:
			case PXGrid.LevelStyle.EditorText:
				s.Height = (s.Width = Unit.Empty);
				break;
			}
			if (flag && s.CustomAttr.IndexOf("overflow") < 0)
			{
				s.CustomAttr += "overflow:hidden";
			}
		}

		// Token: 0x060020CF RID: 8399 RVA: 0x0008627E File Offset: 0x0008447E
		private void NormalizeColumnStyle(PXGridColumn col, PXStyle s, PXGrid.ColumnStyle type)
		{
			if (type == PXGrid.ColumnStyle.Row)
			{
				this.NormalizeLevelStyle(col.Level, s, PXGrid.LevelStyle.Row);
				return;
			}
			if (type - PXGrid.ColumnStyle.Header > 1)
			{
				return;
			}
			this.NormalizeLevelStyle(col.Level, s, PXGrid.LevelStyle.Header);
		}

		// Token: 0x060020D0 RID: 8400 RVA: 0x000862A8 File Offset: 0x000844A8
		private void NormalizeCellStyle(PXGridCell cell, PXStyle s, PXGrid.CellType type)
		{
			PXGrid.LevelStyle type2 = PXGrid.LevelStyle.Row;
			switch (type)
			{
			case PXGrid.CellType.Alt:
				type2 = PXGrid.LevelStyle.AltRow;
				break;
			case PXGrid.CellType.ActiveRow:
				type2 = PXGrid.LevelStyle.ActiveRow;
				break;
			case PXGrid.CellType.ActiveCell:
				type2 = PXGrid.LevelStyle.ActiveCell;
				break;
			}
			this.NormalizeLevelStyle(cell.Column.Level, s, type2);
		}

		// Token: 0x060020D1 RID: 8401 RVA: 0x000862EC File Offset: 0x000844EC
		private void NormalizeGridLines(PXStyle s, PXGridLevelLayout layout)
		{
			NullableGridLines gridLines = layout.GridLines;
			WebBorder border = s.Border;
			if (gridLines == NullableGridLines.Horizontal || gridLines == NullableGridLines.None)
			{
				border.Left.Width = (border.Right.Width = Unit.Pixel(0));
				border.Left.Style = (border.Right.Style = BorderStyle.None);
			}
			if (gridLines == NullableGridLines.Vertical || gridLines == NullableGridLines.None)
			{
				border.Top.Width = (border.Bottom.Width = Unit.Pixel(0));
				border.Top.Style = (border.Bottom.Style = BorderStyle.None);
			}
		}

		// Token: 0x060020D2 RID: 8402 RVA: 0x00086389 File Offset: 0x00084589
		private string GetLevelStyleKey(PXGrid.LevelStyle type, int index)
		{
			return type.ToString() + ((index < 0) ? string.Empty : index.ToString());
		}

		// Token: 0x060020D3 RID: 8403 RVA: 0x000863B0 File Offset: 0x000845B0
		private string GetColumnStyleKey(PXGridColumn col, PXGrid.ColumnStyle styleType)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("L").Append(col.Level.Index);
			stringBuilder.Append("_").Append(col.GetKey());
			stringBuilder.Append("_").Append(styleType.ToString());
			return stringBuilder.ToString();
		}

		// Token: 0x060020D4 RID: 8404 RVA: 0x00086418 File Offset: 0x00084618
		private string GetRowStyleKey(PXGridRow row)
		{
			return "R_" + row.Index.ToString();
		}

		// Token: 0x060020D5 RID: 8405 RVA: 0x00086440 File Offset: 0x00084640
		private void AppendCssClass(List<string> cssL, params string[] keys)
		{
			foreach (string key in keys)
			{
				string cssClass = this.StyleManager.GetCssClass(key);
				if (!string.IsNullOrEmpty(cssClass))
				{
					cssL.Add(cssClass);
				}
			}
		}

		// Token: 0x060020D6 RID: 8406 RVA: 0x00086480 File Offset: 0x00084680
		private void AppendCssClass(List<string> cssL, params Enum[] keys)
		{
			foreach (Enum styleType in keys)
			{
				string cssClass = this.StyleManager.GetCssClass(styleType);
				if (!string.IsNullOrEmpty(cssClass))
				{
					cssL.Add(cssClass);
				}
			}
		}

		// Token: 0x060020D7 RID: 8407 RVA: 0x000864BD File Offset: 0x000846BD
		private string GetGridClassName(PXGrid.GridStyle styleType)
		{
			StringBuilder stringBuilder = new StringBuilder(this.ClientID);
			stringBuilder.Append("_").Append(styleType.ToString());
			return stringBuilder.ToString();
		}

		// Token: 0x060020D8 RID: 8408 RVA: 0x000864F0 File Offset: 0x000846F0
		private string GetLevelClassName(PXGridLevel level, PXGrid.LevelStyle styleType)
		{
			StringBuilder stringBuilder = new StringBuilder(this.ClientID);
			stringBuilder.Append("_").Append(styleType.ToString());
			if (level != null)
			{
				stringBuilder.Append("_L").Append(level.Index);
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060020D9 RID: 8409 RVA: 0x00086548 File Offset: 0x00084748
		private string GetColumnClassName(PXGridColumn col, PXGrid.ColumnStyle styleType)
		{
			StringBuilder stringBuilder = new StringBuilder(this.ID);
			stringBuilder.Append("_L").Append(col.Level.Index);
			stringBuilder.Append("_").Append(col.GetKey());
			stringBuilder.Append("_").Append(styleType.ToString());
			return stringBuilder.ToString();
		}

		// Token: 0x060020DA RID: 8410 RVA: 0x000865B6 File Offset: 0x000847B6
		private string GetRowClassName(PXGridRow row)
		{
			StringBuilder stringBuilder = new StringBuilder(this.ID);
			stringBuilder.Append("_R").Append(row.Index);
			return stringBuilder.ToString();
		}

		// Token: 0x060020DB RID: 8411 RVA: 0x000865DF File Offset: 0x000847DF
		private string ResolveRowCssClass(PXGridRow row)
		{
			return this.StyleManager.GetCssClass(this.GetRowStyleKey(row));
		}

		// Token: 0x060020DC RID: 8412 RVA: 0x000865F4 File Offset: 0x000847F4
		private void ApplyColStyle(PXGridColumn col, PXTableCell cell, PXGrid.ColumnStyle style)
		{
			if (cell != null)
			{
				PXStyle s = this.ResolveColumnStyle(col, style);
				cell.ApplyStyle(s);
			}
		}

		// Token: 0x060020DD RID: 8413 RVA: 0x00086614 File Offset: 0x00084814
		private void ApplyColCSS(PXGridColumn col, PXTableCell cell, PXGrid.ColumnStyle style)
		{
			if (cell != null)
			{
				string cssClass = this.ResolveColumnCssClass(col, style);
				cell.CssClass = cssClass;
			}
		}

		// Token: 0x060020DE RID: 8414 RVA: 0x00086634 File Offset: 0x00084834
		private void ApplyLevelStyle(PXGridLevel level, PXTableCell cell, PXGrid.LevelStyle style)
		{
			if (cell != null)
			{
				PXStyle s = this.ResolveLevelStyle(level, style);
				cell.ApplyStyle(s);
			}
		}

		// Token: 0x060020DF RID: 8415 RVA: 0x00086654 File Offset: 0x00084854
		private void ApplyLevelCSS(PXGridLevel level, PXTableCell cell, PXGrid.LevelStyle style)
		{
			if (cell != null)
			{
				string cssClass = this.ResolveLevelCssClass(level, style);
				cell.CssClass = cssClass;
			}
		}

		// Token: 0x060020E0 RID: 8416 RVA: 0x00086674 File Offset: 0x00084874
		private void SetColMarginStyles(PXGridLevel level)
		{
			PXStyleManager pxstyleManager = this.StyleManager;
			bool supportsCss = pxstyleManager.SupportsCss;
			for (int i = 0; i < level.Columns.Count; i++)
			{
				PXGridColumn pxgridColumn = level.Columns[i];
				if (supportsCss)
				{
					this.ApplyColCSS(pxgridColumn, pxgridColumn.HeaderCell, PXGrid.ColumnStyle.Header);
					this.ApplyColCSS(pxgridColumn, pxgridColumn.FooterCell, PXGrid.ColumnStyle.Footer);
					this.ApplyColCSS(pxgridColumn, pxgridColumn.HeaderCellStat, PXGrid.ColumnStyle.Header);
				}
				else
				{
					this.ApplyColStyle(pxgridColumn, pxgridColumn.HeaderCell, PXGrid.ColumnStyle.Header);
					this.ApplyColStyle(pxgridColumn, pxgridColumn.FooterCell, PXGrid.ColumnStyle.Footer);
					this.ApplyColStyle(pxgridColumn, pxgridColumn.HeaderCellStat, PXGrid.ColumnStyle.Header);
				}
			}
			if (supportsCss)
			{
				this.ApplyLevelCSS(level, level.HeaderCorner, PXGrid.LevelStyle.Header);
				this.ApplyLevelCSS(level, level.FooterCorner, PXGrid.LevelStyle.Footer);
				this.ApplyLevelCSS(level, level.HeaderCornerStat, PXGrid.LevelStyle.Header);
			}
			else
			{
				this.ApplyLevelStyle(level, level.HeaderCorner, PXGrid.LevelStyle.Header);
				this.ApplyLevelStyle(level, level.FooterCorner, PXGrid.LevelStyle.Footer);
				this.ApplyLevelStyle(level, level.HeaderCornerStat, PXGrid.LevelStyle.Header);
			}
			if (pxstyleManager.SupportsCss)
			{
				if (level.HeaderCell != null)
				{
					level.HeaderCell.CssClass = pxstyleManager.ResolveCssClass(PXGrid.GridStyle.HeaderCell);
				}
				if (level.FooterCell != null)
				{
					level.FooterCell.CssClass = pxstyleManager.ResolveCssClass(PXGrid.GridStyle.HeaderCell) + " footer";
					return;
				}
			}
			else
			{
				if (level.HeaderCell != null)
				{
					level.HeaderCell.ApplyStyle(pxstyleManager.ResolveStyle(PXGrid.GridStyle.HeaderCell));
				}
				if (level.FooterCell != null)
				{
					level.FooterCell.ApplyStyle(pxstyleManager.ResolveStyle(PXGrid.GridStyle.HeaderCell));
				}
			}
		}

		// Token: 0x060020E1 RID: 8417 RVA: 0x000867FC File Offset: 0x000849FC
		private TableRow RenderStationaryMargin(PXGridLevel level, bool footer)
		{
			TableRow tableRow = new TableRow();
			PXTableCell pxtableCell = new PXTableCell();
			WebControl webControl = new WebControl(HtmlTextWriterTag.Div);
			webControl.ID = (footer ? "footerDiv" : "headerDiv");
			webControl.Style[HtmlTextWriterStyle.Position] = "relative";
			webControl.Style[HtmlTextWriterStyle.Overflow] = "hidden";
			if (footer)
			{
				webControl.Controls.Add(this.RenderColFooterTable(level));
				level.FooterCell = pxtableCell;
			}
			else
			{
				webControl.Controls.Add(this.RenderColHeaderTable(level));
				level.HeaderCell = pxtableCell;
			}
			tableRow.Cells.Add(pxtableCell);
			pxtableCell.Controls.Add(webControl);
			return tableRow;
		}

		// Token: 0x060020E2 RID: 8418 RVA: 0x000868A5 File Offset: 0x00084AA5
		private PXTable RenderColHeaderTable(PXGridLevel level)
		{
			PXTable pxtable = this.CreateColMarginTable(level);
			pxtable.ID = "headerT";
			pxtable.Rows.Add(this.RenderColHeaderRow(level, true));
			return pxtable;
		}

		// Token: 0x060020E3 RID: 8419 RVA: 0x000868D0 File Offset: 0x00084AD0
		private TableRow RenderColHeaderRow(PXGridLevel level, bool stationary)
		{
			TableRow tableRow = new TableRow();
			tableRow.TableSection = TableRowSection.TableHeader;
			if (level.LayoutFinal.RowSelectorsVisible.GetValueOrDefault())
			{
				PXTableCell pxtableCell = this.RenderGridCornerCell(level);
				tableRow.Cells.Add(pxtableCell);
				if (stationary)
				{
					level.HeaderCornerStat = pxtableCell;
				}
				else
				{
					level.HeaderCorner = pxtableCell;
				}
			}
			foreach (object obj in level.Columns)
			{
				PXGridColumn pxgridColumn = (PXGridColumn)obj;
				PXTableCell pxtableCell2 = this.RenderColHeader(pxgridColumn, stationary);
				tableRow.Cells.Add(pxtableCell2);
				if (stationary)
				{
					pxgridColumn.HeaderCellStat = pxtableCell2;
				}
				else
				{
					pxgridColumn.HeaderCell = pxtableCell2;
				}
			}
			if (this.OnRenderColHeaderTable != null)
			{
				this.OnRenderColHeaderTable();
			}
			return tableRow;
		}

		// Token: 0x060020E4 RID: 8420 RVA: 0x000869B4 File Offset: 0x00084BB4
		private PXTable RenderColFooterTable(PXGridLevel level)
		{
			PXTable pxtable = this.CreateColMarginTable(level);
			pxtable.ID = "footerT";
			pxtable.Rows.Add(this.RenderColFooterRow(level, true));
			return pxtable;
		}

		// Token: 0x060020E5 RID: 8421 RVA: 0x000869DC File Offset: 0x00084BDC
		private TableRow RenderColFooterRow(PXGridLevel level, bool stationary)
		{
			TableRow tableRow = new TableRow();
			tableRow.TableSection = TableRowSection.TableFooter;
			if (level.LayoutFinal.RowSelectorsVisible.GetValueOrDefault())
			{
				PXTableCell pxtableCell = new PXTableCell();
				pxtableCell.Controls.Add(new LiteralControl("&nbsp;"));
				tableRow.Cells.Add(pxtableCell);
				level.FooterCorner = pxtableCell;
			}
			foreach (object obj in level.Columns)
			{
				PXGridColumn pxgridColumn = (PXGridColumn)obj;
				PXTableCell pxtableCell2 = this.RenderColFooter(pxgridColumn, stationary);
				tableRow.Cells.Add(pxtableCell2);
				pxgridColumn.FooterCell = pxtableCell2;
			}
			return tableRow;
		}

		// Token: 0x060020E6 RID: 8422 RVA: 0x00086AA8 File Offset: 0x00084CA8
		private PXTable CreateColMarginTable(PXGridLevel level)
		{
			PXTable pxtable = new PXTable();
			pxtable.CellPadding = level.LayoutFinal.CellPadding;
			pxtable.CellSpacing = level.LayoutFinal.CellSpacing;
			pxtable.BorderMode = level.LayoutFinal.BorderMode;
			pxtable.Layout = TableLayout.Fixed;
			pxtable.Height = Unit.Percentage(100.0);
			pxtable.Width = Unit.Pixel(0);
			if (!base.DesignMode)
			{
				pxtable.Style[HtmlTextWriterStyle.Position] = "relative";
			}
			pxtable.ColGroup = this.RenderColGroup(level);
			return pxtable;
		}

		// Token: 0x060020E7 RID: 8423 RVA: 0x00086B40 File Offset: 0x00084D40
		private PXTableCell RenderColHeader(PXGridColumn col, bool stationary)
		{
			PXTableCell pxtableCell = this.CreateColMarginCell(col, false);
			PXGridLevel level = col.Level;
			string text = stationary ? "colHS" : "colH";
			string[] value = new string[]
			{
				text,
				level.Index.ToString(),
				col.Index.ToString()
			};
			string text2 = string.Join("_", value);
			pxtableCell.ID = text2;
			if (!stationary)
			{
				pxtableCell.Attributes["name"] = this.ClientID + base.ClientIDSeparator.ToString() + text2;
			}
			WebControl webControl = pxtableCell;
			if (col.Type == GridColumnType.CheckBox && col.AllowCheckAll)
			{
				WebControl webControl2 = ControlHelper.RenderSpriteOrImage(pxtableCell, null, level.ImagesFinal.Unchecked);
				webControl2.Enabled = col.AllowUpdate;
				webControl2.Attributes["check"] = "0";
				pxtableCell.Attributes["headerText"] = col.Header.Text;
				pxtableCell.HorizontalAlign = col.TextAlign;
				pxtableCell.Controls.Add(webControl2);
			}
			else
			{
				string text3 = null;
				string text4 = null;
				ImageAlign imageAlign = ImageAlign.AbsMiddle;
				if (col.Header.ImageUrl.Length > 0)
				{
					text3 = PXStyle.ResolveImageUrl(this, col.Header.ImageUrl);
				}
				string marginText = col.GetMarginText(false);
				if (!string.IsNullOrEmpty(marginText))
				{
					webControl = new WebControl(HtmlTextWriterTag.Div);
					pxtableCell.Controls.Add(webControl);
					webControl.CssClass = "GridHeaderWrap";
					if (this.MarkRequired != MarkRequiredMode.False && col.Required && stationary)
					{
						WebControl webControl3 = webControl;
						webControl3.CssClass += " GridReq";
					}
					if (col.TextAlign == HorizontalAlign.Right)
					{
						WebControl webControl4 = webControl;
						webControl4.CssClass += " right";
					}
				}
				if (!string.IsNullOrEmpty(text3))
				{
					WebControl webControl5 = ControlHelper.RenderSpriteOrImage(webControl, null, text3);
					if (webControl5 is PXImage)
					{
						((PXImage)webControl5).ImageAlign = imageAlign;
					}
					if (text4 != null)
					{
						webControl5.Attributes["type"] = text4;
					}
				}
				if (!string.IsNullOrEmpty(marginText))
				{
					WebControl webControl6 = new WebControl(HtmlTextWriterTag.Div);
					webControl6.CssClass = "GridHeaderText";
					webControl6.Controls.Add(new LiteralControl(marginText));
					webControl.Controls.Add(webControl6);
				}
			}
			if (col.SortDirection != SortDirection.None || col.FilterPosted)
			{
				string imageUrl = (col.SortDirection == SortDirection.Descending) ? level.ImagesFinal.SortDesc : level.ImagesFinal.SortAsc;
				if (col.FilterPosted)
				{
					SortDirection sortDirection = col.SortDirection;
					if (sortDirection != SortDirection.Ascending)
					{
						if (sortDirection != SortDirection.Descending)
						{
							imageUrl = level.ImagesFinal.Filter;
						}
						else
						{
							imageUrl = level.ImagesFinal.FilterSortDesc;
						}
					}
					else
					{
						imageUrl = level.ImagesFinal.FilterSortAsc;
					}
					pxtableCell.Attributes["data-filter"] = "1";
				}
				WebControl webControl7 = ControlHelper.RenderSpriteOrImage(webControl, null, imageUrl);
				if (webControl7 is PXImage)
				{
					((PXImage)webControl7).ImageAlign = ImageAlign.Top;
				}
				webControl7.Attributes["data-type"] = "sort";
			}
			return pxtableCell;
		}

		// Token: 0x060020E8 RID: 8424 RVA: 0x00086E64 File Offset: 0x00085064
		private PXTableCell RenderColFooter(PXGridColumn col, bool stationary)
		{
			PXTableCell pxtableCell = this.CreateColMarginCell(col, true);
			PXGridLevel level = col.Level;
			string text = stationary ? "colFS" : "colF";
			string[] value = new string[]
			{
				text,
				level.Index.ToString(),
				col.Index.ToString()
			};
			string text2 = string.Join("_", value);
			pxtableCell.ID = text2;
			if (!stationary)
			{
				pxtableCell.Attributes["name"] = this.ClientID + base.ClientIDSeparator.ToString() + text2;
			}
			pxtableCell.Controls.Add(new LiteralControl(col.GetMarginText(true)));
			return pxtableCell;
		}

		// Token: 0x060020E9 RID: 8425 RVA: 0x00086F1C File Offset: 0x0008511C
		private PXTableCell CreateColMarginCell(PXGridColumn col, bool footer)
		{
			PXGridLevel level = col.Level;
			PXTableCell pxtableCell = new PXTableCell();
			pxtableCell.ToolTip = col.GetMarginToolTip(footer);
			if (!col.Visible)
			{
				pxtableCell.Style[HtmlTextWriterStyle.Display] = "none";
			}
			pxtableCell.HorizontalAlign = col.TextAlign;
			return pxtableCell;
		}

		// Token: 0x060020EA RID: 8426 RVA: 0x00086F6C File Offset: 0x0008516C
		private Unit GetDataTableWidth(PXGridLevel level)
		{
			int num = -1;
			PXStyle pxstyle = this.ResolveLevelStyle(level, PXGrid.LevelStyle.RowSelector);
			Unit width = level.LayoutFinal.RowSelectorsWidth;
			if (!width.IsEmpty && width.Type == UnitType.Pixel)
			{
				pxstyle.Width = width;
				num = (int)pxstyle.GetFinalWidth().Value;
				foreach (object obj in level.Columns)
				{
					PXGridColumn pxgridColumn = (PXGridColumn)obj;
					width = pxgridColumn.WidthFinal;
					if (width.IsEmpty || width.Type != UnitType.Pixel)
					{
						num = -1;
						break;
					}
					if (pxgridColumn.Visible)
					{
						PXStyle pxstyle2 = this.ResolveColumnStyle(pxgridColumn, PXGrid.ColumnStyle.Header);
						pxstyle2.Width = width;
						num += (int)pxstyle2.GetFinalWidth().Value;
					}
				}
			}
			if (num != -1)
			{
				return Unit.Pixel(num);
			}
			return Unit.Percentage(100.0);
		}

		// Token: 0x060020EB RID: 8427 RVA: 0x00087078 File Offset: 0x00085278
		private PXColGroup RenderColGroup(PXGridLevel level)
		{
			PXColGroup pxcolGroup = new PXColGroup();
			if (level.LayoutFinal.RowSelectorsVisible.GetValueOrDefault())
			{
				PXTableCol pxtableCol = new PXTableCol();
				pxtableCol.Width = level.LayoutFinal.RowSelectorsWidth;
				pxcolGroup.Controls.Add(pxtableCol);
			}
			ArrayList arrayList = new ArrayList();
			foreach (object obj in level.Columns)
			{
				PXGridColumn pxgridColumn = (PXGridColumn)obj;
				PXTableCol pxtableCol2 = new PXTableCol();
				pxtableCol2.Width = (pxgridColumn.Visible ? pxgridColumn.WidthFinal : Unit.Pixel(0));
				if (!pxgridColumn.Visible)
				{
					arrayList.Add(pxtableCol2);
				}
				else
				{
					pxcolGroup.Controls.Add(pxtableCol2);
				}
			}
			return pxcolGroup;
		}

		// Token: 0x060020EC RID: 8428 RVA: 0x00087160 File Offset: 0x00085360
		private PXTableCell RenderGridCornerCell(PXGridLevel level)
		{
			PXTableCell pxtableCell = new PXTableCell();
			pxtableCell.HorizontalAlign = HorizontalAlign.Center;
			pxtableCell.Style[HtmlTextWriterStyle.Padding] = Unit.Pixel(0).ToString();
			if (level.ImagesFinal.GridCorner.Length > 0 && level.LayoutFinal.ColumnsMenu.GetValueOrDefault())
			{
				ControlHelper.RenderSpriteOrImage(pxtableCell, null, level.ImagesFinal.GridCorner);
			}
			else
			{
				pxtableCell.Controls.Add(new LiteralControl("&nbsp;"));
			}
			return pxtableCell;
		}

		// Token: 0x060020ED RID: 8429 RVA: 0x000871F0 File Offset: 0x000853F0
		private TableRow RenderCaption()
		{
			TableRow tableRow = new TableRow();
			tableRow.TableSection = TableRowSection.TableFooter;
			PXTableCell pxtableCell = this.captionCell = new PXTableCell();
			pxtableCell.Text = this.Caption;
			tableRow.Cells.Add(pxtableCell);
			return tableRow;
		}

		// Token: 0x060020EE RID: 8430 RVA: 0x00087234 File Offset: 0x00085434
		private PXGrid.CellType ResolveCellType(PXGridCell cell)
		{
			PXGridRow row = cell.Row;
			PXGrid.CellType result = PXGrid.CellType.Normal;
			if (cell.ReadOnly.GetValueOrDefault() || (cell.ReadOnly == null && !cell.Column.AllowUpdate))
			{
				result = PXGrid.CellType.ReadOnly;
			}
			else if (row.Index % 2 == 1)
			{
				result = PXGrid.CellType.Alt;
			}
			GridHighlightMode gridHighlightMode = row.Level.LayoutFinal.HighlightMode;
			if (gridHighlightMode == GridHighlightMode.NotSet)
			{
				gridHighlightMode = GridHighlightMode.Cell;
			}
			if (row == this.activeRow && (gridHighlightMode == GridHighlightMode.Both || gridHighlightMode == GridHighlightMode.Row))
			{
				result = PXGrid.CellType.ActiveRow;
			}
			if (cell == this.activeCell && (gridHighlightMode == GridHighlightMode.Both || gridHighlightMode == GridHighlightMode.Cell))
			{
				result = PXGrid.CellType.ActiveCell;
			}
			return result;
		}

		// Token: 0x060020EF RID: 8431 RVA: 0x000872C8 File Offset: 0x000854C8
		private void SetActiveRowImage(PXGridLevel lev, PXTableCell c)
		{
			WebControl child = ControlHelper.RenderSpriteOrImage(null, null, lev.ImagesFinal.CurrentRow);
			c.Controls.Clear();
			c.Controls.Add(child);
		}

		// Token: 0x060020F0 RID: 8432 RVA: 0x00087300 File Offset: 0x00085500
		private void SetRowsStyles(PXGridRowCollection rows)
		{
			PXGridLevel level = rows.Level;
			this.ResolveLevelStyle(level, PXGrid.LevelStyle.RowSelector);
			bool supportsCss = this.StyleManager.SupportsCss;
			for (int i = 0; i < rows.Count; i++)
			{
				PXGridRow pxgridRow = rows[i];
				if (pxgridRow.SelectorCell != null)
				{
					if (supportsCss)
					{
						string cssClass = this.ResolveLevelCssClass(level, PXGrid.LevelStyle.RowSelector);
						pxgridRow.SelectorCell.CssClass = cssClass;
					}
					else
					{
						PXStyle s = this.ResolveLevelStyle(level, PXGrid.LevelStyle.RowSelector);
						pxgridRow.SelectorCell.ApplyStyle(s);
					}
					if (pxgridRow == this.activeRow)
					{
						this.SetActiveRowImage(level, pxgridRow.SelectorCell);
					}
				}
				string text = this.ResolveRowCssClass(pxgridRow);
				bool flag = !string.IsNullOrEmpty(text);
				for (int j = 0; j < pxgridRow.Cells.Count; j++)
				{
					PXGridCell pxgridCell = pxgridRow.Cells[j];
					if (pxgridCell.TableCell != null)
					{
						PXGrid.CellType cellType = this.ResolveCellType(pxgridCell);
						if (supportsCss)
						{
							string cssClass2 = this.ResolveCellCssClass(pxgridCell, cellType);
							pxgridCell.TableCell.CssClass = cssClass2;
							if (flag)
							{
								PXTableCell tableCell = pxgridCell.TableCell;
								tableCell.CssClass = tableCell.CssClass + " " + text;
							}
						}
						else
						{
							PXStyle s2 = this.ResolveCellStyle(pxgridCell, cellType);
							pxgridCell.TableCell.ApplyStyle(s2);
						}
						PXGridColumn column = pxgridCell.Column;
						if (column.Type == GridColumnType.Button && column.ButtonDisplay == GridButtonDisplay.Always)
						{
							Button button = pxgridCell.TableCell.Controls[0] as Button;
							if (button != null)
							{
								if (supportsCss)
								{
									string cssClass3 = this.ResolveColumnCssClass(column, PXGrid.ColumnStyle.CellButton);
									button.CssClass = cssClass3;
								}
								else
								{
									PXStyle s3 = this.ResolveColumnStyle(column, PXGrid.ColumnStyle.CellButton);
									button.ApplyStyle(s3);
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x060020F1 RID: 8433 RVA: 0x000874B8 File Offset: 0x000856B8
		private TableRow RenderDataTableRow()
		{
			PXGridLevel pxgridLevel = this.Levels[0];
			TableRow tableRow = new TableRow();
			PXTableCell pxtableCell = this.contentCell = new PXTableCell();
			pxtableCell.VerticalAlign = VerticalAlign.Top;
			pxtableCell.Style[HtmlTextWriterStyle.Overflow] = "hidden";
			ControlHelper.CopyHeight(pxtableCell, this);
			tableRow.Cells.Add(pxtableCell);
			WebControl webControl = new WebControl(HtmlTextWriterTag.Div);
			webControl.ID = "scrollDiv";
			ControlHelper.CopyHeight(webControl, this);
			webControl.Style[HtmlTextWriterStyle.Position] = "relative";
			string value = "auto";
			GridScrollBars scrollBars = this.ScrollBars;
			if (scrollBars != GridScrollBars.Always)
			{
				if (scrollBars == GridScrollBars.None)
				{
					value = "hidden";
				}
			}
			else
			{
				value = "scroll";
			}
			webControl.Style[HtmlTextWriterStyle.Overflow] = value;
			pxtableCell.Controls.Add(webControl);
			if (base.DesignMode)
			{
				webControl.Controls.Add(this.RenderDataTable(this.Rows));
			}
			return tableRow;
		}

		// Token: 0x060020F2 RID: 8434 RVA: 0x000875A4 File Offset: 0x000857A4
		private PXTable RenderDataTable(PXGridRowCollection rows)
		{
			PXGridLevel level = rows.Level;
			PXTable pxtable = new PXTable();
			pxtable.ID = "dataT" + level.Index.ToString();
			pxtable.CellPadding = level.LayoutFinal.CellPadding;
			pxtable.CellSpacing = level.LayoutFinal.CellSpacing;
			pxtable.Layout = TableLayout.Fixed;
			pxtable.Width = Unit.Pixel(0);
			pxtable.BorderMode = level.LayoutFinal.BorderMode;
			pxtable.Attributes["name"] = this.ClientID + base.ClientIDSeparator.ToString() + pxtable.ID;
			pxtable.ColGroup = this.RenderColGroup(level);
			TableRow tableRow = this.RenderColHeaderRow(level, false);
			pxtable.Rows.Add(tableRow);
			if ((level.Index == 0 && !base.DesignMode) || !level.LayoutFinal.HeaderVisible.GetValueOrDefault())
			{
				tableRow.Style[HtmlTextWriterStyle.Display] = "none";
			}
			foreach (object obj in rows)
			{
				PXGridRow row = (PXGridRow)obj;
				pxtable.Rows.Add(this.RenderDataRow(row));
			}
			if (level.Index > 0 && level.LayoutFinal.FooterVisible.GetValueOrDefault())
			{
				TableRow row2 = this.RenderColFooterRow(level, false);
				pxtable.Rows.Add(row2);
			}
			return pxtable;
		}

		// Token: 0x060020F3 RID: 8435 RVA: 0x00087744 File Offset: 0x00085944
		private TableRow RenderDataRow(PXGridRow row)
		{
			PXGridLevel level = row.Level;
			PXGridLevelLayout layoutFinal = level.LayoutFinal;
			string text = row.HashCode();
			string[] value = new string[]
			{
				"row",
				text
			};
			TableRow tableRow = new TableRow();
			tableRow.TableSection = TableRowSection.TableBody;
			tableRow.ID = string.Join("_", value);
			if (!layoutFinal.RowHeight.IsEmpty)
			{
				tableRow.Height = layoutFinal.RowHeight;
			}
			if (!row.Visible)
			{
				tableRow.Style[HtmlTextWriterStyle.Display] = "none";
			}
			if (!string.IsNullOrEmpty(row.Key))
			{
				tableRow.Attributes["key"] = row.Key;
			}
			if (layoutFinal.RowSelectorsVisible.GetValueOrDefault())
			{
				PXTableCell pxtableCell = new PXTableCell();
				pxtableCell.Height = layoutFinal.RowHeight;
				pxtableCell.Style[HtmlTextWriterStyle.Padding] = Unit.Pixel(0).ToString();
				string rowLabel = level.ImagesFinal.RowLabel;
				if (rowLabel.Length > 0)
				{
					ControlHelper.RenderSpriteOrImage(pxtableCell, null, rowLabel);
				}
				else
				{
					pxtableCell.Controls.Add(new LiteralControl("&nbsp;"));
				}
				tableRow.Cells.Add(pxtableCell);
				row.SelectorCell = pxtableCell;
			}
			foreach (object obj in row.Cells)
			{
				PXGridCell pxgridCell = (PXGridCell)obj;
				PXTableCell pxtableCell2 = this.RenderDataCell(pxgridCell);
				tableRow.Cells.Add(pxtableCell2);
				pxgridCell.TableCell = pxtableCell2;
			}
			return tableRow;
		}

		// Token: 0x060020F4 RID: 8436 RVA: 0x00087904 File Offset: 0x00085B04
		private PXTableCell RenderDataCell(PXGridCell cell)
		{
			PXGridRow row = cell.Row;
			PXGridColumn column = cell.Column;
			row.Level.LayoutFinal.WrapText.GetValueOrDefault();
			PXGridLevelImages imagesFinal = column.Level.ImagesFinal;
			PXTableCell pxtableCell = new PXTableCell();
			pxtableCell.Attributes["rawValue"] = column.GetValueText(cell.Value, true);
			pxtableCell.ToolTip = cell.FormattedToolTip;
			pxtableCell.HorizontalAlign = column.TextAlign;
			if (!column.Visible)
			{
				pxtableCell.Style[HtmlTextWriterStyle.Display] = "none";
			}
			string text = cell.FormattedText;
			Control child;
			switch (column.Type)
			{
			case GridColumnType.CheckBox:
			{
				bool flag = cell.Value is bool && (bool)cell.Value;
				WebControl webControl = ControlHelper.RenderSpriteOrImage(null, null, flag ? imagesFinal.Checked : imagesFinal.Unchecked);
				webControl.Enabled = column.AllowUpdate;
				child = webControl;
				goto IL_146;
			}
			case GridColumnType.HyperLink:
				child = this.RenderCellHyperLink(text);
				goto IL_146;
			case GridColumnType.Button:
				if (column.ButtonDisplay == GridButtonDisplay.Always)
				{
					child = new Button
					{
						Text = text
					};
					goto IL_146;
				}
				break;
			case GridColumnType.Icon:
				child = this.RenderCellIcon(text);
				goto IL_146;
			}
			if (string.IsNullOrEmpty(text))
			{
				text = "&nbsp;";
			}
			child = new LiteralControl(text);
			IL_146:
			pxtableCell.Controls.Add(child);
			return pxtableCell;
		}

		// Token: 0x060020F5 RID: 8437 RVA: 0x00087A68 File Offset: 0x00085C68
		private HyperLink RenderCellHyperLink(string cellText)
		{
			string[] array = this.SplitLinkURL(cellText);
			return new HyperLink
			{
				NavigateUrl = this.AddVirtualDirToUrl(array[0]),
				Target = array[1],
				Text = (string.IsNullOrEmpty(array[2]) ? array[0] : array[2])
			};
		}

		// Token: 0x060020F6 RID: 8438 RVA: 0x00087AB4 File Offset: 0x00085CB4
		private WebControl RenderCellIcon(string text)
		{
			WebControl webControl = ControlHelper.RenderSpriteOrImage(null, null, text);
			if (webControl is PXImage)
			{
				((PXImage)webControl).ImageAlign = ImageAlign.Middle;
			}
			return webControl;
		}

		// Token: 0x060020F7 RID: 8439 RVA: 0x00087AE0 File Offset: 0x00085CE0
		private string[] SplitLinkURL(string text)
		{
			string text2 = text;
			string text3 = string.Empty;
			string text4 = string.Empty;
			int num;
			if ((num = text2.LastIndexOf("@")) >= 0)
			{
				text4 = text2.Substring(0, num);
				text2 = text2.Substring(num + 1);
				if (text2.StartsWith("["))
				{
					int num2 = text2.IndexOf("]");
					if (num2 > 0)
					{
						text3 = text2.Substring(1, num2 - 1);
						text2 = text2.Substring(num2 + 1);
					}
					else
					{
						text2 = text2.Substring(1);
					}
				}
				else
				{
					text3 = "_blank";
				}
			}
			return new string[]
			{
				text2,
				text3,
				text4
			};
		}

		// Token: 0x060020F8 RID: 8440 RVA: 0x00087B7C File Offset: 0x00085D7C
		private void SetToolsStyles()
		{
			PXStyleManager pxstyleManager = this.StyleManager;
			if (pxstyleManager.SupportsCss)
			{
				if (this.toolsTop != null)
				{
					this.toolsTop.CssClass = pxstyleManager.ResolveCssClass(PXGrid.GridStyle.ToolsCell);
				}
				if (this.toolsBottom != null)
				{
					this.toolsBottom.CssClass = pxstyleManager.ResolveCssClass(PXGrid.GridStyle.ToolsBottom);
				}
				if (this.toolsFilter != null)
				{
					this.toolsFilter.CssClass = pxstyleManager.ResolveCssClass(PXGrid.GridStyle.ToolsCell);
					return;
				}
			}
			else
			{
				if (this.toolsTop != null)
				{
					this.toolsTop.ApplyStyle(pxstyleManager.ResolveStyle(PXGrid.GridStyle.ToolsCell));
				}
				if (this.toolsBottom != null)
				{
					this.toolsBottom.ApplyStyle(pxstyleManager.ResolveStyle(PXGrid.GridStyle.ToolsBottom));
				}
				if (this.toolsFilter != null)
				{
					this.toolsFilter.ApplyStyle(pxstyleManager.ResolveStyle(PXGrid.GridStyle.ToolsCell));
				}
			}
		}

		// Token: 0x060020F9 RID: 8441 RVA: 0x00087C54 File Offset: 0x00085E54
		private void ToolBar_CallBack(object sender, PXCallBackEventArgs e)
		{
			PXCallBackEventHandler pxcallBackEventHandler = (PXCallBackEventHandler)base.Events[PXGrid.CallBackEvent];
			if (pxcallBackEventHandler != null)
			{
				pxcallBackEventHandler(sender, e);
			}
		}

		// Token: 0x060020FA RID: 8442 RVA: 0x00087C84 File Offset: 0x00085E84
		private void ToolBar_ButtonClick(object sender, PXToolBarClickEventArgs e)
		{
			PXToolBarClickEventHandler pxtoolBarClickEventHandler = (PXToolBarClickEventHandler)base.Events[PXGrid.ButtonClickEvent];
			if (pxtoolBarClickEventHandler != null)
			{
				pxtoolBarClickEventHandler(sender, e);
			}
		}

		// Token: 0x060020FB RID: 8443 RVA: 0x00087CB4 File Offset: 0x00085EB4
		internal void RenderExternalButtons(PXToolBar tlb)
		{
			try
			{
				this.ActionBar.UpdateableExcelByDefault = (this.ActionBar.UpdateableExcelByDefault | (this.GetDataSource() as PXDataSource).With((PXDataSource ds) => ds.DataGraph.HasDashboardsSupport()));
				this.HideFilterToolButtons();
				this.ActionBar.CreateActionButton += this.ToolBar_CreateActionButton;
				this.ActionBar.RenderActionButtons(tlb, ActionVisible.External);
				this.ExternalToolBarID = tlb.ClientID;
			}
			finally
			{
				this.ActionBar.CreateActionButton -= this.ToolBar_CreateActionButton;
			}
		}

		// Token: 0x060020FC RID: 8444 RVA: 0x00087D68 File Offset: 0x00085F68
		internal void ApplyExternalButtons(PXToolBar toolbar, List<ToolbarActionItem> items)
		{
			this.ActionBar.UpdateableExcelByDefault = (this.ActionBar.UpdateableExcelByDefault | (this.GetDataSource() as PXDataSource).With((PXDataSource ds) => ds.DataGraph.HasDashboardsSupport()));
			this.HideFilterToolButtons();
			this.ActionBar.ApplyActionButtons(items, ActionVisible.External, this, toolbar);
			this.ExternalToolBarID = toolbar.ClientID;
		}

		// Token: 0x060020FD RID: 8445 RVA: 0x00087DDC File Offset: 0x00085FDC
		private void RenderFilterToolsRow()
		{
			TableRow tableRow = new TableRow();
			this.Controls.Add(tableRow);
			PXTableCell pxtableCell = new PXTableCell();
			pxtableCell.ID = "af";
			PXTableCell pxtableCell2 = pxtableCell;
			this.toolsFilter = pxtableCell;
			PXTableCell pxtableCell3 = pxtableCell2;
			tableRow.Controls.Add(pxtableCell3);
			pxtableCell3.Attributes["filterInside"] = "1";
			PXToolBar pxtoolBar = new PXToolBar();
			pxtoolBar.ID = "tf";
			pxtoolBar.ImageSet = "main";
			pxtoolBar.CommandSourceID = this.ID;
			pxtoolBar.IsClientControl = false;
			PXToolBar pxtoolBar2 = pxtoolBar;
			this.tlbFilterTools = pxtoolBar;
			PXToolBar pxtoolBar3 = pxtoolBar2;
			pxtableCell3.Controls.Add(pxtoolBar3);
			pxtoolBar3.ApplyStyleSheetSkin(this.Page);
			PXToolBar pxtoolBar4 = pxtoolBar3;
			pxtoolBar4.CssClass += " filterTools";
			PXToolBarLabel item = new PXToolBarLabel
			{
				Width = Unit.Percentage(100.0),
				Text = Msg.GetLocal("Drag column header here to configure filter"),
				Key = "dragTip"
			};
			pxtoolBar3.Items.Add(item);
			this.tlbFilters = new PXToolBar
			{
				ID = "filters",
				CanOverflow = false,
				IsClientControl = false
			};
			pxtoolBar3.Items.Add(new PXToolBarLabel
			{
				Width = Unit.Percentage(100.0),
				Control = this.tlbFilters,
				Visible = false,
				ControlTheming = false
			});
			this.tlbFilters.ApplyStyleSheetSkin(this.Page);
			PXToolBar pxtoolBar5 = this.tlbFilters;
			pxtoolBar5.CssClass += " filters";
			bool flag = this.AllowPivotTable && !string.IsNullOrEmpty(this.EditPivotTableUrl);
			if (flag)
			{
				pxtoolBar3.CreateButton("EditPivot", Msg.GetLocal("Edit Pivot Table"), "Settings", "").ToggleMode = true;
			}
			pxtoolBar3.CreateButton("FilterShow", Msg.GetLocal("Filter Settings"), "Filter", "");
			pxtoolBar3.CreateButton("FilterSave", Msg.GetLocal("Save"), "Save", "");
			PXWebCollection<PXToolBarItemCollection, PXToolBarItem> items = pxtoolBar3.Items;
			PXToolBarButton pxtoolBarButton = new PXToolBarButton();
			pxtoolBarButton.Text = "...";
			pxtoolBarButton.RenderMenuButton = new bool?(false);
			pxtoolBarButton.Key = "more";
			pxtoolBarButton.BypassTranslationWarning = true;
			pxtoolBarButton.AlreadyLocalized = true;
			PXToolBarButton pxtoolBarButton2 = pxtoolBarButton;
			items.Add(pxtoolBarButton);
			pxtoolBarButton2.MenuItems.Add(new PXMenuItem(Msg.GetLocal("Save As"), Sprite.Main.GetFullUrl("Copy"))
			{
				CommandName = "FilterSave",
				CommandArgument = "1",
				AlreadyLocalized = true
			});
			if (flag)
			{
				pxtoolBarButton2.MenuItems.Add(new PXMenuItem(Msg.GetLocal("Save As Pivot"), Sprite.Main.GetFullUrl("Copy"))
				{
					CommandName = "FilterSavePivot",
					ShowSeparator = true,
					AlreadyLocalized = true
				});
			}
			pxtoolBarButton2.MenuItems.Add(new PXMenuItem(Msg.GetLocal("Remove"), Sprite.Main.GetFullUrl("Remove"))
			{
				CommandName = "FilterRemove",
				AlreadyLocalized = true
			});
			PXToolBarLabel pxtoolBarLabel = new PXToolBarLabel
			{
				AllowHide = false,
				ControlTheming = false
			};
			pxtoolBarLabel.Control = this.CreateFilterBar();
			pxtoolBar3.Items.Add(pxtoolBarLabel);
			pxtoolBarLabel.Control.ApplyStyleSheetSkin(this.Page);
			PXGenericDataSource pxgenericDataSource = this.GetDataSource() as PXGenericDataSource;
			int? num = null;
			if (pxgenericDataSource != null)
			{
				num = ((PXGenericInqGrph)pxgenericDataSource.DataGraph).Design.SelectTop;
			}
			if (num != null && num.Value > 0)
			{
				PXToolBarLabel pxtoolBarLabel2 = new PXToolBarLabel
				{
					Key = "limitWarn",
					CssClass = "limit-warn"
				};
				WebControl webControl = new WebControl(HtmlTextWriterTag.Div);
				webControl.Attributes["error"] = 1.ToString();
				webControl.ToolTip = Msg.GetLocal("The number of records is limited to {0}.", new object[]
				{
					num.Value
				});
				ControlHelper.RenderSpriteImage(webControl, null, "control", "Warning");
				pxtoolBarLabel2.Control = webControl;
				pxtoolBar3.Items.Add(pxtoolBarLabel2);
			}
		}

		// Token: 0x060020FE RID: 8446 RVA: 0x0008820F File Offset: 0x0008640F
		private void HideFilterToolButtons()
		{
			if (!this.ShowFilterToolbarFinal)
			{
				return;
			}
			this.ActionBar.Actions.FilterShow.ToolBarVisible = ActionVisible.False;
			this.ActionBar.Actions.FilterBar.ToolBarVisible = ActionVisible.False;
		}

		// Token: 0x060020FF RID: 8447 RVA: 0x00088248 File Offset: 0x00086448
		private void RenderToolsRow(bool top)
		{
			TableRow tableRow = new TableRow();
			this.Controls.Add(tableRow);
			this.HideFilterToolButtons();
			PXActionBar pxactionBar = new PXActionBar(this, top ? ActionVisible.Top : ActionVisible.Bottom);
			pxactionBar.CreateActionButton += this.ToolBar_CreateActionButton;
			pxactionBar.EnableTheming = false;
			pxactionBar.ID = (top ? "at" : "ab");
			pxactionBar.Settings = this.ActionBar;
			pxactionBar.Settings.MenuStyles.CopyFrom(this.MenuStyles);
			pxactionBar.Settings.MenuImages.CopyFrom(this.MenuImages);
			if (top)
			{
				this.toolsTop = pxactionBar;
			}
			else
			{
				this.toolsBottom = pxactionBar;
			}
			tableRow.Controls.Add(pxactionBar);
			PXToolBar toolBar = pxactionBar.ToolBar;
			if (toolBar != null)
			{
				if (base.DesignMode)
				{
					toolBar.Height = Unit.Pixel(30);
				}
				pxactionBar.ToolBar.CallBack += this.ToolBar_CallBack;
				pxactionBar.ToolBar.ButtonClick += this.ToolBar_ButtonClick;
				PXToolBar pxtoolBar = this.filterSelector as PXToolBar;
				if (pxtoolBar != null && pxtoolBar.Tag == toolBar)
				{
					if (this.ActionBar.Actions.FilterShow.Enabled)
					{
						bool flag = this.filterNames != null && this.filterNames.Count > 0;
						pxtoolBar.Style[HtmlTextWriterStyle.Display] = (flag ? "" : "none");
					}
					if (pxactionBar.HasOnlyOneControl(pxtoolBar))
					{
						WebControl webControl = toolBar.Parent as WebControl;
						webControl.Controls.Remove(toolBar);
						webControl.Controls.Add(pxtoolBar);
						webControl.Attributes["tabsInside"] = "1";
						pxtoolBar.CanOverflow = true;
						pxtoolBar.Tag = null;
						pxtoolBar.Items.Add(new PXToolBarLabel(Unit.Percentage(100.0), null));
					}
					if (PXBaseDataSource.RedirectHelper.IsPopupLayer(this.Page))
					{
						pxtoolBar.Attributes["data-hidden"] = "1";
						pxtoolBar.Style[HtmlTextWriterStyle.Display] = "none";
					}
				}
			}
			pxactionBar.UpdateableExcelByDefault |= (this.GetDataSource() as PXDataSource).With((PXDataSource ds) => ds.DataGraph.HasDashboardsSupport());
		}

		// Token: 0x06002100 RID: 8448 RVA: 0x0008849C File Offset: 0x0008669C
		private void FillDependOnGridFromDS()
		{
			PXDataSource pxdataSource = this.GetDataSource() as PXDataSource;
			PXToolBarItemCollection customItems = this.ActionBar.CustomItems;
			if (pxdataSource == null || customItems.Count == 0)
			{
				return;
			}
			StateManagedCollection stateManagedCollection = pxdataSource.CallbackCommands;
			Dictionary<string, PXDSCallbackCommand> dictionary = new Dictionary<string, PXDSCallbackCommand>();
			foreach (object obj in stateManagedCollection)
			{
				PXDSCallbackCommand pxdscallbackCommand = (PXDSCallbackCommand)obj;
				if (pxdscallbackCommand.DependOnGrid == this.ID)
				{
					dictionary.Add(pxdscallbackCommand.Name, pxdscallbackCommand);
				}
			}
			for (int i = 0; i < customItems.Count; i++)
			{
				PXToolBarButton pxtoolBarButton = customItems[i] as PXToolBarButton;
				if (pxtoolBarButton != null && (pxtoolBarButton.CommandSourceID == pxdataSource.ID || (pxtoolBarButton.AutoCallBack.Enabled && pxtoolBarButton.AutoCallBack.Target == pxdataSource.ID)))
				{
					string key = string.IsNullOrEmpty(pxtoolBarButton.CommandSourceID) ? pxtoolBarButton.AutoCallBack.Command : pxtoolBarButton.CommandName;
					if (dictionary.ContainsKey(key))
					{
						pxtoolBarButton.DependOnGrid = this.ID;
					}
				}
			}
		}

		// Token: 0x06002101 RID: 8449 RVA: 0x000885E4 File Offset: 0x000867E4
		private void MarkStateColumns()
		{
			PXDataSource pxdataSource = this.GetDataSource() as PXDataSource;
			if (pxdataSource != null)
			{
				foreach (object obj in pxdataSource.CallbackCommands)
				{
					PXDSCallbackCommand pxdscallbackCommand = (PXDSCallbackCommand)obj;
					if (pxdscallbackCommand.DependOnGrid == this.ID && !string.IsNullOrEmpty(pxdscallbackCommand.StateColumn))
					{
						PXGridColumn pxgridColumn = this.Columns[pxdscallbackCommand.StateColumn];
						if (pxgridColumn != null)
						{
							pxgridColumn.IsStateColumn = true;
						}
					}
				}
			}
			PXToolBarItemCollection customItems = this.ActionBar.CustomItems;
			for (int i = 0; i < customItems.Count; i++)
			{
				PXToolBarButton pxtoolBarButton = customItems[i] as PXToolBarButton;
				if (pxtoolBarButton != null && pxtoolBarButton.DependOnGrid == this.ID && !string.IsNullOrEmpty(pxtoolBarButton.StateColumn))
				{
					PXGridColumn pxgridColumn2 = this.Columns[pxtoolBarButton.StateColumn];
					if (pxgridColumn2 != null)
					{
						pxgridColumn2.IsStateColumn = true;
					}
				}
			}
		}

		// Token: 0x06002102 RID: 8450 RVA: 0x00088700 File Offset: 0x00086900
		private void CorrectTollbarButtons()
		{
			PXDataSource pxdataSource = this.GetDataSource() as PXDataSource;
			PXToolBarItemCollection customItems = this.ActionBar.CustomItems;
			if (pxdataSource == null || customItems.Count == 0)
			{
				return;
			}
			IList<PXBaseDataSource.CommandState> commandStates = pxdataSource.GetCommandStates();
			Dictionary<string, IList<PXBaseDataSource.CommandState>> dictionary = new Dictionary<string, IList<PXBaseDataSource.CommandState>>();
			foreach (PXBaseDataSource.CommandState item in commandStates)
			{
				int num = item.Name.Return((string _) => _.IndexOf("@"), -1);
				if (num > 0 && num < item.Name.Length - 1)
				{
					string key = item.Name.Substring(0, num).ToLower();
					IList<PXBaseDataSource.CommandState> list;
					if (!dictionary.TryGetValue(key, out list))
					{
						list = new List<PXBaseDataSource.CommandState>();
						dictionary.Add(key, list);
					}
					list.Add(item);
				}
			}
			for (int i = 0; i < customItems.Count; i++)
			{
				PXToolBarButton pxtoolBarButton = customItems[i] as PXToolBarButton;
				if (pxtoolBarButton != null)
				{
					string command = pxtoolBarButton.CommandName;
					string text = pxtoolBarButton.CommandSourceID;
					if (string.IsNullOrEmpty(command) || string.IsNullOrEmpty(text))
					{
						command = pxtoolBarButton.AutoCallBack.Command;
						text = pxtoolBarButton.AutoCallBack.Target;
					}
					if (!(text != pxdataSource.ID))
					{
						if (!string.IsNullOrEmpty(command))
						{
							command = command.ToLower();
						}
						IList<PXBaseDataSource.CommandState> list2;
						if (dictionary.TryGetValue(command, out list2))
						{
							pxtoolBarButton.MenuItems.Clear();
							foreach (PXBaseDataSource.CommandState commandState in list2)
							{
								PXMenuItem pxmenuItem = new PXMenuItem
								{
									Text = commandState.DisplayName,
									Visible = commandState.Visible,
									Enabled = commandState.Enabled,
									CommandName = commandState.Name,
									CommandSourceID = text,
									ToolTip = commandState.Tooltip,
									SyncVisible = false
								};
								pxmenuItem.PopupCommand.Command = pxtoolBarButton.PopupCommand.Command;
								pxmenuItem.PopupCommand.Enabled = pxtoolBarButton.PopupCommand.Enabled;
								pxmenuItem.PopupCommand.Argument = pxtoolBarButton.PopupCommand.Argument;
								pxmenuItem.PopupCommand.ActiveBehavior = pxtoolBarButton.PopupCommand.ActiveBehavior;
								pxmenuItem.PopupCommand.Target = pxtoolBarButton.PopupCommand.Target;
								pxmenuItem.Images.Normal = commandState.Image.Normal;
								pxmenuItem.Images.Disabled = commandState.Image.Disabled;
								pxmenuItem.Images.Hover = commandState.Image.Hover;
								pxtoolBarButton.MenuItems.Add(pxmenuItem);
							}
							if (pxtoolBarButton.MenuItems.Count > 0)
							{
								pxtoolBarButton.AutoCallBack.Target = string.Empty;
								pxtoolBarButton.AutoCallBack.Command = (pxtoolBarButton.CommandName = string.Empty);
								PXBaseDataSource.CommandState commandState2 = commandStates.FirstOrDefault((PXBaseDataSource.CommandState _) => _.Name.ToLower() == command);
								if (commandState2.Name != null && commandState2.Name.ToLower() == command)
								{
									pxtoolBarButton.Text = commandState2.DisplayName;
									pxtoolBarButton.Tooltip = commandState2.Tooltip;
									pxtoolBarButton.AlreadyLocalized = true;
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x06002103 RID: 8451 RVA: 0x00088AFC File Offset: 0x00086CFC
		private void ToolBar_CreateActionButton(object sender, PXCommandEventArgs e)
		{
			WebControl webControl = null;
			PXToolBar pxtoolBar = (PXToolBar)sender;
			if (e.CommandName == "FilterSet")
			{
				if (string.IsNullOrEmpty((this.suppressBinding || (base.DesignMode && !PXGraph.ProxyIsActive)) ? string.Empty : this.GetFilterView()))
				{
					return;
				}
				webControl = (this.filterSelector = this.CreateFilterSelector(pxtoolBar));
				this.FillFilterSelector();
				if (this.FilterSelector == FilterSelectorType.Tabs && !pxtoolBar.CssClass.Contains("tabsInside"))
				{
					PXToolBar pxtoolBar2 = pxtoolBar;
					pxtoolBar2.CssClass += " tabsInside";
				}
			}
			else if (e.CommandName == "FilterBar")
			{
				if (this.FastFilterFields.Length != 0 && string.IsNullOrEmpty(this.FastFilterID))
				{
					webControl = this.CreateFilterBar();
				}
				else
				{
					e.Cancel = true;
				}
			}
			if (webControl != null)
			{
				webControl.ApplyStyleSheetSkin(this.Page);
				PXToolBarLabel pxtoolBarLabel = new PXToolBarLabel();
				pxtoolBarLabel.AllowHide = (pxtoolBarLabel.ControlTheming = false);
				pxtoolBarLabel.Control = webControl;
				pxtoolBar.Items.Add(pxtoolBarLabel);
				pxtoolBar.ExtraItems.Add(pxtoolBarLabel);
				e.Cancel = true;
			}
		}

		// Token: 0x06002104 RID: 8452 RVA: 0x00088C24 File Offset: 0x00086E24
		internal bool TryCreateActionButtonControl(PXToolBar toolbar, string commandName)
		{
			WebControl webControl = null;
			bool result = false;
			if (commandName == "FilterSet")
			{
				if (string.IsNullOrEmpty((this.suppressBinding || (base.DesignMode && !PXGraph.ProxyIsActive)) ? string.Empty : this.GetFilterView()))
				{
					return false;
				}
				webControl = (this.filterSelector = this.CreateFilterSelector(null));
				this.FillFilterSelector();
			}
			else if (commandName == "FilterBar")
			{
				if (this.FastFilterFields.Length != 0 && string.IsNullOrEmpty(this.FastFilterID))
				{
					webControl = this.CreateFilterBar();
				}
				else
				{
					result = true;
				}
			}
			if (webControl != null)
			{
				webControl.ApplyStyleSheetSkin(this.Page);
				PXToolBarLabel pxtoolBarLabel = new PXToolBarLabel();
				pxtoolBarLabel.AllowHide = (pxtoolBarLabel.ControlTheming = false);
				pxtoolBarLabel.Control = webControl;
				toolbar.ExtraItems.Add(pxtoolBarLabel);
				result = true;
			}
			return result;
		}

		// Token: 0x06002105 RID: 8453 RVA: 0x00088CF4 File Offset: 0x00086EF4
		private WebControl CreateFilterSelector(PXToolBar owner)
		{
			string text = this.BlankFilterHeader;
			if (this.FilterSelector == FilterSelectorType.DropDown)
			{
				PXDropDown pxdropDown = new PXDropDown();
				pxdropDown.IsClientControl = false;
				pxdropDown.ID = "fs";
				pxdropDown.ValueType = TypeCode.String;
				pxdropDown.AllowNull = false;
				pxdropDown.Attributes["type"] = "filter";
				if (string.IsNullOrEmpty(text))
				{
					text = Msg.GetLocal("All Records");
				}
				pxdropDown.Items.Add(new PXListItem(text, PXGrid._FE_FILTER_ID.ToString()));
				return pxdropDown;
			}
			PXToolBar pxtoolBar = new PXToolBar();
			pxtoolBar.Tag = owner;
			pxtoolBar.ID = "ft";
			pxtoolBar.SkinID = "Tab";
			PXToolBarLabel pxtoolBarLabel = new PXToolBarLabel();
			pxtoolBarLabel.Width = Unit.Pixel(10);
			pxtoolBarLabel.AllowHide = false;
			pxtoolBar.Items.Add(pxtoolBarLabel);
			PXToolBarButton pxtoolBarButton = new PXToolBarButton();
			pxtoolBar.Items.Add(pxtoolBarButton);
			if (string.IsNullOrEmpty(text))
			{
				text = Msg.GetLocal("All Records");
				pxtoolBarButton.AlreadyLocalized = true;
			}
			pxtoolBarButton.Text = text;
			pxtoolBarButton.Key = "all";
			pxtoolBarButton.ToggleMode = true;
			pxtoolBarButton.ToggleGroup = "1";
			pxtoolBarButton.Pushed = true;
			pxtoolBarButton.AllowHide = false;
			pxtoolBarButton.Attributes["first-tab"] = "1";
			pxtoolBarButton.AlreadyLocalized = true;
			pxtoolBarButton = new PXToolBarButton();
			pxtoolBar.Items.Add(pxtoolBarButton);
			pxtoolBarButton.ImageSet = "main";
			pxtoolBarButton.ImageKey = "RecordEdit";
			pxtoolBarButton.Key = "edit";
			pxtoolBarButton.AllowHide = false;
			if (!string.IsNullOrEmpty(this.FilterView) || this.ActionBar.Actions.FilterShow.Enabled)
			{
				pxtoolBarButton.Visible = false;
			}
			return pxtoolBar;
		}

		// Token: 0x06002106 RID: 8454 RVA: 0x00088EB0 File Offset: 0x000870B0
		private void FillFilterSelector()
		{
			if (this.filterSelector == null || this.filterNames == null)
			{
				return;
			}
			PXDropDown pxdropDown = this.filterSelector as PXDropDown;
			PXToolBar pxtoolBar = this.filterSelector as PXToolBar;
			if (pxdropDown != null)
			{
				pxdropDown.Items.Clear();
				string text = this.BlankFilterHeader;
				if (string.IsNullOrEmpty(text))
				{
					text = Msg.GetLocal("All Records");
				}
				pxdropDown.Items.Add(new PXListItem(text, PXGrid._FE_FILTER_ID.ToString()));
				using (Dictionary<Guid, string>.Enumerator enumerator = this.filterNames.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<Guid, string> keyValuePair = enumerator.Current;
						PXListItem item = new PXListItem(keyValuePair.Value, keyValuePair.Key.ToString());
						pxdropDown.Items.Add(item);
					}
					return;
				}
			}
			if (pxtoolBar != null)
			{
				if (pxtoolBar.Items.Count > 3)
				{
					while (pxtoolBar.Items[2].Key != "edit")
					{
						pxtoolBar.Items.RemoveAt(2);
					}
				}
				int num = 2;
				foreach (KeyValuePair<Guid, string> keyValuePair2 in this.filterNames)
				{
					PXToolBarButton pxtoolBarButton = new PXToolBarButton();
					pxtoolBar.Items.Insert(num++, pxtoolBarButton);
					pxtoolBarButton.Text = keyValuePair2.Value;
					pxtoolBarButton.AlreadyLocalized = true;
					pxtoolBarButton.Key = keyValuePair2.Key.ToString();
					pxtoolBarButton.ToggleMode = true;
					pxtoolBarButton.ToggleGroup = "1";
				}
				if (pxtoolBar.HasControls())
				{
					pxtoolBar.RecreateChildControls();
				}
			}
		}

		// Token: 0x06002107 RID: 8455 RVA: 0x0008909C File Offset: 0x0008729C
		private PXButtonEdit CreateFilterBar()
		{
			PXButtonEdit pxbuttonEdit = this.filterBar = new PXButtonEdit();
			pxbuttonEdit.ID = "fb";
			pxbuttonEdit.Value = this.fastFilter;
			pxbuttonEdit.Attributes["type"] = "filterBar";
			return pxbuttonEdit;
		}

		// Token: 0x06002108 RID: 8456 RVA: 0x000890E4 File Offset: 0x000872E4
		private void RenderMenuControls()
		{
			this.menuControls.Clear();
			if (this.LocalMenu)
			{
				PXMenu pxmenu = this.CreateLocalMenu();
				this.menuControls.Add(pxmenu);
				this.Controls.Add(pxmenu);
			}
			foreach (object obj in this.Levels)
			{
				PXGridLevel pxgridLevel = (PXGridLevel)obj;
				if (pxgridLevel.LayoutFinal.ColumnsMenu.GetValueOrDefault())
				{
					PXMenu pxmenu = this.CreateColumnsMenu(pxgridLevel);
					this.menuControls.Add(pxmenu);
					this.Controls.Add(pxmenu);
				}
			}
		}

		// Token: 0x06002109 RID: 8457 RVA: 0x000891A0 File Offset: 0x000873A0
		private PXMenu CreateMenuControl()
		{
			PXMenu pxmenu = new PXMenu();
			pxmenu.MenuStyle = MenuStyle.Popup;
			pxmenu.Width = Unit.Pixel(100);
			((IPXScriptControl)pxmenu).RegisterFlags = (ScriptRegisterFlag)6;
			pxmenu.ApplyStyleSheetSkin(this.Page);
			pxmenu.Styles.CopyFrom(this.MenuStyles);
			pxmenu.Images.CopyFrom(this.MenuImages);
			pxmenu.ExpandEffects.CopyFrom(this.ExpandEffects);
			return pxmenu;
		}

		// Token: 0x0600210A RID: 8458 RVA: 0x0008920C File Offset: 0x0008740C
		private PXMenu CreateLocalMenu()
		{
			PXGridActionBar pxgridActionBar = this.ActionBar;
			PXActionBar pxactionBar = new PXActionBar(this, ActionVisible.Top);
			pxactionBar.Settings = pxgridActionBar;
			PXMenu pxmenu = this.CreateMenuControl();
			pxmenu.ID = "menu";
			pxmenu.CommandSourceID = this.ID;
			pxmenu.ImageSet = "main";
			ActionsTextVisible actionsText = pxgridActionBar.ActionsText;
			try
			{
				pxgridActionBar.ActionsText = ActionsTextVisible.True;
				pxgridActionBar.PagerActionsText = true;
				this.CreateMenuItems(pxmenu, pxactionBar.GetActions(ActionVisible.Top));
				if (pxmenu.Items.Count > 0)
				{
					pxmenu.Items[pxmenu.Items.Count - 1].ShowSeparator = true;
				}
				this.CreateMenuItems(pxmenu, pxactionBar.GetActions(ActionVisible.Bottom));
				if (pxmenu.Items.Count > 0)
				{
					pxmenu.Items[pxmenu.Items.Count - 1].ShowSeparator = true;
				}
				this.CreateMenuItems(pxmenu, pxactionBar.GetActions(ActionVisible.External));
			}
			finally
			{
				pxgridActionBar.ActionsText = actionsText;
				pxgridActionBar.PagerActionsText = false;
			}
			return pxmenu;
		}

		// Token: 0x0600210B RID: 8459 RVA: 0x00089314 File Offset: 0x00087514
		private void CreateMenuItems(PXMenu m, List<PXCommandInfo> al)
		{
			int? num = null;
			PXMenuItem pxmenuItem = null;
			foreach (PXCommandInfo pxcommandInfo in al)
			{
				if (pxcommandInfo.GetMenuVisible())
				{
					if (num != null)
					{
						int groupIndex = pxcommandInfo.GroupIndex;
						int? num2 = num;
						if ((groupIndex > num2.GetValueOrDefault() & num2 != null) && pxmenuItem != null)
						{
							pxmenuItem.ShowSeparator = true;
						}
					}
					if (pxcommandInfo.Name.StartsWith("__custom"))
					{
						PXWebCollection<PXToolBarItemCollection, PXToolBarItem> customItems = this.ActionBar.CustomItems;
						string s = pxcommandInfo.Name.Substring("__custom".Length);
						PXToolBarItem pxtoolBarItem = customItems[int.Parse(s)];
						if (pxtoolBarItem is PXToolBarButton)
						{
							pxmenuItem = this.CreateMenuItem(m, (PXToolBarButton)pxtoolBarItem);
						}
						else if (pxtoolBarItem is PXToolBarSeperator && pxmenuItem != null)
						{
							pxmenuItem.ShowSeparator = true;
						}
					}
					else if (pxcommandInfo.Name != "__pager" && pxcommandInfo.Name != "FilterBar")
					{
						if (pxcommandInfo.Name == "FilterShow")
						{
							continue;
						}
						if (pxcommandInfo.Name == "FilterSet")
						{
							PXCommandInfo pxcommandInfo2 = new PXCommandInfo(this.ActionBar.Actions.FilterShow)
							{
								ImageKey = "Filter"
							};
							pxcommandInfo2.Text = Msg.GetLocal("Filter by This Cell Value (Shift+F)");
							this.CreateMenuItem(m, pxcommandInfo2, ref pxmenuItem);
							this.CreateMenuItem(m, new PXCommandInfo(pxcommandInfo)
							{
								Text = Msg.GetLocal("Clear Column Filter")
							}, ref pxmenuItem);
						}
						else
						{
							this.CreateMenuItem(m, pxcommandInfo, ref pxmenuItem);
							if (pxcommandInfo.Name == "FilterSet")
							{
								pxmenuItem.ShowCheckBox = new bool?(true);
							}
						}
					}
					num = new int?(pxcommandInfo.GroupIndex);
				}
			}
			if (m.Items.Count > 0)
			{
				m.Items[m.Items.Count - 1].ShowSeparator = false;
			}
		}

		// Token: 0x0600210C RID: 8460 RVA: 0x00089548 File Offset: 0x00087748
		private PXMenuItem CreateMenuItem(PXMenu owner, PXCommandInfo cmd, ref PXMenuItem item)
		{
			PXMenuItem pxmenuItem;
			item = (pxmenuItem = new PXMenuItem(cmd.Text, cmd.ImageUrl));
			PXMenuItem pxmenuItem2 = pxmenuItem;
			pxmenuItem2.CommandName = cmd.Name;
			pxmenuItem2.ImageSet = cmd.ImageSet;
			pxmenuItem2.ImageKey = cmd.ImageKey;
			pxmenuItem2.Images.Disabled = cmd.DisabledImageUrl;
			owner.Items.Add(pxmenuItem2);
			return pxmenuItem2;
		}

		// Token: 0x0600210D RID: 8461 RVA: 0x000895B0 File Offset: 0x000877B0
		private PXMenuItem CreateMenuItem(PXMenu owner, string command, string text, string imageKey)
		{
			PXMenuItem pxmenuItem = new PXMenuItem(text);
			pxmenuItem.CommandName = command;
			pxmenuItem.ImageKey = imageKey;
			owner.Items.Add(pxmenuItem);
			return pxmenuItem;
		}

		// Token: 0x0600210E RID: 8462 RVA: 0x000895E4 File Offset: 0x000877E4
		private PXMenuItem CreateMenuItem(PXMenu owner, PXToolBarButton btn)
		{
			PXMenuItem pxmenuItem = new PXMenuItem((!string.IsNullOrEmpty(btn.Text)) ? btn.Text : btn.Tooltip);
			pxmenuItem.Images.CopyFrom(btn.Images);
			pxmenuItem.ImageSet = btn.ImageSet;
			pxmenuItem.ImageKey = btn.ImageKey;
			pxmenuItem.CommandName = btn.CommandName;
			pxmenuItem.Target = btn.Target;
			pxmenuItem.NavigateUrl = btn.NavigateUrl;
			pxmenuItem.NavigateParams.CopyFrom(btn.NavigateParams);
			pxmenuItem.AutoPostBack = btn.AutoPostBack;
			pxmenuItem.AutoCallBack.CopyFrom(btn.AutoCallBack);
			pxmenuItem.DependOnGrid = btn.DependOnGrid;
			pxmenuItem.StateColumn = btn.StateColumn;
			pxmenuItem.Value = btn.Key;
			pxmenuItem.SyncText = btn.SyncText;
			pxmenuItem.PopupPanel = btn.PopupPanel;
			pxmenuItem.PopupCommand.CopyFrom(btn.PopupCommand);
			owner.Items.Add(pxmenuItem);
			if (btn.MenuItems.Count > 0)
			{
				foreach (object obj in btn.MenuItems)
				{
					PXMenuItem src = (PXMenuItem)obj;
					PXMenuItem pxmenuItem2 = new PXMenuItem();
					pxmenuItem2.CopyFrom(src);
					pxmenuItem.ChildItems.Add(pxmenuItem2);
				}
			}
			return pxmenuItem;
		}

		// Token: 0x0600210F RID: 8463 RVA: 0x00089758 File Offset: 0x00087958
		private PXMenu CreateColumnsMenu(PXGridLevel level)
		{
			PXMenu pxmenu = this.CreateMenuControl();
			pxmenu.ID = "menu" + level.Index.ToString();
			pxmenu.CommandSourceID = this.ID;
			pxmenu.ImageSet = "main";
			PXMenuItem item = new PXMenuItem(string.Empty)
			{
				ShowSeparator = true
			};
			pxmenu.Items.Add(item);
			this.CreateMenuItem(pxmenu, "LayoutSave", Msg.GetLocal("Save layout"), "Save");
			this.CreateMenuItem(pxmenu, "LayoutReset", Msg.GetLocal("Reset layout"), "Remove");
			return pxmenu;
		}

		// Token: 0x06002110 RID: 8464 RVA: 0x000897FC File Offset: 0x000879FC
		private void SetRowFormStyles()
		{
			bool supportsCss = this.StyleManager.SupportsCss;
			foreach (PXTable pxtable in this.formControls)
			{
				int index = int.Parse(pxtable.ID[pxtable.ID.Length - 1].ToString());
				PXGridLevel level = this.Levels[index];
				if (!supportsCss)
				{
					PXStyle s = this.ResolveLevelStyle(level, PXGrid.LevelStyle.RowForm);
					pxtable.ApplyStyle(s);
				}
				else
				{
					string cssClass = this.ResolveLevelCssClass(level, PXGrid.LevelStyle.RowForm);
					pxtable.CssClass = cssClass;
				}
			}
		}

		// Token: 0x06002111 RID: 8465 RVA: 0x000898B4 File Offset: 0x00087AB4
		private void RenderFilterEditor()
		{
			if (!this.AllowFilter)
			{
				return;
			}
			if ((this.IsOwnCallback || (this.filterActive && this.Page.IsCallback)) && !this.ExternalFilter)
			{
				if (this.filterEditor == null)
				{
					PXFilterEditor pxfilterEditor = this.CreateFilterEditor();
					pxfilterEditor.EnableTheming = true;
					this.filterEditor = pxfilterEditor;
					this.filterEditor.ShowShortcut = this.filterShortCuts;
				}
				this.Controls.Add(this.filterEditor);
			}
		}

		// Token: 0x06002112 RID: 8466 RVA: 0x00089930 File Offset: 0x00087B30
		private TableRow CreateTemplateEditRow(PXGridLevel level)
		{
			PXTableCell pxtableCell = new PXTableCell();
			pxtableCell.Height = Unit.Percentage(100.0);
			pxtableCell.VerticalAlign = VerticalAlign.Top;
			TableRow tableRow = new TableRow();
			tableRow.Controls.Add(pxtableCell);
			pxtableCell.Controls.Add(this.CreateLevelForm(level));
			return tableRow;
		}

		// Token: 0x06002113 RID: 8467 RVA: 0x00089984 File Offset: 0x00087B84
		private void RenderImportPanel()
		{
			if (this.Mode.AllowUpload.GetValueOrDefault())
			{
				if (this.importPanel == null)
				{
					PXImportWizardPanel pximportWizardPanel = this.importPanel = new PXImportWizardPanel();
					pximportWizardPanel.ID = "imp";
					pximportWizardPanel.CommandSourceID = this.DataSourceID;
					pximportWizardPanel.ItemsDataMember = (string.IsNullOrEmpty(this.ImportDataMember) ? this.DataMember : this.ImportDataMember);
					pximportWizardPanel.PanelID = "pnl";
					pximportWizardPanel.EnableTheming = true;
				}
				this.Controls.Add(this.importPanel);
			}
		}

		// Token: 0x06002114 RID: 8468 RVA: 0x00089A18 File Offset: 0x00087C18
		private void RenderFilterSaveDialog()
		{
			if (this.AllowFilter && this.ShowFilterToolbarFinal)
			{
				PXSmartPanel pxsmartPanel = new PXSmartPanel();
				pxsmartPanel.ID = "pnlSaveF";
				pxsmartPanel.Caption = Msg.GetLocal("Filter Settings");
				pxsmartPanel.CaptionVisible = true;
				pxsmartPanel.AlreadyLocalized = true;
				PXSmartPanel pxsmartPanel2 = pxsmartPanel;
				this.filterSavePanel = pxsmartPanel;
				PXSmartPanel pxsmartPanel3 = pxsmartPanel2;
				PXSmartPanel pxsmartPanel4 = pxsmartPanel3;
				((IParserAccessor)pxsmartPanel4).AddParsedSubObject(new PXLayoutRule
				{
					StartColumn = true,
					ControlSize = "L"
				});
				PXTextEdit pxtextEdit = new PXTextEdit();
				pxtextEdit.ID = "edName";
				pxtextEdit.LabelText = Msg.GetLocal("Filter Name:");
				WebControl webControl = pxtextEdit;
				((IParserAccessor)pxsmartPanel4).AddParsedSubObject(pxtextEdit);
				webControl.ApplyStyleSheetSkin(this.Page);
				PXCheckBox pxcheckBox = new PXCheckBox();
				pxcheckBox.ID = "chkShared";
				pxcheckBox.Text = Msg.GetLocal("Shared Configuration");
				webControl = pxcheckBox;
				((IParserAccessor)pxsmartPanel4).AddParsedSubObject(pxcheckBox);
				webControl.ApplyStyleSheetSkin(this.Page);
				PXCheckBox pxcheckBox2 = new PXCheckBox();
				pxcheckBox2.ID = "chkPivot";
				pxcheckBox2.Hidden = true;
				pxcheckBox2.Text = Msg.GetLocal("Pivot Table");
				webControl = pxcheckBox2;
				((IParserAccessor)pxsmartPanel4).AddParsedSubObject(pxcheckBox2);
				webControl.ApplyStyleSheetSkin(this.Page);
				PXPanel pxpanel = new PXPanel
				{
					SkinID = "Buttons"
				};
				pxpanel.ApplyStyleSheetSkin(this.Page);
				((IParserAccessor)pxsmartPanel4).AddParsedSubObject(pxpanel);
				IParserAccessor parserAccessor = pxpanel;
				PXButton pxbutton = new PXButton();
				pxbutton.ID = "btnSave";
				pxbutton.Text = Msg.GetLocal("OK");
				pxbutton.DialogResult = WebDialogResult.OK;
				pxbutton.AlreadyLocalized = true;
				webControl = pxbutton;
				parserAccessor.AddParsedSubObject(pxbutton);
				webControl.ApplyStyleSheetSkin(this.Page);
				this.Controls.Add(pxsmartPanel3);
				pxsmartPanel3.ApplyStyleSheetSkin(this.Page);
				PXSmartPanel pxsmartPanel5 = pxsmartPanel3;
				pxsmartPanel5.CssClass += " filter-save";
			}
		}

		// Token: 0x06002115 RID: 8469 RVA: 0x00089BCC File Offset: 0x00087DCC
		private void RenderRowEditForms()
		{
			this.formControls.Clear();
			foreach (object obj in this.Levels)
			{
				PXGridLevel pxgridLevel = (PXGridLevel)obj;
				if (pxgridLevel.NeedRenderForm || PXPageCustomization.IsDesignMode)
				{
					if (pxgridLevel.Index == 0)
					{
						this.CreateStandardEditors(pxgridLevel);
					}
					if (pxgridLevel.RowTemplateContainer.HasControls())
					{
						PXTable pxtable = pxgridLevel.EditForm;
						if (pxtable == null)
						{
							pxtable = (pxgridLevel.EditForm = this.CreateLevelForm(pxgridLevel));
							pxtable.Style[HtmlTextWriterStyle.Display] = "none";
						}
						this.formControls.Add(pxtable);
						this.Controls.Add(pxtable);
					}
				}
			}
		}

		// Token: 0x06002116 RID: 8470 RVA: 0x00089CA0 File Offset: 0x00087EA0
		private void CreateStandardEditors(PXGridLevel level)
		{
			WebControl rowTemplateContainer = level.RowTemplateContainer;
			bool flag4;
			bool flag3;
			bool flag2;
			bool flag = flag2 = (flag3 = (flag4 = (this.RenderDefaultEditors || this.MatrixMode)));
			if (!this.RenderDefaultEditors)
			{
				foreach (object obj in level.Columns)
				{
					PXGridColumn pxgridColumn = (PXGridColumn)obj;
					if (pxgridColumn.MatrixMode.GetValueOrDefault())
					{
						flag3 = (flag4 = (flag = (flag2 = true)));
						break;
					}
					if (pxgridColumn.FormEditorID == null)
					{
						if (pxgridColumn.Type == GridColumnType.DropDownList)
						{
							flag3 = true;
						}
						if (pxgridColumn.DataType == TypeCode.DateTime)
						{
							flag4 = true;
						}
						if (!string.IsNullOrEmpty(pxgridColumn.ViewName))
						{
							if (pxgridColumn.MaskItems.Count > 0)
							{
								flag2 = true;
							}
							else
							{
								flag = true;
							}
						}
					}
				}
			}
			if (flag4)
			{
				level.CreateTemplateEditor(GridStandardEditor.Date);
			}
			if (flag3)
			{
				level.CreateTemplateEditor(GridStandardEditor.DropDown);
			}
			if (flag)
			{
				level.CreateTemplateEditor(GridStandardEditor.Selector);
			}
			if (flag2)
			{
				level.CreateTemplateEditor(GridStandardEditor.SegmentMask);
			}
			PXDataSourceView pxdataSourceView = level.GetData() as PXDataSourceView;
			if (pxdataSourceView != null && level.ModeFinal.AllowFormEdit.GetValueOrDefault())
			{
				List<string> list = new List<string>(this.PrimaryLevel.DataKeyNames);
				PXSchemaGenerator pxschemaGenerator = new PXSchemaGenerator(this, null);
				if (!string.IsNullOrEmpty(this.StatusField))
				{
					list.Add(this.StatusField);
				}
				foreach (string text in list)
				{
					if (level.GetFieldEditor(text) == null)
					{
						PXFieldState pxfieldState = pxdataSourceView.GetStateExt(null, text) as PXFieldState;
						if (pxfieldState != null)
						{
							PXFieldSchema pxfieldSchema = PXSchemaGenerator.CreateFieldSchema(pxfieldState);
							if (pxfieldSchema != null)
							{
								WebControl webControl = pxschemaGenerator.CreateControlForField(pxfieldSchema);
								if (webControl != null)
								{
									level.AppendFieldEditor(webControl);
								}
							}
						}
					}
				}
			}
			if (this.EditorsCreated != null)
			{
				this.EditorsCreated(this, EventArgs.Empty);
			}
		}

		// Token: 0x06002117 RID: 8471 RVA: 0x00089EA4 File Offset: 0x000880A4
		private PXTable CreateLevelForm(PXGridLevel level)
		{
			PXTable pxtable = new PXTable();
			pxtable.ID = "lf" + level.Index.ToString();
			pxtable.Height = Unit.Pixel(0);
			pxtable.CellPadding = (pxtable.CellSpacing = 0);
			TableRow tableRow = new TableRow();
			TableRow tableRow2 = new TableRow();
			PXTableCell pxtableCell = new PXTableCell();
			PXTableCell pxtableCell2 = new PXTableCell();
			pxtableCell.VerticalAlign = VerticalAlign.Top;
			if (base.DesignMode)
			{
				WebControl rowTemplateContainer = level.RowTemplateContainer;
				rowTemplateContainer.Width = Unit.Percentage(100.0);
				rowTemplateContainer.Style[HtmlTextWriterStyle.Position] = "relative";
				rowTemplateContainer.Style[HtmlTextWriterStyle.Overflow] = "hidden";
				PXStyle pxstyle = this.ResolveLevelStyle(level, PXGrid.LevelStyle.RowForm);
				if (pxstyle != null)
				{
					pxtable.ApplyStyle(pxstyle);
				}
				pxtableCell.Controls.Add(rowTemplateContainer);
				PXLayoutGenerator pxlayoutGenerator = rowTemplateContainer as PXLayoutGenerator;
				if (pxlayoutGenerator == null || !pxlayoutGenerator.HasLayoutRules)
				{
					pxtable.Height = (pxtableCell.Height = (rowTemplateContainer.Height = Unit.Percentage(100.0)));
					rowTemplateContainer.Attributes[DesignerRegion.DesignerRegionAttributeName] = "0";
				}
			}
			else
			{
				PXFormView pxformView = level.FormView;
				if (pxformView == null)
				{
					pxformView = (level.FormView = new PXFormView());
					pxformView.GridViewMode = (pxformView.AllowInsert = true);
					pxformView.AutoFillSearches = false;
					pxformView.DataSourceID = this.DataSourceID;
					pxformView.DataMember = level.DataMember;
					pxformView.SetDataKeyNames(level.DataKeyNames);
					pxformView.AutoRepaint = this.FormViewMode;
					pxformView.Parameters.CopyFrom(this.Parameters);
					pxformView.Select += this.LevelForm_Select;
					pxformView.ReselectWithoutSearch = true;
					PXPanelCallbacks pxpanelCallbacks = pxformView.CallbackCommands;
					List<PXCallbackCommand> list = new List<PXCallbackCommand>();
					list.AddRange(new PXCallbackCommand[]
					{
						pxpanelCallbacks.Refresh,
						pxpanelCallbacks.AddNew
					});
					foreach (PXCallbackCommand pxcallbackCommand in list)
					{
						pxcallbackCommand.RepaintControls = RepaintMode.OwnerContent;
						pxcallbackCommand.RepaintControlsIDs = this.DataSourceID;
					}
					pxpanelCallbacks.Save.CopyFrom(this.CallbackCommands.Save);
					pxpanelCallbacks.Delete.CopyFrom(this.CallbackCommands.Save);
					pxformView.ID = "lv" + level.Index.ToString();
					pxformView.RenderStyle = FormViewStyle.Simple;
					pxformView.TemplateContainer = level.RowTemplateContainer;
					pxformView.AutoSize.Enabled = true;
				}
				pxtableCell.Controls.Add(pxformView);
				pxformView.TemplateContainer.Page = this.Page;
				pxformView.ApplyStyleSheetSkin(this.Page);
			}
			level.FormButtons.Clear();
			if (!base.DesignMode && !this.AllowPaging)
			{
				this.CreateFormViewButton(level, pxtableCell2, "lfFirst", "First", "PageFirst", "First");
				this.CreateFormViewButton(level, pxtableCell2, "lfPrev", "Prev", "PagePrev", "Prev");
				this.CreateFormViewButton(level, pxtableCell2, "lfNext", "Next", "PageNext", "Next");
				this.CreateFormViewButton(level, pxtableCell2, "lfLast", "Last", "PageLast", "Last");
			}
			tableRow.Cells.Add(pxtableCell);
			tableRow2.Cells.Add(pxtableCell2);
			pxtable.Rows.Add(tableRow);
			pxtable.Rows.Add(tableRow2);
			return pxtable;
		}

		// Token: 0x06002118 RID: 8472 RVA: 0x0008A268 File Offset: 0x00088468
		private void LevelForm_Select(object sender, PXSelectEventArgs arg)
		{
			arg.Cancel = (!this.Page.IsCallback || !this.FormViewMode);
			if (!arg.Cancel)
			{
				PXDSSelectArguments selectArgumentsExt = arg.SelectArgumentsExt;
				this.InitializeSelectArguments(arg.SelectArguments);
				this.InitializeSelectArgumentsExt(selectArgumentsExt);
				arg.SelectArguments.RetrieveTotalRowCount = false;
				selectArgumentsExt.IsGridForm = true;
				PXFormView form = (PXFormView)sender;
				if (this.DataKey.Value != null)
				{
					foreach (string text in form.DataKeyNames)
					{
						selectArgumentsExt.Searches[text] = this.DataKey[text];
					}
					PXView pxview = this.DataGraph.ViewNames.Keys.FirstOrDefault((PXView key) => this.DataGraph.ViewNames[key] == form.DataMember);
					IBqlSortColumn[] sortColumns = pxview.BqlSelect.GetSortColumns();
					for (int i = 0; i < sortColumns.Length; i++)
					{
						Type referencedType = sortColumns[i].GetReferencedType();
						string fieldName = pxview.Cache.GetFieldName(referencedType.Name, false);
						if (!selectArgumentsExt.Searches.Contains(fieldName))
						{
							object value = pxview.Cache.GetValue(pxview.Cache.Current, fieldName);
							if (value != null)
							{
								selectArgumentsExt.Searches[fieldName] = value;
							}
						}
					}
					foreach (string text2 in arg.SelectArguments.SortExpression.Split(new char[]
					{
						','
					}))
					{
						if (text2.EndsWith(" desc"))
						{
							text2 = text2.Substring(0, text2.Length - 5);
						}
						if (!selectArgumentsExt.Searches.Contains(text2))
						{
							object value2 = pxview.Cache.GetValue(pxview.Cache.Current, text2);
							if (value2 != null)
							{
								selectArgumentsExt.Searches[text2] = value2;
							}
						}
					}
				}
			}
		}

		// Token: 0x06002119 RID: 8473 RVA: 0x0008A464 File Offset: 0x00088664
		private PXButton CreateFormViewButton(PXGridLevel level, WebControl owner, string id, string text, string imageKey, string command)
		{
			PXButton pxbutton = new PXButton();
			pxbutton.ID = id + level.Index.ToString();
			pxbutton.Text = text;
			pxbutton.ImageSet = "main";
			pxbutton.ImageKey = imageKey;
			pxbutton.CommandName = command;
			pxbutton.Height = Unit.Pixel(24);
			pxbutton.Width = Unit.Pixel(80);
			pxbutton.Style[HtmlTextWriterStyle.MarginLeft] = Unit.Pixel(5).ToString();
			level.FormButtons.Add(pxbutton);
			owner.Controls.Add(pxbutton);
			pxbutton.ApplyStyleSheetSkin(this.Page);
			return pxbutton;
		}

		// Token: 0x0600211A RID: 8474 RVA: 0x0008A514 File Offset: 0x00088714
		private string RenderXmlData()
		{
			StringWriter stringWriter = new StringWriter();
			XmlWriter xmlWriter = ControlHelper.CreateXmlWriter(null, stringWriter);
			xmlWriter.WriteStartElement("PXGrid");
			xmlWriter.WriteAttributeString("ID", this.ClientID);
			xmlWriter.WriteStartElement("Levels");
			xmlWriter.WriteAttributeString("Count", this.Levels.Count.ToString());
			foreach (object obj in this.Levels)
			{
				PXGridLevel level = (PXGridLevel)obj;
				this.RenderLevelXml(xmlWriter, level);
			}
			xmlWriter.WriteEndElement();
			this.RenderRowsXml(xmlWriter, this.Rows, false);
			ControlHelper.CloseXmlWriter(xmlWriter);
			return stringWriter.ToString();
		}

		// Token: 0x0600211B RID: 8475 RVA: 0x0008A5E8 File Offset: 0x000887E8
		private void RenderLevelXml(XmlWriter writer, PXGridLevel level)
		{
			writer.WriteStartElement("Level");
			writer.WriteAttributeString("i", level.Index.ToString());
			if (level.DataKeyNames != null)
			{
				writer.WriteAttributeString("KeyNames", string.Join(";", level.DataKeyNames));
			}
			writer.WriteAttributeString("AllowUpdate", level.GetAllowUpdate().ToString());
			if (level.ModeFinal.AllowDragRows.GetValueOrDefault())
			{
				writer.WriteAttributeString("Draggable", bool.TrueString);
			}
			string[] props = new string[]
			{
				"CellPadding",
				"CellSpacing",
				"FooterVisible",
				"HeaderVisible",
				"WrapText",
				"LevelIndent",
				"RowHeight",
				"RowSelectorsVisible",
				"RowSelectorsWidth",
				"HighlightMode",
				"ShowRowStatus"
			};
			writer.WriteStartElement("Layout");
			this.RenderXmlAttributes(writer, level.LayoutFinal, props);
			writer.WriteEndElement();
			Type typeFromHandle = typeof(PXGrid.LevelStyle);
			string[] names = Enum.GetNames(typeFromHandle);
			writer.WriteStartElement("Styles");
			foreach (string text in names)
			{
				PXGrid.LevelStyle type = (PXGrid.LevelStyle)Enum.Parse(typeFromHandle, text);
				string text2 = this.ResolveLevelCssClass(level, type);
				if (text2.Length > 0)
				{
					writer.WriteAttributeString(text, text2);
				}
			}
			writer.WriteEndElement();
			string[] props2 = new string[]
			{
				"ArrowDown",
				"ArrowUp",
				"CurrentRow",
				"EditRow",
				"NewRow",
				"RowLabel",
				"GridCorner",
				"Expand",
				"Collapse",
				"SortAsc",
				"SortDesc",
				"Checked",
				"Unchecked",
				"DeletedRow",
				"ModifiedRow",
				"InsertedRow",
				"RowError",
				"RowWarning",
				"RowNote",
				"RowNoteEmpty",
				"RowInfo",
				"RowFile",
				"RowFileEmpty",
				"Search",
				"Filter",
				"FilterSortAsc",
				"FilterSortDesc"
			};
			writer.WriteStartElement("Images");
			this.RenderXmlAttributes(writer, level.ImagesFinal, props2);
			writer.WriteEndElement();
			writer.WriteStartElement("Columns");
			foreach (object obj in level.Columns)
			{
				PXGridColumn col = (PXGridColumn)obj;
				this.RenderColumnXml(writer, col);
			}
			writer.WriteEndElement();
			if (level.NeedRowTemplate)
			{
				this.RenderRowTemplateXml(writer, level);
			}
			writer.WriteEndElement();
		}

		// Token: 0x0600211C RID: 8476 RVA: 0x0008A8F8 File Offset: 0x00088AF8
		private void RenderRowTemplateXml(XmlWriter writer, PXGridLevel level)
		{
			writer.WriteStartElement("Row");
			writer.WriteAttributeString("Visible", false.ToString());
			writer.WriteStartElement("Cells");
			foreach (object obj in level.Columns)
			{
				PXGridColumn pxgridColumn = (PXGridColumn)obj;
				writer.WriteStartElement("Cell");
				object value = pxgridColumn.ResolveDefaultValue();
				string valueText = pxgridColumn.GetValueText(value, true);
				string text = pxgridColumn.FormatValue(value);
				writer.WriteAttributeString("Value", valueText);
				if (valueText != text)
				{
					writer.WriteAttributeString("Text", text);
				}
				if (pxgridColumn.Type == GridColumnType.HyperLink)
				{
					string[] array = this.SplitLinkURL(valueText);
					writer.WriteAttributeString("Href", this.AddVirtualDirToUrl(array[0]));
					if (array[1].Length > 0)
					{
						writer.WriteAttributeString("Target", array[1]);
					}
					if (array[2].Length > 0)
					{
						writer.WriteAttributeString("HrefText", array[2]);
					}
				}
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
			writer.WriteEndElement();
		}

		// Token: 0x0600211D RID: 8477 RVA: 0x0008AA34 File Offset: 0x00088C34
		private void RenderColumnXml(XmlWriter writer, PXGridColumn col)
		{
			writer.WriteStartElement("Column");
			writer.WriteAttributeString("i", col.Index.ToString());
			writer.WriteAttributeString("Width", col.WidthFinal.ToString());
			if (col.Type != GridColumnType.NotSet)
			{
				writer.WriteAttributeString("Type", col.Type.ToString());
			}
			if (col.Type == GridColumnType.Button)
			{
				writer.WriteAttributeString("ButtonDisplay", col.ButtonDisplay.ToString());
			}
			if (!col.Visible)
			{
				writer.WriteAttributeString("Visible", col.Visible.ToString());
			}
			if (!col.AllowUpdate)
			{
				writer.WriteAttributeString("AllowUpdate", col.AllowUpdate.ToString());
			}
			if (col.DataType == TypeCode.String || !string.IsNullOrEmpty(col.TextField))
			{
				writer.WriteAttributeString("Ellipsis", bool.TrueString);
			}
			bool flag = false;
			if (!col.HideViewLink && !string.IsNullOrEmpty(col.ViewName))
			{
				PXSelectorBase pxselectorBase = col.Level.GetFieldEditor(col.DataField) as PXSelectorBase;
				if (pxselectorBase != null && pxselectorBase.AllowEdit)
				{
					flag = true;
				}
			}
			if (!string.IsNullOrEmpty(col.LinkCommand) && !string.IsNullOrEmpty(this.DataSourceID))
			{
				flag = true;
			}
			if (flag)
			{
				writer.WriteAttributeString("ViewLink", bool.TrueString);
			}
			if (col.TextAlign != HorizontalAlign.NotSet)
			{
				writer.WriteAttributeString("TextAlign", col.TextAlign.ToString().ToLower());
			}
			if (col.Multiline.GetValueOrDefault())
			{
				writer.WriteAttributeString("Multiline", bool.TrueString);
			}
			if (col.AllowDragDrop)
			{
				writer.WriteAttributeString("Draggable", bool.TrueString);
			}
			if (col.DataField.Length > 0 && !string.IsNullOrEmpty(this.DataMember) && this.DataGraph != null && !this.DataGraph._InactiveViews.ContainsKey(this.DataMember))
			{
				PXGraph dataGraph = this.DataGraph;
				PXCache pxcache = (dataGraph != null) ? dataGraph.Views[this.DataMember].Cache : null;
				if (pxcache != null)
				{
					string[] array = pxcache.GetValueExt(null, col.DataField + "Translations") as string[];
					if (array != null && array.Length != 0)
					{
						writer.WriteAttributeString("DefaultLocale", array[0]);
					}
				}
			}
			Type typeFromHandle = typeof(PXGrid.ColumnStyle);
			string[] names = Enum.GetNames(typeFromHandle);
			writer.WriteStartElement("Styles");
			foreach (string text in names)
			{
				PXGrid.ColumnStyle type = (PXGrid.ColumnStyle)Enum.Parse(typeFromHandle, text);
				PXStyle originalColumnStyle = this.GetOriginalColumnStyle(col, type);
				if (originalColumnStyle.ShouldSerialize())
				{
					writer.WriteAttributeString(text, originalColumnStyle.GetCssClass());
				}
			}
			writer.WriteEndElement();
			string[] props = new string[]
			{
				"Text",
				"ToolTip",
				"ImageUrl"
			};
			writer.WriteStartElement("Header");
			this.RenderXmlAttributes(writer, col.Header, props);
			writer.WriteEndElement();
			string[] props2 = new string[]
			{
				"Text",
				"ToolTip"
			};
			writer.WriteStartElement("Footer");
			this.RenderXmlAttributes(writer, col.Footer, props2);
			writer.WriteEndElement();
			this.RenderValueItemsXml(writer, col.ValueItems, col);
			if (col.MaskItems.Count > 0)
			{
				PXGrid.RenderMaskItemsXml(writer, col.MaskItems);
			}
			writer.WriteEndElement();
		}

		// Token: 0x0600211E RID: 8478 RVA: 0x0008ADC4 File Offset: 0x00088FC4
		private void RenderRowXml(XmlWriter writer, PXGridRow row)
		{
			writer.WriteStartElement("Row");
			writer.WriteAttributeString("i", row.Index.ToString());
			if (!row.Visible)
			{
				writer.WriteAttributeString("Visible", row.Visible.ToString());
			}
			if (row.HasNote)
			{
				writer.WriteAttributeString("HasNote", bool.TrueString);
			}
			if (row.HasFiles)
			{
				writer.WriteAttributeString("HasFiles", bool.TrueString);
			}
			if (!string.IsNullOrEmpty(row.ErrorText))
			{
				writer.WriteAttributeString("Error", row.ErrorText);
			}
			else if (!string.IsNullOrEmpty(row.WarningText))
			{
				writer.WriteAttributeString("Warning", row.WarningText);
			}
			else if (!string.IsNullOrEmpty(row.InfoText))
			{
				writer.WriteAttributeString("Info", row.InfoText);
			}
			if (!string.IsNullOrEmpty(row.EditorID))
			{
				Control control = ControlHelper.FindControl(row.EditorID, this.Page);
				if (control != null)
				{
					writer.WriteAttributeString("EditorID", control.ClientID);
				}
			}
			string cssClass = row.Style.GetCssClass();
			if (!string.IsNullOrEmpty(cssClass))
			{
				writer.WriteAttributeString("Css", cssClass);
			}
			writer.WriteStartElement("Cells");
			foreach (object obj in row.Cells)
			{
				PXGridCell cell = (PXGridCell)obj;
				this.RenderCellXml(writer, cell);
			}
			writer.WriteEndElement();
			if (row.HasChildren)
			{
				this.RenderRowsXml(writer, row.Rows, true);
			}
			writer.WriteEndElement();
		}

		// Token: 0x0600211F RID: 8479 RVA: 0x0008AF7C File Offset: 0x0008917C
		private void RenderRowsXml(XmlWriter writer, PXGridRowCollection rows, bool ignoreAdjustMode)
		{
			writer.WriteStartElement("Rows");
			writer.WriteAttributeString("Level", rows.Level.Index.ToString());
			if (this.errorState.Key != ErrorState.None)
			{
				writer.WriteAttributeString("ErrorLevel", ((int)this.errorState.Key).ToString());
				writer.WriteAttributeString("ErrorText", this.errorState.Value);
			}
			writer.WriteAttributeString("HashCode", (rows.Owner != null) ? rows.Owner.HashCode() : string.Empty);
			if (this.AdjustPageSizeFinal == GridPageSizeMode.None || this.pageAdjusted || ignoreAdjustMode)
			{
				foreach (object obj in rows)
				{
					PXGridRow row = (PXGridRow)obj;
					this.RenderRowXml(writer, row);
				}
			}
			writer.WriteEndElement();
		}

		// Token: 0x06002120 RID: 8480 RVA: 0x0008B07C File Offset: 0x0008927C
		private void RenderCellXml(XmlWriter writer, PXGridCell cell)
		{
			writer.WriteStartElement("Cell");
			string text = cell.ValueText;
			string text2 = cell.FormattedText;
			if (cell.Column.CellTemplate != null)
			{
				WebControl cellTemplateContainer = cell.Column.GetCellTemplateContainer(cell.Row);
				this.Controls.Add(cellTemplateContainer);
				cellTemplateContainer.DataBind();
				text2 = ControlHelper.RenderControl(cellTemplateContainer, true);
				text2 = text2.Replace("<", "@<").Replace(">", "@>");
				text2 = text2.Replace("@[", "<").Replace("@]", ">");
				this.Controls.Remove(cellTemplateContainer);
			}
			else
			{
				Predicate<string> match = (string df) => string.Compare(df, cell.DataField, true) == 0;
				if (!string.IsNullOrEmpty(this.fastFilter) && Array.Find<string>(this.FastFilterFields, match) != null)
				{
					text2 = this.HighlightFilterText(text2);
				}
			}
			bool flag = ((cell.DataType == TypeCode.Empty) ? cell.Column.DataType : cell.DataType) == TypeCode.String;
			if (flag && text2 != null && text2.Length > 1000000)
			{
				text2 = text2.Substring(0, 1000000);
				if (text != null && text.Length > 1000000)
				{
					text = text.Substring(0, 1000000);
				}
			}
			writer.WriteAttributeString("Value", flag ? PXGrid.CleanUpXmlString(text) : text);
			if (text != text2)
			{
				writer.WriteAttributeString("Text", flag ? PXGrid.CleanUpXmlString(text2) : text2);
			}
			if (cell.ReadOnly != null)
			{
				writer.WriteAttributeString("ReadOnly", cell.ReadOnly.ToString());
			}
			if (cell.ToolTip.Length > 0)
			{
				writer.WriteAttributeString("ToolTip", cell.FormattedToolTip);
			}
			if (!this._selectorBound && !string.IsNullOrEmpty(cell.Language))
			{
				writer.WriteAttributeString("Language", cell.Language);
			}
			if (cell.Decimals >= 0)
			{
				writer.WriteAttributeString("Decimals", cell.Decimals.ToString());
			}
			if (cell.DataType != TypeCode.Empty)
			{
				writer.WriteAttributeString("DataType", ((int)cell.DataType).ToString());
			}
			if (!string.IsNullOrEmpty(cell.ErrorText))
			{
				string value = PXGrid.EncodeLineBreak(cell.ErrorText);
				writer.WriteAttributeString(cell.IsWarning ? "Warning" : "Error", value);
			}
			if (!string.IsNullOrEmpty(cell.DisplayFormat))
			{
				writer.WriteAttributeString("DisplayFormat", cell.DisplayFormat);
				if (cell.DataType == TypeCode.DateTime)
				{
					DateTimeFormatInfo dateTimeFormat = CultureInfo.CurrentUICulture.DateTimeFormat;
					writer.WriteAttributeString("DateMask", PXDateTimeEdit.MakeEncodedPattern(cell.DisplayFormat, dateTimeFormat));
				}
			}
			if (!string.IsNullOrEmpty(cell.ViewName))
			{
				writer.WriteAttributeString("ViewName", cell.ViewName);
			}
			if (!string.IsNullOrEmpty(cell.TextField))
			{
				writer.WriteAttributeString("TextField", cell.TextField);
			}
			if (!string.IsNullOrEmpty(cell.ValueField))
			{
				writer.WriteAttributeString("ValueField", cell.ValueField);
			}
			if (cell.IsPassword != null)
			{
				writer.WriteAttributeString("IsPassword", cell.IsPassword.ToString());
			}
			if (cell.TimeMode != null)
			{
				writer.WriteAttributeString("TimeMode", cell.TimeMode.ToString());
			}
			if (cell.Column.Type == GridColumnType.HyperLink)
			{
				string[] array = this.SplitLinkURL(text);
				writer.WriteAttributeString("Href", this.AddVirtualDirToUrl(array[0]));
				if (array[1].Length > 0)
				{
					writer.WriteAttributeString("Target", array[1]);
				}
				if (array[2].Length > 0)
				{
					writer.WriteAttributeString("HrefText", array[2]);
				}
			}
			else if (cell.Column.Type == GridColumnType.Icon && !cell.Column.ValueItems.HasItems())
			{
				string value2 = PXStyle.ResolveImageUrlExt(this, text2);
				writer.WriteAttributeString("ImageUrl", value2);
			}
			string cssClass = cell.Style.GetCssClass();
			if (!string.IsNullOrEmpty(cssClass))
			{
				writer.WriteAttributeString("Css", cssClass);
			}
			this.RenderValueItemsXml(writer, cell.ValueItems, cell.Column);
			if (cell.MaskItems.Count > 0)
			{
				PXGrid.RenderMaskItemsXml(writer, cell.MaskItems);
			}
			writer.WriteEndElement();
		}

		// Token: 0x06002121 RID: 8481 RVA: 0x0008B5C4 File Offset: 0x000897C4
		private string AddVirtualDirToUrl(string url)
		{
			string appDomainAppVirtualPath = PXUrl.GetAppDomainAppVirtualPath();
			if (string.IsNullOrEmpty(appDomainAppVirtualPath) || appDomainAppVirtualPath.Equals("/") || !url.StartsWith("/"))
			{
				return url;
			}
			if (!string.IsNullOrEmpty(url) && !url.StartsWith(appDomainAppVirtualPath))
			{
				return PXUrl.ToAbsoluteUrl("~" + url, true);
			}
			return url;
		}

		// Token: 0x06002122 RID: 8482 RVA: 0x0008B61F File Offset: 0x0008981F
		private static string EncodeLineBreak(string text)
		{
			return text.Replace("\r\n", "%0A");
		}

		// Token: 0x06002123 RID: 8483 RVA: 0x0008B634 File Offset: 0x00089834
		private void RenderValueItemsXml(XmlWriter writer, PXValueItems items, PXGridColumn col)
		{
			PXValueItemCollection items2 = items.Items;
			if (items2.Count == 0)
			{
				return;
			}
			writer.WriteStartElement("ValueItems");
			if (!items.Exclusive)
			{
				writer.WriteAttributeString("Exclusive", bool.FalseString);
			}
			if (items.MultiSelect)
			{
				writer.WriteAttributeString("MultiSelect", bool.TrueString);
			}
			if (items.EmptyPossible)
			{
				writer.WriteAttributeString("EmptyPossible", bool.TrueString);
			}
			for (int i = 0; i < items2.Count; i++)
			{
				writer.WriteStartElement("ValueItem");
				writer.WriteAttributeString("Value", items2[i].Value);
				if (!string.IsNullOrEmpty(items2[i].DisplayValue))
				{
					string text = items2[i].DisplayValue;
					if (col.Type == GridColumnType.Icon || (col.Type == GridColumnType.NotSet && !string.IsNullOrEmpty(items2[i].ImageUrl)))
					{
						if (!string.IsNullOrEmpty(items2[i].ImageUrl))
						{
							string value = PXStyle.ResolveImageUrlExt(this, items2[i].ImageUrl);
							if (!string.IsNullOrEmpty(value))
							{
								writer.WriteAttributeString("ImageUrl", value);
							}
						}
						else
						{
							text = PXStyle.ResolveImageUrlExt(this, text);
						}
					}
					writer.WriteAttributeString("DisplayValue", text);
				}
				if (!items2[i].Enabled)
				{
					writer.WriteAttributeString("Enabled", bool.FalseString);
				}
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}

		// Token: 0x06002124 RID: 8484 RVA: 0x0008B7A0 File Offset: 0x000899A0
		private static void RenderMaskItemsXml(XmlWriter writer, PXMaskItemCollection items)
		{
			if (items.Count == 0)
			{
				return;
			}
			writer.WriteStartElement("MaskItems");
			for (int i = 0; i < items.Count; i++)
			{
				PXMaskItem pxmaskItem = items[i];
				writer.WriteStartElement("Item");
				writer.WriteAttributeString("editMask", ((int)pxmaskItem.EditMask).ToString());
				if (pxmaskItem.Length != 3)
				{
					writer.WriteAttributeString("length", pxmaskItem.Length.ToString());
				}
				if (pxmaskItem.Title != string.Empty)
				{
					writer.WriteAttributeString("title", pxmaskItem.Title.ToString());
				}
				if (!pxmaskItem.Selectable)
				{
					writer.WriteAttributeString("selectable", pxmaskItem.Selectable.ToString());
				}
				if (pxmaskItem.ReadOnly)
				{
					writer.WriteAttributeString("readOnly", pxmaskItem.ReadOnly.ToString());
				}
				if (pxmaskItem.TextCase != TextCase.NotSet)
				{
					writer.WriteAttributeString("textCase", ((int)pxmaskItem.TextCase).ToString());
				}
				if (pxmaskItem.TextAlign != TextAlign.Left)
				{
					writer.WriteAttributeString("textAlign", ((int)pxmaskItem.TextAlign).ToString());
				}
				if (pxmaskItem.EmptyChar != ' ')
				{
					writer.WriteAttributeString("emptyChar", pxmaskItem.EmptyChar.ToString());
				}
				if (pxmaskItem.PromptChar != '_')
				{
					writer.WriteAttributeString("promptChar", pxmaskItem.PromptChar.ToString());
				}
				if (pxmaskItem.Separator != '.')
				{
					writer.WriteAttributeString("separator", pxmaskItem.Separator.ToString());
				}
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}

		// Token: 0x06002125 RID: 8485 RVA: 0x0008B94C File Offset: 0x00089B4C
		internal string HighlightFilterText(string text)
		{
			if (!string.IsNullOrEmpty(this.fastFilter))
			{
				string text2 = this.fastFilter.Replace(' ', '|');
				text2 = text2.Replace("+", "\\+").Replace("?", "\\?").Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)").Replace("[", "\\[").Replace("]", "\\]").Replace("{", "\\{").Replace("}", "\\}");
				text2 = text2.Replace(this.FastFilterWildcard, ".*");
				if (this.FastFilterCondition == PXCondition.RLIKE)
				{
					text2 = "^" + text2;
				}
				Regex regex = new Regex(text2, RegexOptions.IgnoreCase | RegexOptions.Multiline);
				MatchEvaluator evaluator = (Match m) => string.Format("@<span class=\"searchTextMark\"@>{0}@</span@>", m.ToString());
				text = regex.Replace(text, evaluator);
			}
			return text;
		}

		// Token: 0x06002126 RID: 8486 RVA: 0x0008BA5C File Offset: 0x00089C5C
		private static string CleanUpXmlString(string val)
		{
			if (string.IsNullOrEmpty(val))
			{
				return val;
			}
			StringBuilder stringBuilder = new StringBuilder(val.Length);
			foreach (char c in val)
			{
				if ((c == '\t' || c == '\n' || c == '\r' || c > '\u001f') && XmlConvert.IsXmlChar(c))
				{
					stringBuilder.Append(c);
				}
			}
			return stringBuilder.ToString();
		}

		// Token: 0x06002127 RID: 8487 RVA: 0x0008BAC4 File Offset: 0x00089CC4
		private void RenderXmlAttributes(XmlWriter writer, object container, string[] props)
		{
			PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(container);
			foreach (string text in props)
			{
				PropertyDescriptor propertyDescriptor = properties.Find(text, false);
				if (propertyDescriptor != null)
				{
					object value = propertyDescriptor.GetValue(container);
					if (value != null && value.ToString() != string.Empty)
					{
						writer.WriteAttributeString(text, value.ToString());
					}
				}
			}
		}

		// Token: 0x06002128 RID: 8488 RVA: 0x0008BB2C File Offset: 0x00089D2C
		private void GenerateColumns()
		{
			if (!(this.GetDataSource() is PXDataSource) || this.AutoGenerateColumns == ColumnGeneration.None)
			{
				return;
			}
			foreach (object obj in this.Levels)
			{
				PXGridLevel pxgridLevel = (PXGridLevel)obj;
				PXGridColumnCollection columns = pxgridLevel.Columns;
				Dictionary<string, PXGridColumn> dictionary = new Dictionary<string, PXGridColumn>();
				Dictionary<string, PXGridColumn> dictionary2 = new Dictionary<string, PXGridColumn>();
				Dictionary<string, SortDirection> dictionary3 = new Dictionary<string, SortDirection>();
				List<string> list = new List<string>();
				if (this.AutoGenerateColumns == ColumnGeneration.Append || this.AutoGenerateColumns == ColumnGeneration.AppendDynamic)
				{
					foreach (object obj2 in columns)
					{
						PXGridColumn pxgridColumn = (PXGridColumn)obj2;
						if (pxgridColumn.Generated)
						{
							dictionary2.Add(pxgridColumn.DataField, pxgridColumn);
							if (pxgridColumn.SortDirection != SortDirection.None)
							{
								dictionary3.Add(pxgridColumn.DataField, pxgridColumn.SortDirection);
							}
							if (pxgridColumn.FilterPosted)
							{
								list.Add(pxgridColumn.DataField);
							}
						}
						else
						{
							dictionary[pxgridColumn.DataField] = pxgridColumn;
						}
					}
					using (List<PXGridColumn>.Enumerator enumerator3 = dictionary2.Values.ToList<PXGridColumn>().GetEnumerator())
					{
						while (enumerator3.MoveNext())
						{
							PXGridColumn item = enumerator3.Current;
							columns.Remove(item);
						}
						goto IL_14B;
					}
					goto IL_140;
				}
				goto IL_140;
				IL_14B:
				PXDataSourceView pxdataSourceView = pxgridLevel.GetData() as PXDataSourceView;
				PXFieldState[] array = null;
				try
				{
					array = pxdataSourceView.GetSchema().GetFields();
				}
				catch (PXViewDoesNotExistException)
				{
					if (this.AutoGenerateColumns == ColumnGeneration.Append || this.AutoGenerateColumns == ColumnGeneration.AppendDynamic)
					{
						break;
					}
					throw;
				}
				catch (Exception)
				{
					throw;
				}
				foreach (PXFieldState pxfieldState in array)
				{
					if (pxfieldState != null && (((pxfieldState.Visibility & PXUIVisibility.Visible) == PXUIVisibility.Visible && this.AutoGenerateColumns != ColumnGeneration.AppendDynamic) || ((pxfieldState.Visibility & PXUIVisibility.Dynamic) == PXUIVisibility.Dynamic && this.AutoGenerateColumns == ColumnGeneration.AppendDynamic)) && Type.GetTypeCode(pxfieldState.DataType) != TypeCode.Object && !dictionary.ContainsKey(pxfieldState.Name))
					{
						PXGridColumn pxgridColumn2 = new PXGridColumn();
						pxgridColumn2.Generated = true;
						pxgridColumn2.Header.Text = pxfieldState.DisplayName;
						PXGrid.SetColumnProperties(pxgridColumn2, pxfieldState);
						if (dictionary3.ContainsKey(pxgridColumn2.DataField))
						{
							pxgridColumn2.SortDirection = dictionary3[pxgridColumn2.DataField];
						}
						if (list.Contains(pxgridColumn2.DataField))
						{
							pxgridColumn2.FilterPosted = true;
						}
						if ((pxfieldState.Visibility & PXUIVisibility.Dynamic) == PXUIVisibility.Dynamic)
						{
							if (!string.IsNullOrEmpty(pxfieldState.ViewName))
							{
								pxgridColumn2.DisplayMode = ValueDisplayMode.Text;
							}
							pxgridColumn2.SyncText = false;
							pxgridColumn2.Width = PXGrid.DefaultColWidth(TypeCode.String, pxfieldState.DisplayName.Length);
							int num = pxfieldState.Name.IndexOf("__");
							bool flag = false;
							if (num != -1)
							{
								string b = pxfieldState.Name.Substring(0, num);
								for (int j = columns.Count - 1; j >= 0; j--)
								{
									if ((num = columns[j].DataField.IndexOf("__")) != -1 && columns[j].DataField.Substring(0, num) == b)
									{
										columns.Insert(j + 1, pxgridColumn2);
										flag = true;
										break;
									}
								}
							}
							else
							{
								for (int k = columns.Count - 1; k >= 0; k--)
								{
									if (!columns[k].DataField.Contains("__"))
									{
										columns.Insert(k + 1, pxgridColumn2);
										flag = true;
										break;
									}
								}
							}
							if (!flag)
							{
								columns.Add(pxgridColumn2);
							}
						}
						else
						{
							columns.Add(pxgridColumn2);
						}
						if (dictionary2.ContainsKey(pxgridColumn2.DataField))
						{
							PXGridColumn pxgridColumn3 = dictionary2[pxgridColumn2.DataField];
							if (pxgridColumn3.VisiblePosted || pxgridColumn2.VisibleLoaded)
							{
								pxgridColumn2.Visible = pxgridColumn3.Visible;
							}
							pxgridColumn2.Width = pxgridColumn3.Width;
						}
					}
				}
				continue;
				IL_140:
				pxgridLevel.Columns.Clear();
				goto IL_14B;
			}
		}

		// Token: 0x06002129 RID: 8489 RVA: 0x0008BFD0 File Offset: 0x0008A1D0
		private void CreateSystemColumns()
		{
			this.CalculateNoteFileIndicators();
			foreach (object obj in this.Levels)
			{
				PXGridLevel pxgridLevel = (PXGridLevel)obj;
				PXDataSourceView pxdataSourceView = pxgridLevel.GetData() as PXDataSourceView;
				List<string> list = new List<string>(pxgridLevel.DataKeyNames);
				if (pxgridLevel.Index == 0 && !string.IsNullOrEmpty(this.StatusField))
				{
					list.Add(this.StatusField);
				}
				string autoInsertField = pxgridLevel.Mode.AutoInsertField;
				if (pxgridLevel.Index == 0 && string.IsNullOrEmpty(autoInsertField))
				{
					autoInsertField = this.Mode.AutoInsertField;
				}
				if (!string.IsNullOrEmpty(autoInsertField))
				{
					list.Add(autoInsertField);
				}
				using (IEnumerator enumerator2 = pxgridLevel.Columns.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						PXGridColumn col = (PXGridColumn)enumerator2.Current;
						if (col.DataField.Length > 0)
						{
							int num = list.FindIndex((string item) => string.Compare(col.DataField, item, true) == 0);
							if (num > -1)
							{
								list.RemoveAt(num);
								if (list.Count == 0)
								{
									break;
								}
							}
						}
					}
				}
				if (list.Count > 0)
				{
					foreach (string text in list)
					{
						PXGridColumn pxgridColumn = new PXGridColumn();
						pxgridColumn.DataField = text;
						pxgridColumn.Width = 70;
						pxgridColumn.Visible = false;
						pxgridColumn.AllowShowHide = AllowShowHide.False;
						if (pxdataSourceView != null)
						{
							PXFieldState pxfieldState = pxdataSourceView.GetValueExt(null, text) as PXFieldState;
							if (pxfieldState != null)
							{
								pxgridColumn.DataType = Type.GetTypeCode(pxfieldState.DataType);
								pxgridColumn.AllowNull = pxfieldState.Nullable;
								pxgridColumn.DefaultValue = pxfieldState.DefaultValue;
							}
						}
						pxgridLevel.Columns.Add(pxgridColumn);
					}
				}
				bool flag = HttpContext.Current == null && !PXGraph.ProxyIsActive;
				if (pxgridLevel.Index == 0 && !flag && pxdataSourceView != null)
				{
					PXGridColumn pxgridColumn2 = pxgridLevel.Columns["Notes"];
					if (this.NoteIndicator.GetValueOrDefault())
					{
						if (pxgridColumn2 == null)
						{
							PXGridColumn pxgridColumn3 = this.CreateImageColumn(pxgridLevel, "Notes", pxgridLevel.ImagesFinal.RowNoteEmpty, pxgridLevel.ImagesFinal.RowNote, Sprite.Control.GetFullUrl("RowNoteEmpty"));
							pxgridColumn3.Header.ToolTip = Msg.GetLocal("Notes");
							pxgridColumn3.ForceExport = true;
						}
					}
					else if (pxgridColumn2 != null)
					{
						pxgridLevel.Columns.Remove(pxgridColumn2);
					}
					pxgridColumn2 = pxgridLevel.Columns["Files"];
					if (this.FilesIndicator.GetValueOrDefault())
					{
						if (pxgridColumn2 == null)
						{
							PXGridColumn pxgridColumn4 = this.CreateImageColumn(pxgridLevel, "Files", pxgridLevel.ImagesFinal.RowFileEmpty, pxgridLevel.ImagesFinal.RowFile, Sprite.Control.GetFullUrl("RowFileEmpty"));
							pxgridColumn4.Header.ToolTip = Msg.GetLocal("Files");
							pxgridColumn4.ForceExport = true;
						}
					}
					else if (pxgridColumn2 != null)
					{
						pxgridLevel.Columns.Remove(pxgridColumn2);
					}
				}
			}
		}

		// Token: 0x0600212A RID: 8490 RVA: 0x0008C35C File Offset: 0x0008A55C
		private PXGridColumn CreateImageColumn(PXGridLevel level, string key, string imageEmpty, string image, string headerImage)
		{
			PXGridColumn pxgridColumn = new PXGridColumn();
			pxgridColumn.Key = key;
			pxgridColumn.Type = GridColumnType.Icon;
			pxgridColumn.DataType = TypeCode.Int32;
			pxgridColumn.TextAlign = HorizontalAlign.Center;
			pxgridColumn.Header.ImageUrl = headerImage;
			pxgridColumn.Width = 25;
			pxgridColumn.AllowFocus = false;
			pxgridColumn.AllowNull = false;
			pxgridColumn.AllowResize = new bool?(false);
			pxgridColumn.DefaultValue = 0;
			pxgridColumn.ValueItems.Items.Add(new PXValueItem("0", imageEmpty));
			pxgridColumn.ValueItems.Items.Add(new PXValueItem("1", image));
			level.Columns.Insert(0, pxgridColumn);
			return pxgridColumn;
		}

		// Token: 0x0600212B RID: 8491 RVA: 0x0008C414 File Offset: 0x0008A614
		internal void SynchronizeColsState(DataSourceView view)
		{
			if (!(view is PXDataSourceView))
			{
				return;
			}
			this.OnBeforeSyncState(EventArgs.Empty);
			bool flag = PXGraph.ProxyIsActive || PXGraph.GeneratorIsActive;
			PXDataSourceView pxdataSourceView = (PXDataSourceView)view;
			for (int i = 0; i < this.Columns.Count; i++)
			{
				PXGridColumn col = this.Columns[i];
				if (col.DataField.Length != 0)
				{
					if (string.IsNullOrEmpty(col.LinkCommand))
					{
						col.LinkCommand = pxdataSourceView.GetLinkCommand(col.DataField);
					}
					PXFieldState pxfieldState = pxdataSourceView.GetValueExt(null, col.DataField) as PXFieldState;
					if (pxfieldState != null)
					{
						if (pxfieldState.Enabled)
						{
							(pxfieldState as PXStringState).With((PXStringState _) => col.Language = _.Language);
						}
						PXPageRipper.TryToProcessAspxField(col.DataField, pxfieldState, pxdataSourceView.DataGraph, pxdataSourceView._ViewName);
						PxFieldStateProcessor.OnBeforeSyncState(pxdataSourceView, null, col, pxfieldState);
						if (col.SyncNullable)
						{
							col.AllowNull = (pxfieldState.Nullable || !string.IsNullOrEmpty(col.NullTextFinal));
						}
						col.AllowUpdate = (pxdataSourceView.CanUpdate && !pxfieldState.IsReadOnly);
						if (col.SyncText)
						{
							col.Header.Text = pxfieldState.DisplayName;
						}
						col.DataType = Type.GetTypeCode(pxfieldState.DataType);
						if (col.HasDefaultWidth)
						{
							PXGrid.CalculateColumnWidth(col, pxfieldState);
						}
						if (pxfieldState.DefaultValue != null)
						{
							col.DefaultValue = pxfieldState.DefaultValue;
						}
						if (pxfieldState.Length > 0)
						{
							col.MaxLength = pxfieldState.Length;
						}
						if (pxfieldState.Precision > 0)
						{
							col.Decimals = pxfieldState.Precision;
						}
						if (pxfieldState.Required != null)
						{
							col.Required = (pxfieldState.Required.Value && !pxfieldState.IsReadOnly);
						}
						col.ViewName = pxfieldState.ViewName;
						bool flag2 = col.MaxLength >= 60 && col.DataType == TypeCode.String;
						if (!string.IsNullOrEmpty(pxfieldState.ValueField))
						{
							col.ValueField = pxfieldState.ValueField;
						}
						if ((pxfieldState.SelectorMode & PXSelectorMode.Undefined) != PXSelectorMode.Undefined)
						{
							col.DisplayMode = ValueDisplayMode.Hint;
						}
						else if ((pxfieldState.SelectorMode & PXSelectorMode.DisplayModeText) != PXSelectorMode.Undefined)
						{
							col.DisplayMode = ValueDisplayMode.Text;
						}
						else if ((pxfieldState.SelectorMode & PXSelectorMode.DisplayModeValue) != PXSelectorMode.Undefined)
						{
							col.DisplayMode = ValueDisplayMode.Value;
						}
						if (col.DisplayMode != ValueDisplayMode.Value && !string.IsNullOrEmpty(pxfieldState.DescriptionName))
						{
							col.TextField = pxfieldState.DescriptionName;
						}
						bool flag3 = col.VisibleLoaded || col.VisiblePosted;
						bool flag4;
						if (!flag)
						{
							flag4 = ((pxfieldState.Visibility & (PXUIVisibility)11) == PXUIVisibility.Invisible && !pxfieldState.Visible);
						}
						else
						{
							flag4 = (pxfieldState.Visibility == PXUIVisibility.Invisible && !pxfieldState.Visible);
						}
						bool flag5 = col.AllowShowHide == AllowShowHide.Server || (col.AllowShowHide == AllowShowHide.True && (!flag3 || flag4));
						if (flag5 && col.SyncVisible)
						{
							col.Visible = pxfieldState.Visible;
							if (this.isDataBound)
							{
								col.VisibleSynchronized = true;
							}
						}
						if (col.SyncVisibility && flag4)
						{
							col.AllowShowHide = AllowShowHide.False;
						}
						PXStringState pxstringState = pxfieldState as PXStringState;
						if (pxstringState != null && pxstringState.InputMask != null)
						{
							col.DisplayFormat = pxstringState.InputMask;
						}
						PXDateState pxdateState = pxfieldState as PXDateState;
						if (pxdateState != null && !string.IsNullOrEmpty(pxdateState.InputMask))
						{
							col.DisplayFormat = pxdateState.InputMask;
							if ((pxdateState.InputMask.Length > 1 && pxdateState.InputMask.IndexOfAny(new char[]
							{
								'd',
								'M',
								'y'
							}) < 0) || pxdateState.InputMask == "t")
							{
								col.TimeMode = true;
							}
						}
						if (string.IsNullOrEmpty(col.DisplayFormat) || col.FormatGenerated)
						{
							TypeCode dataType = col.DataType;
							if (dataType - TypeCode.Single <= 2)
							{
								col.DisplayFormat = ((pxfieldState.Precision >= 0) ? ("n" + pxfieldState.Precision.ToString()) : "n");
								col.FormatGenerated = true;
							}
						}
						PXImageState pximageState = pxfieldState as PXImageState;
						PXHeaderImageState pxheaderImageState = pxfieldState as PXHeaderImageState;
						PXIntState pxintState = pxfieldState as PXIntState;
						bool flag6 = pxintState != null && pxintState.AllowedValues != null;
						bool flag7 = !string.IsNullOrEmpty(col.FormEditorID) && col.Level.NeedRenderForm;
						if (!flag7)
						{
							PXSegmentedState pxsegmentedState = pxfieldState as PXSegmentedState;
							if (pxsegmentedState != null && pxsegmentedState.Segments != null && !pxsegmentedState.ValidCombos)
							{
								PXSegmentMask.SyncMaskItems(col.MaskItems, pxsegmentedState);
							}
						}
						if (pxstringState != null && pxstringState.AllowedValues != null)
						{
							if (!flag7)
							{
								col.Type = GridColumnType.DropDownList;
							}
							col.ValueItems.Items.Clear();
							col.ValueItems.Exclusive = pxstringState.ExclusiveValues;
							col.ValueItems.MultiSelect = pxstringState.MultiSelect;
							col.ValueItems.EmptyPossible = (pxstringState.EmptyPossible && col.AllowNull);
							for (int j = 0; j < pxstringState.AllowedValues.Length; j++)
							{
								string text = pxstringState.AllowedValues[j];
								if (text != null)
								{
									text = text.TrimEnd(Array.Empty<char>());
								}
								PXValueItem pxvalueItem = new PXValueItem(text, pxstringState.AllowedLabels[j]);
								if (pxstringState.AllowedImages != null && j < pxstringState.AllowedImages.Length)
								{
									pxvalueItem.ImageUrl = pxstringState.AllowedImages[j];
									if (!string.IsNullOrEmpty(pxvalueItem.ImageUrl))
									{
										col.Type = GridColumnType.Icon;
									}
								}
								col.ValueItems.Items.Add(pxvalueItem);
							}
						}
						else if (flag6)
						{
							if (!flag7)
							{
								col.Type = GridColumnType.DropDownList;
							}
							col.ValueItems.Items.Clear();
							col.ValueItems.Exclusive = pxintState.ExclusiveValues;
							col.ValueItems.EmptyPossible = (pxintState.EmptyPossible && col.AllowNull);
							for (int k = 0; k < pxintState.AllowedValues.Length; k++)
							{
								string value = pxintState.AllowedValues[k].ToString(CultureInfo.InvariantCulture);
								col.ValueItems.Items.Add(new PXValueItem(value, pxintState.AllowedLabels[k]));
							}
						}
						if (col.TextAlign == HorizontalAlign.NotSet && TypeHelper.IsNumeric(col.DataType) && !flag6)
						{
							col.TextAlign = HorizontalAlign.Right;
						}
						if (col.DisplayMode == ValueDisplayMode.Text && col.TextAlign == HorizontalAlign.Right)
						{
							col.TextAlign = HorizontalAlign.Left;
						}
						if (pximageState != null)
						{
							col.Type = GridColumnType.Icon;
							if (!string.IsNullOrEmpty(pximageState.HeaderImage))
							{
								col.Header.ImageUrl = pximageState.HeaderImage;
								if (col.SyncText)
								{
									col.Header.ToolTip = pximageState.DisplayName;
								}
								col.Header.Text = "";
							}
							else if (string.IsNullOrEmpty(col.Header.Text))
							{
								col.Header.ImageUrl = Sprite.Control.GetFullUrl("Empty");
							}
							col.TextAlign = HorizontalAlign.Center;
						}
						if (pxheaderImageState != null)
						{
							if (!string.IsNullOrEmpty(pxheaderImageState.HeaderImage))
							{
								col.Header.ImageUrl = pxheaderImageState.HeaderImage;
								col.Header.ToolTip = pxheaderImageState.DisplayName;
								col.Header.Text = "";
							}
							else if (string.IsNullOrEmpty(col.Header.Text))
							{
								col.Header.ImageUrl = Sprite.Control.GetFullUrl("Empty");
							}
						}
						if (flag7)
						{
							IFieldEditor fieldEditor = col.Level.GetFieldEditor(col.DataField);
							if (fieldEditor != null)
							{
								PxFieldStateProcessor.OnBeforeSyncState(pxdataSourceView, null, fieldEditor, pxfieldState);
								PXTextEdit pxtextEdit = fieldEditor as PXTextEdit;
								if (pxtextEdit != null && flag2 && pxtextEdit.TextMode == TextBoxMode.SingleLine)
								{
									pxtextEdit.TextMode = TextBoxMode.MultiLine;
								}
								PXSelectorBase pxselectorBase = fieldEditor as PXSelectorBase;
								if (pxselectorBase != null && col.DisplayMode != ValueDisplayMode.Value)
								{
									pxselectorBase.DisplayMode = col.DisplayMode;
								}
								fieldEditor.ReadOnly = pxfieldState.IsReadOnly;
								if (flag5)
								{
									fieldEditor.Hidden = !pxfieldState.Visible;
								}
								fieldEditor.SynchronizeState(pxfieldState);
								if (!fieldEditor.ValuePosted)
								{
									fieldEditor.Value = pxfieldState.Value;
								}
								PXSegmentMask pxsegmentMask = fieldEditor as PXSegmentMask;
								if (pxsegmentMask != null && !string.IsNullOrEmpty(pxsegmentMask.Wildcard) && col.DataType == TypeCode.String && !string.IsNullOrEmpty(col.DisplayFormat))
								{
									PXGridColumn col2 = col;
									col2.DisplayFormat = col2.DisplayFormat + "||" + pxsegmentMask.Wildcard;
								}
							}
						}
					}
				}
			}
			this.OnAfterSyncState(EventArgs.Empty);
		}

		// Token: 0x0600212C RID: 8492 RVA: 0x0008CE74 File Offset: 0x0008B074
		private void SynchronizeColsStateExt()
		{
			this.SynchronizeColsState(this.GetDataView());
			this.columnsSynchronized = true;
			if (!this.layoutLoaded)
			{
				PXGridColumn pxgridColumn = this.Columns["Notes"];
				PXGridColumn pxgridColumn2 = this.Columns["Files"];
				if (pxgridColumn != null || pxgridColumn2 != null)
				{
					int num = (pxgridColumn != null) ? pxgridColumn.Index : -1;
					int num2 = (pxgridColumn2 != null) ? pxgridColumn2.Index : -1;
					int num3 = Math.Max(num, num2);
					if (num3 == 0 || (num3 == 1 && pxgridColumn != null && pxgridColumn2 != null))
					{
						int i;
						num3 = (i = num3 + 1);
						while (i < this.Columns.Count)
						{
							PXGridColumn pxgridColumn3 = this.Columns[i];
							if (pxgridColumn3.Visible || pxgridColumn3.SyncVisible)
							{
								break;
							}
							i++;
						}
						if (i > num3)
						{
							if (pxgridColumn != null)
							{
								this.Columns.RemoveAt(num);
								i--;
							}
							if (pxgridColumn2 != null)
							{
								this.Columns.RemoveAt(num2);
								i--;
							}
							if (pxgridColumn2 != null)
							{
								this.Columns.Insert(i++, pxgridColumn2);
							}
							if (pxgridColumn != null)
							{
								this.Columns.Insert(i, pxgridColumn);
							}
						}
					}
				}
			}
		}

		// Token: 0x17000B54 RID: 2900
		// (get) Token: 0x0600212D RID: 8493 RVA: 0x0008CF9D File Offset: 0x0008B19D
		// (set) Token: 0x0600212E RID: 8494 RVA: 0x0008CFAF File Offset: 0x0008B1AF
		internal bool TemplateEditMode
		{
			get
			{
				return base.DesignMode && this.templateEditMode;
			}
			set
			{
				if (base.DesignMode)
				{
					this.templateEditMode = value;
				}
			}
		}

		// Token: 0x17000B55 RID: 2901
		// (get) Token: 0x0600212F RID: 8495 RVA: 0x0008CFC0 File Offset: 0x0008B1C0
		// (set) Token: 0x06002130 RID: 8496 RVA: 0x0008CFD2 File Offset: 0x0008B1D2
		internal int TemplateLevel
		{
			get
			{
				if (base.DesignMode)
				{
					return this.templateLevel;
				}
				return 0;
			}
			set
			{
				this.templateLevel = value;
			}
		}

		// Token: 0x06002131 RID: 8497 RVA: 0x0008CFDC File Offset: 0x0008B1DC
		private TableRow CreateNoDataRow()
		{
			TableRow tableRow = new TableRow();
			PXTableCell pxtableCell = new PXTableCell();
			pxtableCell.VerticalAlign = VerticalAlign.Middle;
			pxtableCell.HorizontalAlign = HorizontalAlign.Center;
			pxtableCell.Controls.Add(new LiteralControl(this.NoDataText));
			tableRow.Controls.Add(pxtableCell);
			return tableRow;
		}

		// Token: 0x06002132 RID: 8498 RVA: 0x0008D024 File Offset: 0x0008B224
		internal void NotyfyOnError(PXErrorLevel level, string errorText)
		{
			ErrorState errorState = ErrorState.None;
			switch (level)
			{
			case PXErrorLevel.RowInfo:
				errorState = ErrorState.Info;
				break;
			case PXErrorLevel.Warning:
			case PXErrorLevel.RowWarning:
				errorState = ErrorState.Warning;
				break;
			case PXErrorLevel.Error:
			case PXErrorLevel.RowError:
				errorState = ErrorState.Error;
				break;
			}
			if (errorState > this.errorState.Key)
			{
				this.errorState = new KeyValuePair<ErrorState, string>(errorState, errorText);
			}
		}

		// Token: 0x06002133 RID: 8499 RVA: 0x0008D078 File Offset: 0x0008B278
		internal void SetRenderState(bool load)
		{
			this.EnsureChildControls();
			foreach (PXActionBar pxactionBar in new PXActionBar[]
			{
				this.toolsTop,
				this.toolsBottom
			})
			{
				if (pxactionBar != null)
				{
					ControlHelper.SetRenderState(pxactionBar, load, (ScriptRegisterFlag)6);
					pxactionBar.SetRenderState(load);
				}
			}
			if (this.tlbFilterTools != null)
			{
				ControlHelper.SetRenderState(this.tlbFilterTools, load, (ScriptRegisterFlag)6);
			}
			if (this.tlbFilters != null)
			{
				ControlHelper.SetRenderState(this.tlbFilters, load, (ScriptRegisterFlag)6);
			}
			if (this.filterSelector != null)
			{
				ControlHelper.SetRenderState(this.filterSelector, load, (ScriptRegisterFlag)6);
			}
			if (this.filterBar != null)
			{
				ControlHelper.SetRenderState(this.filterBar, load, (ScriptRegisterFlag)6);
			}
			foreach (PXMenu control in this.menuControls)
			{
				ControlHelper.SetRenderState(control, load, (ScriptRegisterFlag)6);
			}
			if (this.filterEditor != null)
			{
				PXFilterEditor pxfilterEditor = this.filterEditor;
				((IPXScriptControl)pxfilterEditor).RegisterFlags = ScriptRegisterFlag.Modules;
				((IPXDynamicControl)pxfilterEditor).SetRenderState(false);
			}
			if (this.importPanel != null)
			{
				this.importPanel.SetRenderState(load);
			}
			if (this.filterSavePanel != null)
			{
				((IPXDynamicControl)this.filterSavePanel).SetRenderState(load);
			}
			ScriptRegisterFlag loadFlags = (ScriptRegisterFlag)30;
			foreach (object obj in this.Levels)
			{
				PXGridLevel pxgridLevel = (PXGridLevel)obj;
				if (pxgridLevel.FormOkButton != null)
				{
					ControlHelper.SetRenderState(pxgridLevel.FormOkButton, load);
				}
				if (pxgridLevel.FormCancelButton != null)
				{
					ControlHelper.SetRenderState(pxgridLevel.FormCancelButton, load);
				}
				if (pxgridLevel.FormView != null)
				{
					ControlHelper.SetRenderState(pxgridLevel.FormView, load, loadFlags);
				}
				foreach (PXButton control2 in pxgridLevel.FormButtons)
				{
					ControlHelper.SetRenderState(control2, load, (ScriptRegisterFlag)6);
				}
				foreach (KeyValuePair<string, WebControl> keyValuePair in pxgridLevel.TemplateEditors)
				{
					IPXDynamicControl ipxdynamicControl = keyValuePair.Value as IPXDynamicControl;
					ControlHelper.SetRenderState(keyValuePair.Value, load);
					if (ipxdynamicControl != null)
					{
						ipxdynamicControl.SetRenderState(load && !ipxdynamicControl.LoadOnDemand);
					}
				}
			}
			if (this.pivotTable != null)
			{
				ControlHelper.SetRenderState(this.pivotTable, load, loadFlags);
			}
			this.renderState = load;
		}

		// Token: 0x06002134 RID: 8500 RVA: 0x0008D340 File Offset: 0x0008B540
		void IPXDynamicControl.SetRenderState(bool load)
		{
			this.SetRenderState(load);
		}

		// Token: 0x17000B56 RID: 2902
		// (get) Token: 0x06002135 RID: 8501 RVA: 0x0008D349 File Offset: 0x0008B549
		bool IPXDynamicControl.LoadOnDemand
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000B57 RID: 2903
		// (get) Token: 0x06002136 RID: 8502 RVA: 0x0008D34C File Offset: 0x0008B54C
		// (set) Token: 0x06002137 RID: 8503 RVA: 0x0008D354 File Offset: 0x0008B554
		internal bool IsColumnsGenerated { get; private set; }

		// Token: 0x0400080B RID: 2059
		public const string CommandPageNext = "PageNext";

		// Token: 0x0400080C RID: 2060
		public const string CommandPagePrev = "PagePrev";

		// Token: 0x0400080D RID: 2061
		public const string CommandPageFirst = "PageFirst";

		// Token: 0x0400080E RID: 2062
		public const string CommandPageLast = "PageLast";

		// Token: 0x0400080F RID: 2063
		public const string CommandRefresh = "Refresh";

		// Token: 0x04000810 RID: 2064
		public const string CommandSave = "Save";

		// Token: 0x04000811 RID: 2065
		public const string CommandAddNew = "AddNew";

		// Token: 0x04000812 RID: 2066
		public const string CommandDelete = "Delete";

		// Token: 0x04000813 RID: 2067
		public const string CommandSearch = "Search";

		// Token: 0x04000814 RID: 2068
		public const string CommandAdjustCols = "AdjustColumns";

		// Token: 0x04000815 RID: 2069
		public const string CommandEditRecord = "EditRecord";

		// Token: 0x04000816 RID: 2070
		public const string CommandNoteShow = "NoteShow";

		// Token: 0x04000817 RID: 2071
		public const string CommandFilterShow = "FilterShow";

		// Token: 0x04000818 RID: 2072
		public const string CommandFilterSet = "FilterSet";

		// Token: 0x04000819 RID: 2073
		public const string CommandFilterBar = "FilterBar";

		// Token: 0x0400081A RID: 2074
		public const string CommandExportExcel = "ExportExcel";

		// Token: 0x0400081B RID: 2075
		public const string CommandFilesMenu = "FilesMenu";

		// Token: 0x0400081C RID: 2076
		public const string CommandLayoutSave = "LayoutSave";

		// Token: 0x0400081D RID: 2077
		public const string CommandLayoutReset = "LayoutReset";

		// Token: 0x0400081E RID: 2078
		public const string CommandUpload = "Upload";

		// Token: 0x0400081F RID: 2079
		public const string CommandDeleteDefault = "DelDefault";

		// Token: 0x04000820 RID: 2080
		public const string CommandCreateProviderFile = "CreateProviderFile";

		// Token: 0x04000821 RID: 2081
		public const string CommandEditPivot = "EditPivot";

		// Token: 0x04000822 RID: 2082
		public const string CallbackRefresh = "Refresh";

		// Token: 0x04000823 RID: 2083
		public const string CallbackSave = "Save";

		// Token: 0x04000824 RID: 2084
		public const string CallbackFetchRow = "FetchRow";

		// Token: 0x04000825 RID: 2085
		public const string CallbackInitRow = "InitRow";

		// Token: 0x04000826 RID: 2086
		public const string CallbackNoteShow = "NoteShow";

		// Token: 0x04000827 RID: 2087
		public const string CallbackNoteSave = "NoteSave";

		// Token: 0x04000828 RID: 2088
		public const string CallbackFilterShow = "FilterShow";

		// Token: 0x04000829 RID: 2089
		public const string CallbackExportExcel = "ExportExcel";

		// Token: 0x0400082A RID: 2090
		public const string CallbackFilesMenu = "FilesMenu";

		// Token: 0x0400082B RID: 2091
		public const string CallbackCheckFileSize = "checkFileSize";

		// Token: 0x0400082C RID: 2092
		public const string CallbackLayoutSave = "LayoutSave";

		// Token: 0x0400082D RID: 2093
		public const string CallbackLayoutReset = "LayoutReset";

		// Token: 0x0400082E RID: 2094
		public const string CallbackNavigate = "Navigate";

		// Token: 0x0400082F RID: 2095
		public const string CallbackFilterDialog = "FilterDialog";

		// Token: 0x04000830 RID: 2096
		public const string CallbackColumnsDialog = "ColumnsDialog";

		// Token: 0x04000831 RID: 2097
		public const string CallbackFilterSave = "FilterSave";

		// Token: 0x04000832 RID: 2098
		public const string CallbackFilterSavePivot = "FilterSavePivot";

		// Token: 0x04000833 RID: 2099
		public const string CallbackFilterRemove = "FilterRemove";

		// Token: 0x04000835 RID: 2101
		private EmptyMessages _emptyMsg;

		// Token: 0x04000837 RID: 2103
		public bool RenderDefaultEditors;

		// Token: 0x04000838 RID: 2104
		internal string ExternalToolBarID = string.Empty;

		// Token: 0x04000839 RID: 2105
		private static readonly string _defTemporaryFilterCaption = "Filter Applied";

		// Token: 0x0400083D RID: 2109
		private const string _nullValue = "<null>";

		// Token: 0x0400083E RID: 2110
		private const string _EXPORT_PROCESS_KEY = "_GRID_EXPORT_PORCESS_KEY_";

		// Token: 0x0400083F RID: 2111
		private const string _GRID_FILTERS_KEY = "GRID_FILTERS";

		// Token: 0x04000840 RID: 2112
		private static readonly Guid _FE_FILTER_ID = Guid.Empty;

		// Token: 0x04000841 RID: 2113
		internal static readonly Guid _DD_FILTER_ID = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);

		// Token: 0x04000842 RID: 2114
		private PXTableStyle style;

		// Token: 0x04000843 RID: 2115
		private PXGridStyles gridStyles;

		// Token: 0x04000844 RID: 2116
		private PXGridLevelMode mode;

		// Token: 0x04000845 RID: 2117
		private PXGridLevelLayout layout;

		// Token: 0x04000846 RID: 2118
		private PXGridLevelStyles levelStyles;

		// Token: 0x04000847 RID: 2119
		private PXGridLevelImages images;

		// Token: 0x04000848 RID: 2120
		private PXGridEvents clientEvents;

		// Token: 0x04000849 RID: 2121
		private PXMenuStyles menuStyles;

		// Token: 0x0400084A RID: 2122
		private PXMenuImages menuImages;

		// Token: 0x0400084B RID: 2123
		private PXGridLevelCollection levels;

		// Token: 0x0400084C RID: 2124
		private PXGridRowCollection rows;

		// Token: 0x0400084D RID: 2125
		private PXExpandEffects expandEffects;

		// Token: 0x0400084E RID: 2126
		private ScriptRegisterFlag scriptFlags;

		// Token: 0x0400084F RID: 2127
		private PXGridActionBar actionBar;

		// Token: 0x04000850 RID: 2128
		private PXGridCallbacks callbacks;

		// Token: 0x04000851 RID: 2129
		private PXAutoSizeInfo autoSize;

		// Token: 0x04000852 RID: 2130
		private PXParamCollection editPageParams;

		// Token: 0x04000853 RID: 2131
		private PXCallbackSettingsExt autoCallBack;

		// Token: 0x04000854 RID: 2132
		private PXCallbackSettings onChangeCommand;

		// Token: 0x04000855 RID: 2133
		private bool filterShortCuts;

		// Token: 0x04000856 RID: 2134
		private PXLayoutSettings contentLayout;

		// Token: 0x04000857 RID: 2135
		private PXGridExportImages _exportImages;

		// Token: 0x04000858 RID: 2136
		private PXToolBarItemCollection filesDialogToolbarItems;

		// Token: 0x04000859 RID: 2137
		private PXGridCell activeCell;

		// Token: 0x0400085A RID: 2138
		private PXGridRow activeRow;

		// Token: 0x0400085B RID: 2139
		private bool layoutLoaded;

		// Token: 0x0400085C RID: 2140
		private bool hidden;

		// Token: 0x0400085D RID: 2141
		private const FilterSelectorType _defFilterSel = FilterSelectorType.Tabs;

		// Token: 0x0400085E RID: 2142
		private const GridScrollBars _defScroll = GridScrollBars.Auto;

		// Token: 0x0400085F RID: 2143
		private const GridViewMode _defMode = GridViewMode.Flat;

		// Token: 0x04000860 RID: 2144
		private const ColumnGeneration _defGenMode = ColumnGeneration.None;

		// Token: 0x04000861 RID: 2145
		private const string _defNoData = "The control has no data to render.";

		// Token: 0x04000862 RID: 2146
		private const MarkRequiredMode _defMarkRequired = MarkRequiredMode.True;

		// Token: 0x04000863 RID: 2147
		private const GridDropMode _defDropMode = GridDropMode.False;

		// Token: 0x04000864 RID: 2148
		internal bool _selectorBound;

		// Token: 0x04000868 RID: 2152
		private static readonly object DeletingEvent;

		// Token: 0x04000869 RID: 2153
		private static readonly object DeletedEvent;

		// Token: 0x0400086A RID: 2154
		private static readonly object InsertingEvent;

		// Token: 0x0400086B RID: 2155
		private static readonly object InsertedEvent;

		// Token: 0x0400086C RID: 2156
		private static readonly object UpdatingEvent;

		// Token: 0x0400086D RID: 2157
		private static readonly object UpdatedEvent;

		// Token: 0x04000883 RID: 2179
		private static readonly Guid MinFilterId = FilterListAttribute.MinFilterValue;

		// Token: 0x04000884 RID: 2180
		private bool? NoteFieldSpecified;

		// Token: 0x04000886 RID: 2182
		internal bool NeedRepaintRows;

		// Token: 0x04000887 RID: 2183
		private GridPagerMode? storedPagerMode;

		// Token: 0x04000888 RID: 2184
		private IEnumerable<PXGridRow> exportItems;

		// Token: 0x04000889 RID: 2185
		private const int _defPageSize = 25;

		// Token: 0x0400088A RID: 2186
		private const int _defFastFilterMaxLength = 100;

		// Token: 0x0400088B RID: 2187
		private const GridPageSizeMode _defPageSizeMode = GridPageSizeMode.None;

		// Token: 0x0400088C RID: 2188
		private const string _maxExportRows = "maxExportRows";

		// Token: 0x0400088D RID: 2189
		private const string _defFilesField = "NoteFiles";

		// Token: 0x0400088E RID: 2190
		private const string _defNoteField = "NoteText";

		// Token: 0x0400088F RID: 2191
		private const string _defFastFilterWildcard = "*";

		// Token: 0x04000890 RID: 2192
		private const PXCondition _defFastFilterCondition = PXCondition.RLIKE;

		// Token: 0x04000891 RID: 2193
		private List<PXStateChangeInfo> stateChanges;

		// Token: 0x04000892 RID: 2194
		private PXGridClientState clientState;

		// Token: 0x04000893 RID: 2195
		private bool isDataBound;

		// Token: 0x04000894 RID: 2196
		private bool pagePreLoadFired;

		// Token: 0x04000895 RID: 2197
		private bool updateError;

		// Token: 0x04000896 RID: 2198
		private bool newRowActive;

		// Token: 0x04000897 RID: 2199
		private bool itemStateChanged;

		// Token: 0x04000898 RID: 2200
		private bool inputBoxLoaded;

		// Token: 0x04000899 RID: 2201
		private bool indicatorsValid;

		// Token: 0x0400089A RID: 2202
		private bool isFilterVisited;

		// Token: 0x0400089B RID: 2203
		private bool needUpdatePivot;

		// Token: 0x0400089C RID: 2204
		private bool pivotEditorVisible;

		// Token: 0x0400089D RID: 2205
		private int totalRowCount;

		// Token: 0x0400089E RID: 2206
		private int dataSourceCount;

		// Token: 0x0400089F RID: 2207
		private int pageIndex;

		// Token: 0x040008A0 RID: 2208
		private int pageSize = 25;

		// Token: 0x040008A1 RID: 2209
		private int rowIndex = -1;

		// Token: 0x040008A2 RID: 2210
		private int startRowIndex;

		// Token: 0x040008A3 RID: 2211
		private int? formLevel;

		// Token: 0x040008A4 RID: 2212
		private object searchValue;

		// Token: 0x040008A5 RID: 2213
		private object rowDataItem;

		// Token: 0x040008A6 RID: 2214
		private string searchStr;

		// Token: 0x040008A7 RID: 2215
		private string updateResult;

		// Token: 0x040008A8 RID: 2216
		private bool externalSearch;

		// Token: 0x040008A9 RID: 2217
		private string noteIDField;

		// Token: 0x040008AA RID: 2218
		private string noteDocsField;

		// Token: 0x040008AB RID: 2219
		private object[] searchKey;

		// Token: 0x040008AC RID: 2220
		private DataKey dataKey;

		// Token: 0x040008AD RID: 2221
		private DataKey dataValues;

		// Token: 0x040008AE RID: 2222
		private OrderedDictionary keyTable;

		// Token: 0x040008AF RID: 2223
		private OrderedDictionary valuesTable;

		// Token: 0x040008B0 RID: 2224
		private Dictionary<string, object> insertPos;

		// Token: 0x040008B1 RID: 2225
		private bool insertPosMode;

		// Token: 0x040008B2 RID: 2226
		private List<Dictionary<string, object>> rowsToMove;

		// Token: 0x040008B3 RID: 2227
		private List<PXCallbackCommand> callbackCommands;

		// Token: 0x040008B4 RID: 2228
		private PXCallbackManager callback;

		// Token: 0x040008B5 RID: 2229
		private PXFilesDisplay filesDisplaySettings;

		// Token: 0x040008B6 RID: 2230
		private string editingField;

		// Token: 0x040008B7 RID: 2231
		private string[] fastFilterFields;

		// Token: 0x040008B8 RID: 2232
		private string[] quickFilterFields;

		// Token: 0x040008B9 RID: 2233
		private string fastFilter;

		// Token: 0x040008BA RID: 2234
		private bool dataSourceValid;

		// Token: 0x040008BB RID: 2235
		private bool filterViewValid;

		// Token: 0x040008BC RID: 2236
		private bool suppressBinding;

		// Token: 0x040008BD RID: 2237
		private IDataSource dataSource;

		// Token: 0x040008BE RID: 2238
		private PXDSSelectArguments argumentsExt;

		// Token: 0x040008BF RID: 2239
		private DataSourceSelectArguments arguments;

		// Token: 0x040008C0 RID: 2240
		private PXParamCollection parameters;

		// Token: 0x040008C1 RID: 2241
		private PXParamCollection searches;

		// Token: 0x040008C2 RID: 2242
		private bool pendingUpdate;

		// Token: 0x040008C3 RID: 2243
		private bool pageAdjusted;

		// Token: 0x040008C4 RID: 2244
		private bool adjustOnReload;

		// Token: 0x040008C5 RID: 2245
		private KeyValuePair<ErrorState, string> errorState;

		// Token: 0x040008C6 RID: 2246
		private Guid? filterID;

		// Token: 0x040008C7 RID: 2247
		private int? pivotID;

		// Token: 0x040008C8 RID: 2248
		private List<PXFilterRow> filterRows;

		// Token: 0x040008C9 RID: 2249
		private List<PXFilterRow> quickFilters;

		// Token: 0x040008CA RID: 2250
		private List<PXFilterRow> internalFilterRows;

		// Token: 0x040008CB RID: 2251
		private bool filterActive;

		// Token: 0x040008CC RID: 2252
		private bool filterActiveLoaded;

		// Token: 0x040008CD RID: 2253
		private bool reloadFilters;

		// Token: 0x040008CE RID: 2254
		private bool? colsFilterActive;

		// Token: 0x040008CF RID: 2255
		private DataSourceView filterView;

		// Token: 0x040008D0 RID: 2256
		private Dictionary<Guid, string> filterNames;

		// Token: 0x040008D1 RID: 2257
		private Dictionary<Guid, int> filterPivots;

		// Token: 0x040008D2 RID: 2258
		private PXDataSourceView _pxview;

		// Token: 0x040008D3 RID: 2259
		private PXFilesDialog _filesDialog;

		// Token: 0x040008D4 RID: 2260
		private PXFilterDialog _filterDialog;

		// Token: 0x040008D5 RID: 2261
		private PXColumnsDialog _columnsDialog;

		// Token: 0x040008D6 RID: 2262
		private PXFilesDialog _uploadDialog;

		// Token: 0x040008D7 RID: 2263
		internal const string ExportSessionKey = "GridExportData";

		// Token: 0x040008D8 RID: 2264
		private const string _inputBoxID = "ib";

		// Token: 0x040008D9 RID: 2265
		private const string _filesBoxID = "fb";

		// Token: 0x040008DA RID: 2266
		private const string _filterDialogID = "fd";

		// Token: 0x040008DB RID: 2267
		private const string _columnsDialogID = "cd";

		// Token: 0x040008DC RID: 2268
		private const string _filterBoxID = "fe";

		// Token: 0x040008DD RID: 2269
		private const string _filesMenuID = "fmnu";

		// Token: 0x040008DE RID: 2270
		private const string _uploaderID = "_upldr";

		// Token: 0x040008DF RID: 2271
		private const string _errCollection = "PXGrid control with ID '{0}' must have a data source that implements ICollection.";

		// Token: 0x040008E0 RID: 2272
		private const string _errUnhandledEvent = "IDataSource that is the data source for PXGrid '{0}' returned a null view.";

		// Token: 0x040008E1 RID: 2273
		private const string _errDataSource = "DataSourceID of '{0}' must be the ID of a control of the IDataSource type.";

		// Token: 0x040008E2 RID: 2274
		private const string _errViewNotFound = "The view that the PXGrid control '{0}' requested cannot be found.";

		// Token: 0x040008E3 RID: 2275
		private const string _pivotID = "pivotT";

		// Token: 0x040008E4 RID: 2276
		private const string _pivotDsID = "pivotDS";

		// Token: 0x040008E5 RID: 2277
		private const string _pivotFrameID = "pivotE";

		// Token: 0x040008E6 RID: 2278
		internal Action OnRenderColHeaderTable;

		// Token: 0x040008E7 RID: 2279
		private const ScriptRegisterFlag childScriptFlags = (ScriptRegisterFlag)6;

		// Token: 0x040008E8 RID: 2280
		private const int textSizeLimit = 1000000;

		// Token: 0x040008E9 RID: 2281
		internal const string NoteColumnKey = "Notes";

		// Token: 0x040008EA RID: 2282
		internal const string FilesColumnKey = "Files";

		// Token: 0x040008EC RID: 2284
		private PXActionBar toolsTop;

		// Token: 0x040008ED RID: 2285
		private PXActionBar toolsBottom;

		// Token: 0x040008EE RID: 2286
		private PXTableCell captionCell;

		// Token: 0x040008EF RID: 2287
		private PXTableCell contentCell;

		// Token: 0x040008F0 RID: 2288
		private PXTableCell toolsFilter;

		// Token: 0x040008F1 RID: 2289
		private PXToolBar tlbFilterTools;

		// Token: 0x040008F2 RID: 2290
		private PXToolBar tlbFilters;

		// Token: 0x040008F3 RID: 2291
		private List<PXMenu> menuControls = new List<PXMenu>();

		// Token: 0x040008F4 RID: 2292
		private List<PXTable> formControls = new List<PXTable>();

		// Token: 0x040008F5 RID: 2293
		private WebControl filterSelector;

		// Token: 0x040008F6 RID: 2294
		private PXFilterEditor filterEditor;

		// Token: 0x040008F7 RID: 2295
		private PXButtonEdit filterBar;

		// Token: 0x040008F8 RID: 2296
		private PXImportWizardPanel importPanel;

		// Token: 0x040008F9 RID: 2297
		private PXSmartPanel filterSavePanel;

		// Token: 0x040008FA RID: 2298
		private bool templateEditMode;

		// Token: 0x040008FB RID: 2299
		private int templateLevel;

		// Token: 0x040008FC RID: 2300
		private PXStyleManager styleManager;

		// Token: 0x040008FD RID: 2301
		private bool renderState = true;

		// Token: 0x040008FE RID: 2302
		private bool columnsSynchronized;

		// Token: 0x040008FF RID: 2303
		private bool columnsSynchronizedAfterBinding;

		// Token: 0x04000900 RID: 2304
		private PXPivotTable pivotTable;

		// Token: 0x04000901 RID: 2305
		private PXPivotDataSource pivotDS;

		// Token: 0x04000902 RID: 2306
		private const string _headDivID = "headerDiv";

		// Token: 0x04000903 RID: 2307
		private const string _headTableID = "headerT";

		// Token: 0x04000904 RID: 2308
		private const string _footerDivID = "footerDiv";

		// Token: 0x04000905 RID: 2309
		private const string _footerTableID = "footerT";

		// Token: 0x04000906 RID: 2310
		private const string _scrollDivID = "scrollDiv";

		// Token: 0x04000907 RID: 2311
		private const string _dataTableID = "dataT";

		// Token: 0x04000908 RID: 2312
		private const string _rowFormID = "rowForm";

		// Token: 0x04000909 RID: 2313
		private const string _levelFormID = "lf";

		// Token: 0x0400090A RID: 2314
		private const string _levelViewID = "lv";

		// Token: 0x0400090B RID: 2315
		private const string _menuID = "menu";

		// Token: 0x0400090C RID: 2316
		private const string _nextPageID = "nextPage";

		// Token: 0x0400090D RID: 2317
		private const string _prevPageID = "prevPage";

		// Token: 0x0400090E RID: 2318
		private const string _firstPageID = "firstPage";

		// Token: 0x0400090F RID: 2319
		private const string _lastPageID = "lastPage";

		// Token: 0x04000910 RID: 2320
		private const string _nextRangeID = "nextRange";

		// Token: 0x04000911 RID: 2321
		private const string _prevRangeID = "prevRange";

		// Token: 0x04000912 RID: 2322
		private const string _filterSelectorID = "fs";

		// Token: 0x04000913 RID: 2323
		private const string _filterToolBarID = "ft";

		// Token: 0x04000914 RID: 2324
		private const string _filterBarID = "fb";

		// Token: 0x04000915 RID: 2325
		private const string _importPanelID = "imp";

		// Token: 0x04000916 RID: 2326
		internal const string _dataRowID = "row";

		// Token: 0x04000917 RID: 2327
		internal const string _pagerID = "pager";

		// Token: 0x04000918 RID: 2328
		internal const string _toolsID = "tlb";

		// Token: 0x04000919 RID: 2329
		internal const string _colHeadID = "colH";

		// Token: 0x0400091A RID: 2330
		internal const string _colStatHeadID = "colHS";

		// Token: 0x0400091B RID: 2331
		internal const string _colFooterID = "colF";

		// Token: 0x0400091C RID: 2332
		internal const string _colStatFooterID = "colFS";

		// Token: 0x02000399 RID: 921
		internal enum FilterRowType
		{
			// Token: 0x040016AA RID: 5802
			Column = 1,
			// Token: 0x040016AB RID: 5803
			FilterEditor,
			// Token: 0x040016AC RID: 5804
			DrillDown
		}

		// Token: 0x0200039A RID: 922
		public sealed class ExportProcessInfo
		{
			// Token: 0x060037BE RID: 14270 RVA: 0x000E3B3B File Offset: 0x000E1D3B
			private ExportProcessInfo(string uid, string command, string type)
			{
				this._uid = uid;
				this._command = command;
				this._type = type;
			}

			// Token: 0x170011FA RID: 4602
			// (get) Token: 0x060037BF RID: 14271 RVA: 0x000E3B58 File Offset: 0x000E1D58
			public object UID
			{
				get
				{
					return this._uid;
				}
			}

			// Token: 0x170011FB RID: 4603
			// (get) Token: 0x060037C0 RID: 14272 RVA: 0x000E3B60 File Offset: 0x000E1D60
			public string Command
			{
				get
				{
					return this._command;
				}
			}

			// Token: 0x170011FC RID: 4604
			// (get) Token: 0x060037C1 RID: 14273 RVA: 0x000E3B68 File Offset: 0x000E1D68
			public string Type
			{
				get
				{
					return this._type;
				}
			}

			// Token: 0x060037C2 RID: 14274 RVA: 0x000E3B70 File Offset: 0x000E1D70
			public static PXGrid.ExportProcessInfo Parse(string str)
			{
				if (string.IsNullOrEmpty(str))
				{
					throw new ArgumentNullException("str");
				}
				string[] array = str.Split(new char[]
				{
					'|'
				}, StringSplitOptions.RemoveEmptyEntries);
				string command = "unknown";
				string text = null;
				string type = null;
				for (int i = 0; i < array.Length; i++)
				{
					string text2 = array[i];
					if (i != 0)
					{
						if (i != 1)
						{
							i = array.Length;
						}
						else
						{
							text = text2.Split(new char[]
							{
								'$'
							})[0];
						}
					}
					else
					{
						string[] array2 = text2.Split(new char[]
						{
							'$'
						});
						command = array2[0];
						if (array2.Length > 1)
						{
							type = array2[1];
						}
					}
				}
				if (string.IsNullOrEmpty(text))
				{
					text = Guid.NewGuid().ToString();
				}
				return new PXGrid.ExportProcessInfo(text, command, type);
			}

			// Token: 0x060037C3 RID: 14275 RVA: 0x000E3C38 File Offset: 0x000E1E38
			public override string ToString()
			{
				this.ReadProcessInformation();
				string text = this._command;
				if (!string.IsNullOrEmpty(this._type))
				{
					text = text + "$" + this._type;
				}
				return string.Format("{0}|{1}|{2}|{3}|{4}", new object[]
				{
					text,
					this._uid,
					this._status,
					this._seconds,
					this._errors
				});
			}

			// Token: 0x060037C4 RID: 14276 RVA: 0x000E3CB0 File Offset: 0x000E1EB0
			private void ReadProcessInformation()
			{
				if (!this._readInformation)
				{
					this._readInformation = true;
					TimeSpan timeSpan;
					Exception o;
					PXLongRunStatus status = PXLongOperation.GetStatus(this._uid, out timeSpan, out o);
					this._status = status.ToString().ToLower();
					this._seconds = (int)timeSpan.TotalSeconds;
					string errors;
					if (status != PXLongRunStatus.Aborted)
					{
						errors = null;
					}
					else
					{
						errors = o.With((Exception _) => _.Message);
					}
					this._errors = errors;
				}
			}

			// Token: 0x040016AD RID: 5805
			private readonly string _uid;

			// Token: 0x040016AE RID: 5806
			private readonly string _command;

			// Token: 0x040016AF RID: 5807
			private readonly string _type;

			// Token: 0x040016B0 RID: 5808
			private bool _readInformation;

			// Token: 0x040016B1 RID: 5809
			private string _status;

			// Token: 0x040016B2 RID: 5810
			private int _seconds;

			// Token: 0x040016B3 RID: 5811
			private string _errors;
		}

		// Token: 0x0200039B RID: 923
		private enum GridStyle
		{
			// Token: 0x040016B5 RID: 5813
			ToolsCell,
			// Token: 0x040016B6 RID: 5814
			ToolsBottom,
			// Token: 0x040016B7 RID: 5815
			Caption,
			// Token: 0x040016B8 RID: 5816
			SearchEditor,
			// Token: 0x040016B9 RID: 5817
			SearchText,
			// Token: 0x040016BA RID: 5818
			HeaderCell,
			// Token: 0x040016BB RID: 5819
			ContentCell
		}

		// Token: 0x0200039C RID: 924
		private enum LevelStyle
		{
			// Token: 0x040016BD RID: 5821
			Row,
			// Token: 0x040016BE RID: 5822
			AltRow,
			// Token: 0x040016BF RID: 5823
			SelRow,
			// Token: 0x040016C0 RID: 5824
			ActiveCell,
			// Token: 0x040016C1 RID: 5825
			ActiveRow,
			// Token: 0x040016C2 RID: 5826
			Error,
			// Token: 0x040016C3 RID: 5827
			Warning,
			// Token: 0x040016C4 RID: 5828
			RowForm,
			// Token: 0x040016C5 RID: 5829
			RowSelector,
			// Token: 0x040016C6 RID: 5830
			Header,
			// Token: 0x040016C7 RID: 5831
			Footer,
			// Token: 0x040016C8 RID: 5832
			SelHeader,
			// Token: 0x040016C9 RID: 5833
			SelFooter,
			// Token: 0x040016CA RID: 5834
			CellButton,
			// Token: 0x040016CB RID: 5835
			CellEditor,
			// Token: 0x040016CC RID: 5836
			EditorText,
			// Token: 0x040016CD RID: 5837
			ReadOnlyCell
		}

		// Token: 0x0200039D RID: 925
		private enum ColumnStyle
		{
			// Token: 0x040016CF RID: 5839
			Row,
			// Token: 0x040016D0 RID: 5840
			Header,
			// Token: 0x040016D1 RID: 5841
			Footer,
			// Token: 0x040016D2 RID: 5842
			CellButton
		}

		// Token: 0x0200039E RID: 926
		private enum CellType
		{
			// Token: 0x040016D4 RID: 5844
			Normal,
			// Token: 0x040016D5 RID: 5845
			Alt,
			// Token: 0x040016D6 RID: 5846
			ActiveRow,
			// Token: 0x040016D7 RID: 5847
			ActiveCell,
			// Token: 0x040016D8 RID: 5848
			ReadOnly
		}
	}
}
