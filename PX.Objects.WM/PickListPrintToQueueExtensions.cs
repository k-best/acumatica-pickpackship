using System;
using System.Collections;
using System.Collections.Generic;
using PX.Data;
using PX.SM;
using PX.Objects.CR;
using PX.Objects.AR;

namespace PX.Objects.SO
{
    public class SOShipmentFilterExt : PXCacheExtension<SOShipmentFilter>
    {
        public abstract class printQueue : PX.Data.IBqlField { }
        [PXDBString(10)]
        [PXSelector(typeof(SMPrintQueue.printQueue))]
        [PXUIField(DisplayName = "Print Queue")]
        public virtual string PrintQueue { get; set; }
    }

    public class SOInvoiceShipmentExt : PXGraphExtension<SOInvoiceShipment>
    {
        public virtual void SOShipmentFilter_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            if (e.Row == null) return;

            SOShipmentFilter filter = e.Row as SOShipmentFilter;

            string actionID = (string)Base.Orders.GetTargetFill(null, null, null, filter.Action, "@actionID");
            int action = 0;
            int.TryParse(actionID, out action);

            PXUIFieldAttribute.SetVisible<SOShipmentFilterExt.printQueue>(sender, filter, action == SOShipmentEntryActionsAttribute.PrintLabels || action == SOShipmentEntryActionsAttribute.PrintPickList);
        }
    }
    
    public class SOShipmentEntryPrintToQueueExt : PXGraphExtension<SOShipmentEntry>
    {
        public PXAction<SOShipment> action;
        [PXUIField(DisplayName = "Actions", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
        [PXButton]
        protected virtual IEnumerable Action(PXAdapter adapter,
            [PXInt]
            [SOShipmentEntryActions]
            int? actionID,
            [PXString()]
            string ActionName
            )
        {
            object queueName = null;
            if (actionID == SOShipmentEntryActionsAttribute.PrintPickList && adapter.Arguments.TryGetValue("PrintQueue", out queueName) && !String.IsNullOrEmpty(queueName as string))
            {
                GL.Branch company = null;
                using (new PXReadBranchRestrictedScope())
                {
                    company = Base.Company.Select();
                }

                using (PXTransactionScope ts = new PXTransactionScope())
                {
                    var jobMaint = PXGraph.CreateInstance<PrintJobMaint>();
                    foreach (SOShipment shipment in adapter.Get<SOShipment>())
                    {
                        Base.Document.Current = shipment;
                        string actualReportID = new NotificationUtility(Base).SearchReport(ARNotificationSource.Customer, Base.customer.Current, "SO644000", company.BranchID);
                        jobMaint.AddPrintJob(PXMessages.LocalizeFormatNoPrefix(WM.Messages.PrintPickList, shipment.ShipmentNbr), queueName as string, actualReportID, new Dictionary<string, string> { { "ShipmentNbr", shipment.ShipmentNbr } });
                        shipment.PickListPrinted = true;
                        Base.Document.Update(shipment);
                    }
                    Base.Save.Press();
                    ts.Complete();
                }

                return adapter.Get();
            }
            else
            {
                return Base.action.Press(adapter);
            }
        }
    }
}