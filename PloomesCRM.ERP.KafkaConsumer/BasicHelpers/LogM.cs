using PloomesCRMCallbackHub2.Queue.DLL.BasicCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace PloomesCRMCallbackHub2.Queue.DLL.BasicHelpers
{
    public abstract class LogM
    {
        #region LogDataAccess and CTOR

        private static ILogDataAccess _ILogDataAccess = null;
        private static SendGridClient _sendGridClient = null;

        public static void Initialize(ILogDataAccess iLogDataAccess)
        {
            if (_ILogDataAccess is null)
            {
                _ILogDataAccess = iLogDataAccess;
                nextEmergencyMessage = DateTime.UtcNow;
                nextCriticalMessage = DateTime.UtcNow;
                nextCumulativeAlertMessage = DateTime.UtcNow;
                nextCumulativeErrorMessage = DateTime.UtcNow;
                alertLogs = 0;
                errorLogs = 0;
            }

            if (string.Equals(ApiInfo.Env, "dev") is false)
            {
                _sendGridClient ??= new SendGridClient(HotSettings.SendGridApiKey);
            }
        }

        #endregion LogDataAccess and CTOR

        #region Notification control variables

        private const int retryDDDelay = 100; //ms

        private const int retryMongoDbDelay = 200; //ms
        //private const int retrySendGridDelay = 500; //ms

        private static DateTime nextEmergencyMessage;
        private const int emergencyMessageInterval = 1; //hour

        private static DateTime nextCriticalMessage;
        private const int criticalMessageInterval = 4; //hours

        private static DateTime nextCumulativeAlertMessage;
        private const int cumulativeAlertMessageInterval = 3; //hours
        private static int alertLogs;
        private static DateTime resetAlertLogs;
        private const int alertLogsToMessage = 10;
        private const int alertLogsMessageInterval = 5; //Minutes

        private static DateTime nextCumulativeErrorMessage;
        private const int cumulativeErrorMessageInterval = 3; //hours
        private static int errorLogs;
        private static DateTime resetErrorLogs;
        private const int errorLogsToMessage = 50;
        private const int errorLogsMessageInterval = 5; //Minutes

        #endregion Notification control variables

        #region Prefixes

        /*
        //Do not use: 'f', 'a', 'c', 'e', 'w', 'n', 'd', 'o' ou 's'. //Best use: 'i'
        //d for Debug.
        //o or s for OK
        Strings beginning with emerg or f (case-insensitive) map to emerg (0)
        Strings beginning with a (case-insensitive) map to alert (1)
        Strings beginning with c (case-insensitive) map to critical (2)
        Strings beginning with e (case-insensitive)—that do not match emerg—map to error (3)
        Strings beginning with w (case-insensitive) map to warning (4)
        Strings beginning with n (case-insensitive) map to notice (5)
        Strings beginning with i (case-insensitive) map to info (6)
        Strings beginning with d, trace or verbose (case-insensitive) map to debug (7)
        Strings beginning with o or s, or matching OK or Success (case-insensitive) map to OK
        All others map to info (6)
        //*/
        private const string EmergencyPrefix = "Failure";
        private const string AlertPrefix = "Alert";
        private const string CriticalPrefix = "Critical";
        private const string ErrorPrefix = "Error";
        private const string WarnPrefix = "Warn";
        private const string NoticePrefix = "Note";
        private const string InfoPrefix = "Info";
        private const string DebugPrefix = "Debug";
        private const string OkPrefix = "Success";

        #endregion Prefixes


        #region Public Log Methods

        public static bool Emergency(string message, DateTime date, string stacktrace, int? account = null,
            int? user = null, [CallerMemberName] string callerName = "", [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                string complement = string.IsNullOrWhiteSpace(stacktrace)
                    ? $"\nMyStack:\n at {callerName} in {callingFilePath}: line {lineNumber}."
                    : $"\nStack:\n{stacktrace}.";
                string prefix = HotSettings.ClusterName.Equals("ploomes-dev") ? WarnPrefix : EmergencyPrefix;
                if (nextEmergencyMessage < DateTime.UtcNow) //Time to send another emergency message
                {
                    nextEmergencyMessage = DateTime.UtcNow.AddHours(emergencyMessageInterval);
                    // SendMessageNotification($"MailApi Emergency: {HotSettings.DDService} {ApiInfo.Version}", "EMERGENCY: \n" + message);
                } //Time to send another emergency message

                LogToDataDogAsync(prefix, message, date, account, user, callerName, complement).Forget();
            }
            catch (Exception e)
            {
                Console.WriteLine($"EmergencyLOG Exception: {e.Message}\n{e.StackTrace}");
            }

            return false;
        }

        public static bool Critical(string message, DateTime date, string stacktrace, int? account = null,
            int? user = null, [CallerMemberName] string callerName = "", [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                string complement = string.IsNullOrWhiteSpace(stacktrace)
                    ? $"\nMyStack:\n at {callerName} in {callingFilePath}: line {lineNumber}."
                    : $"\nStack:\n{stacktrace}.";
                string prefix = HotSettings.ClusterName.Equals("ploomes-dev") ? WarnPrefix : CriticalPrefix;
                if (nextCriticalMessage < DateTime.UtcNow) //Time to send another emergency message
                {
                    nextCriticalMessage = DateTime.UtcNow.AddHours(criticalMessageInterval);
                    //SendMessageNotification($"MailApi Critical: {HotSettings.DDService} {ApiInfo.Version}", "CRITICAL: \n" + message);
                } //Time to send another emergency message

                LogToDataDogAsync(prefix, message, date, account, user, callerName, complement).Forget();
            }
            catch (Exception e)
            {
                Console.WriteLine($"CriticalLOG Exception: {e.Message}\n{e.StackTrace}");
            }

            return false;
        }

        public static bool Alert(string message, DateTime date, string stacktrace, int? account = null,
            int? user = null, [CallerMemberName] string callerName = "", [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                string complement = string.IsNullOrWhiteSpace(stacktrace)
                    ? $"\nMyStack:\n at {callerName} in {callingFilePath}: line {lineNumber}."
                    : $"\nStack:\n{stacktrace}.";
                if (resetAlertLogs < DateTime.UtcNow) //Reset Error Log count
                {
                    alertLogs = 1;
                    resetAlertLogs = DateTime.UtcNow.AddMinutes(alertLogsMessageInterval);
                } //Reset Error Log count
                else //Add to Error Log count
                {
                    alertLogs++;
                    if (alertLogs >= alertLogsToMessage) //Enough logs to send message
                    {
                        if (nextCumulativeAlertMessage <
                            DateTime.UtcNow) //Time to send another cumulative error message
                        {
                            nextCumulativeAlertMessage = DateTime.UtcNow.AddHours(cumulativeAlertMessageInterval);
                            // SendMessageNotification($"MailApi Alerts: {HotSettings.DDService} {ApiInfo.Version}", "ACCUMULATED Alerts: \n" + message);
                        } //Time to send another cumulative error message

                        alertLogs = 0; //Reset counter
                        resetAlertLogs = DateTime.UtcNow.AddMinutes(alertLogsMessageInterval); //Reset counter
                    } //Enough logs to send message
                } //Add to Error Log count

                LogToDataDogAsync(AlertPrefix, message, date, account, user, callerName, complement).Forget();
            }
            catch (Exception e)
            {
                Console.WriteLine($"AlertLOG Exception: {e.Message}\n{e.StackTrace}");
            }

            return false;
        }

        public static bool Error(string message, DateTime date, string stacktrace, int? account = null,
            int? user = null, string accountKey = "", [CallerMemberName] string callerName = "", [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                var complement = string.IsNullOrWhiteSpace(stacktrace)
                    ? $"\nMyStack:\n at {callerName} in {callingFilePath}: line {lineNumber}."
                    : $"\nStack:\n{stacktrace}.";
                if (resetErrorLogs < DateTime.UtcNow) //Reset Error Log count
                {
                    errorLogs = 1;
                    resetErrorLogs = DateTime.UtcNow.AddMinutes(errorLogsMessageInterval);
                } //Reset Error Log count
                else //Add to Error Log count
                {
                    errorLogs++;
                    if (errorLogs >= errorLogsToMessage) //Enough logs to send message
                    {
                        if (nextCumulativeErrorMessage <
                            DateTime.UtcNow) //Time to send another cumulative error message
                        {
                            nextCumulativeErrorMessage = DateTime.UtcNow.AddHours(cumulativeErrorMessageInterval);
                            SendMessageNotificationAsync($"CB2.Queue Errors: {HotSettings.DDService} {ApiInfo.Version}",
                                "ACCUMULATED Errors: \n" + message).Forget();
                        } //Time to send another cumulative error message

                        errorLogs = 0; //Reset counter
                        resetErrorLogs = DateTime.UtcNow.AddMinutes(errorLogsMessageInterval); //Reset counter
                    } //Enough logs to send message
                } //Add to Error Log count

                LogToDataDogAsync(ErrorPrefix, message, date, account, user, callerName, accountKey,complement).Forget();
            }
            catch (Exception e)
            {
                Console.WriteLine($"ErrorLOG Exception: {e.Message}\n{e.StackTrace}");
            }

            return false;
        }

        private static async Task SendMessageNotificationAsync(string subject, string errormessage)
        {
            if (_sendGridClient is not null)
            {
                try
                {
                    var msg = new SendGridMessage()
                    {
                        From = new EmailAddress("noreply@ploomes.com", "Ploomes"),
                        Subject = $"{subject}: {ApiInfo.Version}"
                    };
                    msg.AddTo(new EmailAddress("lucas.pederzini@ploomes.com", "Lucas - Integrações"));
                    msg.AddTo(new EmailAddress("thomas.r@ploomes.com", "Thomas - Integrações"));
                    msg.AddTo(new EmailAddress("vinicius.vassao@ploomes.com", "Vinicius - Integrações"));
                    
                    if(HotSettings.KafkaTopic.Contains("neppo", StringComparison.InvariantCultureIgnoreCase))
                    {
                        msg.AddTo(new EmailAddress("marhaya.abreu@ploomes.com", "Marhaya - Integrações"));
                    }
                    msg.HtmlContent =
                        $"<span style='font-family:trebuchet ms, helvetica, sans-serif;'>Atenção!<br><br>Verifique os logs de erro da fila {ApiInfo.DdVersion}.<br><br><b>LogMessage: " +
                        errormessage + "</b></span>";
                    msg.PlainTextContent =
                        $"Atenção!\n\nVerifique os logs de erro da fila {ApiInfo.DdVersion}.\n\nLogMessage: " +
                        errormessage;
                    var response = await _sendGridClient.SendEmailAsync(msg);
                    if (response.IsSuccessStatusCode)
                    {
                        Info($"Log Notification sucessful sent", DateTime.UtcNow);
                    }
                    else
                    {
                        string temp = await response.Body.ReadAsStringAsync();
                        Error($"Log Notification Failure, Code: {response.StatusCode}, Body: {temp}",
                            DateTime.UtcNow, "Unable to send Notification");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unable to send Notification: {e.Message}.");
                }
            }
        }

        public static void Debug(string message, DateTime date, int? account = null, int? user = null,
            [CallerMemberName] string callerName = "", [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            LogToDataDogAsync(DebugPrefix, message, date, account, user, callerName,
                $"\nMyStack:\n at {callerName} in {callingFilePath}: line {lineNumber}.").Forget();
        }

        public static void Ok(string message, DateTime date, int? account = null, int? user = null,
            [CallerMemberName] string callerName = "", [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            LogToDataDogAsync(OkPrefix, message, date, account, user, callerName).Forget();
        }


        public static bool Warn(string message, DateTime date, int? account = null, int? user = null, string accountKey = "",
            [CallerMemberName] string callerName = "")
        {
            LogToDataDogAsync(WarnPrefix, message, date, account, user, callerName, accountKey).Forget();
            return false;
        }

        public static bool Note(string message, DateTime date, int? account = null, int? user = null,
            [CallerMemberName] string callerName = "")
        {
            LogToDataDogAsync(NoticePrefix, message, date, account, user, callerName).Forget();
            return false;
        }

        public static bool Info(string message, DateTime date, int? account = null, int? user = null, string accountKey = "",
            [CallerMemberName] string callerName = "")
        {
            LogToDataDogAsync(InfoPrefix, message, date, account, user, callerName, accountKey).Forget();
            return true;
        }

        #endregion Public Log Methods


        #region Call LogDataAccessToLog (DD, Mongo, SendGrid)

        private static async Task LogToDataDogAsync(string prefix, string message, DateTime date, int? account,
            int? user, string methodName, string accountKey = null, string complement = null)
        {
            var ddException = "";
            var ddFirstTryFailure = "";
            var ddSecondTryFailure = "";
            try
            {
                //if (string.Equals("ploomes-dev", HotSettings.ClusterName) == false) //Uncomment when finished, so not more dev DD logs
                {
                    var ddLog = new DatadogLog(prefix, $"{message}\n{complement}", date, account, user, methodName, accountKey ?? string.Empty);
                    try //First DD try
                    {
                        var ddLogResult = await _ILogDataAccess.SendLogToDataDogAsync(ddLog);
                        if (string.IsNullOrWhiteSpace(ddLogResult)) //OK, return
                        {
                            return;
                        } //OK, return

                        ddFirstTryFailure = $"\nDDFirstFailure: {ddLogResult}"; //Add Failure text to message
                    }
                    catch (Exception e) //Add Exception text to message
                    {
                        ddFirstTryFailure += $"\nDDFirstException: {e.Message}";
                    } //First DD try

                    try //Second DD try
                    {
                        await Task.Delay(retryDDDelay);
                        var ddLogResult = await _ILogDataAccess.SendLogToDataDogAsync(ddLog);
                        if (string.IsNullOrWhiteSpace(ddLogResult)) //OK on retry, return
                        {
                            return;
                        } //OK on retry, return

                        ddSecondTryFailure = $"\nDDSecondFailure: {ddLogResult}"; //Add Failure text to message
                    }
                    catch (Exception e) //Add Exception text to message
                    {
                        ddSecondTryFailure += $"\nDDSecondException: {e.Message}";
                    } //Second DD try
                    //SendNoDDNotificationLog($"DDFail: {prefix}, {ApiInfo.Version}", $"{message}\nDate: {date}.\nUser: {user}; account: {account}\n Method: {methodName}\nStack: {complement}"); //Unable to log to DD, NOTIFY
                } //Try log to DD
            }
            catch (Exception ex)
            {
                ddException = $"\nDDException: {ex.Message}";
            }
        }

        #endregion Call LogDataAccessToLog  (DD, Mongo, SendGrid)
    }
}