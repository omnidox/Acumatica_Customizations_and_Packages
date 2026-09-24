using PX.Data;
using PX.Data.BQL;
using PX.Objects.SO;

namespace IstarEDIShipViaResolver
{
    public sealed class SOOrderEDIShipViaExt : PXCacheExtension<SOOrder>
    {
        public abstract class usrManualShipVia : BqlBool.Field<usrManualShipVia> { }
        [PXDBBool]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Manual Ship Via")]
        public bool? UsrManualShipVia { get; set; }

        public abstract class usrAutoShipVia : BqlString.Field<usrAutoShipVia> { }
        [PXDBString(15, IsUnicode = true)]
        [PXUIField(DisplayName = "Last EDI Ship Via", Visible = false,
            Enabled = false)]
        public string UsrAutoShipVia { get; set; }
    }
}
