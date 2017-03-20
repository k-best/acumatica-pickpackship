using System;
using System.Collections;
using PX.Data;
using System.Collections.Generic;

namespace PX.SM
{
    public class PrintJobMaint : PXGraph<PrintJobMaint, SMPrintJob>
    {
        public PXSelect<SMPrintJob> Job;
        public PXSelect<SMPrintJobParameter, Where<SMPrintJobParameter.jobID, Equal<Current<SMPrintJob.jobID>>>> Parameters;

        protected virtual void SMPrintJob_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
        {
            if (e.Operation.Equals(PXDBOperation.Delete))
                return;

            const string fileIdParameter = "FILEID";
            bool isFileId = false;
            SMPrintJob smPrintJob = e.Row as SMPrintJob;
            
            if (smPrintJob != null && String.IsNullOrWhiteSpace(smPrintJob.ReportID))
            {
                foreach (SMPrintJobParameter smPrintJobParameter in Parameters.Select())
                {
                    if (smPrintJobParameter.ParameterName.Trim().ToUpperInvariant().Equals(fileIdParameter))
                    {
                        isFileId = true;
                        break;
                    }
                }

                if (!isFileId)
                    throw new PXRowPersistingException(typeof(SMPrintJob.reportID).Name, null, Objects.WM.Messages.MissingFileIdError);
            }
        }

        public virtual void AddPrintJob(string description, string printQueue, string reportID, Dictionary<string, string> parameters)
        {
            var job = (PX.SM.SMPrintJob)this.Job.Cache.CreateInstance();
            job.Description = description;
            job.PrintQueue = printQueue;
            job.ReportID = reportID;
            this.Job.Insert(job);

            foreach (var p in parameters)
            {
                var parameter = (PX.SM.SMPrintJobParameter)this.Parameters.Cache.CreateInstance();
                parameter.ParameterName = p.Key;
                parameter.ParameterValue = p.Value;
                this.Parameters.Insert(parameter);
            }

            this.Actions.PressSave();
        }
    }
}
