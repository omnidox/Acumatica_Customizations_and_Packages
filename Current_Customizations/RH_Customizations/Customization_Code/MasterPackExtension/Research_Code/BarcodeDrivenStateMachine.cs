using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using PX.Common;
using PX.Data;
using PX.Data.WorkflowAPI;

namespace PX.BarcodeProcessing
{
	/// <summary>The core class of Acumatica's barcode-driven engine.
	/// It connects all components of the barcode-driven engine.</summary>
	/// <typeparam name="TSelf">The type of the self. BarcodeDrivenStateMachine uses curiously recurring template pattern (CRTP) approach to keep its members and components highly typed.</typeparam>
	/// <typeparam name="TGraph">
	/// The host graph. Since the barcode processing is built upon an existing Acumatica ERP form and automates its functions, it is optimal to place all barcode processing logic in a generic extension, which can access the original graph. However, it is necessary to have both the original and the extended forms in the system. That's why the BarcodeDrivenStateMachine descendants do not use an exact target graph by itself, instead they use its empty descendant.
	/// </typeparam>
	/// <remarks>
	///   <para>
	/// While creating a new barcode-driven Acumatica ERP form, you introduce a descendant of this class
	/// enclosed with itself in the TSelf type parameter and with an empty descendant of the target graph
	/// in the TGraph type parameter, and then implement its abstract members.
	/// All the components of this descendant will have the TScanBasis type parameter set to the class
	/// passed into the TSelf type parameter of the descendant.
	/// </para>
	///   <para>All components can access the core class via their Basis property. </para>
	/// </remarks>
	// Token: 0x02000005 RID: 5
	public abstract class BarcodeDrivenStateMachine<TSelf, TGraph> : PXGraphExtension<TGraph>, IBarcodeDrivenStateMachine, IBarcodeDrivenStateMachineComponentDecorator<TSelf> where TSelf : BarcodeDrivenStateMachine<TSelf, TGraph> where TGraph : PXGraph, new()
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000005 RID: 5 RVA: 0x0000208E File Offset: 0x0000028E
		// (set) Token: 0x06000006 RID: 6 RVA: 0x00002096 File Offset: 0x00000296
		private protected bool MultiCommit { protected get; private set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000007 RID: 7 RVA: 0x0000209F File Offset: 0x0000029F
		public ScanHeader Header
		{
			get
			{
				return this.HeaderView.Current ?? new ScanHeader();
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000008 RID: 8 RVA: 0x000020B8 File Offset: 0x000002B8
		public ValueSetter<ScanHeader> HeaderSetter
		{
			get
			{
				return this.HeaderView.GetSetterForCurrent<ScanHeader>().WithEventFiring;
			}
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000009 RID: 9 RVA: 0x000020D8 File Offset: 0x000002D8
		// (set) Token: 0x0600000A RID: 10 RVA: 0x000020E8 File Offset: 0x000002E8
		public Guid? NoteID
		{
			get
			{
				return this.Header.NoteID;
			}
			set
			{
				if (value == null)
				{
					this.HeaderView.Cache.SetDefaultExt<ScanHeader.noteID>(this.Header);
					return;
				}
				this.HeaderSetter.Set<Guid?>((ScanHeader h) => h.NoteID, value);
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600000B RID: 11 RVA: 0x0000215D File Offset: 0x0000035D
		public virtual bool ExplicitConfirmation
		{
			get
			{
				return false;
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600000C RID: 12 RVA: 0x00002160 File Offset: 0x00000360
		protected virtual bool OnlyLocalModeChange
		{
			get
			{
				return true;
			}
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00002164 File Offset: 0x00000364
		public override void Initialize()
		{
			base.Initialize();
			this._reporter = new BarcodeDrivenStateMachine<TSelf, TGraph>.ScanReporterImpl(this);
			this._previousMode = this.Info.Current.ModeCaption;
			this._previousQuestion = this.Info.Current.Question;
			this._previousInstructions = this.Info.Current.Instructions;
			this._previousPrompt = this.Info.Current.Prompt;
			this._previousPromptCombined = this.Info.Current.PromptCombined;
			this.Logs.AllowInsert = (this.Logs.AllowUpdate = (this.Logs.AllowDelete = false));
			this.InitializeModes();
			this.InitializeCommandActions();
			if (string.IsNullOrEmpty(this.HeaderView.Current.Mode))
			{
				this.GetDefaultMode().TakeOver();
				this.PromptAndInstruct();
			}
		}

		// Token: 0x0600000E RID: 14 RVA: 0x00002250 File Offset: 0x00000450
		private void InitializeModes()
		{
			ScanMode<TSelf>[] array = this.CreateScanModes().Select(new Func<ScanMode<TSelf>, ScanMode<TSelf>>(this.DecorateScanMode)).ToArray<ScanMode<TSelf>>();
			ScanMode<TSelf>[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].Init(this.Self);
			}
			this.ScanModes = array.Select(new Func<ScanMode<TSelf>, ScanMode<TSelf>>(this.LateDecorateScanMode)).ToArray<ScanMode<TSelf>>();
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x0600000F RID: 15 RVA: 0x000022B7 File Offset: 0x000004B7
		public TSelf Self
		{
			[DebuggerStepThrough]
			get
			{
				return (TSelf)((object)this);
			}
		}

		/// <summary>
		/// Grants access to a strongly typed version of the host graph
		/// (as compared to PXGraph used in the <see cref="T:PX.BarcodeProcessing.IBarcodeDrivenStateMachine" /> interface)
		/// and also lifts its accessibility up to public
		/// (as compared to the protected Base member of the <see cref="T:PX.Data.PXGraphExtension`1" />).
		/// </summary>
		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000010 RID: 16 RVA: 0x000022BF File Offset: 0x000004BF
		public TGraph Graph
		{
			[DebuggerStepThrough]
			[DebuggerStepperBoundary]
			get
			{
				return base.Base;
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000011 RID: 17 RVA: 0x000022C7 File Offset: 0x000004C7
		PXGraph IBarcodeDrivenStateMachine.Graph
		{
			[DebuggerStepThrough]
			get
			{
				return this.Graph;
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000012 RID: 18 RVA: 0x000022D4 File Offset: 0x000004D4
		public IScanReporter Reporter
		{
			[DebuggerStepThrough]
			get
			{
				return this._reporter;
			}
		}

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000013 RID: 19 RVA: 0x000022DC File Offset: 0x000004DC
		IEnumerable<IScanMode> IBarcodeDrivenStateMachine.ScanModes
		{
			[DebuggerStepThrough]
			get
			{
				return this.ScanModes;
			}
		}

		/// <value>
		/// The value is populated on the extension initialization and is based on the results of
		/// the protected <see cref="M:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.CreateScanModes">CreateScanModes()</see>method.
		/// </value>
		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000014 RID: 20 RVA: 0x000022E4 File Offset: 0x000004E4
		// (set) Token: 0x06000015 RID: 21 RVA: 0x000022EC File Offset: 0x000004EC
		public IEnumerable<ScanMode<TSelf>> ScanModes { get; private set; } = Array.Empty<ScanMode<TSelf>>();

		/// <summary>Creates the modes of the barcode-driven form.
		/// It is the most important method of the class because it provides the most part of the configuration
		/// of the barcode-driven engine.</summary>
		// Token: 0x06000016 RID: 22
		protected abstract IEnumerable<ScanMode<TSelf>> CreateScanModes();

		/// <summary>Returns the default mode of the barcode-driven form.</summary>
		/// <returns>By default, it returns the first mode of the <see cref="P:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.ScanModes" /> collection.</returns>
		// Token: 0x06000017 RID: 23 RVA: 0x000022F5 File Offset: 0x000004F5
		protected virtual ScanMode<TSelf> GetDefaultMode()
		{
			return this.ScanModes.FirstOrDefault<ScanMode<TSelf>>();
		}

		/// <summary>Grants access to the current mode, similar to <see cref="P:PX.BarcodeProcessing.IBarcodeDrivenStateMachine.CurrentMode" />,
		/// but uses a typed approach (that is, ScanMode&lt;TSelf&gt; instead of IScanMode)</summary>
		// Token: 0x1700000D RID: 13
		// (get) Token: 0x06000018 RID: 24 RVA: 0x00002302 File Offset: 0x00000502
		public ScanMode<TSelf> CurrentMode
		{
			get
			{
				return this.ScanModes.FirstOrDefault((ScanMode<TSelf> mode) => mode.Code == this.Header.Mode);
			}
		}

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000019 RID: 25 RVA: 0x0000231B File Offset: 0x0000051B
		IScanMode IBarcodeDrivenStateMachine.CurrentMode
		{
			get
			{
				return this.CurrentMode;
			}
		}

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x0600001A RID: 26 RVA: 0x00002323 File Offset: 0x00000523
		public IScanState CurrentState
		{
			get
			{
				ScanMode<TSelf> currentMode = this.CurrentMode;
				if (currentMode == null)
				{
					return null;
				}
				return currentMode.CurrentState;
			}
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x0600001B RID: 27 RVA: 0x00002336 File Offset: 0x00000536
		public IScanQuestion CurrentQuestion
		{
			get
			{
				ScanMode<TSelf> currentMode = this.CurrentMode;
				if (currentMode == null)
				{
					return null;
				}
				return currentMode.CurrentQuestion;
			}
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002349 File Offset: 0x00000549
		public TMode FindMode<TMode>() where TMode : ScanMode<TSelf>
		{
			return this.ScanModes.OfType<TMode>().FirstOrDefault<TMode>();
		}

		// Token: 0x0600001D RID: 29 RVA: 0x0000235C File Offset: 0x0000055C
		public ScanMode<TSelf> FindMode(string code)
		{
			return this.ScanModes.FirstOrDefault((ScanMode<TSelf> mode) => mode.Code == code);
		}

		// Token: 0x0600001E RID: 30 RVA: 0x0000238D File Offset: 0x0000058D
		protected IEnumerable logs()
		{
			PXDelegateResult pxdelegateResult = new PXDelegateResult();
			pxdelegateResult.IsResultFiltered = true;
			pxdelegateResult.IsResultSorted = true;
			pxdelegateResult.IsResultTruncated = true;
			pxdelegateResult.AddRange(this.Logs.Cache.Cached.RowCast<ScanLog>().Reverse<ScanLog>());
			return pxdelegateResult;
		}

		// Token: 0x0600001F RID: 31 RVA: 0x000023C9 File Offset: 0x000005C9
		[PXButton(CommitChanges = true, Tooltip = "Scan *OK to confirm", Connotation = ActionConnotation.Success)]
		[PXUIField(DisplayName = "OK")]
		protected virtual IEnumerable scanConfirm(PXAdapter adapter)
		{
			return this.scanBarcode(adapter, "*OK");
		}

		// Token: 0x06000020 RID: 32 RVA: 0x000023D7 File Offset: 0x000005D7
		[PXButton(CommitChanges = true, Tooltip = "Scan *RESET to execute")]
		[PXUIField(DisplayName = "Reset")]
		protected virtual IEnumerable scanReset(PXAdapter adapter)
		{
			return this.scanBarcode(adapter, "*RESET");
		}

		// Token: 0x06000021 RID: 33 RVA: 0x000023E5 File Offset: 0x000005E5
		[PXButton(CommitChanges = true)]
		[PXUIField(DisplayName = "Scan", Visible = false)]
		protected virtual IEnumerable scan(PXAdapter adapter)
		{
			this.ProcessBarcode(this.Header.Barcode);
			return adapter.Get();
		}

		// Token: 0x06000022 RID: 34 RVA: 0x000023FF File Offset: 0x000005FF
		public virtual IEnumerable scanBarcode(PXAdapter adapter, string barcode)
		{
			this.Header.Barcode = barcode;
			return this.Scan.Press(adapter);
		}

		// Token: 0x06000023 RID: 35 RVA: 0x0000241C File Offset: 0x0000061C
		protected void InitializeCommandActions()
		{
			foreach (ScanMode<TSelf> scanMode in from m in this.ScanModes
			orderby m == this.CurrentMode descending
			select m)
			{
				foreach (IScanCommand scanCommand in ((IScanMode)scanMode).Commands)
				{
					if (this.Graph.Actions[scanCommand.ButtonName] == null)
					{
						string scanCode = "*" + scanCommand.Code;
						PXNamedAction.AddAction(this.Graph, this.Graph.PrimaryItemType, scanCommand.ButtonName, scanCommand.DisplayName, null, false, (PXAdapter adapter) => this.scanBarcode(adapter, scanCode), new PXEventSubscriberAttribute[]
						{
							new PXButtonAttribute
							{
								CommitChanges = true,
								Tooltip = this.Localize("Scan {0} to execute", new object[]
								{
									scanCode
								})
							}
						});
					}
				}
			}
			foreach (ScanMode<TSelf> scanMode2 in from m in this.ScanModes
			orderby m == this.CurrentMode descending
			select m)
			{
				foreach (IScanRedirect scanRedirect in ((IScanMode)scanMode2).Redirects)
				{
					if (this.Graph.Actions[scanRedirect.ButtonName] == null)
					{
						string scanCode = "@" + scanRedirect.Code;
						PXNamedAction.AddAction(this.Graph, this.Graph.PrimaryItemType, scanRedirect.ButtonName, scanRedirect.DisplayName, null, false, (PXAdapter adapter) => this.scanBarcode(adapter, scanCode), new PXEventSubscriberAttribute[]
						{
							new PXButtonAttribute
							{
								CommitChanges = true,
								Tooltip = this.Localize("Scan {0} to change mode", new object[]
								{
									scanCode
								})
							}
						});
					}
				}
			}
		}

		// Token: 0x06000024 RID: 36 RVA: 0x000026E8 File Offset: 0x000008E8
		protected void ActualizeCommandActions()
		{
			foreach (IScanMode scanMode in from m in this.ScanModes
			orderby m == this.CurrentMode
			select m)
			{
				Dictionary<string, IScanCommand> dictionary = scanMode.Commands.ToDictionary((IScanCommand c) => c.ButtonName);
				Dictionary<string, IScanRedirect> dictionary2 = scanMode.Redirects.ToDictionary((IScanRedirect r) => r.ButtonName);
				foreach (string text in this.Graph.Actions.Keys.Cast<string>().ToArray<string>())
				{
					if (dictionary.ContainsKey(text))
					{
						PXAction pxaction = this.Graph.Actions[text];
						IScanCommand scanCommand = dictionary[text];
						pxaction.SetVisible(scanMode.IsActive && scanMode == this.CurrentMode);
						pxaction.SetEnabled(scanMode.IsActive && scanMode == this.CurrentMode && scanCommand.IsApplicable);
					}
					else if (dictionary2.ContainsKey(text))
					{
						PXAction pxaction2 = this.Graph.Actions[text];
						IScanRedirect scanRedirect = dictionary2[text];
						bool flag = scanMode.IsActive && scanMode == this.CurrentMode && scanRedirect.IsPossible;
						pxaction2.SetVisible(base.Base.IsMobile && flag && this.OnlyLocalModeChange.Implies(scanRedirect.TargetHostType == typeof(TGraph)));
						pxaction2.SetEnabled(flag && (scanRedirect.TargetHostType != typeof(TGraph) || scanRedirect.TargetMode.IsNotIn(null, scanMode.Code)));
					}
				}
			}
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002918 File Offset: 0x00000B18
		protected virtual bool RequireConfirmation()
		{
			return this.Header.QuestionCode != null || (this.Header.ScanState == "CONF" && this.CurrentMode.FindState("CONF") != null);
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002955 File Offset: 0x00000B55
		protected virtual void _(Events.FieldSelecting<ScanHeader, ScanHeader.barcode> e)
		{
			if (base.Base.IsMobile)
			{
				this.Mobile_DisplayNameToPromptHack(e);
			}
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00002970 File Offset: 0x00000B70
		protected virtual void _(Events.FieldUpdated<ScanHeader, ScanHeader.barcode> e)
		{
			if (base.Base.IsMobile && !object.Equals(e.OldValue, e.NewValue) && !object.Equals(string.Empty, e.NewValue))
			{
				this.Mobile_AutoCallbackHack((string)e.NewValue);
			}
		}

		// Token: 0x06000028 RID: 40 RVA: 0x000029C8 File Offset: 0x00000BC8
		protected virtual void _(Events.RowSelected<ScanHeader> e)
		{
			e.Cache.IsDirty = false;
			if (e.Row == null)
			{
				return;
			}
			bool flag = this.RequireConfirmation();
			ScanHeader row = e.Row;
			bool flag2 = ((row != null) ? row.ScanState : null) == "WAIT";
			this.ScanConfirm.SetVisible(this.ExplicitConfirmation || flag);
			this.ScanConfirm.SetEnabled(!flag2 && flag);
			this.ScanReset.SetEnabled(!flag2);
			this.ScanReset.SetConnotation(flag ? ActionConnotation.Danger : ActionConnotation.None);
			this.ActualizeCommandActions();
			if (base.Base.IsMobile)
			{
				this.Info.Cache.AdjustUIReadonly(null).For<ScanInfo.mode>(delegate(PXUIFieldAttribute ui)
				{
					ui.Visible = true;
				}).SameFor<ScanInfo.modeCaption>().SameFor<ScanInfo.message>().SameFor<ScanInfo.prompt>().For<ScanInfo.instructions>(delegate(PXUIFieldAttribute ui)
				{
					ScanInfo scanInfo = this.Info.Current;
					ui.Visible = !string.IsNullOrEmpty((scanInfo != null) ? scanInfo.Instructions : null);
				}).For<ScanInfo.question>(delegate(PXUIFieldAttribute ui)
				{
					ScanInfo scanInfo = this.Info.Current;
					ui.Visible = !string.IsNullOrEmpty((scanInfo != null) ? scanInfo.Question : null);
				});
			}
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002AE4 File Offset: 0x00000CE4
		protected void _(Events.RowInserting<ScanHeader> args)
		{
			if (this.Graph.IsMobile)
			{
				this.Mobile_RestoreHeaderOnFirstOpen(true);
			}
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002AFF File Offset: 0x00000CFF
		protected void _(Events.RowInserted<ScanHeader> args)
		{
			if (this.Graph.IsMobile)
			{
				this.Mobile_RestoreHeaderOnFirstOpen(false);
			}
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002B1A File Offset: 0x00000D1A
		protected virtual void _(Events.RowSelected<ScanInfo> e)
		{
			e.Cache.IsDirty = false;
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00002B28 File Offset: 0x00000D28
		protected virtual void _(Events.FieldUpdated<ScanInfo, ScanInfo.instructions, string> e)
		{
			this.SetCombinedPrompt(e.Row, e.NewValue, e.Row.Prompt, e.Row.Question);
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002B52 File Offset: 0x00000D52
		protected virtual void _(Events.FieldUpdated<ScanInfo, ScanInfo.prompt, string> e)
		{
			this.SetCombinedPrompt(e.Row, e.Row.Instructions, e.NewValue, e.Row.Question);
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00002B7C File Offset: 0x00000D7C
		protected virtual void _(Events.FieldUpdated<ScanInfo, ScanInfo.question, string> e)
		{
			this.SetCombinedPrompt(e.Row, e.Row.Instructions, e.Row.Prompt, e.NewValue);
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00002BA8 File Offset: 0x00000DA8
		private void SetCombinedPrompt(ScanInfo info, string instructions, string prompt, string question)
		{
			string text;
			if (prompt == null && instructions == null && question == null)
			{
				text = null;
			}
			else
			{
				text = (from msg in new string[]
				{
					instructions,
					prompt,
					question
				}
				where !string.IsNullOrEmpty(msg)
				select msg).Aggregate((string acc, string msg) => this.Localize("{0} {1}", new object[]
				{
					acc,
					msg
				}));
			}
			string value = text;
			this.Info.Cache.SetValueExt<ScanInfo.promptCombined>(info, value);
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00002C1D File Offset: 0x00000E1D
		protected virtual void _(Events.RowSelected<ScanLog> e)
		{
			e.Cache.IsDirty = false;
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00002C2C File Offset: 0x00000E2C
		protected virtual bool ProcessBarcode(string barcode)
		{
			if (string.IsNullOrEmpty(barcode))
			{
				return false;
			}
			this.RefreshState();
			if (barcode != null && (barcode.Contains("\n") || barcode.Contains(Environment.NewLine)))
			{
				string[] barcodes = barcode.Split(new string[]
				{
					"\n",
					Environment.NewLine
				}, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
				return this.ProcessMultipleBarcode(barcodes);
			}
			return this.ProcessSingleBarcode(barcode);
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00002CA4 File Offset: 0x00000EA4
		protected void RefreshState()
		{
			foreach (KeyValuePair<Type, PXCache> keyValuePair in base.Base.Caches)
			{
				if (keyValuePair.Key.IsNotIn(typeof(ScanHeader), typeof(ScanInfo), typeof(ScanLog)))
				{
					keyValuePair.Value.ClearQueryCacheObsolete();
				}
			}
			base.Base.SelectTimeStamp();
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00002D44 File Offset: 0x00000F44
		protected virtual bool ProcessMultipleBarcode(string[] barcodes)
		{
			this.MultiCommit = true;
			foreach (string barcode in barcodes)
			{
				this.ProcessSingleBarcode(barcode);
				if (this.Info.Current.MessageType.IsIn("ERR", "WRN"))
				{
					break;
				}
			}
			if (this._saveRequested)
			{
				this.Save.Press();
			}
			return true;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00002DAC File Offset: 0x00000FAC
		protected virtual bool ProcessSingleBarcode(string barcode)
		{
			this.HeaderSetter.Set<bool?>((ScanHeader h) => h.ProcessingSucceeded, null);
			this.HeaderSetter.Set<string>((ScanHeader h) => h.Barcode, barcode);
			this.HeaderSetter.Set<string>((ScanHeader h) => h.InitialScanState, this.Header.ScanState);
			this.HeaderSetter.Set<string>((ScanHeader h) => h.InitialQuestionCode, this.Header.QuestionCode);
			this.HeaderSetter.Set<bool?>((ScanHeader x) => x.HasScanStateChanged, new bool?(false));
			this.HeaderSetter.Set<bool?>((ScanHeader h) => h.HasQuestionCodeChanged, new bool?(false));
			ScanHeader scanHeader = PXCache<ScanHeader>.CreateCopy(this.Header);
			ScanInfo infoBackup = PXCache<ScanInfo>.CreateCopy(this.Info.Current);
			if (string.IsNullOrEmpty(barcode))
			{
				this.ReportError("Scan a valid barcode.", Array.Empty<object>());
				return false;
			}
			bool result;
			try
			{
				if (!this.CanHandleScan(barcode))
				{
					result = true;
				}
				else
				{
					bool flag = this.HandleScan(barcode);
					if (this._bypassLogging)
					{
						result = flag;
					}
					else
					{
						this.HeaderSetter.Set<string>((ScanHeader h) => h.Barcode, string.Empty);
						this.HeaderView.UpdateCurrent();
						this.LogScan(scanHeader, PXCache<ScanHeader>.CreateCopy(this.Header));
						result = flag;
					}
				}
			}
			catch (PXRedirectRequiredException)
			{
				throw;
			}
			catch (Exception e) when (this.HandleScanException(scanHeader, infoBackup, e))
			{
				result = true;
			}
			finally
			{
				this._bypassLogging = false;
			}
			return result;
		}

		/// <summary>Indicates (if set to <see langword="true" />) that the scanned value can be handled by the system.</summary>
		/// <param name="barcode">The scanned barcode value.</param>
		/// <returns>
		///   <c>The value is always <see langword="true" /> by default.</c>
		/// </returns>
		// Token: 0x06000035 RID: 53 RVA: 0x000030BC File Offset: 0x000012BC
		protected virtual bool CanHandleScan(string barcode)
		{
			return true;
		}

		// Token: 0x06000036 RID: 54 RVA: 0x000030C0 File Offset: 0x000012C0
		protected virtual bool HandleScan(string barcode)
		{
			bool flag;
			if (barcode.StartsWith("*"))
			{
				flag = this.ProcessCommand(barcode.ToUpper().Substring(1));
			}
			else if (barcode.StartsWith("@"))
			{
				flag = this.ProcessRedirect(barcode.ToUpper().Substring(1));
			}
			else
			{
				bool? flag2 = this.ProcessCustomScan(barcode);
				if (flag2 != null)
				{
					flag = flag2.Value;
				}
				else if (this.CurrentState != null)
				{
					flag = true;
					this.CurrentState.Process(barcode);
				}
				else
				{
					flag = false;
				}
			}
			if (flag && (this.Header.HasScanStateChanged.GetValueOrDefault() || this.Header.HasQuestionCodeChanged.GetValueOrDefault()))
			{
				this.PromptAndInstruct();
			}
			if (!flag)
			{
				this.ReportError("The {0} string is not a valid command or value.", new object[]
				{
					barcode
				});
			}
			return flag;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x00003194 File Offset: 0x00001394
		protected virtual bool ProcessCommand(string command)
		{
			if (command == "SAVE")
			{
				this.Save.Press();
				this.HeaderSetter.Set<string>((ScanHeader h) => h.Barcode, string.Empty);
				this._bypassLogging = true;
				return true;
			}
			if (command == "CANCEL")
			{
				base.Base.Clear();
				base.Base.SelectTimeStamp();
				this._bypassLogging = true;
				return true;
			}
			if (command == "RESET")
			{
				if (this.Header.QuestionCode != null)
				{
					this.AnswerCurrentQuestion(false);
				}
				this.Reset(true);
				this.SetDefaultState(null, Array.Empty<object>());
				this.ReportInfo("The unconfirmed entries have been cleared.", Array.Empty<object>());
				return true;
			}
			if (command == "OK")
			{
				if (this.Header.QuestionCode != null)
				{
					this.AnswerCurrentQuestion(true);
					return true;
				}
				ConfirmationState<TSelf> confirmationState = this.CurrentState as ConfirmationState<TSelf>;
				if (confirmationState != null)
				{
					confirmationState.Confirm();
					return true;
				}
				return false;
			}
			else
			{
				ScanMode<TSelf> currentMode = this.CurrentMode;
				IScanCommand scanCommand = (currentMode != null) ? currentMode.Commands.FirstOrDefault((ScanCommand<TSelf> c) => c.Code == command) : null;
				if (scanCommand == null)
				{
					return false;
				}
				if (!scanCommand.IsApplicable)
				{
					this.ReportError("The {0} ({1}) command is not available in the current state.", new object[]
					{
						scanCommand.Code,
						scanCommand.DisplayName
					});
					return true;
				}
				return scanCommand.Execute();
			}
		}

		// Token: 0x06000038 RID: 56 RVA: 0x0000334C File Offset: 0x0000154C
		protected virtual bool ProcessRedirect(string command)
		{
			ScanMode<TSelf> currentMode = this.CurrentMode;
			IScanRedirect scanRedirect = (currentMode != null) ? currentMode.Redirects.FirstOrDefault((ScanRedirect<TSelf> r) => r.Code == command && r.IsPossible) : null;
			return scanRedirect != null && scanRedirect.Redirect();
		}

		/// <summary>Provides an ability to process the input that could not be processed by any step of the barcode processing.</summary>
		/// <param name="barcode">The scanned barcode value.</param>
		// Token: 0x06000039 RID: 57 RVA: 0x00003398 File Offset: 0x00001598
		protected virtual bool? ProcessCustomScan(string barcode)
		{
			return null;
		}

		// Token: 0x0600003A RID: 58 RVA: 0x000033B0 File Offset: 0x000015B0
		protected virtual bool HandleScanException(ScanHeader headerBackup, ScanInfo infoBackup, Exception e)
		{
			PXTrace.WriteError(e);
			string errorMsg = this.ConvertExceptionToString(e);
			base.Base.Clear();
			this.HeaderView.GetSetterFor(headerBackup).Set<string>((ScanHeader h) => h.Barcode, string.Empty);
			this.HeaderView.Cache.RestoreCopy(this.HeaderView.Current, headerBackup);
			this.Info.Cache.RestoreCopy(this.Info.Current, infoBackup);
			this.ReportError(errorMsg, Array.Empty<object>());
			this.LogScan(headerBackup, headerBackup);
			return true;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x0000347C File Offset: 0x0000167C
		private void AnswerCurrentQuestion(bool answer)
		{
			ScanMode<TSelf> currentMode = this.CurrentMode;
			ScanQuestion<TSelf> scanQuestion = (currentMode != null) ? currentMode.Questions.FirstOrDefault((ScanQuestion<TSelf> c) => c.Code == this.Header.QuestionCode) : null;
			if (scanQuestion == null)
			{
				throw new InvalidOperationException(string.Concat(new string[]
				{
					"The ",
					this.Header.QuestionCode,
					" question is not present in the ",
					this.CurrentMode.Description,
					" mode."
				}));
			}
			this.HeaderSetter.Set<string>((ScanHeader h) => h.QuestionCode, null);
			this.HeaderSetter.Set<bool?>((ScanHeader h) => h.HasQuestionCodeChanged, new bool?(true));
			scanQuestion.Answer(answer);
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00003591 File Offset: 0x00001791
		public void SaveChanges()
		{
			if (this.MultiCommit)
			{
				this._saveRequested = true;
				return;
			}
			this.Save.Press();
		}

		// Token: 0x0600003D RID: 61 RVA: 0x000035AE File Offset: 0x000017AE
		public void Reset(bool fullReset = false)
		{
			ScanMode<TSelf> currentMode = this.CurrentMode;
			if (currentMode == null)
			{
				return;
			}
			currentMode.Reset(fullReset);
		}

		// Token: 0x0600003E RID: 62 RVA: 0x000035C1 File Offset: 0x000017C1
		public void SetDefaultState(string message = null, params object[] args)
		{
			ScanMode<TSelf> currentMode = this.CurrentMode;
			if (currentMode == null)
			{
				return;
			}
			currentMode.SetDefaultState(message, args);
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000035D8 File Offset: 0x000017D8
		public TScanState FindState<TScanState>(bool exact = false) where TScanState : ScanState<TSelf>
		{
			ScanMode<TSelf> currentMode = this.CurrentMode;
			if (currentMode == null)
			{
				return default(TScanState);
			}
			return currentMode.FindState<TScanState>(exact);
		}

		// Token: 0x06000040 RID: 64 RVA: 0x000035FF File Offset: 0x000017FF
		public IScanState FindState(string state)
		{
			ScanMode<TSelf> currentMode = this.CurrentMode;
			if (currentMode == null)
			{
				return null;
			}
			return currentMode.FindState(state);
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00003613 File Offset: 0x00001813
		public bool HasActive(string state)
		{
			ScanMode<TSelf> currentMode = this.CurrentMode;
			return currentMode != null && currentMode.HasActive(state);
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00003627 File Offset: 0x00001827
		public bool HasActive<TScanState>() where TScanState : ScanState<TSelf>
		{
			ScanMode<TSelf> currentMode = this.CurrentMode;
			return currentMode != null && currentMode.HasActive<TScanState>();
		}

		// Token: 0x06000043 RID: 67 RVA: 0x0000363A File Offset: 0x0000183A
		public ScanMode<TSelf>.Validator<TEntity> TryValidate<TEntity>(TEntity entity)
		{
			return new ScanMode<TSelf>.Validator<TEntity>(this.CurrentMode, entity);
		}

		// Token: 0x06000044 RID: 68 RVA: 0x00003648 File Offset: 0x00001848
		public ScanMode<TSelf>.EntityGetter<TEntity> TryGet<TEntity>()
		{
			return new ScanMode<TSelf>.EntityGetter<TEntity>(this.CurrentMode);
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00003655 File Offset: 0x00001855
		public void Clear(string state, bool when = true)
		{
			ScanMode<TSelf> currentMode = this.CurrentMode;
			if (currentMode == null)
			{
				return;
			}
			currentMode.Clear(state, when);
		}

		// Token: 0x06000046 RID: 70 RVA: 0x00003669 File Offset: 0x00001869
		public void Clear<TScanState>(bool when = true) where TScanState : ScanState<TSelf>
		{
			ScanMode<TSelf> currentMode = this.CurrentMode;
			if (currentMode == null)
			{
				return;
			}
			currentMode.Clear<TScanState>(when);
		}

		// Token: 0x06000047 RID: 71 RVA: 0x0000367C File Offset: 0x0000187C
		public bool TryProcessBy<TScanState>(string barcode, StateSubstitutionRule substitutionRule = StateSubstitutionRule.SuppressAll) where TScanState : ScanState<TSelf>
		{
			ScanMode<TSelf> currentMode = this.CurrentMode;
			return currentMode != null && currentMode.TryProcessBy<TScanState>(barcode, substitutionRule);
		}

		// Token: 0x06000048 RID: 72 RVA: 0x00003694 File Offset: 0x00001894
		internal bool TryProcessByMode<TScanMode>(string barcode, StateSubstitutionRule substitutionRule = StateSubstitutionRule.SuppressAll) where TScanMode : ScanMode<TSelf>
		{
			TScanMode tscanMode = this.ScanModes.OfType<TScanMode>().FirstOrDefault((TScanMode m) => m.IsActive);
			if (tscanMode != null)
			{
				ScanState<TSelf> defaultState = tscanMode.DefaultState;
				if (defaultState != null)
				{
					return tscanMode.TryProcessBy(defaultState.Code, barcode, substitutionRule);
				}
			}
			return false;
		}

		// Token: 0x06000049 RID: 73 RVA: 0x000036FD File Offset: 0x000018FD
		public bool TryProcessBy(string state, string barcode, StateSubstitutionRule substitutionRule = StateSubstitutionRule.SuppressAll)
		{
			ScanMode<TSelf> currentMode = this.CurrentMode;
			return currentMode != null && currentMode.TryProcessBy(state, barcode, substitutionRule);
		}

		// Token: 0x0600004A RID: 74 RVA: 0x00003714 File Offset: 0x00001914
		public AbsenceHandling ProcessByMode<TScanMode>(string barcode) where TScanMode : ScanMode<TSelf>
		{
			if (!this.TryGet<object>().ByMode<TScanMode>(barcode))
			{
				return AbsenceHandling.Skipped;
			}
			if (!this.TryProcessByMode<TScanMode>(barcode, (StateSubstitutionRule)254))
			{
				return AbsenceHandling.Failed;
			}
			this.SetScanMode<TScanMode>();
			this.CurrentState.Process(barcode);
			return AbsenceHandling.Done;
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00003764 File Offset: 0x00001964
		public void SetScanMode(string mode)
		{
			this.ScanModes.First((ScanMode<TSelf> m) => m.Code == mode && m.IsActive).TakeOver();
		}

		// Token: 0x0600004C RID: 76 RVA: 0x0000379A File Offset: 0x0000199A
		public void SetScanMode<TScanMode>() where TScanMode : ScanMode<TSelf>
		{
			this.ScanModes.OfType<TScanMode>().First((TScanMode m) => m.IsActive).TakeOver();
		}

		// Token: 0x0600004D RID: 77 RVA: 0x000037D5 File Offset: 0x000019D5
		public void SetScanState<TScanState>(string message = null, params object[] args) where TScanState : ScanState<TSelf>
		{
			this.SetScanState(this.CurrentMode.FindState<TScanState>(false).Code, message, args, null);
		}

		// Token: 0x0600004E RID: 78 RVA: 0x000037F6 File Offset: 0x000019F6
		public void SetScanState(string state, string message = null, params object[] args)
		{
			this.SetScanState(state, message, args, null);
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00003804 File Offset: 0x00001A04
		private void SetScanState(string state, string message, object[] args, Action onTransition)
		{
			IScanState scanState = this.CurrentMode.FindState(state);
			if (scanState == null)
			{
				throw new InvalidOperationException(string.Concat(new string[]
				{
					"The ",
					state,
					" state is not present in the ",
					this.CurrentMode.Description,
					" mode."
				}));
			}
			this.HeaderSetter.Set<bool?>((ScanHeader x) => x.HasScanStateChanged, new bool?(true));
			this.HeaderSetter.Set<string>((ScanHeader x) => x.PrevScanState, this.Header.ScanState);
			IScanState currentState = this.CurrentState;
			if (currentState != null)
			{
				currentState.Dismiss();
			}
			if (onTransition != null)
			{
				onTransition();
			}
			this.HeaderSetter.Set<string>((ScanHeader x) => x.ScanState, scanState.Code);
			if (message != null)
			{
				this.ReportInfo(message, args);
			}
			this.ClearErrors();
			scanState.TakeOver();
			if (this.CurrentState.Code == scanState.Code && (!scanState.IsActive || scanState.IsSkippable))
			{
				scanState.MoveToNextState();
				return;
			}
		}

		// Token: 0x06000050 RID: 80 RVA: 0x000039AD File Offset: 0x00001BAD
		public void DispatchNext(string message = null, params object[] args)
		{
			this.DispatchNextFrom(this.CurrentState.Code, message, args);
		}

		// Token: 0x06000051 RID: 81 RVA: 0x000039C4 File Offset: 0x00001BC4
		public void DispatchNextFrom(string state, string message = null, params object[] args)
		{
			IScanTransition[] source = (from IScanTransition tr in this.CurrentMode.Transitions
			where tr.SourceState == state && tr.IsApplicable
			select tr).ToArray<IScanTransition>();
			IScanTransition anyTransition = source.FirstOrDefault<IScanTransition>();
			if (anyTransition != null)
			{
				this.SetScanState(anyTransition.TargetState, message, args, delegate()
				{
					anyTransition.Transit();
				});
				return;
			}
			if (state == "CONF")
			{
				this.SetDefaultState(message, args);
				return;
			}
			IScanState scanState = this.CurrentMode.FindState("CONF");
			if (scanState != null)
			{
				this.SetScanState(scanState.Code, message, args, null);
				return;
			}
			throw new InvalidOperationException("The " + state + " state has no applicable transitions to other states.");
		}

		/// <summary>Reconfigures a created mode before its initialization, which means that its initialization could be altered.</summary>
		/// <param name="original">The original scan mode.</param>
		// Token: 0x06000052 RID: 82 RVA: 0x00003A92 File Offset: 0x00001C92
		protected virtual ScanMode<TSelf> DecorateScanMode(ScanMode<TSelf> original)
		{
			return original;
		}

		/// <summary>Reconfigure a created mode after its initialization, which means that its behavior could be altered based on a component presence.</summary>
		/// <param name="original">The original scan mode.</param>
		// Token: 0x06000053 RID: 83 RVA: 0x00003A95 File Offset: 0x00001C95
		protected virtual ScanMode<TSelf> LateDecorateScanMode(ScanMode<TSelf> original)
		{
			return original;
		}

		/// <summary>Reconfigures a created state, which means that its behavior could be altered.</summary>
		/// <param name="original">The original scan state.</param>
		// Token: 0x06000054 RID: 84 RVA: 0x00003A98 File Offset: 0x00001C98
		protected virtual ScanState<TSelf> DecorateScanState(ScanState<TSelf> original)
		{
			return original;
		}

		/// <summary>Reconfigures a created command, which means that its behavior could be altered.</summary>
		/// <param name="original">The original scan command.</param>
		// Token: 0x06000055 RID: 85 RVA: 0x00003A9B File Offset: 0x00001C9B
		protected virtual ScanCommand<TSelf> DecorateScanCommand(ScanCommand<TSelf> original)
		{
			return original;
		}

		/// <summary>Reconfigures a created mode redirect, which means that its behavior can be altered.</summary>
		/// <param name="original">The original mode redirect.</param>
		// Token: 0x06000056 RID: 86 RVA: 0x00003A9E File Offset: 0x00001C9E
		protected virtual ScanRedirect<TSelf> DecorateScanRedirect(ScanRedirect<TSelf> original)
		{
			return original;
		}

		/// <summary>
		/// Reconfigures a created transition, which means that its behavior could be altered.
		/// </summary>
		/// <param name="original">The original scan transition.</param>
		// Token: 0x06000057 RID: 87 RVA: 0x00003AA1 File Offset: 0x00001CA1
		protected virtual ScanTransition<TSelf> DecorateScanTransition(ScanTransition<TSelf> original)
		{
			return original;
		}

		/// <summary>Reconfigures a created question, which means that its behavior can be altered.</summary>
		/// <param name="original">The original scan question.</param>
		// Token: 0x06000058 RID: 88 RVA: 0x00003AA4 File Offset: 0x00001CA4
		protected virtual ScanQuestion<TSelf> DecorateScanQuestion(ScanQuestion<TSelf> original)
		{
			return original;
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00003AA7 File Offset: 0x00001CA7
		ScanState<TSelf> IBarcodeDrivenStateMachineComponentDecorator<!0>.DecorateScanState(ScanState<TSelf> original)
		{
			return this.DecorateScanState(original);
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00003AB0 File Offset: 0x00001CB0
		ScanCommand<TSelf> IBarcodeDrivenStateMachineComponentDecorator<!0>.DecorateScanCommand(ScanCommand<TSelf> original)
		{
			return this.DecorateScanCommand(original);
		}

		// Token: 0x0600005B RID: 91 RVA: 0x00003AB9 File Offset: 0x00001CB9
		ScanRedirect<TSelf> IBarcodeDrivenStateMachineComponentDecorator<!0>.DecorateScanRedirect(ScanRedirect<TSelf> original)
		{
			return this.DecorateScanRedirect(original);
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00003AC2 File Offset: 0x00001CC2
		ScanTransition<TSelf> IBarcodeDrivenStateMachineComponentDecorator<!0>.DecorateScanTransition(ScanTransition<TSelf> original)
		{
			return this.DecorateScanTransition(original);
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00003ACB File Offset: 0x00001CCB
		ScanQuestion<TSelf> IBarcodeDrivenStateMachineComponentDecorator<!0>.DecorateScanQuestion(ScanQuestion<TSelf> original)
		{
			return this.DecorateScanQuestion(original);
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00003AD4 File Offset: 0x00001CD4
		internal void PromptAndInstruct()
		{
			IScanState currentState = this.CurrentState;
			if (currentState != null)
			{
				this.Prompt(currentState.Prompt, Array.Empty<object>());
				IScanQuestion currentQuestion = this.CurrentQuestion;
				this.Ask((currentQuestion != null) ? currentQuestion.Prompt : null, Array.Empty<object>());
				this.Instruct(currentState.Instructions, Array.Empty<object>());
			}
		}

		// Token: 0x0600005F RID: 95 RVA: 0x00003B2A File Offset: 0x00001D2A
		private void Prompt(string promptMsg, params object[] args)
		{
			this.AlterScanInfo("PRM", promptMsg, args);
		}

		// Token: 0x06000060 RID: 96 RVA: 0x00003B39 File Offset: 0x00001D39
		private void Ask(string questionMsg, params object[] args)
		{
			this.AlterScanInfo("QST", questionMsg, args);
		}

		// Token: 0x06000061 RID: 97 RVA: 0x00003B48 File Offset: 0x00001D48
		private void Instruct(string instructMsg, params object[] args)
		{
			this.AlterScanInfo("INS", instructMsg, args);
		}

		// Token: 0x06000062 RID: 98 RVA: 0x00003B57 File Offset: 0x00001D57
		public void ReportInfo(string infoMsg, params object[] args)
		{
			this.AlterScanInfo("INF", infoMsg, args);
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00003B66 File Offset: 0x00001D66
		public void ReportWarning(string warnMsg, params object[] args)
		{
			this.AlterScanInfo("WRN", warnMsg, args);
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00003B75 File Offset: 0x00001D75
		public void ReportComplete(string completeMsg, params object[] args)
		{
			this.AlterScanInfo("COM", completeMsg, args);
		}

		// Token: 0x06000065 RID: 101 RVA: 0x00003B84 File Offset: 0x00001D84
		public void ReportError(string errorMsg, params object[] args)
		{
			this.AlterScanInfo("ERR", errorMsg, args);
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00003B93 File Offset: 0x00001D93
		public void ClearErrors()
		{
			this.AlterScanInfo("NON", null, null);
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00003BA4 File Offset: 0x00001DA4
		protected virtual void AlterScanInfo(string messageType, string message, params object[] args)
		{
			BarcodeDrivenStateMachine<TSelf, TGraph>.<>c__DisplayClass133_0 CS$<>8__locals1;
			CS$<>8__locals1.messageType = messageType;
			CS$<>8__locals1.<>4__this = this;
			if (CS$<>8__locals1.messageType == "PRM")
			{
				this.Info.Cache.SetValueExt<ScanInfo.prompt>(this.Info.Current, (message != null) ? this.Localize(message, args) : null);
			}
			else if (CS$<>8__locals1.messageType == "QST")
			{
				this.Info.Cache.SetValueExt<ScanInfo.question>(this.Info.Current, (message != null) ? this.Localize(message, args) : null);
			}
			else if (CS$<>8__locals1.messageType == "INS")
			{
				this.Info.Cache.SetValueExt<ScanInfo.instructions>(this.Info.Current, (message != null) ? this.Localize(message, args) : null);
			}
			else if (CS$<>8__locals1.messageType != "NON")
			{
				this.Info.Cache.SetValueExt<ScanInfo.message>(this.Info.Current, this.Localize(message, args));
				this.Info.Cache.SetValueExt<ScanInfo.messageType>(this.Info.Current, CS$<>8__locals1.messageType);
			}
			if (base.Base.IsMobile)
			{
				this.Info.Cache.RaiseExceptionHandling<ScanInfo.message>(this.Info.Current, this.Info.Current.Message, this.<AlterScanInfo>g__GetException|133_0<ScanInfo.message>(this.Info.Current, ref CS$<>8__locals1));
				return;
			}
			this.HeaderView.Cache.RaiseExceptionHandling<ScanHeader.message>(this.Header, this.FullMessage, this.<AlterScanInfo>g__GetException|133_0<ScanHeader.message>(this.Header, ref CS$<>8__locals1));
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00003D51 File Offset: 0x00001F51
		[Obsolete]
		protected virtual void _(Events.FieldSelecting<ScanHeader, ScanHeader.message> e)
		{
			e.ReturnValue = this.FullMessage;
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000069 RID: 105 RVA: 0x00003D60 File Offset: 0x00001F60
		[Obsolete]
		public string FullMessage
		{
			get
			{
				return string.Concat(new string[]
				{
					this.Info.Current.Mode,
					Environment.NewLine,
					this.Info.Current.Message,
					Environment.NewLine,
					this.Info.Current.PromptCombined
				});
			}
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00003DC4 File Offset: 0x00001FC4
		public string Localize(string strMessage, params object[] args)
		{
			object[] args2 = args.Select(delegate(object x)
			{
				if (x is decimal)
				{
					string text = ((decimal)x).ToString();
					if (text != null && text.Contains(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator))
					{
						return text.TrimEnd(new char[]
						{
							'0'
						}).TrimEnd(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator.ToCharArray());
					}
				}
				if (x == null)
				{
					return null;
				}
				return x.ToString().Trim();
			}).ToArray<string>();
			return PXMessages.LocalizeFormatNoPrefix(strMessage, args2);
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00003E04 File Offset: 0x00002004
		protected virtual void LogScan(ScanHeader headerBefore, ScanHeader headerAfter)
		{
			this.Logs.Cache.Insert(new ScanLog
			{
				ScanTime = new DateTime?(PXTimeZoneInfo.Now),
				Scan = headerBefore.Barcode,
				Mode = this._previousMode,
				Message = this.Info.Current.Message,
				MessageType = this.Info.Current.MessageType,
				Question = this._previousQuestion,
				NewQuestion = this.Info.Current.Question,
				Instructions = this._previousInstructions,
				NewInstructions = this.Info.Current.Instructions,
				Prompt = this._previousPrompt,
				NewPrompt = this.Info.Current.Prompt,
				PromptCombined = this._previousPromptCombined,
				NewPromptCombined = this.Info.Current.PromptCombined,
				HeaderStateBefore = headerBefore,
				HeaderStateAfter = headerAfter
			});
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00003F14 File Offset: 0x00002114
		public string GetCaption()
		{
			IEnumerable<string> enumerable = (from msg in this.GetCaptionComponents()
			where !string.IsNullOrEmpty(msg)
			select msg).With(delegate(IEnumerable<string> msgs)
			{
				if (!msgs.Any<string>())
				{
					return null;
				}
				return msgs;
			});
			if (enumerable == null)
			{
				return null;
			}
			return enumerable.Aggregate((string acc, string msg) => this.Localize("{0} - {1}", new object[]
			{
				acc,
				msg
			}));
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00003F86 File Offset: 0x00002186
		protected virtual IEnumerable<string> GetCaptionComponents()
		{
			if (this.ScanModes.Count<ScanMode<TSelf>>() > 1)
			{
				yield return this.Info.Current.ModeCaption;
			}
			yield break;
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00003F96 File Offset: 0x00002196
		public void Warn<TScanQuestion>(string message = null, params object[] args) where TScanQuestion : ScanQuestion<TSelf>
		{
			this.ApplyQuestion(this.CurrentMode.Questions.OfType<TScanQuestion>().FirstOrDefault<TScanQuestion>().Code, true, message, args);
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00003FC0 File Offset: 0x000021C0
		public void Warn(string questionCode, string message = null, params object[] args)
		{
			this.ApplyQuestion(questionCode, true, message, args);
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00003FCC File Offset: 0x000021CC
		public void Ask<TScanQuestion>(string message = null, params object[] args) where TScanQuestion : ScanQuestion<TSelf>
		{
			this.ApplyQuestion(this.CurrentMode.Questions.OfType<TScanQuestion>().FirstOrDefault<TScanQuestion>().Code, false, message, args);
		}

		// Token: 0x06000071 RID: 113 RVA: 0x00003FF6 File Offset: 0x000021F6
		public void Ask(string questionCode, string message = null, params object[] args)
		{
			this.ApplyQuestion(questionCode, false, message, args);
		}

		// Token: 0x06000072 RID: 114 RVA: 0x00004004 File Offset: 0x00002204
		protected void ApplyQuestion(string questionCode, bool isWarning, string message = null, params object[] args)
		{
			IScanQuestion scanQuestion = this.CurrentMode.Questions.FirstOrDefault((ScanQuestion<TSelf> q) => q.Code == questionCode);
			if (scanQuestion != null)
			{
				if (message != null)
				{
					if (isWarning)
					{
						this.ReportWarning(message, args);
					}
					else
					{
						this.ReportInfo(message, args);
					}
				}
				this.HeaderSetter.Set<bool?>((ScanHeader h) => h.HasQuestionCodeChanged, new bool?(true));
				this.HeaderSetter.Set<string>((ScanHeader h) => h.PrevQuestionCode, this.Header.QuestionCode);
				this.HeaderSetter.Set<string>((ScanHeader h) => h.QuestionCode, scanQuestion.Code);
				return;
			}
			throw new InvalidOperationException(string.Concat(new string[]
			{
				"The ",
				questionCode,
				" question is not present in the ",
				this.CurrentMode.Description,
				" mode."
			}));
		}

		// Token: 0x06000073 RID: 115 RVA: 0x00004187 File Offset: 0x00002387
		public void RevokeQuestion<TScanQuestion>() where TScanQuestion : ScanQuestion<TSelf>
		{
			this.RevokeQuestion(this.CurrentMode.Questions.OfType<TScanQuestion>().FirstOrDefault<TScanQuestion>().Code);
		}

		// Token: 0x06000074 RID: 116 RVA: 0x000041B0 File Offset: 0x000023B0
		public void RevokeQuestion(string questionCode)
		{
			if (this.Header.QuestionCode != null && this.Header.QuestionCode == questionCode)
			{
				this.HeaderSetter.Set<string>((ScanHeader h) => h.QuestionCode, null);
			}
		}

		/// Overrides <seealso cref="M:PX.Data.PXGraph.Clear" />
		// Token: 0x06000075 RID: 117 RVA: 0x00004225 File Offset: 0x00002425
		[PXOverride]
		public void Clear(Action base_Clear)
		{
			this._clearHeader = true;
			base_Clear();
			this._clearHeader = false;
		}

		/// Overrides <seealso cref="M:PX.Data.PXGraph.Clear(PX.Data.PXClearOption)" />
		// Token: 0x06000076 RID: 118 RVA: 0x0000423C File Offset: 0x0000243C
		[PXOverride]
		public void Clear(PXClearOption option, Action<PXClearOption> base_Clear)
		{
			if (this._clearHeader)
			{
				this.OnBeforeFullClear();
				ScanHeader header = this.Header;
				string text = (header != null) ? header.Mode : null;
				base_Clear(option);
				ScanHeader header2 = this.Header;
				if (text != null && header2.Mode != text)
				{
					this.SetScanMode(text);
					this.PromptAndInstruct();
					return;
				}
			}
			else
			{
				ScanHeader header3 = this.Header;
				ScanInfo scanInfo = this.Info.Current;
				ScanLog[] array = this.Logs.Cache.Cached.RowCast<ScanLog>().ToArray<ScanLog>();
				base_Clear(option);
				if (header3 != null && this.HeaderView.Cache.Locate(header3) == null)
				{
					this.HeaderView.Cache.SetStatus(header3, PXEntryStatus.Inserted);
				}
				if (scanInfo != null && this.Info.Cache.Locate(scanInfo) == null)
				{
					this.Info.Cache.SetStatus(scanInfo, PXEntryStatus.Inserted);
				}
				if (this.Logs.Cache.Cached.Count() == 0L)
				{
					foreach (ScanLog item in array)
					{
						this.Logs.Cache.Insert(item);
					}
				}
			}
		}

		// Token: 0x06000077 RID: 119 RVA: 0x0000436B File Offset: 0x0000256B
		protected virtual void OnBeforeFullClear()
		{
		}

		// Token: 0x06000078 RID: 120 RVA: 0x0000436D File Offset: 0x0000256D
		public ScanTransition<TSelf> Transition(Func<ScanTransition<TSelf>, ScanTransition<TSelf>> config)
		{
			return config(new ScanTransition<TSelf>());
		}

		// Token: 0x06000079 RID: 121 RVA: 0x0000437A File Offset: 0x0000257A
		public IEnumerable<ScanTransition<TSelf>> StateFlow(Func<ScanStateFlow<TSelf>.IFrom, IEnumerable<ScanTransition<TSelf>>> config)
		{
			return config(new ScanStateFlow<TSelf>());
		}

		/// <summary>Starts an awaitable long-running operation upon a document (usually within a ScanCommand&lt;TScanBasis&gt;).</summary>
		/// <typeparam name="TData">The type of the data.</typeparam>
		/// <param name="action">The action.</param>
		/// <remarks>You should configure the long-running operation by using the following methods:
		/// <see cref="M:PX.BarcodeProcessing.ScanLongRunAwaiter`2.WithDescription(System.String,System.Object[])" />,
		/// <see cref="M:PX.BarcodeProcessing.ScanLongRunAwaiter`2.ActualizeDataBy(System.Func{`0,`1,`1})" />,
		/// <see cref="M:PX.BarcodeProcessing.ScanLongRunAwaiter`2.OnSuccess(System.Action{PX.BarcodeProcessing.ScanLongRunAwaiter{`0,`1}.ISuccessProcessor})" />,
		/// <see cref="M:PX.BarcodeProcessing.ScanLongRunAwaiter`2.OnFail(System.Action{PX.BarcodeProcessing.ScanLongRunAwaiter{`0,`1}.IResultProcessor})" />, and
		/// <see cref="M:PX.BarcodeProcessing.ScanLongRunAwaiter`2.BeginAwait(`1)" />.</remarks>
		// Token: 0x0600007A RID: 122 RVA: 0x00004387 File Offset: 0x00002587
		public ScanLongRunAwaiter<TSelf, TData> WaitFor<TData>(Action<TSelf, TData> action)
		{
			return new ScanLongRunAwaiter<TSelf, TData>(this.Self, delegate(TSelf self)
			{
				self.RefreshState();
			})
			{
				AwaitLongRunManually = this._awaitLongRunManually,
				LongRunAction = action
			};
		}

		/// <summary>
		/// Starts an asynchronous long-running operation upon a document (usually within a ScanCommand&lt;TScanBasis&gt;).
		/// </summary>
		/// <typeparam name="TData">The type of the data.</typeparam>
		/// <param name="asyncAction">The asynchronous action.</param>
		/// <returns></returns>
		// Token: 0x0600007B RID: 123 RVA: 0x000043C6 File Offset: 0x000025C6
		public ScanLongRunAwaiter<TSelf, TData> AwaitFor<TData>(Func<TSelf, TData, CancellationToken, Task> asyncAction)
		{
			return new ScanLongRunAwaiter<TSelf, TData>(this.Self, delegate(TSelf self)
			{
				self.RefreshState();
			})
			{
				AwaitLongRunManually = this._awaitLongRunManually,
				AsyncLongRunAction = asyncAction
			};
		}

		// Token: 0x0600007C RID: 124 RVA: 0x00004405 File Offset: 0x00002605
		public static implicit operator TGraph(BarcodeDrivenStateMachine<TSelf, TGraph> bdsm)
		{
			return bdsm.Graph;
		}

		// Token: 0x0600007D RID: 125 RVA: 0x0000440D File Offset: 0x0000260D
		public static implicit operator PXGraph(BarcodeDrivenStateMachine<TSelf, TGraph> bdsm)
		{
			return bdsm.Graph;
		}

		// Token: 0x0600007E RID: 126 RVA: 0x0000441C File Offset: 0x0000261C
		public static void WithSuppressedRedirects(Action action)
		{
			try
			{
				action();
			}
			catch (PXBaseRedirectException)
			{
			}
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00004444 File Offset: 0x00002644
		public static Task WithSuppressedRedirects(Func<Task> action)
		{
			BarcodeDrivenStateMachine<TSelf, TGraph>.<WithSuppressedRedirects>d__166 <WithSuppressedRedirects>d__;
			<WithSuppressedRedirects>d__.<>t__builder = AsyncTaskMethodBuilder.Create();
			<WithSuppressedRedirects>d__.action = action;
			<WithSuppressedRedirects>d__.<>1__state = -1;
			<WithSuppressedRedirects>d__.<>t__builder.Start<BarcodeDrivenStateMachine<TSelf, TGraph>.<WithSuppressedRedirects>d__166>(ref <WithSuppressedRedirects>d__);
			return <WithSuppressedRedirects>d__.<>t__builder.Task;
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00004487 File Offset: 0x00002687
		public string SightOf<TField>() where TField : IBqlField
		{
			object stateExt = this.HeaderView.Cache.GetStateExt<TField>(this.Header);
			if (stateExt == null)
			{
				return null;
			}
			return stateExt.ToString();
		}

		// Token: 0x06000081 RID: 129 RVA: 0x000044AA File Offset: 0x000026AA
		public string SightOf<TField>(IBqlTable row) where TField : IBqlField
		{
			object stateExt = this.Graph.Caches[BqlCommand.GetItemType<TField>()].GetStateExt<TField>(row);
			if (stateExt == null)
			{
				return null;
			}
			return stateExt.ToString();
		}

		// Token: 0x06000082 RID: 130 RVA: 0x000044D7 File Offset: 0x000026D7
		public bool IsValid<TField>(object value, out string error) where TField : IBqlField
		{
			return this.IsValid<TField, ScanHeader>(this.Header, value, out error);
		}

		// Token: 0x06000083 RID: 131 RVA: 0x000044E8 File Offset: 0x000026E8
		public bool IsValid<TField, TTable>(TTable instance, object value, out string error) where TField : IBqlField where TTable : class, IBqlTable, new()
		{
			bool result;
			try
			{
				base.Base.Caches<TTable>().RaiseFieldVerifying<TField>(instance, ref value);
				error = null;
				result = true;
			}
			catch (PXSetPropertyException ex)
			{
				error = ex.MessageNoPrefix;
				result = false;
			}
			return result;
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00004538 File Offset: 0x00002738
		public bool HasUIErrors<TTable>(TTable row, out IReadOnlyDictionary<string, string> uiErrors) where TTable : class, IBqlTable, new()
		{
			PXCache<TTable> cache = this.Graph.Caches<TTable>();
			uiErrors = PXUIFieldAttribute.GetErrors(cache, row, new PXErrorLevel[]
			{
				PXErrorLevel.Error
			}).Concat(PXUIFieldAttribute.GetErrors(cache, row, new PXErrorLevel[]
			{
				PXErrorLevel.RowError
			})).ToDictionary<string, string>();
			return uiErrors.Any<KeyValuePair<string, string>>();
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00004598 File Offset: 0x00002798
		public bool HasUIErrors<TTable>(TTable row, out FlowStatus error) where TTable : class, IBqlTable, new()
		{
			IReadOnlyDictionary<string, string> readOnlyDictionary;
			if (this.HasUIErrors<TTable>(row, out readOnlyDictionary))
			{
				if (readOnlyDictionary.Count == 1)
				{
					error = FlowStatus.Fail(readOnlyDictionary.Single<KeyValuePair<string, string>>().Value, Array.Empty<object>());
				}
				else
				{
					string errorList = string.Join(Environment.NewLine, from e in readOnlyDictionary
					select this.Localize("An error occurred during processing of the field {0}: {1}.", new object[]
					{
						e.Key,
						e.Value
					}));
					error = FlowStatus.Fail("The document could not be updated due to multiple errors. See the trace for details.", Array.Empty<object>()).WithPostAction(delegate
					{
						PXTrace.WriteError(errorList);
					});
				}
				return true;
			}
			error = FlowStatus.Ok;
			return false;
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00004638 File Offset: 0x00002838
		public bool HasFault<T>(T entity, Func<T, Validation?> prevValidation, out Validation fault)
		{
			return this.HasFault<T>(entity, delegate(T e)
			{
				Validation? validation = prevValidation(e);
				if (validation == null)
				{
					return Validation.Ok;
				}
				return validation.GetValueOrDefault();
			}, out fault);
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00004668 File Offset: 0x00002868
		public bool HasFault<T>(T entity, Func<T, Validation> prevValidation, out Validation fault)
		{
			fault = prevValidation(entity);
			bool? isError = fault.IsError;
			bool flag = false;
			return !(isError.GetValueOrDefault() == flag & isError != null);
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000088 RID: 136 RVA: 0x000046A0 File Offset: 0x000028A0
		public bool IsWithinReset
		{
			get
			{
				ScanHeader header = this.Header;
				return ((header != null) ? header.Barcode : null) != null && this.Header.Barcode.StartsWith("*") && this.Header.Barcode.Trim().ToUpper().Substring(1) == "RESET";
			}
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00004700 File Offset: 0x00002900
		public string ConvertExceptionToString(Exception e)
		{
			string text = e.Message;
			PXOuterException ex = e as PXOuterException;
			if (ex != null)
			{
				if (ex.InnerMessages.Length != 0)
				{
					text = text + Environment.NewLine + string.Join(Environment.NewLine, ex.InnerMessages);
				}
				else if (ex.Row != null)
				{
					text = text + Environment.NewLine + string.Join(Environment.NewLine, from kvp in PXUIFieldAttribute.GetErrors(base.Base.Caches[ex.Row.GetType()], ex.Row, Array.Empty<PXErrorLevel>())
					select kvp.Value);
				}
			}
			return text;
		}

		/// Overrides <seealso cref="P:PX.Data.PXGraph.PrimaryItemType" />
		// Token: 0x0600008A RID: 138 RVA: 0x000047BB File Offset: 0x000029BB
		[PXOverride]
		public Type get_PrimaryItemType(Func<Type> base_PrimaryItemType)
		{
			return typeof(ScanHeader);
		}

		/// Overrides <seealso cref="P:PX.Data.PXGraph.PrimaryView" />
		// Token: 0x0600008B RID: 139 RVA: 0x000047C7 File Offset: 0x000029C7
		[PXOverride]
		public string get_PrimaryView(Func<string> base_PrimaryView)
		{
			return "HeaderView";
		}

		// Token: 0x0600008C RID: 140 RVA: 0x000047D0 File Offset: 0x000029D0
		private void Mobile_RestoreHeaderOnFirstOpen(bool prepare)
		{
			if (prepare)
			{
				if (this.HeaderView.Current != null && !string.IsNullOrEmpty(this.HeaderView.Current.Mode))
				{
					this._restoreHeaderOnFirstOpen = true;
					return;
				}
			}
			else if (this._restoreHeaderOnFirstOpen)
			{
				this.GetDefaultMode().TakeOver();
			}
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00004820 File Offset: 0x00002A20
		private void Mobile_DisplayNameToPromptHack(Events.FieldSelecting<ScanHeader, ScanHeader.barcode> e)
		{
			if (this._bypassBarcodeFieldSelecting)
			{
				return;
			}
			try
			{
				this._bypassBarcodeFieldSelecting = true;
				PXFieldState pxfieldState = (PXFieldState)e.Cache.GetStateExt<ScanHeader.barcode>(e.Row);
				if (pxfieldState != null && e.Row != null)
				{
					pxfieldState.DisplayName = this.Info.Current.Prompt;
				}
				e.ReturnState = pxfieldState;
			}
			finally
			{
				this._bypassBarcodeFieldSelecting = false;
				e.Cancel = true;
			}
		}

		// Token: 0x0600008E RID: 142 RVA: 0x000048A0 File Offset: 0x00002AA0
		private void Mobile_AutoCallbackHack(string barcode)
		{
			if (!string.IsNullOrEmpty(barcode) && !this._awaitLongRunManually)
			{
				this._awaitLongRunManually = true;
				try
				{
					this.Scan.Press();
				}
				catch (PXRedirectRequiredException)
				{
					this.ReportError("The {0} string is not a valid command or value.", new object[]
					{
						barcode
					});
				}
				finally
				{
					this._awaitLongRunManually = false;
				}
			}
		}

		// Token: 0x0600008F RID: 143 RVA: 0x00004910 File Offset: 0x00002B10
		[DebuggerStepThrough]
		[DebuggerStepperBoundary]
		public TExt Get<TExt>() where TExt : PXGraphExtension<TGraph>
		{
			return this.Get<TExt>(false);
		}

		// Token: 0x06000090 RID: 144 RVA: 0x0000491C File Offset: 0x00002B1C
		[DebuggerStepThrough]
		[DebuggerStepperBoundary]
		public TExt Get<TExt>(bool skipCache) where TExt : PXGraphExtension<TGraph>
		{
			if (skipCache)
			{
				return this.Graph.FindImplementation<TExt>();
			}
			PXGraphExtension<TGraph> pxgraphExtension;
			if (this.ExtensionsCache.TryGetValue(typeof(TExt), out pxgraphExtension))
			{
				return (TExt)((object)pxgraphExtension);
			}
			TExt text = this.Graph.FindImplementation<TExt>();
			this.ExtensionsCache.Add(typeof(TExt), text);
			return text;
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00004A64 File Offset: 0x00002C64
		[CompilerGenerated]
		private PXSetPropertyException <AlterScanInfo>g__GetException|133_0<TMessageField>(IBqlTable row, ref BarcodeDrivenStateMachine<TSelf, TGraph>.<>c__DisplayClass133_0 A_2) where TMessageField : IBqlField
		{
			string messageType = A_2.messageType;
			if (messageType != null)
			{
				int length = messageType.Length;
				if (length == 3)
				{
					char c = messageType[0];
					if (c <= 'E')
					{
						if (c != 'C')
						{
							if (c != 'E')
							{
								goto IL_141;
							}
							if (!(messageType == "ERR"))
							{
								goto IL_141;
							}
							return new PXSetPropertyException<TMessageField>("Error Has Occurred", PXErrorLevel.Error);
						}
						else if (!(messageType == "COM"))
						{
							goto IL_141;
						}
					}
					else if (c != 'I')
					{
						switch (c)
						{
						case 'N':
							if (!(messageType == "NON"))
							{
								goto IL_141;
							}
							return null;
						case 'O':
							goto IL_141;
						case 'P':
							if (!(messageType == "PRM"))
							{
								goto IL_141;
							}
							break;
						case 'Q':
							if (!(messageType == "QST"))
							{
								goto IL_141;
							}
							break;
						default:
							if (c != 'W')
							{
								goto IL_141;
							}
							if (!(messageType == "WRN"))
							{
								goto IL_141;
							}
							return new PXSetPropertyException<TMessageField>("Warning!", PXErrorLevel.Warning);
						}
					}
					else if (!(messageType == "INS") && !(messageType == "INF"))
					{
						goto IL_141;
					}
					ValueTuple<string, PXErrorLevel> errorWithLevel = PXUIFieldAttribute.GetErrorWithLevel<TMessageField>(base.Base.Caches[BqlCommand.GetItemType<TMessageField>()], row);
					string item = errorWithLevel.Item1;
					PXErrorLevel item2 = errorWithLevel.Item2;
					if (item2 == PXErrorLevel.Undefined || item == null)
					{
						return null;
					}
					return new PXSetPropertyException<TMessageField>(item, item2);
				}
			}
			IL_141:
			throw new ArgumentOutOfRangeException("messageType");
		}

		// Token: 0x04000003 RID: 3
		private BarcodeDrivenStateMachine<TSelf, TGraph>.ScanReporterImpl _reporter;

		// Token: 0x04000004 RID: 4
		private string _previousMode;

		// Token: 0x04000005 RID: 5
		private string _previousQuestion;

		// Token: 0x04000006 RID: 6
		private string _previousInstructions;

		// Token: 0x04000007 RID: 7
		private string _previousPrompt;

		// Token: 0x04000008 RID: 8
		private string _previousPromptCombined;

		// Token: 0x04000009 RID: 9
		private bool _saveRequested;

		// Token: 0x0400000A RID: 10
		private bool _bypassLogging;

		/// <summary>The main data view of <see cref="T:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2" /> that holds the current mode and state.
		/// This is an extension point for additional states.</summary>
		/// <remarks>This view is virtual (that is, of the <see cref="T:PX.Data.PXFilter`1" /> type) and uses unbound DACs.
		/// Usually it is not used directly by an application developer.</remarks>
		// Token: 0x0400000D RID: 13
		public ScanHeaderView HeaderView;

		/// <summary>An additional data view that is used for reporting purposes.</summary>
		/// <remarks>This view is virtual (that is, of the <see cref="T:PX.Data.PXFilter`1" /> type) and uses unbound DACs.
		/// Usually it is not used directly by an application developer.</remarks>
		// Token: 0x0400000E RID: 14
		public PXFilter<ScanInfo> Info;

		/// <summary>
		/// An additional data view that keeps scan logs of the session.
		/// </summary>
		/// <remarks>This view is virtual (that is, of the <see cref="T:PX.Data.PXFilter`1" /> type) and uses unbound DACs.
		/// Usually it is not used directly by an application developer.</remarks>
		// Token: 0x0400000F RID: 15
		public PXFilter<ScanLog> Logs;

		/// <summary>
		/// Saves a form's state, as the standard Save button does.
		/// However, this action is used mostly internally
		/// by the <see cref="M:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.SaveChanges">BarcodeDrivenStateMachine.SaveChanges()</see> method.
		/// </summary>
		/// <remarks>This action is not intended to be used by an application developer.
		/// It has complete and fixed functionality that is vital for the barcode-based forms.</remarks>
		// Token: 0x04000010 RID: 16
		public PXSave<ScanHeader> Save;

		/// <summary>
		/// Resets a form's state, as the standard Cancel button does. 
		/// </summary>
		/// <inheritdoc cref="F:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.Save" path="/remarks" />
		// Token: 0x04000011 RID: 17
		public PXCancel<ScanHeader> Cancel;

		/// <summary>Is used for the OK answer to questions and warnings generated by <see cref="T:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2" />.</summary>
		/// <inheritdoc cref="F:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.Save" path="/remarks" />
		// Token: 0x04000012 RID: 18
		public PXAction<ScanHeader> ScanConfirm;

		/// <summary>
		/// Is used for resetting the barcode processing cycle.
		/// As compared to the <see cref="F:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.Cancel" /> action, this action does not clear the form completely.
		/// </summary>
		/// <inheritdoc cref="F:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.Save" path="/remarks" />
		// Token: 0x04000013 RID: 19
		public PXAction<ScanHeader> ScanReset;

		/// <summary>The main action of <see cref="T:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2" /> that is executed every time
		/// a barcode is scanned. It is an entry point for the barcode processing cycle.</summary>
		/// <inheritdoc cref="F:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.Save" path="/remarks" />
		// Token: 0x04000014 RID: 20
		public PXAction<ScanHeader> Scan;

		// Token: 0x04000015 RID: 21
		private bool _clearHeader;

		// Token: 0x04000016 RID: 22
		private bool _restoreHeaderOnFirstOpen;

		// Token: 0x04000017 RID: 23
		private bool _bypassBarcodeFieldSelecting;

		// Token: 0x04000018 RID: 24
		private bool _awaitLongRunManually;

		// Token: 0x04000019 RID: 25
		private readonly Dictionary<Type, PXGraphExtension<TGraph>> ExtensionsCache = new Dictionary<Type, PXGraphExtension<TGraph>>();

		// Token: 0x02000041 RID: 65
		private class ScanReporterImpl : IScanReporterExtended, IScanReporter
		{
			// Token: 0x060002B1 RID: 689 RVA: 0x000084BB File Offset: 0x000066BB
			public ScanReporterImpl(BarcodeDrivenStateMachine<TSelf, TGraph> basis)
			{
				this._basis = basis;
			}

			// Token: 0x060002B2 RID: 690 RVA: 0x000084CA File Offset: 0x000066CA
			public void PromptAndInstruct()
			{
				this._basis.PromptAndInstruct();
			}

			// Token: 0x060002B3 RID: 691 RVA: 0x000084D7 File Offset: 0x000066D7
			public void Prompt(string promptMsg, params object[] args)
			{
				this._basis.Prompt(promptMsg, args);
			}

			// Token: 0x060002B4 RID: 692 RVA: 0x000084E6 File Offset: 0x000066E6
			public void Instruct(string instructMsg, params object[] args)
			{
				this._basis.Instruct(instructMsg, args);
			}

			// Token: 0x060002B5 RID: 693 RVA: 0x000084F5 File Offset: 0x000066F5
			public void Info(string infoMsg, params object[] args)
			{
				this._basis.ReportInfo(infoMsg, args);
			}

			// Token: 0x060002B6 RID: 694 RVA: 0x00008504 File Offset: 0x00006704
			public void Warning(string warnMsg, params object[] args)
			{
				this._basis.ReportWarning(warnMsg, args);
			}

			// Token: 0x060002B7 RID: 695 RVA: 0x00008513 File Offset: 0x00006713
			public void Complete(string completeMsg, params object[] args)
			{
				this._basis.ReportComplete(completeMsg, args);
			}

			// Token: 0x060002B8 RID: 696 RVA: 0x00008522 File Offset: 0x00006722
			public void Error(string errorMsg, params object[] args)
			{
				this._basis.ReportError(errorMsg, args);
			}

			// Token: 0x060002B9 RID: 697 RVA: 0x00008531 File Offset: 0x00006731
			public void ClearErrors()
			{
				this._basis.ClearErrors();
			}

			// Token: 0x040000DA RID: 218
			private readonly BarcodeDrivenStateMachine<TSelf, TGraph> _basis;
		}

		/// <summary>A non-generic version of the <see cref="T:PX.BarcodeProcessing.ScanMode`1" /> class.</summary>
		/// <remarks>This nested class encloses the TScanBasis type parameter of its base generic class with the TSelf type parameter of its Basis.
		/// Therefore, the class simplifies component defining in scope of an extension.</remarks>
		// Token: 0x02000042 RID: 66
		public abstract class ScanMode : ScanMode<TSelf>
		{
			// Token: 0x060002BA RID: 698 RVA: 0x0000853E File Offset: 0x0000673E
			[DebuggerStepThrough]
			[DebuggerStepperBoundary]
			public TExt Get<TExt>() where TExt : PXGraphExtension<TGraph>
			{
				return base.Basis.Get<TExt>();
			}
		}

		/// <summary>
		/// A simplified version of the <see cref="T:PX.BarcodeProcessing.EntityState`2" /> class.
		/// </summary>
		/// <typeparam name="TEntity">The type of the entity.</typeparam>
		/// <inheritdoc cref="T:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.ScanMode" path="/remarks" />
		// Token: 0x02000043 RID: 67
		public abstract class EntityState<TEntity> : EntityState<TSelf, TEntity>
		{
			// Token: 0x060002BC RID: 700 RVA: 0x00008558 File Offset: 0x00006758
			[DebuggerStepThrough]
			[DebuggerStepperBoundary]
			public TExt Get<TExt>() where TExt : PXGraphExtension<TGraph>
			{
				return base.Basis.Get<TExt>();
			}
		}

		/// <summary>
		/// A non-generic version of the <see cref="T:PX.BarcodeProcessing.MediatorState`1" /> class.
		/// </summary>
		/// <inheritdoc cref="T:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.ScanMode" path="/remarks" />
		// Token: 0x02000044 RID: 68
		public abstract class MediatorState : MediatorState<TSelf>
		{
			// Token: 0x060002BE RID: 702 RVA: 0x00008572 File Offset: 0x00006772
			[DebuggerStepThrough]
			[DebuggerStepperBoundary]
			public TExt Get<TExt>() where TExt : PXGraphExtension<TGraph>
			{
				return base.Basis.Get<TExt>();
			}
		}

		/// <summary>A non-generic version of the <see cref="T:PX.BarcodeProcessing.ConfirmationState`1" /> class.</summary>
		/// <inheritdoc cref="T:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.ScanMode" path="/remarks" />
		// Token: 0x02000045 RID: 69
		public abstract class ConfirmationState : ConfirmationState<TSelf>
		{
			// Token: 0x170000E1 RID: 225
			// (get) Token: 0x060002C0 RID: 704 RVA: 0x0000858C File Offset: 0x0000678C
			protected sealed override bool ExplicitConfirmation
			{
				get
				{
					return base.Basis.ExplicitConfirmation;
				}
			}

			// Token: 0x060002C1 RID: 705 RVA: 0x0000859E File Offset: 0x0000679E
			[DebuggerStepThrough]
			[DebuggerStepperBoundary]
			public TExt Get<TExt>() where TExt : PXGraphExtension<TGraph>
			{
				return base.Basis.Get<TExt>();
			}
		}

		/// <summary>A non-generic version of the <see cref="T:PX.BarcodeProcessing.ScanCommand`1" /> class.</summary>
		/// <inheritdoc cref="T:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.ScanMode" path="/remarks" />
		// Token: 0x02000046 RID: 70
		public abstract class ScanCommand : ScanCommand<TSelf>
		{
			// Token: 0x060002C3 RID: 707 RVA: 0x000085B8 File Offset: 0x000067B8
			[DebuggerStepThrough]
			[DebuggerStepperBoundary]
			public TExt Get<TExt>() where TExt : PXGraphExtension<TGraph>
			{
				return base.Basis.Get<TExt>();
			}
		}

		/// <summary>A non-generic version of the <see cref="T:PX.BarcodeProcessing.ScanQuestion`1" /> class.</summary>
		/// <inheritdoc cref="T:PX.BarcodeProcessing.BarcodeDrivenStateMachine`2.ScanMode" path="/remarks" />
		// Token: 0x02000047 RID: 71
		public abstract class ScanQuestion : ScanQuestion<TSelf>
		{
			// Token: 0x060002C5 RID: 709 RVA: 0x000085D2 File Offset: 0x000067D2
			[DebuggerStepThrough]
			[DebuggerStepperBoundary]
			public TExt Get<TExt>() where TExt : PXGraphExtension<TGraph>
			{
				return base.Basis.Get<TExt>();
			}
		}

		// Token: 0x02000048 RID: 72
		public abstract class RedirectFrom<TForeignBasis> : PX.BarcodeProcessing.RedirectFrom<TForeignBasis>.To<TSelf> where TForeignBasis : PXGraphExtension, IBarcodeDrivenStateMachine
		{
		}

		// Token: 0x02000049 RID: 73
		[PXLocalizable]
		public abstract class Msg
		{
			// Token: 0x040000DB RID: 219
			public const string ScreenCleared = "The unconfirmed entries have been cleared.";

			// Token: 0x040000DC RID: 220
			public const string CommandUnknown = "The {0} string is not a valid command or value.";

			// Token: 0x040000DD RID: 221
			public const string CommandIsDisabled = "The {0} ({1}) command is not available in the current state.";

			// Token: 0x040000DE RID: 222
			public const string SeveralErrorsSeeTrace = "The document could not be updated due to multiple errors. See the trace for details.";

			// Token: 0x040000DF RID: 223
			public const string OKToolTip = "Scan *OK to confirm";

			// Token: 0x040000E0 RID: 224
			public const string ResetToolTip = "Scan *RESET to execute";

			// Token: 0x040000E1 RID: 225
			public const string CommandTooltip = "Scan {0} to execute";

			// Token: 0x040000E2 RID: 226
			public const string RedirectTooltip = "Scan {0} to change mode";

			// Token: 0x040000E3 RID: 227
			public const string BarcodePrompt = "Scan a valid barcode.";

			// Token: 0x040000E4 RID: 228
			public const string Warning = "Warning!";

			// Token: 0x040000E5 RID: 229
			public const string Fits = "Matched";
		}

		/// <summary>
		/// A simplified version of the <see cref="T:PX.Data.PXGraphExtension`2" />,
		/// which is used to simplify creation of the logic parts of the components.
		/// </summary>
		// Token: 0x0200004A RID: 74
		public abstract class ScanExtension : PXGraphExtension<TSelf, TGraph>
		{
			// Token: 0x170000E2 RID: 226
			// (get) Token: 0x060002C9 RID: 713 RVA: 0x000085FC File Offset: 0x000067FC
			public TGraph Graph
			{
				[DebuggerStepThrough]
				[DebuggerStepperBoundary]
				get
				{
					return base.Base;
				}
			}

			// Token: 0x170000E3 RID: 227
			// (get) Token: 0x060002CA RID: 714 RVA: 0x00008604 File Offset: 0x00006804
			public TSelf Basis
			{
				[DebuggerStepThrough]
				[DebuggerStepperBoundary]
				get
				{
					return base.Base1;
				}
			}
		}

		/// <summary>
		/// A simplified version of the <see cref="T:PX.Data.PXGraphExtension`3" />,
		/// which is used to simplify creation of the logic parts of the components.
		/// </summary>
		// Token: 0x0200004B RID: 75
		public abstract class ScanExtension<TTargetExtension> : PXGraphExtension<TTargetExtension, TSelf, TGraph> where TTargetExtension : PXGraphExtension<TSelf, TGraph>
		{
			// Token: 0x170000E4 RID: 228
			// (get) Token: 0x060002CC RID: 716 RVA: 0x00008614 File Offset: 0x00006814
			public TGraph Graph
			{
				[DebuggerStepThrough]
				[DebuggerStepperBoundary]
				get
				{
					return base.Base;
				}
			}

			// Token: 0x170000E5 RID: 229
			// (get) Token: 0x060002CD RID: 717 RVA: 0x0000861C File Offset: 0x0000681C
			public TSelf Basis
			{
				[DebuggerStepThrough]
				[DebuggerStepperBoundary]
				get
				{
					return base.Base1;
				}
			}

			// Token: 0x170000E6 RID: 230
			// (get) Token: 0x060002CE RID: 718 RVA: 0x00008624 File Offset: 0x00006824
			public TTargetExtension Target
			{
				[DebuggerStepThrough]
				[DebuggerStepperBoundary]
				get
				{
					return base.Base2;
				}
			}
		}
	}
}
