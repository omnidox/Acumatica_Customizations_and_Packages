using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net;
using System.Text;
using System.Web.UI;
using PX.Common;
using PX.Data;

namespace PX.Web.UI
{
	// Token: 0x020000B1 RID: 177
	[ToolboxData("<{0}:PXDataSource Width=100% runat=server></{0}:PXDataSource>")]
	public class PXDataSource : PXBaseDataSource, IHierarchicalDataSource
	{
		// Token: 0x0600134A RID: 4938 RVA: 0x0004ED64 File Offset: 0x0004CF64
		private void RegisterDefaultDatasource()
		{
			if (!this.IsDefaultDatasource)
			{
				return;
			}
			PXPage pxpage = this.Page as PXPage;
			if (pxpage == null)
			{
				if (base.DesignMode)
				{
					return;
				}
				if (PXSiteMap.IsPortal)
				{
					return;
				}
				throw new PXException("The page should be inherited from PXPage.");
			}
			else
			{
				if (!PXPage.IsDeclaredInAspx(this))
				{
					return;
				}
				if (pxpage.DefaultDataSource != null)
				{
					return;
				}
				pxpage.DefaultDataSource = this;
				return;
			}
		}

		// Token: 0x0600134B RID: 4939 RVA: 0x0004EDBE File Offset: 0x0004CFBE
		protected override void OnInit(EventArgs e)
		{
			this.OnInitImpl(e);
		}

		// Token: 0x0600134C RID: 4940 RVA: 0x0004EDC7 File Offset: 0x0004CFC7
		protected void OnInitImpl(EventArgs e)
		{
			this.RegisterDefaultDatasource();
			base.OnInit(e);
		}

		// Token: 0x0600134D RID: 4941 RVA: 0x0004EDD8 File Offset: 0x0004CFD8
		public string GetPath(string viewName, object item)
		{
			bool flag = false;
			string[] array = null;
			foreach (object obj in this.DataTrees)
			{
				PXTreeDataMember pxtreeDataMember = (PXTreeDataMember)obj;
				if (string.Compare(pxtreeDataMember.TreeView, viewName, StringComparison.OrdinalIgnoreCase) == 0)
				{
					array = pxtreeDataMember.TreeKeys;
					flag = !flag;
					break;
				}
				if (!flag)
				{
					flag = true;
					array = pxtreeDataMember.TreeKeys;
				}
			}
			string[] array2 = new string[array.Length + 1];
			if (flag)
			{
				if (string.IsNullOrEmpty(viewName))
				{
					viewName = this.DataTrees[0].TreeView;
				}
			}
			else
			{
				array2[0] = viewName;
			}
			for (int i = 1; i < array2.Length; i++)
			{
				object obj2 = base.DataGraph.GetValueExt(viewName, item, array[i - 1]);
				if (obj2 is PXFieldState)
				{
					obj2 = ((PXFieldState)obj2).Value;
				}
				if (obj2 != null)
				{
					array2[i] = this.Escape(obj2.ToString());
				}
			}
			return string.Join(this.KeySeparatorChar, array2);
		}

		// Token: 0x0600134E RID: 4942 RVA: 0x0004EEEC File Offset: 0x0004D0EC
		public object GetTreeValue(string viewName, object item, string fieldName)
		{
			if (string.IsNullOrEmpty(viewName) && this.DataTrees.Count > 0)
			{
				viewName = this.DataTrees[0].TreeView;
			}
			return base.GetValue(viewName, item, fieldName);
		}

		// Token: 0x0600134F RID: 4943 RVA: 0x0004EF20 File Offset: 0x0004D120
		public object GetTreeValueExt(string viewName, object item, string fieldName)
		{
			if (string.IsNullOrEmpty(viewName) && this.DataTrees.Count > 0)
			{
				viewName = this.DataTrees[0].TreeView;
			}
			return base.GetValueExt(viewName, item, fieldName);
		}

		// Token: 0x06001350 RID: 4944 RVA: 0x0004EF54 File Offset: 0x0004D154
		public object GetTreeStateExt(string viewName, object item, string fieldName)
		{
			if (string.IsNullOrEmpty(viewName) && this.DataTrees.Count > 0)
			{
				viewName = this.DataTrees[0].TreeView;
			}
			return base.GetStateExt(viewName, item, fieldName);
		}

