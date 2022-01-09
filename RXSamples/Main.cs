using System;
using System.Reflection;
// using log4net;

namespace RXSamples
{
    public class Program
    {
        #region Private Methods

        private static void Main(string[] args)
        {
            // Logging.InitFromConfigFile();
            //
            // s_log.Log(Severity.Debug, "[Setup] - Start Tests");

            var samples = new Samples();

            //            			samples.A01_Interval_FromMain();
            //			samples.A03_GenerateWithTime_FromMain();
            //			samples.A2_1_Subject_FromMain();
            //            samples.A2_2_SubscribedProtected_FromMain();
            //            			samples.A7_1_EventLoopScheduler_FromMain();
            //samples.A8_Merge_FromMain();

            Console.ReadLine();
        }

        #endregion

        #region Fields

        // private static readonly ILog s_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        #endregion
    }
}