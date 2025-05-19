// ''''''''''''''''''''''''''''''''''''''''''''''''''''''
// 
//  Author: Kashif Iqbal Khan
//  Create Date: Thursday 19th July, 2007
//  Comments: 
// 
// ''''''''''''''''''''''''''''''''''''''''''''''''''''''

using System;

namespace ReqIFBridge.Utility
{
	/// <summary>
	/// Summary description for OperationResult.
	/// </summary>
	
	#region class OperationResult

	public sealed class OperationResult
	{
		
		private OperationStatusTypes operationStatus = OperationStatusTypes.Failed;
		private String operationMessage = String.Empty;
		private Object tag;
		private Exception exceptionObject;
        private Object secondaryObject;

        public static readonly OperationResult SuccessWithNoMessage = new OperationResult(OperationStatusTypes.Success);

		public static readonly OperationResult SuccessWithMessage = new OperationResult(OperationStatusTypes.Success);
		#region Constructors

		public OperationResult(OperationStatusTypes status)
		{
			this.operationStatus = status;
		}

		public OperationResult(OperationStatusTypes status, String message)
		{
			this.operationStatus = status;
			this.Message = message;
		}

		public OperationResult(OperationStatusTypes status, Object tag)
		{
			this.operationStatus = status;
			this.tag = tag;
		}
		public OperationResult(OperationStatusTypes status, Object tag,Exception exceptionObject)
		{
			this.operationStatus = status;
			this.tag = tag;
			this.exceptionObject = exceptionObject;
		}
        public OperationResult(OperationStatusTypes status, Object tag, Object secondaryObject)
        {
            this.operationStatus = status;
            this.tag = tag;
            this.secondaryObject = secondaryObject;
        }

        #endregion

        #region Properties


        public OperationStatusTypes OperationStatus
		{
			get
			{
				return this.operationStatus;
			}
		}

		public string Message
		{
			get
			{
				return this.operationMessage;
			}
			set
			{
				if (value == null)
				{
					this.operationMessage = String.Empty;
				}
				else
				{
					this.operationMessage = value;
				}
			}
		}
		
		public Object Tag
		{
			get
			{
				return this.tag;
			}
		}
		
		public Exception ExceptionObject
		{
			get
			{
				return exceptionObject;
			}
		}

        public Object SecondaryObject
        {
            get
            {
                return this.secondaryObject;
            }
        }

        #endregion

        public override string ToString()
		{
			return "[Status:" + operationStatus + "; Message:" + operationMessage + "]";
 		}

	} // end class 
	
	#endregion

	#region enum OperationStatusTypes
	
	public enum OperationStatusTypes
	{
		/// <summary>
		/// Operation was completed successfully
		/// </summary>
		Success = 1,
		/// <summary>
		/// Operation failed
		/// </summary>
		Failed = 2,
		/// <summary>
		/// Operation was canceled by User
		/// </summary>
		InProgress = 4,
		/// <summary>
		/// Operation was aborted in middle
		/// </summary>
		Aborted = 8
	}
	
	#endregion

} // end namespace

