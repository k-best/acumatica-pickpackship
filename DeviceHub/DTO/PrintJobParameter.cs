namespace Acumatica.DeviceHub.DTO
{
    public class PrintJobParameter
    {
        public string JobID { get; set; }
        public string ReportID { get; set; }
        public string PrintQueue { get; set; }
        public string Description { get; set; }
        public string ParameterName { get; set; }
        public string ParameterValue { get; set; }
    }
}