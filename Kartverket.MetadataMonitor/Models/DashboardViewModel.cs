namespace Kartverket.MetadataMonitor.Models
{
    public class DashboardViewModel
    {
        public int TotalResultCount { get; set; }
        public int TotalResultOk { get; set; }
        public int TotalResultFailed { get; set; }
        public int TotalNotValidated { get; set; }

        /* INSPIRE */
        public Result InspireService { get; set; }
        public Result InspireDataset { get; set; }
        public Result InspireSeries { get; set; }

        /* Norge Digitalt (ND) */
        public Result NdService { get; set; }
        public Result NdDataset { get; set; }
        public Result NdSeries { get; set; }

        public Result NdSoftware { get; set; }
        public Result NdOther { get; set; }

        public int TotalInspireOk
        {
            get { return InspireService.Ok + InspireDataset.Ok + InspireSeries.Ok; }
        }

        public int TotalInspireFailed
        {
            get { return InspireService.Failed + InspireDataset.Failed + InspireSeries.Failed; }
        }

        public int TotalNdOk
        {
            get { return NdService.Ok + NdDataset.Ok + NdSeries.Ok + NdSoftware.Ok; }
        }

        public int TotalNdFailed
        {
            get { return NdService.Failed + NdDataset.Failed + NdSeries.Failed + NdSoftware.Failed; }
        }

    }

    public class Result
    {
        public int Ok { get; set; }
        public int Failed { get; set; }
        public int Unknown { get; set; }
    }
}