		// Token: 0x06001351 RID: 4945 RVA: 0x0004EF88 File Offset: 0x0004D188
		private string Escape(string str)
		{
			return str.Replace(this.KeySeparatorChar, WebUtility.UrlEncode(this.KeySeparatorChar));
		}

		// Token: 0x06001352 RID: 4946 RVA: 0x0004EFA1 File Offset: 0x0004D1A1
		private string Unescape(string str)
		{
			return str.Replace(WebUtility.UrlEncode(this.KeySeparatorChar), this.KeySeparatorChar);
		}

		// Token: 0x170006B7 RID: 1719
		// (get) Token: 0x06001353 RID: 4947 RVA: 0x0004EFBA File Offset: 0x0004D1BA
		[DefaultValue(null)]
		[MergableProperty(false)]
		[PersistenceMode(PersistenceMode.InnerProperty)]
		[Category("Base Property")]
		[Description("The available trees.")]
		public PXTreeDataMemberCollection DataTrees
		{
			get
			{
				if (this.dataTrees == null)
				{
					this.dataTrees = new PXTreeDataMemberCollection();
				}
				return this.dataTrees;
			}
		}

		// Token: 0x06001354 RID: 4948 RVA: 0x0004EFD5 File Offset: 0x0004D1D5
		public HierarchicalDataSourceView GetHierarchicalView(string viewPath)
		{
			return new PXTreeDataSourceView(this, viewPath);
		}

		// Token: 0x06001355 RID: 4949 RVA: 0x0004EFE0 File Offset: 0x0004D1E0
		public IEnumerable ExecuteSelect(string viewPath)
		{
			int num = -1;
			int num2 = 0;
			List<string> list = new List<string>();
			foreach (string text in viewPath.Split(new char[]
			{
				this.KeySeparatorChar[0]
			}))
			{
				if (object.Equals(text, ""))
				{
					list.Add(null);
				}
				else
				{
					list.Add(this.Unescape(text));
				}
			}
			string text2 = null;
			if (list.Count > 0)
			{
				text2 = list[0];
				list.RemoveAt(0);
			}
			if (string.IsNullOrEmpty(text2) && this.DataTrees.Count > 0)
			{
				text2 = this.DataTrees[0].TreeView;
			}
			if (!string.IsNullOrEmpty(text2))
			{
				PXGraph dataGraph = base.DataGraph;
				string viewName = text2;
				object[] parameters = list.ToArray();
				return dataGraph.ExecuteSelect(viewName, parameters, null, null, null, null, ref num2, -1, ref num);
			}
			return new object[0];
		}

		// Token: 0x14000059 RID: 89
		// (add) Token: 0x06001356 RID: 4950 RVA: 0x0004F0C3 File Offset: 0x0004D2C3
		// (remove) Token: 0x06001357 RID: 4951 RVA: 0x0004F0C5 File Offset: 0x0004D2C5
		public event EventHandler DataSourceChanged
		{
			add
			{
			}
			remove
			{
			}
		}

		// Token: 0x06001358 RID: 4952 RVA: 0x0004F0C8 File Offset: 0x0004D2C8
		internal override void OnCommandPreparing(PXDSCallbackCommand info)
		{
			base.OnCommandPreparing(info);
			if (!string.IsNullOrEmpty(info.DependOnTree))
			{
				PXTreeView tree = ControlHelper.FindControl(info.DependOnTree, this.Page) as PXTreeView;
				this.SynchronizeTree(tree);
			}
		}

		// Token: 0x06001359 RID: 4953 RVA: 0x0004F107 File Offset: 0x0004D307
		internal void SynchronizeTree(PXTreeView tree)
		{
			this.SynchronizeTree(tree, true);
		}

