using PX.Data;
using PX.Objects.CS;
using PX.Objects.SO;

namespace PX.Objects.WM
{
    public class SOSetupMaintExt : PXGraphExtension<SOSetupMaint>
    {
        protected virtual void SOSetup_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            SOSetup setup = (SOSetup)e.Row;

            if (setup != null && !PXAccess.FeatureInstalled<FeaturesSet.warehouseLocation>())
            {
                SOSetupExt setupExt = PXCache<SOSetup>.GetExtension<SOSetupExt>(setup);

                if (setupExt != null)
                {
                    PXUIFieldAttribute.SetVisible<SOSetupExt.usePickLocation>(sender, setup, false);
                }
            }
        }
    }
}
