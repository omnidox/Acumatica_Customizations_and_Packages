using System;
using ASCiStarKohls.DataProvider;
using ASCiStarKohls.DataProvider.Interface;
using Autofac;

namespace ASCiStarKohls
{
	// Token: 0x02000004 RID: 4
	public class ServiceRegistrator : Module
	{
		// Token: 0x06000004 RID: 4 RVA: 0x00002082 File Offset: 0x00000282
		protected override void Load(ContainerBuilder builder)
		{
			base.Load(builder);
			RegistrationExtensions.RegisterType<SODataProvider>(builder).As<ISODataProvider>().InstancePerLifetimeScope();
		}
	}
}
