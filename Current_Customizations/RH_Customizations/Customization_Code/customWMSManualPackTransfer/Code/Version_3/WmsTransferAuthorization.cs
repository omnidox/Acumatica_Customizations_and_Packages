using System.Linq;
using System.Security.Principal;
using PX.Common;

namespace CustomWMS2
{
    public static class WmsTransferAuthorization
    {
        public const string AdministratorRole = "Administrator";
        public const string WarehouseSupervisorRole = "Warehouse Supervisor";
        public const string ManualPackTransferRole = "WMS Manual Pack Transfer";

        public static bool IsAuthorized()
        {
            IPrincipal user = PXContext.PXIdentity.AuthUser;

            if (user == null)
                return false;

            string[] allowedRoles =
            {
                AdministratorRole,
                WarehouseSupervisorRole,
                ManualPackTransferRole
            };

            return allowedRoles.Any(user.IsInRole);
        }
    }
}