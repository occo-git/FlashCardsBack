using Application.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Mapping
{
    public static class ErrorCodeMapper 
    {
        public const string ErrorCode = "ErrorCode";
        public const string ErrAccountNotActive = "ERR_ACCOUNT_NOT_ACTIVE";
        public const string ErrConfirmationFailed = "ERR_CONFIRMATION_FAILED";
        public const string ErrConfirmationLinkMismatch = "ERR_CONFIRMATION_LINK_MISMATCH";
        public const string ErrConfirmationSendFail = "ERR_CONFIRMATION_SEND_FAIL";
        public const string ErrEmailAlreadyConfirmed = "ERR_EMAIL_ALREADY_CONFIRMED";
        public const string ErrEmailNotConfirmed = "ERR_EMAIL_NOT_CONFIRMED";
        public const string ErrTokenInvalidFormat = "ERR_TOKEN_INVALID_FORMAT";
        public const string GenericError = "ERR_ERROR";

        public static string Map(Exception? ex) => ex switch
        {
            null => String.Empty,
            AccountNotActiveException => ErrAccountNotActive,
            ConfirmationFailedException => ErrConfirmationFailed,
            ConfirmationLinkMismatchException => ErrConfirmationLinkMismatch,
            ConfirmationSendFailException => ErrConfirmationSendFail,
            EmailAlreadyConfirmedException => ErrEmailAlreadyConfirmed,
            EmailNotConfirmedException => ErrEmailNotConfirmed,
            TokenInvalidFormatException => ErrTokenInvalidFormat,
            _ => GenericError
        };
    }
}