		// Token: 0x0600135A RID: 4954 RVA: 0x0004F114 File Offset: 0x0004D314
		internal void SynchronizeTree(PXTreeView tree, bool dataBind)
		{
			if (tree == null || tree.DataBindings.Count == 0)
			{
				return;
			}
			if (string.IsNullOrEmpty(tree.DataBindings[0].DataMember) || tree.DataSourceID != this.ID)
			{
				return;
			}
			if (dataBind)
			{
				tree.DataBind();
			}
			if (tree.SelectedNode == null)
			{
				return;
			}
			object[] array = new object[1];
			string[] array2 = new string[1];
			object[] array3 = new object[1];
			int num = 0;
			int num2 = 0;
			array[0] = PXDataSource.TryConvertToGuid((tree.SelectedNode.Parent == null) ? tree.SelectedValue : tree.SelectedNode.Parent.Value);
			array2[0] = tree.DataBindings[0].ValueField;
			array3[0] = PXDataSource.TryConvertToGuid(tree.SelectedValue);
			string dataMember = tree.DataBindings[0].DataMember;
			base.DataGraph.Caches[base.DataGraph.GetItemType(dataMember)].Current = null;
			using (IEnumerator enumerator = base.DataGraph.ExecuteSelect(dataMember, array, array3, array2, null, null, ref num, 1, ref num2).GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					object obj = enumerator.Current;
					if (!base.GetUpdatable(dataMember))
					{
						base.DataGraph.Caches[base.DataGraph.GetItemType(dataMember)].Current = ((obj is PXResult) ? ((PXResult)obj)[0] : obj);
					}
				}
			}
		}

		// Token: 0x0600135B RID: 4955 RVA: 0x0004F2B0 File Offset: 0x0004D4B0
		private static object TryConvertToGuid(string v)
		{
			Guid? guid = GUID.CreateGuid(v);
			if (guid == null)
			{
				return v;
			}
			return guid.GetValueOrDefault();
		}

