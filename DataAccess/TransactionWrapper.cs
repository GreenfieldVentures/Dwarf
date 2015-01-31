using System;
using System.Runtime.InteropServices;

namespace Evergreen.Dwarf.DataAccess
{
    public interface ITransactionWrapper: IDisposable
    {
        void Commit();
        void Rollback();
    }

    /// <summary>
    /// Class for wrapping multiple IDwarf's DB events into one single transction
    /// </summary>
    public class TransactionWrapper<T> : ITransactionWrapper
    {
        #region Variables

        private bool isTransactionHandled;

        #endregion Variables

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        internal TransactionWrapper()
        {
            DbContextHelper<T>.BeginOperation();
        }

        #endregion Constructors

        #region Properties

        #region DisableExceptionLogging

        /// <summary>
        /// If true, a possible exception will not be logged
        /// </summary>
        public bool DisableExceptionLogging { get; set; }

        #endregion DisableExceptionLogging

        #endregion Properties

        #region Methods

        #region Commit

        /// <summary>
        /// See base
        /// </summary>
        public void Commit()
        {
            if (isTransactionHandled)
                return;

            try
            {
                DbContextHelper<T>.FinalizeOperation(false);
                isTransactionHandled = true;
            }
            catch (Exception ex)
            {
                DbContextHelper<T>.FinalizeOperation(true);

                if (!DisableExceptionLogging)
                    DwarfContext<T>.GetConfiguration().ErrorLogService.Logg(ex);
                
                throw;
            }
            finally
            {
                DbContextHelper<T>.EndOperation();
            }
        }

        #endregion Commit

        #region Rollback

        /// <summary>
        /// See base
        /// </summary>
        public void Rollback()
        {
            if (isTransactionHandled)
                return;
            
            try
            {
                DbContextHelper<T>.FinalizeOperation(true);
                isTransactionHandled = true;
            }
            catch (Exception ex)
            {
                if (!DisableExceptionLogging)
                    DwarfContext<T>.GetConfiguration().ErrorLogService.Logg(ex);

                throw;
            }
            finally
            {
                DbContextHelper<T>.EndOperation();
            }
         }

        #endregion Rollback

        #region BeginTransaction

        /// <summary>
        /// Opens a new transcation
        /// </summary>
        /// <returns></returns>
        public static TransactionWrapper<T> BeginTransaction()
        {
            return new TransactionWrapper<T>();
        }        

        #endregion BeginTransaction

        #region Dispose

        /// <summary>
        /// See base
        /// </summary>
        public void Dispose()
        {
            if (!isTransactionHandled)
            {
                if (Marshal.GetExceptionPointers() != IntPtr.Zero || Marshal.GetExceptionCode() != 0)
                    Rollback();
                else
                    Commit();
            }
        }

        #endregion Dispose

        #endregion Methods
    }
}
