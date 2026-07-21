using PX.Data;

namespace CustomWMS
{
    /// <summary>
    /// Centralized debug tracing for CustomWMS.
    ///
    /// Set Enabled = true while debugging.
    /// Set Enabled = false for production.
    /// </summary>
    public static class WmsDebugTrace
    {
        /// <summary>
        /// Master switch for all CustomWMS trace output.
        /// Default is OFF.
        /// </summary>
        public static bool Enabled { get; set; } = false;

        /// <summary>
        /// Individual trace category switches.
        /// These are only evaluated when Enabled is true.
        /// </summary>
        public static bool InfoEnabled { get; set; } = true;
        public static bool WarningEnabled { get; set; } = true;
        public static bool ErrorEnabled { get; set; } = true;

        /// <summary>
        /// Writes an informational trace message when enabled.
        /// </summary>
        public static void Info(string message)
        {
            if (!Enabled || !InfoEnabled)
                return;

            PXTrace.WriteInformation(message);
        }

        /// <summary>
        /// Writes a warning trace message when enabled.
        /// </summary>
        public static void Warning(string message)
        {
            if (!Enabled || !WarningEnabled)
                return;

            PXTrace.WriteWarning(message);
        }

        /// <summary>
        /// Writes an error trace message when enabled.
        /// </summary>
        public static void Error(string message)
        {
            if (!Enabled || !ErrorEnabled)
                return;

            PXTrace.WriteError(message);
        }
    }
}