		// Token: 0x0600135C RID: 4956 RVA: 0x0004F2DC File Offset: 0x0004D4DC
		protected internal override IEnumerable ExecuteSelect(string viewName, DataSourceSelectArguments arguments, PXDSSelectArguments pxarguments)
		{
			if (base.DataGraph._InactiveViews.ContainsKey(viewName))
			{
				return new object[0];
			}
			if (arguments.MaximumRows != 1 || pxarguments == null || pxarguments.Searches == null || pxarguments.Searches.Count == 0)
			{
				return base.ExecuteSelect(viewName, arguments, pxarguments);
			}
			PXTreeDataMember pxtreeDataMember = null;
			if (string.IsNullOrEmpty(viewName) && this.DataTrees.Count > 0)
			{
				pxtreeDataMember = this.DataTrees[0];
			}
			else
			{
				foreach (object obj in this.DataTrees)
				{
					PXTreeDataMember pxtreeDataMember2 = (PXTreeDataMember)obj;
					if (string.Equals(pxtreeDataMember2.TreeView, viewName))
					{
						pxtreeDataMember = pxtreeDataMember2;
						break;
					}
				}
			}
			List<string> list = null;
			if (pxtreeDataMember != null)
			{
				list = new List<string>(pxtreeDataMember.TreeKeys);
				int i = 0;
				while (i < list.Count)
				{
					bool flag = false;
					foreach (object obj2 in pxarguments.Parameters)
					{
						if (string.Equals(((DictionaryEntry)obj2).Key as string, list[i], StringComparison.OrdinalIgnoreCase))
						{
							list.RemoveAt(i);
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						i++;
					}
				}
			}
			if (list == null || list.Count == 0)
			{
				return base.ExecuteSelect(viewName, arguments, pxarguments);
			}
			if (pxarguments.TreePaths != null)
			{
				foreach (string key in pxarguments.TreePaths.Keys.ToArray<string>())
				{
					pxarguments.TreePaths[key] = new List<string>();
				}
			}
			object obj3 = this.executeSelect(pxtreeDataMember.TreeView, arguments, pxarguments, list, new HashSet<string>());
			if (pxarguments.TreePaths != null)
			{
				foreach (string key2 in pxarguments.TreePaths.Keys.ToArray<string>())
				{
					pxarguments.TreePaths[key2] = string.Join("/", ((List<string>)pxarguments.TreePaths[key2]).ToArray());
				}
			}
			List<object> list2 = new List<object>();
			if (obj3 != null)
			{
				list2.Add(obj3);
			}
			return list2;
		}

		// Token: 0x0600135D RID: 4957 RVA: 0x0004F53C File Offset: 0x0004D73C
		private object executeSelect(string viewName, DataSourceSelectArguments arguments, PXDSSelectArguments pxarguments, List<string> absent, HashSet<string> verified)
		{
			using (IEnumerator enumerator = base.ExecuteSelect(viewName, new DataSourceSelectArguments(arguments.SortExpression, arguments.StartRowIndex, arguments.MaximumRows)
			{
				RetrieveTotalRowCount = arguments.RetrieveTotalRowCount
			}, pxarguments).GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return enumerator.Current;
				}
			}
			DataSourceSelectArguments arguments2 = new DataSourceSelectArguments(arguments.SortExpression, 0, 0);
			PXDSSelectArguments pxdsselectArguments = new PXDSSelectArguments(pxarguments.Parameters ?? new OrderedDictionary(StringComparer.OrdinalIgnoreCase), null);
			foreach (object data in base.ExecuteSelect(viewName, arguments2, pxdsselectArguments))
			{
				OrderedDictionary orderedDictionary = new OrderedDictionary(StringComparer.OrdinalIgnoreCase);
				foreach (object obj in pxdsselectArguments.Parameters)
				{
					DictionaryEntry dictionaryEntry = (DictionaryEntry)obj;
					orderedDictionary.Add(dictionaryEntry.Key, dictionaryEntry.Value);
				}
				PXDSSelectArguments pxdsselectArguments2 = new PXDSSelectArguments(orderedDictionary, pxarguments.Searches);
				pxdsselectArguments2.TreePaths = pxarguments.TreePaths;
				StringBuilder stringBuilder = new StringBuilder();
				foreach (string text in absent)
				{
					object obj2 = base.DataGraph.GetValueExt(viewName, data, text);
					if (obj2 is PXFieldState)
					{
						obj2 = ((PXFieldState)obj2).Value;
					}
					stringBuilder.Append('|');
					if (obj2 != null)
					{
						stringBuilder.Append(obj2);
						pxdsselectArguments2.Parameters[text] = obj2.ToString();
					}
					else
					{
						pxdsselectArguments2.Parameters[text] = null;
					}
				}
				if (verified.Add(stringBuilder.ToString()))
				{
					DataSourceSelectArguments dataSourceSelectArguments = new DataSourceSelectArguments(arguments.SortExpression, arguments.StartRowIndex, arguments.MaximumRows);
					dataSourceSelectArguments.RetrieveTotalRowCount = arguments.RetrieveTotalRowCount;
					if (pxarguments.TreePaths != null)
					{
						foreach (string text2 in pxarguments.TreePaths.Keys.ToArray<string>())
						{
							object obj3 = base.DataGraph.GetValueExt(viewName, data, text2);
							if (obj3 is PXFieldState)
							{
								obj3 = ((PXFieldState)obj3).Value;
							}
							if (obj3 is string)
							{
								((List<string>)pxarguments.TreePaths[text2]).Add((string)obj3);
							}
							else if (obj3 != null)
							{
								((List<string>)pxarguments.TreePaths[text2]).Add(base.DataGraph.Views[viewName].Cache.ValueToString(text2, obj3));
							}
							else
							{
								((List<string>)pxarguments.TreePaths[text2]).Add("");
							}
						}
					}
					object obj4 = this.executeSelect(viewName, dataSourceSelectArguments, pxdsselectArguments2, absent, verified);
					if (obj4 != null)
					{
						return obj4;
					}
					if (pxarguments.TreePaths != null)
					{
						foreach (string key in pxarguments.TreePaths.Keys.ToArray<string>())
						{
							((List<string>)pxarguments.TreePaths[key]).RemoveAt(((List<string>)pxarguments.TreePaths[key]).Count - 1);
						}
					}
				}
			}
			return null;
		}

		// Token: 0x170006B8 RID: 1720
		// (get) Token: 0x0600135E RID: 4958 RVA: 0x0004F92C File Offset: 0x0004DB2C
		// (set) Token: 0x0600135F RID: 4959 RVA: 0x0004F943 File Offset: 0x0004DB43
		[DefaultValue(":")]
		[Description("A reserved character, which cannot be used in field values.")]
		public string KeySeparatorChar
		{
			get
			{
				return STM.GetProp<string>(this.ViewState, "KeySeparatorChar", ":");
			}
			set
			{
				STM.SetProp<string>(this.ViewState, "KeySeparatorChar", value, ":");
			}
		}

		// Token: 0x04000523 RID: 1315
		public bool IsDefaultDatasource = true;

		// Token: 0x04000524 RID: 1316
		private PXTreeDataMemberCollection dataTrees;

		// Token: 0x04000525 RID: 1317
		private const string DefaultKeySeparator = ":";
	}
}
