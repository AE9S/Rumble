using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace Utility
{    
    /// <summary>
    /// Commonly Used Methods and Functions.
    /// </summary>
    public static class UtilityMethods
    {
        // TODO: change this before final release
        public static string DefaultExceptionWindowTitle = @"OH SHIT YO!!!";

        /// <summary>
        /// Handles any exceptions by displaying them to the user if compiled in debug mode
        /// or writing them to the log file specified in the application settings if compiled in release mode.
        /// </summary>
        /// <param name="ex">The Exception to log.</param>
        public static void ExceptionHandler(Exception ex, string TraceString)
        {
            string detail = ExceptionStringBuilder(ex, 0, TraceString);
            Debug.WriteLine(detail);
            MessageBox.Show(detail, DefaultExceptionWindowTitle);
        } // ExceptionHandler

        /// <summary>
        /// Recursive method to build a full detail of the supplied exception and all inner exceptions.
        /// </summary>
        /// <param name="ex">The Exception to document.</param>
        /// <param name="TabLevel">The number of tab levels to prefix exception detail with. 
        /// When this method is called from outside itself this parameter should be zero.</param>
        /// <returns></returns>
        private static string ExceptionStringBuilder(Exception ex, int TabLevel, string TraceString)
        {
            string tabPrefix = string.Empty;
            for (int i = 0; i < TabLevel; i++)
            {
                tabPrefix += "\t";
            } // for
            System.Text.StringBuilder detail = new System.Text.StringBuilder();
            detail.AppendLine(tabPrefix + "EXCEPTION OCCURED AT " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
            detail.AppendLine(tabPrefix + "\tMESSAGE");
            detail.AppendLine(tabPrefix + "\t-------");
            detail.AppendLine(tabPrefix + "\t" + ex.Message);
            detail.AppendLine();
            detail.AppendLine(tabPrefix + "\tSOURCE");
            detail.AppendLine(tabPrefix + "\t------");
            detail.AppendLine(tabPrefix + "\t" + ex.Source);
            detail.AppendLine();
            detail.AppendLine(tabPrefix + "\tSTACKTRACE");
            detail.AppendLine(tabPrefix + "\t----------");
            detail.AppendLine(tabPrefix + "\t" + ex.StackTrace);
            detail.AppendLine();
            detail.AppendLine(tabPrefix + "\tTRACE STRING");
            detail.AppendLine(tabPrefix + "\t----------");
            detail.AppendLine(tabPrefix + "\t" + TraceString);
            detail.AppendLine();
            if (ex.InnerException != null)
            {
                detail.AppendLine(tabPrefix + "\tINNER EXCEPTION");
                detail.AppendLine(tabPrefix + "\t---------------");
                detail.AppendLine(tabPrefix + "\t" + ex.InnerException.ToString());
                detail.AppendLine(tabPrefix + "-----------------------------------------------------");
                detail.AppendLine(tabPrefix + "-----------------------------------------------------");
                detail.AppendLine(string.Empty);
                detail.Append(ExceptionStringBuilder(ex.InnerException, ++TabLevel, TraceString));
            } // if            
            detail.AppendLine(string.Empty);
            return detail.ToString();
        } // ExceptionStringBuilder

    } // UtilityMethods
} // Utility
