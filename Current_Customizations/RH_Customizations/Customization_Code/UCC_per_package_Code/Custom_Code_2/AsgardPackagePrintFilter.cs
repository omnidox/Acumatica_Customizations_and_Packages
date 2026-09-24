using System;
using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;

namespace AA.Objects.Labels.Integration.PerPackage
{
    /// <summary>
    /// Popup filter used to let the user choose which Asgard model
    /// should be used for printing the selected package label.
    /// </summary>
    [Serializable]
    [PXCacheName("Asgard Package Print Filter")]
    public class AsgardPackagePrintFilter : PXBqlTable, IBqlTable
    {
        #region SelectedModelID
        /// <summary>
        /// User-selected Asgard model.
        ///
        /// This reuses the same model constraints as native Box Print:
        /// - Screen = Shipments
        /// - BasedOnView contains Packages
        /// - ModelType is Group or Single
        /// </summary>
        [ALModelIDForeign(
            typeof(Where<
                ALModel.screenID, Equal<ACConstants.ScreenIDs.Shipments>,
                And<
                    ALModel.basedOnView, Contains<ALConstants.ViewNames.Packages>,
                    And<
                        ALModel.modelType, In3<ALModelType.group, ALModelType.single>
                    >
                >
            >),
            DisplayName = "Label Model")]
        [PXForeignReference(typeof(FK.Model))]
        [PXUIField(DisplayName = "Label Model", Required = true)]
        [PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
        public virtual Guid? SelectedModelID { get; set; }

        public abstract class selectedModelID : BqlGuid.Field<selectedModelID> { }
        #endregion

        #region FK
        public static class FK
        {
            public class Model : PrimaryKeyOf<ALModel>.By<ALModel.labelID>.ForeignKeyOf<AsgardPackagePrintFilter>.By<AsgardPackagePrintFilter.selectedModelID>
            {
            }
        }
        #endregion
    }
}