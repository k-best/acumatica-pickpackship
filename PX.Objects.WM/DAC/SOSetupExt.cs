using PX.Data;

namespace PX.Objects.SO
{
    public class SOSetupExt : PXCacheExtension<SOSetup>
    {
        public abstract class usePickLocation : IBqlField { }
        [PXDBBool]
        [PXDefault(false)]
        [PXUIField(DisplayName = "Prompt for Warehouse Location during picking")]
        public virtual bool? UsePickLocation { get; set; }
    }
}