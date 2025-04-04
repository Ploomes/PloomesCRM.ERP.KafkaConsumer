using PloomesCRMCallbackHub2.Queue.DLL.BasicHelpers;
using System.Threading.Tasks;
using PloomesCRMCallbackHub2.Queue.DLL.Entities;

namespace PloomesCRMCallbackHub2.Queue.DLL.Business
{
    public interface IDataAccess : ILogDataAccess
    {
        Task<bool> CheckIfRequestForThisItemWasAlreadyExecutedAsync(string v);
        #region Send Data to CallbackHub2

        Task<bool>
            SendObjectToCallBackHub2Async( GenericIntegrationObject objToSend,bool delay = false);

        #endregion
        
    }
}