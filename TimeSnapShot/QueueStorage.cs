using Microsoft.WindowsAzure.Storage;
using System;

namespace QueueStorage
{
    public class Common
    {
        //get this from azure
        const string storageConnectionString = "DefaultEndpointsProtocol=https;AccountName=abccortanademo8695;AccountKey=vAXgpMK/MeQzqptQzKtx5fhz1v0m13bwjdTqjv7GCApvNrHbn114unCeM9rqtsCIqcUItkGytll5MVz1gwkUgQ==;EndpointSuffix=core.windows.net";

        /// <summary>
        /// Validate the connection string information in app.config and throws an exception if it looks like 
        /// the user hasn't updated this to valid values. 
        /// </summary>
        /// <param name="storageConnectionString">The storage connection string</param>
        /// <returns>CloudStorageAccount object</returns>
        public static CloudStorageAccount CreateStorageAccountFromConnectionString(string _storageConnectionString)
        {
            CloudStorageAccount storageAccount;
            try
            {
                var cnString = string.IsNullOrEmpty(_storageConnectionString) ? storageConnectionString : _storageConnectionString ;
                System.Diagnostics.Trace.WriteLine(cnString);

                storageAccount = CloudStorageAccount.Parse(cnString);
            }
            catch (FormatException)
            {
                System.Diagnostics.Trace.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                throw;
            }
            catch (ArgumentException)
            {
                System.Diagnostics.Trace.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                throw;
            }

            return storageAccount;
        }

        public static void WriteException(Exception ex)
        {
            System.Diagnostics.Trace.WriteLine(string.Format("Exception thrown. {0}, msg = {1}", ex.Source, ex.Message));
        }

    }
}
